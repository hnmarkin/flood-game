using System;
using UnityEngine;

public class FloodDamageCalculator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private WaterSimulator waterSimulator;

    [Header("Damage Settings")]
    [Tooltip("Dollar value assigned to one tile at 100% damage.")]
    [SerializeField] private float maxDamagePerTile = 10f;

    [Tooltip("Minimum water depth before a tile counts as flooded.")]
    [SerializeField] private float floodedThreshold = 0.01f;

    [Header("Depth -> Damage Curve (basic)")]
    [Tooltip("At or above this depth, tile reaches full damage.")]
    [SerializeField] private float depthForFullDamage = 2.0f;

    [Tooltip("Optional: only count interior sim cells 1..N.")]
    [SerializeField] private bool useInteriorCellsOnly = true;

    private float[,] _maxWaterDepthSeen;

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
                _maxWaterDepthSeen[x, y] = Mathf.Max(0f, tileMapData.water[x, y]);
            }
        }

        RecalculateDamage();
    }

    public void RecalculateDamage()
    {
        if (tileMapData == null || tileMapData.water == null)
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
        float totalDamagePercent = 0f;
        float maxDepth = 0f;
        int countedTiles = 0;

        int startX = useInteriorCellsOnly ? 1 : 0;
        int endX   = useInteriorCellsOnly ? tileMapData.N : tileMapData.GridWidth - 1;
        int startY = useInteriorCellsOnly ? 1 : 0;
        int endY   = useInteriorCellsOnly ? tileMapData.N : tileMapData.GridHeight - 1;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                float currentDepth = Mathf.Max(0f, tileMapData.water[x, y]);

                if (currentDepth > _maxWaterDepthSeen[x, y])
                    _maxWaterDepthSeen[x, y] = currentDepth;

                float visualDamageDepth = _maxWaterDepthSeen[x, y];

                if (visualDamageDepth >= floodedThreshold)
                    floodedCount++;

                float damagePercent = EvaluateDamagePercent(visualDamageDepth);
                float tileDamage = damagePercent * maxDamagePerTile;

                totalDamage += tileDamage;
                totalDamagePercent += damagePercent;
                countedTiles++;

                if (visualDamageDepth > maxDepth)
                    maxDepth = visualDamageDepth;
            }
        }

        FloodedTileCount = floodedCount;
        MaxDepthReached = maxDepth;
        TotalEstimatedDamage = totalDamage;
        AverageDamagePercent = countedTiles > 0
            ? (totalDamagePercent / countedTiles) * 100f
            : 0f;

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

    public string GetSeverityLabel()
    {
        if (AverageDamagePercent < 10f) return "Low";
        if (AverageDamagePercent < 35f) return "Moderate";
        if (AverageDamagePercent < 65f) return "High";
        return "Severe";
    }
}