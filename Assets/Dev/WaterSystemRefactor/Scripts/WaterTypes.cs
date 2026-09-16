using System;
using UnityEngine;

// Shared configuration and value types used by the Dev Water System's controller and simulation engine.

/// <summary>Chooses whether the Dev Water System advances manually for testing or on an automatic interval.</summary>
public enum WaterStepMode
{
    Manual,
    Automatic
}

/// <summary>Lifecycle values supplied by Game State to the Dev water lifecycle seam.</summary>
public enum WaterGameFlow
{
    Loading,
    Gameplay,
    Pause,
    MainMenu
}

/// <summary>Phase values supplied by Game State to the Dev water lifecycle seam.</summary>
public enum WaterGamePhase
{
    Preparation,
    Crisis,
    Scoring
}

/// <summary>Identifies the scenario-owned storm profile currently driving water physics.</summary>
public enum WaterProfileStage
{
    Baseline,
    Preliminary,
    Crisis
}

/// <summary>Controls whether missing integration contracts are fatal or explicitly Dev-only.</summary>
public enum WaterConfigurationMode
{
    Production,
    DevDefaultsWithWarnings
}

/// <summary>Defines what exists immediately beyond one logical map edge.</summary>
public enum WaterBoundaryMode
{
    Wall,
    Source,
    Sink
}

/// <summary>Identifies one of the four logical map edges.</summary>
public enum WaterBoundarySide
{
    North,
    East,
    South,
    West
}

/// <summary>Identifies the map region that a configured water source affects.</summary>
public enum WaterSourceKind
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
public class WaterSimulationSettings
{
    [Header("Physics")]
    [Min(0.01f)] public float dx = 1f;
    [Min(0.01f)] public float dy = 1f;
    [Min(0.001f)] public float dt = 0.25f;
    [Min(0f)] public float gravity = 9.81f;
    [Range(0f, 0.99f)] public float friction = 0.02f;
    [Min(0f)] public float maxWaterDepth = 100f;

    [Header("Boundary")]
    // Retained for deserializing older scenario assets. New assets should configure each edge below.
    public bool useBoundaryWalls = true;
    [Min(0f)] public float boundaryHeightPadding = 2f;
    public WaterBoundarySettings northBoundary = new WaterBoundarySettings();
    public WaterBoundarySettings eastBoundary = new WaterBoundarySettings();
    public WaterBoundarySettings southBoundary = new WaterBoundarySettings();
    public WaterBoundarySettings westBoundary = new WaterBoundarySettings();

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

    public WaterSimulationSettings Clone()
    {
        WaterSimulationSettings clone = (WaterSimulationSettings)MemberwiseClone();
        clone.northBoundary = northBoundary != null ? northBoundary.Clone() : null;
        clone.eastBoundary = eastBoundary != null ? eastBoundary.Clone() : null;
        clone.southBoundary = southBoundary != null ? southBoundary.Clone() : null;
        clone.westBoundary = westBoundary != null ? westBoundary.Clone() : null;
        return clone;
    }

    public void Sanitize()
    {
        dx = IsFinitePositive(dx) ? Mathf.Max(0.01f, dx) : 1f;
        dy = IsFinitePositive(dy) ? Mathf.Max(0.01f, dy) : 1f;
        dt = IsFinitePositive(dt) ? Mathf.Max(0.001f, dt) : 0.25f;
        gravity = IsFiniteNonNegative(gravity) ? gravity : 0f;
        friction = IsFiniteNonNegative(friction) ? Mathf.Clamp(friction, 0f, 0.99f) : 0f;
        maxWaterDepth = IsFiniteNonNegative(maxWaterDepth) ? maxWaterDepth : 0f;
        boundaryHeightPadding = IsFiniteNonNegative(boundaryHeightPadding) ? boundaryHeightPadding : 0f;
        spreadInterval = IsFinitePositive(spreadInterval) ? Mathf.Max(0.05f, spreadInterval) : 2f;
        spreadLayersPerTick = Mathf.Max(1, spreadLayersPerTick);
        expandFromWaterThreshold = IsFiniteNonNegative(expandFromWaterThreshold) ? expandFromWaterThreshold : 0f;
        baseDrainageDepthPerSecond = IsFiniteNonNegative(baseDrainageDepthPerSecond) ? baseDrainageDepthPerSecond : 0f;
        windForceScale = IsFiniteNonNegative(windForceScale) ? windForceScale : 0f;
        overtopDepthForFullFlow = IsFinitePositive(overtopDepthForFullFlow) ? Mathf.Max(0.01f, overtopDepthForFullFlow) : 1f;

        EnsureBoundarySettings();
        NormalizeLegacyBoundarySetting();
        northBoundary.Sanitize();
        eastBoundary.Sanitize();
        southBoundary.Sanitize();
        westBoundary.Sanitize();
    }

    public bool IsValid(out string error)
    {
        if (!IsFinitePositive(dx) || !IsFinitePositive(dy) || !IsFinitePositive(dt))
        {
            error = "dx, dy, and dt must be finite positive values.";
            return false;
        }

        if (!IsFiniteNonNegative(gravity) || !IsFiniteNonNegative(friction) || friction > 0.99f ||
            !IsFiniteNonNegative(maxWaterDepth) || !IsFiniteNonNegative(boundaryHeightPadding) ||
            !IsFinitePositive(spreadInterval) || !IsFiniteNonNegative(expandFromWaterThreshold) ||
            !IsFiniteNonNegative(baseDrainageDepthPerSecond) || !IsFiniteNonNegative(windForceScale) ||
            !IsFinitePositive(overtopDepthForFullFlow) || spreadLayersPerTick < 1)
        {
            error = "Simulation settings contain an invalid finite value.";
            return false;
        }

        if (!IsValidBoundary(northBoundary, "north", out error) ||
            !IsValidBoundary(eastBoundary, "east", out error) ||
            !IsValidBoundary(southBoundary, "south", out error) ||
            !IsValidBoundary(westBoundary, "west", out error))
            return false;

        error = null;
        return true;
    }

    public WaterBoundarySettings GetBoundary(WaterBoundarySide side)
    {
        EnsureBoundarySettings();
        switch (side)
        {
            case WaterBoundarySide.North: return northBoundary;
            case WaterBoundarySide.East: return eastBoundary;
            case WaterBoundarySide.South: return southBoundary;
            case WaterBoundarySide.West: return westBoundary;
            default: return northBoundary;
        }
    }

    private void EnsureBoundarySettings()
    {
        if (northBoundary == null) northBoundary = new WaterBoundarySettings();
        if (eastBoundary == null) eastBoundary = new WaterBoundarySettings();
        if (southBoundary == null) southBoundary = new WaterBoundarySettings();
        if (westBoundary == null) westBoundary = new WaterBoundarySettings();
    }

    private void NormalizeLegacyBoundarySetting()
    {
        if (useBoundaryWalls ||
            northBoundary.mode != WaterBoundaryMode.Wall ||
            eastBoundary.mode != WaterBoundaryMode.Wall ||
            southBoundary.mode != WaterBoundaryMode.Wall ||
            westBoundary.mode != WaterBoundaryMode.Wall)
            return;

        northBoundary.mode = WaterBoundaryMode.Sink;
        eastBoundary.mode = WaterBoundaryMode.Sink;
        southBoundary.mode = WaterBoundaryMode.Sink;
        westBoundary.mode = WaterBoundaryMode.Sink;
    }

    private static bool IsValidBoundary(WaterBoundarySettings boundary, string name, out string error)
    {
        if (boundary == null)
        {
            error = $"The {name} boundary settings are missing.";
            return false;
        }

        if (!boundary.IsValid(out error))
        {
            error = $"The {name} boundary settings are invalid: {error}";
            return false;
        }

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

/// <summary>Configuration for one external edge of the simulation.</summary>
[Serializable]
public sealed class WaterBoundarySettings
{
    public WaterBoundaryMode mode = WaterBoundaryMode.Wall;

    [Tooltip("Depth added per simulated second when this edge is a limitless source.")]
    [Min(0f)] public float sourceDepthPerSecond;

    [Tooltip("Depth removed per simulated second through a Wall edge. Ignored for Source and Sink edges.")]
    [Min(0f)] public float seepageDepthPerSecond;

    public WaterBoundarySettings Clone()
    {
        return (WaterBoundarySettings)MemberwiseClone();
    }

    public void Sanitize()
    {
        if (!Enum.IsDefined(typeof(WaterBoundaryMode), mode))
            mode = WaterBoundaryMode.Wall;

        sourceDepthPerSecond = IsFiniteNonNegative(sourceDepthPerSecond)
            ? sourceDepthPerSecond
            : 0f;
        seepageDepthPerSecond = IsFiniteNonNegative(seepageDepthPerSecond)
            ? seepageDepthPerSecond
            : 0f;
    }

    public bool IsValid(out string error)
    {
        if (!Enum.IsDefined(typeof(WaterBoundaryMode), mode))
        {
            error = "Boundary mode is unknown.";
            return false;
        }

        if (!IsFiniteNonNegative(sourceDepthPerSecond) || !IsFiniteNonNegative(seepageDepthPerSecond))
        {
            error = "Boundary source and seepage depths must be finite and non-negative.";
            return false;
        }

        if (mode == WaterBoundaryMode.Source && sourceDepthPerSecond <= 0f)
        {
            error = "A source boundary requires a positive source depth per second.";
            return false;
        }

        if (mode != WaterBoundaryMode.Source && sourceDepthPerSecond > 0f)
        {
            error = "Source depth is only valid for a Source boundary.";
            return false;
        }

        if (mode != WaterBoundaryMode.Wall && seepageDepthPerSecond > 0f)
        {
            error = "Seepage depth is only valid for a Wall boundary.";
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

/// <summary>Describes one initial or continuous water source used by the simulation engine.</summary>
[Serializable]
public class WaterSourceSpec
{
    public WaterSourceKind kind = WaterSourceKind.ExistingWaterBodies;

    [Tooltip("Absolute water depth added when this is used as an initial source.")]
    [Min(0f)] public float initialDepth;

    [Tooltip("Water depth added per simulated second when this is used as a continuous source.")]
    [Min(0f)] public float continuousDepthPerSecond;

    public bool scaleByRainfallRate;
    public bool scaleByExternalWaterLoad = true;
    public bool scaleByAntecedentWetness;

    public WaterSourceSpec Clone()
    {
        return (WaterSourceSpec)MemberwiseClone();
    }

    public bool IsValid(out string error)
    {
        if (!Enum.IsDefined(typeof(WaterSourceKind), kind))
        {
            error = "Source kind is unknown.";
            return false;
        }

        if (!IsFiniteNonNegative(initialDepth) || !IsFiniteNonNegative(continuousDepthPerSecond))
        {
            error = "Initial and continuous source depths must be finite and non-negative.";
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

/// <summary>Complete persistent water behavior for one scenario storm stage.</summary>
[Serializable]
public class WaterStormProfile
{
    [SerializeField] private string profileName;
    [SerializeField] private WaterSimulationSettings simulationSettings = new WaterSimulationSettings();
    [SerializeField] private WaterSourceSpec[] continuousSources = Array.Empty<WaterSourceSpec>();

    public string ProfileName => string.IsNullOrWhiteSpace(profileName) ? "Unnamed profile" : profileName;

    public WaterSimulationSettings CreateSettingsInstance()
    {
        return simulationSettings != null ? simulationSettings.Clone() : null;
    }

    public WaterSourceSpec[] CreateContinuousSourceInstances()
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
            foreach (WaterSourceSpec source in continuousSources)
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

    internal static WaterSourceSpec[] CloneSources(WaterSourceSpec[] sources)
    {
        if (sources == null || sources.Length == 0)
            return Array.Empty<WaterSourceSpec>();

        WaterSourceSpec[] clones = new WaterSourceSpec[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            clones[i] = sources[i] != null ? sources[i].Clone() : null;

        return clones;
    }
}

/// <summary>One optional, scenario-authored preliminary flooding batch.</summary>
[Serializable]
public class WaterPreliminaryFloodingConfig
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
public struct WaterModifierSnapshot
{
    public float DrainageEfficiency;
    public float RainfallRate;
    public float AntecedentWetness;
    public float ExternalWaterLoad;
    public float WindStress;
    public Vector2 WindDirection;
    public float EventPacing;

    public static WaterModifierSnapshot Defaults()
    {
        return new WaterModifierSnapshot
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
        DrainageEfficiency = IsFiniteNonNegative(DrainageEfficiency) ? DrainageEfficiency : 1f;
        RainfallRate = IsFiniteNonNegative(RainfallRate) ? RainfallRate : 1f;
        AntecedentWetness = IsFiniteNonNegative(AntecedentWetness) ? AntecedentWetness : 1f;
        ExternalWaterLoad = IsFiniteNonNegative(ExternalWaterLoad) ? ExternalWaterLoad : 1f;
        WindStress = IsFiniteNonNegative(WindStress) ? WindStress : 0f;
        EventPacing = IsFinitePositive(EventPacing) ? Mathf.Max(0.01f, EventPacing) : 1f;

        if (!IsFinite(WindDirection.x) || !IsFinite(WindDirection.y) || WindDirection.sqrMagnitude <= 0.0001f)
            WindDirection = Vector2.right;
        else
            WindDirection.Normalize();
    }

    public bool IsValid(out string error)
    {
        if (!IsFiniteNonNegative(DrainageEfficiency) || !IsFiniteNonNegative(RainfallRate) ||
            !IsFiniteNonNegative(AntecedentWetness) || !IsFiniteNonNegative(ExternalWaterLoad) ||
            !IsFiniteNonNegative(WindStress) || !IsFinitePositive(EventPacing) ||
            !IsFinite(WindDirection.x) || !IsFinite(WindDirection.y))
        {
            error = "Modifier values must be finite and within their documented bounds.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsFinitePositive(float value)
    {
        return IsFinite(value) && value > 0f;
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return IsFinite(value) && value >= 0f;
    }
}

/// <summary>Adapter implemented by the public modifier controller used by water.</summary>
public interface IWaterModifierProvider
{
    bool TryGetResolvedWaterModifiers(out WaterModifierSnapshot modifiers, out string error);
}

/// <summary>Reports the aggregate result of the most recent Dev Water System simulation step.</summary>
public struct WaterStepSummary
{
    public int StepIndex;
    public float DeltaTime;
    public int WetTileCount;
    public int DirtyTileCount;
    public float TotalWater;
    public float MaxDepth;
}

/// <summary>Immutable water result produced for projection consumers.</summary>
public sealed class WaterProjection
{
    private readonly float[] _waterDepths;

    public WaterProjection(
        Vector2Int origin,
        int width,
        int height,
        WaterProfileStage profileStage,
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
    public WaterProfileStage ProfileStage { get; }
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
