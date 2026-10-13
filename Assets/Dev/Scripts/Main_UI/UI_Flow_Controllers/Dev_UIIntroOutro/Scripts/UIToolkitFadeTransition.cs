using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIToolkitFadeTransition : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField, Min(0f)] private float defaultDurationSeconds = 0.35f;
    [SerializeField] private AnimationCurve opacityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<VisualElement, Coroutine> _activeFades = new Dictionary<VisualElement, Coroutine>();

    private void OnValidate()
    {
        defaultDurationSeconds = Mathf.Max(0f, defaultDurationSeconds);

        if (opacityCurve == null || opacityCurve.length == 0)
            opacityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    private void OnDisable()
    {
        StopAllFadeRoutines();
    }

    public void ShowInstant(VisualElement element)
    {
        if (!ValidateElement(element, nameof(ShowInstant)))
            return;

        StopFade(element);
        element.style.display = DisplayStyle.Flex;
        element.style.opacity = 1f;
    }

    public void HideInstant(VisualElement element)
    {
        if (!ValidateElement(element, nameof(HideInstant)))
            return;

        StopFade(element);
        element.style.opacity = 0f;
        element.style.display = DisplayStyle.None;
    }

    public Coroutine FadeIn(VisualElement element, float duration)
    {
        if (!ValidateElement(element, nameof(FadeIn)))
            return null;

        StopFade(element);
        element.style.display = DisplayStyle.Flex;
        element.style.opacity = 0f;
        return StartTrackedFade(element, 0f, 1f, duration, false);
    }

    public Coroutine FadeOut(VisualElement element, float duration)
    {
        if (!ValidateElement(element, nameof(FadeOut)))
            return null;

        StopFade(element);
        element.style.display = DisplayStyle.Flex;
        return StartTrackedFade(element, GetCurrentOpacity(element), 0f, duration, true);
    }

    public Coroutine FadeTo(VisualElement element, float targetOpacity, float duration)
    {
        if (!ValidateElement(element, nameof(FadeTo)))
            return null;

        StopFade(element);
        element.style.display = DisplayStyle.Flex;
        return StartTrackedFade(element, GetCurrentOpacity(element), Mathf.Clamp01(targetOpacity), duration, false);
    }

    public Coroutine FadeIn(VisualElement element)
    {
        return FadeIn(element, defaultDurationSeconds);
    }

    public Coroutine FadeOut(VisualElement element)
    {
        return FadeOut(element, defaultDurationSeconds);
    }

    private Coroutine StartTrackedFade(
        VisualElement element,
        float startOpacity,
        float targetOpacity,
        float duration,
        bool hideWhenComplete)
    {
        Coroutine routine = StartCoroutine(FadeRoutine(element, startOpacity, targetOpacity, duration, hideWhenComplete));
        _activeFades[element] = routine;
        return routine;
    }

    private IEnumerator FadeRoutine(
        VisualElement element,
        float startOpacity,
        float targetOpacity,
        float duration,
        bool hideWhenComplete)
    {
        float safeDuration = Mathf.Max(0f, duration);

        if (safeDuration <= 0f)
        {
            element.style.opacity = targetOpacity;

            if (hideWhenComplete)
                element.style.display = DisplayStyle.None;

            _activeFades.Remove(element);
            yield break;
        }

        float elapsedSeconds = 0f;

        while (elapsedSeconds < safeDuration)
        {
            elapsedSeconds += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedSeconds / safeDuration);
            float easedTime = opacityCurve != null && opacityCurve.length > 0
                ? opacityCurve.Evaluate(normalizedTime)
                : normalizedTime;

            element.style.opacity = Mathf.Lerp(startOpacity, targetOpacity, easedTime);
            yield return null;
        }

        element.style.opacity = targetOpacity;

        if (hideWhenComplete)
            element.style.display = DisplayStyle.None;

        _activeFades.Remove(element);
    }

    private void StopFade(VisualElement element)
    {
        if (element == null)
            return;

        if (!_activeFades.TryGetValue(element, out Coroutine routine) || routine == null)
            return;

        StopCoroutine(routine);
        _activeFades.Remove(element);
    }

    private void StopAllFadeRoutines()
    {
        foreach (Coroutine routine in _activeFades.Values)
        {
            if (routine != null)
                StopCoroutine(routine);
        }

        _activeFades.Clear();
    }

    private bool ValidateElement(VisualElement element, string methodName)
    {
        if (element != null)
            return true;

        Debug.LogWarning($"[UIToolkitFadeTransition] {methodName} received a null VisualElement.");
        return false;
    }

    private float GetCurrentOpacity(VisualElement element)
    {
        if (element == null)
            return 1f;

        return Mathf.Clamp01(element.resolvedStyle.opacity);
    }
}
