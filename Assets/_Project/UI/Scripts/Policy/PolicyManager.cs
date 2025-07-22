using UnityEngine;
using UnityEngine.Events;
using System;

[System.Serializable]
public class PolicySelectionData
{
    public string cardName;
    public CardType cardType;
    public string description;
    public int residentialOpinion;
    public int corporateOpinion;
    public int politicalOpinion;
    public int money;
    public int actionPoints;

    public PolicySelectionData()
    {
        cardName = "";
        cardType = CardType.General;
        description = "";
        residentialOpinion = 0;
        corporateOpinion = 0;
        politicalOpinion = 0;
        money = 0;
        actionPoints = 0;
    }

    public PolicySelectionData(CardData cardData)
    {
        if (cardData != null)
        {
            cardName = cardData.cardName;
            cardType = cardData.cardType;
            description = cardData.description;
            residentialOpinion = cardData.residentialOpinion;
            corporateOpinion = cardData.corporateOpinion;
            politicalOpinion = cardData.politicalOpinion;
            money = cardData.money;
            actionPoints = cardData.actionPoints;
        }
        else
        {
            // Initialize with default values
            cardName = "";
            cardType = CardType.General;
            description = "";
            residentialOpinion = 0;
            corporateOpinion = 0;
            politicalOpinion = 0;
            money = 0;
            actionPoints = 0;
        }
    }
}

[System.Serializable]
public class PolicySelectedEvent : UnityEvent<PolicySelectionData> { }

public class PolicyManager : MonoBehaviour
{
    public static PolicyManager Instance { get; private set; }

    [Header("Current Selection")]
    [SerializeField] private PolicySelectionData _currentSelection = new PolicySelectionData();

    [Header("Events")]
    public PolicySelectedEvent OnPolicySelected;

    // Properties for easy access
    public PolicySelectionData CurrentSelection => _currentSelection;
    public string SelectedCardName => _currentSelection.cardName;
    public CardType SelectedCardType => _currentSelection.cardType;
    public string SelectedDescription => _currentSelection.description;
    public int SelectedResidentialOpinion => _currentSelection.residentialOpinion;
    public int SelectedCorporateOpinion => _currentSelection.corporateOpinion;
    public int SelectedPoliticalOpinion => _currentSelection.politicalOpinion;
    public int SelectedMoney => _currentSelection.money;
    public int SelectedActionPoints => _currentSelection.actionPoints;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Updates the current policy selection with new card data
    /// </summary>
    /// <param name="cardData">The card data to select</param>
    public void SelectPolicy(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("Attempted to select null CardData");
            return;
        }

        // Update current selection
        _currentSelection = new PolicySelectionData(cardData);

        // Trigger event for any listeners
        OnPolicySelected?.Invoke(_currentSelection);

        Debug.Log($"Policy selected: {_currentSelection.cardName}");
    }

    /// <summary>
    /// Updates the current selection with custom data
    /// </summary>
    /// <param name="selectionData">The policy selection data</param>
    public void SelectPolicy(PolicySelectionData selectionData)
    {
        if (selectionData == null)
        {
            Debug.LogWarning("Attempted to select null PolicySelectionData");
            return;
        }

        _currentSelection = selectionData;
        OnPolicySelected?.Invoke(_currentSelection);

        Debug.Log($"Policy selected: {_currentSelection.cardName}");
    }

    /// <summary>
    /// Clears the current selection
    /// </summary>
    public void ClearSelection()
    {
        _currentSelection = new PolicySelectionData();
        OnPolicySelected?.Invoke(_currentSelection);

        Debug.Log("Policy selection cleared");
    }

    /// <summary>
    /// Checks if a policy is currently selected
    /// </summary>
    /// <returns>True if a policy is selected</returns>
    public bool HasSelection()
    {
        return !string.IsNullOrEmpty(_currentSelection.cardName);
    }

    /// <summary>
    /// Gets a copy of the current selection data
    /// </summary>
    /// <returns>A copy of the current policy selection data</returns>
    public PolicySelectionData GetSelectionCopy()
    {
        return new PolicySelectionData
        {
            cardName = _currentSelection.cardName,
            cardType = _currentSelection.cardType,
            description = _currentSelection.description,
            residentialOpinion = _currentSelection.residentialOpinion,
            corporateOpinion = _currentSelection.corporateOpinion,
            politicalOpinion = _currentSelection.politicalOpinion,
            money = _currentSelection.money,
            actionPoints = _currentSelection.actionPoints
        };
    }
}
