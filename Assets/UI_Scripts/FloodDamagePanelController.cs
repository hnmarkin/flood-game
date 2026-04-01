using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class FloodDamagePanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloodDamageCalculator damageCalculator;

    private Label _floodedTilesValue;
    private Label _maxDepthValue;
    private Label _totalDamageValue;
    private Label _avgDamageValue;
    private Label _severityValue;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[FloodDamagePanelController] No UIDocument found.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        _floodedTilesValue = root.Q<Label>("flooded-tiles-value");
        _maxDepthValue     = root.Q<Label>("max-depth-value");
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

        if (_floodedTilesValue != null)
            _floodedTilesValue.text = damageCalculator.FloodedTileCount.ToString();

        if (_maxDepthValue != null)
            _maxDepthValue.text = $"{damageCalculator.MaxDepthReached:0.00} m";

        if (_totalDamageValue != null)
            _totalDamageValue.text = $"${damageCalculator.TotalEstimatedDamage:0}";

        if (_avgDamageValue != null)
            _avgDamageValue.text = $"{damageCalculator.AverageDamagePercent:0.0}%";

        if (_severityValue != null)
            _severityValue.text = damageCalculator.GetSeverityLabel();

        if (_severityValue != null)
        {
            string severity = damageCalculator.GetSeverityLabel();
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
}