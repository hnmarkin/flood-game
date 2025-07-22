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

[System.Serializable]
public class ResourcesChangedEvent : UnityEvent { }

public class PolicyManager : MonoBehaviour
{
    public static PolicyManager Instance { get; private set; }

    [Header("Current Selection")]
    [SerializeField] private PolicySelectionData _currentSelection = new PolicySelectionData();

    [Header("Resource Data")]
    [SerializeField] private ResourcesData _resourcesData;

    [Header("Events")]
    public PolicySelectedEvent OnPolicySelected;
    public ResourcesChangedEvent OnResourcesChanged;

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

    // Resource properties
    public int CurrentMoney => _resourcesData?.Money ?? 0;
    public int CurrentActionPoints => _resourcesData?.ActionPoints ?? 0;

    // Affordability check for current selection
    public bool CanAffordSelectedPolicy => HasSelection() && CanAffordPolicy(_currentSelection);

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

    #region Resource Management

    /// <summary>
    /// Sets the current money amount
    /// </summary>
    /// <param name="amount">New money amount</param>
    public void SetMoney(int amount)
    {
        if (_resourcesData != null)
        {
            _resourcesData.Money = amount;
            Debug.Log($"Money updated to: {_resourcesData.Money}");
            OnResourcesChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning("ResourcesData is not assigned to PolicyManager!");
        }
    }

    /// <summary>
    /// Sets the current action points amount
    /// </summary>
    /// <param name="amount">New action points amount</param>
    public void SetActionPoints(int amount)
    {
        if (_resourcesData != null)
        {
            _resourcesData.ActionPoints = amount;
            Debug.Log($"Action points updated to: {_resourcesData.ActionPoints}");
            OnResourcesChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning("ResourcesData is not assigned to PolicyManager!");
        }
    }

    /// <summary>
    /// Modifies the current money by the specified amount
    /// </summary>
    /// <param name="amount">Amount to add (positive) or subtract (negative)</param>
    public void ModifyMoney(int amount)
    {
        if (_resourcesData != null)
        {
            _resourcesData.Money += amount;
            Debug.Log($"Money modified by {amount}, now: {_resourcesData.Money}");
            OnResourcesChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning("ResourcesData is not assigned to PolicyManager!");
        }
    }

    /// <summary>
    /// Modifies the current action points by the specified amount
    /// </summary>
    /// <param name="amount">Amount to add (positive) or subtract (negative)</param>
    public void ModifyActionPoints(int amount)
    {
        if (_resourcesData != null)
        {
            _resourcesData.ActionPoints += amount;
            Debug.Log($"Action points modified by {amount}, now: {_resourcesData.ActionPoints}");
            OnResourcesChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning("ResourcesData is not assigned to PolicyManager!");
        }
    }

    #endregion

    #region Affordability Checks

    /// <summary>
    /// Checks if a specific policy can be afforded with current resources
    /// </summary>
    /// <param name="policyData">The policy data to check</param>
    /// <returns>True if the policy can be afforded</returns>
    public bool CanAffordPolicy(PolicySelectionData policyData)
    {
        if (policyData == null || _resourcesData == null)
            return false;

        return _resourcesData.Money >= policyData.money && _resourcesData.ActionPoints >= policyData.actionPoints;
    }

    /// <summary>
    /// Checks if a specific CardData can be afforded with current resources
    /// </summary>
    /// <param name="cardData">The card data to check</param>
    /// <returns>True if the card can be afforded</returns>
    public bool CanAffordPolicy(CardData cardData)
    {
        if (cardData == null || _resourcesData == null)
            return false;

        return _resourcesData.Money >= cardData.money && _resourcesData.ActionPoints >= cardData.actionPoints;
    }

    /// <summary>
    /// Attempts to execute the currently selected policy, deducting resources if affordable
    /// </summary>
    /// <returns>True if the policy was executed successfully</returns>
    public bool TryExecuteSelectedPolicy()
    {
        if (!HasSelection())
        {
            Debug.LogWarning("No policy selected to execute!");
            return false;
        }

        if (!CanAffordSelectedPolicy)
        {
            Debug.LogWarning($"Cannot afford selected policy '{SelectedCardName}'. " +
                           $"Costs: {SelectedMoney} money, {SelectedActionPoints} action points. " +
                           $"Available: {CurrentMoney} money, {CurrentActionPoints} action points.");
            return false;
        }

        // Deduct the costs
        ModifyMoney(-SelectedMoney);
        ModifyActionPoints(-SelectedActionPoints);

        Debug.Log($"Executed policy '{SelectedCardName}' successfully!");
        
        // Note: Opinions are allowed to go negative, so they would be applied elsewhere
        // For example: OpinionSystem.ModifyOpinions(SelectedResidentialOpinion, SelectedCorporateOpinion, SelectedPoliticalOpinion);

        return true;
    }

    #endregion
}
