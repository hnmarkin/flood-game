using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class FloodDamagePanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloodDamageCalculator damageCalculator;

    private Label _zoneValue;
    private Label _totalDamageValue;
    private Label _avgDamageValue;
    private Label _severityValue;
    private string _currentGeoid;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[FloodDamagePanelController] No UIDocument found.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        _zoneValue         = root.Q<Label>("zone-value");
        _totalDamageValue  = root.Q<Label>("total-damage-value");
        _avgDamageValue    = root.Q<Label>("avg-damage-value");
        _severityValue     = root.Q<Label>("severity-value");

        if (damageCalculator != null)
        {
            damageCalculator.OnDamageUpdated += RefreshUI;
            damageCalculator.Initialize();
            RefreshUI();
        }
    }

    private void OnDisable()
    {
        if (damageCalculator != null)
            damageCalculator.OnDamageUpdated -= RefreshUI;
    }

    private void RefreshUI()
    {
        if (damageCalculator == null) return;

        if (string.IsNullOrWhiteSpace(_currentGeoid))
        {
            SetEmptyState();
            return;
        }

        if (!damageCalculator.TryGetZoneDamageSummary(_currentGeoid, out var summary))
        {
            SetEmptyState();
            return;
        }

        if (_zoneValue != null)
            _zoneValue.text = summary.Geoid;

        if (_totalDamageValue != null)
            _totalDamageValue.text = $"${summary.TotalEstimatedDamage:0}";

        if (_avgDamageValue != null)
            _avgDamageValue.text = $"{summary.AverageDamagePercent:0.0}%";

        if (_severityValue != null)
        {
            string severity = damageCalculator.GetSeverityLabel(summary);
            _severityValue.text = severity.ToUpper();

            _severityValue.RemoveFromClassList("severity-low");
            _severityValue.RemoveFromClassList("severity-moderate");
            _severityValue.RemoveFromClassList("severity-high");
            _severityValue.RemoveFromClassList("severity-severe");

            switch (severity)
            {
                case "Low":
                    _severityValue.AddToClassList("severity-low");
                    break;
                case "Moderate":
                    _severityValue.AddToClassList("severity-moderate");
                    break;
                case "High":
                    _severityValue.AddToClassList("severity-high");
                    break;
                default:
                    _severityValue.AddToClassList("severity-severe");
                    break;
            }
        }
    }

    public void SetZone(string geoid)
    {
        _currentGeoid = string.IsNullOrWhiteSpace(geoid) ? null : geoid.Trim();
        RefreshUI();
    }

    public void ClearZone()
    {
        _currentGeoid = null;
        SetEmptyState();
    }

    private void SetEmptyState()
    {
        if (_zoneValue != null)
            _zoneValue.text = "--";

        if (_totalDamageValue != null)
            _totalDamageValue.text = "$0";

        if (_avgDamageValue != null)
            _avgDamageValue.text = "0.0%";

        if (_severityValue == null)
            return;

        _severityValue.text = "LOW";
        _severityValue.RemoveFromClassList("severity-low");
        _severityValue.RemoveFromClassList("severity-moderate");
        _severityValue.RemoveFromClassList("severity-high");
        _severityValue.RemoveFromClassList("severity-severe");
        _severityValue.AddToClassList("severity-low");
    }
}
