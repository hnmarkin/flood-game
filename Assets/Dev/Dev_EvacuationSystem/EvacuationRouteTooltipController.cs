using UnityEngine;
using UnityEngine.UIElements;

/*
Inspector references required by EvacuationRouteTooltipController:
- Tooltip UI Document: optional. Prefer the Global HUD UIDocument. If unassigned, this
  controller searches for GlobalHUDController, then falls back to any loaded UIDocument.

Current behavior:
EvacuationRouteTooltipController owns a single UI Toolkit Label named
evacuation_route_tooltip. It creates the label programmatically if the UXML does not already
contain it, keeps it hidden by default, positions it near the mouse cursor, and does not
calculate route data or route hover state.
*/

public class EvacuationRouteTooltipController : MonoBehaviour
{
    private const string TooltipName = "evacuation_route_tooltip";

    [Header("References")]
    [SerializeField] private UIDocument tooltipUIDocument;

    [Header("Tooltip Settings")]
    [SerializeField] private Vector2 cursorOffset = new(16f, 18f);
    [SerializeField, Min(80f)] private float maxTooltipWidth = 240f;
    [SerializeField, Min(0f)] private float screenEdgePadding = 8f;
    [SerializeField] private bool debugLogs;

    private Label _tooltipLabel;
    private VisualElement _root;
    private bool _isVisible;

    public bool IsTooltipVisible => _isVisible;

    private void OnEnable()
    {
        EnsureTooltip();
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnValidate()
    {
        maxTooltipWidth = Mathf.Max(80f, maxTooltipWidth);
        screenEdgePadding = Mathf.Max(0f, screenEdgePadding);
    }

    public void SetTooltipUIDocument(UIDocument uiDocument)
    {
        if (tooltipUIDocument == uiDocument)
            return;

        tooltipUIDocument = uiDocument;
        _root = null;
        _tooltipLabel = null;
        _isVisible = false;
        EnsureTooltip();
    }

    public void SetDebugLogs(bool enableDebugLogs)
    {
        debugLogs = enableDebugLogs;
    }

    public void ShowTooltip(string tooltipText, Vector2 screenPosition)
    {
        if (string.IsNullOrWhiteSpace(tooltipText))
        {
            HideTooltip();
            return;
        }

        if (!EnsureTooltip())
            return;

        _tooltipLabel.text = tooltipText;
        _tooltipLabel.style.display = DisplayStyle.Flex;
        _tooltipLabel.BringToFront();
        _isVisible = true;
        UpdateTooltipPosition(screenPosition);
    }

    public void UpdateTooltipPosition(Vector2 screenPosition)
    {
        if (!_isVisible || !EnsureTooltip())
            return;

        Vector2 panelPosition = ScreenToRootPosition(screenPosition);
        float rootWidth = GetRootWidth();
        float rootHeight = GetRootHeight();
        float tooltipWidth = GetTooltipWidth();
        float tooltipHeight = GetTooltipHeight();

        float maxLeft = Mathf.Max(screenEdgePadding, rootWidth - tooltipWidth - screenEdgePadding);
        float maxTop = Mathf.Max(screenEdgePadding, rootHeight - tooltipHeight - screenEdgePadding);
        float left = Mathf.Clamp(panelPosition.x + cursorOffset.x, screenEdgePadding, maxLeft);
        float top = Mathf.Clamp(panelPosition.y + cursorOffset.y, screenEdgePadding, maxTop);

        _tooltipLabel.style.left = left;
        _tooltipLabel.style.top = top;
    }

    public void HideTooltip()
    {
        if (_tooltipLabel != null)
            _tooltipLabel.style.display = DisplayStyle.None;

        _isVisible = false;
    }

    private bool EnsureTooltip()
    {
        if (_tooltipLabel != null && _root != null)
            return true;

        if (tooltipUIDocument == null)
            tooltipUIDocument = ResolveTooltipUIDocument();

        if (tooltipUIDocument == null || tooltipUIDocument.rootVisualElement == null)
        {
            if (debugLogs)
                Debug.LogWarning("[EvacuationRouteTooltipController] No UIDocument is available for the evacuation route tooltip.");

            return false;
        }

        _root = tooltipUIDocument.rootVisualElement;
        _tooltipLabel = _root.Q<Label>(TooltipName);

        if (_tooltipLabel == null)
        {
            _tooltipLabel = new Label
            {
                name = TooltipName,
                pickingMode = PickingMode.Ignore,
            };
            _root.Add(_tooltipLabel);
        }

        ApplyTooltipStyle(_tooltipLabel);
        return true;
    }

    private UIDocument ResolveTooltipUIDocument()
    {
        if (GlobalHUDController.Instance != null &&
            GlobalHUDController.Instance.TryGetComponent(out UIDocument globalDocument))
        {
            return globalDocument;
        }

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        UIDocument[] documents = UnityEngine.Object.FindObjectsOfType<UIDocument>(true);
#endif

        UIDocument fallback = null;

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];

            if (document == null || document.rootVisualElement == null)
                continue;

            fallback ??= document;

            VisualElement root = document.rootVisualElement;

            if (root.Q<Label>("money_label") != null ||
                root.Q<Label>("pop_label") != null ||
                root.Q<Label>("zone_label") != null)
            {
                return document;
            }
        }

        return fallback;
    }

    private void ApplyTooltipStyle(Label tooltipLabel)
    {
        tooltipLabel.style.position = Position.Absolute;
        tooltipLabel.style.display = DisplayStyle.None;
        tooltipLabel.style.maxWidth = maxTooltipWidth;
        tooltipLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.82f);
        tooltipLabel.style.color = Color.white;
        tooltipLabel.style.fontSize = 11;
        tooltipLabel.style.whiteSpace = WhiteSpace.Normal;
        tooltipLabel.style.paddingLeft = 7;
        tooltipLabel.style.paddingRight = 7;
        tooltipLabel.style.paddingTop = 5;
        tooltipLabel.style.paddingBottom = 5;
        tooltipLabel.style.borderTopLeftRadius = 4;
        tooltipLabel.style.borderTopRightRadius = 4;
        tooltipLabel.style.borderBottomLeftRadius = 4;
        tooltipLabel.style.borderBottomRightRadius = 4;
    }

    private Vector2 ScreenToRootPosition(Vector2 screenPosition)
    {
        float rootWidth = GetRootWidth();
        float rootHeight = GetRootHeight();
        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        float x = Mathf.Clamp01(screenPosition.x / screenWidth) * rootWidth;
        float y = Mathf.Clamp01((screenHeight - screenPosition.y) / screenHeight) * rootHeight;
        return new Vector2(x, y);
    }

    private float GetRootWidth()
    {
        if (_root != null && _root.resolvedStyle.width > 0f)
            return _root.resolvedStyle.width;

        return Mathf.Max(1f, Screen.width);
    }

    private float GetRootHeight()
    {
        if (_root != null && _root.resolvedStyle.height > 0f)
            return _root.resolvedStyle.height;

        return Mathf.Max(1f, Screen.height);
    }

    private float GetTooltipWidth()
    {
        return _tooltipLabel != null && _tooltipLabel.resolvedStyle.width > 0f
            ? _tooltipLabel.resolvedStyle.width
            : maxTooltipWidth;
    }

    private float GetTooltipHeight()
    {
        return _tooltipLabel != null && _tooltipLabel.resolvedStyle.height > 0f
            ? _tooltipLabel.resolvedStyle.height
            : 96f;
    }
}
