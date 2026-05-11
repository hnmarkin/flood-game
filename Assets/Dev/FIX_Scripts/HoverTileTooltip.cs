using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class HoverTileTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;
    [SerializeField] private TileMapData tileMapData;

    [Header("UI Toolkit Document")]
    [SerializeField] private UIDocument zoneInfoDocument;

    [Header("UI Element Names")]
    [SerializeField] private string panelName = "zone_info_panel";
    [SerializeField] private string categoryLabelName = "category_label";
    [SerializeField] private string populationLabelName = "population_label";
    [SerializeField] private string elevationLabelName = "elevation_label";
    [SerializeField] private string zoneLabelName = "zone_label";

    [Header("Behavior")]
    [SerializeField] private bool zoneInfoMode = false;
    [SerializeField] private float hoverDelay = 3f;
    [SerializeField] private float stationaryPixelThreshold = 3f;

    [Header("Panel Position")]
    [SerializeField] private bool movePanelNearCursor = true;
    [SerializeField] private Vector2 panelOffset = new Vector2(18f, 18f);

    private VisualElement _root;
    private VisualElement _panel;

    private Label _categoryLabel;
    private Label _populationLabel;
    private Label _elevationLabel;
    private Label _zoneLabel;

    private Vector2 _lastMouseScreenPosition;
    private Vector3Int _currentCell;
    private bool _hasCell = false;

    private float _hoverTimer = 0f;
    private bool _tooltipVisible = false;

    private Vector2Int _tileOrigin = Vector2Int.zero;

    private void Awake()
    {
        if (sceneCamera == null)
            sceneCamera = Camera.main;

        zoneInfoMode = false;

        BindUI();
        UpdateTileOrigin();
        HideTooltip();

        Debug.Log("[HoverTileTooltip] Zone info mode forced OFF at startup.");
    }

    private void OnEnable()
    {
        BindUI();
        HideTooltip();
    }

    private void Update()
    {
        if (!zoneInfoMode)
            return;

        HandleHover();
    }

    public void ToggleZoneInfoFromUI()
    {
        if (zoneInfoMode)
        {
            DisableZoneInfoFromUI();
        }
        else
        {
            EnableZoneInfoFromUI();
        }
    }

    public void EnableZoneInfoFromUI()
    {
        zoneInfoMode = true;

        _lastMouseScreenPosition = Input.mousePosition;
        _hoverTimer = 0f;
        _hasCell = false;

        HideTooltip();

        Debug.Log("[HoverTileTooltip] Zone info mode ON. Keep cursor still over a zone for 3 seconds.");
    }

    public void DisableZoneInfoFromUI()
    {
        zoneInfoMode = false;

        _hoverTimer = 0f;
        _hasCell = false;

        HideTooltip();
        GlobalHUDController.Instance?.ClearHoveredZoneInfo();

        Debug.Log("[HoverTileTooltip] Zone info mode OFF.");
    }

    private void BindUI()
    {
        if (zoneInfoDocument == null)
        {
            Debug.LogWarning("[HoverTileTooltip] zoneInfoDocument is not assigned.");
            return;
        }

        _root = zoneInfoDocument.rootVisualElement;

        if (_root == null)
        {
            Debug.LogWarning("[HoverTileTooltip] UIDocument rootVisualElement is null.");
            return;
        }

        _panel = _root.Q<VisualElement>(panelName);

        _categoryLabel = _root.Q<Label>(categoryLabelName);
        _populationLabel = _root.Q<Label>(populationLabelName);
        _elevationLabel = _root.Q<Label>(elevationLabelName);
        _zoneLabel = _root.Q<Label>(zoneLabelName);

        if (_panel == null)
            Debug.LogWarning($"[HoverTileTooltip] Could not find panel named '{panelName}'.");

        if (_categoryLabel == null)
            Debug.LogWarning($"[HoverTileTooltip] Could not find label named '{categoryLabelName}'.");

        if (_populationLabel == null)
            Debug.LogWarning($"[HoverTileTooltip] Could not find label named '{populationLabelName}'.");

        if (_elevationLabel == null)
            Debug.LogWarning($"[HoverTileTooltip] Could not find label named '{elevationLabelName}'.");

        if (_zoneLabel == null)
            Debug.LogWarning($"[HoverTileTooltip] Could not find label named '{zoneLabelName}'.");

        if (_panel != null)
        {
            _panel.style.display = DisplayStyle.None;

            if (movePanelNearCursor)
                _panel.style.position = Position.Absolute;
        }
    }

    private void HandleHover()
    {
        if (!ValidateReferences())
            return;

        Tilemap groundTilemap = jsonMapLoader.groundTilemap;

        if (groundTilemap == null)
            return;

        Vector2 mouseScreen = Input.mousePosition;
        float movement = Vector2.Distance(mouseScreen, _lastMouseScreenPosition);

        if (movement > stationaryPixelThreshold)
        {
            _lastMouseScreenPosition = mouseScreen;
            ResetHover();
            return;
        }

        Vector3 world = ScreenToWorld(mouseScreen);
        Vector3Int cell = groundTilemap.WorldToCell(world);

        if (!_hasCell || cell != _currentCell)
        {
            _hasCell = true;
            _currentCell = cell;
            ResetHover();
            return;
        }

        if (!TryGetZoneInfo(world, cell, out string category, out int population, out int elevation, out string zone))
        {
            GlobalHUDController.Instance?.ClearHoveredZoneInfo();
            ResetHover();
            return;
        }

        GlobalHUDController.Instance?.SetHoveredZoneInfo(zone, population);

        _hoverTimer += Time.deltaTime;

        if (_hoverTimer >= hoverDelay)
        {
            UpdateTooltipText(category, population, elevation, zone);
            PositionTooltip(mouseScreen);
            ShowTooltip();
        }
    }

    private bool TryGetZoneInfo(
        Vector3 world,
        Vector3Int cell,
        out string category,
        out int population,
        out int elevation,
        out string zone
    )
    {
        category = "Unknown";
        population = 0;
        elevation = 0;
        zone = "N/A";

        int r;
        int c;
        int pop;
        string foundCategory;
        string geoid;

        bool hit = jsonMapLoader.TryGetTileInfoAtWorld(
            world,
            out r,
            out c,
            out foundCategory,
            out geoid,
            out pop
        );

        if (!hit || string.IsNullOrEmpty(geoid))
            return false;

        category = string.IsNullOrEmpty(foundCategory) ? "Unknown" : foundCategory;
        population = pop;
        zone = geoid;
        elevation = GetElevationFromCell(cell);

        return true;
    }

    private int GetElevationFromCell(Vector3Int cell)
    {
        if (tileMapData == null || jsonMapLoader == null || jsonMapLoader.groundTilemap == null)
            return 0;

        UpdateTileOrigin();

        int tx = cell.x - _tileOrigin.x;
        int ty = cell.y - _tileOrigin.y;

        if (tx < 0 || ty < 0 || tx >= tileMapData.N || ty >= tileMapData.N)
            return 0;

        TileInstance tile = tileMapData.Get(new Vector2Int(tx, ty));

        if (tile == null)
            return 0;

        return tile.elevation;
    }

    private void UpdateTileOrigin()
    {
        if (jsonMapLoader == null || jsonMapLoader.groundTilemap == null)
            return;

        Tilemap groundTilemap = jsonMapLoader.groundTilemap;

        groundTilemap.CompressBounds();
        BoundsInt bounds = groundTilemap.cellBounds;

        _tileOrigin = new Vector2Int(bounds.xMin, bounds.yMin);
    }

    private Vector3 ScreenToWorld(Vector2 mouseScreen)
    {
        float zDistance = -sceneCamera.transform.position.z;

        Vector3 world = sceneCamera.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, zDistance)
        );

        world.z = 0f;
        return world;
    }

    private void UpdateTooltipText(string category, int population, int elevation, string zone)
    {
        if (_categoryLabel != null)
            _categoryLabel.text = $"Category: {category}";

        if (_populationLabel != null)
            _populationLabel.text = $"Population: {population:N0}";

        if (_elevationLabel != null)
            _elevationLabel.text = $"Elevation: {elevation}";

        if (_zoneLabel != null)
            _zoneLabel.text = $"Zone: {zone}";
    }

    private void PositionTooltip(Vector2 mouseScreen)
    {
        if (!movePanelNearCursor || _panel == null || _root == null)
            return;

        Vector2 panelPosition;

        // Correct way for UI Toolkit runtime panels.
        // Converts Input.mousePosition screen pixels into UI Toolkit panel coordinates.
        if (_root.panel != null)
        {
            panelPosition = RuntimePanelUtils.ScreenToPanel(_root.panel, mouseScreen);
        }
        else
        {
            // Fallback, in case the panel is not ready yet.
            panelPosition = new Vector2(mouseScreen.x, Screen.height - mouseScreen.y);
        }

        // style.left/top are relative to the panel's parent, not always the root.
        VisualElement parent = _panel.parent != null ? _panel.parent : _root;
        Vector2 localPosition = parent.WorldToLocal(panelPosition);

        float panelWidth = _panel.resolvedStyle.width;
        float panelHeight = _panel.resolvedStyle.height;

        if (panelWidth <= 0f)
            panelWidth = 180f;

        if (panelHeight <= 0f)
            panelHeight = 100f;

        float parentWidth = parent.resolvedStyle.width;
        float parentHeight = parent.resolvedStyle.height;

        float x = localPosition.x + panelOffset.x;
        float y = localPosition.y + panelOffset.y;

        // Keep the panel inside its parent.
        x = Mathf.Clamp(x, 0f, Mathf.Max(0f, parentWidth - panelWidth));
        y = Mathf.Clamp(y, 0f, Mathf.Max(0f, parentHeight - panelHeight));

        _panel.style.left = x;
        _panel.style.top = y;
    }

    private void ShowTooltip()
    {
        if (_panel == null)
            return;

        if (!_tooltipVisible)
        {
            _panel.style.display = DisplayStyle.Flex;
            _tooltipVisible = true;
        }
    }

    private void HideTooltip()
    {
        if (_panel != null)
            _panel.style.display = DisplayStyle.None;

        _tooltipVisible = false;
    }

    private void ResetHover()
    {
        _hoverTimer = 0f;
        HideTooltip();
    }

    private bool ValidateReferences()
    {
        if (sceneCamera == null)
            return false;

        if (jsonMapLoader == null)
            return false;

        if (jsonMapLoader.groundTilemap == null)
            return false;

        if (zoneInfoDocument == null)
            return false;

        if (_panel == null)
            BindUI();

        return true;
    }
}