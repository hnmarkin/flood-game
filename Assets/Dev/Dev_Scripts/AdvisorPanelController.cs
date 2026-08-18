using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AdvisorPanelController : MonoBehaviour
{
    private const string PanelContainerName = "advisor_panel_container";
    private const string LabelBoxName = "advisor_label_box";
    private const string LabelName = "advisor_label";
    private const string CloseButtonName = "advisor_close_button";
    private const string HiddenClassName = "advisor-popup-hidden";
    private const string VisibleClassName = "advisor-popup-visible";

    [Header("References")]
    [SerializeField] private ZoneBaselineRiskController baselineRiskController;
    [SerializeField] private UIDocument advisorUIDocument;
    [SerializeField] private HighRiskManager highRiskManager;

    [Header("Popup Timing")]
    [SerializeField] private float popupDelay = 0.2f;
    [SerializeField] private float secondsPerCharacter = 0.02f;
    [SerializeField] private float autoHideAfterSeconds;
    [SerializeField] private bool hideOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Label _advisorLabel;
    private VisualElement _popupTarget;
    private Button _closeButton;
    private Coroutine _typingRoutine;
    private Coroutine _autoHideRoutine;
    private Coroutine _bindRoutine;
    private bool _uiBound;

    private void Awake()
    {
        if (baselineRiskController == null)
            baselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (advisorUIDocument == null)
            advisorUIDocument = GetComponent<UIDocument>();

        if (highRiskManager == null)
            highRiskManager = FindFirstObjectByType<HighRiskManager>();
    }

    private void OnEnable()
    {
        BindUI();

        if (!_uiBound && _bindRoutine == null)
            _bindRoutine = StartCoroutine(BindUIWhenReady());

        if (highRiskManager != null)
            highRiskManager.BaselineRiskInspectionShown += OnBaselineRiskInspectionShown;
    }

    private void OnDisable()
    {
        if (highRiskManager != null)
            highRiskManager.BaselineRiskInspectionShown -= OnBaselineRiskInspectionShown;

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        UnbindCloseButton();
        StopActiveRoutines();
        _advisorLabel = null;
        _popupTarget = null;
        _closeButton = null;
        _uiBound = false;
    }

    public void ShowBaselineRiskAdvisorMessage()
    {
        if (!ValidateReferences())
            return;

        if (!_uiBound && !BindUI())
        {
            Debug.LogWarning("[AdvisorPanelController] Advisor UI is not ready yet.");
            return;
        }

        if (!baselineRiskController.EnsureBaselineRiskCalculated())
        {
            Debug.LogWarning("[AdvisorPanelController] Baseline risk is not available yet, so the advisor message could not be shown.");
            return;
        }

        string message = BuildAdvisorMessage(baselineRiskController.GetAllRiskResults());
        StartAdvisorMessage(message);
    }

    public void HideAdvisorPanel()
    {
        StopActiveRoutines();

        if (_advisorLabel != null)
            _advisorLabel.text = string.Empty;

        if (_popupTarget != null)
        {
            _popupTarget.pickingMode = PickingMode.Ignore;
            SetPopupVisible(false);
        }
    }

    private IEnumerator BindUIWhenReady()
    {
        const int maxFramesToWait = 30;
        int waitedFrames = 0;

        while (!_uiBound && waitedFrames < maxFramesToWait)
        {
            if (BindUI())
                break;

            waitedFrames++;
            yield return null;
        }

        if (!_uiBound)
            Debug.LogWarning("[AdvisorPanelController] Could not bind the advisor panel UI from the assigned UIDocument.");

        _bindRoutine = null;
    }

    private bool BindUI()
    {
        if (_uiBound || advisorUIDocument == null)
            return _uiBound;

        VisualElement root = advisorUIDocument.rootVisualElement;

        if (root == null)
            return false;

        _advisorLabel = root.Q<Label>(LabelName);
        _popupTarget = root.Q<VisualElement>(PanelContainerName) ?? root.Q<VisualElement>(LabelBoxName);
        _closeButton = root.Q<Button>(CloseButtonName);

        if (_advisorLabel == null || _popupTarget == null)
            return false;

        _uiBound = true;
        BindCloseButton();

        if (hideOnStart)
            HideAdvisorPanel();
        else
            SetPopupVisible(true);

        return true;
    }

    private void BindCloseButton()
    {
        if (_closeButton == null)
            return;

        _closeButton.clicked -= OnCloseButtonClicked;
        _closeButton.clicked += OnCloseButtonClicked;
    }

    private void UnbindCloseButton()
    {
        if (_closeButton == null)
            return;

        _closeButton.clicked -= OnCloseButtonClicked;
    }

    private void OnCloseButtonClicked()
    {
        HideAdvisorPanel();
    }

    private void OnBaselineRiskInspectionShown()
    {
        ShowBaselineRiskAdvisorMessage();
    }

    private void StartAdvisorMessage(string message)
    {
        StopActiveRoutines();

        _typingRoutine = StartCoroutine(ShowAdvisorMessageRoutine(message));
    }

    private IEnumerator ShowAdvisorMessageRoutine(string message)
    {
        if (_advisorLabel == null || _popupTarget == null)
            yield break;

        _advisorLabel.text = string.Empty;
        _popupTarget.pickingMode = PickingMode.Position;

        if (popupDelay > 0f)
            yield return new WaitForSeconds(popupDelay);

        SetPopupVisible(true);

        string safeMessage = message ?? string.Empty;

        for (int i = 1; i <= safeMessage.Length; i++)
        {
            _advisorLabel.text = safeMessage.Substring(0, i);

            if (secondsPerCharacter > 0f)
                yield return new WaitForSeconds(secondsPerCharacter);
            else
                yield return null;
        }

        _typingRoutine = null;

        if (autoHideAfterSeconds > 0f)
            _autoHideRoutine = StartCoroutine(AutoHideRoutine(autoHideAfterSeconds));
    }

    private IEnumerator AutoHideRoutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        _autoHideRoutine = null;
        HideAdvisorPanel();
    }

    private string BuildAdvisorMessage(IReadOnlyList<ZoneBaselineRiskData> riskResults)
    {
        int criticalCount = 0;
        int highCount = 0;

        if (riskResults != null)
        {
            for (int i = 0; i < riskResults.Count; i++)
            {
                switch (riskResults[i].riskLevel)
                {
                    case RiskLevel.Critical:
                        criticalCount++;
                        break;

                    case RiskLevel.High:
                        highCount++;
                        break;
                }
            }
        }

        string criticalText = FormatZoneCount(criticalCount, "critical zone", "critical zones");
        string highText = FormatZoneCount(highCount, "high-risk zone", "high-risk zones");

        return $"Flood risk assessment complete. We identified {criticalText} and {highText}. " +
               "You do not have enough resources to protect everything, so choose carefully. " +
               "Inspect the highlighted zones and place barriers where they can protect low-lying, populated areas near the water first.";
    }

    private string FormatZoneCount(int count, string singularLabel, string pluralLabel)
    {
        return count == 1 ? $"1 {singularLabel}" : $"{count} {pluralLabel}";
    }

    private void SetPopupVisible(bool isVisible)
    {
        if (_popupTarget == null)
            return;

        if (isVisible)
        {
            _popupTarget.RemoveFromClassList(HiddenClassName);
            _popupTarget.AddToClassList(VisibleClassName);
        }
        else
        {
            _popupTarget.RemoveFromClassList(VisibleClassName);
            _popupTarget.AddToClassList(HiddenClassName);
        }
    }

    private void StopActiveRoutines()
    {
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }

        if (_autoHideRoutine != null)
        {
            StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = null;
        }
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (baselineRiskController == null)
        {
            Debug.LogError("[AdvisorPanelController] ZoneBaselineRiskController is not assigned.");
            isValid = false;
        }

        if (advisorUIDocument == null)
        {
            Debug.LogError("[AdvisorPanelController] Advisor UIDocument is not assigned.");
            isValid = false;
        }

        if (highRiskManager == null && debugLogs)
            Debug.LogWarning("[AdvisorPanelController] HighRiskManager is not assigned. ShowBaselineRiskAdvisorMessage() can still be called manually.");

        return isValid;
    }
}
