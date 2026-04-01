using UnityEngine;
using UnityEngine.UIElements;

public class FloodDamageUIDocumentToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument floodDamageDocument;

    [Header("Optional")]
    [SerializeField] private bool hideOnStart = true;

    private VisualElement _damagePanel;
    private bool _isVisible;

    private void Awake()
    {
        if (floodDamageDocument == null)
        {
            Debug.LogError("[FloodDamageUIDocumentToggle] Flood Damage UIDocument is not assigned.");
            return;
        }

        var root = floodDamageDocument.rootVisualElement;
        _damagePanel = root.Q<VisualElement>("damage-panel");

        if (_damagePanel == null)
        {
            Debug.LogError("[FloodDamageUIDocumentToggle] Could not find 'damage-panel' in UXML.");
            return;
        }

        _isVisible = !hideOnStart;

        if (hideOnStart)
            _damagePanel.AddToClassList("hidden");
        else
            _damagePanel.RemoveFromClassList("hidden");
    }

    public void TogglePanel()
    {
        if (_damagePanel == null) return;

        _isVisible = !_isVisible;

        if (_isVisible)
            _damagePanel.RemoveFromClassList("hidden");
        else
            _damagePanel.AddToClassList("hidden");
    }

    public void ShowPanel()
    {
        if (_damagePanel == null) return;

        _isVisible = true;
        _damagePanel.RemoveFromClassList("hidden");
    }

    public void HidePanel()
    {
        if (_damagePanel == null) return;

        _isVisible = false;
        _damagePanel.AddToClassList("hidden");
    }
}