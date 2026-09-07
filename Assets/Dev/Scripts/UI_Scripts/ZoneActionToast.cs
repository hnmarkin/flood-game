using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ZoneActionToast : MonoBehaviour
{
    [SerializeField] private float duration = 2.0f;

    private Label toast;
    private Coroutine routine;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        toast = doc.rootVisualElement.Q<Label>("toast_label");
        HideImmediate();
    }

    public void Show(string message)
    {
        if (toast == null) return;

        toast.text = message;
        toast.RemoveFromClassList("hidden");

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AutoHide());
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(duration);
        HideImmediate();
        routine = null;
    }

    private void HideImmediate()
    {
        toast?.AddToClassList("hidden");
    }
}