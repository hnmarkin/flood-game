/*
Required Inspector references:
- TileMapData: map tile/category data used to choose valid communication tower tiles.
- Communication Tilemap: the Tilemap used to convert tower cells to world-space centers. Falls back to ZoneController.TerrainTilemap.
- ZoneController: supplies zone data, coordinate conversion and associated-zone outlines; otherwise resolved once in Awake.
- ZoneThinOutlineByHover: retained only for legacy Inspector serialization.
- ShelterManager: optional; avoids active placed shelter tiles. Old System now... not in use
- ShelterCandidateController: optional; avoids valid shelter candidate tiles when available. Old system not in use
- Main Camera: optional; used for mouse-to-tile hover/click lookup. Falls back to Camera.main.
- Map Hover Tooltip: optional UI Toolkit tooltip controller for hovered communication tower metadata.
- Tooltip UI Document: optional HUD UIDocument host for the communication tower tooltip UXML.
- Main Tower Sprite: optional; sprite used for inactive communication towers.
- Zone Tower Sprite: optional; sprite used for activated communication towers. Falls back to Main Tower Sprite.
- Tower Visual Root: optional parent for spawned tower visuals. A runtime child is created if missing.

Current behavior:
- Calculates a simple tower candidate for grouped zone GEOIDs.
- Stores tower data and per-zone communication statuses.
- Shows/hides tower visuals when Communication mode is toggled.
- Lets the player activate inactive towers by clicking them while Communication mode is enabled.
- Highlights the hovered tower's associated zones and shows a UXML hover tooltip if assigned.
- Does not calculate route scores, evacuation data, warning cards, dispatch, or detailed communication UI.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public enum ZoneCommunicationStatus
{
    Unreached,
    Pending,
    Warned,
}

[Serializable]
public class CommunicationTowerData
{
    public string towerId;
    public Vector3Int tileCell;
    public Vector3 worldPosition;
    public List<string> associatedZoneGeoids = new();
    public bool isActive;
}

public class CommunicationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap communicationTilemap;
    [SerializeField] private ZoneController zoneController;
    // Retained for existing Inspector assignments; outlines use ZoneController.
    [SerializeField] private ShelterController shelterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MapHoverTooltipController mapHoverTooltip;
    [SerializeField] private UIDocument tooltipUIDocument;
    [SerializeField] private CommunicationPanelController communicationPanelController;

    [Header("Tower Visuals")]
    [SerializeField] private Sprite mainTowerSprite;
    [SerializeField] private Sprite zoneTowerSprite;
    [SerializeField] private Transform towerVisualRoot;
    [SerializeField] private Color inactiveTowerColor = new(0.62f, 0.86f, 1f, 0.85f);
    [SerializeField] private Color activeTowerColor = new(0.25f, 1f, 0.55f, 1f);
    [SerializeField] private Color hoveredTowerColor = new(1f, 0.95f, 0.35f, 1f);
    [SerializeField] private Color associatedZoneHoverColor = new(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private float towerVisualScale = 0.45f;
    [SerializeField] private float towerHeightOffset = 0.35f;
    [SerializeField] private string towerSortingLayerName = "Default";
    [SerializeField] private int towerSortingOrder = 2300;

    [Header("Tower Settings")]
    [SerializeField] private int zonesPerTower = 3;
    [SerializeField] private int towerSearchRadius = 4;
    [SerializeField] private bool showInactiveTowersAtStart = true;
    [SerializeField] private bool avoidShelterTiles = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly CommunicationState _state = new();
    private readonly CommunicationTowerVisuals _visuals = new();
    private readonly CommunicationInputController _input = new();
    private readonly CommunicationPresentation _presentation = new();

    private bool _loggedActionButtonConnected;
    private Coroutine _generateWhenReadyRoutine;
    private MapHoverTooltipController _tooltipBindingsTarget;
    private CommunicationPresentation.TooltipBindings _tooltipBindings;

    public bool IsCommunicationModeActive => _state.IsCommunicationModeActive;
    public IReadOnlyList<CommunicationTowerData> CommunicationTowers => _state.Towers;

    public event Action<bool> CommunicationModeChanged;
    public event Action<CommunicationTowerData> OnTowerActivated;
    public event Action<string> OnZoneWarned;

    private void Awake()
    {
        ResolveReferences();
        ResolveZoneController();
    }

    private void Start()
    {
        _generateWhenReadyRoutine = StartCoroutine(GenerateWhenReadyRoutine());
    }

    private void Update()
    {
        if (!_state.IsCommunicationModeActive)
            return;

        UpdateHoveredTower();
        HandleTowerClick();
    }

    private void OnDisable()
    {
        ClearHoveredTower();

        if (_generateWhenReadyRoutine != null)
        {
            StopCoroutine(_generateWhenReadyRoutine);
            _generateWhenReadyRoutine = null;
        }
    }

    private void OnValidate()
    {
        zonesPerTower = Mathf.Max(1, zonesPerTower);
        towerSearchRadius = Mathf.Max(1, towerSearchRadius);
        towerVisualScale = Mathf.Max(0.01f, towerVisualScale);
    }

    public void ToggleCommunicationMode()
    {
        SetCommunicationMode(!_state.IsCommunicationModeActive);
    }

    public void SetCommunicationMode(bool active)
    {
        if (active)
        {
            GenerateCommunicationTowers();
            ShowCommunicationTowers();
            _state.IsCommunicationModeActive = true;

            if (debugLogs)
                Debug.Log("[CommunicationTowerController] Communication mode enabled.");
        }
        else
        {
            _state.IsCommunicationModeActive = false;
            ClearHoveredTower();
            SetTowerVisualsVisible(showInactiveTowersAtStart, keepActiveTowersVisible: true);

            if (debugLogs)
                Debug.Log("[CommunicationTowerController] Communication mode disabled.");
        }

        CommunicationModeChanged?.Invoke(_state.IsCommunicationModeActive);
    }

    public void ShowCommunicationTowers()
    {
        GenerateCommunicationTowers();
        SetTowerVisualsVisible(true);
    }

    public void HideCommunicationTowers()
    {
        ClearHoveredTower();
        SetTowerVisualsVisible(false, keepActiveTowersVisible: true);
    }

    public bool TryGetCommunicationStatus(string geoid, out ZoneCommunicationStatus status)
    {
        return _state.TryGetCommunicationStatus(geoid, out status);
    }

    public IReadOnlyList<CommunicationTowerData> GetAllTowers()
    {
        return _state.Towers;
    }

    public IReadOnlyList<CommunicationTowerData> GetActiveTowers()
    {
        return _state.GetActiveTowers();
    }

    public ZoneCommunicationStatus GetZoneCommunicationStatus(string geoid)
    {
        return _state.GetZoneCommunicationStatus(geoid);
    }

    public Dictionary<string, ZoneCommunicationStatus> GetAllZoneCommunicationStatuses()
    {
        return _state.GetAllZoneCommunicationStatuses();
    }

    public void MarkZoneWarned(string geoid)
    {
        if (!_state.TryMarkZoneWarned(geoid, out string normalizedGeoid, out ZoneCommunicationStatus previousStatus))
            return;

        if (debugLogs)
        {
            Debug.Log(
                $"[CommunicationTowerController] Zone {normalizedGeoid} communication status changed: " +
                $"{previousStatus} -> Warned.");
        }

        OnZoneWarned?.Invoke(normalizedGeoid);
    }

    public void NotifyActionButtonConnected()
    {
        if (_loggedActionButtonConnected)
            return;

        _loggedActionButtonConnected = true;
        Debug.Log("[CommunicationTowerController] action_Button6 connected to Communication mode toggle.");
    }

    [ContextMenu("Regenerate Communication Towers")]
    public void RegenerateCommunicationTowers()
    {
        _state.HasGeneratedTowers = false;
        GenerateCommunicationTowers();
        SetTowerVisualsVisible(_state.IsCommunicationModeActive || showInactiveTowersAtStart, keepActiveTowersVisible: true);
    }

    private IEnumerator GenerateWhenReadyRoutine()
    {
        const int maxFramesToWait = 120;
        int waitedFrames = 0;

        while (!_state.HasGeneratedTowers && waitedFrames < maxFramesToWait)
        {
            GenerateCommunicationTowers(logWarnings: false);

            if (_state.HasGeneratedTowers)
                break;

            waitedFrames++;
            yield return null;
        }

        if (!_state.HasGeneratedTowers)
            GenerateCommunicationTowers();

        SetTowerVisualsVisible(_state.IsCommunicationModeActive || showInactiveTowersAtStart, keepActiveTowersVisible: true);
        _generateWhenReadyRoutine = null;
    }

    private void GenerateCommunicationTowers(bool logWarnings = true)
    {
        if (_state.HasGeneratedTowers)
            return;

        ResolveReferences();
        ClearTowerVisuals();
        _state.ClearTowers();

        if (!ValidateGenerationReferences(logWarnings))
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationTowerController] Communication towers generated: 0.");

            return;
        }

        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles = zoneController.GetAllZoneTiles();

        if (allZoneTiles == null || allZoneTiles.Count == 0)
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationTowerController] Communication towers generated: 0.");

            return;
        }

        if (debugLogs)
            Debug.Log($"[CommunicationTowerController] Zones per tower: {zonesPerTower}.");

        List<string> geoids = CommunicationTowerGenerator.GetOrderedGeoids(allZoneTiles);
        _state.InitializeZoneStatuses(geoids);
        CommunicationTowerGenerator generator = new(
            tileMapData, communicationTilemap, zoneController, shelterController,
            zonesPerTower, towerSearchRadius, avoidShelterTiles);

        foreach (CommunicationTowerData tower in generator.GenerateTowers(geoids, allZoneTiles))
        {
            _state.AddTower(tower);
            SpawnTowerVisual(tower);

            if (debugLogs)
                Debug.Log($"[CommunicationTowerController] Tower {tower.towerId} serves zones: {string.Join(", ", tower.associatedZoneGeoids)}.");
        }

        _state.HasGeneratedTowers = true;

        if (debugLogs)
            Debug.Log($"[CommunicationTowerController] Communication towers generated: {_state.Towers.Count}.");
    }

    private void ResolveReferences()
    {
        if (tileMapData == null)
            tileMapData = FindTileMapDataAsset();

        if (shelterController == null)
            shelterController = FindFirstObjectByType<ShelterController>();


        if (communicationTilemap == null && zoneController != null)
            communicationTilemap = zoneController.TerrainTilemap;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mapHoverTooltip == null)
            mapHoverTooltip = FindFirstObjectByType<MapHoverTooltipController>();

        if (tooltipUIDocument == null)
            tooltipUIDocument = ResolveTooltipUIDocument();

        if (communicationPanelController == null)
            communicationPanelController = FindFirstObjectByType<CommunicationPanelController>();

        if (mapHoverTooltip == null)
        {
            GameObject tooltipObject = new("MapHoverTooltipController");
            tooltipObject.transform.SetParent(transform, false);
            mapHoverTooltip = tooltipObject.AddComponent<MapHoverTooltipController>();
        }

        EnsureTooltipConfigured();

        if (towerVisualRoot == null)
        {
            GameObject root = new("CommunicationTowerVisuals");
            root.transform.SetParent(transform, false);
            towerVisualRoot = root.transform;
        }
    }

    private void ResolveZoneController()
    {
        if (zoneController != null)
            return;

        if (zoneController == null)
            zoneController = GetComponent<ZoneController>();

        if (zoneController != null)
            return;

        // Resolve once during initialization; do not bind to an additive scene's map.
        foreach (ZoneController candidate in FindObjectsByType<ZoneController>(FindObjectsSortMode.None))
        {
            if (candidate.gameObject.scene != gameObject.scene || !candidate.isActiveAndEnabled)
                continue;

            if (zoneController != null)
            {
                zoneController = null;
                Debug.LogWarning("[CommunicationTowerController] Multiple active ZoneControllers found in this scene. Assign ZoneController explicitly; tower generation and communication outlines require it.", this);
                return;
            }

            zoneController = candidate;
        }

        if (zoneController == null)
            Debug.LogWarning("[CommunicationTowerController] ZoneController could not be resolved during initialization. Tower generation and communication outlines require it.", this);
    }

    private void EnsureTooltipConfigured()
    {
        _presentation.EnsureTooltipConfigured(GetTooltipBindings(), ref tooltipUIDocument, debugLogs);
    }

    private UIDocument ResolveTooltipUIDocument()
    {
        return _presentation.ResolveTooltipUIDocument();
    }

    private bool ValidateGenerationReferences(bool logWarnings = true)
    {
        bool isValid = true;

        if (tileMapData == null)
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationTowerController] TileMapData is not assigned.");

            isValid = false;
        }

        if (zoneController == null)
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationTowerController] ZoneController is not assigned.");

            isValid = false;
        }

        if (communicationTilemap == null)
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationTowerController] Communication Tilemap is not assigned.");

            isValid = false;
        }

        return isValid;
    }

    private void SpawnTowerVisual(CommunicationTowerData tower)
    {
        if (tower != null && !string.IsNullOrEmpty(tower.towerId) && towerVisualRoot == null)
            ResolveReferences();

        _visuals.SpawnTowerVisual(tower, towerVisualRoot, GetVisualSettings());
    }

    private void UpdateTowerVisual(CommunicationTowerData tower)
    {
        _visuals.UpdateTowerVisual(tower, _presentation.HoveredTower, GetVisualSettings());
    }

    private void ClearTowerVisuals()
    {
        _visuals.ClearTowerVisuals();
    }

    private void SetTowerVisualsVisible(bool visible, bool keepActiveTowersVisible = false)
    {
        _visuals.SetTowerVisualsVisible(_state.Towers, visible, keepActiveTowersVisible);
    }

    private void UpdateHoveredTower()
    {
        CommunicationInputController.PointerSample pointer = _input.SamplePointer(mainCamera, communicationTilemap, _state);
        _presentation.UpdateHoveredTower(
            pointer.tower, pointer.screenPosition, _visuals, GetVisualSettings(),
            GetPresentationContext(), ref tooltipUIDocument);
    }

    private void HandleTowerClick()
    {
        if (_input.IsTowerClickRequested(_presentation.HoveredTower))
            ActivateTower(_presentation.HoveredTower);
    }

    private void ActivateTower(CommunicationTowerData tower)
    {
        if (tower == null)
            return;

        if (!_state.TryActivateTower(tower))
        {
            if (debugLogs)
                Debug.Log($"[CommunicationTowerController] Tower {tower.towerId} is already active.");

            OpenCommunicationPanel(tower);
            return;
        }

        UpdateTowerVisual(tower);

        if (debugLogs)
            Debug.Log($"[CommunicationTowerController] Tower {tower.towerId} activated.");

        foreach (string geoid in _state.SetTowerZonesPending(tower))
        {
            if (debugLogs)
            {
                Debug.Log(
                    $"[CommunicationTowerController] Zone {geoid} communication status changed: " +
                    "Unreached -> Pending.");
            }
        }

        OnTowerActivated?.Invoke(tower);
        OpenCommunicationPanel(tower);
    }

    private void OpenCommunicationPanel(CommunicationTowerData tower)
    {
        if (communicationPanelController == null)
            communicationPanelController = FindFirstObjectByType<CommunicationPanelController>();

        _presentation.OpenCommunicationPanel(
            tower, communicationPanelController != null ? communicationPanelController.ShowForTower : null);
    }

    private void ClearHoveredTower()
    {
        _presentation.ClearHoveredTower(_visuals, GetVisualSettings(), GetPresentationContext());
    }

    private CommunicationTowerVisuals.Settings GetVisualSettings()
    {
        return new CommunicationTowerVisuals.Settings(
            mainTowerSprite, zoneTowerSprite, inactiveTowerColor, activeTowerColor, hoveredTowerColor,
            towerVisualScale, towerHeightOffset, towerSortingLayerName, towerSortingOrder);
    }

    private CommunicationPresentation.Context GetPresentationContext()
    {
        return new CommunicationPresentation.Context(
            zoneController, associatedZoneHoverColor, GetTooltipBindings(), debugLogs, this);
    }

    private CommunicationPresentation.TooltipBindings GetTooltipBindings()
    {
        if (mapHoverTooltip == null)
            return default;

        if (!ReferenceEquals(_tooltipBindingsTarget, mapHoverTooltip))
        {
            // Cache method bindings only, never document/settings snapshots.
            // The presentation helper borrows these ports for each operation.
            MapHoverTooltipController tooltip = mapHoverTooltip;
            _tooltipBindingsTarget = tooltip;
            _tooltipBindings = new CommunicationPresentation.TooltipBindings(
                tooltip.SetTooltipUIDocument,
                () => tooltip.SetCommunicationTowerController(this),
                tooltip.SetDebugLogs,
                tooltip.ShowCommunicationTowerTooltip,
                tooltip.HideCommunicationTowerTooltip,
                () => tooltip.IsCommunicationTowerTooltipVisible);
        }

        return _tooltipBindings;
    }

    private TileMapData FindTileMapDataAsset()
    {
        TileMapData[] tileMaps = Resources.FindObjectsOfTypeAll<TileMapData>();

        for (int i = 0; i < tileMaps.Length; i++)
        {
            TileMapData candidate = tileMaps[i];

            if (candidate != null && (candidate.N > 0 || candidate.sizeX > 0))
                return candidate;
        }

        return null;
    }
}
