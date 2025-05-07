using UnityEngine;

public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public int residentialOpinion, corporateOpinion, politicalOpinion;
    public int money;
}
