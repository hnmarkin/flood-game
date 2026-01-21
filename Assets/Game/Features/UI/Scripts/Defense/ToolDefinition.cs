using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "UI/Tool Definition")]
public class ToolDefinition : ScriptableObject
{
    [Header("Tool Properties")]
    public string toolName;
    public Sprite toolIcon;
    public TileBase tileToPaint;

    [Header("Tool Rules")]
    public bool continuousPainting;

    [Header("Simulation Impact")]
    public float elevationAdd;
    public float waterDrainRate;
    public float infiltrationRate;
}
