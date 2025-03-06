using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/FloodData", order = 1)]
public class FloodData : ScriptableObject
{
    public int ruinedbuildings;
    public float amountFlooded;
    public string[] madCorpos;
}