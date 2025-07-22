using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Example component that demonstrates how to use the PolicyManager to access selected card data
/// This can be used as a reference for implementing similar functionality in other parts of your game
/// </summary>
public class PolicyDisplayExample : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text cardNameDisplay;
    [SerializeField] private TMP_Text residentialOpinionDisplay;
    [SerializeField] private TMP_Text corporateOpinionDisplay;
    [SerializeField] private TMP_Text politicalOpinionDisplay;
    [SerializeField] private TMP_Text moneyDisplay;
    [SerializeField] private TMP_Text actionPointsDisplay;
    [SerializeField] private TMP_Text descriptionDisplay;
    [SerializeField] private Button useSelectedPolicyButton;

    private void Start()
    {
        // Subscribe to policy selection events
        if (PolicyManager.Instance != null)
        {
            PolicyManager.Instance.OnPolicySelected.AddListener(OnPolicySelected);
        }

        // Set up button
        if (useSelectedPolicyButton != null)
        {
            useSelectedPolicyButton.onClick.AddListener(UseSelectedPolicy);
        }

        // Initialize display
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (PolicyManager.Instance != null)
        {
            PolicyManager.Instance.OnPolicySelected.RemoveListener(OnPolicySelected);
        }

        if (useSelectedPolicyButton != null)
        {
            useSelectedPolicyButton.onClick.RemoveListener(UseSelectedPolicy);
        }
    }

    /// <summary>
    /// Called when a new policy is selected via the event system
    /// </summary>
    /// <param name="selectionData">The newly selected policy data</param>
    private void OnPolicySelected(PolicySelectionData selectionData)
    {
        Debug.Log($"Policy selection changed to: {selectionData.cardName}");
        UpdateDisplay();
    }

    /// <summary>
    /// Updates the UI display with current selection data
    /// </summary>
    private void UpdateDisplay()
    {
        if (PolicyManager.Instance == null)
            return;

        var currentSelection = PolicyManager.Instance.CurrentSelection;

        if (cardNameDisplay != null)
            cardNameDisplay.text = $"Selected: {currentSelection.cardName}";

        if (residentialOpinionDisplay != null)
            residentialOpinionDisplay.text = $"Residential: {currentSelection.residentialOpinion}";

        if (corporateOpinionDisplay != null)
            corporateOpinionDisplay.text = $"Corporate: {currentSelection.corporateOpinion}";

        if (politicalOpinionDisplay != null)
            politicalOpinionDisplay.text = $"Political: {currentSelection.politicalOpinion}";

        if (moneyDisplay != null)
            moneyDisplay.text = $"Money: {currentSelection.money}";

        if (actionPointsDisplay != null)
            actionPointsDisplay.text = $"Action Points: {currentSelection.actionPoints}";

        if (descriptionDisplay != null)
            descriptionDisplay.text = currentSelection.description;

        // Enable/disable button based on selection
        if (useSelectedPolicyButton != null)
            useSelectedPolicyButton.interactable = PolicyManager.Instance.HasSelection();
    }

    /// <summary>
    /// Example method that uses the selected policy data
    /// </summary>
    private void UseSelectedPolicy()
    {
        if (PolicyManager.Instance == null || !PolicyManager.Instance.HasSelection())
        {
            Debug.LogWarning("No policy selected!");
            return;
        }

        // Access the selected data directly
        var selectedCard = PolicyManager.Instance.CurrentSelection;
        
        Debug.Log($"Using policy: {selectedCard.cardName}");
        Debug.Log($"This will affect opinions by: R{selectedCard.residentialOpinion}, C{selectedCard.corporateOpinion}, P{selectedCard.politicalOpinion}");
        Debug.Log($"Cost: {selectedCard.money} money, {selectedCard.actionPoints} action points");

        // Example: Apply the policy effects to your game state
        ApplyPolicyEffects(selectedCard);
    }

    /// <summary>
    /// Example method that applies policy effects to game systems
    /// </summary>
    /// <param name="policyData">The policy data to apply</param>
    private void ApplyPolicyEffects(PolicySelectionData policyData)
    {
        // This is where you would integrate with your game systems
        // For example:
        
        // Update player resources
        // GameStateManager.Instance.ModifyMoney(policyData.money);
        // GameStateManager.Instance.ModifyActionPoints(policyData.actionPoints);
        
        // Update opinion systems
        // OpinionSystem.Instance.ModifyResidentialOpinion(policyData.residentialOpinion);
        // OpinionSystem.Instance.ModifyCorporateOpinion(policyData.corporateOpinion);
        // OpinionSystem.Instance.ModifyPoliticalOpinion(policyData.politicalOpinion);

        Debug.Log($"Applied policy effects for: {policyData.cardName}");
        
        // Clear the selection after use (optional)
        // PolicyManager.Instance.ClearSelection();
    }

    /// <summary>
    /// Example method to manually access current selection (without events)
    /// </summary>
    [ContextMenu("Test Access Current Selection")]
    private void TestAccessCurrentSelection()
    {
        if (PolicyManager.Instance == null)
        {
            Debug.Log("PolicyManager not found!");
            return;
        }

        if (!PolicyManager.Instance.HasSelection())
        {
            Debug.Log("No policy currently selected.");
            return;
        }

        // Direct property access
        Debug.Log($"Selected Card Name: {PolicyManager.Instance.SelectedCardName}");
        Debug.Log($"Selected Money: {PolicyManager.Instance.SelectedMoney}");
        Debug.Log($"Selected Action Points: {PolicyManager.Instance.SelectedActionPoints}");
        
        // Full object access
        var selection = PolicyManager.Instance.CurrentSelection;
        Debug.Log($"Full selection: {selection.cardName} - R:{selection.residentialOpinion} C:{selection.corporateOpinion} P:{selection.politicalOpinion}");
    }
}
