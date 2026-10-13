using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Staged public controller for the Dev Shelter refactor. It coordinates state,
/// candidate resolution, placement rules, metrics, presentation, and input seams.
/// </summary>
public class ShelterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloodDefenseBoxStamp floodDefense;
    [SerializeField] private JsonMapLoader jsonMapLoader;
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ZoneBaselineRiskController zoneBaselineRiskController;
    [SerializeField] private ShelterPresentation presentation;
    [SerializeField] private ShelterPlacementInput placementInput;

    [Header("Shelter Types")]
    [SerializeField] private List<ShelterTypeDefinition> shelterTypes = new();

    [Header("Placement Rules")]
    [SerializeField] private bool allowMultipleSheltersPerCandidate;
    [SerializeField] private bool limitPlacedShelters = true;
    [SerializeField, Min(1)] private int maxPlacedShelters = 3;

    [Header("Mitigation Defaults")]
    [SerializeField, Range(0f, 1f)] private float defaultRouteAccessibility = 1.0f;
    [SerializeField, Range(0f, 1f)] private float defaultShelterSafety = 0.9f;

    [Header("Candidate Resolution")]
    [SerializeField] private ShelterCandidateSettings candidateSettings = new();

    [Header("Placement UX")]
    [SerializeField] private bool keepPlacementModeActiveAfterPlacement;
    [SerializeField] private bool showCandidateHighlightsDuringPlacement = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly ShelterState _state = new();
    private readonly ShelterCandidateResolver _candidateResolver = new();
    private readonly List<ShelterCandidateResult> _candidateResults = new();

    private ShelterPlacement _placement;
    private bool _hasCachedCandidates;
    private bool _isShowingCandidates;
    private bool _isShelterPlacementModeActive;
    private bool _placementModeShowedCandidateHighlights;
    private bool _candidateHighlightsWereShowingBeforePlacement;
    private int _placementStartedFrame = -1;
    private ShelterTypeDefinition _selectedShelterType;

    public event Action OnSheltersChanged;
    public event Action<PlacedShelterData> OnShelterPlaced;
    public event Action<bool> ShelterPlacementModeChanged;

    public bool IsShelterCandidateModeActive => _isShowingCandidates;
    public bool IsShelterPlacementModeActive => _isShelterPlacementModeActive;
    public bool HasConfiguredShelterPlacementVisuals =>
        presentation != null && presentation.HasConfiguredShelterPlacementVisuals;
    public IReadOnlyList<ShelterTypeDefinition> ShelterTypes => shelterTypes;
    public bool AllowMultipleSheltersPerCandidate => allowMultipleSheltersPerCandidate;
    public float DefaultRouteAccessibility => defaultRouteAccessibility;
    public float DefaultShelterSafety => defaultShelterSafety;
    public Camera MainCamera => mainCamera;
    public Tilemap PlacementTilemap => GetPlacementTilemap();

    private void Awake()
    {
        EnsureServices();
        ResolveReferences();
        EnsureDefaultShelterTypes();
    }

    private void OnEnable()
    {
        EnsureServices();
        ResolveReferences();
        RefreshPlacedShelterZoneOutlines();
    }

    private void OnDisable()
    {
        ExitShelterPlacementMode();
        HideShelterCandidates();
        presentation?.ClearPlacedShelterZoneOutlines();
    }

    private void OnValidate()
    {
        defaultRouteAccessibility = Mathf.Clamp01(defaultRouteAccessibility);
        defaultShelterSafety = Mathf.Clamp01(defaultShelterSafety);
        maxPlacedShelters = Mathf.Max(1, maxPlacedShelters);

        if (candidateSettings == null)
            candidateSettings = new ShelterCandidateSettings();

        candidateSettings.Validate();
        EnsureDefaultShelterTypes();
    }

    [ContextMenu("Refresh Shelter Candidates")]
    public void RefreshShelterCandidates()
    {
        EnsureServices();
        ResolveReferences();
        _candidateResults.Clear();
        _hasCachedCandidates = false;

        if (!ValidateReferences())
            return;

        ShelterCandidateContext context = BuildCandidateContext();
        if (!_candidateResolver.RefreshCandidates(context, candidateSettings, _candidateResults))
        {
            Debug.LogWarning("[ShelterController] Shelter candidates could not be refreshed from the current map data.");
            return;
        }

        int validCount = 0;

        for (int i = 0; i < _candidateResults.Count; i++)
        {
            if (_candidateResults[i] != null && _candidateResults[i].isValid)
                validCount++;
        }

        _hasCachedCandidates = true;

        if (debugLogs)
        {
            Debug.Log(
                $"[ShelterController] Refreshed {validCount} valid shelter candidate groups from map zones.");
        }

        if (_isShowingCandidates)
            ShowShelterCandidates();
    }

    [ContextMenu("Show Shelter Candidates")]
    public void ShowShelterCandidates()
    {
        if (!_hasCachedCandidates)
            RefreshShelterCandidates();

        if (_candidateResults.Count == 0)
        {
            if (debugLogs)
                Debug.Log("[ShelterController] No shelter candidates are cached to show.");

            _isShowingCandidates = false;
            return;
        }

        if (presentation == null)
        {
            Debug.LogWarning("[ShelterController] Shelter presentation is not assigned, so candidates cannot be shown.");
            _isShowingCandidates = false;
            return;
        }

        presentation.ShowShelterCandidates(
            _candidateResults,
            CanPlaceShelterAtTile,
            IsZoneEnabledForShelterPlacement);

        _isShowingCandidates = HasVisibleCandidates();
    }

    [ContextMenu("Hide Shelter Candidates")]
    public void HideShelterCandidates()
    {
        presentation?.ClearCandidateVisuals();
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
        EnsureServices();

        if (!ValidateReferences())
            return;

        if (!TryGetDefaultShelterType(out _selectedShelterType))
        {
            Debug.LogWarning("[ShelterController] Shelter placement mode cannot start because no shelter type is available.");
            return;
        }

        if (!_hasCachedCandidates)
            RefreshShelterCandidates();

        if (_candidateResults.Count == 0)
        {
            Debug.LogWarning("[ShelterController] Shelter placement mode cannot start because no shelter candidates are available.");
            return;
        }

        if (floodDefense != null && floodDefense.IsZoneBoundaryModeActive)
            floodDefense.ExitBuildModeFromUI();

        _candidateHighlightsWereShowingBeforePlacement = _isShowingCandidates;
        _placementModeShowedCandidateHighlights =
            showCandidateHighlightsDuringPlacement && !_candidateHighlightsWereShowingBeforePlacement;

        if (showCandidateHighlightsDuringPlacement)
            ShowShelterCandidates();

        _isShelterPlacementModeActive = true;
        _placementStartedFrame = Time.frameCount;
        placementInput?.ResolveReferences();

        bool hasGhostVisual = presentation != null && presentation.PrepareGhost(_selectedShelterType);
        ShelterPlacementModeChanged?.Invoke(true);

        if (debugLogs)
        {
            Debug.Log($"[ShelterController] Shelter placement mode enabled with default type '{_selectedShelterType.shelterTypeName}'.");
            Debug.Log(hasGhostVisual
                ? "[ShelterController] Shelter ghost visual prepared and activated."
                : "[ShelterController] Shelter placement mode is active, but the ghost visual could not be prepared.");
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
        presentation?.SetGhostVisible(false);

        if (_placementModeShowedCandidateHighlights && !_candidateHighlightsWereShowingBeforePlacement)
            HideShelterCandidates();

        _placementModeShowedCandidateHighlights = false;
        _candidateHighlightsWereShowingBeforePlacement = false;

        if (wasActive)
        {
            ShelterPlacementModeChanged?.Invoke(false);

            if (debugLogs)
                Debug.Log("[ShelterController] Shelter placement mode disabled.");
        }
    }

    public void HandlePlacementPointerUnavailable()
    {
        if (!_isShelterPlacementModeActive)
            return;

        presentation?.SetGhostVisible(false);
    }

    public void HandlePlacementHover(Vector3Int hoveredCell)
    {
        if (!_isShelterPlacementModeActive)
            return;

        bool canPlace = CanPlaceShelterOnCell(
            hoveredCell,
            out ShelterCandidateResult hoveredCandidate,
            out _);

        presentation?.UpdateGhostVisual(
            hoveredCell,
            hoveredCandidate,
            canPlace,
            GetPlacementTilemap(),
            _selectedShelterType);
    }

    public void HandlePlacementClick(Vector3Int hoveredCell)
    {
        if (!_isShelterPlacementModeActive || Time.frameCount == _placementStartedFrame)
            return;

        bool canPlace = CanPlaceShelterOnCell(
            hoveredCell,
            out ShelterCandidateResult hoveredCandidate,
            out string invalidReason);

        if (!canPlace)
        {
            if (TryGetShelterAtTile(hoveredCell, out PlacedShelterData existingShelter))
            {
                Debug.Log("[ShelterController] Shelter already exists here.\n" + GetShelterInfoText(existingShelter));
            }
            else if (!CanPlaceMoreShelters())
            {
                Debug.LogWarning($"[ShelterController] Shelter placement blocked. Max placed shelters reached: {GetMaxPlacedShelters()}.");
            }
            else
            {
                Debug.Log($"[ShelterController] Invalid placement attempt at {hoveredCell}: {invalidReason}");
            }

            return;
        }

        TryPlaceShelter(hoveredCandidate);
    }

    public IReadOnlyList<ShelterCandidateResult> GetShelterCandidates()
    {
        return _candidateResults;
    }

    public ShelterCandidateResult GetCandidateAtTile(Vector3Int cell)
    {
        return _candidateResolver.TryFindValidCandidateAtCell(_candidateResults, cell, out ShelterCandidateResult candidate)
            ? candidate
            : null;
    }

    public bool IsValidShelterCandidateTile(Vector3Int cell)
    {
        return _candidateResolver.TryFindValidCandidateAtCell(_candidateResults, cell, out _) &&
               CanPlaceShelterAtTile(cell);
    }

    public bool TryGetDefaultShelterType(out ShelterTypeDefinition shelterType)
    {
        EnsureDefaultShelterTypes();

        shelterType = shelterTypes.Count > 0 ? shelterTypes[0] : null;
        return shelterType != null;
    }

    public IReadOnlyList<PlacedShelterData> GetAllPlacedShelters()
    {
        return _state.PlacedShelters;
    }

    public List<PlacedShelterData> GetActiveShelters()
    {
        return _state.GetActiveShelters();
    }

    public bool HasActiveShelters()
    {
        return _state.HasActiveShelters();
    }

    public bool CanPlaceMoreShelters()
    {
        EnsureServices();
        return _placement.CanPlaceMoreShelters(limitPlacedShelters, maxPlacedShelters);
    }

    public int GetActiveShelterCount()
    {
        return _state.GetActiveShelterCount();
    }

    public int GetMaxPlacedShelters()
    {
        return maxPlacedShelters;
    }

    public bool HasShelterAtTile(Vector3Int tileCell)
    {
        return _state.HasShelterAtTile(tileCell);
    }

    public bool CanPlaceShelterAtTile(Vector3Int tileCell)
    {
        EnsureServices();
        return _placement.CanPlaceShelterAtTile(tileCell, allowMultipleSheltersPerCandidate);
    }

    public bool TryGetShelterAtTile(Vector3Int tileCell, out PlacedShelterData shelter)
    {
        return _state.TryGetShelterAtTile(tileCell, out shelter);
    }

    public bool TryGetActiveShelterForZone(string geoid, out PlacedShelterData shelter)
    {
        return _state.TryGetActiveShelterForZone(geoid, out shelter);
    }

    public bool RegisterShelter(
        ShelterTypeDefinition shelterType,
        Vector3Int tileCell,
        Vector3 worldPosition,
        IList<string> associatedZoneGeoids,
        IDictionary<string, int> associatedZonePopulations,
        float candidateTileSafety,
        out PlacedShelterData shelterData)
    {
        EnsureServices();
        bool registered = _placement.TryRegisterShelter(
            shelterType,
            tileCell,
            worldPosition,
            associatedZoneGeoids,
            associatedZonePopulations,
            candidateTileSafety,
            defaultRouteAccessibility,
            allowMultipleSheltersPerCandidate,
            limitPlacedShelters,
            maxPlacedShelters,
            out shelterData,
            out string failureReason);

        if (!registered)
        {
            Debug.LogWarning($"[ShelterController] Cannot register shelter: {failureReason}");
            return false;
        }

        RaiseShelterChangedEvents(shelterData);
        return true;
    }

    public float GetShelterMitigationForZone(string geoid)
    {
        return _state.GetShelterMitigationForZone(geoid);
    }

    public int GetProtectedPopulationForZone(string geoid)
    {
        return _state.GetProtectedPopulationForZone(geoid);
    }

    public string GetShelterInfoText(PlacedShelterData shelter)
    {
        return ShelterMetrics.GetShelterInfoText(shelter);
    }

    public void RefreshPlacedShelterZoneOutlines()
    {
        presentation?.RefreshPlacedShelterZoneOutlines(GetActiveShelters(), IsZoneEnabledForShelterPlacement);
    }

    private bool TryPlaceShelter(ShelterCandidateResult candidate)
    {
        if (candidate == null || !candidate.isValid)
        {
            Debug.LogWarning("[ShelterController] Placement failed because the selected candidate is invalid.");
            return false;
        }

        if (!CanPlaceMoreShelters())
        {
            Debug.LogWarning($"[ShelterController] Shelter placement blocked. Max placed shelters reached: {GetMaxPlacedShelters()}.");
            return false;
        }

        if (_selectedShelterType == null && !TryGetDefaultShelterType(out _selectedShelterType))
        {
            Debug.LogWarning("[ShelterController] Placement failed because no shelter type is available.");
            return false;
        }

        Dictionary<string, int> populationByZone = BuildAssociatedZonePopulationLookup(candidate.associatedZoneGeoids);
        float shelterSafety = CalculateShelterSafety(candidate.tileCell);

        bool registered = _placement.TryRegisterShelter(
            _selectedShelterType,
            candidate.tileCell,
            candidate.worldPosition,
            candidate.associatedZoneGeoids,
            populationByZone,
            shelterSafety,
            defaultRouteAccessibility,
            allowMultipleSheltersPerCandidate,
            limitPlacedShelters,
            maxPlacedShelters,
            out PlacedShelterData shelterData,
            out string failureReason);

        if (!registered)
        {
            Debug.LogWarning($"[ShelterController] Placement failed: {failureReason}");
            return false;
        }

        RaiseShelterChangedEvents(shelterData);
        presentation?.SpawnPlacedShelter(shelterData, _selectedShelterType);

        if (debugLogs)
        {
            Debug.Log($"[ShelterController] Valid shelter placed at {candidate.tileCell}.");
            Debug.Log("[ShelterController] " + GetShelterInfoText(shelterData));
        }

        if (_isShowingCandidates)
            ShowShelterCandidates();

        RefreshPlacedShelterZoneOutlines();

        if (!keepPlacementModeActiveAfterPlacement)
            ExitShelterPlacementMode();

        return true;
    }

    private bool CanPlaceShelterOnCell(
        Vector3Int cell,
        out ShelterCandidateResult candidate,
        out string invalidReason)
    {
        candidate = null;
        invalidReason = "No valid shelter candidate exists at this tile.";

        if (!_hasCachedCandidates)
            RefreshShelterCandidates();

        if (!CanPlaceMoreShelters())
        {
            invalidReason = $"Shelter placement limit reached: {GetActiveShelterCount()}/{GetMaxPlacedShelters()}.";
            return false;
        }

        if (!CanPlaceShelterAtTile(cell))
        {
            invalidReason = "A shelter already occupies this candidate tile.";
            return false;
        }

        if (!_candidateResolver.TryFindValidCandidateAtCell(_candidateResults, cell, out candidate))
        {
            if (_candidateResolver.TryGetTileSnapshotFromCell(cell, out ShelterTileSnapshot snapshot))
            {
                if (snapshot.isWaterCategory || snapshot.waterHeight > candidateSettings.deepFloodWaterThreshold)
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

    private Dictionary<string, int> BuildAssociatedZonePopulationLookup(List<string> associatedZoneGeoids)
    {
        Dictionary<string, int> populationByZone = new();

        if (associatedZoneGeoids == null)
            return populationByZone;

        if (zoneBaselineRiskController != null)
            zoneBaselineRiskController.EnsureBaselineRiskCalculated();

        for (int i = 0; i < associatedZoneGeoids.Count; i++)
        {
            string geoid = ShelterMetrics.NormalizeGeoid(associatedZoneGeoids[i]);

            if (string.IsNullOrEmpty(geoid) || populationByZone.ContainsKey(geoid))
                continue;

            int population = 0;

            if (zoneBaselineRiskController != null &&
                zoneBaselineRiskController.TryGetRiskData(geoid, out ZoneBaselineRiskData riskData))
            {
                population = Mathf.Max(0, riskData.rawPopulation);
            }
            else
            {
                population = GetFallbackZonePopulation(geoid);
            }

            populationByZone.Add(geoid, population);
        }

        return populationByZone;
    }

    private int GetFallbackZonePopulation(string geoid)
    {
        if (floodDefense == null ||
            tileMapData == null ||
            !floodDefense.TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles))
        {
            return 0;
        }

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
        if (!_candidateResolver.TryGetTileSnapshotFromCell(cell, out ShelterTileSnapshot snapshot))
            return defaultShelterSafety;

        _candidateResolver.GetElevationBounds(out float minElevation, out float maxElevation);

        return ShelterMetrics.CalculateShelterSafety(
            snapshot,
            minElevation,
            maxElevation,
            candidateSettings.deepFloodWaterThreshold);
    }

    private bool IsZoneEnabledForShelterPlacement(string geoid)
    {
        return _candidateResolver.IsZoneEnabledForShelterPlacement(geoid);
    }

    private bool HasVisibleCandidates()
    {
        for (int i = 0; i < _candidateResults.Count; i++)
        {
            ShelterCandidateResult candidate = _candidateResults[i];

            if (candidate != null && candidate.isValid && CanPlaceShelterAtTile(candidate.tileCell))
                return true;
        }

        return false;
    }

    private void RaiseShelterChangedEvents(PlacedShelterData shelterData)
    {
        OnShelterPlaced?.Invoke(shelterData);
        OnSheltersChanged?.Invoke();
    }

    private ShelterCandidateContext BuildCandidateContext()
    {
        return new ShelterCandidateContext
        {
            FloodDefense = floodDefense,
            JsonMapLoader = jsonMapLoader,
            TileMapData = tileMapData,
            TerrainTilemap = terrainTilemap,
            TargetTilemap = targetTilemap,
            ZoneBaselineRiskController = zoneBaselineRiskController,
            DebugLogs = debugLogs,
        };
    }

    private void EnsureServices()
    {
        if (_placement == null)
            _placement = new ShelterPlacement(_state);

        if (candidateSettings == null)
            candidateSettings = new ShelterCandidateSettings();
    }

    private void ResolveReferences()
    {
        if (floodDefense == null)
            floodDefense = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (jsonMapLoader == null)
            jsonMapLoader = FindFirstObjectByType<JsonMapLoader>();

        if (zoneBaselineRiskController == null)
            zoneBaselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (terrainTilemap == null && floodDefense != null)
            terrainTilemap = floodDefense.TerrainTilemap;

        if (targetTilemap == null)
            targetTilemap = terrainTilemap;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (presentation == null)
            presentation = GetComponent<ShelterPresentation>() ?? FindFirstObjectByType<ShelterPresentation>();

        if (placementInput == null)
            placementInput = GetComponent<ShelterPlacementInput>() ?? FindFirstObjectByType<ShelterPlacementInput>();

        presentation?.ResolveReferences();
    }

    private bool ValidateReferences()
    {
        ResolveReferences();

        bool isValid = true;

        if (floodDefense == null)
        {
            Debug.LogError("[ShelterController] FloodDefenseBoxStamp is not assigned.");
            isValid = false;
        }

        if (jsonMapLoader == null)
        {
            Debug.LogError("[ShelterController] JsonMapLoader is not assigned.");
            isValid = false;
        }

        if (tileMapData == null)
        {
            Debug.LogError("[ShelterController] TileMapData is not assigned.");
            isValid = false;
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("[ShelterController] Terrain Tilemap is not assigned.");
            isValid = false;
        }

        if (GetPlacementTilemap() == null)
        {
            Debug.LogError("[ShelterController] Placement target Tilemap is not assigned.");
            isValid = false;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[ShelterController] Main Camera is not assigned.");
            isValid = false;
        }

        if (presentation == null)
            Debug.LogWarning("[ShelterController] ShelterPresentation is not assigned. Shelter visuals will not be shown.");

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

    private void EnsureDefaultShelterTypes()
    {
        if (shelterTypes == null)
            shelterTypes = new List<ShelterTypeDefinition>();

        if (shelterTypes.Count > 0)
            return;

        shelterTypes.Add(new ShelterTypeDefinition
        {
            shelterTypeName = "Community Shelter",
            capacity = 250,
            cost = 100,
            safetyMultiplier = 1f,
            description = "General-purpose emergency shelter."
        });
        shelterTypes.Add(new ShelterTypeDefinition
        {
            shelterTypeName = "Storm Shelter",
            capacity = 150,
            cost = 150,
            safetyMultiplier = 1f,
            description = "Smaller high-safety shelter option."
        });
        shelterTypes.Add(new ShelterTypeDefinition
        {
            shelterTypeName = "School Shelter",
            capacity = 400,
            cost = 200,
            safetyMultiplier = 1f,
            description = "Large-capacity shelter option."
        });
    }
}
