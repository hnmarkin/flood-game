using UnityEngine;

public class AlertAnimation : MonoBehaviour
{
    [SerializeField] private GameObject alertObject;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private LeanTweenType ease = LeanTweenType.easeOutCubic;

    public void PlaySlideIn(Vector2 targetAnchoredPosition, float duration = -1f)
    {
        if (alertObject == null) alertObject = this.gameObject;
        float d = duration > 0 ? duration : slideDuration;

        RectTransform rt = alertObject.GetComponent<RectTransform>();
        if (rt != null)
        {
            LeanTween.cancel(alertObject);
            float startX = rt.anchoredPosition.x;
            LeanTween.value(alertObject, startX, targetAnchoredPosition.x, d)
                .setEase(ease)
                .setOnUpdate((float x) => rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y));
            return;
        }

        // World-space fallback
        LeanTween.cancel(alertObject);
        Vector3 targetWorld = new Vector3(targetAnchoredPosition.x, targetAnchoredPosition.y, alertObject.transform.localPosition.z);
        LeanTween.moveLocal(alertObject, targetWorld, d).setEase(ease);
    }
}
