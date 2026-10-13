using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FloodDefenseBoxStamp : MonoBehaviour, IBarrierProvider
{
    public static event Action OnAllZoneBarriersPlaced;
    public event Action<bool> ZoneBoundaryModeChanged;
    public event Action<int, int> ZoneBarrierPlaced;

    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;
    [SerializeField, HideInInspector] private WaterSimulator waterSimulator;
    [SerializeField] private ZoneController zoneController;
    [SerializeField] private WaterController waterController;
    [SerializeField] private BarrierController barrierController;

    private NpcToastController _npcToast;
    private BarrierController _subscribedBarrierController;

    [SerializeField, HideInInspector] private bool zoneBoundaryMode = false;

    [SerializeField, HideInInspector] private int maxZoneBarriers = 1;

    [SerializeField, HideInInspector] private bool autoStartFloodAfterBarrierLimit = true;
    [SerializeField, HideInInspector] private int requiredBarriersBeforeFlood = 3;

    [SerializeField, HideInInspector] private float barrierHeight = 40f;

    [SerializeField, HideInInspector] private float seepage = 0.001f;

    [SerializeField, HideInInspector] private Tilemap previewTilemap;
    [SerializeField, HideInInspector] private TileBase previewTile;
    [SerializeField, HideInInspector] private Color previewColor = new Color(0.2f, 1f, 0.2f, 0.55f);

    [SerializeField, HideInInspector] private Tilemap zoneOutlineTilemap;
    [SerializeField, HideInInspector] private TileBase zoneOutlineTile;
    [SerializeField, HideInInspector] private Color zoneOutlineColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField, HideInInspector] private bool drawCommittedOutline = true;

    [SerializeField, HideInInspector] private bool debugLogs = true;

    public int BarriersPlaced => barrierController != null
        ? barrierController.BarriersPlaced
        : 0;

    public int ActualMaxZoneBarriers => barrierController != null
        ? barrierController.ActualMaxZoneBarriers
        : 1;

    public int RequiredBarriersBeforeFlood => barrierController != null
        ? barrierController.RequiredBarriersBeforeFlood
        : 1;

    public bool IsZoneBoundaryModeActive => barrierController != null
        ? barrierController.IsZoneBoundaryModeActive
        : false;

    public TileMapData MapData => tileMapData;

    public Vector2Int TileOrigin => zoneController != null
        ? zoneController.TileOrigin
        : Vector2Int.zero;

    public Tilemap TerrainTilemap => zoneController != null && zoneController.TerrainTilemap != null
        ? zoneController.TerrainTilemap
        : terrainTilemap;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        _npcToast = NpcToastController.Instance != null
            ? NpcToastController.Instance
            : FindFirstObjectByType<NpcToastController>();

        EnsureZoneControllerConfigured();
        EnsureBarrierControllerConfigured();
        PreserveLegacySerializedConfiguration();

        zoneBoundaryMode = false;
        barrierController?.SetPlaceBarrierMode(false);

        if (_npcToast == null)
            Debug.LogWarning("[FloodDefenseBoxStamp] NpcToastController not found in scene.");
        else if (ShouldLog)
            Debug.Log("[FloodDefenseBoxStamp] NpcToastController found OK.");

        if (ShouldLog)
            Debug.Log("[FloodDefenseBoxStamp] Startup complete. Zone boundary mode forced OFF.");
    }

    private void OnEnable()
    {
        EnsureBarrierControllerConfigured();
        SubscribeToBarrierController();
    }

    private void OnDisable()
    {
        if (barrierController != null)
            barrierController.ExitZoneBoundaryPlacementMode();

        UnsubscribeFromBarrierController();
        zoneBoundaryMode = false;
    }

    private bool ShouldLog => barrierController != null && barrierController.DebugLogsEnabled;

    private void PreserveLegacySerializedConfiguration()
    {
        _ = waterSimulator;
        _ = zoneBoundaryMode;
        _ = maxZoneBarriers;
        _ = autoStartFloodAfterBarrierLimit;
        _ = requiredBarriersBeforeFlood;
        _ = barrierHeight;
        _ = seepage;
        _ = previewTilemap;
        _ = previewTile;
        _ = previewColor;
        _ = zoneOutlineTilemap;
        _ = zoneOutlineTile;
        _ = zoneOutlineColor;
        _ = drawCommittedOutline;
        _ = debugLogs;
    }

    private bool EnsureBarrierControllerConfigured()
    {
        if (barrierController == null)
            barrierController = GetComponent<BarrierController>();

        if (barrierController == null)
            barrierController = gameObject.AddComponent<BarrierController>();

        if (waterController == null)
            waterController = FindFirstObjectByType<WaterController>();

        if (_npcToast == null)
        {
            _npcToast = NpcToastController.Instance != null
                ? NpcToastController.Instance
                : FindFirstObjectByType<NpcToastController>();
        }

        barrierController.ConfigureReferencesFromLegacyAdapter(
            tileMapData,
            terrainTilemap,
            mainCamera,
            jsonMapLoader,
            zoneController,
            waterController,
            _npcToast);

        SubscribeToBarrierController();
        return true;
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
            Debug.LogWarning("[FloodDefenseBoxStamp] ZoneController was missing and was added at runtime. Add it in the scene for serialized setup.");
        }

        if (zoneController == null)
            return false;

        zoneController.Configure(tileMapData, terrainTilemap, jsonMapLoader, ShouldLog);
        return true;
    }

    private void SubscribeToBarrierController()
    {
        if (_subscribedBarrierController == barrierController)
            return;

        UnsubscribeFromBarrierController();

        if (barrierController == null)
            return;

        barrierController.ZoneBoundaryModeChanged += OnBarrierModeChanged;
        barrierController.ZoneBarrierPlaced += OnBarrierPlaced;
        barrierController.AllZoneBarriersPlaced += OnAllBarriersPlaced;
        _subscribedBarrierController = barrierController;
    }

    private void UnsubscribeFromBarrierController()
    {
        if (_subscribedBarrierController == null)
            return;

        _subscribedBarrierController.ZoneBoundaryModeChanged -= OnBarrierModeChanged;
        _subscribedBarrierController.ZoneBarrierPlaced -= OnBarrierPlaced;
        _subscribedBarrierController.AllZoneBarriersPlaced -= OnAllBarriersPlaced;
        _subscribedBarrierController = null;
    }

    private void OnBarrierModeChanged(bool isActive)
    {
        zoneBoundaryMode = isActive;
        ZoneBoundaryModeChanged?.Invoke(isActive);
    }

    private void OnBarrierPlaced(int placed, int required)
    {
        ZoneBarrierPlaced?.Invoke(placed, required);
    }

    private void OnAllBarriersPlaced()
    {
        OnAllZoneBarriersPlaced?.Invoke();
    }

    public void ToggleBuildModeFromUI()
    {
        EnsureBarrierControllerConfigured();
        barrierController.ToggleBuildModeFromUI();
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
        EnsureBarrierControllerConfigured();
        barrierController.EnablePlaceBarrierMode();
    }

    public void DisablePlaceBarrierMode()
    {
        EnsureBarrierControllerConfigured();
        barrierController.DisablePlaceBarrierMode();
    }

    public void SetPlaceBarrierMode(bool enabled)
    {
        EnsureBarrierControllerConfigured();
        barrierController.SetPlaceBarrierMode(enabled);
    }

    public void EnterZoneBoundaryPlacementMode()
    {
        EnsureBarrierControllerConfigured();
        barrierController.EnterZoneBoundaryPlacementMode();
    }

    public void ExitZoneBoundaryPlacementMode()
    {
        EnsureBarrierControllerConfigured();
        barrierController.ExitZoneBoundaryPlacementMode();
    }

    public bool EnsureZoneIndexReadyForExternalUse()
    {
        return EnsureZoneControllerConfigured() &&
               zoneController.EnsureZoneIndexReadyForExternalUse();
    }

    public bool TryGetZoneTiles(string geoid, out HashSet<Vector2Int> zoneTiles)
    {
        zoneTiles = null;

        if (string.IsNullOrEmpty(geoid))
            return false;

        if (!EnsureZoneControllerConfigured())
            return false;

        return zoneController.TryGetZoneTiles(geoid, out zoneTiles);
    }

    public IReadOnlyDictionary<string, HashSet<Vector2Int>> GetAllZoneTiles()
    {
        if (!EnsureZoneControllerConfigured())
            return null;

        return zoneController.GetAllZoneTiles();
    }

    public bool TryGetZoneTilesAtWorld(
        Vector3 world,
        out string geoid,
        out HashSet<Vector2Int> zoneTiles)
    {
        geoid = null;
        zoneTiles = null;

        if (!EnsureZoneControllerConfigured())
            return false;

        return zoneController.TryGetZoneTilesAtWorld(world, out geoid, out zoneTiles);
    }

    public Vector3Int NormalizedTileToCell(Vector2Int tile)
    {
        if (EnsureZoneControllerConfigured())
            return zoneController.NormalizedTileToCell(tile);

        return new Vector3Int(tile.x, tile.y, 0);
    }

    public bool IsBlockedX(int x, int y)
    {
        return barrierController != null && barrierController.IsBlockedX(x, y);
    }

    public bool IsBlockedY(int x, int y)
    {
        return barrierController != null && barrierController.IsBlockedY(x, y);
    }

    public float GetBarrierHeightX(int x, int y)
    {
        return barrierController != null ? barrierController.GetBarrierHeightX(x, y) : 0f;
    }

    public float GetBarrierHeightY(int x, int y)
    {
        return barrierController != null ? barrierController.GetBarrierHeightY(x, y) : 0f;
    }

    public float GetSeepageX(int x, int y)
    {
        return barrierController != null ? barrierController.GetSeepageX(x, y) : 0f;
    }

    public float GetSeepageY(int x, int y)
    {
        return barrierController != null ? barrierController.GetSeepageY(x, y) : 0f;
    }
}
