using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;

public class CardLoader : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text residentialOpinionText;
    [SerializeField] private TMP_Text corporateOpinionText;
    [SerializeField] private TMP_Text politicalOpinionText;
    //Money and action points are lists of images
    [SerializeField] private List<Image> moneyImages;
    [SerializeField] private List<Image> actionPointsImages;

    [SerializeField] private AvailableLoader _infoTextManager;
    [SerializeField] private Toggle _cardButton;
    
    // Visual feedback components for affordability
    [SerializeField] private CanvasGroup cardCanvasGroup;
    [SerializeField] private float disabledAlpha = 0.5f;
    [SerializeField] private Color disabledTextColor = Color.gray;
    
    private Color originalTextColor;
    private bool isAffordable = true;

    public CardData _cardData { get; private set; }

    private void Start()
    {
        // Store original text color
        if (cardNameText != null)
            originalTextColor = cardNameText.color;
            
        // Subscribe to resource changes
        if (PolicyManager.Instance != null)
        {
            PolicyManager.Instance.OnResourcesChanged.AddListener(OnResourcesChanged);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (PolicyManager.Instance != null)
        {
            PolicyManager.Instance.OnResourcesChanged.RemoveListener(OnResourcesChanged);
        }
    }
    
    /// <summary>
    /// Called when resources change - updates affordability
    /// </summary>
    private void OnResourcesChanged()
    {
        UpdateAffordability();
    }

    public void LoadCard(CardData cardData)
    {
        // Load the card data in
        _cardData = cardData;

        if (_cardData == null)
        {
            Debug.LogError("Card data is null!");
            return;
        }

        cardNameText.text = _cardData.cardName;
        residentialOpinionText.text = _cardData.residentialOpinion.ToString();
        corporateOpinionText.text = _cardData.corporateOpinion.ToString();
        politicalOpinionText.text = _cardData.politicalOpinion.ToString();

        // Set money pips
        for (int i = 0; i < _cardData.money; i++)
        {
            if (i < moneyImages.Count)
            {
                moneyImages[i].gameObject.SetActive(true);
            }
        }

        // Set action points pips
        for (int i = 0; i < _cardData.actionPoints; i++)
        {
            if (i < actionPointsImages.Count)
            {
                actionPointsImages[i].gameObject.SetActive(true);
            }
        }
        
        // Update affordability after loading
        UpdateAffordability();
    }

    /// <summary>
    /// Updates the affordability state of this card based on current resources
    /// </summary>
    public void UpdateAffordability()
    {
        if (_cardData == null || PolicyManager.Instance == null)
        {
            isAffordable = true; // Default to affordable if we can't check
            return;
        }

        // Check affordability through PolicyManager
        isAffordable = PolicyManager.Instance.CanAffordPolicy(_cardData);
        
        // Update visual state
        UpdateVisualState();
    }

    /// <summary>
    /// Updates the visual appearance based on affordability
    /// </summary>
    private void UpdateVisualState()
    {
        // Simply reduce alpha by 50% if unaffordable, otherwise full opacity
        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.alpha = isAffordable ? 1f : 0.5f;
        }
    }

    /// <summary>
    /// Public method to refresh affordability (call when resources change)
    /// </summary>
    public void RefreshAffordability()
    {
        UpdateAffordability();
    }

    public void OnClick()
    {
        // Check if card is affordable before allowing selection
        if (!isAffordable)
        {
            Debug.Log($"Cannot select {_cardData.cardName} - insufficient resources! " +
                     $"Costs: {_cardData.money} money, {_cardData.actionPoints} action points. " +
                     $"Available: {PolicyManager.Instance?.CurrentMoney} money, {PolicyManager.Instance?.CurrentActionPoints} action points.");
            // Optionally show a UI message to the player here
        }

        Debug.Log($"Card clicked: {_cardData.cardName}");
        
        // Update the PolicyManager with the current selection
        if (PolicyManager.Instance != null && _cardData != null)
        {
            PolicyManager.Instance.SelectPolicy(_cardData);
        }
        else if (_cardData == null)
        {
            Debug.LogError("CardData is null when trying to select policy!");
        }
        else
        {
            Debug.LogError("PolicyManager instance not found! Make sure PolicyManager is in the scene.");
        }

        // Keep the existing info text functionality
        if (_infoTextManager == null)
        {
            _infoTextManager = GetComponentInParent<AvailableLoader>();
        }

        if (_infoTextManager != null && _cardData != null)
        {
            _infoTextManager.SetInfoText(_cardData.description);
        }
        else if (_cardData != null)
        {
            Debug.LogWarning("InfoTextManager not assigned to CardLoader!");
        }
    }
}
