using System;
using UnityEngine;

/// <summary>
/// Persistent data for one logical water-map cell. Coordinates are implicit in the
/// cell's position in Dev_WaterMapData; mutable simulation values live elsewhere.
/// </summary>
[Serializable]
public sealed class Dev_WaterMapCell
{
    [SerializeField] private bool exists;
    [SerializeField] private int elevation;
    [SerializeField] private Dev_WaterTerrainDefinition terrain;
    [SerializeField, Min(0f)] private float initialWaterDepth;
    [SerializeField] private bool initialWaterSource;

    public bool Exists => exists;
    public int Elevation => elevation;
    public Dev_WaterTerrainDefinition Terrain => terrain;
    public float InitialWaterDepth => Mathf.Max(0f, initialWaterDepth);
    public bool InitialWaterSource => initialWaterSource;

    public void Configure(
        bool cellExists,
        int cellElevation,
        Dev_WaterTerrainDefinition terrainDefinition,
        float waterDepth,
        bool waterSource)
    {
        exists = cellExists;
        elevation = cellElevation;
        terrain = terrainDefinition;
        initialWaterDepth = Mathf.Max(0f, waterDepth);
        initialWaterSource = waterSource;
    }
}
