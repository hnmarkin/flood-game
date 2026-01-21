using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable]
public class PolicySelectedEvent : UnityEvent<CardData> { }

public class PolicyManager : MonoBehaviour
{
    public static PolicyManager Instance { get; private set; }

    [Header("Selection & Animation")]
    [SerializeField] private Transform policySlotContainer;
    [SerializeField] private float animationDuration = 1.0f;
    
    [Header("Resources")]
    [SerializeField] private ResourcesData resourcesData;

    [Header("Events")]
    public PolicySelectedEvent OnPolicySelected;
    public UnityEvent OnResourcesChanged;

    // Current state
    private CardData selectedCard;
    private CardLoader selectedCardLoader;
    private static int nextSlotIndex = 0;

    // Public properties
    public CardData SelectedCard => selectedCard;
    public bool HasSelection => selectedCard != null;
    public bool CanAffordSelected => HasSelection && CanAfford(selectedCard);

    private void Awake()
    {
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
    /// Select a policy card
    /// </summary>
    public void SelectPolicy(CardData cardData, CardLoader cardLoader = null)
    {
        selectedCard = cardData;
        selectedCardLoader = cardLoader;
        OnPolicySelected?.Invoke(cardData);
    }

    /// <summary>
    /// Check if a policy can be afforded
    /// </summary>
    public bool CanAfford(CardData card)
    {
        if (card == null || resourcesData == null) return false;
        return resourcesData.Money >= card.money && resourcesData.ActionPoints >= card.actionPoints;
    }

    /// <summary>
    /// Animate selected card to enacted slot
    /// </summary>
    public void EnactSelectedPolicy(System.Action onComplete = null)
    {
        if (!HasSelection || !CanAffordSelected)
        {
            onComplete?.Invoke();
            return;
        }

        // Deduct resources
        resourcesData.Money -= selectedCard.money;
        resourcesData.ActionPoints -= selectedCard.actionPoints;
        OnResourcesChanged?.Invoke();

        // Animate if we have the card reference
        if (selectedCardLoader != null)
        {
            AnimateCardToSlot(selectedCardLoader.gameObject, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }

        // Clear selection
        selectedCard = null;
        selectedCardLoader = null;
    }

    /// <summary>
    /// Animate card to next available slot
    /// </summary>
    private void AnimateCardToSlot(GameObject cardObject, System.Action onComplete)
    {
        Transform targetSlot = GetNextSlot();
        if (targetSlot == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Create animated copy
        GameObject copy = Instantiate(cardObject, targetSlot);
        copy.name = cardObject.name + "_Enacted";
        
        // Add Canvas component to override sorting order and render on top during animation
        Canvas animationCanvas = copy.AddComponent<Canvas>();
        animationCanvas.overrideSorting = true;
        animationCanvas.sortingOrder = 1000; // High value to ensure it renders on top

        // Add GraphicRaycaster to maintain UI functionality if needed
        copy.AddComponent<GraphicRaycaster>();
        
        // Disable interactions on copy
        if (copy.GetComponent<Button>()) copy.GetComponent<Button>().interactable = false;
        if (copy.GetComponent<CardLoader>()) copy.GetComponent<CardLoader>().enabled = false;

        // Set up animation positions
        Vector3 startPos = targetSlot.InverseTransformPoint(cardObject.transform.position);
        copy.transform.localPosition = startPos;

        // Mark original as enacted
        MarkAsEnacted(cardObject);

        // Animate to center of slot
        LeanTween.moveLocal(copy, Vector3.zero, animationDuration)
            .setEaseInOutQuad()
            .setOnComplete(() => {
                // Remove the override Canvas components now that animation is complete
                //if (animationCanvas != null) DestroyImmediate(animationCanvas);
                
                GraphicRaycaster raycaster = copy.GetComponent<GraphicRaycaster>();
                if (raycaster != null) DestroyImmediate(raycaster);
                
                nextSlotIndex++;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Mark original card as enacted
    /// </summary>
    private void MarkAsEnacted(GameObject card)
    {
        // Disable interactions
        if (card.GetComponent<Button>()) card.GetComponent<Button>().interactable = false;
        if (card.GetComponent<CardLoader>()) card.GetComponent<CardLoader>().enabled = false;

        // Hide button container
        Transform buttonContainer = card.transform.Find("ButtonContainer");
        if (buttonContainer != null)
        {
            buttonContainer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Get next available slot
    /// </summary>
    private Transform GetNextSlot()
    {
        if (policySlotContainer == null || nextSlotIndex >= policySlotContainer.childCount)
            return null;
        return policySlotContainer.GetChild(nextSlotIndex);
    }

    /// <summary>
    /// Reset for new game
    /// </summary>
    public static void Reset()
    {
        nextSlotIndex = 0;
    }
}
