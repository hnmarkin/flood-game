using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class ZoneOutlineByHover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;
    [SerializeField] private Tilemap groundTilemap;

    [Header("Line Rendering")]
    [SerializeField] private LineRenderer linePrefab; // assign a LineRenderer prefab OR a LineRenderer component on a prefab GO
    [SerializeField] private float zOffset = -0.1f;   // bring outline slightly “in front” (depends on your sorting)
    [SerializeField] private float simplifyEpsilon = 0.001f; // optional, keep tiny

    [Header("Behavior")]
    [SerializeField] private bool outliningEnabled = true;

    // GeoID -> cells in that zone
    private Dictionary<string, HashSet<Vector3Int>> geoidToCells = new();

    private string currentHoverGeoid = null;
    private readonly List<LineRenderer> activeLines = new();

    void Awake()
    {
        if (!sceneCamera) sceneCamera = Camera.main;
    }

    void Start()
    {
        BuildZoneIndex();
    }

    void Update()
    {
        if (!outliningEnabled) return;
        if (sceneCamera == null || jsonMapLoader == null || groundTilemap == null) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouse = Input.mousePosition;
        Vector3 world = sceneCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -sceneCamera.transform.position.z));

        int r, c, pop;
        string category, geoid;
        bool ok = jsonMapLoader.TryGetTileInfoAtWorld(world, out r, out c, out category, out geoid, out pop);

        if (!ok || string.IsNullOrEmpty(geoid))
        {
            ClearOutline();
            currentHoverGeoid = null;
            return;
        }

        if (geoid == currentHoverGeoid) return;

        currentHoverGeoid = geoid;
        DrawOutlineForGeoid(geoid);
    }

    // Hook to a button if you want
    public void ToggleOutlining()
    {
        outliningEnabled = !outliningEnabled;
        if (!outliningEnabled)
        {
            ClearOutline();
            currentHoverGeoid = null;
        }
    }

    public void BuildZoneIndex()
    {
        geoidToCells.Clear();

        if (jsonMapLoader == null || jsonMapLoader.cellToRC == null || jsonMapLoader.geoidGrid == null)
            return;

        foreach (var kvp in jsonMapLoader.cellToRC)
        {
            Vector3Int cell = kvp.Key;
            Vector2Int rc = kvp.Value;

            int r = rc.x;
            int c = rc.y;

            string g = jsonMapLoader.geoidGrid[r, c];
            if (string.IsNullOrEmpty(g)) continue;

            if (!geoidToCells.TryGetValue(g, out var set))
            {
                set = new HashSet<Vector3Int>();
                geoidToCells[g] = set;
            }
            set.Add(new Vector3Int(cell.x, cell.y, 0));
        }

        Debug.Log($"[ZoneOutlineByHover] Indexed {geoidToCells.Count} zones.");
    }

    private void DrawOutlineForGeoid(string geoid)
    {
        ClearOutline();

        if (!geoidToCells.TryGetValue(geoid, out var zoneCells) || zoneCells.Count == 0)
            return;

        // 1) Extract boundary edges in grid-space (corners on integer lattice)
        var edges = ExtractBoundaryEdges(zoneCells);

        // 2) Stitch edges into loops (each loop is a list of grid points)
        var loops = StitchEdgesIntoLoops(edges);

        // 3) Convert grid points -> world points and draw line(s)
        foreach (var loop in loops)
        {
            if (loop.Count < 3) continue;

            var lr = InstantiateLine();
            var pts = new Vector3[loop.Count];

            for (int i = 0; i < loop.Count; i++)
            {
                // Convert grid corner (x,y) to world position.
                // We interpret grid corner coordinates relative to cell centers:
                // corner (cx,cy) corresponds to groundTilemap.CellToWorld(new Vector3Int(cx,cy,0)) but that's cell origin.
                // Best: use CellToWorld for cell origin and add half-cell offsets.
                Vector2Int gp = loop[i];
                Vector3 w = GridCornerToWorld(gp.x, gp.y);
                w.z += zOffset;
                pts[i] = w;
            }

            // Optional: simplify could be added later. Keeping as-is for reliability.
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);

            activeLines.Add(lr);
        }
    }

    private LineRenderer InstantiateLine()
    {
        if (linePrefab == null)
        {
            // fallback: create one
            var go = new GameObject("ZoneOutlineLine");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.widthMultiplier = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
            return lr;
        }

        var inst = Instantiate(linePrefab, transform);
        inst.gameObject.SetActive(true);
        inst.loop = true;
        inst.useWorldSpace = true;
        return inst;
    }

    private void ClearOutline()
    {
        for (int i = 0; i < activeLines.Count; i++)
        {
            if (activeLines[i] != null)
                Destroy(activeLines[i].gameObject);
        }
        activeLines.Clear();
    }

    // ----------------------
    // Boundary Edge Extraction
    // ----------------------

    // Represents an undirected edge between two grid points
    private struct Edge
    {
        public Vector2Int a;
        public Vector2Int b;

        public Edge(Vector2Int a, Vector2Int b)
        {
            this.a = a;
            this.b = b;
        }
    }

    private static List<Edge> ExtractBoundaryEdges(HashSet<Vector3Int> zoneCells)
    {
        // Each cell is [x,x+1]x[y,y+1] in grid-corner space.
        // Boundary edges are edges where neighbor cell is not in zone.
        var edges = new List<Edge>();

        foreach (var c in zoneCells)
        {
            int x = c.x;
            int y = c.y;

            // Neighbor checks (4-dir)
            bool hasN = zoneCells.Contains(new Vector3Int(x, y + 1, 0));
            bool hasS = zoneCells.Contains(new Vector3Int(x, y - 1, 0));
            bool hasE = zoneCells.Contains(new Vector3Int(x + 1, y, 0));
            bool hasW = zoneCells.Contains(new Vector3Int(x - 1, y, 0));

            // corners in grid point space
            var bl = new Vector2Int(x, y);
            var br = new Vector2Int(x + 1, y);
            var tl = new Vector2Int(x, y + 1);
            var tr = new Vector2Int(x + 1, y + 1);

            // If no neighbor on that side, that side is boundary => add edge
            if (!hasS) edges.Add(new Edge(bl, br)); // bottom
            if (!hasN) edges.Add(new Edge(tl, tr)); // top
            if (!hasW) edges.Add(new Edge(bl, tl)); // left
            if (!hasE) edges.Add(new Edge(br, tr)); // right
        }

        return edges;
    }

    // ----------------------
    // Stitch edges into loops
    // ----------------------

    private static List<List<Vector2Int>> StitchEdgesIntoLoops(List<Edge> edges)
    {
        // Build adjacency map: point -> list of connected points
        var adj = new Dictionary<Vector2Int, List<Vector2Int>>();
        void AddAdj(Vector2Int u, Vector2Int v)
        {
            if (!adj.TryGetValue(u, out var list))
            {
                list = new List<Vector2Int>();
                adj[u] = list;
            }
            list.Add(v);
        }

        foreach (var e in edges)
        {
            AddAdj(e.a, e.b);
            AddAdj(e.b, e.a);
        }

        var loops = new List<List<Vector2Int>>();
        var used = new HashSet<(Vector2Int, Vector2Int)>();

        foreach (var e in edges)
        {
            // start from an unused directed edge
            if (used.Contains((e.a, e.b)) && used.Contains((e.b, e.a)))
                continue;

            var loop = new List<Vector2Int>();
            Vector2Int start = e.a;
            Vector2Int current = e.a;
            Vector2Int prev = e.b; // "fake prev" to choose a direction, we'll correct below

            // pick a real next neighbor from current
            Vector2Int next = e.b;

            loop.Add(current);

            int safety = 0;
            while (safety++ < 100000)
            {
                used.Add((current, next));
                prev = current;
                current = next;
                loop.Add(current);

                if (current == start)
                    break;

                if (!adj.TryGetValue(current, out var neighbors) || neighbors.Count == 0)
                    break;

                // Choose the next neighbor that isn't the edge we just came from, and isn't used if possible
                Vector2Int candidate = neighbors[0];

                if (neighbors.Count == 1)
                {
                    candidate = neighbors[0];
                }
                else
                {
                    // Prefer neighbor that isn't prev and not yet used
                    candidate = prev;
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        var n = neighbors[i];
                        if (n == prev) continue;
                        if (!used.Contains((current, n)))
                        {
                            candidate = n;
                            break;
                        }
                        candidate = n;
                    }
                }

                next = candidate;
            }

            // Remove duplicate last point if needed (LineRenderer loop can handle it, but keeping clean)
            if (loop.Count > 1 && loop[loop.Count - 1] == loop[0])
            {
                // keep it closed; LineRenderer loop = true is fine either way
            }

            if (loop.Count >= 4)
                loops.Add(loop);
        }

        return loops;
    }

    // ----------------------
    // Grid corner -> World
    // ----------------------

    private Vector3 GridCornerToWorld(int cornerX, int cornerY)
    {
        // In a Tilemap, cell (x,y) has a world origin at CellToWorld.
        // A "corner" point at (cornerX, cornerY) corresponds to the origin of that cell coordinate in corner space.
        // Use CellToWorld on that coordinate directly; this maps nicely for isometric layouts too.
        Vector3 w = groundTilemap.CellToWorld(new Vector3Int(cornerX, cornerY, 0));

        // For many tilemaps, CellToWorld gives the bottom-left of the cell.
        // For isometric, it maps the diamond corners consistently.
        // If your outline appears offset, we can add a half-cell adjustment here.
        return w;
    }
}
