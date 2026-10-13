using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class AdvisorMainController : MonoBehaviour
{
    private const string AdvisorMainHostName = "advisor_main_instance";
    private const string AdvisorMainRootName = "advisor_main_root";
    private const string SmallAdvisorRootName = "advisor_panel_root";
    private const string InspectButtonName = "inspect_btn";
    private const string CloseButtonName = "close_btn";

    [Header("References")]
    [SerializeField] private UIDocument advisorMainUIDocument;
    [SerializeField] private UIDocument smallAdvisorUIDocument;
    [SerializeField] private HighRiskManager highRiskManager;
    [SerializeField] private ZoneBaselineRiskController baselineRiskController;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpeningWeatherAlertShown = new UnityEvent();
    [SerializeField] private UnityEvent onInspectRiskRequested = new UnityEvent();

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private VisualElement _advisorMainHost;
    private VisualElement _advisorMainRoot;
    private VisualElement _smallAdvisorRoot;
    private Button _inspectButton;
    private Button _closeButton;
    private StyleEnum<DisplayStyle> _smallAdvisorDisplayBeforeOpening;
    private bool _isBound;
    private bool _smallAdvisorHiddenByOpening;

    public event Action OpeningWeatherAlertShown;
    public event Action InspectRiskRequested;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindUI(false);
        HideOpeningBriefing(false);
    }

    private void OnDisable()
    {
        UnbindCallbacks();
        _isBound = false;
        _advisorMainHost = null;
        _advisorMainRoot = null;
        _smallAdvisorRoot = null;
        _inspectButton = null;
        _closeButton = null;
    }

    public void Configure(HighRiskManager riskManager, ZoneBaselineRiskController riskController)
    {
        if (highRiskManager == null)
            highRiskManager = riskManager;

        if (baselineRiskController == null)
            baselineRiskController = riskController;

        ResolveReferences();
        BindUI(false);
    }

    public void ShowOpeningBriefing()
    {
        if (!_isBound && !BindUI(true))
            return;

        HideSmallAdvisorForOpening();
        SetElementDisplay(_advisorMainHost, DisplayStyle.Flex);
        SetElementDisplay(_advisorMainRoot, DisplayStyle.Flex);

        if (_advisorMainRoot != null)
            _advisorMainRoot.pickingMode = PickingMode.Position;

        onOpeningWeatherAlertShown?.Invoke();
        OpeningWeatherAlertShown?.Invoke();

        if (debugLogs)
            Debug.Log("[AdvisorMainController] Opening weather alert shown.");
    }

    public void HideOpeningBriefing()
    {
        HideOpeningBriefing(true);
    }

    private void HideOpeningBriefing(bool restoreSmallAdvisor)
    {
        if (!_isBound)
            BindUI(false);

        SetElementDisplay(_advisorMainRoot, DisplayStyle.None);
        SetElementDisplay(_advisorMainHost, DisplayStyle.None);

        if (_advisorMainRoot != null)
            _advisorMainRoot.pickingMode = PickingMode.Ignore;

        if (restoreSmallAdvisor)
            RestoreSmallAdvisorAfterOpening();
    }

    private bool BindUI(bool logWarnings)
    {
        ResolveReferences();

        if (advisorMainUIDocument == null || advisorMainUIDocument.rootVisualElement == null)
        {
            if (logWarnings)
                Debug.LogWarning("[AdvisorMainController] AdvisorMain UIDocument was not found.");

            return false;
        }

        VisualElement root = advisorMainUIDocument.rootVisualElement;
        _advisorMainHost = root.Q<VisualElement>(AdvisorMainHostName);
        _advisorMainRoot = root.Q<VisualElement>(AdvisorMainRootName);

        if (_advisorMainRoot == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[AdvisorMainController] Could not find '{AdvisorMainRootName}'.");

            return false;
        }

        _inspectButton = _advisorMainRoot.Q<Button>(InspectButtonName);
        _closeButton = _advisorMainRoot.Q<Button>(CloseButtonName);

        if (_inspectButton == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[AdvisorMainController] Could not find '{InspectButtonName}'.");

            return false;
        }

        if (smallAdvisorUIDocument != null && smallAdvisorUIDocument.rootVisualElement != null)
            _smallAdvisorRoot = smallAdvisorUIDocument.rootVisualElement.Q<VisualElement>(SmallAdvisorRootName);

        BindCallbacks();
        ButtonPopupManipulator.ApplyTo(_inspectButton);
        _isBound = true;
        return true;
    }

    private void BindCallbacks()
    {
        _inspectButton.clicked -= OnInspectRiskClicked;
        _inspectButton.clicked += OnInspectRiskClicked;

        if (_closeButton == null)
            return;

        _closeButton.clicked -= OnCloseClicked;
        _closeButton.clicked += OnCloseClicked;
    }

    private void UnbindCallbacks()
    {
        if (_inspectButton != null)
            _inspectButton.clicked -= OnInspectRiskClicked;

        if (_closeButton != null)
            _closeButton.clicked -= OnCloseClicked;
    }

    private void OnInspectRiskClicked()
    {
        if (!ShowBaselineRiskOverlay())
            return;

        HideOpeningBriefing(true);
        onInspectRiskRequested?.Invoke();
        InspectRiskRequested?.Invoke();
    }

    private void OnCloseClicked()
    {
        HideOpeningBriefing(true);
    }

    private bool ShowBaselineRiskOverlay()
    {
        ResolveReferences();

        if (highRiskManager != null)
        {
            highRiskManager.ShowBaselineRiskOverlay();
            return true;
        }

        if (baselineRiskController != null)
        {
            baselineRiskController.RecalculateBaselineRiskAndHighlight();
            return true;
        }

        Debug.LogError("[AdvisorMainController] No baseline risk display controller was found.");
        return false;
    }

    private void HideSmallAdvisorForOpening()
    {
        if (_smallAdvisorRoot == null || _smallAdvisorHiddenByOpening)
            return;

        if (_advisorMainRoot != null && IsAncestor(_smallAdvisorRoot, _advisorMainRoot))
            return;

        _smallAdvisorDisplayBeforeOpening = _smallAdvisorRoot.style.display;
        _smallAdvisorRoot.style.display = DisplayStyle.None;
        _smallAdvisorHiddenByOpening = true;
    }

    private void RestoreSmallAdvisorAfterOpening()
    {
        if (_smallAdvisorRoot == null || !_smallAdvisorHiddenByOpening)
            return;

        _smallAdvisorRoot.style.display = _smallAdvisorDisplayBeforeOpening;
        _smallAdvisorHiddenByOpening = false;
    }

    private void ResolveReferences()
    {
        if (advisorMainUIDocument == null)
            advisorMainUIDocument = FindUIDocumentContaining(AdvisorMainRootName);

        if (smallAdvisorUIDocument == null)
            smallAdvisorUIDocument = FindUIDocumentContaining(SmallAdvisorRootName);

        if (highRiskManager == null)
            highRiskManager = UnityEngine.Object.FindFirstObjectByType<HighRiskManager>();

        if (baselineRiskController == null)
            baselineRiskController = UnityEngine.Object.FindFirstObjectByType<ZoneBaselineRiskController>();
    }

    private static UIDocument FindUIDocumentContaining(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
            return null;

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        UIDocument[] documents = UnityEngine.Object.FindObjectsOfType<UIDocument>(true);
#pragma warning restore CS0618
#endif

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];

            if (document == null || document.rootVisualElement == null)
                continue;

            if (document.rootVisualElement.Q<VisualElement>(elementName) != null)
                return document;
        }

        return null;
    }

    private static bool IsAncestor(VisualElement possibleAncestor, VisualElement element)
    {
        for (VisualElement current = element.parent; current != null; current = current.parent)
        {
            if (current == possibleAncestor)
                return true;
        }

        return false;
    }

    private static void SetElementDisplay(VisualElement element, DisplayStyle displayStyle)
    {
        if (element != null)
            element.style.display = displayStyle;
    }
}
