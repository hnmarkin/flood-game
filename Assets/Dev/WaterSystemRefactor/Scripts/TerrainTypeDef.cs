using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Reusable terrain type that defines water simulation behavior and renderer data.
/// </summary>
[CreateAssetMenu(fileName = "TerrainTypeDef", menuName = "Dev/Water System/Terrain Type Definition")]
public sealed class TerrainTypeDef : ScriptableObject
{
    [SerializeField] private string terrainId;
    [SerializeField] private bool participatesInSimulation = true;
    [Min(0f)]
    [SerializeField] private float drainageMultiplier = 1f;
    [FormerlySerializedAs("visualDefinition")]
    [SerializeField] private RendererDef rendererDefinition;

    public string TerrainId => terrainId;
    public bool ParticipatesInSimulation => participatesInSimulation;
    public float DrainageMultiplier => Mathf.Max(0f, drainageMultiplier);
    public RendererDef RendererDefinition => rendererDefinition;

    public bool IsValidForProduction(out string error)
    {
        if (string.IsNullOrWhiteSpace(terrainId))
        {
            error = "Terrain ID is missing.";
            return false;
        }

        if (float.IsNaN(drainageMultiplier) || float.IsInfinity(drainageMultiplier) || drainageMultiplier < 0f)
        {
            error = "Terrain drainage multiplier must be finite and non-negative.";
            return false;
        }

        error = null;
        return true;
    }

    public void Configure(
        string id,
        bool canSimulate,
        float drainage,
        RendererDef renderer)
    {
        terrainId = id;
        participatesInSimulation = canSimulate;
        drainageMultiplier = Mathf.Max(0f, drainage);
        rendererDefinition = renderer;
    }
}
