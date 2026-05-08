using UnityEngine;
using System.Collections;

public class NpcIntroOnStart : MonoBehaviour
{
    [SerializeField] private string speaker = "Guide";

    [TextArea(2, 5)]
    [SerializeField] private string firstMessage =
        "Welcome. Protect the most vulnerable flood zone first.";

    [TextArea(3, 8)]
    [SerializeField] private string secondMessage =
        "Watch the flood spread, review the map carefully, and focus your first decisions on the highest-risk zones.";

    [Header("Toast Timing")]
    [SerializeField] private float firstDelayBeforeShow = 2.0f;
    [SerializeField] private float secondsPerChar = 0.05f;
    [SerializeField] private float holdAfterFinished = 3.5f;
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private bool autoHide = true;
    [SerializeField] private float delayAfterFirstToast = 2.5f;

    private void Start()
    {
        StartCoroutine(ShowIntroSequence());
    }

    private IEnumerator ShowIntroSequence()
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
