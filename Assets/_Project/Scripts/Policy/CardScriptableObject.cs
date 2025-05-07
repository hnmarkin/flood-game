using UnityEngine;

public enum CardType   { General, Residential, Corporate, Political }

[CreateAssetMenu(menuName = "Policy/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    [TextArea] public string description;
    public int residentialOpinion, corporateOpinion, politicalOpinion;
    public int money;
    public int actionPoints;
}
