using UnityEngine;
using System.Collections;

public class NpcOutroOnEnd : MonoBehaviour
{
    [SerializeField] private string speaker = "Guide";

    [TextArea(2, 5)]
    [SerializeField] private string firstMessage =
        "All available barriers have been placed.";

    [TextArea(3, 8)]
    [SerializeField] private string secondMessage =
        "Good work. Now watch how the floodwater responds to your decisions. Some areas may remain protected, while others may still be affected by seepage or surrounding terrain.";

    [Header("Toast Timing")]
    [SerializeField] private float firstDelayBeforeShow = 0.5f;
    [SerializeField] private float secondsPerChar = 0.05f;
    [SerializeField] private float holdAfterFinished = 3.5f;
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private bool autoHide = true;
    [SerializeField] private float delayAfterFirstToast = 2.5f;

    [Header("Behavior")]
    [SerializeField] private bool playOnlyOnce = true;

    private bool _hasPlayed = false;

    private void OnEnable()
    {
        FloodDefenseBoxStamp.OnAllZoneBarriersPlaced += PlayOutro;
    }

    private void OnDisable()
    {
        FloodDefenseBoxStamp.OnAllZoneBarriersPlaced -= PlayOutro;
    }

    public void PlayOutro()
    {
        if (playOnlyOnce && _hasPlayed)
            return;

        _hasPlayed = true;
        StartCoroutine(ShowOutroSequence());
    }

    private IEnumerator ShowOutroSequence()
    {
        yield return new WaitUntil(() => NpcToastController.Instance != null);

        NpcToastController.Instance.Show(
            speaker,
            firstMessage,
            firstDelayBeforeShow,
            useTypewriter,
            secondsPerChar,
            holdAfterFinished,
            autoHide,
            NpcToastController.ToastAnchor.Bottom
        );

        yield return new WaitForSeconds(CalculateToastDuration(firstMessage, firstDelayBeforeShow));
        yield return new WaitForSeconds(delayAfterFirstToast);

        if (string.IsNullOrWhiteSpace(secondMessage))
            yield break;

        NpcToastController.Instance.Show(
            speaker,
            secondMessage,
            0f,
            useTypewriter,
            secondsPerChar,
            holdAfterFinished,
            autoHide,
            NpcToastController.ToastAnchor.Top
        );
    }

    private float CalculateToastDuration(string message, float delayBeforeShow)
    {
        float typingDuration = useTypewriter ? message.Length * secondsPerChar : 0f;
        float holdDuration = autoHide ? holdAfterFinished : 0f;

        return delayBeforeShow + typingDuration + holdDuration;
    }
}