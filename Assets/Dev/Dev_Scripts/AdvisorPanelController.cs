using System;
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
    [SerializeField] private FloodDefenseBoxStamp floodDefense;
    [SerializeField] private WaterSimulator waterSimulator;
    [SerializeField] private ShelterManager shelterManager;
    [SerializeField] private EvacuationController evacuationController;
    [SerializeField] private CommunicationTowerController communicationTowerController;

    [Header("Popup Timing")]
    [SerializeField] private float popupDelay = 0.2f;
    [SerializeField] private float secondsPerCharacter = 0.02f;
    [SerializeField] private float autoHideAfterSeconds;
    [SerializeField] private bool hideOnStart = true;

    [Header("Message Safety")]
    [SerializeField, Min(80)] private int maxAdvisorMessageCharacters = 220;
    [SerializeField] private bool subscribeToGameplayEvents = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Label _advisorLabel;
    private VisualElement _popupTarget;
    private Button _closeButton;
    private Coroutine _typingRoutine;
    private Coroutine _autoHideRoutine;
    private Coroutine _bindRoutine;
    private bool _uiBound;
    private bool _subscribedToFloodDefense;
    private bool _subscribedToWaterSimulator;
    private bool _subscribedToShelterManager;
    private bool _subscribedToEvacuationController;
    private bool _subscribedToCommunicationTowerController;
    private bool _subscribedToHighRiskManager;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindUI();

        if (!_uiBound && _bindRoutine == null)
            _bindRoutine = StartCoroutine(BindUIWhenReady());

        SubscribeToGameplayEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameplayEvents();

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

    private void OnValidate()
    {
        popupDelay = Mathf.Max(0f, popupDelay);
        secondsPerCharacter = Mathf.Max(0f, secondsPerCharacter);
        autoHideAfterSeconds = Mathf.Max(0f, autoHideAfterSeconds);
        maxAdvisorMessageCharacters = Mathf.Max(80, maxAdvisorMessageCharacters);
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
        ShowAdvisorMessage(message);
    }

    public void ShowAdvisorMessage(string message)
    {
        if (!_uiBound && !BindUI())
        {
            Debug.LogWarning("[AdvisorPanelController] Advisor UI is not ready yet.");
            return;
        }

        StartAdvisorMessage(SanitizeAdvisorMessage(message));
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
        ConfigureAdvisorLabel();
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

    private void ResolveReferences()
    {
        if (baselineRiskController == null)
            baselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (advisorUIDocument == null)
            advisorUIDocument = GetComponent<UIDocument>();

        if (highRiskManager == null)
            highRiskManager = FindFirstObjectByType<HighRiskManager>();

        if (floodDefense == null)
            floodDefense = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (waterSimulator == null)
            waterSimulator = FindFirstObjectByType<WaterSimulator>();

        if (shelterManager == null)
            shelterManager = FindFirstObjectByType<ShelterManager>();

        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (communicationTowerController == null)
            communicationTowerController = FindFirstObjectByType<CommunicationTowerController>();
    }

    private void SubscribeToGameplayEvents()
    {
        if (!subscribeToGameplayEvents)
            return;

        ResolveReferences();

        if (floodDefense != null && !_subscribedToFloodDefense)
        {
            floodDefense.ZoneBarrierPlaced += OnZoneBarrierPlaced;
            _subscribedToFloodDefense = true;
        }

        if (waterSimulator != null && !_subscribedToWaterSimulator)
        {
            waterSimulator.OnSimulationStarted += OnFloodSimulationStarted;
            _subscribedToWaterSimulator = true;
        }

        if (shelterManager != null && !_subscribedToShelterManager)
        {
            shelterManager.OnShelterPlaced += OnShelterPlaced;
            _subscribedToShelterManager = true;
        }

        if (evacuationController != null && !_subscribedToEvacuationController)
        {
            evacuationController.OnEvacuationRouteSelected += OnEvacuationRouteSelected;
            evacuationController.OnEvacuationRouteConfirmed += OnEvacuationRouteConfirmed;
            _subscribedToEvacuationController = true;
        }

        if (communicationTowerController != null && !_subscribedToCommunicationTowerController)
        {
            communicationTowerController.OnTowerActivated += OnTowerActivated;
            communicationTowerController.OnZoneWarned += OnZoneWarned;
            _subscribedToCommunicationTowerController = true;
        }

        if (highRiskManager != null && !_subscribedToHighRiskManager)
        {
            highRiskManager.BaselineRiskInspectionShown += OnBaselineRiskInspectionShown;
            _subscribedToHighRiskManager = true;
        }
    }

    private void UnsubscribeFromGameplayEvents()
    {
        if (floodDefense != null && _subscribedToFloodDefense)
        {
            floodDefense.ZoneBarrierPlaced -= OnZoneBarrierPlaced;
            _subscribedToFloodDefense = false;
        }

        if (waterSimulator != null && _subscribedToWaterSimulator)
        {
            waterSimulator.OnSimulationStarted -= OnFloodSimulationStarted;
            _subscribedToWaterSimulator = false;
        }

        if (shelterManager != null && _subscribedToShelterManager)
        {
            shelterManager.OnShelterPlaced -= OnShelterPlaced;
            _subscribedToShelterManager = false;
        }

        if (evacuationController != null && _subscribedToEvacuationController)
        {
            evacuationController.OnEvacuationRouteSelected -= OnEvacuationRouteSelected;
            evacuationController.OnEvacuationRouteConfirmed -= OnEvacuationRouteConfirmed;
            _subscribedToEvacuationController = false;
        }

        if (communicationTowerController != null && _subscribedToCommunicationTowerController)
        {
            communicationTowerController.OnTowerActivated -= OnTowerActivated;
            communicationTowerController.OnZoneWarned -= OnZoneWarned;
            _subscribedToCommunicationTowerController = false;
        }

        if (highRiskManager != null && _subscribedToHighRiskManager)
        {
            highRiskManager.BaselineRiskInspectionShown -= OnBaselineRiskInspectionShown;
            _subscribedToHighRiskManager = false;
        }
    }

    private void OnZoneBarrierPlaced(int placed, int required)
    {
        ShowAdvisorMessage($"Barrier placed. Defense progress is {Mathf.Max(0, placed)}/{Mathf.Max(1, required)}.");
    }

    private void OnFloodSimulationStarted()
    {
        ShowAdvisorMessage("Flood simulation started. Watch live impact and prioritize zones still exposed.");
    }

    private void OnShelterPlaced(PlacedShelterData shelter)
    {
        string shelterId = DemoDisplayIdFormatter.GetShelterDisplayId(shelter != null ? shelter.shelterId : null);
        int zoneCount = shelter != null && shelter.associatedZoneGeoids != null ? shelter.associatedZoneGeoids.Count : 0;
        int capacity = shelter != null ? Mathf.Max(0, shelter.capacity) : 0;

        ShowAdvisorMessage($"{shelterId} is active. It can serve {FormatZoneCount(zoneCount, "zone", "zones")} with {capacity:N0} capacity.");
    }

    private void OnEvacuationRouteSelected(SimpleEvacuationRoute route)
    {
        if (evacuationController != null && evacuationController.HasConfirmedEvacuationRoute)
            return;

        string routeId = DemoDisplayIdFormatter.GetEvacuationRouteDisplayId(route != null ? route.routeId : null);
        ShowAdvisorMessage($"{routeId} selected. Review the route and shelter capacity before confirming.");
    }

    private void OnEvacuationRouteConfirmed(SimpleEvacuationRoute route)
    {
        string routeId = DemoDisplayIdFormatter.GetEvacuationRouteDisplayId(route != null ? route.routeId : null);
        string zoneId = DemoDisplayIdFormatter.GetZoneDisplayId(route != null ? route.sourceZoneGeoid : null);
        string shelterId = DemoDisplayIdFormatter.GetShelterDisplayId(route != null ? route.destinationShelterId : null);

        ShowAdvisorMessage($"{routeId} confirmed for {zoneId} to {shelterId}. Route selection is locked.");
    }

    private void OnTowerActivated(CommunicationTowerData tower)
    {
        string towerId = DemoDisplayIdFormatter.GetTowerDisplayId(tower != null ? tower.towerId : null);
        int zoneCount = tower != null && tower.associatedZoneGeoids != null ? tower.associatedZoneGeoids.Count : 0;

        ShowAdvisorMessage($"{towerId} activated. {FormatZoneCount(zoneCount, "zone", "zones")} are ready for warnings.");
    }

    private void OnZoneWarned(string geoid)
    {
        string zoneId = DemoDisplayIdFormatter.GetZoneDisplayId(geoid);
        ShowAdvisorMessage($"Warning sent to {zoneId}. Residents now have evacuation instructions.");
    }

    private void ConfigureAdvisorLabel()
    {
        if (_advisorLabel == null)
            return;

        _advisorLabel.style.whiteSpace = WhiteSpace.Normal;
        _advisorLabel.style.overflow = Overflow.Hidden;
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

        string safeMessage = SanitizeAdvisorMessage(message);

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

        return $"Flood risk assessment complete. We identified {criticalText} and {highText}. "; // +
              // "You do not have enough resources to protect everything, so choose carefully. " +
               //"Inspect the highlighted zones and place barriers where they can protect low-lying, populated areas near the water first.";
    }

    private string FormatZoneCount(int count, string singularLabel, string pluralLabel)
    {
        return count == 1 ? $"1 {singularLabel}" : $"{count} {pluralLabel}";
    }

    private string SanitizeAdvisorMessage(string message)
    {
        string safeMessage = string.IsNullOrWhiteSpace(message)
            ? "Advisor update unavailable."
            : string.Join(" ", message.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

        if (safeMessage.Length <= maxAdvisorMessageCharacters)
            return safeMessage;

        int trimmedLength = Mathf.Max(1, maxAdvisorMessageCharacters - 3);
        return safeMessage.Substring(0, trimmedLength).TrimEnd() + "...";
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
        ResolveReferences();

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
