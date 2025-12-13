using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HoverTileTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;
    [SerializeField] private TileMapData tileMapData;

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Behavior")]
    [SerializeField] private float hoverDelay = 3f; // seconds to wait before showing tooltip

    Vector3Int _currentCell;
    bool _hasCell = false;
    float _hoverTimer = 0f;
    bool _tooltipVisible = false;

    void Awake()
    {
        if (!sceneCamera) sceneCamera = Camera.main;
        
        if (tooltipPanel != null)
            tooltipPanel.pivot = new Vector2(0.5f, 0f);  // bottom middle

        HideTooltip();
    }

    void Update()
    {
        if (sceneCamera == null || jsonMapLoader == null || canvas == null || tooltipPanel == null)
            return;

        // Convert mouse to world
        Vector3 mouseScreen = Input.mousePosition;

        // For an orthographic camera looking at Z=0:
        Vector3 world = sceneCamera.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, -sceneCamera.transform.position.z)
        );

        // Compute tile cell from ground tilemap
        var groundTilemap = jsonMapLoader.groundTilemap;
        if (groundTilemap == null) return;

        Vector3Int cell = groundTilemap.WorldToCell(world);

        // If we moved to a different cell, reset
        if (!_hasCell || cell != _currentCell)
        {
            _hasCell = true;
            _currentCell = cell;
            _hoverTimer = 0f;
            if (_tooltipVisible) HideTooltip();
        }

        _hoverTimer += Time.deltaTime;

        if (_hoverTimer >= hoverDelay)
        {
            // Time to show/update tooltip
            UpdateTooltipForCell(cell, mouseScreen);
        }
    }

    void UpdateTooltipForCell(Vector3Int cell, Vector3 mouseScreen)
    {
        // Use your existing helper to get JSON-based info
        int r, c, pop;
        string category, geoid;

        if (!jsonMapLoader.TryGetTileInfoAtWorld(
                sceneCamera.ScreenToWorldPoint(
                    new Vector3(mouseScreen.x, mouseScreen.y, -sceneCamera.transform.position.z)
                ),
                out r, out c, out category, out geoid, out pop))
        {
            // No tile info here
            HideTooltip();
            return;
        }

        // Elevation from TileMapData / TileInstance
        int elevation = 0;
        if (tileMapData != null)
        {
            // TileMapData stores tiles keyed by cell.x, cell.y
            var ti = tileMapData.Get(new Vector2Int(cell.x, cell.y));
            if (ti != null)
                elevation = ti.elevation;
        }

        // Build text
        string catLabel = string.IsNullOrEmpty(category) ? "Unknown" : category;
        string zoneLabel = string.IsNullOrEmpty(geoid) ? "N/A" : geoid;

        tooltipText.text =
            $"Category: {catLabel}\n" +
            $"Population: {pop}\n" +
            $"Elevation: {elevation}\n" +
            $"Zone: {zoneLabel}";

        // Position panel near mouse
        PositionTooltip(mouseScreen);

        ShowTooltip();
    }

    void PositionTooltip(Vector3 mouseScreen)
    {
        RectTransform canvasRect = canvas.transform as RectTransform;

        // Convert screen point to anchored position in the canvas
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mouseScreen,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        // Offset: a bit above the cursor
        float verticalOffset = 20f;
        Vector2 targetPos = localPoint + new Vector2(0f, verticalOffset);

        // Clamp so the panel stays fully on-screen
        Vector2 panelSize = tooltipPanel.sizeDelta;
        Vector2 halfSize = panelSize * 0.5f;

        // Canvas rect extents
        float minX = -canvasRect.rect.width  * 0.5f + halfSize.x;
        float maxX =  canvasRect.rect.width  * 0.5f - halfSize.x;
        float minY = -canvasRect.rect.height * 0.5f + halfSize.y;
        float maxY =  canvasRect.rect.height * 0.5f - halfSize.y;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        tooltipPanel.anchoredPosition = targetPos;
    }


    void ShowTooltip()
    {
        if (!_tooltipVisible)
        {
            tooltipPanel.gameObject.SetActive(true);
            _tooltipVisible = true;
        }
    }

    void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.gameObject.SetActive(false);
        _tooltipVisible = false;
    }
}
