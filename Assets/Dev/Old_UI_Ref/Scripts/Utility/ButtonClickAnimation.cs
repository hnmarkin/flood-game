using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonClickAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private GameObject buttonObject; // The GameObject to move (defaults to this GameObject)
    [SerializeField] private float animationDuration = 0.2f; // seconds
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource camSource; // optional, will try Camera.main if null
    [SerializeField] private float x_change = 0f;
    [SerializeField] private float y_change = -4f;

    private Vector3 originalPosition;

    private void Awake()
    {
        if (buttonObject == null)
            buttonObject = this.gameObject;

        if (camSource == null && Camera.main != null)
            camSource = Camera.main.GetComponent<AudioSource>();

        if (buttonObject != null)
            originalPosition = buttonObject.transform.localPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveButtonDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReturnButtonToOriginalPosition();
    }

    public void MoveButtonDown()
    {
        if (buttonObject == null) return;

        if (buttonClickSound != null && camSource != null)
            camSource.PlayOneShot(buttonClickSound);

        Vector3 targetPos = new Vector3(
            originalPosition.x + x_change,
            originalPosition.y + y_change,
            originalPosition.z);

        LeanTween.moveLocal(buttonObject, targetPos, animationDuration).setEaseOutQuad();
    }

    public void ReturnButtonToOriginalPosition()
    {
        if (buttonObject == null) return;
        LeanTween.moveLocal(buttonObject, originalPosition, animationDuration).setEaseOutQuad();
    }
}
