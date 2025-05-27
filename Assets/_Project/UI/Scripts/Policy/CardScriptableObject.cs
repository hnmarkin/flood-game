using UnityEngine;

public enum CardType   { General, Residential, Corporate, Political }

[CreateAssetMenu(menuName = "Policy/Card")]
public class CardData : ScriptableObject
{
    private const int MinMoney = -4;
    private const int MaxMoney = 4;
    private const int MinActionPoints = 0;
    private const int MaxActionPoints = 4;
    public string cardName;
    public CardType cardType;
    [TextArea] public string description;
    public int residentialOpinion, corporateOpinion, politicalOpinion;
    public int money;
    public int actionPoints;

// Clamp Money and Action Points to their respective ranges
    public int Money
    {
        get => money;
        set => money = Mathf.Clamp(value, MinMoney, MaxMoney);
    }

    public int ActionPoints
    {
        get => actionPoints;
        set => actionPoints = Mathf.Clamp(value, MinActionPoints, MaxActionPoints);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure money is within the specified range
        if (money < MinMoney || money > MaxMoney)
        {
            Debug.LogWarning($"Money value {money} is out of range. Clamping to [{MinMoney}, {MaxMoney}]");
            money = Mathf.Clamp(money, MinMoney, MaxMoney);
        }

        // Ensure action points are within the specified range
        if (actionPoints < MinActionPoints || actionPoints > MaxActionPoints)
        {
            Debug.LogWarning($"Action Points value {actionPoints} is out of range. Clamping to [{MinActionPoints}, {MaxActionPoints}]");
            actionPoints = Mathf.Clamp(actionPoints, MinActionPoints, MaxActionPoints);
        }
    }
#endif
}
