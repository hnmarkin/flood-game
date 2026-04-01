using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class NpcToastController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float delayBeforeShow = 1.2f;     // wait after sandbagging
    [SerializeField] private float secondsPerChar = 0.02f;     // typing speed
    [SerializeField] private float holdAfterTyped = 1.5f;      // how long to keep visible after finished
    [SerializeField] private bool hideAfterHold = true;

    [Header("UXML Names")]
    [SerializeField] private string toastName = "npc_toast";
    [SerializeField] private string textName  = "npc_toast_text";

    private VisualElement toast;
    private Label toastText;

    private Coroutine routine;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("NpcToastController: No UIDocument found on this GameObject.");
            return;
        }

        var root = doc.rootVisualElement;

        toast = root.Q<VisualElement>(toastName);
        toastText = root.Q<Label>(textName);

        if (toast == null) Debug.LogError($"NpcToastController: Missing #{toastName} in GameHUD.uxml");
        if (toastText == null) Debug.LogError($"NpcToastController: Missing #{textName} in GameHUD.uxml");

        toast?.AddToClassList("hidden");
    }

    /// <summary>
    /// Call this from FloodDefenseBoxStamp after committing the zone barrier.
    /// </summary>
    public void Show(string message)
    {
        if (toast == null || toastText == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        // Ensure hidden while waiting
        toast.AddToClassList("hidden");
        toastText.text = "";

        // Wait before showing
        if (delayBeforeShow > 0f)
            yield return new WaitForSeconds(delayBeforeShow);

        // Show container
        toast.RemoveFromClassList("hidden");

        // Typewriter
        for (int i = 1; i <= message.Length; i++)
        {
            toastText.text = message.Substring(0, i);

            if (secondsPerChar > 0f)
                yield return new WaitForSeconds(secondsPerChar);
            else
                yield return null;
        }

        // Hold after fully typed
        if (holdAfterTyped > 0f)
            yield return new WaitForSeconds(holdAfterTyped);

        if (hideAfterHold)
            toast.AddToClassList("hidden");

        routine = null;
    }

    public void HideNow()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        if (toastText != null) toastText.text = "";
        if (toast != null) toast.AddToClassList("hidden");
    }
}