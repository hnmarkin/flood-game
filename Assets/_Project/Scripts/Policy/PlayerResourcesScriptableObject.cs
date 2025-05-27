using UnityEngine;

[CreateAssetMenu(menuName = "Policy/Player Resources")]
public class PlayerResourcesScriptableObject : ScriptableObject
{   
    // Define the properties for player resources
    public int money;
    public int actionPoints;
    public int residentialOpinion;
    public int corporateOpinion;
    public int politicalOpinion;
}
