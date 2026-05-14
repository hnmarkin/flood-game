using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class ZoneFocusController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap highlightTilemap;     // overlay tilemap
    [SerializeField] private TileBase highlightTile;       // a simple tile (can be your DynamicTile or plain Tile)

    [Header("UI")]
    [SerializeField] private GameObject dimBackground;     // fullscreen UI panel (Image)
    [SerializeField] private HoverTileTooltip tooltip;     // optional: disable tooltip while focused

    [Header("Camera Focus")]
    [SerializeField] private float zoomPadding = 1.2f;
    [SerializeField] private float zoomLerpSpeed = 8f;

    private Dictionary<string, List<Vector3Int>> geoidToCells = new();
    private bool builtIndex = false;

    private bool isFocused = false;
    private string focusedGeoid = null;

    private Vector3 camStartPos;
    private float camStartOrtho;

    private Vector3 targetCamPos;
    private float targetOrtho;

    void Awake()
    {
        if (!sceneCamera) sceneCamera = Camera.main;
        if (dimBackground) dimBackground.SetActive(false);
    }

    void Start()
    {
        // Save camera start state
        camStartPos = sceneCamera.transform.position;
        camStartOrtho = sceneCamera.orthographicSize;

        // In case JsonMapLoader paints in Awake (it does), we can build index in Start
        BuildGeoIndex();
    }

    void Update()
    {
        if (!builtIndex || jsonMapLoader == null || sceneCamera == null) return;

        // Smooth camera motion if focusing
        if (isFocused)
        {
            sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, targetCamPos, Time.deltaTime * zoomLerpSpeed);
            sceneCamera.orthographicSize = Mathf.Lerp(sceneCamera.orthographicSize, targetOrtho, Time.deltaTime * zoomLerpSpeed);
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector3 mouse = Input.mousePosition;
        Vector3 world = sceneCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -sceneCamera.transform.position.z));

        // Determine GeoID under cursor using your helper
        int r, c, pop;
        string category, geoid;

        if (!jsonMapLoader.TryGetTileInfoAtWorld(world, out r, out c, out category, out geoid, out pop))
        {
            // Clicked off-map => exit focus if active
            if (isFocused) ExitFocus();
            return;
        }

        geoid = string.IsNullOrEmpty(geoid) ? null : geoid;

        if (!isFocused)
        {
            if (geoid == null) return; // no zone
            EnterFocus(geoid);
        }
        else
        {
            // If clicking same zone, stay; otherwise exit (or switch)
            if (geoid == null || geoid != focusedGeoid) ExitFocus();
        }
    }

    private void EnterFocus(string geoid)
    {
        if (!geoidToCells.TryGetValue(geoid, out var cells) || cells.Count == 0) return;

        isFocused = true;
        focusedGeoid = geoid;

        if (tooltip) tooltip.enabled = true;
        if (dimBackground) dimBackground.SetActive(true);

        PaintHighlight(cells);
        FocusCameraToCells(cells);
    }

    private void ExitFocus()
    {
        isFocused = false;
        focusedGeoid = null;

        if (tooltip) tooltip.enabled = true;
        if (dimBackground) dimBackground.SetActive(false);

        if (highlightTilemap) highlightTilemap.ClearAllTiles();

        // Return camera
        targetCamPos = camStartPos;
        targetOrtho = camStartOrtho;

        // Smooth return
        sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, targetCamPos, 1f);
        sceneCamera.orthographicSize = targetOrtho;
    }

    private void PaintHighlight(List<Vector3Int> cells)
    {
        if (highlightTilemap == null || highlightTile == null) return;
        highlightTilemap.ClearAllTiles();

        foreach (var cell in cells)
            highlightTilemap.SetTile(cell, highlightTile);

        highlightTilemap.RefreshAllTiles();
    }

    private void FocusCameraToCells(List<Vector3Int> cells)
    {
        // Compute bounds in cell space
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var c in cells)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }

        // Convert to world bounds using groundTilemap’s cell centers
        var tm = jsonMapLoader.groundTilemap;
        Vector3 worldMin = tm.GetCellCenterWorld(new Vector3Int(minX, minY, 0));
        Vector3 worldMax = tm.GetCellCenterWorld(new Vector3Int(maxX, maxY, 0));
        Vector3 center = (worldMin + worldMax) * 0.5f;

        // Camera target position
        targetCamPos = new Vector3(center.x, center.y, sceneCamera.transform.position.z);

        // Estimate orthographic size needed
        float width = Mathf.Abs(worldMax.x - worldMin.x);
        float height = Mathf.Abs(worldMax.y - worldMin.y);

        float aspect = (float)Screen.width / Screen.height;
        float sizeForWidth = (width * 0.5f) / aspect;
        float sizeForHeight = height * 0.5f;

        targetOrtho = Mathf.Max(sizeForWidth, sizeForHeight) * zoomPadding;
        targetOrtho = Mathf.Max(1f, targetOrtho);
    }

    public void BuildGeoIndex()
    {
        geoidToCells.Clear();

        if (jsonMapLoader == null || jsonMapLoader.payload == null)
        {
            // payload may not be set yet; we can retry later if needed
            builtIndex = false;
            return;
        }

        // We already have cellToRC which maps cell -> (r,c). Great.
        foreach (var kvp in jsonMapLoader.cellToRC)
        {
            Vector3Int cell = kvp.Key;
            var rc = kvp.Value;

            int r = rc.x;
            int c = rc.y;

            string g = jsonMapLoader.geoidGrid[r, c];
            if (string.IsNullOrEmpty(g)) continue;

            if (!geoidToCells.TryGetValue(g, out var list))
            {
                list = new List<Vector3Int>();
                geoidToCells[g] = list;
            }

            list.Add(cell);
        }

        builtIndex = true;
        Debug.Log($"[ZoneFocusController] Indexed {geoidToCells.Count} GeoID zones.");
    }
}
