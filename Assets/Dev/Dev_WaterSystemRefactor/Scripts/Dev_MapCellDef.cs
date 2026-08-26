using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Persistent data for one logical map cell. Coordinates are implicit in the
/// cell's position in Dev_MapDef; mutable water values live in Dev_WaterState.
/// </summary>
[Serializable]
public sealed class Dev_MapCellDef
{
    [SerializeField] private bool exists;
    [SerializeField] private int elevation;
    [SerializeField] private Dev_TerrainTypeDef terrain;
    [SerializeField, Min(0f)] private float initialWaterDepth;
    [FormerlySerializedAs("initialWaterSource")]
    [SerializeField] private bool isInitialWaterBody;

    public bool Exists => exists;
    public int Elevation => elevation;
    public Dev_TerrainTypeDef Terrain => terrain;
    public float InitialWaterDepth => Mathf.Max(0f, initialWaterDepth);
    public bool IsInitialWaterBody => isInitialWaterBody;

    public void Configure(
        bool cellExists,
        int cellElevation,
        Dev_TerrainTypeDef terrainDefinition,
        float waterDepth,
        bool initialWaterBody)
    {
        exists = cellExists;
        elevation = cellElevation;
        terrain = terrainDefinition;
        initialWaterDepth = Mathf.Max(0f, waterDepth);
        isInitialWaterBody = initialWaterBody;
    }
}
