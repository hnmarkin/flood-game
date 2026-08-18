using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CollapsiblePanelController : MonoBehaviour
{
    [Serializable]
    private sealed class PanelBinding
    {
        public string panelLabel;
        public UIDocument panelUIDocument;
        public string fullPanelName;
        public string minimizeButtonName;
        public string collapsedButtonName;

        [NonSerialized] public VisualElement fullPanel;
        [NonSerialized] public VisualElement collapsedElement;
        [NonSerialized] public Button minimizeButton;
        [NonSerialized] public Button collapsedButton;
        [NonSerialized] public Action minimizeHandler;
        [NonSerialized] public Action restoreHandler;
        [NonSerialized] public bool isBound;

        public PanelBinding(string panelLabel, string fullPanelName, string minimizeButtonName, string collapsedButtonName)
        {
            this.panelLabel = panelLabel;
            this.fullPanelName = fullPanelName;
            this.minimizeButtonName = minimizeButtonName;
            this.collapsedButtonName = collapsedButtonName;
        }
    }

    [Header("Shared Root")]
    [SerializeField] private UIDocument sharedRootUIDocument;

    [Header("Panels")]
    [SerializeField] private PanelBinding[] panels = CreateDefaultPanels();

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Coroutine _bindRoutine;

    private void Reset()
    {
        sharedRootUIDocument = GetComponent<UIDocument>();
        EnsureDefaultPanels();
    }

    private void Awake()
    {
        if (sharedRootUIDocument == null)
            sharedRootUIDocument = GetComponent<UIDocument>();

        EnsureDefaultPanels();
    }

    private void OnEnable()
    {
        bool allPanelsBound = TryBindPanels(false);

        if (!allPanelsBound && _bindRoutine == null)
            _bindRoutine = StartCoroutine(BindPanelsWhenReady());
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        for (int i = 0; i < panels.Length; i++)
            UnbindPanel(panels[i]);
    }

    private IEnumerator BindPanelsWhenReady()
    {
        const int maxFramesToWait = 30;
        int waitedFrames = 0;

        while (waitedFrames < maxFramesToWait)
        {
            if (TryBindPanels(false))
                break;

            waitedFrames++;
            yield return null;
        }

        TryBindPanels(true);
        _bindRoutine = null;
    }

    private bool TryBindPanels(bool logWarnings)
    {
        bool allPanelsBound = true;

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null)
                continue;

            if (panels[i].isBound)
                continue;

            if (!TryBindPanel(panels[i], logWarnings))
                allPanelsBound = false;
        }

        return allPanelsBound;
    }

    private bool TryBindPanel(PanelBinding panel, bool logWarnings)
    {
        UIDocument targetDocument = ResolveDocumentForPanel(panel);

        if (targetDocument == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[CollapsiblePanelController] No UIDocument is assigned for the {panel.panelLabel} panel.");

            return false;
        }

        VisualElement root = targetDocument.rootVisualElement;

        if (root == null)
            return false;

        panel.fullPanel = root.Q<VisualElement>(panel.fullPanelName);
        panel.minimizeButton = root.Q<Button>(panel.minimizeButtonName);
        panel.collapsedButton = root.Q<Button>(panel.collapsedButtonName);

        if (panel.collapsedButton != null)
        {
            panel.collapsedElement = panel.collapsedButton.parent ?? panel.collapsedButton;
        }
        else
        {
            panel.collapsedElement = root.Q<VisualElement>(panel.collapsedButtonName);
            panel.collapsedButton = panel.collapsedElement?.Q<Button>();
        }

        bool isValid =
            panel.fullPanel != null &&
            panel.minimizeButton != null &&
            panel.collapsedButton != null &&
            panel.collapsedElement != null;

        if (!isValid)
        {
            if (logWarnings)
                LogMissingElementWarnings(panel, targetDocument.name);

            return false;
        }

        panel.minimizeHandler ??= () => SetPanelExpanded(panel, false);
        panel.restoreHandler ??= () => SetPanelExpanded(panel, true);

        panel.minimizeButton.clicked -= panel.minimizeHandler;
        panel.minimizeButton.clicked += panel.minimizeHandler;

        panel.collapsedButton.clicked -= panel.restoreHandler;
        panel.collapsedButton.clicked += panel.restoreHandler;

        SetPanelExpanded(panel, true);
        panel.isBound = true;

        if (debugLogs)
            Debug.Log($"[CollapsiblePanelController] Bound collapse controls for the {panel.panelLabel} panel.");

        return true;
    }

    private void SetPanelExpanded(PanelBinding panel, bool isExpanded)
    {
        if (panel.fullPanel == null || panel.collapsedElement == null)
            return;

        panel.fullPanel.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        panel.collapsedElement.style.display = isExpanded ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void UnbindPanel(PanelBinding panel)
    {
        if (panel == null)
            return;

        if (panel.minimizeButton != null && panel.minimizeHandler != null)
            panel.minimizeButton.clicked -= panel.minimizeHandler;

        if (panel.collapsedButton != null && panel.restoreHandler != null)
            panel.collapsedButton.clicked -= panel.restoreHandler;

        panel.fullPanel = null;
        panel.collapsedElement = null;
        panel.minimizeButton = null;
        panel.collapsedButton = null;
        panel.isBound = false;
    }

    private void LogMissingElementWarnings(PanelBinding panel, string documentName)
    {
        if (panel.fullPanel == null)
        {
            Debug.LogWarning(
                $"[CollapsiblePanelController] Could not find full panel '{panel.fullPanelName}' in UIDocument '{documentName}' for {panel.panelLabel}.");
        }

        if (panel.minimizeButton == null)
        {
            Debug.LogWarning(
                $"[CollapsiblePanelController] Could not find minimize button '{panel.minimizeButtonName}' in UIDocument '{documentName}' for {panel.panelLabel}.");
        }

        if (panel.collapsedButton == null)
        {
            Debug.LogWarning(
                $"[CollapsiblePanelController] Could not find a collapsed button or collapsed container named '{panel.collapsedButtonName}' in UIDocument '{documentName}' for {panel.panelLabel}.");
        }
    }

    private UIDocument ResolveDocumentForPanel(PanelBinding panel)
    {
        UIDocument targetDocument = panel.panelUIDocument != null ? panel.panelUIDocument : sharedRootUIDocument;

        if (targetDocument != null)
            return targetDocument;

        UIDocument matchedDocument = FindUIDocumentContaining(panel.fullPanelName);

        if (matchedDocument != null)
        {
            panel.panelUIDocument = matchedDocument;

            if (debugLogs)
            {
                Debug.Log(
                    $"[CollapsiblePanelController] Auto-assigned UIDocument '{matchedDocument.name}' for the {panel.panelLabel} panel using full panel '{panel.fullPanelName}'.");
            }
        }

        return matchedDocument;
    }

    private static UIDocument FindUIDocumentContaining(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
            return null;

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        UIDocument[] documents = UnityEngine.Object.FindObjectsOfType<UIDocument>(true);
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

    private void EnsureDefaultPanels()
    {
        PanelBinding[] defaultPanels = CreateDefaultPanels();

        if (panels == null || panels.Length == 0)
        {
            panels = defaultPanels;
            return;
        }

        UpgradeKnownPanelNames(panels);

        List<PanelBinding> mergedPanels = null;

        for (int i = 0; i < defaultPanels.Length; i++)
        {
            PanelBinding defaultPanel = defaultPanels[i];

            if (HasEquivalentPanelBinding(defaultPanel))
                continue;

            mergedPanels ??= new List<PanelBinding>(panels);
            mergedPanels.Add(defaultPanel);

            if (debugLogs && Application.isPlaying)
            {
                Debug.Log(
                    $"[CollapsiblePanelController] Added missing default collapse binding for the {defaultPanel.panelLabel} panel.");
            }
        }

        if (mergedPanels != null)
            panels = mergedPanels.ToArray();
    }

    private bool HasEquivalentPanelBinding(PanelBinding targetPanel)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            PanelBinding panel = panels[i];

            if (panel == null)
                continue;

            if (string.Equals(panel.panelLabel, targetPanel.panelLabel, StringComparison.Ordinal))
                return true;

            if (string.Equals(panel.fullPanelName, targetPanel.fullPanelName, StringComparison.Ordinal))
                return true;

            if (string.Equals(panel.minimizeButtonName, targetPanel.minimizeButtonName, StringComparison.Ordinal))
                return true;

            if (string.Equals(panel.collapsedButtonName, targetPanel.collapsedButtonName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static void UpgradeKnownPanelNames(PanelBinding[] panelBindings)
    {
        for (int i = 0; i < panelBindings.Length; i++)
        {
            PanelBinding panel = panelBindings[i];

            if (panel == null)
                continue;

            if (string.Equals(panel.collapsedButtonName, "alert_box_collapsed_btn", StringComparison.Ordinal))
                panel.collapsedButtonName = "alert_box_collapse_btn";
        }
    }

    private static PanelBinding[] CreateDefaultPanels()
    {
        return new[]
        {
            new PanelBinding("Actions", "actions_full_panel", "actions_minimize_btn", "actions_collapsed_btn"),
            new PanelBinding("Advisor", "advisor_panel_full", "advisor_minimize_button", "advisor_collapse_btn"),
            new PanelBinding("Alerts", "alert_full_panel", "alert_minimize_btn", "alert_box_collapse_btn"),
            new PanelBinding("Inventory Tools", "inventory_full", "inventory_minimize_btn", "inventory_collapse_btn"),
            new PanelBinding("Mission Checklist", "mission_checklist_full", "mission_checklist_minimize_btn", "mission_checklist_collapse_btn"),
        };
    }
}
