using UnityEngine;

/// <summary>
/// Persistent, scenario-owned water profiles and the optional preliminary flooding batch.
/// The controller clones this data; live simulation never writes back to this asset.
/// </summary>
[CreateAssetMenu(fileName = "Dev_WaterScenarioConfig", menuName = "Dev/Water System/Scenario Config")]
public class Dev_WaterScenarioConfig : ScriptableObject
{
    [Header("Storm Profiles")]
    [SerializeField] private Dev_WaterStormProfile baselineProfile = new Dev_WaterStormProfile();
    [SerializeField] private Dev_WaterStormProfile preliminaryProfile = new Dev_WaterStormProfile();
    [SerializeField] private Dev_WaterStormProfile crisisProfile = new Dev_WaterStormProfile();

    [Header("Baseline Initial Water")]
    [SerializeField] private Dev_WaterSourceSpec[] initialSources =
    {
        new Dev_WaterSourceSpec
        {
            kind = Dev_WaterSourceKind.ExistingWaterBodies,
            depth = 10f,
            scaleByExternalWaterLoad = true
        }
    };

    [Header("Optional Preliminary Flooding")]
    [SerializeField] private bool hasPreliminaryFlooding;
    [SerializeField] private Dev_WaterPreliminaryFloodingConfig preliminaryFlooding = new Dev_WaterPreliminaryFloodingConfig();

    public bool TryCreateProfile(
        Dev_WaterProfileStage stage,
        out Dev_WaterSimulationSettings settings,
        out Dev_WaterSourceSpec[] continuousSources,
        out string error)
    {
        Dev_WaterStormProfile profile = GetProfile(stage);
        settings = null;
        continuousSources = System.Array.Empty<Dev_WaterSourceSpec>();

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

    public Dev_WaterSourceSpec[] CreateInitialSourceInstances()
    {
        return Dev_WaterStormProfile.CloneSources(initialSources);
    }

    public bool TryGetPreliminaryFlooding(out Dev_WaterPreliminaryFloodingConfig configuration, out string error)
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
        foreach (Dev_WaterProfileStage stage in System.Enum.GetValues(typeof(Dev_WaterProfileStage)))
        {
            if (!TryCreateProfile(stage, out _, out _, out error))
                return false;
        }

        if (initialSources != null)
        {
            foreach (Dev_WaterSourceSpec source in initialSources)
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

    private Dev_WaterStormProfile GetProfile(Dev_WaterProfileStage stage)
    {
        switch (stage)
        {
            case Dev_WaterProfileStage.Baseline: return baselineProfile;
            case Dev_WaterProfileStage.Preliminary: return preliminaryProfile;
            case Dev_WaterProfileStage.Crisis: return crisisProfile;
            default: return null;
        }
    }
}
