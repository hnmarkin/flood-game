using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class BarrierController : MonoBehaviour
{
    public event Action<bool> ZoneBoundaryModeChanged;
    public event Action<int, int> ZoneBarrierPlaced;
    public event Action AllZoneBarriersPlaced;

    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;
    [SerializeField] private ZoneController zoneController;
    [SerializeField] private WaterController waterController;
    [SerializeField] private NpcToastController npcToast;

    [Header("Barrier System Components")]
    [SerializeField] private BarrierPlacementInput placementInput;
    [SerializeField] private BarrierVisuals visuals;
    [SerializeField] private BarrierPhysicsAdapter physicsAdapter;

    [Header("Zone Boundary Placement")]
    [SerializeField] private bool zoneBoundaryMode;
    [SerializeField, Min(1)] private int maxZoneBarriers = 1;
    [SerializeField, Min(1)] private int requiredBarriersBeforeFlood = 3;

    [Header("Barrier Logic")]
    [SerializeField] private float barrierHeight = 40f;
    [SerializeField] private float seepage = 0.001f;

    [Header("Preview")]
    [SerializeField] private Tilemap previewTilemap;
    [SerializeField] private TileBase previewTile;
    [SerializeField] private Color previewColor = new Color(0.2f, 1f, 0.2f, 0.55f);

    [Header("Committed Zone Outline")]
    [SerializeField] private Tilemap zoneOutlineTilemap;
    [SerializeField] private TileBase zoneOutlineTile;
    [SerializeField] private Color zoneOutlineColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private bool drawCommittedOutline = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly BarrierPlacementState _placementState = new BarrierPlacementState();

    private string _hoverGeoid;
    private HashSet<Vector2Int> _hoverZoneTiles;
    private Coroutine _enterZoneModeCoroutine;

    public int BarriersPlaced => _placementState.CommittedZoneCount;

    public int ActualMaxZoneBarriers
    {
        get
        {
            int requestedLimit = Mathf.Max(1, maxZoneBarriers);
            int zoneCount = zoneController != null && zoneController.IsInitialized
                ? zoneController.ZoneCount
                : 0;

            if (zoneCount == 0)
                return requestedLimit;

            return Mathf.Clamp(requestedLimit, 1, zoneCount);
        }
    }

    public int RequiredBarriersBeforeFlood => Mathf.Clamp(
        requiredBarriersBeforeFlood > 0 ? requiredBarriersBeforeFlood : ActualMaxZoneBarriers,
        1,
        ActualMaxZoneBarriers);

    public bool IsZoneBoundaryModeActive => zoneBoundaryMode;
    public bool DebugLogsEnabled => debugLogs;

    private void Awake()
    {
        ResolveReferences();
        InitializePlacementState();
        ConfigureChildComponents();
        SetZoneBoundaryModeActive(false);
        ClearHoverAndPreview();
        UpdateGlobalHUDBarrierProgress();
    }

    private void Update()
    {
        if (!zoneBoundaryMode)
            return;

        ZoneBoundaryUpdate();
    }

    private void OnDisable()
    {
        ExitZoneBoundaryPlacementMode();
    }

    private void OnValidate()
    {
        maxZoneBarriers = Mathf.Max(1, maxZoneBarriers);
        requiredBarriersBeforeFlood = Mathf.Max(1, requiredBarriersBeforeFlood);
        barrierHeight = Mathf.Max(0f, barrierHeight);
        seepage = Mathf.Max(0f, seepage);
    }

    public void ConfigureReferencesFromLegacyAdapter(
        TileMapData mapData,
        Tilemap terrain,
        Camera camera,
        JsonMapLoader loader,
        ZoneController zones,
        WaterController water,
        NpcToastController toast)
    {
        tileMapData = mapData;
        terrainTilemap = terrain;
        mainCamera = camera != null ? camera : mainCamera;
        jsonMapLoader = loader;
        zoneController = zones != null ? zones : zoneController;
        waterController = water != null ? water : waterController;
        npcToast = toast != null ? toast : npcToast;

        ResolveReferences();
        InitializePlacementState();
        ConfigureChildComponents();
        UpdateGlobalHUDBarrierProgress();
    }

    public void ToggleBuildModeFromUI()
    {
        if (zoneBoundaryMode || _enterZoneModeCoroutine != null)
            DisablePlaceBarrierMode();
        else
            EnablePlaceBarrierMode();
    }

    public void EnterBuildModeFromUI()
    {
        EnablePlaceBarrierMode();
    }

    public void ExitBuildModeFromUI()
    {
        DisablePlaceBarrierMode();
    }

    public void EnablePlaceBarrierMode()
    {
        if (zoneBoundaryMode || _enterZoneModeCoroutine != null)
            return;

        EnterZoneBoundaryPlacementMode();
    }

    public void DisablePlaceBarrierMode()
    {
        ExitZoneBoundaryPlacementMode();
    }

    public void SetPlaceBarrierMode(bool enabled)
    {
        if (enabled)
            EnablePlaceBarrierMode();
        else
            DisablePlaceBarrierMode();
    }

    public void EnterZoneBoundaryPlacementMode()
    {
        if (_enterZoneModeCoroutine != null)
            StopCoroutine(_enterZoneModeCoroutine);

        _enterZoneModeCoroutine = StartCoroutine(EnterZoneBoundaryPlacementModeRoutine());
    }

    public void ExitZoneBoundaryPlacementMode()
    {
        if (_enterZoneModeCoroutine != null)
        {
            StopCoroutine(_enterZoneModeCoroutine);
            _enterZoneModeCoroutine = null;
        }

        SetZoneBoundaryModeActive(false);
        ClearHoverAndPreview();

        if (debugLogs)
            Debug.Log("[BarrierController] Zone boundary mode OFF.");
    }

    public bool IsBlockedX(int x, int y)
    {
        return _placementState.IsBlockedX(x, y);
    }

    public bool IsBlockedY(int x, int y)
    {
        return _placementState.IsBlockedY(x, y);
    }

    public float GetBarrierHeightX(int x, int y)
    {
        return _placementState.GetBarrierHeightX(x, y);
    }

    public float GetBarrierHeightY(int x, int y)
    {
        return _placementState.GetBarrierHeightY(x, y);
    }

    public float GetSeepageX(int x, int y)
    {
        return _placementState.GetSeepageX(x, y);
    }

    public float GetSeepageY(int x, int y)
    {
        return _placementState.GetSeepageY(x, y);
    }

    private IEnumerator EnterZoneBoundaryPlacementModeRoutine()
    {
        if (!ValidateRequiredReferences())
        {
            _enterZoneModeCoroutine = null;
            yield break;
        }

        SetZoneBoundaryModeActive(false);
        ClearHoverAndPreview();

        int attempts = 0;
        const int maxAttempts = 30;

        while (!IsZoneLoaderReady() && attempts < maxAttempts)
        {
            attempts++;
            yield return null;
        }

        BuildZoneIndex();
        UpdateGlobalHUDBarrierProgress();

        int zoneCount = zoneController != null ? zoneController.ZoneCount : 0;
        if (zoneCount == 0)
        {
            ShowMessage("Zone boundary mode could not start because no zones were found.");
            _enterZoneModeCoroutine = null;
            yield break;
        }

        if (BarriersPlaced >= ActualMaxZoneBarriers)
        {
            ShowMessage($"Barrier limit reached: {BarriersPlaced}/{ActualMaxZoneBarriers} zone barriers placed.");
            _enterZoneModeCoroutine = null;
            yield break;
        }

        SetZoneBoundaryModeActive(true);
        ClearHoverAndPreview();

        if (debugLogs)
        {
            Debug.Log($"[BarrierController] Zone boundary mode ON. Hover a zone and left-click to barricade. " +
                      $"Placed={BarriersPlaced}/{ActualMaxZoneBarriers}, Total zones={zoneCount}");
        }

        _enterZoneModeCoroutine = null;
    }

    private void ZoneBoundaryUpdate()
    {
        if (!ValidateRuntimeReferences())
            return;

        if (placementInput.EscapePressed)
        {
            ExitZoneBoundaryPlacementMode();
            return;
        }

        if (BarriersPlaced >= ActualMaxZoneBarriers)
        {
            ShowMessage($"Barrier limit reached: {BarriersPlaced}/{ActualMaxZoneBarriers} zone barriers placed.");
            ExitZoneBoundaryPlacementMode();
            return;
        }

        if (!placementInput.TryGetHoveredZone(zoneController, out string geoid, out HashSet<Vector2Int> zoneTiles))
        {
            ClearHoverAndPreview();
            return;
        }

        bool alreadyCommitted = _placementState.HasCommittedZone(geoid);
        if (_hoverGeoid != geoid || _hoverZoneTiles != zoneTiles)
        {
            _hoverGeoid = geoid;
            _hoverZoneTiles = zoneTiles;

            if (alreadyCommitted)
                visuals.ClearPreview();
            else
                visuals.DrawZoneBoundaryPreview(geoid, zoneController);
        }

        if (placementInput.CommitPressed)
            TryCommitZoneBoundaryBarrier(geoid, zoneTiles);
    }

    private void TryCommitZoneBoundaryBarrier(string geoid, HashSet<Vector2Int> zoneTiles)
    {
        if (string.IsNullOrEmpty(geoid))
            return;

        if (zoneTiles == null || zoneTiles.Count == 0)
            return;

        if (_placementState.HasCommittedZone(geoid))
        {
            ShowMessage("This zone already has a boundary barrier.");
            return;
        }

        if (BarriersPlaced >= ActualMaxZoneBarriers)
        {
            ShowMessage($"Barrier limit reached: {BarriersPlaced}/{ActualMaxZoneBarriers} zone barriers placed.");
            ExitZoneBoundaryPlacementMode();
            return;
        }

        if (!zoneController.TryGetBoundaryEdges(geoid, out IReadOnlyList<ZoneBoundaryEdge> boundaryEdges))
        {
            ShowMessage("Zone boundary could not be resolved.");
            return;
        }

        if (!_placementState.CanStoreBoundaryEdges(boundaryEdges, out string legacyError))
        {
            ShowMessage($"Barrier placement rejected: {legacyError}");
            return;
        }

        BarrierPhysicsRegistrationResult physicsResult = physicsAdapter.TryRegisterZoneBoundary(
            geoid,
            boundaryEdges,
            zoneController,
            barrierHeight,
            seepage);

        if (!physicsResult.Succeeded)
        {
            string message = $"Barrier placement rejected: {physicsResult.Message}";
            if (physicsResult.Status == BarrierPhysicsRegistrationStatus.PartialCommit)
            {
                message = $"Partial barrier commit error: {physicsResult.Message}";
                Debug.LogError($"[BarrierController] {message}");
            }
            else if (debugLogs)
            {
                Debug.LogWarning($"[BarrierController] {message}");
            }

            ShowMessage(message);
            return;
        }

        if (!_placementState.TryRecordCommittedZone(
                geoid,
                boundaryEdges,
                barrierHeight,
                seepage,
                out int blockedEdges,
                out string recordError))
        {
            string message = $"Barrier placement failed after water registration: {recordError}. WaterController has no rollback API.";
            Debug.LogError($"[BarrierController] {message}");
            ShowMessage(message);
            return;
        }

        UpdateGlobalHUDBarrierProgress();
        visuals.PaintCommittedZoneOutline(geoid, zoneController);

        int requiredBeforeFlood = RequiredBarriersBeforeFlood;
        ShowMessage($"Zone barrier placed: {BarriersPlaced}/{ActualMaxZoneBarriers}");

        if (debugLogs)
        {
            Debug.Log($"[BarrierController] Zone boundary committed. Zone={geoid}, Zone tiles={zoneTiles.Count}, " +
                      $"Blocked edges={blockedEdges}, Placed={BarriersPlaced}/{ActualMaxZoneBarriers}");
        }

        ZoneBarrierPlaced?.Invoke(BarriersPlaced, requiredBeforeFlood);

        ClearHoverAndPreview();

        if (BarriersPlaced >= ActualMaxZoneBarriers)
        {
            ShowMessage($"All available barriers have been placed: {BarriersPlaced}/{ActualMaxZoneBarriers}");
            AllZoneBarriersPlaced?.Invoke();
            ExitZoneBoundaryPlacementMode();
        }
    }

    private bool ValidateRequiredReferences()
    {
        bool ok = true;

        ResolveReferences();
        ConfigureChildComponents();

        if (tileMapData == null)
        {
            Debug.LogError("[BarrierController] tileMapData is not assigned.");
            ok = false;
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("[BarrierController] terrainTilemap is not assigned.");
            ok = false;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[BarrierController] mainCamera is not assigned.");
            ok = false;
        }

        if (jsonMapLoader == null)
        {
            Debug.LogError("[BarrierController] jsonMapLoader is not assigned.");
            ok = false;
        }

        if (zoneController == null)
        {
            Debug.LogError("[BarrierController] ZoneController is not assigned.");
            ok = false;
        }

        if (physicsAdapter == null)
        {
            Debug.LogError("[BarrierController] BarrierPhysicsAdapter is not assigned.");
            ok = false;
        }

        if (visuals == null || !visuals.HasPreviewSetup)
        {
            Debug.LogError("[BarrierController] Barrier preview Tilemap and Tile are required.");
            ok = false;
        }

        if (!_placementState.HasLegacyGrid && !InitializePlacementState())
        {
            Debug.LogError("[BarrierController] Barrier placement state could not initialize.");
            ok = false;
        }

        return ok;
    }

    private bool ValidateRuntimeReferences()
    {
        if (tileMapData == null ||
            terrainTilemap == null ||
            mainCamera == null ||
            jsonMapLoader == null ||
            zoneController == null ||
            placementInput == null ||
            visuals == null ||
            physicsAdapter == null)
        {
            return false;
        }

        if (!_placementState.HasLegacyGrid)
            InitializePlacementState();

        return _placementState.HasLegacyGrid;
    }

    private bool IsZoneLoaderReady()
    {
        return EnsureZoneControllerConfigured() && zoneController.IsZoneLoaderReady();
    }

    private void BuildZoneIndex()
    {
        if (!EnsureZoneControllerConfigured())
            return;

        zoneController.RebuildZoneIndex();

        if (debugLogs)
        {
            Debug.Log($"[BarrierController] BuildZoneIndex forwarded. Zones found={zoneController.ZoneCount}. " +
                      $"Requested limit={maxZoneBarriers}. Actual limit={ActualMaxZoneBarriers}.");
        }
    }

    private bool EnsureZoneControllerConfigured()
    {
        if (zoneController == null)
            zoneController = GetComponent<ZoneController>();

        if (zoneController == null)
            zoneController = FindFirstObjectByType<ZoneController>();

        if (zoneController == null)
        {
            zoneController = gameObject.AddComponent<ZoneController>();
            Debug.LogWarning("[BarrierController] ZoneController was missing and was added at runtime. Add it in the scene for serialized setup.");
        }

        if (zoneController == null)
            return false;

        zoneController.Configure(tileMapData, terrainTilemap, jsonMapLoader, debugLogs);
        return true;
    }

    private bool InitializePlacementState()
    {
        if (tileMapData == null)
            return false;

        bool initialized = _placementState.InitializeLegacyGrid(
            tileMapData.GridWidth,
            tileMapData.GridHeight);

        if (initialized && debugLogs)
        {
            Debug.Log($"[BarrierController] Initialized barrier placement state. Grid={tileMapData.GridWidth}x{tileMapData.GridHeight}.");
        }

        return initialized;
    }

    private void ResolveReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (zoneController == null)
            zoneController = GetComponent<ZoneController>();

        if (waterController == null)
            waterController = FindFirstObjectByType<WaterController>();

        if (npcToast == null)
            npcToast = NpcToastController.Instance != null
                ? NpcToastController.Instance
                : FindFirstObjectByType<NpcToastController>();

        if (placementInput == null)
            placementInput = GetComponent<BarrierPlacementInput>();

        if (placementInput == null)
            placementInput = gameObject.AddComponent<BarrierPlacementInput>();

        if (visuals == null)
            visuals = GetComponent<BarrierVisuals>();

        if (visuals == null)
            visuals = gameObject.AddComponent<BarrierVisuals>();

        if (physicsAdapter == null)
            physicsAdapter = GetComponent<BarrierPhysicsAdapter>();

        if (physicsAdapter == null)
            physicsAdapter = gameObject.AddComponent<BarrierPhysicsAdapter>();
    }

    private void ConfigureChildComponents()
    {
        if (placementInput != null)
            placementInput.Configure(mainCamera);

        if (visuals != null)
        {
            visuals.Configure(
                previewTilemap,
                previewTile,
                previewColor,
                zoneOutlineTilemap,
                zoneOutlineTile,
                zoneOutlineColor,
                drawCommittedOutline,
                debugLogs);
        }

        if (physicsAdapter != null)
            physicsAdapter.Configure(waterController, terrainTilemap, debugLogs);
    }

    private void SetZoneBoundaryModeActive(bool isActive)
    {
        if (zoneBoundaryMode == isActive)
            return;

        zoneBoundaryMode = isActive;
        ZoneBoundaryModeChanged?.Invoke(zoneBoundaryMode);
    }

    private void ClearHoverAndPreview()
    {
        _hoverGeoid = null;
        _hoverZoneTiles = null;

        if (visuals != null)
            visuals.ClearPreview();
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[BarrierController] {message}");

        if (npcToast != null)
            npcToast.Show(message);
    }

    private void UpdateGlobalHUDBarrierProgress()
    {
        if (GlobalHUDController.Instance == null)
            return;

        GlobalHUDController.Instance.SetBarrierProgress(
            BarriersPlaced,
            ActualMaxZoneBarriers);
    }
}
