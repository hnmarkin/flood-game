using UnityEngine;
using UnityEngine.UIElements;

public class FloodDamageUIDocumentToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument floodDamageDocument;
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;

    [Header("Popup Placement")]
    [SerializeField] private Vector2 panelOffset = new Vector2(18f, 18f);
    [SerializeField] private float screenPadding = 12f;

    [Header("Optional")]
    [SerializeField] private bool hideOnStart = true;

    private VisualElement _root;
    private VisualElement _damagePanel;
    private FloodDamagePanelController _panelController;
    private bool _isVisible;
    private string _currentHoverGeoid;

    private void Awake()
    {
        if (floodDamageDocument == null)
        {
            Debug.LogError("[FloodDamageUIDocumentToggle] Flood Damage UIDocument is not assigned.");
            return;
        }

        _root = floodDamageDocument.rootVisualElement;
        _damagePanel = _root.Q<VisualElement>("damage-panel");
        _panelController = floodDamageDocument.GetComponent<FloodDamagePanelController>();

        if (_damagePanel == null)
        {
            Debug.LogError("[FloodDamageUIDocumentToggle] Could not find 'damage-panel' in UXML.");
            return;
        }

        if (!sceneCamera) sceneCamera = Camera.main;
        if (jsonMapLoader == null) jsonMapLoader = FindObjectOfType<JsonMapLoader>();

        _root.pickingMode = PickingMode.Ignore;
        _damagePanel.pickingMode = PickingMode.Ignore;

        _isVisible = !hideOnStart;
        _damagePanel.AddToClassList("hidden");
        _panelController?.ClearZone();
    }

    private void Update()
    {
        if (!_isVisible || _damagePanel == null)
            return;

        if (!sceneCamera) sceneCamera = Camera.main;
        if (jsonMapLoader == null) jsonMapLoader = FindObjectOfType<JsonMapLoader>();

        if (sceneCamera == null || jsonMapLoader == null)
            return;

        Vector2 mouseScreen = Input.mousePosition;
        Vector3 world = sceneCamera.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, -sceneCamera.transform.position.z));

        if (!jsonMapLoader.TryGetTileInfoAtWorld(world, out _, out _, out _, out string geoid, out _) ||
            string.IsNullOrWhiteSpace(geoid))
        {
            HideHoveredPanel();
            return;
        }

        geoid = geoid.Trim();

        if (_currentHoverGeoid != geoid)
        {
            _currentHoverGeoid = geoid;
            _panelController?.SetZone(geoid);
        }

        ShowVisualPanel();
        UpdatePanelPosition(mouseScreen);
    }

    public void TogglePanel()
    {
        if (_damagePanel == null)
            return;

        _isVisible = !_isVisible;

        if (_isVisible)
            HideHoveredPanel();
        else
            HidePanel();
    }

    public void ToggleZoneDamagePopup()
    {
        TogglePanel();
    }

    public void ShowPanel()
    {
        if (_damagePanel == null)
            return;

        _isVisible = true;
        HideHoveredPanel();
    }

    public void HidePanel()
    {
        if (_damagePanel == null)
            return;

        _isVisible = false;
        _damagePanel.AddToClassList("hidden");
        _currentHoverGeoid = null;
        _panelController?.ClearZone();
    }

    private void HideHoveredPanel()
    {
        _currentHoverGeoid = null;
        _damagePanel.AddToClassList("hidden");
        _panelController?.ClearZone();
    }

    private void ShowVisualPanel()
    {
        _damagePanel.RemoveFromClassList("hidden");
    }

    private void UpdatePanelPosition(Vector2 mouseScreen)
    {
        if (_damagePanel.panel == null || _root == null)
            return;

        Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(_damagePanel.panel, mouseScreen);

        float panelWidth = _damagePanel.resolvedStyle.width;
        float panelHeight = _damagePanel.resolvedStyle.height;
        float rootWidth = _root.resolvedStyle.width;
        float rootHeight = _root.resolvedStyle.height;

        if (float.IsNaN(panelWidth) || panelWidth <= 0f) panelWidth = 166f;
        if (float.IsNaN(panelHeight) || panelHeight <= 0f) panelHeight = 108f;
        if (float.IsNaN(rootWidth) || rootWidth <= 0f) rootWidth = Screen.width;
        if (float.IsNaN(rootHeight) || rootHeight <= 0f) rootHeight = Screen.height;

        float left = panelPoint.x + panelOffset.x;
        float top = panelPoint.y + panelOffset.y;

        if (left + panelWidth > rootWidth - screenPadding)
            left = panelPoint.x - panelWidth - Mathf.Abs(panelOffset.x);

        if (top + panelHeight > rootHeight - screenPadding)
            top = panelPoint.y - panelHeight - Mathf.Abs(panelOffset.y);

        float maxLeft = Mathf.Max(screenPadding, rootWidth - panelWidth - screenPadding);
        float maxTop = Mathf.Max(screenPadding, rootHeight - panelHeight - screenPadding);

        _damagePanel.style.left = Mathf.Clamp(left, screenPadding, maxLeft);
        _damagePanel.style.top = Mathf.Clamp(top, screenPadding, maxTop);
    }
}
