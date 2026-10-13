using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class AdvisorPanelLinkController : MonoBehaviour
{
    private const string SmallFullPanelName = "advisor_panel_full";
    private const string SmallCollapsedPanelName = "advisor_panel_collapse";
    private const string SmallCollapsedButtonName = "advisor_collapse_btn";
    private const string ExpandButtonName = "advisor_expand_btn";
    private const string BigRootName = "advisor_big_root";
    private const string MinimizeButtonName = "minimize_btn";
    private const string DoneButtonName = "done_btn";

    [Header("References")]
    [SerializeField] private UIDocument smallAdvisorUIDocument;
    [SerializeField] private UIDocument bigAdvisorUIDocument;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private VisualElement _smallFullPanel;
    private VisualElement _smallCollapsedElement;
    private VisualElement _bigDocumentRoot;
    private VisualElement _bigRoot;
    private Button _advisorExpandButton;
    private Button _minimizeButton;
    private Button _doneButton;
    private Coroutine _bindRoutine;
    private bool _isBound;

    private void Reset()
    {
        smallAdvisorUIDocument = GetComponent<UIDocument>();
        bigAdvisorUIDocument = GetComponent<UIDocument>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (!TryBindUI(false) && _bindRoutine == null)
            _bindRoutine = StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        UnbindCallbacks();
        ClearBindings();
    }

    public void OpenBigPanel()
    {
        if (!_isBound && !TryBindUI(true))
            return;

        SetElementDisplay(_smallFullPanel, DisplayStyle.None);
        SetElementDisplay(_smallCollapsedElement, DisplayStyle.None);
        SetElementDisplay(GetBigVisibilityTarget(), DisplayStyle.Flex);

        if (debugLogs)
            Debug.Log("[AdvisorPanelLinkController] Big Advisor Panel opened.");
    }

    public void CloseBigPanel()
    {
        if (!_isBound && !TryBindUI(true))
            return;

        SetElementDisplay(GetBigVisibilityTarget(), DisplayStyle.None);
        SetSmallAdvisorCollapsed();

        if (debugLogs)
            Debug.Log("[AdvisorPanelLinkController] Big Advisor Panel closed.");
    }

    private IEnumerator BindWhenReady()
    {
        const int maxFramesToWait = 30;
        int waitedFrames = 0;

        while (!_isBound && waitedFrames < maxFramesToWait)
        {
            if (TryBindUI(false))
                break;

            waitedFrames++;
            yield return null;
        }

        if (!_isBound)
            TryBindUI(true);

        _bindRoutine = null;
    }

    private bool TryBindUI(bool logWarnings)
    {
        if (_isBound)
            return true;

        ResolveReferences();

        VisualElement smallRoot = smallAdvisorUIDocument != null ? smallAdvisorUIDocument.rootVisualElement : null;
        VisualElement bigDocumentRoot = bigAdvisorUIDocument != null ? bigAdvisorUIDocument.rootVisualElement : null;

        if (smallRoot == null || bigDocumentRoot == null)
        {
            if (logWarnings)
                LogMissingDocumentWarnings(smallRoot, bigDocumentRoot);

            return false;
        }

        _smallFullPanel = smallRoot.Q<VisualElement>(SmallFullPanelName);
        _smallCollapsedElement = smallRoot.Q<VisualElement>(SmallCollapsedPanelName);
        _advisorExpandButton = smallRoot.Q<Button>(ExpandButtonName);

        Button collapsedButton = smallRoot.Q<Button>(SmallCollapsedButtonName);

        if (_smallCollapsedElement == null)
            _smallCollapsedElement = collapsedButton?.parent ?? collapsedButton;

        _bigRoot = bigDocumentRoot.Q<VisualElement>(BigRootName);
        _bigDocumentRoot = bigDocumentRoot;
        _minimizeButton = bigDocumentRoot.Q<Button>(MinimizeButtonName);
        _doneButton = bigDocumentRoot.Q<Button>(DoneButtonName);

        bool isValid =
            _smallFullPanel != null &&
            _smallCollapsedElement != null &&
            _advisorExpandButton != null &&
            _bigRoot != null &&
            _minimizeButton != null &&
            _doneButton != null;

        if (!isValid)
        {
            if (logWarnings)
                LogMissingElementWarnings();

            return false;
        }

        BindCallbacks();
        SetElementDisplay(GetBigVisibilityTarget(), DisplayStyle.None);
        _isBound = true;

        if (debugLogs)
            Debug.Log("[AdvisorPanelLinkController] Advisor expand button connected.");

        return true;
    }

    private void BindCallbacks()
    {
        _advisorExpandButton.clicked -= OpenBigPanel;
        _advisorExpandButton.clicked += OpenBigPanel;

        _minimizeButton.clicked -= CloseBigPanel;
        _minimizeButton.clicked += CloseBigPanel;

        _doneButton.clicked -= CloseBigPanel;
        _doneButton.clicked += CloseBigPanel;
    }

    private void UnbindCallbacks()
    {
        if (_advisorExpandButton != null)
            _advisorExpandButton.clicked -= OpenBigPanel;

        if (_minimizeButton != null)
            _minimizeButton.clicked -= CloseBigPanel;

        if (_doneButton != null)
            _doneButton.clicked -= CloseBigPanel;
    }

    private void SetSmallAdvisorCollapsed()
    {
        SetElementDisplay(_smallFullPanel, DisplayStyle.None);
        SetElementDisplay(_smallCollapsedElement, DisplayStyle.Flex);

        if (debugLogs)
            Debug.Log("[AdvisorPanelLinkController] Small Advisor Panel set to collapsed state.");
    }

    private static void SetElementDisplay(VisualElement element, DisplayStyle displayStyle)
    {
        if (element != null)
            element.style.display = displayStyle;
    }

    private VisualElement GetBigVisibilityTarget()
    {
        return bigAdvisorUIDocument != null && bigAdvisorUIDocument != smallAdvisorUIDocument
            ? _bigDocumentRoot
            : _bigRoot;
    }

    private void ResolveReferences()
    {
        if (smallAdvisorUIDocument == null)
            smallAdvisorUIDocument = FindUIDocumentContaining(SmallFullPanelName);

        if (bigAdvisorUIDocument == null)
            bigAdvisorUIDocument = FindUIDocumentContaining(BigRootName);
    }

    private static UIDocument FindUIDocumentContaining(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
            return null;

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        UIDocument[] documents = Object.FindObjectsOfType<UIDocument>(true);
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

    private void ClearBindings()
    {
        _smallFullPanel = null;
        _smallCollapsedElement = null;
        _bigDocumentRoot = null;
        _bigRoot = null;
        _advisorExpandButton = null;
        _minimizeButton = null;
        _doneButton = null;
        _isBound = false;
    }

    private void LogMissingDocumentWarnings(VisualElement smallRoot, VisualElement bigDocumentRoot)
    {
        if (smallAdvisorUIDocument == null)
            Debug.LogWarning("[AdvisorPanelLinkController] Small Advisor UIDocument is not assigned.");
        else if (smallRoot == null)
            Debug.LogWarning("[AdvisorPanelLinkController] Small Advisor UIDocument root is not ready.");

        if (bigAdvisorUIDocument == null)
            Debug.LogWarning("[AdvisorPanelLinkController] Big Advisor UIDocument is not assigned.");
        else if (bigDocumentRoot == null)
            Debug.LogWarning("[AdvisorPanelLinkController] Big Advisor UIDocument root is not ready.");
    }

    private void LogMissingElementWarnings()
    {
        if (_advisorExpandButton == null)
            Debug.LogWarning($"[AdvisorPanelLinkController] Could not find '{ExpandButtonName}'.");

        if (_minimizeButton == null)
            Debug.LogWarning($"[AdvisorPanelLinkController] Could not find '{MinimizeButtonName}'.");

        if (_doneButton == null)
            Debug.LogWarning($"[AdvisorPanelLinkController] Could not find '{DoneButtonName}'.");

        if (_smallFullPanel == null)
            Debug.LogWarning($"[AdvisorPanelLinkController] Could not find '{SmallFullPanelName}'.");

        if (_smallCollapsedElement == null)
        {
            Debug.LogWarning(
                $"[AdvisorPanelLinkController] Could not find collapsed Advisor element '{SmallCollapsedPanelName}' or '{SmallCollapsedButtonName}'.");
        }

        if (_bigRoot == null)
            Debug.LogWarning($"[AdvisorPanelLinkController] Could not find '{BigRootName}'.");
    }
}
