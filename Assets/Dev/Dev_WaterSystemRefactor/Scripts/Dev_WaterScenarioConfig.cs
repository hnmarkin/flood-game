using UnityEngine;

[CreateAssetMenu(fileName = "Dev_WaterScenarioConfig", menuName = "Dev/Water System/Scenario Config")]
public class Dev_WaterScenarioConfig : ScriptableObject
{
    [SerializeField] private Dev_WaterSimulationSettings simulationSettings = new Dev_WaterSimulationSettings();

    [Header("Water Sources")]
    [SerializeField] private Dev_WaterSourceSpec[] initialSources =
    {
        new Dev_WaterSourceSpec
        {
            kind = Dev_WaterSourceKind.ExistingWaterBodies,
            depth = 10f,
            scaleByExternalWaterLoad = true
        }
    };

    [SerializeField] private Dev_WaterSourceSpec[] continuousSources;

    [Header("Tile Type Fallbacks")]
    [SerializeField] private TileType waterTileType;

    public TileType WaterTileType => waterTileType;

    public Dev_WaterSimulationSettings CreateSettingsInstance()
    {
        Dev_WaterSimulationSettings clone = simulationSettings != null
            ? simulationSettings.Clone()
            : new Dev_WaterSimulationSettings();

        clone.Sanitize();
        return clone;
    }

    public Dev_WaterSourceSpec[] CreateInitialSourceInstances()
    {
        return CloneSources(initialSources);
    }

    public Dev_WaterSourceSpec[] CreateContinuousSourceInstances()
    {
        return CloneSources(continuousSources);
    }

    private static Dev_WaterSourceSpec[] CloneSources(Dev_WaterSourceSpec[] sources)
    {
        if (sources == null || sources.Length == 0)
            return System.Array.Empty<Dev_WaterSourceSpec>();

        Dev_WaterSourceSpec[] clones = new Dev_WaterSourceSpec[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            clones[i] = sources[i] != null ? sources[i].Clone() : null;

        return clones;
    }
}
