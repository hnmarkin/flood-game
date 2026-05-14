using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ScoringSceneController : MonoBehaviour
{
    [SerializeField] private string outcomeTabName = "tab-outcome";
    [SerializeField] private string metricsTabName = "tab-metrics";
    [SerializeField] private string reactionsTabName = "tab-reactions";

    [SerializeField] private string outcomeButtonName = "tab-outcome-btn";
    [SerializeField] private string metricsButtonName = "tab-metrics-btn";
    [SerializeField] private string reactionsButtonName = "tab-reactions-btn";

    private UIDocument _document;
    private VisualElement _outcomeTab;
    private VisualElement _metricsTab;
    private VisualElement _reactionsTab;
    private Button _outcomeButton;
    private Button _metricsButton;
    private Button _reactionsButton;

    private void OnEnable()
    {
        _document = GetComponent<UIDocument>();

        if (_document == null || _document.rootVisualElement == null)
        {
            Debug.LogError("[ScoringSceneController] UIDocument or rootVisualElement is missing.");
            return;
        }

        VisualElement root = _document.rootVisualElement;

        _outcomeTab = root.Q<VisualElement>(outcomeTabName);
        _metricsTab = root.Q<VisualElement>(metricsTabName);
        _reactionsTab = root.Q<VisualElement>(reactionsTabName);
        _outcomeButton = root.Q<Button>(outcomeButtonName);
        _metricsButton = root.Q<Button>(metricsButtonName);
        _reactionsButton = root.Q<Button>(reactionsButtonName);

        if (_outcomeButton != null)
            _outcomeButton.clicked += ShowOutcomeTab;

        if (_metricsButton != null)
            _metricsButton.clicked += ShowMetricsTab;

        if (_reactionsButton != null)
            _reactionsButton.clicked += ShowReactionsTab;

        ShowOutcomeTab();
    }

    private void OnDisable()
    {
        if (_outcomeButton != null)
            _outcomeButton.clicked -= ShowOutcomeTab;

        if (_metricsButton != null)
            _metricsButton.clicked -= ShowMetricsTab;

        if (_reactionsButton != null)
            _reactionsButton.clicked -= ShowReactionsTab;
    }

    private void ShowOutcomeTab()
    {
        SetTabState(_outcomeTab, _outcomeButton, true);
        SetTabState(_metricsTab, _metricsButton, false);
        SetTabState(_reactionsTab, _reactionsButton, false);
    }

    private void ShowMetricsTab()
    {
        SetTabState(_outcomeTab, _outcomeButton, false);
        SetTabState(_metricsTab, _metricsButton, true);
        SetTabState(_reactionsTab, _reactionsButton, false);
    }

    private void ShowReactionsTab()
    {
        SetTabState(_outcomeTab, _outcomeButton, false);
        SetTabState(_metricsTab, _metricsButton, false);
        SetTabState(_reactionsTab, _reactionsButton, true);
    }

    private static void SetTabState(VisualElement tab, Button button, bool isVisible)
    {
        if (tab != null)
            tab.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

        if (button == null)
            return;

        button.EnableInClassList("active", isVisible);
    }
}
