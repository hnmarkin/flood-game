using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AlertsController : MonoBehaviour
{
    private const string HighRiskZonesLabelName = "high_risk_zones_alert_label";
    private const string BarrierUnitsLabelName = "barrier_units_alert_label";
    private const string TimeBeforeFloodLabelName = "time_before_flood_alert_label";

    [Header("References")]
    [SerializeField] private UIDocument alertsUIDocument;
    [SerializeField] private ZoneBaselineRiskController baselineRiskController;

    [Header("Alert Values")]
    [SerializeField] private int barrierUnitsAvailable = 8;
    [SerializeField] private int turnsBeforeFlood = 3;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Label _highRiskZonesLabel;
    private Label _barrierUnitsLabel;
    private Label _timeBeforeFloodLabel;
    private Coroutine _bindRoutine;
    private bool _uiBound;

    private void Awake()
    {
        if (alertsUIDocument == null)
            alertsUIDocument = GetComponent<UIDocument>();

        if (baselineRiskController == null)
            baselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();
    }

    private void OnEnable()
    {
        BindUI();

        if (!_uiBound && _bindRoutine == null)
            _bindRoutine = StartCoroutine(BindUIWhenReady());

        if (baselineRiskController != null)
            baselineRiskController.BaselineRiskCalculated += OnBaselineRiskCalculated;

        RefreshAlerts();
    }

    private void OnDisable()
    {
        if (baselineRiskController != null)
            baselineRiskController.BaselineRiskCalculated -= OnBaselineRiskCalculated;

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        _highRiskZonesLabel = null;
        _barrierUnitsLabel = null;
        _timeBeforeFloodLabel = null;
        _uiBound = false;
    }

    public void RefreshAlerts()
    {
        if (!_uiBound && !BindUI())
            return;

        SetAlertText(_highRiskZonesLabel, BuildHighRiskZonesText(GetRiskResults()));
        SetAlertText(_barrierUnitsLabel, $"Barrier units available: {Mathf.Max(0, barrierUnitsAvailable)}");
        SetAlertText(_timeBeforeFloodLabel, BuildTurnsText(Mathf.Max(0, turnsBeforeFlood)));

        if (debugLogs)
            Debug.Log("[AlertsController] Refreshed strategy-phase alerts.");
    }

    public void SetBarrierUnitsAvailable(int available)
    {
        barrierUnitsAvailable = Mathf.Max(0, available);
        RefreshAlerts();
    }

    public void SetTurnsBeforeFlood(int turns)
    {
        turnsBeforeFlood = Mathf.Max(0, turns);
        RefreshAlerts();
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
            Debug.LogWarning("[AlertsController] Could not bind the alerts labels from the assigned UIDocument.");

        _bindRoutine = null;
    }

    private bool BindUI()
    {
        if (_uiBound || alertsUIDocument == null)
            return _uiBound;

        VisualElement root = alertsUIDocument.rootVisualElement;

        if (root == null)
            return false;

        _highRiskZonesLabel = root.Q<Label>(HighRiskZonesLabelName);
        _barrierUnitsLabel = root.Q<Label>(BarrierUnitsLabelName);
        _timeBeforeFloodLabel = root.Q<Label>(TimeBeforeFloodLabelName);

        if (_highRiskZonesLabel == null)
            Debug.LogWarning($"[AlertsController] Could not find Label named '{HighRiskZonesLabelName}'.");

        if (_barrierUnitsLabel == null)
            Debug.LogWarning($"[AlertsController] Could not find Label named '{BarrierUnitsLabelName}'.");

        if (_timeBeforeFloodLabel == null)
            Debug.LogWarning($"[AlertsController] Could not find Label named '{TimeBeforeFloodLabelName}'.");

        _uiBound = _highRiskZonesLabel != null || _barrierUnitsLabel != null || _timeBeforeFloodLabel != null;
        return _uiBound;
    }

    private IReadOnlyList<ZoneBaselineRiskData> GetRiskResults()
    {
        if (baselineRiskController == null)
            return null;

        if (!baselineRiskController.EnsureBaselineRiskCalculated())
            return null;

        return baselineRiskController.GetAllRiskResults();
    }

    private string BuildHighRiskZonesText(IReadOnlyList<ZoneBaselineRiskData> riskResults)
    {
        if (riskResults == null)
            return "High-risk zones pending assessment";

        int highRiskCount = 0;

        for (int i = 0; i < riskResults.Count; i++)
        {
            if (riskResults[i].riskLevel == RiskLevel.High || riskResults[i].riskLevel == RiskLevel.Critical)
                highRiskCount++;
        }

        return $"{highRiskCount} high-risk {(highRiskCount == 1 ? "zone" : "zones")} identified";
    }

    private string BuildTurnsText(int turns)
    {
        string suffix = turns == 1 ? "turn" : "turns";
        return $"Time before flood: {turns} {suffix}";
    }

    private void SetAlertText(Label label, string value)
    {
        if (label != null)
            label.text = value;
    }

    private void OnBaselineRiskCalculated()
    {
        RefreshAlerts();
    }
}
