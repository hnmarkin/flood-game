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
    
    private Vector3 originalPosition;
    
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
}
