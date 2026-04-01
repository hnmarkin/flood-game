using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneInfoHoverToggle : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    public bool IsEnabled { get; private set; }

    private void Reset()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TMP_Text>();

        SetEnabled(false);

        if (button != null)
            button.onClick.AddListener(() => SetEnabled(!IsEnabled));
        else
            Debug.LogWarning("[ZoneInfoHoverToggle] Button not assigned.");
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        if (label != null)
            label.text = enabled ? "Zone Information: ON" : "Zone Information: OFF";
    }
}
