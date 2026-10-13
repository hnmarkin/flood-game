using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIVisibilityGroupController : MonoBehaviour
{
    [Header("HUD Documents")]
    [SerializeField] private UIDocument resourceBarDocument;
    [SerializeField] private UIDocument actionsPanelDocument;
    [SerializeField] private UIDocument alertsPanelDocument;
    [SerializeField] private UIDocument inventoryToolsDocument;
    [SerializeField] private UIDocument missionChecklistDocument;

    [Header("Advisor and Score Documents")]
    [SerializeField] private UIDocument advisorMainDocument;
    [SerializeField] private UIDocument advisorPanelDocument;
    [SerializeField] private UIDocument scorePanelDocument;

    [Header("Root Element Names")]
    [SerializeField] private string resourceBarRootName = "HUDRoot";
    [SerializeField] private string actionsPanelRootName = "actions_root";
    [SerializeField] private string alertsPanelRootName = "alert_root";
    [SerializeField] private string inventoryToolsRootName = "inventory_root";
    [SerializeField] private string missionChecklistRootName = "mission_checklist_root";
    [SerializeField] private string advisorMainHostName = "advisor_main_instance";
    [SerializeField] private string advisorMainRootName = "advisor_main_root";
    [SerializeField] private string advisorPanelRootName = "advisor_panel_root";
    [SerializeField] private string scorePanelRootName = "score_panel_root";

    [Header("Behavior")]
    [SerializeField] private bool autoFindMissingDocuments = true;
    [SerializeField] private bool showAdvisorPanelDuringGameplay = true;
    [SerializeField] private bool keepResourceBarVisibleOnScore = true;

    private readonly HashSet<string> _warningKeys = new HashSet<string>();

    public UIDocument AdvisorMainDocument => advisorMainDocument;
    public UIDocument AdvisorPanelDocument => advisorPanelDocument;
    public UIDocument ScorePanelDocument => scorePanelDocument;
    public string AdvisorMainRootName => advisorMainRootName;
    public string ScorePanelRootName => scorePanelRootName;

    private void Awake()
    {
        ResolveDocuments();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(resourceBarRootName))
            resourceBarRootName = "HUDRoot";

        if (string.IsNullOrWhiteSpace(actionsPanelRootName))
            actionsPanelRootName = "actions_root";

        if (string.IsNullOrWhiteSpace(alertsPanelRootName))
            alertsPanelRootName = "alert_root";

        if (string.IsNullOrWhiteSpace(inventoryToolsRootName))
            inventoryToolsRootName = "inventory_root";

        if (string.IsNullOrWhiteSpace(missionChecklistRootName))
            missionChecklistRootName = "mission_checklist_root";

        if (string.IsNullOrWhiteSpace(advisorMainRootName))
            advisorMainRootName = "advisor_main_root";

        if (string.IsNullOrWhiteSpace(advisorPanelRootName))
            advisorPanelRootName = "advisor_panel_root";

        if (string.IsNullOrWhiteSpace(scorePanelRootName))
            scorePanelRootName = "score_panel_root";
    }

    public void ResolveDocuments()
    {
        if (!autoFindMissingDocuments)
            return;

        resourceBarDocument = ResolveDocument(resourceBarDocument, resourceBarRootName, null);
        actionsPanelDocument = ResolveDocument(actionsPanelDocument, actionsPanelRootName, null);
        alertsPanelDocument = ResolveDocument(alertsPanelDocument, alertsPanelRootName, null);
        inventoryToolsDocument = ResolveDocument(inventoryToolsDocument, inventoryToolsRootName, null);
        missionChecklistDocument = ResolveDocument(missionChecklistDocument, missionChecklistRootName, null);
        advisorMainDocument = ResolveDocument(advisorMainDocument, advisorMainRootName, advisorMainHostName);
        advisorPanelDocument = ResolveDocument(advisorPanelDocument, advisorPanelRootName, null);
        scorePanelDocument = ResolveDocument(scorePanelDocument, scorePanelRootName, null);
    }

    public void ShowIntroState()
    {
        ResolveDocuments();

        SetGameplayHudVisible(false);
        SetScorePanelVisible(false);
        SetAdvisorPanelVisible(false);
        SetResourceBarVisible(true);
        SetAdvisorMainVisible(true);
    }

    public void ShowGameplayState()
    {
        ResolveDocuments();

        SetAdvisorMainVisible(false);
        SetScorePanelVisible(false);
        SetResourceBarVisible(true);
        SetGameplayHudVisible(true);

        if (showAdvisorPanelDuringGameplay)
            SetAdvisorPanelVisible(true);
    }

    public void ShowScoreState()
    {
        ResolveDocuments();

        SetGameplayHudVisible(false);
        SetAdvisorMainVisible(false);
        SetAdvisorPanelVisible(false);
        SetResourceBarVisible(keepResourceBarVisibleOnScore);
        SetScorePanelVisible(true);
    }

    public void SetDocumentVisible(UIDocument document, bool visible)
    {
        if (document == null)
        {
            WarnOnce("document-null", "[UIVisibilityGroupController] UIDocument is not assigned.");
            return;
        }

        VisualElement root = document.rootVisualElement;

        if (root == null)
        {
            WarnOnce(
                "document-root-null-" + document.GetInstanceID(),
                $"[UIVisibilityGroupController] UIDocument '{document.name}' does not have a rootVisualElement yet.");
            return;
        }

        SetElementVisible(root, visible);
    }

    public void SetDocumentVisible(UIDocument document, string rootElementName, bool visible)
    {
        if (string.IsNullOrWhiteSpace(rootElementName))
        {
            SetDocumentVisible(document, visible);
            return;
        }

        if (!TryGetElement(document, rootElementName, rootElementName, true, out VisualElement element))
            return;

        SetElementVisible(element, visible);
    }

    public bool TryGetAdvisorMainRoot(out VisualElement root)
    {
        ResolveDocuments();
        return TryGetElement(advisorMainDocument, advisorMainRootName, "AdvisorMain", false, out root);
    }

    public bool TryGetAdvisorMainHost(out VisualElement host)
    {
        ResolveDocuments();
        return TryGetElement(advisorMainDocument, advisorMainHostName, "AdvisorMain host", false, out host);
    }

    public bool TryGetScorePanelRoot(out VisualElement root)
    {
        ResolveDocuments();
        return TryGetElement(scorePanelDocument, scorePanelRootName, "ScorePanel", false, out root);
    }

    private void SetGameplayHudVisible(bool visible)
    {
        SetDocumentVisible(actionsPanelDocument, actionsPanelRootName, visible, "ActionsPanel", true);
        SetDocumentVisible(alertsPanelDocument, alertsPanelRootName, visible, "AlertsPanel", true);
        SetDocumentVisible(inventoryToolsDocument, inventoryToolsRootName, visible, "InventoryTools", true);
        SetDocumentVisible(missionChecklistDocument, missionChecklistRootName, visible, "MissionChecklistPanel", true);
    }

    private void SetResourceBarVisible(bool visible)
    {
        SetDocumentVisible(resourceBarDocument, resourceBarRootName, visible, "ResourceBar", true);
    }

    public void SetAdvisorMainVisible(bool visible)
    {
        if (!string.IsNullOrWhiteSpace(advisorMainHostName) &&
            TryGetElement(advisorMainDocument, advisorMainHostName, "AdvisorMain host", false, out VisualElement host))
        {
            SetElementVisible(host, visible);
        }

        SetDocumentVisible(advisorMainDocument, advisorMainRootName, visible, "AdvisorMain", true);
    }

    private void SetAdvisorPanelVisible(bool visible)
    {
        SetDocumentVisible(advisorPanelDocument, advisorPanelRootName, visible, "AdvisorPanel", true);
    }

    public void SetScorePanelVisible(bool visible)
    {
        SetDocumentVisible(scorePanelDocument, scorePanelRootName, visible, "ScorePanel", true);
    }

    private void SetDocumentVisible(
        UIDocument document,
        string rootElementName,
        bool visible,
        string panelLabel,
        bool warnIfMissing)
    {
        if (!TryGetElement(document, rootElementName, panelLabel, warnIfMissing, out VisualElement element))
            return;

        SetElementVisible(element, visible);
    }

    private bool TryGetElement(
        UIDocument document,
        string rootElementName,
        string panelLabel,
        bool warnIfMissing,
        out VisualElement element)
    {
        element = null;

        if (document == null)
        {
            if (warnIfMissing)
                WarnOnce(panelLabel + "-document", $"[UIVisibilityGroupController] {panelLabel} UIDocument is not assigned.");

            return false;
        }

        VisualElement documentRoot = document.rootVisualElement;

        if (documentRoot == null)
        {
            if (warnIfMissing)
            {
                WarnOnce(
                    panelLabel + "-root-null",
                    $"[UIVisibilityGroupController] {panelLabel} UIDocument '{document.name}' does not have a rootVisualElement yet.");
            }

            return false;
        }

        if (string.IsNullOrWhiteSpace(rootElementName))
        {
            element = documentRoot;
            return true;
        }

        element = documentRoot.Q<VisualElement>(rootElementName);

        if (element != null)
            return true;

        if (warnIfMissing)
        {
            WarnOnce(
                panelLabel + "-missing-" + rootElementName,
                $"[UIVisibilityGroupController] Could not find '{rootElementName}' for {panelLabel}. Check the assigned UIDocument or root element name.");
        }

        return false;
    }

    private void SetElementVisible(VisualElement element, bool visible)
    {
        if (element == null)
            return;

        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private UIDocument ResolveDocument(UIDocument currentDocument, string primaryElementName, string secondaryElementName)
    {
        if (currentDocument != null)
            return currentDocument;

        UIDocument foundDocument = FindUIDocumentContaining(primaryElementName);

        if (foundDocument == null && !string.IsNullOrWhiteSpace(secondaryElementName))
            foundDocument = FindUIDocumentContaining(secondaryElementName);

        return foundDocument;
    }

    private static UIDocument FindUIDocumentContaining(string elementName)
    {
        if (string.IsNullOrWhiteSpace(elementName))
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

    private void WarnOnce(string key, string message)
    {
        if (_warningKeys.Add(key))
            Debug.LogWarning(message);
    }
}
