using UnityEngine;
using UnityEngine.UI;
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
    private CardLoader _selectedCardLoader; // Track which card is currently selected

    [Header("Resource Data")]
    [SerializeField] private ResourcesData _resourcesData;

    [Header("Animation")]
    [SerializeField] private Transform policySlotContainer; // The content area of your scroll view with vertical layout
    [SerializeField] private ScrollRect scrollRect; // Reference to the scroll rect component
    [SerializeField] private float enactAnimationDuration = 1.0f; // Duration for the enact animation

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

    // Animation tracking
    private static int nextSlotIndex = 0; // Static to persist across scenes

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
    /// Updates the current policy selection with card data and tracks the source card
    /// </summary>
    /// <param name="cardData">The card data to select</param>
    /// <param name="cardLoader">The CardLoader component that was selected</param>
    public void SelectPolicy(CardData cardData, CardLoader cardLoader)
    {
        if (cardData == null)
        {
            Debug.LogWarning("Attempted to select null CardData");
            return;
        }

        // Update current selection
        _currentSelection = new PolicySelectionData(cardData);
        _selectedCardLoader = cardLoader;

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
        _selectedCardLoader = null;
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

    /// <summary>
    /// Gets the ButtonClickAnimation component from the currently selected card
    /// </summary>
    /// <returns>ButtonClickAnimation component, or null if none selected or found</returns>
    public ButtonClickAnimation GetSelectedCardAnimation()
    {
        if (_selectedCardLoader == null)
            return null;

        return _selectedCardLoader.GetComponent<ButtonClickAnimation>();
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

    #region Policy Animation

    /// <summary>
    /// Animates the currently selected card to the next available policy slot
    /// </summary>
    /// <param name="onComplete">Callback when animation completes</param>
    public void AnimateSelectedCardToSlot(System.Action onComplete = null)
    {
        if (_selectedCardLoader == null)
        {
            Debug.LogWarning("No card selected for animation!");
            onComplete?.Invoke();
            return;
        }

        if (policySlotContainer == null)
        {
            Debug.LogError("PolicySlotContainer not assigned to PolicyManager!");
            onComplete?.Invoke();
            return;
        }

        // Get the target slot
        Transform targetSlot = GetNextAvailableSlot();
        if (targetSlot == null)
        {
            Debug.LogWarning("No available policy slots!");
            onComplete?.Invoke();
            return;
        }

        // Get the card's GameObject for animation
        GameObject cardObject = _selectedCardLoader.gameObject;

        // Convert target position to world space, then to local space of the card's parent
        Vector3 worldTargetPos = targetSlot.position;
        Vector3 localTargetPos = cardObject.transform.parent.InverseTransformPoint(worldTargetPos);

        // Create a copy of the card for animation (keeps original in place)
        GameObject animatingObject = CreateAnimationCopy(cardObject, targetSlot);

        // Make the original card appear enacted (gray and reduced alpha)
        MarkCardAsEnacted(cardObject);

        // Convert target position to world space, then to local space of the animating object's parent (now the target slot)
        Vector3 worldStartPos = cardObject.transform.position;
        Vector3 localStartPos = targetSlot.InverseTransformPoint(worldStartPos);
        localTargetPos = Vector3.zero; // Target is center of the slot

        // Set the starting position
        animatingObject.transform.localPosition = localStartPos;

        // Animate to the slot center
        LeanTween.moveLocal(animatingObject, localTargetPos, enactAnimationDuration)
            .setEaseInOutQuad()
            .setOnComplete(() => {
                // Remove the override Canvas component now that animation is complete
                Canvas animationCanvas = animatingObject.GetComponent<Canvas>();
                if (animationCanvas != null) DestroyImmediate(animationCanvas);
                
                GraphicRaycaster raycaster = animatingObject.GetComponent<GraphicRaycaster>();
                if (raycaster != null) DestroyImmediate(raycaster);

                // Auto-scroll to show the new policy if needed
                AutoScrollToSlot(targetSlot);

                // Increment slot index for next policy
                nextSlotIndex++;

                // Clear the selection since the card is now enacted
                ClearSelection();

                // Call completion callback
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Gets the next available slot in the policy container
    /// </summary>
    /// <returns>Transform of the next slot, or null if none available</returns>
    private Transform GetNextAvailableSlot()
    {
        if (policySlotContainer == null || nextSlotIndex >= policySlotContainer.childCount)
            return null;

        return policySlotContainer.GetChild(nextSlotIndex);
    }

    /// <summary>
    /// Creates a copy of the card for animation purposes
    /// </summary>
    /// <param name="originalCard">The original card GameObject</param>
    /// <param name="targetSlot">The slot where the copy will be animated to</param>
    /// <returns>GameObject copy for animation</returns>
    private GameObject CreateAnimationCopy(GameObject originalCard, Transform targetSlot)
    {
        GameObject copy = Instantiate(originalCard, targetSlot);
        copy.name = originalCard.name + "_AnimatingCopy";

        // Add a Canvas component to override sorting order and render on top during animation
        Canvas animationCanvas = copy.AddComponent<Canvas>();
        animationCanvas.overrideSorting = true;
        animationCanvas.sortingOrder = 1000; // High value to ensure it renders on top

        // Add GraphicRaycaster to maintain UI functionality if needed
        copy.AddComponent<GraphicRaycaster>();

        // Disable any interactive components on the copy
        Button copyButton = copy.GetComponent<Button>();
        if (copyButton != null) copyButton.interactable = false;

        Toggle copyToggle = copy.GetComponent<Toggle>();
        if (copyToggle != null) copyToggle.interactable = false;

        // Disable the CardLoader component to prevent interactions
        CardLoader copyLoader = copy.GetComponent<CardLoader>();
        if (copyLoader != null) copyLoader.enabled = false;

        return copy;
    }

    /// <summary>
    /// Disables interactions on a card during animation
    /// </summary>
    /// <param name="card">The card to disable interactions for</param>
    private void DisableCardInteractions(GameObject card)
    {
        // Disable interactive components
        Button cardButton = card.GetComponent<Button>();
        if (cardButton != null) cardButton.interactable = false;

        Toggle cardToggle = card.GetComponent<Toggle>();
        if (cardToggle != null) cardToggle.interactable = false;

        // Disable the CardLoader component to prevent interactions
        CardLoader cardLoader = card.GetComponent<CardLoader>();
        if (cardLoader != null) cardLoader.enabled = false;
    }

    /// <summary>
    /// Marks the original card as enacted with visual changes
    /// </summary>
    /// <param name="card">The card to mark as enacted</param>
    private void MarkCardAsEnacted(GameObject card)
    {
        // Disable interactions first
        DisableCardInteractions(card);

        // Find and deactivate everything under the "ButtonContainer" child
        Transform buttonContainer = card.transform.Find("ButtonContainer");
        if (buttonContainer != null)
        {
            buttonContainer.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"ButtonContainer not found in card: {card.name}");
        }

        // Optional: Add a visual indicator that this policy was enacted
        // You could add an "ENACTED" overlay or checkmark here
    }

    /// <summary>
    /// Fills the policy slot with the animated object
    /// </summary>
    /// <param name="slot">The slot to fill</param>
    /// <param name="animatedObject">The object that animated to this slot</param>
    private void FillPolicySlot(Transform slot, GameObject animatedObject)
    {
        // Parent the animated object to the slot
        animatedObject.transform.SetParent(slot, false);

        // Reset local position and scale to fit the slot
        animatedObject.transform.localPosition = Vector3.zero;
        animatedObject.transform.localScale = Vector3.one;

        // Optional: Add a component to mark this slot as filled
        // slot.gameObject.AddComponent<FilledPolicySlot>();
    }

    /// <summary>
    /// Auto-scrolls the scroll view to show the newly enacted policy
    /// </summary>
    /// <param name="targetSlot">The slot that was just filled</param>
    private void AutoScrollToSlot(Transform targetSlot)
    {
        if (scrollRect == null || policySlotContainer == null) return;

        // Calculate the normalized position of the target slot
        RectTransform contentRect = policySlotContainer.GetComponent<RectTransform>();
        RectTransform slotRect = targetSlot.GetComponent<RectTransform>();

        if (contentRect != null && slotRect != null)
        {
            // Calculate the position of the slot relative to the content
            float slotPosition = Mathf.Abs(slotRect.localPosition.y);
            float contentHeight = contentRect.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;

            // Calculate normalized scroll position (0 = top, 1 = bottom)
            float normalizedPosition = 1f - (slotPosition / (contentHeight - viewportHeight));
            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            // Animate the scroll position
            LeanTween.value(scrollRect.verticalNormalizedPosition, normalizedPosition, 0.5f)
                .setOnUpdate((float val) => {
                    scrollRect.verticalNormalizedPosition = val;
                })
                .setEaseOutQuad();
        }
    }

    /// <summary>
    /// Public method to reset the slot index (useful for testing or game restart)
    /// </summary>
    public static void ResetSlotIndex()
    {
        nextSlotIndex = 0;
    }

    #endregion
}
