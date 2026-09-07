using UnityEngine;

/// <summary>
/// Persistent, scenario-owned water profiles and the optional preliminary flooding batch.
/// The controller clones this data; live simulation never writes back to this asset.
/// </summary>
[CreateAssetMenu(fileName = "ScenarioDef", menuName = "Dev/Water System/Scenario Definition")]
public class ScenarioDef : ScriptableObject
{
    [Header("Storm Profiles")]
    [SerializeField] private WaterStormProfile baselineProfile = new WaterStormProfile();
    [SerializeField] private WaterStormProfile preliminaryProfile = new WaterStormProfile();
    [SerializeField] private WaterStormProfile crisisProfile = new WaterStormProfile();

    [Header("Baseline Initial Water")]
    [SerializeField] private WaterSourceSpec[] initialSources =
    {
        new WaterSourceSpec
        {
            kind = WaterSourceKind.ExistingWaterBodies,
            initialDepth = 10f,
            scaleByExternalWaterLoad = true
        }
    };

    [Header("Optional Preliminary Flooding")]
    [SerializeField] private bool hasPreliminaryFlooding;
    [SerializeField] private WaterPreliminaryFloodingConfig preliminaryFlooding = new WaterPreliminaryFloodingConfig();

    public bool TryCreateProfile(
        WaterProfileStage stage,
        out WaterSimulationSettings settings,
        out WaterSourceSpec[] continuousSources,
        out string error)
    {
        WaterStormProfile profile = GetProfile(stage);
        settings = null;
        continuousSources = System.Array.Empty<WaterSourceSpec>();

        if (profile == null)
        {
            error = $"The {stage} water profile is missing.";
            return false;
        }

        if (!profile.IsValid(out error))
        {
            error = $"The {stage} water profile is invalid: {error}";
            return false;
        }

        settings = profile.CreateSettingsInstance();
        continuousSources = profile.CreateContinuousSourceInstances();
        error = null;
        return true;
    }

    public WaterSourceSpec[] CreateInitialSourceInstances()
    {
        return WaterStormProfile.CloneSources(initialSources);
    }

    public bool TryGetPreliminaryFlooding(out WaterPreliminaryFloodingConfig configuration, out string error)
    {
        configuration = preliminaryFlooding;
        if (!hasPreliminaryFlooding)
        {
            error = "This scenario does not configure preliminary flooding.";
            return false;
        }

        if (configuration == null)
        {
            error = "Preliminary flooding configuration is missing.";
            return false;
        }

        if (!configuration.IsValid(out error))
            return false;

        error = null;
        return true;
    }

    public bool IsValidForProduction(out string error)
    {
        foreach (WaterProfileStage stage in System.Enum.GetValues(typeof(WaterProfileStage)))
        {
            if (!TryCreateProfile(stage, out _, out _, out error))
                return false;
        }

        if (initialSources != null)
        {
            foreach (WaterSourceSpec source in initialSources)
            {
                if (source == null)
                {
                    error = "An initial water source is missing.";
                    return false;
                }

                if (!source.IsValid(out error))
                    return false;
            }
        }

        if (hasPreliminaryFlooding && !TryGetPreliminaryFlooding(out _, out error))
            return false;

        error = null;
        return true;
    }

    private WaterStormProfile GetProfile(WaterProfileStage stage)
    {
        switch (stage)
        {
            case WaterProfileStage.Baseline: return baselineProfile;
            case WaterProfileStage.Preliminary: return preliminaryProfile;
            case WaterProfileStage.Crisis: return crisisProfile;
            default: return null;
        }
    }
}
