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
    [SerializeField] private ShelterController shelterController;
    [SerializeField] private EvacuationController evacuationController;
    [SerializeField] private CommunicationTowerController communicationTowerController;

    [Header("Popup Timing")]
    [SerializeField] private float popupDelay = 0.2f;
    [SerializeField] private float secondsPerCharacter = 0.02f;
    [SerializeField] private float autoHideAfterSeconds;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Advisor Tone")]
    [SerializeField, Min(120)] private int maxAdvisorMessageCharacters = 280;
    [SerializeField] private bool useVariedMessages = true;

    [Header("Message Safety")]
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
    private bool _subscribedToShelterController;
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
        maxAdvisorMessageCharacters = Mathf.Max(120, maxAdvisorMessageCharacters);
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

        string message = BuildBaselineRiskAdvisorMessage(baselineRiskController.GetAllRiskResults());
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

        if (shelterController == null)
            shelterController = FindFirstObjectByType<ShelterController>();

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

        if (shelterController != null && !_subscribedToShelterController)
        {
            shelterController.OnShelterPlaced += OnShelterPlaced;
            _subscribedToShelterController = true;
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

        if (shelterController != null && _subscribedToShelterController)
        {
            shelterController.OnShelterPlaced -= OnShelterPlaced;
            _subscribedToShelterController = false;
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
        placed = Mathf.Max(0, placed);
        required = Mathf.Max(1, required);

        if (placed >= required)
        {
            ShowAdvisorMessage(PickMessage(
                $"Barrier count reached {placed}/{required}. A wall can slow water, but it cannot carry anyone home. The next weakness may be movement.",
                $"Barriers are in place: {placed}/{required}. Good pressure against the river. Now the question is whether people can still reach safety.",
                $"Defense line complete: {placed}/{required}. The flood has resistance now. But resistance is not rescue."
            ));

            return;
        }

        ShowAdvisorMessage(PickMessage(
            $"Barrier placed: {placed}/{required}. Useful move. Water rarely attacks politely, so watch the bends, roads, and low edges.",
            $"One more barrier down: {placed}/{required}. It may help, or it may only buy time. Time is still valuable.",
            $"Barrier progress: {placed}/{required}. The river just met an obstacle. Let’s see whether the people behind it have options."
        ));
    }

    private void OnFloodSimulationStarted()
    {
        ShowAdvisorMessage(PickMessage(
            "The flood is moving. The map will start telling the truth now; watch what fails first.",
            "Water is active. From here, every delay has a cost, even when the damage is not visible yet.",
            "The storm has entered the board. Roads, warnings, and shelters may matter more than they did a moment ago."
        ));
    }

    private void OnShelterPlaced(PlacedShelterData shelter)
    {
        string shelterId = DemoDisplayIdFormatter.GetShelterDisplayId(shelter != null ? shelter.shelterId : null);
        int zoneCount = shelter != null && shelter.associatedZoneGeoids != null ? shelter.associatedZoneGeoids.Count : 0;
        int capacity = shelter != null ? Mathf.Max(0, shelter.capacity) : 0;

        string zonesText = FormatZoneCount(zoneCount, "zone", "zones");

        ShowAdvisorMessage(PickMessage(
            $"{shelterId} is active: {zonesText}, {capacity:N0} capacity. A safe place exists now; whether people can find it is another question.",
            $"{shelterId} can hold {capacity:N0}. That changes the map, but not the outcome by itself. Access and awareness still matter.",
            $"Shelter online: {shelterId}. It covers {zonesText}. Good refuge, but refuge without a clear path can become an island."
        ));
    }

    private void OnEvacuationRouteSelected(SimpleEvacuationRoute route)
    {
        if (evacuationController != null && evacuationController.HasConfirmedEvacuationRoute)
            return;

        string routeId = DemoDisplayIdFormatter.GetEvacuationRouteDisplayId(route != null ? route.routeId : null);

        ShowAdvisorMessage(PickMessage(
            $"{routeId} is under review. Short paths are tempting; dry paths are usually wiser.",
            $"{routeId} selected. It looks useful, but water has a habit of changing the meaning of a road.",
            $"Route preview active: {routeId}. A line on the map is only as good as the ground beneath it."
        ));
    }

    private void OnEvacuationRouteConfirmed(SimpleEvacuationRoute route)
    {
        string routeId = DemoDisplayIdFormatter.GetEvacuationRouteDisplayId(route != null ? route.routeId : null);
        string zoneId = DemoDisplayIdFormatter.GetZoneDisplayId(route != null ? route.sourceZoneGeoid : null);
        string shelterId = DemoDisplayIdFormatter.GetShelterDisplayId(route != null ? route.destinationShelterId : null);

        ShowAdvisorMessage(PickMessage(
            $"{routeId} confirmed: {zoneId} to {shelterId}. A path exists now. The question is whether the warning reaches people in time.",
            $"{zoneId} now has a route to {shelterId}. That is a strong piece on the board, but the flood is still moving.",
            $"Evacuation route locked: {routeId}. Good. Keep an eye on the roads near rising water; routes age quickly in a storm."
        ));
    }

    private void OnTowerActivated(CommunicationTowerData tower)
    {
        string towerId = DemoDisplayIdFormatter.GetTowerDisplayId(tower != null ? tower.towerId : null);
        int zoneCount = tower != null && tower.associatedZoneGeoids != null ? tower.associatedZoneGeoids.Count : 0;

        string zonesText = FormatZoneCount(zoneCount, "zone", "zones");

        ShowAdvisorMessage(PickMessage(
            $"{towerId} is online. {zonesText} can hear you now. What they hear, and when, may decide more than the tower itself.",
            $"Signal restored from {towerId}. Information moves faster than floodwater, but only if it is sent early enough.",
            $"{towerId} activated. The city has a voice again. Some zones may need that voice more urgently than others."
        ));
    }

    private void OnZoneWarned(string geoid)
    {
        string zoneId = DemoDisplayIdFormatter.GetZoneDisplayId(geoid);

        ShowAdvisorMessage(PickMessage(
            $"Warning sent to {zoneId}. Now the message is out. Whether it becomes safety depends on shelter, route, and timing.",
            $"{zoneId} has been warned. Good signal. But a warning without a reachable destination can still leave people stranded.",
            $"Alert delivered to {zoneId}. The clock just became kinder there, but only a little."
        ));
    }

    private string BuildBaselineRiskAdvisorMessage(IReadOnlyList<ZoneBaselineRiskData> riskResults)
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

        return PickMessage(
            $"Risk scan complete: {criticalText}, {highText}. The obvious danger is water; the hidden danger is isolation.",
            $"The map is uneasy: {criticalText} and {highText}. Look for places where people, roads, and rising water meet.",
            $"Inspection complete. We have {criticalText} and {highText}. Some zones need protection; others may need a way out.",
            $"Risk data is in. {criticalText}, {highText}. The hardest choices are not always where the water is deepest."
        );
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
            yield return WaitForSecondsSafe(popupDelay);

        SetPopupVisible(true);

        string safeMessage = SanitizeAdvisorMessage(message);

        for (int i = 1; i <= safeMessage.Length; i++)
        {
            _advisorLabel.text = safeMessage.Substring(0, i);

            if (secondsPerCharacter > 0f)
                yield return WaitForSecondsSafe(secondsPerCharacter);
            else
                yield return null;
        }

        _typingRoutine = null;

        if (autoHideAfterSeconds > 0f)
            _autoHideRoutine = StartCoroutine(AutoHideRoutine(autoHideAfterSeconds));
    }

    private IEnumerator AutoHideRoutine(float delaySeconds)
    {
        yield return WaitForSecondsSafe(delaySeconds);
        _autoHideRoutine = null;
        HideAdvisorPanel();
    }

    private IEnumerator WaitForSecondsSafe(float seconds)
    {
        if (seconds <= 0f)
            yield break;

        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(seconds);
        else
            yield return new WaitForSeconds(seconds);
    }

    private string PickMessage(params string[] options)
    {
        if (options == null || options.Length == 0)
            return "Advisor signal unclear. Recheck the map before committing the next move.";

        if (!useVariedMessages)
            return options[0];

        int index = UnityEngine.Random.Range(0, options.Length);
        return options[index];
    }

    private string FormatZoneCount(int count, string singularLabel, string pluralLabel)
    {
        return count == 1 ? $"1 {singularLabel}" : $"{count} {pluralLabel}";
    }

    private string SanitizeAdvisorMessage(string message)
    {
        string safeMessage = string.IsNullOrWhiteSpace(message)
            ? "Advisor signal unclear. Recheck the map before committing the next move."
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