using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum PopulationAggregationMode
{
    SumPerTile,
    MaxPerZone,
    FirstNonZero,
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical,
}

[Serializable]
public struct ZoneBaselineRiskData
{
    public string geoid;
    public int rawPopulation;
    public int tileCount;
    public float averageElevation;
    public float minimumElevation;
    public float distanceToWaterCells;
    public float distanceRisk;
    public float elevationRisk;
    public float populationRisk;
    public float baselineRiskScore;
    public float originalBaselineRiskScore;
    public float adjustedBaselineRiskScore;
    public float originalPopulationRisk;
    public float adjustedPopulationRisk;
    public float shelterMitigationApplied;
    public float evacuationMitigationApplied;
    public float combinedPopulationMitigation;
    public int estimatedPeopleProtected;
    public int estimatedPeopleEvacuated;
    public RiskLevel riskLevel;
    public RiskLevel adjustedRiskLevel;
    public bool hasWorldCenter;
    public Vector3 worldCenter;
}

public class ZoneBaselineRiskController : MonoBehaviour
{
    private sealed class ZoneComputationState
    {
        public int rawPopulation;
        public int tileCount;
        public HashSet<Vector2Int> zoneTiles;
        public float elevationSum;
        public int elevationSampleCount;
        public float minimumElevation = float.MaxValue;
        public float distanceToWaterCells = float.MaxValue;
    }

    [Header("References")]
    [SerializeField] private FloodDefenseBoxStamp floodDefense;
    [SerializeField] private JsonMapLoader jsonMapLoader;
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private ZoneThinOutlineByHover zoneOutlineHighlighter;
    [SerializeField] private ShelterManager shelterManager;
    [SerializeField] private EvacuationController evacuationController;

    [Header("Risk Settings")]
    [SerializeField] private float maxRiskDistanceCells = 12f;
    [SerializeField] private float distanceWeight = 0.40f;
    [SerializeField] private float elevationWeight = 0.35f;
    [SerializeField] private float populationWeight = 0.25f;
    [SerializeField] private PopulationAggregationMode populationAggregationMode = PopulationAggregationMode.MaxPerZone;

    [Header("Thresholds")]
    [SerializeField, Range(0f, 1f)] private float mediumRiskThreshold = 0.30f;
    [SerializeField, Range(0f, 1f)] private float highRiskThreshold = 0.60f;
    [SerializeField, Range(0f, 1f)] private float criticalRiskThreshold = 0.80f;

    [Header("Highlight Settings")]
    [SerializeField] private bool autoCalculateOnStart = true;
    [SerializeField] private bool autoHighlightHighRiskOnStart = true;
    [SerializeField, Min(1)] private int maxHighlightedZones = 10;
    [SerializeField] private Color criticalRiskOutlineColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highRiskOutlineColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color mediumRiskOutlineColor = new Color(1f, 0.95f, 0.2f, 1f);
    [SerializeField] private Color lowRiskOutlineColor = new Color(0.25f, 0.85f, 0.35f, 1f);

    [Header("Water Detection")]
    [SerializeField] private List<string> waterCategoryKeywords = new()
    {
        "water",
        "river",
        "stream",
        "lake",
        "ocean",
        "flood_source",
    };

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool debugPrintTopZones = true;
    [SerializeField, Min(1)] private int debugTopZoneCount = 10;

    private readonly Dictionary<string, ZoneBaselineRiskData> _riskByGeoid = new();
    private readonly List<ZoneBaselineRiskData> _sortedRiskData = new();

    private Coroutine _startupRoutine;
    private bool _hasCalculatedRisk;
    private bool _loggedMissingWaterWarning;
    private bool _loggedMissingElevationWarning;
    private bool _isSubscribedToShelterManager;

    public bool HasCalculatedBaselineRisk => _hasCalculatedRisk;
    public event Action BaselineRiskCalculated;
    public event Action ShelterAdjustedRiskRefreshed;

    private void Awake()
    {
        if (floodDefense == null)
            floodDefense = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (jsonMapLoader == null)
            jsonMapLoader = FindFirstObjectByType<JsonMapLoader>();

        if (zoneOutlineHighlighter == null)
            zoneOutlineHighlighter = FindFirstObjectByType<ZoneThinOutlineByHover>();

        if (shelterManager == null)
            shelterManager = FindFirstObjectByType<ShelterManager>();

        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();
    }

    private void OnEnable()
    {
        if (shelterManager == null)
            shelterManager = FindFirstObjectByType<ShelterManager>();

        if (shelterManager != null && !_isSubscribedToShelterManager)
        {
            shelterManager.OnSheltersChanged += OnSheltersChanged;
            _isSubscribedToShelterManager = true;
        }
    }

    private void Start()
    {
        if (!autoCalculateOnStart)
            return;

        _startupRoutine = StartCoroutine(CalculateWhenReadyRoutine(autoHighlightHighRiskOnStart));
    }

    private void OnDisable()
    {
        if (shelterManager != null && _isSubscribedToShelterManager)
        {
            shelterManager.OnSheltersChanged -= OnSheltersChanged;
            _isSubscribedToShelterManager = false;
        }

        if (_startupRoutine != null)
        {
            StopCoroutine(_startupRoutine);
            _startupRoutine = null;
        }
    }

    private void OnValidate()
    {
        maxRiskDistanceCells = Mathf.Max(0.01f, maxRiskDistanceCells);
        maxHighlightedZones = Mathf.Max(1, maxHighlightedZones);
        debugTopZoneCount = Mathf.Max(1, debugTopZoneCount);

        mediumRiskThreshold = Mathf.Clamp01(mediumRiskThreshold);
        highRiskThreshold = Mathf.Clamp(highRiskThreshold, mediumRiskThreshold, 1f);
        criticalRiskThreshold = Mathf.Clamp(criticalRiskThreshold, highRiskThreshold, 1f);
    }

    [ContextMenu("Recalculate Baseline Risk")]
    public void RecalculateBaselineRisk()
    {
        TryCalculateBaselineRisk();
    }

    [ContextMenu("Recalculate Baseline Risk And Highlight")]
    public void RecalculateBaselineRiskAndHighlight()
    {
        if (TryCalculateBaselineRisk())
            HighlightHighRiskZones();
    }

    public void HighlightHighRiskZones()
    {
        if (zoneOutlineHighlighter == null)
        {
            Debug.LogWarning("[ZoneBaselineRiskController] Cannot highlight high-risk zones because ZoneThinOutlineByHover is not assigned.");
            return;
        }

        List<ZoneBaselineRiskData> highRiskZones = GetHighRiskZones();
        int limit = Mathf.Min(maxHighlightedZones, highRiskZones.Count);
        List<ZoneRiskOutlineRequest> outlineRequests = new(limit);

        for (int i = 0; i < limit; i++)
        {
            ZoneBaselineRiskData riskData = highRiskZones[i];

            if (string.IsNullOrWhiteSpace(riskData.geoid))
                continue;

            outlineRequests.Add(new ZoneRiskOutlineRequest(riskData.geoid, GetRiskColorForLevel(riskData.riskLevel)));
        }

        zoneOutlineHighlighter.ShowRiskOutlines(outlineRequests);

        if (debugLogs)
            Debug.Log($"[ZoneBaselineRiskController] Highlighted {outlineRequests.Count} high-risk zones.");
    }

    public void ClearRiskHighlights()
    {
        if (zoneOutlineHighlighter != null)
            zoneOutlineHighlighter.ClearPersistentZoneOutlines();
    }

    public List<ZoneBaselineRiskData> GetHighRiskZones()
    {
        List<ZoneBaselineRiskData> results = new();

        for (int i = 0; i < _sortedRiskData.Count; i++)
        {
            if (_sortedRiskData[i].baselineRiskScore >= highRiskThreshold)
                results.Add(_sortedRiskData[i]);
        }

        return results;
    }

    public List<ZoneBaselineRiskData> GetTopRiskZones(int count)
    {
        List<ZoneBaselineRiskData> results = new();

        if (count <= 0)
            return results;

        int limit = Mathf.Min(count, _sortedRiskData.Count);

        for (int i = 0; i < limit; i++)
            results.Add(_sortedRiskData[i]);

        return results;
    }

    public IReadOnlyList<ZoneBaselineRiskData> GetAllRiskResults()
    {
        return _sortedRiskData;
    }

    public bool EnsureBaselineRiskCalculated()
    {
        return _hasCalculatedRisk || TryCalculateBaselineRisk();
    }

    public bool TryGetRiskData(string geoid, out ZoneBaselineRiskData data)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
        {
            data = default;
            return false;
        }

        return _riskByGeoid.TryGetValue(geoid, out data);
    }

    public void RefreshShelterAdjustedRisk()
    {
        if (!_hasCalculatedRisk && !TryCalculateBaselineRisk())
            return;

        ApplyShelterAdjustedRisk(true);
    }

    public void RefreshEvacuationAdjustedRisk()
    {
        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (!_hasCalculatedRisk && !TryCalculateBaselineRisk())
            return;

        ApplyShelterAdjustedRisk(true);
    }

    public float GetAdjustedRiskForZone(string geoid)
    {
        return TryGetRiskData(geoid, out ZoneBaselineRiskData data)
            ? data.adjustedBaselineRiskScore
            : 0f;
    }

    public float GetOriginalRiskForZone(string geoid)
    {
        return TryGetRiskData(geoid, out ZoneBaselineRiskData data)
            ? data.originalBaselineRiskScore
            : 0f;
    }

    public float GetShelterMitigationForZone(string geoid)
    {
        return TryGetRiskData(geoid, out ZoneBaselineRiskData data)
            ? data.shelterMitigationApplied
            : 0f;
    }

    public float GetEvacuationMitigationForZone(string geoid)
    {
        return TryGetRiskData(geoid, out ZoneBaselineRiskData data)
            ? data.evacuationMitigationApplied
            : 0f;
    }

    public float GetCombinedPopulationMitigationForZone(string geoid)
    {
        return TryGetRiskData(geoid, out ZoneBaselineRiskData data)
            ? data.combinedPopulationMitigation
            : 0f;
    }

    public bool TryGetZoneCenterWorld(string geoid, out Vector3 centerWorld)
    {
        centerWorld = Vector3.zero;
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return false;

        if (_riskByGeoid.TryGetValue(geoid, out ZoneBaselineRiskData cachedRiskData) && cachedRiskData.hasWorldCenter)
        {
            centerWorld = cachedRiskData.worldCenter;
            return true;
        }

        if (!ValidateReferences())
            return false;

        if (!floodDefense.TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles) || zoneTiles == null || zoneTiles.Count == 0)
            return false;

        return TryGetZoneCenterWorld(zoneTiles, out centerWorld);
    }

    public Color GetRiskColorForLevel(RiskLevel level)
    {
        switch (level)
        {
            case RiskLevel.Critical:
                return criticalRiskOutlineColor;

            case RiskLevel.High:
                return highRiskOutlineColor;

            case RiskLevel.Medium:
                return mediumRiskOutlineColor;

            default:
                return lowRiskOutlineColor;
        }
    }

    public bool TryGetRiskColorForZone(string geoid, out Color riskColor)
    {
        riskColor = lowRiskOutlineColor;

        if (!TryGetRiskData(geoid, out ZoneBaselineRiskData data))
            return false;

        riskColor = GetRiskColorForLevel(data.riskLevel);
        return true;
    }

    public Color GetRiskColorForZone(string geoid)
    {
        return TryGetRiskColorForZone(geoid, out Color riskColor) ? riskColor : lowRiskOutlineColor;
    }

    private void OnSheltersChanged()
    {
        RefreshShelterAdjustedRisk();
    }

    private void ApplyShelterAdjustedRisk(bool logResult)
    {
        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (!_hasCalculatedRisk || _sortedRiskData.Count == 0)
            return;

        GetNormalizedWeights(
            out float normalizedDistanceWeight,
            out float normalizedElevationWeight,
            out float normalizedPopulationWeight);

        int adjustedCount = 0;

        for (int i = 0; i < _sortedRiskData.Count; i++)
        {
            ZoneBaselineRiskData data = _sortedRiskData[i];
            float shelterMitigation = shelterManager != null
                ? shelterManager.GetShelterMitigationForZone(data.geoid)
                : 0f;
            float evacuationMitigation = evacuationController != null
                ? evacuationController.GetEvacuationMitigationForZone(data.geoid)
                : 0f;
            float combinedPopulationMitigation = 1f - ((1f - Mathf.Clamp01(shelterMitigation)) * (1f - Mathf.Clamp01(evacuationMitigation)));
            int estimatedPeopleProtected = shelterManager != null
                ? shelterManager.GetProtectedPopulationForZone(data.geoid)
                : 0;
            int estimatedPeopleEvacuated = evacuationController != null
                ? evacuationController.GetEvacuatedPopulationForZone(data.geoid)
                : 0;

            float originalPopulationRisk = data.originalPopulationRisk > 0f
                ? data.originalPopulationRisk
                : data.populationRisk;
            float adjustedPopulationRisk = Mathf.Clamp01(originalPopulationRisk * (1f - combinedPopulationMitigation));
            float adjustedBaselineRisk =
                (normalizedDistanceWeight * data.distanceRisk) +
                (normalizedElevationWeight * data.elevationRisk) +
                (normalizedPopulationWeight * adjustedPopulationRisk);

            data.originalBaselineRiskScore = data.originalBaselineRiskScore > 0f
                ? data.originalBaselineRiskScore
                : data.baselineRiskScore;
            data.originalPopulationRisk = originalPopulationRisk;
            data.adjustedPopulationRisk = adjustedPopulationRisk;
            data.shelterMitigationApplied = Mathf.Clamp01(shelterMitigation);
            data.evacuationMitigationApplied = Mathf.Clamp01(evacuationMitigation);
            data.combinedPopulationMitigation = Mathf.Clamp01(combinedPopulationMitigation);
            data.estimatedPeopleProtected = Mathf.Max(0, estimatedPeopleProtected);
            data.estimatedPeopleEvacuated = Mathf.Max(0, estimatedPeopleEvacuated);
            data.adjustedBaselineRiskScore = Mathf.Clamp01(adjustedBaselineRisk);
            data.adjustedRiskLevel = GetRiskLevel(data.adjustedBaselineRiskScore);

            _sortedRiskData[i] = data;
            _riskByGeoid[data.geoid] = data;
            adjustedCount++;
        }

        ShelterAdjustedRiskRefreshed?.Invoke();

        if (logResult && debugLogs)
            Debug.Log($"[ZoneBaselineRiskController] Population-adjusted baseline risk refreshed for {adjustedCount} zones.");
    }

    private IEnumerator CalculateWhenReadyRoutine(bool highlightAfterCalculate)
    {
        if (!ValidateReferences())
        {
            _startupRoutine = null;
            yield break;
        }

        while (!IsMapReady())
            yield return null;

        _startupRoutine = null;

        if (!TryCalculateBaselineRisk())
            yield break;

        if (highlightAfterCalculate)
            HighlightHighRiskZones();
    }

    private bool TryCalculateBaselineRisk()
    {
        if (!ValidateReferences())
            return false;

        if (!IsMapReady())
        {
            Debug.LogWarning("[ZoneBaselineRiskController] Cannot calculate baseline risk yet because the map data is not ready.");
            return false;
        }

        IReadOnlyDictionary<string, HashSet<Vector2Int>> zoneLookup = floodDefense.GetAllZoneTiles();

        if (zoneLookup == null || zoneLookup.Count == 0)
        {
            Debug.LogWarning("[ZoneBaselineRiskController] No zone tiles were available for baseline risk calculation.");
            return false;
        }

        int mapSize = tileMapData.N;
        int[,] distanceMap = BuildDistanceToWaterMap(mapSize, out bool foundWaterSources);
        bool hasElevationRange = TryGetMapElevationRange(mapSize, out float minMapElevation, out float maxMapElevation);

        _riskByGeoid.Clear();
        _sortedRiskData.Clear();

        Dictionary<string, ZoneComputationState> stateByGeoid = new();
        int maxPopulationAcrossZones = 0;

        foreach (KeyValuePair<string, HashSet<Vector2Int>> pair in zoneLookup)
        {
            string geoid = NormalizeGeoid(pair.Key);
            HashSet<Vector2Int> zoneTiles = pair.Value;

            if (string.IsNullOrEmpty(geoid) || zoneTiles == null || zoneTiles.Count == 0)
                continue;

            ZoneComputationState state = BuildZoneComputationState(zoneTiles, distanceMap, foundWaterSources);
            stateByGeoid[geoid] = state;

            if (state.rawPopulation > maxPopulationAcrossZones)
                maxPopulationAcrossZones = state.rawPopulation;
        }

        GetNormalizedWeights(out float normalizedDistanceWeight, out float normalizedElevationWeight, out float normalizedPopulationWeight);

        foreach (KeyValuePair<string, ZoneComputationState> pair in stateByGeoid)
        {
            string geoid = pair.Key;
            ZoneComputationState state = pair.Value;

            float averageElevation = state.elevationSampleCount > 0
                ? state.elevationSum / state.elevationSampleCount
                : 0f;
            float minimumElevation = state.minimumElevation == float.MaxValue ? 0f : state.minimumElevation;
            float distanceToWaterCells = (!foundWaterSources || state.distanceToWaterCells == float.MaxValue)
                ? maxRiskDistanceCells
                : state.distanceToWaterCells;

            float distanceRisk = foundWaterSources
                ? 1f - Mathf.Clamp01(distanceToWaterCells / maxRiskDistanceCells)
                : 0f;
            float elevationRisk = hasElevationRange && state.elevationSampleCount > 0
                ? 1f - Mathf.InverseLerp(minMapElevation, maxMapElevation, averageElevation)
                : 0f;
            float populationRisk = maxPopulationAcrossZones > 0
                ? Mathf.Log(state.rawPopulation + 1f) / Mathf.Log(maxPopulationAcrossZones + 1f)
                : 0f;

            float baselineRiskScore =
                (normalizedDistanceWeight * distanceRisk) +
                (normalizedElevationWeight * elevationRisk) +
                (normalizedPopulationWeight * populationRisk);

            bool hasWorldCenter = TryGetZoneCenterWorld(state.zoneTiles, out Vector3 worldCenter);

            ZoneBaselineRiskData riskData = new ZoneBaselineRiskData
            {
                geoid = geoid,
                rawPopulation = state.rawPopulation,
                tileCount = state.tileCount,
                averageElevation = averageElevation,
                minimumElevation = minimumElevation,
                distanceToWaterCells = distanceToWaterCells,
                distanceRisk = Mathf.Clamp01(distanceRisk),
                elevationRisk = Mathf.Clamp01(elevationRisk),
                populationRisk = Mathf.Clamp01(populationRisk),
                baselineRiskScore = Mathf.Clamp01(baselineRiskScore),
                originalBaselineRiskScore = Mathf.Clamp01(baselineRiskScore),
                adjustedBaselineRiskScore = Mathf.Clamp01(baselineRiskScore),
                originalPopulationRisk = Mathf.Clamp01(populationRisk),
                adjustedPopulationRisk = Mathf.Clamp01(populationRisk),
                shelterMitigationApplied = 0f,
                estimatedPeopleProtected = 0,
                riskLevel = GetRiskLevel(baselineRiskScore),
                adjustedRiskLevel = GetRiskLevel(baselineRiskScore),
                hasWorldCenter = hasWorldCenter,
                worldCenter = worldCenter,
            };

            _riskByGeoid[geoid] = riskData;
            _sortedRiskData.Add(riskData);
        }

        _sortedRiskData.Sort((a, b) =>
        {
            int scoreCompare = b.baselineRiskScore.CompareTo(a.baselineRiskScore);
            return scoreCompare != 0 ? scoreCompare : string.CompareOrdinal(a.geoid, b.geoid);
        });

        _hasCalculatedRisk = _sortedRiskData.Count > 0;

        if (_hasCalculatedRisk)
            ApplyShelterAdjustedRisk(false);

        LogCalculationSummary();

        if (_hasCalculatedRisk)
            BaselineRiskCalculated?.Invoke();

        return _hasCalculatedRisk;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (floodDefense == null)
        {
            Debug.LogError("[ZoneBaselineRiskController] floodDefense is not assigned.");
            isValid = false;
        }

        if (jsonMapLoader == null)
        {
            Debug.LogError("[ZoneBaselineRiskController] jsonMapLoader is not assigned.");
            isValid = false;
        }

        if (tileMapData == null)
        {
            Debug.LogError("[ZoneBaselineRiskController] tileMapData is not assigned.");
            isValid = false;
        }

        if (terrainTilemap == null && floodDefense != null)
            terrainTilemap = floodDefense.TerrainTilemap;

        if (terrainTilemap == null)
        {
            Debug.LogError("[ZoneBaselineRiskController] terrainTilemap is not assigned.");
            isValid = false;
        }

        return isValid;
    }

    private bool IsMapReady()
    {
        if (!HasRequiredReferences())
            return false;

        if (jsonMapLoader.payload == null)
            return false;

        if (jsonMapLoader.cellToRC == null || jsonMapLoader.cellToRC.Count == 0)
            return false;

        if (jsonMapLoader.geoidGrid == null || jsonMapLoader.catGrid == null || jsonMapLoader.popGrid == null)
            return false;

        if (tileMapData.N <= 0)
            return false;

        return floodDefense.EnsureZoneIndexReadyForExternalUse();
    }

    private bool HasRequiredReferences()
    {
        if (terrainTilemap == null && floodDefense != null)
            terrainTilemap = floodDefense.TerrainTilemap;

        return floodDefense != null &&
               jsonMapLoader != null &&
               tileMapData != null &&
               terrainTilemap != null;
    }

    private ZoneComputationState BuildZoneComputationState(
        HashSet<Vector2Int> zoneTiles,
        int[,] distanceMap,
        bool foundWaterSources)
    {
        ZoneComputationState state = new()
        {
            tileCount = zoneTiles.Count,
            zoneTiles = zoneTiles,
        };

        bool firstNonZeroPopulationSet = false;

        foreach (Vector2Int normalizedTile in zoneTiles)
        {
            Vector2Int rc;
            bool hasRowColumn = TryGetRowColumn(normalizedTile, out rc);

            int population = GetPopulationValue(normalizedTile, rc, hasRowColumn);

            switch (populationAggregationMode)
            {
                case PopulationAggregationMode.SumPerTile:
                    state.rawPopulation += population;
                    break;

                case PopulationAggregationMode.MaxPerZone:
                    if (population > state.rawPopulation)
                        state.rawPopulation = population;
                    break;

                case PopulationAggregationMode.FirstNonZero:
                    if (!firstNonZeroPopulationSet && population > 0)
                    {
                        state.rawPopulation = population;
                        firstNonZeroPopulationSet = true;
                    }
                    break;
            }

            if (TryGetElevation(normalizedTile, rc, out float elevation))
            {
                state.elevationSum += elevation;
                state.elevationSampleCount++;

                if (elevation < state.minimumElevation)
                    state.minimumElevation = elevation;
            }

            if (!foundWaterSources)
                continue;

            int distance = distanceMap[normalizedTile.x, normalizedTile.y];

            if (distance >= 0 && distance < state.distanceToWaterCells)
                state.distanceToWaterCells = distance;
        }

        return state;
    }

    private int[,] BuildDistanceToWaterMap(int mapSize, out bool foundWaterSources)
    {
        int[,] distanceMap = new int[mapSize, mapSize];
        Queue<Vector2Int> frontier = new();

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                distanceMap[x, y] = -1;

                if (!TryGetCategoryAtNormalizedTile(new Vector2Int(x, y), out string category))
                    continue;

                if (!IsWaterCategory(category))
                    continue;

                distanceMap[x, y] = 0;
                frontier.Enqueue(new Vector2Int(x, y));
            }
        }

        foundWaterSources = frontier.Count > 0;

        if (!foundWaterSources && !_loggedMissingWaterWarning)
        {
            Debug.LogWarning("[ZoneBaselineRiskController] No water-source tiles were found. Distance risk will default to 0.");
            _loggedMissingWaterWarning = true;
            return distanceMap;
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

                if (next.x < 0 || next.y < 0 || next.x >= mapSize || next.y >= mapSize)
                    continue;

                if (distanceMap[next.x, next.y] >= 0)
                    continue;

                distanceMap[next.x, next.y] = nextDistance;
                frontier.Enqueue(next);
            }
        }

        return distanceMap;
    }

    private bool TryGetMapElevationRange(int mapSize, out float minElevation, out float maxElevation)
    {
        minElevation = float.MaxValue;
        maxElevation = float.MinValue;

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                Vector2Int tile = new Vector2Int(x, y);
                Vector2Int rc;

                if (!TryGetRowColumn(tile, out rc))
                    continue;

                if (!TryGetElevation(tile, rc, out float elevation))
                    continue;

                if (elevation < minElevation)
                    minElevation = elevation;

                if (elevation > maxElevation)
                    maxElevation = elevation;
            }
        }

        if (minElevation == float.MaxValue || maxElevation == float.MinValue)
            return false;

        if (Mathf.Approximately(minElevation, maxElevation))
            maxElevation = minElevation + 0.0001f;

        return true;
    }

    private bool TryGetRowColumn(Vector2Int normalizedTile, out Vector2Int rc)
    {
        rc = default;

        if (floodDefense == null || terrainTilemap == null || jsonMapLoader == null || jsonMapLoader.cellToRC == null)
            return false;

        Vector3Int cell = floodDefense.NormalizedTileToCell(normalizedTile);
        return jsonMapLoader.cellToRC.TryGetValue(cell, out rc);
    }

    private int GetPopulationValue(Vector2Int normalizedTile, Vector2Int rc, bool hasRowColumn)
    {
        TileInstance tile = TryGetTileInstance(normalizedTile);

        if (tile != null)
            return Mathf.Max(0, tile.population);

        if (hasRowColumn && jsonMapLoader.popGrid != null)
            return Mathf.Max(0, jsonMapLoader.popGrid[rc.x, rc.y]);

        return 0;
    }

    private bool TryGetZoneCenterWorld(HashSet<Vector2Int> zoneTiles, out Vector3 centerWorld)
    {
        centerWorld = Vector3.zero;

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

        centerWorld = sum / sampleCount;
        return true;
    }

    private bool TryGetCategoryAtNormalizedTile(Vector2Int normalizedTile, out string category)
    {
        category = null;

        TileInstance tile = TryGetTileInstance(normalizedTile);

        if (tile != null && !string.IsNullOrEmpty(tile.category))
        {
            category = tile.category;
            return true;
        }

        if (!TryGetRowColumn(normalizedTile, out Vector2Int rc))
            return false;

        if (jsonMapLoader.catGrid == null)
            return false;

        category = jsonMapLoader.catGrid[rc.x, rc.y];
        return !string.IsNullOrEmpty(category);
    }

    private bool TryGetElevation(Vector2Int normalizedTile, Vector2Int rc, out float elevation)
    {
        elevation = 0f;

        TileInstance tile = TryGetTileInstance(normalizedTile);

        if (tile != null)
        {
            elevation = tile.elevation;
            return true;
        }

        if (!_loggedMissingElevationWarning)
        {
            Debug.LogWarning($"[ZoneBaselineRiskController] Elevation data was missing for tile {normalizedTile}. Elevation risk will default to 0 for tiles without data.");
            _loggedMissingElevationWarning = true;
        }

        return false;
    }

    private TileInstance TryGetTileInstance(Vector2Int normalizedTile)
    {
        if (tileMapData == null)
            return null;

        if (normalizedTile.x < 0 || normalizedTile.y < 0 || normalizedTile.x >= tileMapData.N || normalizedTile.y >= tileMapData.N)
            return null;

        return tileMapData.Get(normalizedTile);
    }

    private void GetNormalizedWeights(out float normalizedDistanceWeight, out float normalizedElevationWeight, out float normalizedPopulationWeight)
    {
        float safeDistanceWeight = Mathf.Max(0f, distanceWeight);
        float safeElevationWeight = Mathf.Max(0f, elevationWeight);
        float safePopulationWeight = Mathf.Max(0f, populationWeight);
        float total = safeDistanceWeight + safeElevationWeight + safePopulationWeight;

        if (total <= Mathf.Epsilon)
        {
            normalizedDistanceWeight = 1f / 3f;
            normalizedElevationWeight = 1f / 3f;
            normalizedPopulationWeight = 1f / 3f;
            return;
        }

        normalizedDistanceWeight = safeDistanceWeight / total;
        normalizedElevationWeight = safeElevationWeight / total;
        normalizedPopulationWeight = safePopulationWeight / total;
    }

    private RiskLevel GetRiskLevel(float score)
    {
        if (score >= criticalRiskThreshold)
            return RiskLevel.Critical;

        if (score >= highRiskThreshold)
            return RiskLevel.High;

        if (score >= mediumRiskThreshold)
            return RiskLevel.Medium;

        return RiskLevel.Low;
    }

    private bool IsWaterCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return false;

        if (waterCategoryKeywords == null || waterCategoryKeywords.Count == 0)
            return false;

        for (int i = 0; i < waterCategoryKeywords.Count; i++)
        {
            string keyword = waterCategoryKeywords[i];

            if (string.IsNullOrEmpty(keyword))
                continue;

            if (category.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void LogCalculationSummary()
    {
        if (!_hasCalculatedRisk)
        {
            Debug.LogWarning("[ZoneBaselineRiskController] Baseline risk calculation completed, but no zones produced cached results.");
            return;
        }

        if (debugLogs)
            Debug.Log($"[ZoneBaselineRiskController] Baseline risk calculated for {_sortedRiskData.Count} zones.");

        if (!debugPrintTopZones)
            return;

        int limit = Mathf.Min(debugTopZoneCount, _sortedRiskData.Count);

        Debug.Log("[ZoneBaselineRiskController] Top risk zones:");

        for (int i = 0; i < limit; i++)
        {
            ZoneBaselineRiskData data = _sortedRiskData[i];
            Debug.Log(
                $"[ZoneBaselineRiskController] GEOID={data.geoid} | Risk={data.baselineRiskScore:F2} {data.riskLevel} | " +
                $"Pop={data.rawPopulation} | AvgElev={data.averageElevation:F2} | MinElev={data.minimumElevation:F2} | DistWater={data.distanceToWaterCells:F0}");
        }
    }

    private string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? null : geoid.Trim();
    }
}
