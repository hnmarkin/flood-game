/*
Required Inspector references:
- TileMapData: map tile/category data used to choose valid communication tower tiles.
- FloodDefenseBoxStamp: provides zone GEOID tile groups and normalized-tile to Tilemap-cell conversion.
- Communication Tilemap: the Tilemap used to convert tower cells to world-space centers. Falls back to FloodDefenseBoxStamp.TerrainTilemap.
- ZoneThinOutlineByHover: optional; highlights a hovered tower's associated zone cluster.
- ShelterManager: optional; avoids active placed shelter tiles.
- ShelterCandidateController: optional; avoids valid shelter candidate tiles when available.
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

public class CommunicationTowerController : MonoBehaviour
{
    private readonly struct TileSnapshot
    {
        public readonly string category;
        public readonly bool isWater;

        public TileSnapshot(string category, bool isWater)
        {
            this.category = category;
            this.isWater = isWater;
        }
    }

    private sealed class ZoneCandidate
    {
        public string geoid;
        public Vector2 normalizedCenter;
    }

    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private FloodDefenseBoxStamp floodDefenseBoxStamp;
    [SerializeField] private Tilemap communicationTilemap;
    [SerializeField] private ZoneThinOutlineByHover zoneVisualController;
    [SerializeField] private ShelterManager shelterManager;
    [SerializeField] private ShelterCandidateController shelterCandidateController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MapHoverTooltipController mapHoverTooltip;
    [SerializeField] private UIDocument tooltipUIDocument;

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

    private readonly List<CommunicationTowerData> _communicationTowers = new();
    private readonly Dictionary<string, ZoneCommunicationStatus> _zoneStatusByGeoid = new();
    private readonly Dictionary<Vector3Int, CommunicationTowerData> _towerByCell = new();
    private readonly Dictionary<string, GameObject> _towerVisualById = new();
    private readonly Dictionary<string, SpriteRenderer> _towerRendererById = new();

    private CommunicationTowerData _hoveredTower;
    private bool _isCommunicationModeActive;
    private bool _hasGeneratedTowers;
    private bool _loggedMissingSpriteWarning;
    private bool _loggedActionButtonConnected;
    private Coroutine _generateWhenReadyRoutine;

    public bool IsCommunicationModeActive => _isCommunicationModeActive;
    public IReadOnlyList<CommunicationTowerData> CommunicationTowers => _communicationTowers;

    public event Action<bool> CommunicationModeChanged;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        _generateWhenReadyRoutine = StartCoroutine(GenerateWhenReadyRoutine());
    }

    private void Update()
    {
        if (!_isCommunicationModeActive)
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
        SetCommunicationMode(!_isCommunicationModeActive);
    }

    public void SetCommunicationMode(bool active)
    {
        if (active)
        {
            GenerateCommunicationTowers();
            ShowCommunicationTowers();
            _isCommunicationModeActive = true;

            if (debugLogs)
                Debug.Log("[CommunicationTowerController] Communication mode enabled.");
        }
        else
        {
            _isCommunicationModeActive = false;
            ClearHoveredTower();
            SetTowerVisualsVisible(showInactiveTowersAtStart, keepActiveTowersVisible: true);

            if (debugLogs)
                Debug.Log("[CommunicationTowerController] Communication mode disabled.");
        }

        CommunicationModeChanged?.Invoke(_isCommunicationModeActive);
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
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
        {
            status = ZoneCommunicationStatus.Unreached;
            return false;
        }

        return _zoneStatusByGeoid.TryGetValue(geoid, out status);
    }

    public void MarkZoneWarned(string geoid)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return;

        if (!_zoneStatusByGeoid.TryGetValue(geoid, out ZoneCommunicationStatus previousStatus))
            previousStatus = ZoneCommunicationStatus.Unreached;

        _zoneStatusByGeoid[geoid] = ZoneCommunicationStatus.Warned;

        if (debugLogs && previousStatus != ZoneCommunicationStatus.Warned)
        {
            Debug.Log(
                $"[CommunicationTowerController] Zone {geoid} communication status changed: " +
                $"{previousStatus} -> Warned.");
        }
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
        _hasGeneratedTowers = false;
        GenerateCommunicationTowers();
        SetTowerVisualsVisible(_isCommunicationModeActive || showInactiveTowersAtStart, keepActiveTowersVisible: true);
    }

    private IEnumerator GenerateWhenReadyRoutine()
    {
        const int maxFramesToWait = 120;
        int waitedFrames = 0;

        while (!_hasGeneratedTowers && waitedFrames < maxFramesToWait)
        {
            GenerateCommunicationTowers(logWarnings: false);

            if (_hasGeneratedTowers)
                break;

            waitedFrames++;
            yield return null;
        }

        if (!_hasGeneratedTowers)
            GenerateCommunicationTowers();

        SetTowerVisualsVisible(_isCommunicationModeActive || showInactiveTowersAtStart, keepActiveTowersVisible: true);
        _generateWhenReadyRoutine = null;
    }

    private void GenerateCommunicationTowers(bool logWarnings = true)
    {
        if (_hasGeneratedTowers)
            return;

        ResolveReferences();
        ClearTowerVisuals();

        _communicationTowers.Clear();
        _towerByCell.Clear();

        if (!ValidateGenerationReferences(logWarnings))
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationTowerController] Communication towers generated: 0.");

            return;
        }

        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles = floodDefenseBoxStamp.GetAllZoneTiles();

        if (allZoneTiles == null || allZoneTiles.Count == 0)
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationTowerController] Communication towers generated: 0.");

            return;
        }

        if (debugLogs)
            Debug.Log($"[CommunicationTowerController] Zones per tower: {zonesPerTower}.");

        List<string> geoids = new(allZoneTiles.Keys);
        geoids.Sort(StringComparer.Ordinal);
        InitializeZoneStatuses(geoids);

        HashSet<Vector3Int> occupiedTowerCells = new();
        int towerIndex = 1;

        List<List<string>> towerGroups = BuildZoneClusters(geoids, allZoneTiles);

        for (int groupIndex = 0; groupIndex < towerGroups.Count; groupIndex++)
        {
            List<string> groupGeoids = towerGroups[groupIndex];

            if (!TryGetGroupCenter(groupGeoids, allZoneTiles, out Vector2 groupCenter))
                continue;

            if (!TryFindTowerTile(groupCenter, occupiedTowerCells, out Vector2Int normalizedTowerTile))
            {
                Debug.LogWarning($"[CommunicationTowerController] No valid communication tower tile found near zone group {string.Join(", ", groupGeoids)}.");
                continue;
            }

            Vector3Int tileCell = floodDefenseBoxStamp.NormalizedTileToCell(normalizedTowerTile);
            Vector3 worldPosition = communicationTilemap.GetCellCenterWorld(tileCell);

            CommunicationTowerData tower = new()
            {
                towerId = $"tower_{towerIndex:00}",
                tileCell = tileCell,
                worldPosition = worldPosition,
                associatedZoneGeoids = groupGeoids,
                isActive = false,
            };

            _communicationTowers.Add(tower);
            _towerByCell[tileCell] = tower;
            occupiedTowerCells.Add(tileCell);
            SpawnTowerVisual(tower);

            if (debugLogs)
                Debug.Log($"[CommunicationTowerController] Tower {tower.towerId} serves zones: {string.Join(", ", tower.associatedZoneGeoids)}.");

            towerIndex++;
        }

        _hasGeneratedTowers = true;

        if (debugLogs)
            Debug.Log($"[CommunicationTowerController] Communication towers generated: {_communicationTowers.Count}.");
    }

    private void ResolveReferences()
    {
        if (floodDefenseBoxStamp == null)
            floodDefenseBoxStamp = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (tileMapData == null && floodDefenseBoxStamp != null)
            tileMapData = floodDefenseBoxStamp.MapData;

        if (tileMapData == null)
            tileMapData = FindTileMapDataAsset();

        if (zoneVisualController == null)
            zoneVisualController = FindFirstObjectByType<ZoneThinOutlineByHover>();

        if (shelterManager == null)
            shelterManager = FindFirstObjectByType<ShelterManager>();

        if (shelterCandidateController == null)
            shelterCandidateController = FindFirstObjectByType<ShelterCandidateController>();

        if (communicationTilemap == null && floodDefenseBoxStamp != null)
            communicationTilemap = floodDefenseBoxStamp.TerrainTilemap;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mapHoverTooltip == null)
            mapHoverTooltip = FindFirstObjectByType<MapHoverTooltipController>();

        if (tooltipUIDocument == null)
            tooltipUIDocument = ResolveTooltipUIDocument();

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

    private void EnsureTooltipConfigured()
    {
        if (mapHoverTooltip == null)
            return;

        if (tooltipUIDocument == null)
            tooltipUIDocument = ResolveTooltipUIDocument();

        mapHoverTooltip.SetTooltipUIDocument(tooltipUIDocument);
        mapHoverTooltip.SetCommunicationTowerController(this);
        mapHoverTooltip.SetDebugLogs(debugLogs);
    }

    private UIDocument ResolveTooltipUIDocument()
    {
        if (GlobalHUDController.Instance != null &&
            GlobalHUDController.Instance.TryGetComponent(out UIDocument globalDocument))
        {
            return globalDocument;
        }

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        UIDocument[] documents = UnityEngine.Object.FindObjectsOfType<UIDocument>(true);
#endif

        UIDocument fallback = null;

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];

            if (document == null || document.rootVisualElement == null)
                continue;

            fallback ??= document;

            VisualElement root = document.rootVisualElement;

            if (root.Q<Label>("money_label") != null ||
                root.Q<Label>("pop_label") != null ||
                root.Q<Label>("zone_label") != null ||
                root.Q<VisualElement>("communication_tooltip_root") != null)
            {
                return document;
            }
        }

        return fallback;
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

        if (floodDefenseBoxStamp == null)
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationTowerController] FloodDefenseBoxStamp is not assigned.");

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

    private void InitializeZoneStatuses(List<string> geoids)
    {
        _zoneStatusByGeoid.Clear();

        for (int i = 0; i < geoids.Count; i++)
        {
            string geoid = NormalizeGeoid(geoids[i]);

            if (!string.IsNullOrEmpty(geoid) && !_zoneStatusByGeoid.ContainsKey(geoid))
                _zoneStatusByGeoid.Add(geoid, ZoneCommunicationStatus.Unreached);
        }
    }

    private List<List<string>> BuildZoneClusters(
        List<string> geoids,
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles)
    {
        List<List<string>> groups = new();
        List<ZoneCandidate> unassignedZones = new();

        for (int i = 0; i < geoids.Count; i++)
        {
            string geoid = NormalizeGeoid(geoids[i]);

            if (string.IsNullOrEmpty(geoid) ||
                !TryGetZoneCenter(geoid, allZoneTiles, out Vector2 center))
            {
                continue;
            }

            unassignedZones.Add(new ZoneCandidate
            {
                geoid = geoid,
                normalizedCenter = center,
            });
        }

        while (unassignedZones.Count > 0)
        {
            ZoneCandidate seed = unassignedZones[0];
            unassignedZones.RemoveAt(0);

            List<ZoneCandidate> cluster = new() { seed };

            while (cluster.Count < zonesPerTower && unassignedZones.Count > 0)
            {
                Vector2 clusterCenter = GetClusterCenter(cluster);
                int nearestIndex = FindNearestZoneIndex(unassignedZones, clusterCenter);

                cluster.Add(unassignedZones[nearestIndex]);
                unassignedZones.RemoveAt(nearestIndex);
            }

            List<string> groupGeoids = new();

            for (int i = 0; i < cluster.Count; i++)
                groupGeoids.Add(cluster[i].geoid);

            groups.Add(groupGeoids);
        }

        return groups;
    }

    private bool TryGetGroupCenter(
        List<string> groupGeoids,
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles,
        out Vector2 groupCenter)
    {
        groupCenter = Vector2.zero;

        if (groupGeoids == null || groupGeoids.Count == 0)
            return false;

        Vector2 sum = Vector2.zero;
        int tileCount = 0;

        for (int i = 0; i < groupGeoids.Count; i++)
        {
            string geoid = groupGeoids[i];

            if (!allZoneTiles.TryGetValue(geoid, out HashSet<Vector2Int> zoneTiles) || zoneTiles == null)
                continue;

            foreach (Vector2Int tile in zoneTiles)
            {
                sum += tile;
                tileCount++;
            }
        }

        if (tileCount == 0)
            return false;

        groupCenter = sum / tileCount;
        return true;
    }

    private bool TryGetZoneCenter(
        string geoid,
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles,
        out Vector2 center)
    {
        center = Vector2.zero;

        if (string.IsNullOrEmpty(geoid) ||
            allZoneTiles == null ||
            !allZoneTiles.TryGetValue(geoid, out HashSet<Vector2Int> zoneTiles) ||
            zoneTiles == null ||
            zoneTiles.Count == 0)
        {
            return false;
        }

        Vector2 sum = Vector2.zero;
        int tileCount = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            sum += tile;
            tileCount++;
        }

        if (tileCount == 0)
            return false;

        center = sum / tileCount;
        return true;
    }

    private Vector2 GetClusterCenter(List<ZoneCandidate> cluster)
    {
        if (cluster == null || cluster.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;

        for (int i = 0; i < cluster.Count; i++)
            sum += cluster[i].normalizedCenter;

        return sum / cluster.Count;
    }

    private int FindNearestZoneIndex(List<ZoneCandidate> candidates, Vector2 center)
    {
        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            float distance = Vector2.SqrMagnitude(candidates[i].normalizedCenter - center);

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestIndex = i;
        }

        return nearestIndex;
    }

    private bool TryFindTowerTile(
        Vector2 groupCenter,
        HashSet<Vector3Int> occupiedTowerCells,
        out Vector2Int bestTile)
    {
        bestTile = default;
        int maxRadius = Mathf.Max(towerSearchRadius, GetMapLimit());
        float bestScore = float.MaxValue;
        bool found = false;

        Vector2Int roundedCenter = new(
            Mathf.RoundToInt(groupCenter.x),
            Mathf.RoundToInt(groupCenter.y)
        );

        for (int radius = 0; radius <= maxRadius; radius++)
        {
            bool foundAtRadius = false;

            for (int y = roundedCenter.y - radius; y <= roundedCenter.y + radius; y++)
            {
                for (int x = roundedCenter.x - radius; x <= roundedCenter.x + radius; x++)
                {
                    if (Mathf.Abs(x - roundedCenter.x) != radius && Mathf.Abs(y - roundedCenter.y) != radius)
                        continue;

                    Vector2Int candidate = new(x, y);

                    if (!TryScoreTowerTile(candidate, groupCenter, occupiedTowerCells, out float score))
                        continue;

                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    bestTile = candidate;
                    found = true;
                    foundAtRadius = true;
                }
            }

            if (foundAtRadius)
                return true;
        }

        return found;
    }

    private bool TryScoreTowerTile(
        Vector2Int tile,
        Vector2 groupCenter,
        HashSet<Vector3Int> occupiedTowerCells,
        out float score)
    {
        score = float.MaxValue;

        if (!TryGetTileSnapshot(tile, out TileSnapshot snapshot) || snapshot.isWater)
            return false;

        Vector3Int cell = floodDefenseBoxStamp.NormalizedTileToCell(tile);

        if (occupiedTowerCells != null && occupiedTowerCells.Contains(cell))
            return false;

        if (avoidShelterTiles && IsShelterTile(cell))
            return false;

        int priority = GetTowerTilePriority(tile, snapshot);
        float distance = Vector2.Distance(new Vector2(tile.x, tile.y), groupCenter);
        score = (priority * 1000f) + distance;
        return true;
    }

    private bool TryGetTileSnapshot(Vector2Int tile, out TileSnapshot snapshot)
    {
        snapshot = default;

        if (!IsWithinMap(tile) || tileMapData == null)
            return false;

        TileInstance tileInstance = tileMapData.Get(tile);

        if (tileInstance == null)
            return false;

        string category = tileInstance.category;
        bool isWater = IsWaterCategory(category) ||
                       (tileInstance.tileType != null && tileInstance.tileType.isWater);

        snapshot = new TileSnapshot(category, isWater);
        return true;
    }

    private int GetTowerTilePriority(Vector2Int tile, TileSnapshot snapshot)
    {
        if (IsBuildingCategory(snapshot.category))
            return 0;

        if (IsRoadAdjacentUsableTile(tile, snapshot))
            return 1;

        if (IsLandCategory(snapshot.category))
            return 2;

        return 3;
    }

    private bool IsRoadAdjacentUsableTile(Vector2Int tile, TileSnapshot snapshot)
    {
        if (snapshot.isWater)
            return false;

        if (!IsLandCategory(snapshot.category) && !IsBuildingCategory(snapshot.category))
            return false;

        Vector2Int[] directions =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
        };

        for (int i = 0; i < directions.Length; i++)
        {
            if (!TryGetTileSnapshot(tile + directions[i], out TileSnapshot neighborSnapshot))
                continue;

            if (IsRoadCategory(neighborSnapshot.category))
                return true;
        }

        return false;
    }

    private bool IsShelterTile(Vector3Int cell)
    {
        if (shelterManager != null && shelterManager.TryGetShelterAtTile(cell, out PlacedShelterData placedShelter))
        {
            if (placedShelter == null || placedShelter.isActive)
                return true;
        }

        if (shelterCandidateController != null && shelterCandidateController.IsValidShelterCandidateTile(cell))
            return true;

        return false;
    }

    private void SpawnTowerVisual(CommunicationTowerData tower)
    {
        if (tower == null || string.IsNullOrEmpty(tower.towerId))
            return;

        if (towerVisualRoot == null)
            ResolveReferences();

        GameObject towerObject = new($"CommunicationTower_{tower.towerId}");
        towerObject.transform.SetParent(towerVisualRoot, false);
        Vector3 visualPosition = tower.worldPosition;
        visualPosition.y += towerHeightOffset;
        towerObject.transform.position = visualPosition;
        towerObject.transform.localScale = Vector3.one * towerVisualScale;

        SpriteRenderer spriteRenderer = towerObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForTower(tower);
        spriteRenderer.color = tower.isActive ? activeTowerColor : inactiveTowerColor;
        spriteRenderer.sortingLayerName = towerSortingLayerName;
        spriteRenderer.sortingOrder = towerSortingOrder;

        if (spriteRenderer.sprite == null && !_loggedMissingSpriteWarning)
        {
            _loggedMissingSpriteWarning = true;
            Debug.LogWarning("[CommunicationTowerController] Main Tower Sprite and Zone Tower Sprite are not assigned. Empty tower placeholder objects will still be spawned.");
        }

        _towerVisualById[tower.towerId] = towerObject;
        _towerRendererById[tower.towerId] = spriteRenderer;
    }

    private void UpdateTowerVisual(CommunicationTowerData tower)
    {
        if (tower == null || string.IsNullOrEmpty(tower.towerId))
            return;

        if (!_towerRendererById.TryGetValue(tower.towerId, out SpriteRenderer spriteRenderer) || spriteRenderer == null)
            return;

        spriteRenderer.sprite = GetSpriteForTower(tower);
        spriteRenderer.color = tower == _hoveredTower
            ? hoveredTowerColor
            : tower.isActive
                ? activeTowerColor
                : inactiveTowerColor;
    }

    private Sprite GetSpriteForTower(CommunicationTowerData tower)
    {
        if (tower != null && tower.isActive)
            return zoneTowerSprite != null ? zoneTowerSprite : mainTowerSprite;

        return mainTowerSprite != null ? mainTowerSprite : zoneTowerSprite;
    }

    private void ClearTowerVisuals()
    {
        foreach (KeyValuePair<string, GameObject> pair in _towerVisualById)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }

        _towerVisualById.Clear();
        _towerRendererById.Clear();
    }

    private void SetTowerVisualsVisible(bool visible, bool keepActiveTowersVisible = false)
    {
        for (int i = 0; i < _communicationTowers.Count; i++)
        {
            CommunicationTowerData tower = _communicationTowers[i];

            if (tower == null || string.IsNullOrEmpty(tower.towerId))
                continue;

            if (!_towerVisualById.TryGetValue(tower.towerId, out GameObject visual) || visual == null)
                continue;

            visual.SetActive(visible || (keepActiveTowersVisible && tower.isActive));
        }
    }

    private void UpdateHoveredTower()
    {
        Vector2 mouseScreen = new(Input.mousePosition.x, Input.mousePosition.y);

        if (!TryGetTowerUnderMouse(out CommunicationTowerData tower))
        {
            ClearHoveredTower();
            return;
        }

        if (_hoveredTower == tower)
        {
            ShowTowerTooltip(tower, mouseScreen);
            return;
        }

        ClearHoveredTower();
        _hoveredTower = tower;
        UpdateTowerVisual(_hoveredTower);
        ShowAssociatedZoneHighlight(_hoveredTower);
        ShowTowerTooltip(_hoveredTower, mouseScreen);
    }

    private void HandleTowerClick()
    {
        if (_hoveredTower == null || !Input.GetMouseButtonDown(0))
            return;

        ActivateTower(_hoveredTower);
    }

    private bool TryGetTowerUnderMouse(out CommunicationTowerData tower)
    {
        tower = null;

        if (mainCamera == null || communicationTilemap == null)
            return false;

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        Vector3Int cell = communicationTilemap.WorldToCell(world);
        return _towerByCell.TryGetValue(cell, out tower) && tower != null;
    }

    private void ActivateTower(CommunicationTowerData tower)
    {
        if (tower == null)
            return;

        if (tower.isActive)
        {
            if (debugLogs)
                Debug.Log($"[CommunicationTowerController] Tower {tower.towerId} is already active.");

            return;
        }

        tower.isActive = true;
        UpdateTowerVisual(tower);

        if (debugLogs)
            Debug.Log($"[CommunicationTowerController] Tower {tower.towerId} activated.");

        for (int i = 0; i < tower.associatedZoneGeoids.Count; i++)
        {
            string geoid = NormalizeGeoid(tower.associatedZoneGeoids[i]);

            if (string.IsNullOrEmpty(geoid))
                continue;

            if (!_zoneStatusByGeoid.TryGetValue(geoid, out ZoneCommunicationStatus previousStatus))
                previousStatus = ZoneCommunicationStatus.Unreached;

            if (previousStatus != ZoneCommunicationStatus.Unreached)
                continue;

            _zoneStatusByGeoid[geoid] = ZoneCommunicationStatus.Pending;

            if (debugLogs)
            {
                Debug.Log(
                    $"[CommunicationTowerController] Zone {geoid} communication status changed: " +
                    "Unreached -> Pending.");
            }
        }
    }

    private void ShowAssociatedZoneHighlight(CommunicationTowerData tower)
    {
        if (tower == null || zoneVisualController == null)
            return;

        List<ZoneRiskOutlineRequest> requests = new();

        for (int i = 0; i < tower.associatedZoneGeoids.Count; i++)
        {
            string geoid = NormalizeGeoid(tower.associatedZoneGeoids[i]);

            if (!string.IsNullOrEmpty(geoid))
                requests.Add(new ZoneRiskOutlineRequest(geoid, associatedZoneHoverColor));
        }

        zoneVisualController.ShowCommunicationZoneOutlines(requests);
    }

    private void ClearHoveredTower()
    {
        if (_hoveredTower != null)
        {
            CommunicationTowerData previousTower = _hoveredTower;
            _hoveredTower = null;
            UpdateTowerVisual(previousTower);
        }

        if (zoneVisualController != null)
            zoneVisualController.ClearCommunicationZoneOutlines();

        HideTowerTooltip();
    }

    private void ShowTowerTooltip(CommunicationTowerData tower, Vector2 screenPosition)
    {
        if (tower == null)
        {
            HideTowerTooltip();
            return;
        }

        EnsureTooltipConfigured();

        if (mapHoverTooltip == null)
            return;

        mapHoverTooltip.ShowCommunicationTowerTooltip(tower, screenPosition);
    }

    private void HideTowerTooltip()
    {
        if (mapHoverTooltip == null || !mapHoverTooltip.IsCommunicationTowerTooltipVisible)
            return;

        mapHoverTooltip.HideCommunicationTowerTooltip();
    }

    private bool IsWithinMap(Vector2Int tile)
    {
        if (tileMapData == null)
            return false;

        int width = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeX;
        int height = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeY;
        return tile.x >= 0 && tile.y >= 0 && tile.x < width && tile.y < height;
    }

    private int GetMapLimit()
    {
        if (tileMapData == null)
            return towerSearchRadius;

        int width = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeX;
        int height = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeY;
        return Mathf.Max(width, height, towerSearchRadius);
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

    private bool IsWaterCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        string normalized = category.ToLowerInvariant();
        return normalized.Contains("water") ||
               normalized.Contains("river") ||
               normalized.Contains("stream") ||
               normalized.Contains("lake") ||
               normalized.Contains("ocean") ||
               normalized.Contains("flood");
    }

    private bool IsBuildingCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        switch (category.ToLowerInvariant())
        {
            case "building":
            case "industrial":
            case "residential":
            case "commercial":
            case "city":
                return true;

            default:
                return false;
        }
    }

    private bool IsRoadCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        switch (category.ToLowerInvariant())
        {
            case "road":
            case "highway":
            case "rail":
                return true;

            default:
                return false;
        }
    }

    private bool IsLandCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        switch (category.ToLowerInvariant())
        {
            case "land":
            case "grass":
            case "park":
            case "forest":
            case "mountain":
            case "beach":
            case "field":
            case "soil":
                return true;

            default:
                return false;
        }
    }

    private string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? null : geoid.Trim();
    }
}
