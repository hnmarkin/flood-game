using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/FloodData", order = 1)]
public class FloodData : ScriptableObject
{
    public int homesFloodedPercent;
    public int casualties;
    public int utilityDowntimeHours;

    public int businessesAffectedPercent;
    public int economicLosses;
    public int infrastructureDamagePercent;
}