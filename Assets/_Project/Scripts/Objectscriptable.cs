using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/FloodData", order = 1)]
public class FloodData : ScriptableObject
{
    public float homesFloodedPercent;
    public float casualties;
    public float utilityDowntimeHours;

    public float businessesAffectedPercent;
    public float economicLosses;
    public float infrastructureDamagePercent;
}