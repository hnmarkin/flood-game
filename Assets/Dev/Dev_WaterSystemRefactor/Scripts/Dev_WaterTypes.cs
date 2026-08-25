using System;
using UnityEngine;

// Shared configuration and value types used by the Dev Water System's controller and simulation engine.

/// <summary>Chooses whether the Dev Water System advances manually for testing or on an automatic interval.</summary>
public enum Dev_WaterStepMode
{
    Manual,
    Automatic
}

/// <summary>Lifecycle values supplied by Game State to the Dev water lifecycle seam.</summary>
public enum Dev_WaterGameFlow
{
    Loading,
    Gameplay,
    Pause,
    MainMenu
}

/// <summary>Phase values supplied by Game State to the Dev water lifecycle seam.</summary>
public enum Dev_WaterGamePhase
{
    Preparation,
    Crisis,
    Scoring
}

/// <summary>Identifies the scenario-owned storm profile currently driving water physics.</summary>
public enum Dev_WaterProfileStage
{
    Baseline,
    Preliminary,
    Crisis
}

/// <summary>Controls whether missing integration contracts are fatal or explicitly Dev-only.</summary>
public enum Dev_WaterConfigurationMode
{
    Production,
    DevDefaultsWithWarnings
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

    public bool IsValid(out string error)
    {
        if (!IsFinitePositive(dx) || !IsFinitePositive(dy) || !IsFinitePositive(dt))
        {
            error = "dx, dy, and dt must be finite positive values.";
            return false;
        }

        if (!IsFiniteNonNegative(gravity) || !IsFiniteNonNegative(friction) ||
            !IsFiniteNonNegative(maxWaterDepth) || !IsFiniteNonNegative(boundaryHeightPadding) ||
            !IsFinitePositive(spreadInterval) || !IsFiniteNonNegative(expandFromWaterThreshold) ||
            !IsFiniteNonNegative(baseDrainageDepthPerSecond) || !IsFiniteNonNegative(windForceScale) ||
            !IsFinitePositive(overtopDepthForFullFlow) || spreadLayersPerTick < 1)
        {
            error = "Simulation settings contain an invalid finite value.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
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

    public bool IsValid(out string error)
    {
        if (float.IsNaN(depth) || float.IsInfinity(depth) || depth < 0f)
        {
            error = "Source depth must be finite and non-negative.";
            return false;
        }

        error = null;
        return true;
    }
}

/// <summary>Complete persistent water behavior for one scenario storm stage.</summary>
[Serializable]
public class Dev_WaterStormProfile
{
    [SerializeField] private string profileName;
    [SerializeField] private Dev_WaterSimulationSettings simulationSettings = new Dev_WaterSimulationSettings();
    [SerializeField] private Dev_WaterSourceSpec[] continuousSources = Array.Empty<Dev_WaterSourceSpec>();

    public string ProfileName => string.IsNullOrWhiteSpace(profileName) ? "Unnamed profile" : profileName;

    public Dev_WaterSimulationSettings CreateSettingsInstance()
    {
        return simulationSettings != null ? simulationSettings.Clone() : null;
    }

    public Dev_WaterSourceSpec[] CreateContinuousSourceInstances()
    {
        return CloneSources(continuousSources);
    }

    public bool IsValid(out string error)
    {
        if (simulationSettings == null)
        {
            error = "Simulation settings are missing.";
            return false;
        }

        if (!simulationSettings.IsValid(out error))
            return false;

        if (continuousSources != null)
        {
            foreach (Dev_WaterSourceSpec source in continuousSources)
            {
                if (source == null || !source.IsValid(out error))
                {
                    error = source == null ? "A continuous source is missing." : error;
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    internal static Dev_WaterSourceSpec[] CloneSources(Dev_WaterSourceSpec[] sources)
    {
        if (sources == null || sources.Length == 0)
            return Array.Empty<Dev_WaterSourceSpec>();

        Dev_WaterSourceSpec[] clones = new Dev_WaterSourceSpec[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            clones[i] = sources[i] != null ? sources[i].Clone() : null;

        return clones;
    }
}

/// <summary>One optional, scenario-authored preliminary flooding batch.</summary>
[Serializable]
public class Dev_WaterPreliminaryFloodingConfig
{
    [Min(1)] [SerializeField] private int completedPreparationTurnThreshold = 1;
    [Min(0.001f)] [SerializeField] private float simulatedDuration = 1f;

    public int CompletedPreparationTurnThreshold => completedPreparationTurnThreshold;
    public float SimulatedDuration => simulatedDuration;

    public bool IsValid(out string error)
    {
        if (completedPreparationTurnThreshold < 1 || float.IsNaN(simulatedDuration) ||
            float.IsInfinity(simulatedDuration) || simulatedDuration <= 0f)
        {
            error = "The preliminary flooding threshold and simulated duration must be positive.";
            return false;
        }

        error = null;
        return true;
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

/// <summary>Adapter implemented by the public modifier controller used by water.</summary>
public interface IDev_WaterModifierProvider
{
    bool TryGetResolvedWaterModifiers(out Dev_WaterModifierSnapshot modifiers, out string error);
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

/// <summary>Immutable water result produced for projection consumers.</summary>
public sealed class Dev_WaterProjection
{
    private readonly float[] _waterDepths;

    public Dev_WaterProjection(
        Vector2Int origin,
        int width,
        int height,
        Dev_WaterProfileStage profileStage,
        float simulatedDuration,
        float[] waterDepths)
    {
        Origin = origin;
        Width = width;
        Height = height;
        ProfileStage = profileStage;
        SimulatedDuration = simulatedDuration;
        _waterDepths = waterDepths != null ? (float[])waterDepths.Clone() : Array.Empty<float>();
    }

    public Vector2Int Origin { get; }
    public int Width { get; }
    public int Height { get; }
    public Dev_WaterProfileStage ProfileStage { get; }
    public float SimulatedDuration { get; }

    public float GetWaterDepth(Vector2Int tileCell)
    {
        int x = tileCell.x - Origin.x;
        int y = tileCell.y - Origin.y;
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return 0f;

        return _waterDepths[y * Width + x];
    }
}
