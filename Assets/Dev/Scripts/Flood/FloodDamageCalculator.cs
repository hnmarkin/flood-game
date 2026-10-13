using System;
using System.Collections.Generic;
using UnityEngine;

public class FloodDamageCalculator : MonoBehaviour
{
    public readonly struct DamageSummary
    {
        public readonly string Geoid;
        public readonly int FloodedTileCount;
        public readonly float MaxDepthReached;
        public readonly float TotalEstimatedDamage;
        public readonly float AverageDamagePercent;
        public readonly int DamageableTileCount;

        public DamageSummary(
            string geoid,
            int floodedTileCount,
            float maxDepthReached,
            float totalEstimatedDamage,
            float averageDamagePercent,
            int damageableTileCount)
        {
            Geoid = geoid;
            FloodedTileCount = floodedTileCount;
            MaxDepthReached = maxDepthReached;
            TotalEstimatedDamage = totalEstimatedDamage;
            AverageDamagePercent = averageDamagePercent;
            DamageableTileCount = damageableTileCount;
        }

        public bool IsValid => DamageableTileCount > 0;
    }

    private sealed class DamageAccumulator
    {
        public int floodedTileCount;
        public float maxDepthReached;
        public float totalEstimatedDamage;
        public float totalPossibleDamage;
        public int damageableTileCount;
    }

    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private WaterSimulator waterSimulator;

    [Header("Damage Settings")]
    [Tooltip("Fallback dollar value assigned to one fully damaged tile when no better per-tile value is available.")]
    [SerializeField] private float maxDamagePerTile = 10f;

    [Tooltip("Minimum water depth before a tile counts as flooded.")]
    [SerializeField] private float floodedThreshold = 0.01f;

    [Tooltip("When enabled, damage persists based on the deepest water each tile has seen.")]
    [SerializeField] private bool usePeakWaterDepth = true;

    [Header("Depth -> Damage Curve (basic)")]
    [Tooltip("At or above this depth, tile reaches full damage.")]
    [SerializeField] private float depthForFullDamage = 2.0f;

    [Tooltip("Optional: only count interior sim cells 1..N.")]
    [SerializeField] private bool useInteriorCellsOnly = true;

    [Header("Per-Tile Value Estimation")]
    [Tooltip("Adds extra exposed value for populated tiles when econVal is only a placeholder.")]
    [SerializeField] private float damageValuePerResident = 0.1f;

    [SerializeField] private float buildingValueMultiplier = 1.0f;
    [SerializeField] private float industrialValueMultiplier = 1.25f;
    [SerializeField] private float infrastructureValueMultiplier = 0.45f;
    [SerializeField] private float landValueMultiplier = 0.2f;

    private float[,] _maxWaterDepthSeen;
    private readonly Dictionary<string, DamageSummary> _zoneDamageSummaries = new();

    public int FloodedTileCount { get; private set; }
    public float MaxDepthReached { get; private set; }
    public float TotalEstimatedDamage { get; private set; }
    public float AverageDamagePercent { get; private set; }

    public event Action OnDamageUpdated;

    private void OnEnable()
    {
        if (waterSimulator != null)
            waterSimulator.OnSimulationStep += RecalculateDamage;
    }

    private void OnDisable()
    {
        if (waterSimulator != null)
            waterSimulator.OnSimulationStep -= RecalculateDamage;
    }

    public void Initialize()
    {
        if (tileMapData == null)
        {
            Debug.LogError("[FloodDamageCalculator] No TileMapData assigned.");
            return;
        }

        _maxWaterDepthSeen = new float[tileMapData.GridWidth, tileMapData.GridHeight];

        int startX = useInteriorCellsOnly ? 1 : 0;
        int endX   = useInteriorCellsOnly ? tileMapData.N : tileMapData.GridWidth - 1;
        int startY = useInteriorCellsOnly ? 1 : 0;
        int endY   = useInteriorCellsOnly ? tileMapData.N : tileMapData.GridHeight - 1;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                _maxWaterDepthSeen[x, y] = tileMapData.water != null
                    ? Mathf.Max(0f, tileMapData.water[x, y])
                    : 0f;
            }
        }

        RecalculateDamage();
    }

    public void RecalculateDamage()
    {
        if (tileMapData == null)
            return;

        if (_maxWaterDepthSeen == null ||
            _maxWaterDepthSeen.GetLength(0) != tileMapData.GridWidth ||
            _maxWaterDepthSeen.GetLength(1) != tileMapData.GridHeight)
        {
            Initialize();
            return;
        }

        int floodedCount = 0;
        float totalDamage = 0f;
        float totalPossibleDamage = 0f;
        float maxDepth = 0f;
        var zoneAccumulators = new Dictionary<string, DamageAccumulator>();

        ClearTileDamageData();

        int startX = useInteriorCellsOnly ? 1 : 0;
        int endX   = useInteriorCellsOnly ? tileMapData.N : tileMapData.GridWidth - 1;
        int startY = useInteriorCellsOnly ? 1 : 0;
        int endY   = useInteriorCellsOnly ? tileMapData.N : tileMapData.GridHeight - 1;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                float currentDepth = tileMapData.water != null
                    ? Mathf.Max(0f, tileMapData.water[x, y])
                    : 0f;

                if (currentDepth > _maxWaterDepthSeen[x, y])
                    _maxWaterDepthSeen[x, y] = currentDepth;

                float damageDepth = usePeakWaterDepth ? _maxWaterDepthSeen[x, y] : currentDepth;

                if (!TryGetTileForSimulationCell(x, y, out var tile))
                    continue;

                if (!IsDamageableTile(tile))
                {
                    tile.damage = 0;
                    continue;
                }

                if (damageDepth >= floodedThreshold)
                    floodedCount++;

                float damagePercent = EvaluateDamagePercent(damageDepth);
                float tileValue = EstimateTileValue(tile);
                float tileDamage = damagePercent * tileValue;
                string geoid = NormalizeGeoid(tile.geoid);

                totalDamage += tileDamage;
                totalPossibleDamage += tileValue;
                tile.damage = Mathf.RoundToInt(tileDamage);

                if (damageDepth > maxDepth)
                    maxDepth = damageDepth;

                if (!string.IsNullOrEmpty(geoid))
                {
                    if (!zoneAccumulators.TryGetValue(geoid, out var zoneAccumulator))
                    {
                        zoneAccumulator = new DamageAccumulator();
                        zoneAccumulators[geoid] = zoneAccumulator;
                    }

                    zoneAccumulator.damageableTileCount++;
                    zoneAccumulator.totalEstimatedDamage += tileDamage;
                    zoneAccumulator.totalPossibleDamage += tileValue;

                    if (damageDepth >= floodedThreshold)
                        zoneAccumulator.floodedTileCount++;

                    if (damageDepth > zoneAccumulator.maxDepthReached)
                        zoneAccumulator.maxDepthReached = damageDepth;
                }
            }
        }

        FloodedTileCount = floodedCount;
        MaxDepthReached = maxDepth;
        TotalEstimatedDamage = totalDamage;
        AverageDamagePercent = totalPossibleDamage > 0f
            ? (totalDamage / totalPossibleDamage) * 100f
            : 0f;
        RebuildZoneDamageSummaries(zoneAccumulators);

        OnDamageUpdated?.Invoke();
    }

    private float EvaluateDamagePercent(float depth)
    {
        if (depth < floodedThreshold)
            return 0f;

        // Simple linear model for now:
        // threshold -> 0 damage, full depth -> 100% damage
        float normalized = Mathf.InverseLerp(floodedThreshold, depthForFullDamage, depth);
        return Mathf.Clamp01(normalized);
    }

    private void ResetMetrics()
    {
        FloodedTileCount = 0;
        MaxDepthReached = 0f;
        TotalEstimatedDamage = 0f;
        AverageDamagePercent = 0f;
        _zoneDamageSummaries.Clear();
    }

    private void ClearTileDamageData()
    {
        for (int y = 0; y < tileMapData.N; y++)
        {
            for (int x = 0; x < tileMapData.N; x++)
            {
                TileInstance tile = tileMapData.Get(new Vector2Int(x, y));
                if (tile != null)
                    tile.damage = 0;
            }
        }
    }

    private bool TryGetTileForSimulationCell(int simX, int simY, out TileInstance tile)
    {
        tile = null;

        int tileX = simX - 1;
        int tileY = simY - 1;

        if (tileX < 0 || tileY < 0 || tileX >= tileMapData.N || tileY >= tileMapData.N)
            return false;

        tile = tileMapData.Get(new Vector2Int(tileX, tileY));
        return tile != null;
    }

    private bool IsDamageableTile(TileInstance tile)
    {
        if (tile == null)
            return false;

        if (tile.tileType != null && tile.tileType.isWater)
            return false;

        string category = NormalizeCategory(tile.category);
        return category != "water";
    }

    private float EstimateTileValue(TileInstance tile)
    {
        if (tile == null)
            return 0f;

        float explicitEconomicValue = Mathf.Max(0f, tile.econVal);
        if (explicitEconomicValue > 1f)
            return explicitEconomicValue;

        float categoryMultiplier = GetCategoryValueMultiplier(tile.category);
        if (categoryMultiplier <= 0f)
            return 0f;

        float baseValue = maxDamagePerTile * categoryMultiplier;
        float populationValue = Mathf.Max(0, tile.population) * damageValuePerResident;
        return Mathf.Max(maxDamagePerTile * 0.1f, baseValue + populationValue);
    }

    private float GetCategoryValueMultiplier(string category)
    {
        switch (NormalizeCategory(category))
        {
            case "building":
            case "residential":
            case "commercial":
            case "city":
                return buildingValueMultiplier;

            case "industrial":
                return industrialValueMultiplier;

            case "road":
            case "highway":
            case "rail":
                return infrastructureValueMultiplier;

            case "land":
            case "park":
            case "forest":
            case "beach":
            case "mountain":
                return landValueMultiplier;

            case "water":
                return 0f;

            default:
                return landValueMultiplier;
        }
    }

    private static string NormalizeCategory(string category)
    {
        return string.IsNullOrWhiteSpace(category)
            ? string.Empty
            : category.Trim().ToLowerInvariant();
    }

    private static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid)
            ? string.Empty
            : geoid.Trim();
    }

    private void RebuildZoneDamageSummaries(Dictionary<string, DamageAccumulator> zoneAccumulators)
    {
        _zoneDamageSummaries.Clear();

        foreach (var pair in zoneAccumulators)
        {
            DamageAccumulator accumulator = pair.Value;
            float averageDamagePercent = accumulator.totalPossibleDamage > 0f
                ? (accumulator.totalEstimatedDamage / accumulator.totalPossibleDamage) * 100f
                : 0f;

            _zoneDamageSummaries[pair.Key] = new DamageSummary(
                pair.Key,
                accumulator.floodedTileCount,
                accumulator.maxDepthReached,
                accumulator.totalEstimatedDamage,
                averageDamagePercent,
                accumulator.damageableTileCount);
        }
    }

    public bool TryGetZoneDamageSummary(string geoid, out DamageSummary summary)
    {
        string normalizedGeoid = NormalizeGeoid(geoid);
        if (!string.IsNullOrEmpty(normalizedGeoid) &&
            _zoneDamageSummaries.TryGetValue(normalizedGeoid, out summary))
        {
            return true;
        }

        summary = default;
        return false;
    }

    public string GetSeverityLabel(DamageSummary summary)
    {
        return GetSeverityLabel(summary.AverageDamagePercent);
    }

    public string GetSeverityLabel(float averageDamagePercent)
    {
        if (averageDamagePercent < 10f) return "Low";
        if (averageDamagePercent < 35f) return "Moderate";
        if (averageDamagePercent < 65f) return "High";
        return "Severe";
    }

    public string GetSeverityLabel()
    {
        return GetSeverityLabel(AverageDamagePercent);
    }
}
