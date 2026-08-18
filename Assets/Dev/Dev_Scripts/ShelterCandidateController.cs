using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

[Serializable]
public class ShelterZoneOverride
{
    public string geoid;
    public bool shelterPlacementEnabled = true;
}

[Serializable]
public class ShelterCandidateResult
{
    public string candidateId;
    public Vector3Int tileCell;
    public Vector3 worldPosition;
    public List<string> associatedZoneGeoids = new();
    public int zoneCount;
    public float suitabilityScore;
    public bool isValid;
    public string debugReason;
}

public class ShelterCandidateController : MonoBehaviour
{
    private sealed class ZoneContext
    {
        public string geoid;
        public HashSet<Vector2Int> tiles;
        public Vector2 normalizedCenter;
        public Vector3 worldCenter;
        public bool hasWorldCenter;
        public bool isEnabled;
    }

    private sealed class ShelterGroup
    {
        public readonly List<ZoneContext> hostZones = new();
        public readonly List<ZoneContext> associatedZones = new();
    }

    [Header("References")]
    [SerializeField] private FloodDefenseBoxStamp floodDefense;
    [SerializeField] private JsonMapLoader jsonMapLoader;
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ZoneThinOutlineByHover zoneOutlineHighlighter;
    [SerializeField] private ZoneBaselineRiskController zoneBaselineRiskController;
    [SerializeField] private FloodImpactController floodImpactController;
    [SerializeField] private ShelterManager shelterManager;
    [SerializeField] private UIDocument inventoryToolsDocument;
    [SerializeField] private Transform labelParent;
    [SerializeField] private TMP_FontAsset labelFontAsset;

    [Header("Grouping")]
    [SerializeField, Min(1)] private int minZonesPerShelter = 3;
    [SerializeField, Min(1)] private int maxZonesPerShelter = 5;
    [SerializeField] private bool allowSingleZoneShelters = false;
    [SerializeField] private bool allowUndersizedFallbackGroups = true;

    [Header("Zone Overrides")]
    [SerializeField] private bool defaultZoneEnabled = true;
    [SerializeField] private bool allowDisabledZonesToBeServedByNearbyShelters = false;
    [SerializeField] private List<ShelterZoneOverride> zoneOverrides = new();

    [Header("Candidate Suitability")]
    [SerializeField, Min(0f)] private float deepFloodWaterThreshold = 0.25f;
    [SerializeField, Min(0)] private int fallbackSearchRadiusCells = 4;
    [SerializeField, Min(1)] private int supportFeatureDistanceForMaxScore = 4;
    [SerializeField, Min(1)] private int waterClearanceDistanceForMaxScore = 6;
    [SerializeField, Min(0.01f)] private float centerPreferenceDistanceCells = 10f;
    [SerializeField, Min(0f)] private float elevationWeight = 1.1f;
    [SerializeField, Min(0f)] private float centerWeight = 1.0f;
    [SerializeField, Min(0f)] private float supportFeatureWeight = 0.45f;
    [SerializeField, Min(0f)] private float waterClearanceWeight = 0.55f;
    [SerializeField, Min(0f)] private float waterPenaltyWeight = 0.8f;
    [SerializeField] private List<string> waterCategoryKeywords = new()
    {
        "water",
        "river",
        "stream",
        "lake",
        "ocean",
        "flood_source",
    };

    [Header("Visualization")]
    [SerializeField] private Color candidateTileColor = new Color(0.15f, 0.9f, 1f, 1f);
    [SerializeField] private Color associatedZoneColor = new Color(0.3f, 0.85f, 0.85f, 1f);
    [SerializeField] private bool showCandidateLabels = true;
    [SerializeField] private float labelHeightOffset = 0.85f;
    [SerializeField] private float labelFontSize = 3.25f;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private int labelSortingOrder = 2150;

    [Header("Shelter Placement")]
    [SerializeField] private GameObject shelterGhostPrefab;
    [SerializeField] private SpriteRenderer shelterGhostRenderer;
    [SerializeField] private Sprite defaultShelterSprite;
    [SerializeField] private GameObject placedShelterPrefab;
    [SerializeField] private bool keepPlacementModeActiveAfterPlacement = false;
    [SerializeField] private bool showCandidateHighlightsDuringPlacement = true;
    [SerializeField] private Color validPlacementColor = new Color(0.35f, 1f, 0.35f, 0.75f);
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0.2f, 0.2f, 0.55f);
    [SerializeField] private float ghostHeightOffset = 0.25f;
    [SerializeField] private int ghostSortingOrder = 2200;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<ShelterCandidateResult> _candidateResults = new();
    private readonly List<GameObject> _spawnedLabels = new();
    private readonly List<GameObject> _placedShelterObjects = new();
    private readonly Dictionary<string, bool> _zoneEnabledLookup = new();

    private bool _hasCachedCandidates;
    private bool _isShowingCandidates;
    private bool _isShelterPlacementModeActive;
    private bool _placementModeShowedCandidateHighlights;
    private bool _candidateHighlightsWereShowingBeforePlacement;
    private int _placementStartedFrame = -1;
    private ShelterTypeDefinition _selectedShelterType;
    private GameObject _runtimeGhostObject;
    private SpriteRenderer _runtimeGhostRenderer;
    private SpriteRenderer[] _runtimeGhostRenderers = Array.Empty<SpriteRenderer>();
    private Sprite _generatedFallbackShelterSprite;
    private bool _loggedMissingDefaultShelterSprite;

    public bool IsShelterCandidateModeActive => _isShowingCandidates;
    public bool IsShelterPlacementModeActive => _isShelterPlacementModeActive;
    public bool HasConfiguredShelterPlacementVisuals => shelterGhostPrefab != null || shelterGhostRenderer != null || defaultShelterSprite != null;
    public event Action<bool> ShelterPlacementModeChanged;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (_isShelterPlacementModeActive)
            UpdateShelterPlacementMode();
    }

    private void OnDisable()
    {
        ExitShelterPlacementMode();
        HideShelterCandidates();
    }

    private void OnValidate()
    {
        minZonesPerShelter = Mathf.Max(1, minZonesPerShelter);
        maxZonesPerShelter = Mathf.Max(minZonesPerShelter, maxZonesPerShelter);
        supportFeatureDistanceForMaxScore = Mathf.Max(1, supportFeatureDistanceForMaxScore);
        waterClearanceDistanceForMaxScore = Mathf.Max(1, waterClearanceDistanceForMaxScore);
        centerPreferenceDistanceCells = Mathf.Max(0.01f, centerPreferenceDistanceCells);
        labelFontSize = Mathf.Max(0.1f, labelFontSize);
        ghostHeightOffset = Mathf.Max(0f, ghostHeightOffset);
    }

    [ContextMenu("Refresh Shelter Candidates")]
    public void RefreshShelterCandidates()
    {
        _candidateResults.Clear();
        _zoneEnabledLookup.Clear();
        _hasCachedCandidates = false;

        if (!ValidateReferences())
            return;

        if (!IsMapReady())
        {
            Debug.LogWarning("[ShelterCandidateController] Shelter candidates cannot be refreshed yet because map data is not ready.");
            return;
        }

        BuildZoneEnabledLookup();

        List<ZoneContext> enabledZones = new();
        List<ZoneContext> disabledServableZones = new();

        if (!TryBuildZoneContexts(enabledZones, disabledServableZones))
        {
            Debug.LogWarning("[ShelterCandidateController] Shelter candidates could not be built because no valid zones were found.");
            return;
        }

        float minElevation;
        float maxElevation;
        GetElevationBounds(out minElevation, out maxElevation);

        int mapSize = tileMapData.N;
        int[,] supportDistanceMap = BuildDistanceMap(mapSize, IsSupportFeatureTile);
        int[,] waterDistanceMap = BuildDistanceMap(mapSize, IsWaterTile);

        List<ShelterGroup> groups = BuildGroups(enabledZones);

        if (allowDisabledZonesToBeServedByNearbyShelters && disabledServableZones.Count > 0)
            AttachDisabledServedZones(disabledServableZones, groups);

        int validCount = 0;

        for (int i = 0; i < groups.Count; i++)
        {
            ShelterCandidateResult result = BuildCandidateResult(
                i,
                groups[i],
                supportDistanceMap,
                waterDistanceMap,
                minElevation,
                maxElevation);

            if (result.isValid)
                validCount++;

            _candidateResults.Add(result);
        }

        _hasCachedCandidates = true;

        if (debugLogs)
            Debug.Log($"[ShelterCandidateController] Refreshed {validCount} valid shelter candidate groups from {enabledZones.Count} enabled zones.");

        if (_isShowingCandidates)
            ShowShelterCandidates();
    }

    [ContextMenu("Show Shelter Candidates")]
    public void ShowShelterCandidates()
    {
        if (!_hasCachedCandidates)
            RefreshShelterCandidates();

        ClearVisuals();

        if (_candidateResults.Count == 0)
        {
            if (debugLogs)
                Debug.Log("[ShelterCandidateController] No shelter candidates are cached to show.");

            _isShowingCandidates = false;
            return;
        }

        List<ZoneRiskOutlineRequest> zoneRequests = new();
        List<TileOutlineRequest> tileRequests = new();

        for (int i = 0; i < _candidateResults.Count; i++)
        {
            ShelterCandidateResult result = _candidateResults[i];

            if (!result.isValid)
                continue;

            if (shelterManager != null && !shelterManager.CanPlaceShelterAtTile(result.tileCell))
                continue;

            tileRequests.Add(new TileOutlineRequest(result.tileCell, candidateTileColor));

            for (int zoneIndex = 0; zoneIndex < result.associatedZoneGeoids.Count; zoneIndex++)
            {
                string geoid = NormalizeGeoid(result.associatedZoneGeoids[zoneIndex]);

                // Disabled zones may be served when explicitly allowed, but they remain visually inactive.
                if (string.IsNullOrEmpty(geoid) || !IsZoneEnabledForShelterPlacement(geoid))
                    continue;

                zoneRequests.Add(new ZoneRiskOutlineRequest(geoid, associatedZoneColor));
            }

            if (showCandidateLabels)
                SpawnCandidateLabel(result);
        }

        if (zoneOutlineHighlighter != null)
        {
            zoneOutlineHighlighter.ShowShelterAssociatedZoneOutlines(zoneRequests);
            zoneOutlineHighlighter.ShowShelterCandidateTiles(tileRequests);
        }

        _isShowingCandidates = tileRequests.Count > 0;

        if (debugLogs)
            Debug.Log($"[ShelterCandidateController] Showing {tileRequests.Count} shelter candidate tile highlights.");
    }

    [ContextMenu("Hide Shelter Candidates")]
    public void HideShelterCandidates()
    {
        ClearVisuals();
        _isShowingCandidates = false;
    }

    public void ToggleShelterCandidates()
    {
        if (_isShowingCandidates)
        {
            HideShelterCandidates();
            return;
        }

        ShowShelterCandidates();
    }

    public void EnterShelterPlacementMode()
    {
        if (!ValidateReferences())
            return;

        if (shelterManager == null)
        {
            Debug.LogWarning("[ShelterCandidateController] ShelterManager is not assigned, so shelter placement mode cannot start.");
            return;
        }

        if (!shelterManager.TryGetDefaultShelterType(out _selectedShelterType))
        {
            Debug.LogWarning("[ShelterCandidateController] Shelter placement mode cannot start because no shelter type is available.");
            return;
        }

        if (!_hasCachedCandidates)
            RefreshShelterCandidates();

        if (_candidateResults.Count == 0)
        {
            Debug.LogWarning("[ShelterCandidateController] Shelter placement mode cannot start because no shelter candidates are available.");
            return;
        }

        if (floodDefense != null && floodDefense.IsZoneBoundaryModeActive)
            floodDefense.ExitBuildModeFromUI();

        _candidateHighlightsWereShowingBeforePlacement = _isShowingCandidates;
        _placementModeShowedCandidateHighlights = showCandidateHighlightsDuringPlacement && !_candidateHighlightsWereShowingBeforePlacement;

        if (showCandidateHighlightsDuringPlacement)
            ShowShelterCandidates();

        _isShelterPlacementModeActive = true;
        _placementStartedFrame = Time.frameCount;
        bool hasGhostVisual = EnsureGhostRenderer();
        PrepareGhostVisuals(validPlacementColor);
        SetGhostVisible(true);
        ShelterPlacementModeChanged?.Invoke(true);

        if (debugLogs)
        {
            Debug.Log($"[ShelterCandidateController] Shelter placement mode enabled with default type '{_selectedShelterType.shelterTypeName}'.");
            Debug.Log(hasGhostVisual
                ? "[ShelterCandidateController] Shelter ghost visual prepared and activated."
                : "[ShelterCandidateController] Shelter placement mode is active, but the ghost visual could not be prepared.");
        }
    }

    public void ToggleShelterPlacementMode()
    {
        if (_isShelterPlacementModeActive)
            ExitShelterPlacementMode();
        else
            EnterShelterPlacementMode();
    }

    public void ExitShelterPlacementMode()
    {
        bool wasActive = _isShelterPlacementModeActive;
        _isShelterPlacementModeActive = false;
        _selectedShelterType = null;
        SetGhostVisible(false);

        if (_placementModeShowedCandidateHighlights && !_candidateHighlightsWereShowingBeforePlacement)
            HideShelterCandidates();

        _placementModeShowedCandidateHighlights = false;
        _candidateHighlightsWereShowingBeforePlacement = false;

        if (wasActive)
        {
            ShelterPlacementModeChanged?.Invoke(false);

            if (debugLogs)
                Debug.Log("[ShelterCandidateController] Shelter placement mode disabled.");
        }
    }

    public IReadOnlyList<ShelterCandidateResult> GetShelterCandidates()
    {
        return _candidateResults;
    }

    public bool IsValidShelterCandidateTile(Vector3Int cell)
    {
        return TryFindValidCandidateAtCell(cell, out _) &&
               (shelterManager == null || shelterManager.CanPlaceShelterAtTile(cell));
    }

    public ShelterCandidateResult GetCandidateAtTile(Vector3Int cell)
    {
        return TryFindValidCandidateAtCell(cell, out ShelterCandidateResult candidate) ? candidate : null;
    }

    private void UpdateShelterPlacementMode()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitShelterPlacementMode();
            return;
        }

        if (!TryGetMouseTileCell(out Vector3Int hoveredCell))
        {
            SetGhostVisible(false);
            return;
        }

        bool canPlace = CanPlaceShelterOnCell(hoveredCell, out ShelterCandidateResult hoveredCandidate, out string invalidReason);
        UpdateGhostVisual(hoveredCell, hoveredCandidate, canPlace);

        if (!Input.GetMouseButtonDown(0) || Time.frameCount == _placementStartedFrame)
            return;

        if (!canPlace)
        {
            if (shelterManager != null && shelterManager.TryGetShelterAtTile(hoveredCell, out PlacedShelterData existingShelter))
            {
                Debug.Log("[ShelterCandidateController] Shelter already exists here.\n" + shelterManager.GetShelterInfoText(existingShelter));
            }
            else if (shelterManager != null && !shelterManager.CanPlaceMoreShelters())
            {
                Debug.LogWarning($"[ShelterManager] Shelter placement blocked. Max placed shelters reached: {shelterManager.GetMaxPlacedShelters()}.");
            }
            else
            {
                Debug.Log($"[ShelterCandidateController] Invalid placement attempt at {hoveredCell}: {invalidReason}");
            }

            return;
        }

        TryPlaceShelter(hoveredCandidate);
    }

    private bool TryPlaceShelter(ShelterCandidateResult candidate)
    {
        if (candidate == null || !candidate.isValid)
        {
            Debug.LogWarning("[ShelterCandidateController] Placement failed because the selected candidate is invalid.");
            return false;
        }

        if (shelterManager == null)
        {
            Debug.LogWarning("[ShelterCandidateController] Placement failed because ShelterManager is not assigned.");
            return false;
        }

        if (!shelterManager.CanPlaceMoreShelters())
        {
            Debug.LogWarning($"[ShelterManager] Shelter placement blocked. Max placed shelters reached: {shelterManager.GetMaxPlacedShelters()}.");
            return false;
        }

        if (_selectedShelterType == null && !shelterManager.TryGetDefaultShelterType(out _selectedShelterType))
        {
            Debug.LogWarning("[ShelterCandidateController] Placement failed because no shelter type is available.");
            return false;
        }

        Dictionary<string, int> populationByZone = BuildAssociatedZonePopulationLookup(candidate.associatedZoneGeoids);
        float shelterSafety = CalculateShelterSafety(candidate.tileCell);
        Sprite placedShelterSprite = GetSelectedShelterSprite();

        bool registered = shelterManager.RegisterShelter(
            _selectedShelterType,
            candidate.tileCell,
            candidate.worldPosition,
            candidate.associatedZoneGeoids,
            populationByZone,
            shelterSafety,
            out PlacedShelterData shelterData);

        if (!registered)
            return false;

        SpawnPlacedShelter(shelterData, placedShelterSprite);

        if (debugLogs)
        {
            Debug.Log($"[ShelterCandidateController] Valid shelter placed at {candidate.tileCell}.");
            Debug.Log("[ShelterCandidateController] " + shelterManager.GetShelterInfoText(shelterData));
        }

        if (_isShowingCandidates)
            ShowShelterCandidates();

        if (!keepPlacementModeActiveAfterPlacement)
            ExitShelterPlacementMode();

        return true;
    }

    private bool CanPlaceShelterOnCell(Vector3Int cell, out ShelterCandidateResult candidate, out string invalidReason)
    {
        candidate = null;
        invalidReason = "No valid shelter candidate exists at this tile.";

        if (!_hasCachedCandidates)
            RefreshShelterCandidates();

        if (shelterManager == null)
        {
            invalidReason = "ShelterManager is not assigned.";
            return false;
        }

        if (!shelterManager.CanPlaceMoreShelters())
        {
            invalidReason = $"Shelter placement limit reached: {shelterManager.GetActiveShelterCount()}/{shelterManager.GetMaxPlacedShelters()}.";
            return false;
        }

        if (!shelterManager.CanPlaceShelterAtTile(cell))
        {
            invalidReason = "A shelter already occupies this candidate tile.";
            return false;
        }

        if (!TryFindValidCandidateAtCell(cell, out candidate))
        {
            if (TryGetTileSnapshotFromCell(cell, out TileSnapshot snapshot))
            {
                if (snapshot.isWaterCategory || snapshot.waterHeight > deepFloodWaterThreshold)
                    invalidReason = "Shelters cannot be placed on water or deeply flooded tiles.";
                else if (!string.IsNullOrEmpty(snapshot.geoid) && !IsZoneEnabledForShelterPlacement(snapshot.geoid))
                    invalidReason = $"Zone '{snapshot.geoid}' is disabled for shelter placement.";
            }

            return false;
        }

        if (candidate.associatedZoneGeoids == null || candidate.associatedZoneGeoids.Count == 0)
        {
            invalidReason = "Shelter candidate has no associated zones.";
            return false;
        }

        invalidReason = null;
        return true;
    }

    private bool TryFindValidCandidateAtCell(Vector3Int cell, out ShelterCandidateResult candidate)
    {
        candidate = null;

        for (int i = 0; i < _candidateResults.Count; i++)
        {
            ShelterCandidateResult result = _candidateResults[i];

            if (result == null || !result.isValid || result.tileCell != cell)
                continue;

            candidate = result;
            return true;
        }

        return false;
    }

    private bool TryGetMouseTileCell(out Vector3Int cell)
    {
        cell = default;
        Tilemap placementTilemap = GetPlacementTilemap();

        if (mainCamera == null || placementTilemap == null)
            return false;

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        cell = placementTilemap.WorldToCell(world);
        return true;
    }

    private void UpdateGhostVisual(Vector3Int cell, ShelterCandidateResult candidate, bool isValidPlacement)
    {
        if (!EnsureGhostRenderer())
            return;

        Tilemap placementTilemap = GetPlacementTilemap();

        if (placementTilemap == null)
        {
            Debug.LogWarning("[ShelterCandidateController] Shelter ghost cannot follow cursor because no placement tilemap is assigned.");
            return;
        }

        Vector3 worldPosition = candidate != null && candidate.isValid
            ? candidate.worldPosition
            : placementTilemap.GetCellCenterWorld(cell);

        _runtimeGhostObject.transform.position = worldPosition + new Vector3(0f, ghostHeightOffset, 0f);
        PrepareGhostVisuals(isValidPlacement ? validPlacementColor : invalidPlacementColor);
        SetGhostVisible(true);
    }

    private bool EnsureGhostRenderer()
    {
        if (_runtimeGhostObject != null && _runtimeGhostRenderers != null && _runtimeGhostRenderers.Length > 0)
            return true;

        if (shelterGhostPrefab != null)
        {
            _runtimeGhostObject = Instantiate(shelterGhostPrefab, transform);
            _runtimeGhostObject.name = "ShelterPlacementGhost";

            if (debugLogs)
                Debug.Log("[ShelterCandidateController] Shelter ghost prefab instantiated.");
        }
        else if (shelterGhostRenderer != null)
        {
            _runtimeGhostRenderer = shelterGhostRenderer;
            _runtimeGhostObject = shelterGhostRenderer.gameObject;

            if (debugLogs)
                Debug.Log("[ShelterCandidateController] Using assigned scene SpriteRenderer for shelter ghost.");
        }
        else
        {
            Debug.LogWarning("[ShelterCandidateController] Missing shelter ghost prefab. Creating a runtime SpriteRenderer fallback.");
            _runtimeGhostObject = new GameObject("ShelterPlacementGhost");
            _runtimeGhostObject.transform.SetParent(transform, false);
            _runtimeGhostRenderer = _runtimeGhostObject.AddComponent<SpriteRenderer>();
        }

        if (_runtimeGhostObject == null)
        {
            Debug.LogWarning("[ShelterCandidateController] Shelter ghost GameObject could not be created.");
            return false;
        }

        _runtimeGhostRenderers = _runtimeGhostObject.GetComponentsInChildren<SpriteRenderer>(true);

        if ((_runtimeGhostRenderers == null || _runtimeGhostRenderers.Length == 0) && _runtimeGhostObject != null)
        {
            _runtimeGhostRenderer = _runtimeGhostObject.AddComponent<SpriteRenderer>();
            _runtimeGhostRenderers = new[] { _runtimeGhostRenderer };
            Debug.LogWarning("[ShelterCandidateController] Shelter ghost prefab had no SpriteRenderer. Added a fallback SpriteRenderer.");
        }

        _runtimeGhostRenderer = _runtimeGhostRenderers != null && _runtimeGhostRenderers.Length > 0
            ? _runtimeGhostRenderers[0]
            : null;

        if (_runtimeGhostRenderer == null)
        {
            Debug.LogWarning("[ShelterCandidateController] Shelter ghost requires a SpriteRenderer.");
            return false;
        }

        PrepareGhostVisuals(validPlacementColor);
        SetGhostVisible(false);
        return true;
    }

    private void ApplyGhostSprite(Sprite sprite)
    {
        if (_runtimeGhostRenderers == null || _runtimeGhostRenderers.Length == 0)
            return;

        Sprite safeSprite = sprite != null ? sprite : GetGeneratedFallbackShelterSprite();

        for (int i = 0; i < _runtimeGhostRenderers.Length; i++)
        {
            SpriteRenderer renderer = _runtimeGhostRenderers[i];

            if (renderer == null)
                continue;

            if (renderer.sprite == null)
                renderer.sprite = safeSprite;
        }
    }

    private void PrepareGhostVisuals(Color color)
    {
        if (_runtimeGhostObject == null)
            return;

        if (_runtimeGhostObject.transform.localScale.sqrMagnitude <= 0.0001f)
            _runtimeGhostObject.transform.localScale = Vector3.one;

        if (!_loggedMissingDefaultShelterSprite &&
            defaultShelterSprite == null &&
            (_selectedShelterType == null || _selectedShelterType.shelterSprite == null))
        {
            Debug.LogWarning("[ShelterCandidateController] No default shelter sprite assigned. A generated fallback sprite will be used for the shelter ghost.");
            _loggedMissingDefaultShelterSprite = true;
        }

        ApplyGhostSprite(GetSelectedShelterSprite());

        for (int i = 0; i < _runtimeGhostRenderers.Length; i++)
        {
            SpriteRenderer renderer = _runtimeGhostRenderers[i];

            if (renderer == null)
                continue;

            renderer.enabled = true;
            renderer.sortingOrder = ghostSortingOrder;
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Max(0.1f, color.a));
        }
    }

    private void SetGhostVisible(bool isVisible)
    {
        if (_runtimeGhostObject != null)
            _runtimeGhostObject.SetActive(isVisible);
    }

    private void SpawnPlacedShelter(PlacedShelterData shelterData, Sprite shelterSprite)
    {
        if (shelterData == null)
            return;

        GameObject shelterObject;

        if (placedShelterPrefab != null)
        {
            shelterObject = Instantiate(placedShelterPrefab, shelterData.worldPosition, Quaternion.identity, transform);
        }
        else
        {
            Debug.LogWarning("[ShelterCandidateController] Missing placed shelter prefab. Creating a runtime SpriteRenderer fallback for the placed shelter.");
            shelterObject = new GameObject($"PlacedShelter_{shelterData.shelterTypeName}_{shelterData.tileCell.x}_{shelterData.tileCell.y}");
            shelterObject.transform.SetParent(transform, false);
            shelterObject.transform.position = shelterData.worldPosition;
            shelterObject.AddComponent<SpriteRenderer>();
        }

        PreparePlacedShelterVisuals(shelterObject, shelterSprite);

        _placedShelterObjects.Add(shelterObject);
    }

    private void PreparePlacedShelterVisuals(GameObject shelterObject, Sprite shelterSprite)
    {
        if (shelterObject == null)
            return;

        shelterObject.SetActive(true);

        SpriteRenderer[] renderers = shelterObject.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            renderers = new[] { shelterObject.AddComponent<SpriteRenderer>() };
            Debug.LogWarning("[ShelterCandidateController] Placed shelter prefab had no SpriteRenderer. Added a fallback SpriteRenderer.");
        }

        Sprite safeSprite = shelterSprite != null ? shelterSprite : GetGeneratedFallbackShelterSprite();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (renderer.sprite == null)
                renderer.sprite = safeSprite;

            renderer.enabled = true;
            renderer.sortingOrder = ghostSortingOrder - 1;
            renderer.color = Color.white;
        }
    }

    private Sprite GetSelectedShelterSprite()
    {
        if (_selectedShelterType != null && _selectedShelterType.shelterSprite != null)
            return _selectedShelterType.shelterSprite;

        if (defaultShelterSprite != null)
            return defaultShelterSprite;

        return GetGeneratedFallbackShelterSprite();
    }

    private Sprite GetGeneratedFallbackShelterSprite()
    {
        if (_generatedFallbackShelterSprite != null)
            return _generatedFallbackShelterSprite;

        const int size = 16;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = "GeneratedShelterFallbackSprite";
        _generatedFallbackShelterSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _generatedFallbackShelterSprite;
    }

    private Dictionary<string, int> BuildAssociatedZonePopulationLookup(List<string> associatedZoneGeoids)
    {
        Dictionary<string, int> populationByZone = new();

        if (associatedZoneGeoids == null)
            return populationByZone;

        if (zoneBaselineRiskController != null)
            zoneBaselineRiskController.EnsureBaselineRiskCalculated();

        for (int i = 0; i < associatedZoneGeoids.Count; i++)
        {
            string geoid = NormalizeGeoid(associatedZoneGeoids[i]);

            if (string.IsNullOrEmpty(geoid) || populationByZone.ContainsKey(geoid))
                continue;

            int population = 0;

            if (zoneBaselineRiskController != null && zoneBaselineRiskController.TryGetRiskData(geoid, out ZoneBaselineRiskData riskData))
                population = Mathf.Max(0, riskData.rawPopulation);
            else
                population = GetFallbackZonePopulation(geoid);

            populationByZone.Add(geoid, population);
        }

        return populationByZone;
    }

    private int GetFallbackZonePopulation(string geoid)
    {
        if (floodDefense == null || tileMapData == null || !floodDefense.TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles))
            return 0;

        int maxPopulation = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            TileInstance tileInstance = tileMapData.Get(tile);

            if (tileInstance != null && tileInstance.population > maxPopulation)
                maxPopulation = tileInstance.population;
        }

        return Mathf.Max(0, maxPopulation);
    }

    private float CalculateShelterSafety(Vector3Int cell)
    {
        float fallbackSafety = shelterManager != null ? shelterManager.DefaultShelterSafety : 0.9f;

        if (!TryGetTileSnapshotFromCell(cell, out TileSnapshot snapshot))
            return fallbackSafety;

        if (snapshot.isWaterCategory)
            return 0f;

        GetElevationBounds(out float minElevation, out float maxElevation);

        float elevationRange = Mathf.Max(0.0001f, maxElevation - minElevation);
        float elevationScore = Mathf.Clamp01((snapshot.elevation - minElevation) / elevationRange);
        float waterSafety = 1f - Mathf.Clamp01(snapshot.waterHeight / Mathf.Max(0.01f, deepFloodWaterThreshold));

        return Mathf.Clamp01((0.65f + (0.35f * elevationScore)) * waterSafety);
    }

    private bool TryGetTileSnapshotFromCell(Vector3Int cell, out TileSnapshot snapshot)
    {
        snapshot = default;

        if (!TryCellToNormalizedTile(cell, out Vector2Int normalizedTile))
            return false;

        return TryGetTileSnapshot(normalizedTile, out snapshot);
    }

    private bool TryCellToNormalizedTile(Vector3Int cell, out Vector2Int normalizedTile)
    {
        normalizedTile = default;

        Tilemap placementTilemap = GetPlacementTilemap();

        if (placementTilemap == null || tileMapData == null)
            return false;

        BoundsInt bounds = placementTilemap.cellBounds;
        normalizedTile = new Vector2Int(cell.x - bounds.xMin, cell.y - bounds.yMin);
        return IsWithinMap(normalizedTile);
    }

    private void ResolveReferences()
    {
        if (floodDefense == null)
            floodDefense = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (jsonMapLoader == null)
            jsonMapLoader = FindFirstObjectByType<JsonMapLoader>();

        if (zoneOutlineHighlighter == null)
            zoneOutlineHighlighter = FindFirstObjectByType<ZoneThinOutlineByHover>();

        if (zoneBaselineRiskController == null)
            zoneBaselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (floodImpactController == null)
            floodImpactController = FindFirstObjectByType<FloodImpactController>();

        if (shelterManager == null)
            shelterManager = FindFirstObjectByType<ShelterManager>();

        if (terrainTilemap == null && floodDefense != null)
            terrainTilemap = floodDefense.TerrainTilemap;

        if (targetTilemap == null)
            targetTilemap = terrainTilemap;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (labelParent == null)
            labelParent = transform;
    }

    private bool ValidateReferences()
    {
        ResolveReferences();

        bool isValid = true;

        if (floodDefense == null)
        {
            Debug.LogError("[ShelterCandidateController] FloodDefenseBoxStamp is not assigned.");
            isValid = false;
        }

        if (jsonMapLoader == null)
        {
            Debug.LogError("[ShelterCandidateController] JsonMapLoader is not assigned.");
            isValid = false;
        }

        if (tileMapData == null)
        {
            Debug.LogError("[ShelterCandidateController] TileMapData is not assigned.");
            isValid = false;
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("[ShelterCandidateController] Terrain Tilemap is not assigned.");
            isValid = false;
        }

        if (GetPlacementTilemap() == null)
        {
            Debug.LogError("[ShelterCandidateController] Placement target Tilemap is not assigned.");
            isValid = false;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[ShelterCandidateController] Main Camera is not assigned.");
            isValid = false;
        }

        if (zoneOutlineHighlighter == null)
        {
            Debug.LogWarning("[ShelterCandidateController] ZoneThinOutlineByHover is not assigned. Shelter candidate highlights will not be shown.");
        }

        return isValid;
    }

    private Tilemap GetPlacementTilemap()
    {
        if (targetTilemap != null)
            return targetTilemap;

        if (terrainTilemap != null)
            return terrainTilemap;

        return floodDefense != null ? floodDefense.TerrainTilemap : null;
    }

    private bool IsMapReady()
    {
        if (floodDefense == null || jsonMapLoader == null || tileMapData == null)
            return false;

        if (jsonMapLoader.payload == null)
            return false;

        if (jsonMapLoader.cellToRC == null || jsonMapLoader.cellToRC.Count == 0)
            return false;

        if (jsonMapLoader.geoidGrid == null || jsonMapLoader.catGrid == null)
            return false;

        if (tileMapData.N <= 0)
            return false;

        return floodDefense.EnsureZoneIndexReadyForExternalUse();
    }

    private void BuildZoneEnabledLookup()
    {
        _zoneEnabledLookup.Clear();

        for (int i = 0; i < zoneOverrides.Count; i++)
        {
            ShelterZoneOverride zoneOverride = zoneOverrides[i];
            string geoid = NormalizeGeoid(zoneOverride != null ? zoneOverride.geoid : null);

            if (string.IsNullOrEmpty(geoid))
                continue;

            _zoneEnabledLookup[geoid] = zoneOverride.shelterPlacementEnabled;
        }
    }

    private bool TryBuildZoneContexts(List<ZoneContext> enabledZones, List<ZoneContext> disabledServableZones)
    {
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZones = floodDefense.GetAllZoneTiles();

        if (allZones == null || allZones.Count == 0)
            return false;

        foreach (KeyValuePair<string, HashSet<Vector2Int>> pair in allZones)
        {
            string geoid = NormalizeGeoid(pair.Key);

            if (string.IsNullOrEmpty(geoid) || pair.Value == null || pair.Value.Count == 0)
                continue;

            ZoneContext zone = new ZoneContext
            {
                geoid = geoid,
                tiles = pair.Value,
                isEnabled = IsZoneEnabledForShelterPlacement(geoid),
            };

            zone.normalizedCenter = CalculateNormalizedCenter(pair.Value);
            zone.hasWorldCenter = TryGetZoneCenterWorld(geoid, pair.Value, out zone.worldCenter);

            if (zone.isEnabled)
            {
                enabledZones.Add(zone);
            }
            else if (allowDisabledZonesToBeServedByNearbyShelters)
            {
                disabledServableZones.Add(zone);
            }
            else if (debugLogs)
            {
                Debug.Log($"[ShelterCandidateController] Skipping disabled zone '{geoid}' from shelter placement.");
            }
        }

        enabledZones.Sort(CompareZonesByCenter);
        disabledServableZones.Sort(CompareZonesByCenter);

        return enabledZones.Count > 0;
    }

    private List<ShelterGroup> BuildGroups(List<ZoneContext> enabledZones)
    {
        List<ShelterGroup> groups = new();
        List<ZoneContext> remaining = new(enabledZones);

        // Distance-based grouping keeps this lightweight on the low-resolution grid.
        // It can be swapped later for exact adjacency without changing the controller API.
        while (remaining.Count >= minZonesPerShelter)
        {
            List<ZoneContext> bestGroupMembers = null;
            float bestCompactness = float.MaxValue;
            int maxGroupSize = Mathf.Min(maxZonesPerShelter, remaining.Count);

            for (int groupSize = maxGroupSize; groupSize >= minZonesPerShelter; groupSize--)
            {
                List<ZoneContext> bestForSize = null;
                float bestForSizeCompactness = float.MaxValue;

                for (int zoneIndex = 0; zoneIndex < remaining.Count; zoneIndex++)
                {
                    ZoneContext seed = remaining[zoneIndex];
                    List<ZoneContext> candidateMembers = GetNearestZones(seed, remaining, groupSize);
                    float candidateCompactness = ComputeGroupCompactness(candidateMembers);

                    if (candidateCompactness + 0.001f < bestForSizeCompactness)
                    {
                        bestForSizeCompactness = candidateCompactness;
                        bestForSize = candidateMembers;
                    }
                }

                if (bestForSize != null)
                {
                    bestGroupMembers = bestForSize;
                    bestCompactness = bestForSizeCompactness;
                    break;
                }
            }

            if (bestGroupMembers == null)
                break;

            ShelterGroup group = new();
            group.hostZones.AddRange(bestGroupMembers);
            group.associatedZones.AddRange(bestGroupMembers);
            groups.Add(group);

            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                if (bestGroupMembers.Contains(remaining[i]))
                    remaining.RemoveAt(i);
            }

            if (debugLogs)
                Debug.Log($"[ShelterCandidateController] Built shelter group with {bestGroupMembers.Count} enabled zones. Compactness={bestCompactness:0.00}");
        }

        DistributeRemainingZones(remaining, groups);
        return groups;
    }

    private void DistributeRemainingZones(List<ZoneContext> remaining, List<ShelterGroup> groups)
    {
        if (remaining.Count == 0)
            return;

        List<ZoneContext> stillUnassigned = new();

        for (int i = 0; i < remaining.Count; i++)
        {
            ZoneContext zone = remaining[i];
            ShelterGroup nearestGroup = FindNearestGroupWithCapacity(zone, groups);

            if (nearestGroup == null)
            {
                stillUnassigned.Add(zone);
                continue;
            }

            nearestGroup.hostZones.Add(zone);
            nearestGroup.associatedZones.Add(zone);
        }

        if (stillUnassigned.Count == 0)
            return;

        if (!allowUndersizedFallbackGroups)
        {
            if (debugLogs)
                Debug.Log($"[ShelterCandidateController] Skipped {stillUnassigned.Count} leftover zones because undersized fallback groups are disabled.");

            return;
        }

        int startIndex = 0;

        while (startIndex < stillUnassigned.Count)
        {
            int remainingCount = stillUnassigned.Count - startIndex;
            int groupSize = Mathf.Min(maxZonesPerShelter, remainingCount);

            if (groupSize == 1 && !allowSingleZoneShelters)
            {
                if (debugLogs)
                    Debug.Log($"[ShelterCandidateController] Left zone '{stillUnassigned[startIndex].geoid}' without a shelter candidate because single-zone shelters are disabled.");

                startIndex++;
                continue;
            }

            ShelterGroup group = new();

            for (int i = 0; i < groupSize; i++)
            {
                ZoneContext zone = stillUnassigned[startIndex + i];
                group.hostZones.Add(zone);
                group.associatedZones.Add(zone);
            }

            groups.Add(group);
            startIndex += groupSize;
        }
    }

    private void AttachDisabledServedZones(List<ZoneContext> disabledServableZones, List<ShelterGroup> groups)
    {
        if (groups.Count == 0)
            return;

        for (int i = 0; i < disabledServableZones.Count; i++)
        {
            ZoneContext disabledZone = disabledServableZones[i];
            ShelterGroup group = FindNearestGroupWithCapacity(disabledZone, groups);

            if (group == null)
            {
                if (debugLogs)
                    Debug.Log($"[ShelterCandidateController] Disabled zone '{disabledZone.geoid}' could not be attached to a nearby shelter group.");

                continue;
            }

            group.associatedZones.Add(disabledZone);
        }
    }

    private ShelterGroup FindNearestGroupWithCapacity(ZoneContext zone, List<ShelterGroup> groups)
    {
        ShelterGroup bestGroup = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < groups.Count; i++)
        {
            ShelterGroup group = groups[i];

            if (group.associatedZones.Count >= maxZonesPerShelter)
                continue;

            Vector2 groupCenter = CalculateGroupCenter(group.associatedZones);
            float sqrDistance = (groupCenter - zone.normalizedCenter).sqrMagnitude;

            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                bestGroup = group;
            }
        }

        return bestGroup;
    }

    private List<ZoneContext> GetNearestZones(ZoneContext seed, List<ZoneContext> availableZones, int count)
    {
        List<ZoneContext> orderedZones = new(availableZones);
        orderedZones.Sort((a, b) => CompareDistanceToSeed(seed, a, b));

        if (orderedZones.Count > count)
            orderedZones.RemoveRange(count, orderedZones.Count - count);

        return orderedZones;
    }

    private int CompareDistanceToSeed(ZoneContext seed, ZoneContext a, ZoneContext b)
    {
        float aDistance = (a.normalizedCenter - seed.normalizedCenter).sqrMagnitude;
        float bDistance = (b.normalizedCenter - seed.normalizedCenter).sqrMagnitude;
        int distanceCompare = aDistance.CompareTo(bDistance);

        if (distanceCompare != 0)
            return distanceCompare;

        return string.CompareOrdinal(a.geoid, b.geoid);
    }

    private float ComputeGroupCompactness(List<ZoneContext> zones)
    {
        if (zones == null || zones.Count == 0)
            return float.MaxValue;

        Vector2 center = CalculateGroupCenter(zones);
        float totalDistance = 0f;

        for (int i = 0; i < zones.Count; i++)
            totalDistance += Vector2.Distance(zones[i].normalizedCenter, center);

        return totalDistance / zones.Count;
    }

    private ShelterCandidateResult BuildCandidateResult(
        int groupIndex,
        ShelterGroup group,
        int[,] supportDistanceMap,
        int[,] waterDistanceMap,
        float minElevation,
        float maxElevation)
    {
        ShelterCandidateResult result = new()
        {
            candidateId = $"shelter_candidate_{groupIndex + 1:00}",
        };

        if (group == null || group.hostZones.Count == 0)
        {
            result.isValid = false;
            result.debugReason = "No enabled host zones were available for this shelter group.";
            return result;
        }

        for (int i = 0; i < group.associatedZones.Count; i++)
            result.associatedZoneGeoids.Add(group.associatedZones[i].geoid);

        result.zoneCount = result.associatedZoneGeoids.Count;

        Vector2 groupCenter = CalculateGroupCenter(group.associatedZones);
        Vector2Int? bestTile = null;
        float bestScore = float.MinValue;
        string bestReason = "No suitable dry land tile was found.";

        HashSet<Vector2Int> searchedTiles = new();

        for (int i = 0; i < group.hostZones.Count; i++)
        {
            foreach (Vector2Int tile in group.hostZones[i].tiles)
            {
                if (!searchedTiles.Add(tile))
                    continue;

                EvaluateCandidateTile(tile, groupCenter, supportDistanceMap, waterDistanceMap, minElevation, maxElevation, ref bestTile, ref bestScore, ref bestReason);
            }
        }

        if (!bestTile.HasValue && fallbackSearchRadiusCells > 0)
        {
            Vector2Int centerTile = new(
                Mathf.RoundToInt(groupCenter.x),
                Mathf.RoundToInt(groupCenter.y));

            for (int offsetY = -fallbackSearchRadiusCells; offsetY <= fallbackSearchRadiusCells; offsetY++)
            {
                for (int offsetX = -fallbackSearchRadiusCells; offsetX <= fallbackSearchRadiusCells; offsetX++)
                {
                    Vector2Int tile = new(centerTile.x + offsetX, centerTile.y + offsetY);

                    if (!IsWithinMap(tile) || !searchedTiles.Add(tile))
                        continue;

                    EvaluateCandidateTile(tile, groupCenter, supportDistanceMap, waterDistanceMap, minElevation, maxElevation, ref bestTile, ref bestScore, ref bestReason);
                }
            }
        }

        if (!bestTile.HasValue)
        {
            result.isValid = false;
            result.suitabilityScore = bestScore;
            result.debugReason = bestReason;
            return result;
        }

        Vector3Int cell = floodDefense.NormalizedTileToCell(bestTile.Value);
        result.tileCell = cell;
        result.worldPosition = terrainTilemap.GetCellCenterWorld(cell);
        result.suitabilityScore = bestScore;
        result.isValid = true;
        result.debugReason = bestReason;
        return result;
    }

    private void EvaluateCandidateTile(
        Vector2Int tile,
        Vector2 groupCenter,
        int[,] supportDistanceMap,
        int[,] waterDistanceMap,
        float minElevation,
        float maxElevation,
        ref Vector2Int? bestTile,
        ref float bestScore,
        ref string bestReason)
    {
        if (!TryGetTileSnapshot(tile, out TileSnapshot snapshot))
            return;

        if (snapshot.isWaterCategory || snapshot.waterHeight > deepFloodWaterThreshold)
            return;

        if (!string.IsNullOrEmpty(snapshot.geoid) && !IsZoneEnabledForShelterPlacement(snapshot.geoid))
            return;

        float elevationRange = Mathf.Max(0.0001f, maxElevation - minElevation);
        float elevationScore = Mathf.Clamp01((snapshot.elevation - minElevation) / elevationRange);
        float centerDistance = Vector2.Distance(new Vector2(tile.x, tile.y), groupCenter);
        float centerScore = 1f - Mathf.Clamp01(centerDistance / centerPreferenceDistanceCells);
        float supportScore = ScoreDistanceMap(supportDistanceMap, tile, supportFeatureDistanceForMaxScore);
        float waterClearanceScore = ScoreDistanceMap(waterDistanceMap, tile, waterClearanceDistanceForMaxScore);
        float categoryScore = GetCategorySuitabilityScore(snapshot.category);

        float score =
            categoryScore +
            (elevationScore * elevationWeight) +
            (centerScore * centerWeight) +
            (supportScore * supportFeatureWeight) +
            (waterClearanceScore * waterClearanceWeight) -
            (Mathf.Max(0f, snapshot.waterHeight) * waterPenaltyWeight);

        if (score <= bestScore)
            return;

        bestTile = tile;
        bestScore = score;
        bestReason = $"Category={snapshot.category}, Elev={snapshot.elevation:0.##}, Water={snapshot.waterHeight:0.##}, CenterScore={centerScore:0.##}";
    }

    private bool TryGetTileSnapshot(Vector2Int tile, out TileSnapshot snapshot)
    {
        snapshot = default;

        if (!IsWithinMap(tile))
            return false;

        TileInstance tileInstance = tileMapData.Get(tile);
        Vector2Int rc = default;

        if (tileInstance == null && !TryGetRowColumn(tile, out rc))
            return false;

        snapshot.category = tileInstance != null ? tileInstance.category : jsonMapLoader.catGrid[rc.x, rc.y];
        snapshot.geoid = tileInstance != null ? NormalizeGeoid(tileInstance.geoid) : NormalizeGeoid(jsonMapLoader.geoidGrid[rc.x, rc.y]);
        snapshot.elevation = tileInstance != null ? tileInstance.elevation : 0f;
        snapshot.waterHeight = tileInstance != null ? tileInstance.waterHeight : 0f;
        snapshot.isWaterCategory = IsWaterCategory(snapshot.category) || (tileInstance != null && tileInstance.tileType != null && tileInstance.tileType.isWater);
        return true;
    }

    private int[,] BuildDistanceMap(int mapSize, Func<Vector2Int, bool> isSourceTile)
    {
        int[,] distanceMap = new int[mapSize, mapSize];
        Queue<Vector2Int> frontier = new();

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                distanceMap[x, y] = -1;
                Vector2Int tile = new(x, y);

                if (!isSourceTile(tile))
                    continue;

                distanceMap[x, y] = 0;
                frontier.Enqueue(tile);
            }
        }

        Vector2Int[] directions =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            int nextDistance = distanceMap[current.x, current.y] + 1;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];

                if (!IsWithinMap(next) || distanceMap[next.x, next.y] >= 0)
                    continue;

                distanceMap[next.x, next.y] = nextDistance;
                frontier.Enqueue(next);
            }
        }

        return distanceMap;
    }

    private bool IsSupportFeatureTile(Vector2Int tile)
    {
        if (!TryGetTileSnapshot(tile, out TileSnapshot snapshot))
            return false;

        string category = snapshot.category;

        if (string.IsNullOrEmpty(category))
            return false;

        switch (category.ToLowerInvariant())
        {
            case "road":
            case "highway":
            case "rail":
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

    private bool IsWaterTile(Vector2Int tile)
    {
        if (!TryGetTileSnapshot(tile, out TileSnapshot snapshot))
            return false;

        return snapshot.isWaterCategory;
    }

    private float ScoreDistanceMap(int[,] distanceMap, Vector2Int tile, int maxDistanceForFullScore)
    {
        if (distanceMap == null || !IsWithinMap(tile))
            return 0f;

        int distance = distanceMap[tile.x, tile.y];

        if (distance < 0)
            return 0f;

        return 1f - Mathf.Clamp01((float)distance / Mathf.Max(1, maxDistanceForFullScore));
    }

    private float GetCategorySuitabilityScore(string category)
    {
        if (string.IsNullOrEmpty(category))
            return 0.1f;

        switch (category.ToLowerInvariant())
        {
            case "building":
            case "industrial":
            case "residential":
            case "commercial":
            case "city":
                return 0.55f;

            case "land":
            case "park":
            case "forest":
            case "mountain":
            case "beach":
                return 0.35f;

            case "road":
            case "highway":
            case "rail":
                return 0.15f;

            default:
                return 0.2f;
        }
    }

    private bool TryGetZoneCenterWorld(string geoid, HashSet<Vector2Int> zoneTiles, out Vector3 worldCenter)
    {
        if (zoneBaselineRiskController != null && zoneBaselineRiskController.TryGetZoneCenterWorld(geoid, out worldCenter))
            return true;

        worldCenter = Vector3.zero;

        if (zoneTiles == null || zoneTiles.Count == 0 || floodDefense == null || terrainTilemap == null)
            return false;

        Vector3 sum = Vector3.zero;
        int sampleCount = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            Vector3Int cell = floodDefense.NormalizedTileToCell(tile);
            sum += terrainTilemap.GetCellCenterWorld(cell);
            sampleCount++;
        }

        if (sampleCount == 0)
            return false;

        worldCenter = sum / sampleCount;
        return true;
    }

    private bool TryGetRowColumn(Vector2Int normalizedTile, out Vector2Int rc)
    {
        rc = default;

        if (floodDefense == null || jsonMapLoader == null || jsonMapLoader.cellToRC == null)
            return false;

        Vector3Int cell = floodDefense.NormalizedTileToCell(normalizedTile);
        return jsonMapLoader.cellToRC.TryGetValue(cell, out rc);
    }

    private void GetElevationBounds(out float minElevation, out float maxElevation)
    {
        minElevation = float.MaxValue;
        maxElevation = float.MinValue;

        for (int y = 0; y < tileMapData.N; y++)
        {
            for (int x = 0; x < tileMapData.N; x++)
            {
                TileInstance tile = tileMapData.Get(new Vector2Int(x, y));

                if (tile == null)
                    continue;

                if (tile.elevation < minElevation)
                    minElevation = tile.elevation;

                if (tile.elevation > maxElevation)
                    maxElevation = tile.elevation;
            }
        }

        if (minElevation == float.MaxValue || maxElevation == float.MinValue)
        {
            minElevation = 0f;
            maxElevation = 1f;
            return;
        }

        if (Mathf.Approximately(minElevation, maxElevation))
            maxElevation = minElevation + 1f;
    }

    private void SpawnCandidateLabel(ShelterCandidateResult result)
    {
        Transform parent = labelParent != null ? labelParent : transform;
        GameObject labelObject = new GameObject($"ShelterCandidateLabel_{result.candidateId}");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = result.worldPosition + new Vector3(0f, labelHeightOffset, 0f);

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            if (labelObject.transform.position.z <= mainCamera.transform.position.z)
            {
                Vector3 position = labelObject.transform.position;
                position.z = mainCamera.transform.position.z + 1f;
                labelObject.transform.position = position;
            }

            labelObject.transform.rotation = mainCamera.transform.rotation;
        }

        TextMeshPro textLabel = labelObject.AddComponent<TextMeshPro>();

        if (labelFontAsset != null)
            textLabel.font = labelFontAsset;

        textLabel.alignment = TextAlignmentOptions.Center;
        textLabel.fontSize = labelFontSize;
        textLabel.enableWordWrapping = false;
        textLabel.overflowMode = TextOverflowModes.Overflow;
        textLabel.color = labelColor;
        textLabel.text = $"Shelter Candidate\nServes {result.zoneCount} zones";

        Renderer textRenderer = textLabel.GetComponent<Renderer>();

        if (textRenderer != null)
            textRenderer.sortingOrder = labelSortingOrder;

        _spawnedLabels.Add(labelObject);
    }

    private void ClearVisuals()
    {
        if (zoneOutlineHighlighter != null)
            zoneOutlineHighlighter.ClearShelterCandidateHighlights();

        for (int i = 0; i < _spawnedLabels.Count; i++)
        {
            if (_spawnedLabels[i] != null)
                Destroy(_spawnedLabels[i]);
        }

        _spawnedLabels.Clear();
    }

    private bool IsZoneEnabledForShelterPlacement(string geoid)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return false;

        if (_zoneEnabledLookup.TryGetValue(geoid, out bool isEnabled))
            return isEnabled;

        return defaultZoneEnabled;
    }

    private bool IsWithinMap(Vector2Int tile)
    {
        return tile.x >= 0 &&
               tile.y >= 0 &&
               tile.x < tileMapData.N &&
               tile.y < tileMapData.N;
    }

    private bool IsWaterCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return false;

        string normalized = category.ToLowerInvariant();

        for (int i = 0; i < waterCategoryKeywords.Count; i++)
        {
            string keyword = waterCategoryKeywords[i];

            if (!string.IsNullOrEmpty(keyword) && normalized.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static Vector2 CalculateNormalizedCenter(HashSet<Vector2Int> zoneTiles)
    {
        if (zoneTiles == null || zoneTiles.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        int count = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            sum += new Vector2(tile.x, tile.y);
            count++;
        }

        return count > 0 ? sum / count : Vector2.zero;
    }

    private static Vector2 CalculateGroupCenter(List<ZoneContext> zones)
    {
        if (zones == null || zones.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;

        for (int i = 0; i < zones.Count; i++)
            sum += zones[i].normalizedCenter;

        return sum / zones.Count;
    }

    private static int CompareZonesByCenter(ZoneContext a, ZoneContext b)
    {
        int yCompare = a.normalizedCenter.y.CompareTo(b.normalizedCenter.y);

        if (yCompare != 0)
            return yCompare;

        int xCompare = a.normalizedCenter.x.CompareTo(b.normalizedCenter.x);

        if (xCompare != 0)
            return xCompare;

        return string.CompareOrdinal(a.geoid, b.geoid);
    }

    private static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? null : geoid.Trim();
    }

    private struct TileSnapshot
    {
        public string geoid;
        public string category;
        public float elevation;
        public float waterHeight;
        public bool isWaterCategory;
    }
}
