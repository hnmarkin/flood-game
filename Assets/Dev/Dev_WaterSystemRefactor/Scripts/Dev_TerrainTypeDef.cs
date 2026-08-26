using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Reusable terrain type that defines water simulation behavior and renderer data.
/// </summary>
[CreateAssetMenu(fileName = "Dev_TerrainTypeDef", menuName = "Dev/Water System/Terrain Type Definition")]
public sealed class Dev_TerrainTypeDef : ScriptableObject
{
    [SerializeField] private string terrainId;
    [SerializeField] private bool participatesInSimulation = true;
    [Min(0f)]
    [SerializeField] private float drainageMultiplier = 1f;
    [FormerlySerializedAs("visualDefinition")]
    [SerializeField] private Dev_RendererDef rendererDefinition;

    public string TerrainId => terrainId;
    public bool ParticipatesInSimulation => participatesInSimulation;
    public float DrainageMultiplier => Mathf.Max(0f, drainageMultiplier);
    public Dev_RendererDef RendererDefinition => rendererDefinition;

    public void Configure(
        string id,
        bool canSimulate,
        float drainage,
        Dev_RendererDef renderer)
    {
        terrainId = id;
        participatesInSimulation = canSimulate;
        drainageMultiplier = Mathf.Max(0f, drainage);
        rendererDefinition = renderer;
    }
}
