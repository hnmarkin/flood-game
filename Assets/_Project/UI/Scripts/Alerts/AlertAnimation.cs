using UnityEngine;

public class AlertAnimation : MonoBehaviour
{
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private LeanTweenType ease = LeanTweenType.easeOutCubic;

    public void PlaySlideIn(GameObject alertObject, Vector2 targetAnchoredPosition)
    {
        if (alertObject == null) alertObject = this.gameObject;
        float d = slideDuration;

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

    public float PlaySlideOut(GameObject alertObject, Vector2 targetAnchoredPosition)
    {
        if (alertObject == null) alertObject = this.gameObject;
        float d = slideDuration;

        RectTransform rt = alertObject.GetComponent<RectTransform>();
        if (rt != null)
        {
            LeanTween.cancel(alertObject);
            float startX = rt.anchoredPosition.x;
            LeanTween.value(alertObject, startX, targetAnchoredPosition.x, d)
                .setEase(ease)
                .setOnUpdate((float x) => rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y))
                .setOnComplete(() => Destroy(alertObject));
            return d;
        }

        // World-space fallback
        LeanTween.cancel(alertObject);
        Vector3 targetWorld = new Vector3(targetAnchoredPosition.x, targetAnchoredPosition.y, alertObject.transform.localPosition.z);
        LeanTween.moveLocal(alertObject, targetWorld, d)
            .setEase(ease)
            .setOnComplete(() => Destroy(alertObject));
        return d;
    }
}
