using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Persistent data for one logical map cell. Coordinates are implicit in the
/// cell's position in MapDef; mutable water values live in WaterState.
/// </summary>
[Serializable]
public sealed class MapCellDef
{
    [SerializeField] private bool exists;
    [SerializeField] private int elevation;
    [SerializeField] private TerrainTypeDef terrain;
    [SerializeField, Min(0f)] private float initialWaterDepth;
    [FormerlySerializedAs("initialWaterSource")]
    [SerializeField] private bool isInitialWaterBody;

    public bool Exists => exists;
    public int Elevation => elevation;
    public TerrainTypeDef Terrain => terrain;
    public float InitialWaterDepth => IsFiniteNonNegative(initialWaterDepth)
        ? Mathf.Max(0f, initialWaterDepth)
        : 0f;
    public bool IsInitialWaterBody => isInitialWaterBody;

    public bool IsValidForProduction(out string error)
    {
        if (!exists)
        {
            error = "The map cell is not marked as existing.";
            return false;
        }

        if (terrain == null)
        {
            error = "The map cell has no terrain definition.";
            return false;
        }

        if (!terrain.IsValidForProduction(out error))
            return false;

        if (!IsFiniteNonNegative(initialWaterDepth))
        {
            error = "Initial water depth must be finite and non-negative.";
            return false;
        }

        if (!terrain.ParticipatesInSimulation && (initialWaterDepth > 0f || isInitialWaterBody))
        {
            error = "Non-simulating terrain cannot contain initial water.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
