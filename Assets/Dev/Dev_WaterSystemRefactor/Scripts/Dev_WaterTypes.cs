using System;
using UnityEngine;

// Shared configuration and value types used by the Dev Water System's controller and simulation engine.

/// <summary>Chooses whether the Dev Water System advances manually for testing or on an automatic interval.</summary>
public enum Dev_WaterStepMode
{
    Manual,
    Automatic
}

/// <summary>Identifies the map region that a configured water source affects.</summary>
public enum Dev_WaterSourceKind
{
    FullMap,
    Edges,
    Corners,
    ExistingWaterBodies,
    Rainfall,
    Boundary
}

/// <summary>Contains validated numerical settings for one Dev Water System simulation run.</summary>
[Serializable]
public class Dev_WaterSimulationSettings
{
    [Header("Physics")]
    [Min(0.01f)] public float dx = 1f;
    [Min(0.01f)] public float dy = 1f;
    [Min(0.001f)] public float dt = 0.25f;
    [Min(0f)] public float gravity = 9.81f;
    [Range(0f, 0.99f)] public float friction = 0.02f;
    [Min(0f)] public float maxWaterDepth = 100f;

    [Header("Boundary")]
    public bool useBoundaryWalls = true;
    [Min(0f)] public float boundaryHeightPadding = 2f;

    [Header("Spread Gating")]
    public bool useSpreadGating = true;
    [Min(0.05f)] public float spreadInterval = 2f;
    [Min(1)] public int spreadLayersPerTick = 1;
    [Min(0f)] public float expandFromWaterThreshold = 0.001f;
    public bool expandOnceImmediatelyOnStart = true;

    [Header("Drainage")]
    [Tooltip("Base depth removed per simulated second before Drainage Efficiency is applied.")]
    [Min(0f)] public float baseDrainageDepthPerSecond = 0f;

    [Header("Wind")]
    [Tooltip("Flow acceleration bias applied from Wind Stress and Wind Direction.")]
    [Min(0f)] public float windForceScale = 0f;

    [Header("Barriers")]
    [Tooltip("How far above barrier height water must be before full flow resumes.")]
    [Min(0.01f)] public float overtopDepthForFullFlow = 1f;

    public Dev_WaterSimulationSettings Clone()
    {
        return (Dev_WaterSimulationSettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        dx = Mathf.Max(0.01f, dx);
        dy = Mathf.Max(0.01f, dy);
        dt = Mathf.Max(0.001f, dt);
        gravity = Mathf.Max(0f, gravity);
        friction = Mathf.Clamp(friction, 0f, 0.99f);
        maxWaterDepth = Mathf.Max(0f, maxWaterDepth);
        boundaryHeightPadding = Mathf.Max(0f, boundaryHeightPadding);
        spreadInterval = Mathf.Max(0.05f, spreadInterval);
        spreadLayersPerTick = Mathf.Max(1, spreadLayersPerTick);
        expandFromWaterThreshold = Mathf.Max(0f, expandFromWaterThreshold);
        baseDrainageDepthPerSecond = Mathf.Max(0f, baseDrainageDepthPerSecond);
        windForceScale = Mathf.Max(0f, windForceScale);
        overtopDepthForFullFlow = Mathf.Max(0.01f, overtopDepthForFullFlow);
    }
}

/// <summary>Describes one initial or continuous water source used by the simulation engine.</summary>
[Serializable]
public class Dev_WaterSourceSpec
{
    public Dev_WaterSourceKind kind = Dev_WaterSourceKind.ExistingWaterBodies;

    [Tooltip("Initial sources use this as absolute depth. Continuous sources use this as depth per second.")]
    [Min(0f)] public float depth = 10f;

    public bool scaleByRainfallRate;
    public bool scaleByExternalWaterLoad = true;
    public bool scaleByAntecedentWetness;

    public Dev_WaterSourceSpec Clone()
    {
        return (Dev_WaterSourceSpec)MemberwiseClone();
    }
}

/// <summary>Captures external modifier values for a water step; defaults are used until the real modifier system exists.</summary>
[Serializable]
public struct Dev_WaterModifierSnapshot
{
    public float DrainageEfficiency;
    public float RainfallRate;
    public float AntecedentWetness;
    public float ExternalWaterLoad;
    public float WindStress;
    public Vector2 WindDirection;
    public float EventPacing;

    public static Dev_WaterModifierSnapshot Defaults()
    {
        return new Dev_WaterModifierSnapshot
        {
            DrainageEfficiency = 1f,
            RainfallRate = 1f,
            AntecedentWetness = 1f,
            ExternalWaterLoad = 1f,
            WindStress = 0f,
            WindDirection = Vector2.right,
            EventPacing = 1f
        };
    }

    public void Sanitize()
    {
        DrainageEfficiency = Mathf.Max(0f, DrainageEfficiency);
        RainfallRate = Mathf.Max(0f, RainfallRate);
        AntecedentWetness = Mathf.Max(0f, AntecedentWetness);
        ExternalWaterLoad = Mathf.Max(0f, ExternalWaterLoad);
        WindStress = Mathf.Max(0f, WindStress);
        EventPacing = Mathf.Max(0.01f, EventPacing);

        if (WindDirection.sqrMagnitude <= 0.0001f)
            WindDirection = Vector2.right;
        else
            WindDirection.Normalize();
    }
}

/// <summary>Reports the aggregate result of the most recent Dev Water System simulation step.</summary>
public struct Dev_WaterStepSummary
{
    public int StepIndex;
    public float DeltaTime;
    public int WetTileCount;
    public int DirtyTileCount;
    public float TotalWater;
    public float MaxDepth;
}
