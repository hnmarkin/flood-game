using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class NpcToastController : MonoBehaviour
{
    public enum ToastAnchor
    {
        Bottom,
        Top
    }

    public static NpcToastController Instance { get; private set; }

    [Header("Default Timing")]
    [SerializeField] private float defaultDelayBeforeShow = 1.2f;
    [SerializeField] private float defaultSecondsPerChar = 0.02f;
    [SerializeField] private float defaultHoldAfterTyped = 1.5f;
    [SerializeField] private bool defaultHideAfterHold = true;
    [SerializeField] private bool defaultUseTypewriter = true;

    [Header("UXML Names")]
    [SerializeField] private string toastName = "npc_toast";
    [SerializeField] private string speakerNameLabel = "npc_toast_name";   // optional
    [SerializeField] private string textName = "npc_toast_text";
    [SerializeField] private ToastAnchor defaultAnchor = ToastAnchor.Bottom;

    private VisualElement toast;
    private Label toastSpeaker;
    private Label toastText;

    private Coroutine routine;
    private const string BottomAnchorClass = "npc-toast-bottom";
    private const string TopAnchorClass = "npc-toast-top";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NpcToastController: Duplicate instance found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

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
        toastSpeaker = root.Q<Label>(speakerNameLabel); // optional
        toastText = root.Q<Label>(textName);

        if (toast == null) Debug.LogError($"NpcToastController: Missing #{toastName}");
        if (toastText == null) Debug.LogError($"NpcToastController: Missing #{textName}");

        ApplyAnchor(defaultAnchor);
        toast?.AddToClassList("hidden");
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Simple call
    public void Show(string message)
    {
        Show("", message);
    }

    // Speaker + message using defaults
    public void Show(string speaker, string message)
    {
        Show(
            speaker,
            message,
            defaultDelayBeforeShow,
            defaultUseTypewriter,
            defaultSecondsPerChar,
            defaultHoldAfterTyped,
            defaultHideAfterHold,
            defaultAnchor
        );
    }

    // Full control
    public void Show(
        string speaker,
        string message,
        float delayBeforeShow,
        bool useTypewriter,
        float secondsPerChar,
        float holdAfterTyped,
        bool hideAfterHold
    )
    {
        Show(
            speaker,
            message,
            delayBeforeShow,
            useTypewriter,
            secondsPerChar,
            holdAfterTyped,
            hideAfterHold,
            defaultAnchor
        );
    }

    public void Show(
        string speaker,
        string message,
        float delayBeforeShow,
        bool useTypewriter,
        float secondsPerChar,
        float holdAfterTyped,
        bool hideAfterHold,
        ToastAnchor anchor
    )
    {
        if (toast == null || toastText == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(
            ShowRoutine(
                speaker,
                message,
                delayBeforeShow,
                useTypewriter,
                secondsPerChar,
                holdAfterTyped,
                hideAfterHold,
                anchor
            )
        );
    }

    private IEnumerator ShowRoutine(
        string speaker,
        string message,
        float delayBeforeShow,
        bool useTypewriter,
        float secondsPerChar,
        float holdAfterTyped,
        bool hideAfterHold,
        ToastAnchor anchor
    )
    {
        ApplyAnchor(anchor);
        toast.AddToClassList("hidden");

        if (toastSpeaker != null)
        {
            toastSpeaker.text = speaker;
            toastSpeaker.style.display = string.IsNullOrWhiteSpace(speaker)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        toastText.text = "";

        if (delayBeforeShow > 0f)
            yield return new WaitForSeconds(delayBeforeShow);

        toast.RemoveFromClassList("hidden");

        if (useTypewriter)
        {
            for (int i = 1; i <= message.Length; i++)
            {
                toastText.text = message.Substring(0, i);

                if (secondsPerChar > 0f)
                    yield return new WaitForSeconds(secondsPerChar);
                else
                    yield return null;
            }
        }
        else
        {
            toastText.text = message;
        }

        if (hideAfterHold)
        {
            if (holdAfterTyped > 0f)
                yield return new WaitForSeconds(holdAfterTyped);

            HideNow();
        }

        routine = null;
    }

    public void HideNow()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (toastText != null)
            toastText.text = "";

        if (toast != null)
            toast.AddToClassList("hidden");
    }

    private void ApplyAnchor(ToastAnchor anchor)
    {
        if (toast == null) return;

        toast.RemoveFromClassList(BottomAnchorClass);
        toast.RemoveFromClassList(TopAnchorClass);
        toast.AddToClassList(anchor == ToastAnchor.Top ? TopAnchorClass : BottomAnchorClass);
    }
}
