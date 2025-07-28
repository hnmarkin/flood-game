using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ButtonClickAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private GameObject buttonObject; // The button GameObject to animate
    [SerializeField] private float animationDuration = 0.2f; // Duration of the animation in seconds
    [SerializeField] private AudioClip buttonClickSound; // Sound to play on button click
    [SerializeField] private AudioSource camSource; // AudioSource to play the sound
    [SerializeField] private float x_change;
    [SerializeField] private float y_change; // X and Y change values for the button movement
    
    [Header("Enact Animation")]
    [SerializeField] private Transform policySlotContainer; // The content area of your scroll view with vertical layout
    [SerializeField] private ScrollRect scrollRect; // Reference to the scroll rect component
    [SerializeField] private float enactAnimationDuration = 1.0f; // Duration for the enact animation
    
    private Vector3 originalPosition;
    private static int nextSlotIndex = 0; // Static to persist across all button instances
    
    void Awake()
    {
        camSource = Camera.main.GetComponent<AudioSource>();
        if (camSource == null) Debug.LogWarning("Main Camera has no AudioSource!");
        
        // Store the original position
        if (buttonObject != null)
            originalPosition = buttonObject.transform.localPosition;
    }
    
    // Called when pointer is pressed down on this UI element
    public void OnPointerDown(PointerEventData eventData)
    {
        MoveButtonDown();
    }
    
    // Called when pointer is released
    public void OnPointerUp(PointerEventData eventData)
    {
        ReturnButtonToOriginalPosition();
    }
    
    /// <summary>
    /// Animates the button/card to the next available policy slot
    /// </summary>
    /// <param name="onComplete">Callback when animation completes</param>
    public void EnactAnimation(System.Action onComplete = null)
    {
        if (policySlotContainer == null || buttonObject == null)
        {
            Debug.LogError("PolicySlotContainer or ButtonObject not assigned!");
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
        
        // Convert target position to world space, then to local space of the button's parent
        Vector3 worldTargetPos = targetSlot.position;
        Vector3 localTargetPos = buttonObject.transform.parent.InverseTransformPoint(worldTargetPos);
        
        // Create a copy of the button for animation (optional - keeps original in place)
        GameObject animatingObject = CreateAnimationCopy();
        
        // Animate to the slot
        LeanTween.moveLocal(animatingObject, localTargetPos, enactAnimationDuration)
            .setEaseInOutQuad()
            .setOnComplete(() => {
                // Fill the slot visually
                FillPolicySlot(targetSlot, animatingObject);
                
                // Auto-scroll to show the new policy if needed
                AutoScrollToSlot(targetSlot);
                
                // Increment slot index for next policy
                nextSlotIndex++;
                
                // Call completion callback
                onComplete?.Invoke();
            });
    }
    
    private void MoveButtonDown()
    {
        // Play sound
        if (buttonClickSound != null && camSource != null)
            camSource.PlayOneShot(buttonClickSound);
            
        // Move the button down and right
        Vector3 targetPos = new Vector3(
            originalPosition.x + x_change, 
            originalPosition.y + y_change, 
            originalPosition.z);
            
        // Move without returning automatically
        LeanTween.moveLocal(buttonObject, targetPos, animationDuration).setEaseOutQuad();
    }
    
    private void ReturnButtonToOriginalPosition()
    {
        // Return to original position
        LeanTween.moveLocal(buttonObject, originalPosition, animationDuration).setEaseOutQuad();
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
    /// Creates a copy of the button for animation purposes
    /// </summary>
    /// <returns>GameObject copy for animation</returns>
    private GameObject CreateAnimationCopy()
    {
        GameObject copy = Instantiate(buttonObject, buttonObject.transform.parent);
        copy.transform.position = buttonObject.transform.position;
        copy.name = buttonObject.name + "_AnimatingCopy";
        
        // Disable any interactive components on the copy
        Button copyButton = copy.GetComponent<Button>();
        if (copyButton != null) copyButton.interactable = false;
        
        return copy;
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
}
