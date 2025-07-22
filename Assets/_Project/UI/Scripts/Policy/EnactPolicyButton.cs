using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Example script for an ENACT button that uses PolicyManager's affordability system
/// Attach this to your ENACT button GameObject
/// </summary>
public class EnactPolicyButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button enactButton;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text statusText;

    [Header("Button Text")]
    [SerializeField] private string defaultButtonText = "ENACT POLICY";
    [SerializeField] private string cantAffordText = "INSUFFICIENT RESOURCES";
    [SerializeField] private string noSelectionText = "SELECT A POLICY";

    private void Start()
    {
        // Get button component if not assigned
        if (enactButton == null)
            enactButton = GetComponent<Button>();

        // Subscribe to policy selection changes
        if (PolicyManager.Instance != null)
        {
            PolicyManager.Instance.OnPolicySelected.AddListener(OnPolicySelectionChanged);
            PolicyManager.Instance.OnResourcesChanged.AddListener(OnResourcesChanged);
        }

        // Set up button click event
        if (enactButton != null)
        {
            enactButton.onClick.AddListener(OnEnactButtonClicked);
        }

        // Initialize button state
        UpdateButtonState();
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (PolicyManager.Instance != null)
        {
            PolicyManager.Instance.OnPolicySelected.RemoveListener(OnPolicySelectionChanged);
            PolicyManager.Instance.OnResourcesChanged.RemoveListener(OnResourcesChanged);
        }

        if (enactButton != null)
        {
            enactButton.onClick.RemoveListener(OnEnactButtonClicked);
        }
    }

    /// <summary>
    /// Called when policy selection changes
    /// </summary>
    /// <param name="selectionData">The new selection data</param>
    private void OnPolicySelectionChanged(PolicySelectionData selectionData)
    {
        UpdateButtonState();
    }

    /// <summary>
    /// Called when resources change - updates button state
    /// </summary>
    private void OnResourcesChanged()
    {
        UpdateButtonState();
    }

    /// <summary>
    /// Updates the button's interactable state and text based on current selection and resources
    /// </summary>
    private void UpdateButtonState()
    {
        if (PolicyManager.Instance == null)
        {
            SetButtonState(false, "POLICY MANAGER NOT FOUND");
            return;
        }

        if (!PolicyManager.Instance.HasSelection())
        {
            SetButtonState(false, noSelectionText);
            return;
        }

        if (PolicyManager.Instance.CanAffordSelectedPolicy)
        {
            SetButtonState(true, defaultButtonText);
            UpdateStatusText($"Ready to enact: {PolicyManager.Instance.SelectedCardName}");
        }
        else
        {
            SetButtonState(false, cantAffordText);
            UpdateStatusText($"Need {PolicyManager.Instance.SelectedMoney} money, {PolicyManager.Instance.SelectedActionPoints} action points. " +
                           $"Have {PolicyManager.Instance.CurrentMoney} money, {PolicyManager.Instance.CurrentActionPoints} action points.");
        }
    }

    /// <summary>
    /// Sets the button state and text
    /// </summary>
    /// <param name="interactable">Whether the button should be clickable</param>
    /// <param name="text">Text to display on the button</param>
    private void SetButtonState(bool interactable, string text)
    {
        if (enactButton != null)
            enactButton.interactable = interactable;

        if (buttonText != null)
            buttonText.text = text;
    }

    /// <summary>
    /// Updates the status text
    /// </summary>
    /// <param name="text">Text to display in the status area</param>
    private void UpdateStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    /// <summary>
    /// Called when the ENACT button is clicked
    /// </summary>
    private void OnEnactButtonClicked()
    {
        if (PolicyManager.Instance == null)
        {
            Debug.LogError("PolicyManager not found!");
            return;
        }

        // Try to execute the selected policy
        bool success = PolicyManager.Instance.TryExecuteSelectedPolicy();

        if (success)
        {
            // Policy was enacted successfully
            OnPolicyEnacted();
        }
        else
        {
            // Policy couldn't be enacted (insufficient resources or no selection)
            OnPolicyEnactFailed();
        }

        // Update button state after attempting to enact
        UpdateButtonState();
    }

    /// <summary>
    /// Called when a policy is successfully enacted
    /// </summary>
    private void OnPolicyEnacted()
    {
        var policy = PolicyManager.Instance.CurrentSelection;
        
        Debug.Log($"Policy '{policy.cardName}' enacted successfully!");
        
        // Here you would integrate with your game systems to apply the policy effects
        ApplyPolicyEffects(policy);
        
        // Optional: Clear selection after successful enactment
        // PolicyManager.Instance.ClearSelection();
        
        // Optional: Show success feedback to player
        UpdateStatusText($"Enacted: {policy.cardName}");
    }

    /// <summary>
    /// Called when policy enactment fails
    /// </summary>
    private void OnPolicyEnactFailed()
    {
        Debug.LogWarning("Failed to enact policy - insufficient resources or no selection");
        
        // Optional: Show error feedback to player
        if (!PolicyManager.Instance.HasSelection())
        {
            UpdateStatusText("Please select a policy first");
        }
        else
        {
            UpdateStatusText("Insufficient resources to enact this policy");
        }
    }

    /// <summary>
    /// Applies the policy effects to your game systems
    /// This is where you'd integrate with your opinion system, game state, etc.
    /// </summary>
    /// <param name="policy">The policy that was enacted</param>
    private void ApplyPolicyEffects(PolicySelectionData policy)
    {
        // Example integrations:
        
        // Apply opinion changes (these can go negative)
        // OpinionSystem.Instance?.ModifyResidentialOpinion(policy.residentialOpinion);
        // OpinionSystem.Instance?.ModifyCorporateOpinion(policy.corporateOpinion);
        // OpinionSystem.Instance?.ModifyPoliticalOpinion(policy.politicalOpinion);
        
        // Update game state
        // GameStateManager.Instance?.OnPolicyEnacted(policy);
        
        // Trigger events for other systems
        // EventSystem.Instance?.TriggerPolicyEnacted(policy);
        
        // Save progress
        // SaveSystem.Instance?.SaveGameState();
        
        Debug.Log($"Applied effects for policy: {policy.cardName} " +
                 $"(R:{policy.residentialOpinion}, C:{policy.corporateOpinion}, P:{policy.politicalOpinion})");
    }

    /// <summary>
    /// Public method to manually refresh the button state
    /// Call this if resources change outside of the PolicyManager
    /// </summary>
    public void RefreshButtonState()
    {
        UpdateButtonState();
    }

    /// <summary>
    /// Context menu method for testing in the editor
    /// </summary>
    [ContextMenu("Test Button State")]
    private void TestButtonState()
    {
        UpdateButtonState();
        
        if (PolicyManager.Instance != null)
        {
            Debug.Log($"Has Selection: {PolicyManager.Instance.HasSelection()}");
            Debug.Log($"Can Afford: {PolicyManager.Instance.CanAffordSelectedPolicy}");
            Debug.Log($"Current Money: {PolicyManager.Instance.CurrentMoney}");
            Debug.Log($"Current Action Points: {PolicyManager.Instance.CurrentActionPoints}");
            
            if (PolicyManager.Instance.HasSelection())
            {
                Debug.Log($"Selected Policy: {PolicyManager.Instance.SelectedCardName}");
                Debug.Log($"Policy Cost: {PolicyManager.Instance.SelectedMoney} money, {PolicyManager.Instance.SelectedActionPoints} action points");
            }
        }
    }
}
