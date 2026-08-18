using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ZoneFloodImpactResult
{
    public string geoid;
    public RiskLevel riskLevel;
    public int totalTileCount;
    public int floodedTileCount;
    public float floodedFraction;
    public float averageWaterDepth;
    public float maxWaterDepth;
    public int rawPopulation;
    public float estimatedAssetValue;
    public float affectedPopulation;
    public float originalAffectedPopulation;
    public float affectedPopulationAfterShelter;
    public float affectedPopulationAfterEvacuation;
    public float exposedPopulationAfterMitigation;
    public int peopleProtectedByShelter;
    public int peopleEvacuated;
    public float shelterMitigationApplied;
    public float evacuationMitigationApplied;
    public float combinedPopulationMitigation;
    public float physicalLiveFloodRisk;
    public float liveFloodRisk;
    public float estimatedDamage;
    public float estimatedDamageBeforeShelter;
    public float estimatedDamageAfterShelter;
    public float estimatedDamageAfterEvacuation;
    public bool hasWorldCenter;
    public Vector3 worldCenter;
}

public class FloodImpactController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloodDefenseBoxStamp floodDefense;
    [SerializeField] private ZoneBaselineRiskController baselineRiskController;
    [SerializeField] private ShelterManager shelterManager;
    [SerializeField] private EvacuationController evacuationController;
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private WaterSimulator waterSimulator;

    [Header("Risk Settings")]
    [SerializeField] private float floodedFractionWeight = 0.40f;
    [SerializeField] private float averageDepthWeight = 0.25f;
    [SerializeField] private float maxDepthWeight = 0.20f;
    [SerializeField] private float populationWeight = 0.15f;
    [SerializeField] private float maxExpectedWaterDepth = 2.0f;
    [SerializeField] private float floodedDepthThreshold = 0.01f;

    [Header("Thresholds")]
    [SerializeField, Range(0f, 1f)] private float mediumRiskThreshold = 0.30f;
    [SerializeField, Range(0f, 1f)] private float highRiskThreshold = 0.60f;
    [SerializeField, Range(0f, 1f)] private float criticalRiskThreshold = 0.80f;

    [Header("Damage Settings")]
    [SerializeField] private float baseAssetValuePerTile = 1000f;
    [SerializeField] private float assetValuePerPerson = 500f;
    [SerializeField] private float vulnerabilityMultiplier = 1.0f;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnSimulationStart = true;
    [SerializeField] private bool refreshOnSimulationStep = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly Dictionary<string, ZoneFloodImpactResult> _impactByGeoid = new Dictionary<string, ZoneFloodImpactResult>();
    private readonly List<ZoneFloodImpactResult> _sortedImpactResults = new List<ZoneFloodImpactResult>();
    private bool _isSubscribedToShelterManager;

    public bool HasCalculatedLiveImpact { get; private set; }

    public event Action FloodImpactRefreshed;

    private void Awake()
    {
        if (floodDefense == null)
            floodDefense = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (baselineRiskController == null)
            baselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (shelterManager == null)
            shelterManager = FindFirstObjectByType<ShelterManager>();

        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (tileMapData == null)
        {
            TileMapData[] tileMaps = Resources.FindObjectsOfTypeAll<TileMapData>();
            if (tileMaps != null && tileMaps.Length > 0)
                tileMapData = tileMaps[0];
        }

        if (waterSimulator == null)
            waterSimulator = FindFirstObjectByType<WaterSimulator>();
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

        if (waterSimulator != null)
        {
            waterSimulator.OnSimulationStarted += OnSimulationStarted;
            waterSimulator.OnSimulationStep += OnSimulationStep;
        }
    }

    private void OnDisable()
    {
        if (shelterManager != null && _isSubscribedToShelterManager)
        {
            shelterManager.OnSheltersChanged -= OnSheltersChanged;
            _isSubscribedToShelterManager = false;
        }

        if (waterSimulator != null)
        {
            waterSimulator.OnSimulationStarted -= OnSimulationStarted;
            waterSimulator.OnSimulationStep -= OnSimulationStep;
        }
    }

    private void OnValidate()
    {
        maxExpectedWaterDepth = Mathf.Max(0.01f, maxExpectedWaterDepth);
        floodedDepthThreshold = Mathf.Max(0f, floodedDepthThreshold);
        vulnerabilityMultiplier = Mathf.Max(0f, vulnerabilityMultiplier);

        mediumRiskThreshold = Mathf.Clamp01(mediumRiskThreshold);
        highRiskThreshold = Mathf.Clamp(highRiskThreshold, mediumRiskThreshold, 1f);
        criticalRiskThreshold = Mathf.Clamp(criticalRiskThreshold, highRiskThreshold, 1f);
    }

    [ContextMenu("Refresh Flood Impact")]
    public void RefreshFloodImpact()
    {
        RecalculateFloodImpact();
    }

    public IReadOnlyList<ZoneFloodImpactResult> GetAllFloodImpactResults()
    {
        return _sortedImpactResults;
    }

    public void RefreshEvacuationAdjustedImpact()
    {
        RecalculateFloodImpact();
    }

    public List<ZoneFloodImpactResult> GetTopFloodImpactZones(int count)
    {
        List<ZoneFloodImpactResult> results = new List<ZoneFloodImpactResult>();

        if (count <= 0)
            return results;

        int limit = Mathf.Min(count, _sortedImpactResults.Count);

        for (int i = 0; i < limit; i++)
            results.Add(_sortedImpactResults[i]);

        return results;
    }

    public bool TryGetFloodImpactResult(string geoid, out ZoneFloodImpactResult result)
    {
        result = default;
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return false;

        return _impactByGeoid.TryGetValue(geoid, out result);
    }

    private void OnSimulationStarted()
    {
        if (refreshOnSimulationStart)
            RefreshFloodImpact();
    }

    private void OnSimulationStep()
    {
        if (refreshOnSimulationStep)
            RefreshFloodImpact();
    }

    private void OnSheltersChanged()
    {
        if (!HasCalculatedLiveImpact)
            return;

        RefreshFloodImpact();

        if (debugLogs)
            Debug.Log("[FloodImpactController] Live flood impact adjusted after shelter placement.");
    }

    private void RecalculateFloodImpact()
    {
        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (!ValidateReferences())
        {
            ClearCachedImpactResults();
            return;
        }

        if (!floodDefense.EnsureZoneIndexReadyForExternalUse())
        {
            ClearCachedImpactResults();
            Debug.LogWarning("[FloodImpactController] Zone data is not ready, so flood impact could not be calculated yet.");
            return;
        }

        IReadOnlyDictionary<string, HashSet<Vector2Int>> zoneLookup = floodDefense.GetAllZoneTiles();
        if (zoneLookup == null || zoneLookup.Count == 0)
        {
            ClearCachedImpactResults();
            Debug.LogWarning("[FloodImpactController] No zone tiles were available for live flood impact calculation.");
            return;
        }

        if (baselineRiskController != null)
            baselineRiskController.EnsureBaselineRiskCalculated();

        _impactByGeoid.Clear();
        _sortedImpactResults.Clear();

        int maxPopulationAcrossZones = 0;

        foreach (KeyValuePair<string, HashSet<Vector2Int>> pair in zoneLookup)
        {
            string geoid = NormalizeGeoid(pair.Key);
            HashSet<Vector2Int> zoneTiles = pair.Value;

            if (string.IsNullOrEmpty(geoid) || zoneTiles == null || zoneTiles.Count == 0)
                continue;

            int rawPopulation = GetZonePopulation(geoid, zoneTiles);
            if (rawPopulation > maxPopulationAcrossZones)
                maxPopulationAcrossZones = rawPopulation;
        }

        GetNormalizedWeights(
            out float normalizedFloodedFractionWeight,
            out float normalizedAverageDepthWeight,
            out float normalizedMaxDepthWeight,
            out float normalizedPopulationWeight);

        foreach (KeyValuePair<string, HashSet<Vector2Int>> pair in zoneLookup)
        {
            string geoid = NormalizeGeoid(pair.Key);
            HashSet<Vector2Int> zoneTiles = pair.Value;

            if (string.IsNullOrEmpty(geoid) || zoneTiles == null || zoneTiles.Count == 0)
                continue;

            int totalTileCount = zoneTiles.Count;
            int floodedTileCount = 0;
            float floodedDepthSum = 0f;
            float maxWaterDepth = 0f;
            int rawPopulation = GetZonePopulation(geoid, zoneTiles);

            foreach (Vector2Int tile in zoneTiles)
            {
                float waterDepth = GetWaterDepth(tile);

                if (waterDepth < floodedDepthThreshold)
                    continue;

                floodedTileCount++;
                floodedDepthSum += waterDepth;

                if (waterDepth > maxWaterDepth)
                    maxWaterDepth = waterDepth;
            }

            float floodedFraction = totalTileCount > 0
                ? Mathf.Clamp01((float)floodedTileCount / totalTileCount)
                : 0f;

            float averageWaterDepth = floodedTileCount > 0
                ? floodedDepthSum / floodedTileCount
                : 0f;

            float averageDepthRisk = Mathf.Clamp01(averageWaterDepth / maxExpectedWaterDepth);
            float maxDepthRisk = Mathf.Clamp01(maxWaterDepth / maxExpectedWaterDepth);
            float shelterMitigation = shelterManager != null
                ? shelterManager.GetShelterMitigationForZone(geoid)
                : 0f;
            float evacuationMitigation = evacuationController != null
                ? evacuationController.GetEvacuationMitigationForZone(geoid)
                : 0f;
            float combinedPopulationMitigation = 1f - ((1f - Mathf.Clamp01(shelterMitigation)) * (1f - Mathf.Clamp01(evacuationMitigation)));
            float shelterAdjustedPopulation = Mathf.Max(0f, rawPopulation * (1f - shelterMitigation));
            float mitigatedPopulation = Mathf.Max(0f, rawPopulation * (1f - combinedPopulationMitigation));
            float adjustedPopulationRisk = maxPopulationAcrossZones > 0
                ? Mathf.Log(mitigatedPopulation + 1f) / Mathf.Log(maxPopulationAcrossZones + 1f)
                : 0f;

            float physicalLiveFloodRisk =
                (normalizedFloodedFractionWeight * floodedFraction) +
                (normalizedAverageDepthWeight * averageDepthRisk) +
                (normalizedMaxDepthWeight * maxDepthRisk);
            float liveFloodRisk =
                (normalizedFloodedFractionWeight * floodedFraction) +
                (normalizedAverageDepthWeight * averageDepthRisk) +
                (normalizedMaxDepthWeight * maxDepthRisk) +
                (normalizedPopulationWeight * adjustedPopulationRisk);

            float physicalAssetValue = baseAssetValuePerTile * totalTileCount;
            float populationAssetValueBeforeShelter = assetValuePerPerson * rawPopulation;
            float populationAssetValueAfterShelter = assetValuePerPerson * shelterAdjustedPopulation;
            float populationAssetValueAfterEvacuation = assetValuePerPerson * mitigatedPopulation;

            float estimatedAssetValue = Mathf.Max(
                0f,
                physicalAssetValue + populationAssetValueBeforeShelter);

            float depthSeverity = Mathf.Clamp01(((averageWaterDepth + maxWaterDepth) * 0.5f) / maxExpectedWaterDepth);
            float originalAffectedPopulation = Mathf.Clamp(rawPopulation * floodedFraction * depthSeverity, 0f, rawPopulation);
            float affectedPopulationAfterShelter = Mathf.Clamp(shelterAdjustedPopulation * floodedFraction * depthSeverity, 0f, rawPopulation);
            float affectedPopulationAfterEvacuation = Mathf.Clamp(mitigatedPopulation * floodedFraction * depthSeverity, 0f, rawPopulation);
            int peopleProtectedByShelter = Mathf.Max(0, Mathf.RoundToInt(originalAffectedPopulation - affectedPopulationAfterShelter));
            int peopleEvacuated = evacuationController != null
                ? evacuationController.GetEvacuatedPopulationForZone(geoid)
                : 0;
            float damageSeverity = floodedFraction * depthSeverity * vulnerabilityMultiplier;
            float estimatedDamageBeforeShelter = Mathf.Clamp(
                estimatedAssetValue * damageSeverity,
                0f,
                estimatedAssetValue);
            float estimatedDamageAfterShelter = Mathf.Clamp(
                (physicalAssetValue + populationAssetValueAfterShelter) * damageSeverity,
                0f,
                estimatedAssetValue);
            float estimatedDamageAfterEvacuation = Mathf.Clamp(
                (physicalAssetValue + populationAssetValueAfterEvacuation) * damageSeverity,
                0f,
                estimatedAssetValue);

            bool hasWorldCenter = TryGetZoneCenterWorld(geoid, zoneTiles, out Vector3 worldCenter);

            ZoneFloodImpactResult result = new ZoneFloodImpactResult
            {
                geoid = geoid,
                riskLevel = GetRiskLevel(liveFloodRisk),
                totalTileCount = totalTileCount,
                floodedTileCount = floodedTileCount,
                floodedFraction = floodedFraction,
                averageWaterDepth = averageWaterDepth,
                maxWaterDepth = maxWaterDepth,
                rawPopulation = rawPopulation,
                estimatedAssetValue = estimatedAssetValue,
                affectedPopulation = affectedPopulationAfterEvacuation,
                originalAffectedPopulation = originalAffectedPopulation,
                affectedPopulationAfterShelter = affectedPopulationAfterShelter,
                affectedPopulationAfterEvacuation = affectedPopulationAfterEvacuation,
                exposedPopulationAfterMitigation = mitigatedPopulation,
                peopleProtectedByShelter = peopleProtectedByShelter,
                peopleEvacuated = Mathf.Max(0, peopleEvacuated),
                shelterMitigationApplied = Mathf.Clamp01(shelterMitigation),
                evacuationMitigationApplied = Mathf.Clamp01(evacuationMitigation),
                combinedPopulationMitigation = Mathf.Clamp01(combinedPopulationMitigation),
                physicalLiveFloodRisk = Mathf.Clamp01(physicalLiveFloodRisk),
                liveFloodRisk = Mathf.Clamp01(liveFloodRisk),
                estimatedDamage = estimatedDamageAfterEvacuation,
                estimatedDamageBeforeShelter = estimatedDamageBeforeShelter,
                estimatedDamageAfterShelter = estimatedDamageAfterShelter,
                estimatedDamageAfterEvacuation = estimatedDamageAfterEvacuation,
                hasWorldCenter = hasWorldCenter,
                worldCenter = worldCenter,
            };

            _impactByGeoid[geoid] = result;
            _sortedImpactResults.Add(result);
        }

        _sortedImpactResults.Sort((a, b) =>
        {
            int scoreCompare = b.liveFloodRisk.CompareTo(a.liveFloodRisk);
            return scoreCompare != 0 ? scoreCompare : string.CompareOrdinal(a.geoid, b.geoid);
        });

        HasCalculatedLiveImpact = _sortedImpactResults.Count > 0;
        FloodImpactRefreshed?.Invoke();

        if (debugLogs)
            Debug.Log($"[FloodImpactController] Refreshed live flood impact for {_sortedImpactResults.Count} zones.");
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (floodDefense == null)
        {
            Debug.LogError("[FloodImpactController] FloodDefenseBoxStamp is not assigned.");
            isValid = false;
        }

        if (tileMapData == null)
        {
            Debug.LogError("[FloodImpactController] TileMapData is not assigned.");
            isValid = false;
        }

        return isValid;
    }

    private void ClearCachedImpactResults()
    {
        _impactByGeoid.Clear();
        _sortedImpactResults.Clear();
        HasCalculatedLiveImpact = false;
    }

    private int GetZonePopulation(string geoid, HashSet<Vector2Int> zoneTiles)
    {
        if (baselineRiskController != null && baselineRiskController.TryGetRiskData(geoid, out ZoneBaselineRiskData baselineRiskData))
            return Mathf.Max(0, baselineRiskData.rawPopulation);

        int maxPopulation = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            TileInstance tileInstance = TryGetTileInstance(tile);
            if (tileInstance == null)
                continue;

            if (tileInstance.population > maxPopulation)
                maxPopulation = tileInstance.population;
        }

        return Mathf.Max(0, maxPopulation);
    }

    private float GetWaterDepth(Vector2Int normalizedTile)
    {
        if (tileMapData.water != null)
        {
            int simX = normalizedTile.x + 1;
            int simY = normalizedTile.y + 1;

            if (simX >= 0 &&
                simY >= 0 &&
                simX < tileMapData.water.GetLength(0) &&
                simY < tileMapData.water.GetLength(1))
            {
                return Mathf.Max(0f, tileMapData.water[simX, simY]);
            }
        }

        TileInstance tileInstance = TryGetTileInstance(normalizedTile);
        return tileInstance != null ? Mathf.Max(0f, tileInstance.waterHeight) : 0f;
    }

    private TileInstance TryGetTileInstance(Vector2Int normalizedTile)
    {
        if (tileMapData == null)
            return null;

        if (normalizedTile.x < 0 ||
            normalizedTile.y < 0 ||
            normalizedTile.x >= tileMapData.sizeX ||
            normalizedTile.y >= tileMapData.sizeY)
        {
            return null;
        }

        return tileMapData.Get(normalizedTile);
    }

    private bool TryGetZoneCenterWorld(string geoid, HashSet<Vector2Int> zoneTiles, out Vector3 worldCenter)
    {
        worldCenter = Vector3.zero;

        if (baselineRiskController != null && baselineRiskController.TryGetZoneCenterWorld(geoid, out worldCenter))
            return true;

        if (zoneTiles == null || zoneTiles.Count == 0 || floodDefense == null || floodDefense.TerrainTilemap == null)
            return false;

        Vector3 sum = Vector3.zero;
        int sampleCount = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            Vector3Int cell = floodDefense.NormalizedTileToCell(tile);
            sum += floodDefense.TerrainTilemap.GetCellCenterWorld(cell);
            sampleCount++;
        }

        if (sampleCount == 0)
            return false;

        worldCenter = sum / sampleCount;
        return true;
    }

    private void GetNormalizedWeights(
        out float normalizedFloodedFractionWeight,
        out float normalizedAverageDepthWeight,
        out float normalizedMaxDepthWeight,
        out float normalizedPopulationWeight)
    {
        float safeFloodedFractionWeight = Mathf.Max(0f, floodedFractionWeight);
        float safeAverageDepthWeight = Mathf.Max(0f, averageDepthWeight);
        float safeMaxDepthWeight = Mathf.Max(0f, maxDepthWeight);
        float safePopulationWeight = Mathf.Max(0f, populationWeight);
        float total = safeFloodedFractionWeight + safeAverageDepthWeight + safeMaxDepthWeight + safePopulationWeight;

        if (total <= Mathf.Epsilon)
        {
            normalizedFloodedFractionWeight = 0.25f;
            normalizedAverageDepthWeight = 0.25f;
            normalizedMaxDepthWeight = 0.25f;
            normalizedPopulationWeight = 0.25f;
            return;
        }

        normalizedFloodedFractionWeight = safeFloodedFractionWeight / total;
        normalizedAverageDepthWeight = safeAverageDepthWeight / total;
        normalizedMaxDepthWeight = safeMaxDepthWeight / total;
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

    private static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? string.Empty : geoid.Trim();
    }
}
