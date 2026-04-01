using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;


public class FloodDefenseBoxStamp : MonoBehaviour, IBarrierProvider
{
    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Camera mainCamera;

    private NpcToastController _npcToast;

    [Header("Zone Outline Visual (Committed)")]
    [SerializeField] private LineRenderer zoneOutlinePrefab;   // assign your yellow line prefab here
    [SerializeField] private Transform zoneOutlineParent;      // optional parent (can be this.transform)
    [SerializeField] private float outlineZOffset = -0.1f;     // adjust if hidden
    [SerializeField] private float outlineWidth = 0.25f;       // make visible
    [SerializeField] private int outlineSortingOrder = 50;     // above tilemaps

    // Keep committed outlines so they persist
    private readonly Dictionary<string, List<LineRenderer>> _committedOutlines = new();

    [Header("Build Mode")]
    [SerializeField] private bool buildMode = false;

    [Header("Barrier Logic")]
    [SerializeField] private float barrierHeight = 40f; // big wall
    [SerializeField] private float seepage = 0f;

    [Header("Preview")]
    [SerializeField] private Tilemap previewTilemap;   // BarrierPreviewTilemap
    [SerializeField] private TileBase previewTile;     // a grey/transparent tile

    [Header("Close Loop")]
    [SerializeField] private bool closeLoopOnCommit = true;

    [Header("Zone Boundary Mode")]
    [SerializeField] private bool zoneBoundaryMode = false;
    [SerializeField] private JsonMapLoader jsonMapLoader;  // needed to get geoid and zone membership

    [Header("Zone Outline (Committed)")]
    [SerializeField] private Tilemap zoneOutlineTilemap;    // <-- assign your ZoneOutlineTilemap here
    [SerializeField] private TileBase zoneOutlineTile;      // <-- a simple 1x1 pixel tile (we’ll tint it)
    [SerializeField] private Color zoneOutlineColor = new Color(1f, 0.85f, 0.15f, 1f); // hazard yellow
    [SerializeField] private bool drawCommittedOutline = true;

    // optional: stop placing sandbag prefabs for zone mode
    [SerializeField] private bool placeSandbagPrefabsInZoneMode = false;

    // Track zones already committed so you can’t re-outline forever
    private readonly HashSet<string> _committedZones = new();

    // Cache: geoid -> tiles in that zone (tile coords in [0..N-1])
    private readonly Dictionary<string, HashSet<Vector2Int>> _geoidToTiles = new();

    // Hover state for zone mode
    private string _hoverGeoid = null;
    private HashSet<Vector2Int> _hoverZoneTiles = null;

    // Optional: if you have a separate green preview tile, assign it.
    // If null, we’ll just tint the preview tilemap green when “closed”.
    [SerializeField] private TileBase closedPreviewTile = null;

    // Normal / closed colors
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color closedPreviewColor = new Color(0.2f, 1f, 0.2f, 0.55f);


    [Header("Optional Visuals")]
    [SerializeField] private GameObject sandbagPrefab;
    [SerializeField] private Transform sandbagParent;
    [SerializeField] private float visualZ = -1f;

    private float[,] _barrierHX, _barrierHY, _seepX, _seepY;
    private int[,] _blockedX, _blockedY;

    private Vector2Int _tileOrigin;

    private readonly Dictionary<(int x, int y), GameObject> _xEdgeVisuals = new();
    private readonly Dictionary<(int x, int y), GameObject> _yEdgeVisuals = new();

    // -------- New: polyline placement state --------
    private readonly List<Vector2Int> _pillars = new();
    private Vector2Int _lastHoverTile = new Vector2Int(int.MinValue, int.MinValue);

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        InitializeArraysAndOrigin();
        BuildZoneIndex();
        ClearPreviewAndSelection();
        _npcToast = FindFirstObjectByType<NpcToastController>();
        if (_npcToast == null)
            Debug.LogWarning("[FloodDefenseBoxStamp] NpcToastController not found in scene.");
        else
            Debug.Log("[FloodDefenseBoxStamp] NpcToastController found OK.");
    }

    // Hook your UI button to THIS (instead of EnterBuildModeFromUI/ExitBuildModeFromUI)
    public void ToggleBuildModeFromUI()
    {
        buildMode = !buildMode;

        if (!buildMode)
        {
            ClearPreviewAndSelection();
            Debug.Log("[FloodDefenseBoxStamp] Build mode OFF.");
        }
        else
        {
            Debug.Log("[FloodDefenseBoxStamp] Build mode ON. Left click to add pillars, Right click to commit.");
        }
    }

    // If you still need these for existing UI wiring, keep them as wrappers.
    public void EnterBuildModeFromUI()
    {
        if (!buildMode) ToggleBuildModeFromUI();
        else Debug.Log("[FloodDefenseBoxStamp] Build mode already ON.");
    }

    public void ExitBuildModeFromUI()
    {
        if (buildMode) ToggleBuildModeFromUI();
        else Debug.Log("[FloodDefenseBoxStamp] Build mode already OFF.");
    }

    public void EnterZoneBoundaryPlacementMode()
    {
        buildMode = false;
        ClearPreviewAndSelection();

        zoneBoundaryMode = true;
        _hoverGeoid = null;
        _hoverZoneTiles = null;

        // Build zone index after loader has definitely finished its Start/Awake work
        StopAllCoroutines();
        StartCoroutine(InitZoneModeNextFrame());

        Debug.Log("[FloodDefenseBoxStamp] Zone boundary mode ON. Hover a zone and left-click to barricade.");
    }

    private IEnumerator InitZoneModeNextFrame()
    {
        // Wait one frame so JsonMapLoader has a chance to finish initializing
        yield return null;

        BuildZoneIndex();

        // Hard diagnostic
        Debug.Log($"[FloodDefenseBoxStamp] Zone index built: {_geoidToTiles.Count} geoids. " +
                $"jsonMapLoader={(jsonMapLoader ? "OK" : "NULL")} " +
                $"tileMapData={(tileMapData ? "OK" : "NULL")} origin={_tileOrigin}");
    }

    public void ExitZoneBoundaryPlacementMode()
    {
        zoneBoundaryMode = false;
        _hoverGeoid = null;
        _hoverZoneTiles = null;

        if (previewTilemap) previewTilemap.ClearAllTiles();
        Debug.Log("[FloodDefenseBoxStamp] Zone boundary mode OFF.");
    }

    private void Update()
    {
        // Zone mode should run even when buildMode is OFF
        if (zoneBoundaryMode)
        {
            ZoneBoundaryUpdate();
            return;
        }

        // existing polyline build mode
        if (!buildMode) return;
        if (tileMapData == null || terrainTilemap == null || mainCamera == null) return;

        // ESC cancels the current uncommitted selection (keeps build mode ON)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearPreviewAndSelection();
            Debug.Log("[FloodDefenseBoxStamp] Selection cleared (ESC).");
            return;
        }

        UpdateHoverPreviewPolyline();

        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int tile = MouseToTile();
            if (tile.x < 0) return;

            if (_pillars.Count == 0 || _pillars[_pillars.Count - 1] != tile)
            {
                _pillars.Add(tile);
                UpdateHoverPreviewPolyline(force: true);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            CommitPolylineBarrier();
        }
    }

    private void InitializeArraysAndOrigin()
    {
        if (tileMapData == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] tileMapData is null.");
            return;
        }

        int w = tileMapData.GridWidth;   // N+2
        int h = tileMapData.GridHeight;  // N+2

        _barrierHX = new float[w, h];
        _barrierHY = new float[w, h];
        _seepX = new float[w, h];
        _seepY = new float[w, h];

        _blockedX = new int[w, h];
        _blockedY = new int[w, h];

        terrainTilemap.CompressBounds();
        var b = terrainTilemap.cellBounds;
        _tileOrigin = new Vector2Int(b.xMin, b.yMin);

        Debug.Log($"[FloodDefenseBoxStamp] Initialized. Tile origin={_tileOrigin}.");
    }

    private Vector2Int MouseToTile()
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        Vector3Int cell = terrainTilemap.WorldToCell(world);

        int tx = cell.x - _tileOrigin.x;
        int ty = cell.y - _tileOrigin.y;

        int N = tileMapData.N;
        if (tx < 0 || ty < 0 || tx >= N || ty >= N)
        {
            return new Vector2Int(-1, -1);
        }

        if (tileMapData.Get(new Vector2Int(tx, ty)) == null)
        {
            return new Vector2Int(-1, -1);
        }

        return new Vector2Int(tx, ty);
    }

    // ----------------- NEW: Polyline placement -----------------

    private void CommitPolylineBarrier()
    {
        if (_pillars.Count < 2)
        {
            ClearPreviewAndSelection();
            return;
        }

        // Collect all "wall tiles" along the polyline (and closing segment if enabled)
        var wallTiles = new HashSet<Vector2Int>();

        // segments between pillars
        for (int i = 0; i < _pillars.Count - 1; i++)
        {
            var tiles = GetLineTiles(_pillars[i], _pillars[i + 1]);
            foreach (var t in tiles) wallTiles.Add(t);
        }

        // closing segment last -> first
        if (closeLoopOnCommit && _pillars.Count >= 3)
        {
            var first = _pillars[0];
            var last  = _pillars[_pillars.Count - 1];
            if (last != first)
            {
                var closeTiles = GetLineTiles(last, first);
                foreach (var t in closeTiles) wallTiles.Add(t);
            }
        }

        // Now make each wall tile "solid" by blocking its perimeter edges
        foreach (var t in wallTiles)
            BlockAllEdgesAroundTile(t);

        Debug.Log($"[FloodDefenseBoxStamp] Committed barrier tiles={wallTiles.Count}, pillars={_pillars.Count}");

        ClearPreviewAndSelection();
    }


    // Bresenham line on grid, returns tile coords including endpoints
    private List<Vector2Int> GetLineTiles(Vector2Int start, Vector2Int end)
    {
        var result = new List<Vector2Int>();

        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            result.Add(new Vector2Int(x0, y0));
            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        // IMPORTANT: Bresenham can create diagonal adjacency; we want 4-neighbor steps for edges.
        // Expand diagonals into orthogonal steps.
        return ExpandTo4NeighborSteps(result);
    }

    private List<Vector2Int> ExpandTo4NeighborSteps(List<Vector2Int> coarse)
    {
        if (coarse.Count <= 1) return coarse;

        var expanded = new List<Vector2Int> { coarse[0] };

        for (int i = 0; i < coarse.Count - 1; i++)
        {
            Vector2Int a = coarse[i];
            Vector2Int b = coarse[i + 1];
            Vector2Int d = b - a;

            // If already 4-neighbor adjacent, keep it
            if (Mathf.Abs(d.x) + Mathf.Abs(d.y) == 1)
            {
                expanded.Add(b);
                continue;
            }

            // If diagonal step, split into two orthogonal steps.
            // Choose an order: X then Y (you can swap if you prefer)
            if (Mathf.Abs(d.x) == 1 && Mathf.Abs(d.y) == 1)
            {
                var mid = new Vector2Int(a.x + d.x, a.y);
                expanded.Add(mid);
                expanded.Add(b);
                continue;
            }

            // If something larger slipped through (shouldn't), step it Manhattan-style
            Vector2Int cur = a;
            while (cur != b)
            {
                if (cur.x != b.x) cur.x += (b.x > cur.x) ? 1 : -1;
                else if (cur.y != b.y) cur.y += (b.y > cur.y) ? 1 : -1;
                expanded.Add(cur);
            }
        }

        return expanded;
    }

    private void UpdateHoverPreviewPolyline(bool force = false)
    {
        if (previewTilemap == null || previewTile == null) return;

        Vector2Int hover = MouseToTile();
        if (hover.x < 0)
        {
            // off-grid: show only committed segments (no live segment)
            hover = new Vector2Int(int.MinValue, int.MinValue);
        }

        if (!force && hover == _lastHoverTile) return;
        _lastHoverTile = hover;

        bool showClosed = IsClosedPreview(hover);

        // Color feedback: green when a closed loop is formed/ready
        previewTilemap.color = showClosed ? closedPreviewColor : previewColor;

        previewTilemap.ClearAllTiles();

        // Select which tile to draw with
        TileBase drawTile = (showClosed && closedPreviewTile != null) ? closedPreviewTile : previewTile;

        // No pillars yet: just show hover hint
        if (_pillars.Count == 0)
        {
            if (hover.x != int.MinValue)
                SetPreviewTile(hover.x, hover.y, drawTile);
            return;
        }

        // Draw pillars
        foreach (var p in _pillars)
            SetPreviewTile(p.x, p.y, drawTile);

        // Draw committed segments
        for (int i = 0; i < _pillars.Count - 1; i++)
        {
            var tiles = GetLineTiles(_pillars[i], _pillars[i + 1]);
            foreach (var t in tiles)
                SetPreviewTile(t.x, t.y, drawTile);
        }

        // Draw live segment from last pillar -> hover (if valid)
        if (hover.x != int.MinValue)
        {
            var last = _pillars[_pillars.Count - 1];
            var liveTiles = GetLineTiles(last, hover);
            foreach (var t in liveTiles)
                SetPreviewTile(t.x, t.y, drawTile);
        }

        // If it’s “ready to close”, also show the closing segment explicitly:
        // last -> first (especially helpful if hover isn't exactly first due to input jitter)
        if (showClosed && closeLoopOnCommit)
        {
            var last = _pillars[_pillars.Count - 1];
            var first = _pillars[0];

            // If already closed, no need to redraw, but it doesn’t hurt.
            var closeTiles = GetLineTiles(last, first);
            foreach (var t in closeTiles)
                SetPreviewTile(t.x, t.y, drawTile);
        }
    }

    private void BuildZoneIndex()
    {
        _geoidToTiles.Clear();

        if (jsonMapLoader == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] BuildZoneIndex failed: jsonMapLoader is NULL (assign it in Inspector).");
            return;
        }

        if (tileMapData == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] BuildZoneIndex failed: tileMapData is NULL.");
            return;
        }

        if (jsonMapLoader.payload == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] BuildZoneIndex: jsonMapLoader.payload is NULL (not ready yet).");
            return;
        }

        if (jsonMapLoader.cellToRC == null || jsonMapLoader.cellToRC.Count == 0)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] BuildZoneIndex: jsonMapLoader.cellToRC empty (not ready yet).");
            return;
        }

        // Build from jsonMapLoader.cellToRC + geoidGrid
        foreach (var kvp in jsonMapLoader.cellToRC)
        {
            var cell = kvp.Key;
            var rc = kvp.Value;

            int r = rc.x;
            int c = rc.y;

            string g = jsonMapLoader.geoidGrid[r, c];
            if (string.IsNullOrEmpty(g)) continue;

            int tx = cell.x - _tileOrigin.x;
            int ty = cell.y - _tileOrigin.y;

            int N = tileMapData.N;
            if (tx < 0 || ty < 0 || tx >= N || ty >= N) continue;

            if (!_geoidToTiles.TryGetValue(g, out var set))
            {
                set = new HashSet<Vector2Int>();
                _geoidToTiles[g] = set;
            }

            set.Add(new Vector2Int(tx, ty));
        }

        Debug.Log($"[FloodDefenseBoxStamp] BuildZoneIndex OK: {_geoidToTiles.Count} geoids.");
    }

    private void ZoneBoundaryUpdate()
    {
        if (jsonMapLoader == null || terrainTilemap == null || mainCamera == null) return;

        // ESC cancels zone selection mode
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitZoneBoundaryPlacementMode();
            return;
        }

        // Hover geoid under cursor
        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        int r, c, pop;
        string category, geoid;

        bool hit = jsonMapLoader.TryGetTileInfoAtWorld(world, out r, out c, out category, out geoid, out pop);
        if (!hit || string.IsNullOrEmpty(geoid) || !_geoidToTiles.TryGetValue(geoid, out var zoneTiles))
        {
            _hoverGeoid = null;
            _hoverZoneTiles = null;
            if (previewTilemap) previewTilemap.ClearAllTiles();
            return;
        }

        // If hover changed, redraw preview
        if (_hoverGeoid != geoid)
        {
            _hoverGeoid = geoid;
            _hoverZoneTiles = zoneTiles;
            DrawZoneBoundaryPreview(zoneTiles);
        }

        // Left click commits the barricade
        if (Input.GetMouseButtonDown(0))
        {
            CommitZoneBoundaryBarrier(zoneTiles);
            ExitZoneBoundaryPlacementMode();
        }
    }

    private void CommitZoneBoundaryBarrier(HashSet<Vector2Int> zoneTiles)
    {
        if (zoneTiles == null || zoneTiles.Count == 0) return;

        foreach (var t in zoneTiles)
        {
            var left  = new Vector2Int(t.x - 1, t.y);
            var right = new Vector2Int(t.x + 1, t.y);
            var down  = new Vector2Int(t.x, t.y - 1);
            var up    = new Vector2Int(t.x, t.y + 1);

            int sx = t.x + 1;
            int sy = t.y + 1;

            if (!zoneTiles.Contains(left))  SetBlockedX_NoVisual(sx,     sy, true);
            if (!zoneTiles.Contains(right)) SetBlockedX_NoVisual(sx + 1, sy, true);
            if (!zoneTiles.Contains(down))  SetBlockedY_NoVisual(sx, sy,     true);
            if (!zoneTiles.Contains(up))    SetBlockedY_NoVisual(sx, sy + 1, true);
        }

        // ✅ Paint permanent outline on ZoneOutlineTilemap
        PaintCommittedZoneOutline(_hoverGeoid, zoneTiles);

        if (_npcToast != null)
        {
            _npcToast.Show("Strong decision. Sandbagging a high vulnerable zone helps protect the hospital and many nearby residents. Sandbags can slow floodwaters, buying valuable time during a storm. In future rounds, combining barriers with other measures—like drainage improvements or early warnings—could strengthen your overall flood defense.");
        }

        if (!string.IsNullOrEmpty(_hoverGeoid))
        {
            DrawCommittedZoneOutline(_hoverGeoid, zoneTiles);
        }

        Debug.Log($"[FloodDefenseBoxStamp] Zone boundary committed. Zone tiles={zoneTiles.Count}");

        Debug.Log($"[FloodDefenseBoxStamp] Zone boundary committed. Zone tiles={zoneTiles.Count}");
    }



    private void DrawZoneBoundaryPreview(HashSet<Vector2Int> zoneTiles)
    {
        if (previewTilemap == null || previewTile == null) return;

        previewTilemap.ClearAllTiles();
        previewTilemap.color = closedPreviewColor; // use green to indicate boundary mode

        foreach (var t in zoneTiles)
        {
            if (IsBoundaryTile(t, zoneTiles))
                SetPreviewTile(t.x, t.y, previewTile);
        }
    }

    private bool IsBoundaryTile(Vector2Int t, HashSet<Vector2Int> zoneTiles)
    {
        // boundary if any 4-neighbor is missing
        if (!zoneTiles.Contains(new Vector2Int(t.x - 1, t.y))) return true;
        if (!zoneTiles.Contains(new Vector2Int(t.x + 1, t.y))) return true;
        if (!zoneTiles.Contains(new Vector2Int(t.x, t.y - 1))) return true;
        if (!zoneTiles.Contains(new Vector2Int(t.x, t.y + 1))) return true;
        return false;
    }

    private void PaintCommittedZoneOutline(string geoid, HashSet<Vector2Int> zoneTiles)
    {
        if (!drawCommittedOutline) return;
        if (zoneOutlineTilemap == null || zoneOutlineTile == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] zoneOutlineTilemap or zoneOutlineTile is not assigned.");
            return;
        }

        // Optional: only paint once per zone
        if (!string.IsNullOrEmpty(geoid) && _committedZones.Contains(geoid))
            return;

        zoneOutlineTilemap.color = zoneOutlineColor;

        foreach (var t in zoneTiles)
        {
            if (!IsBoundaryTile(t, zoneTiles)) continue;

            // same conversion you already use for preview tilemap
            var cell = new Vector3Int(t.x + _tileOrigin.x, t.y + _tileOrigin.y, 0);
            zoneOutlineTilemap.SetTile(cell, zoneOutlineTile);
        }

        zoneOutlineTilemap.RefreshAllTiles();

        if (!string.IsNullOrEmpty(geoid))
            _committedZones.Add(geoid);

        Debug.Log($"[FloodDefenseBoxStamp] Painted committed outline for zone {geoid}.");
    }


    private void SetPreviewTile(int tx, int ty, TileBase tile)
    {
        var cell = new Vector3Int(tx + _tileOrigin.x, ty + _tileOrigin.y, 0);
        previewTilemap.SetTile(cell, tile);
    }

    private void SetPreviewTile(int tx, int ty)
    {
        SetPreviewTile(tx, ty, previewTile);
    }


    private void ClearPreviewAndSelection()
    {
        _pillars.Clear();
        _lastHoverTile = new Vector2Int(int.MinValue, int.MinValue);

        if (previewTilemap != null)
            previewTilemap.ClearAllTiles();
    }

    private void BlockAllEdgesAroundTile(Vector2Int t)
    {
        // Tile (tx,ty) corresponds to sim cell (sx,sy) = (tx+1, ty+1)
        int sx = t.x + 1;
        int sy = t.y + 1;

        // Left & Right edges (X edges)
        SetBlockedX(sx,     sy, true); // left edge
        SetBlockedX(sx + 1, sy, true); // right edge

        // Bottom & Top edges (Y edges)
        SetBlockedY(sx, sy,     true); // bottom edge
        SetBlockedY(sx, sy + 1, true); // top edge
    }


    // ----------------- Existing barrier setters + visuals -----------------

    private void SetBlockedX(int x, int y, bool blocked)
    {
        if (x < 0 || y < 0 || x >= _blockedX.GetLength(0) || y >= _blockedX.GetLength(1)) return;

        _blockedX[x, y] = blocked ? 1 : 0;
        _barrierHX[x, y] = blocked ? barrierHeight : 0f;
        _seepX[x, y] = blocked ? seepage : 0f;

        if (blocked) UpdateXEdgeVisual(x, y);
        else RemoveXEdgeVisual(x, y);
    }

    private void SetBlockedY(int x, int y, bool blocked)
    {
        if (x < 0 || y < 0 || x >= _blockedY.GetLength(0) || y >= _blockedY.GetLength(1)) return;

        _blockedY[x, y] = blocked ? 1 : 0;
        _barrierHY[x, y] = blocked ? barrierHeight : 0f;
        _seepY[x, y] = blocked ? seepage : 0f;

        if (blocked) UpdateYEdgeVisual(x, y);
        else RemoveYEdgeVisual(x, y);
    }

    private void UpdateXEdgeVisual(int x, int y)
    {
        if (sandbagPrefab == null || terrainTilemap == null) return;

        int leftTx = x - 2;
        int ty = y - 1;

        Vector3Int leftCell  = new Vector3Int(leftTx + _tileOrigin.x, ty + _tileOrigin.y, 0);
        Vector3Int rightCell = new Vector3Int(leftTx + 1 + _tileOrigin.x, ty + _tileOrigin.y, 0);

        Vector3 a = terrainTilemap.GetCellCenterWorld(leftCell);
        Vector3 b = terrainTilemap.GetCellCenterWorld(rightCell);

        Vector3 pos = (a + b) * 0.5f; pos.z = visualZ;
        var key = (x, y);

        if (!_xEdgeVisuals.TryGetValue(key, out var go) || go == null)
        {
            go = Instantiate(sandbagPrefab, pos, Quaternion.identity, sandbagParent);
            _xEdgeVisuals[key] = go;
        }
        else go.transform.position = pos;

        Vector3 dir = (b - a);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.localScale = Vector3.one;
    }

    private void UpdateYEdgeVisual(int x, int y)
    {
        if (sandbagPrefab == null || terrainTilemap == null) return;

        int tx = x - 1;
        int bottomTy = y - 2;

        Vector3Int bottomCell = new Vector3Int(tx + _tileOrigin.x, bottomTy + _tileOrigin.y, 0);
        Vector3Int topCell    = new Vector3Int(tx + _tileOrigin.x, bottomTy + 1 + _tileOrigin.y, 0);

        Vector3 a = terrainTilemap.GetCellCenterWorld(bottomCell);
        Vector3 b = terrainTilemap.GetCellCenterWorld(topCell);

        Vector3 pos = (a + b) * 0.5f; pos.z = visualZ;
        var key = (x, y);

        if (!_yEdgeVisuals.TryGetValue(key, out var go) || go == null)
        {
            go = Instantiate(sandbagPrefab, pos, Quaternion.identity, sandbagParent);
            _yEdgeVisuals[key] = go;
        }
        else go.transform.position = pos;

        Vector3 dir = (b - a);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.localScale = Vector3.one;
    }

    private void RemoveXEdgeVisual(int x, int y)
    {
        var key = (x, y);
        if (_xEdgeVisuals.TryGetValue(key, out var go) && go != null)
        {
            Destroy(go);
            _xEdgeVisuals.Remove(key);
        }
    }

    private void RemoveYEdgeVisual(int x, int y)
    {
        var key = (x, y);
        if (_yEdgeVisuals.TryGetValue(key, out var go) && go != null)
        {
            Destroy(go);
            _yEdgeVisuals.Remove(key);
        }
    }

    private bool IsClosedPreview(Vector2Int hover)
    {
        if (_pillars.Count < 3) return false;

        // "Can close" if hover is exactly the first pillar
        bool canClose = (hover.x != int.MinValue && hover == _pillars[0]) ||
                        (_pillars[_pillars.Count - 1] == _pillars[0]);

        if (!canClose) return false;

        // Must have at least 4 distinct points to plausibly enclose area
        var distinct = new HashSet<Vector2Int>(_pillars);
        if (distinct.Count < 4) return false;

        // Cheap non-collinear check: find 3 points that form non-zero area
        Vector2Int p0 = _pillars[0];
        for (int i = 1; i < _pillars.Count - 1; i++)
        {
            Vector2Int p1 = _pillars[i];
            Vector2Int p2 = _pillars[i + 1];

            int area2 = (p1.x - p0.x) * (p2.y - p0.y) - (p1.y - p0.y) * (p2.x - p0.x);
            if (area2 != 0) return true; // non-collinear => can enclose
        }

        return false;
    }

    private void SetBlockedX_NoVisual(int x, int y, bool blocked)
    {
        if (x < 0 || y < 0 || x >= _blockedX.GetLength(0) || y >= _blockedX.GetLength(1)) return;

        _blockedX[x, y] = blocked ? 1 : 0;
        _barrierHX[x, y] = blocked ? barrierHeight : 0f;
        _seepX[x, y] = blocked ? seepage : 0f;

        // NO prefab visuals here
    }

    private void SetBlockedY_NoVisual(int x, int y, bool blocked)
    {
        if (x < 0 || y < 0 || x >= _blockedY.GetLength(0) || y >= _blockedY.GetLength(1)) return;

        _blockedY[x, y] = blocked ? 1 : 0;
        _barrierHY[x, y] = blocked ? barrierHeight : 0f;
        _seepY[x, y] = blocked ? seepage : 0f;

        // NO prefab visuals here
    }

    private void DrawCommittedZoneOutline(string geoid, HashSet<Vector2Int> zoneTilesSim)
    {
        if (zoneOutlinePrefab == null || terrainTilemap == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] Missing zoneOutlinePrefab or terrainTilemap.");
            return;
        }

        // If we already drew this geoid, don’t duplicate
        if (_committedOutlines.ContainsKey(geoid))
            return;

        // Convert sim tiles -> tilemap cell coords
        var zoneCells = new HashSet<Vector3Int>();
        foreach (var t in zoneTilesSim)
        {
            var cell = new Vector3Int(t.x + _tileOrigin.x, t.y + _tileOrigin.y, 0);
            zoneCells.Add(cell);
        }

        // Build boundary edges + loops (same idea as ZoneOutlineByHover)
        var edges = ExtractBoundaryEdges(zoneCells);
        var loops = StitchEdgesIntoLoops(edges);

        var lines = new List<LineRenderer>();

        foreach (var loop in loops)
        {
            if (loop.Count < 3) continue;

            var lr = InstantiateCommittedLine();
            lr.positionCount = loop.Count;

            for (int i = 0; i < loop.Count; i++)
            {
                var gp = loop[i];
                Vector3 w = terrainTilemap.CellToWorld(new Vector3Int(gp.x, gp.y, 0));
                w.z += outlineZOffset;
                lr.SetPosition(i, w);
            }

            lr.loop = true;
            lines.Add(lr);
        }

        _committedOutlines[geoid] = lines;

        Debug.Log($"[FloodDefenseBoxStamp] Drew committed outline for geoid={geoid} loops={lines.Count}");
    }

    private LineRenderer InstantiateCommittedLine()
    {
        Transform parent = (zoneOutlineParent != null) ? zoneOutlineParent : this.transform;

        var lr = Instantiate(zoneOutlinePrefab, parent);
        lr.gameObject.SetActive(true);

        lr.useWorldSpace = true;
        lr.loop = true;

        // Visibility tuning
        lr.widthMultiplier = outlineWidth;
        lr.sortingOrder = outlineSortingOrder;

        return lr;
    }

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
        var edges = new List<Edge>();

        foreach (var c in zoneCells)
        {
            int x = c.x;
            int y = c.y;

            bool hasN = zoneCells.Contains(new Vector3Int(x, y + 1, 0));
            bool hasS = zoneCells.Contains(new Vector3Int(x, y - 1, 0));
            bool hasE = zoneCells.Contains(new Vector3Int(x + 1, y, 0));
            bool hasW = zoneCells.Contains(new Vector3Int(x - 1, y, 0));

            var bl = new Vector2Int(x, y);
            var br = new Vector2Int(x + 1, y);
            var tl = new Vector2Int(x, y + 1);
            var tr = new Vector2Int(x + 1, y + 1);

            if (!hasS) edges.Add(new Edge(bl, br));
            if (!hasN) edges.Add(new Edge(tl, tr));
            if (!hasW) edges.Add(new Edge(bl, tl));
            if (!hasE) edges.Add(new Edge(br, tr));
        }

        return edges;
    }

    private static List<List<Vector2Int>> StitchEdgesIntoLoops(List<Edge> edges)
    {
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
            if (used.Contains((e.a, e.b)) && used.Contains((e.b, e.a)))
                continue;

            var loop = new List<Vector2Int>();
            Vector2Int start = e.a;
            Vector2Int current = e.a;
            Vector2Int next = e.b;

            loop.Add(current);

            int safety = 0;
            while (safety++ < 100000)
            {
                used.Add((current, next));
                var prev = current;
                current = next;
                loop.Add(current);

                if (current == start)
                    break;

                if (!adj.TryGetValue(current, out var neighbors) || neighbors.Count == 0)
                    break;

                Vector2Int candidate = neighbors[0];

                if (neighbors.Count > 1)
                {
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

            if (loop.Count >= 4)
                loops.Add(loop);
        }

        return loops;
    }

    // ---------- IBarrierProvider ----------
    public bool IsBlockedX(int x, int y)
    {
        if (_blockedX == null) return false;
        if (x < 0 || y < 0 || x >= _blockedX.GetLength(0) || y >= _blockedX.GetLength(1)) return false;
        return _blockedX[x, y] > 0;
    }

    public bool IsBlockedY(int x, int y)
    {
        if (_blockedY == null) return false;
        if (x < 0 || y < 0 || x >= _blockedY.GetLength(0) || y >= _blockedY.GetLength(1)) return false;
        return _blockedY[x, y] > 0;
    }

    public float GetBarrierHeightX(int x, int y)
    {
        if (_barrierHX == null) return 0f;
        if (x < 0 || y < 0 || x >= _barrierHX.GetLength(0) || y >= _barrierHX.GetLength(1)) return 0f;
        return _barrierHX[x, y];
    }

    public float GetSeepageX(int x, int y)
    {
        if (_seepX == null) return 0f;
        if (x < 0 || y < 0 || x >= _seepX.GetLength(0) || y >= _seepX.GetLength(1)) return 0f;
        return _seepX[x, y];
    }

    public float GetBarrierHeightY(int x, int y)
    {
        if (_barrierHY == null) return 0f;
        if (x < 0 || y < 0 || x >= _barrierHY.GetLength(0) || y >= _barrierHY.GetLength(1)) return 0f;
        return _barrierHY[x, y];
    }

    public float GetSeepageY(int x, int y)
    {
        if (_seepY == null) return 0f;
        if (x < 0 || y < 0 || x >= _seepY.GetLength(0) || y >= _seepY.GetLength(1)) return 0f;
        return _seepY[x, y];
    }
}
