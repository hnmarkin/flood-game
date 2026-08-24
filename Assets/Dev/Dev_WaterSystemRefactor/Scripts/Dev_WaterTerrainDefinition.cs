using UnityEngine;

/// <summary>
/// Persistent logical terrain definition used by the water simulation.
/// </summary>
[CreateAssetMenu(fileName = "Dev_WaterTerrainDefinition", menuName = "Dev/Water System/Terrain Definition")]
public sealed class Dev_WaterTerrainDefinition : ScriptableObject
{
    [SerializeField] private string terrainId;
    [SerializeField] private bool participatesInSimulation = true;
    [SerializeField] private bool isInitialWaterBody;
    [Min(0f)]
    [SerializeField] private float drainageMultiplier = 1f;
    [SerializeField] private Dev_WaterVisualDefinition visualDefinition;

    public string TerrainId => terrainId;
    public bool ParticipatesInSimulation => participatesInSimulation;
    public bool IsInitialWaterBody => isInitialWaterBody;
    public float DrainageMultiplier => Mathf.Max(0f, drainageMultiplier);
    public Dev_WaterVisualDefinition VisualDefinition => visualDefinition;

    public void Configure(
        string id,
        bool canSimulate,
        bool waterBody,
        float drainage,
        Dev_WaterVisualDefinition visual)
    {
        terrainId = id;
        participatesInSimulation = canSimulate;
        isInitialWaterBody = waterBody;
        drainageMultiplier = Mathf.Max(0f, drainage);
        visualDefinition = visual;
    }
}
