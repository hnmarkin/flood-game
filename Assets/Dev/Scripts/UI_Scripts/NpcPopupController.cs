using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class NpcPopupController : MonoBehaviour
{
    [SerializeField] private float autoHideSeconds = 2.5f;

    private VisualElement popup;
    private Label npcName;
    private Label npcDialog;
    private Button okButton;

    private Coroutine hideRoutine;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;

        popup = root.Q<VisualElement>("npc_popup");
        npcName = root.Q<Label>("npc_name");
        npcDialog = root.Q<Label>("npc_dialog");
        okButton = root.Q<Button>("npc_ok");

        popup?.AddToClassList("hidden");

        if (okButton != null)
        {
            okButton.clicked -= HideNow;
            okButton.clicked += HideNow;
        }
    }

    private void OnDisable()
    {
        if (okButton != null)
            okButton.clicked -= HideNow;

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }

    public void Show(string speaker, string message)
    {
        if (popup == null) return;

        npcName.text = speaker;
        npcDialog.text = message;

        popup.RemoveFromClassList("hidden");

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(AutoHide());
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        HideNow();
        hideRoutine = null;
    }

    private void HideNow()
    {
        popup?.AddToClassList("hidden");
    }
}