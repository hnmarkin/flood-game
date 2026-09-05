using UnityEngine;

/// <summary>
/// Controller seam for flood projections. It replaces immutable forecast results
/// when water or Game State changes, and never mutates or renders live water.
/// </summary>
public sealed class Dev_ProjectionController : MonoBehaviour
{
    [SerializeField] private Dev_WaterController waterController;
    [Min(0f)] [SerializeField] private float forecastSimulatedDuration;

    private int _transactionDepth;
    private bool _forecastDirty;

    public event System.Action<Dev_WaterProjection> OnForecastReplaced;
    public Dev_WaterProjection CurrentForecast { get; private set; }

    private void OnEnable()
    {
        if (waterController == null)
            return;

        waterController.OnWaterInitialized += OnWaterChanged;
        waterController.OnWaterProfileChanged += OnWaterProfileChanged;
        waterController.OnWaterSimulationStepped += OnWaterStepped;
        RefreshForecast();
    }

    private void OnDisable()
    {
        if (waterController == null)
            return;

        waterController.OnWaterInitialized -= OnWaterChanged;
        waterController.OnWaterProfileChanged -= OnWaterProfileChanged;
        waterController.OnWaterSimulationStepped -= OnWaterStepped;
    }

    /// <summary>Allows Game State to coalesce a completed-change transaction into one replacement.</summary>
    public void BeginForecastChangeTransaction()
    {
        _transactionDepth++;
    }

    public void EndForecastChangeTransaction()
    {
        if (_transactionDepth == 0)
            return;

        _transactionDepth--;
        if (_transactionDepth == 0 && _forecastDirty)
            RefreshForecast();
    }

    public void NotifyGameTimeAdvanced()
    {
        MarkForecastDirty();
    }

    public void NotifyCompletedDefenseChanged()
    {
        MarkForecastDirty();
    }

    public void NotifyWaterAffectingModifierChanged()
    {
        MarkForecastDirty();
    }

    public void SetForecastSimulatedDuration(float simulatedDuration)
    {
        if (float.IsNaN(simulatedDuration) || float.IsInfinity(simulatedDuration) || simulatedDuration < 0f)
            return;

        forecastSimulatedDuration = simulatedDuration;
        MarkForecastDirty();
    }

    public bool RefreshForecast()
    {
        if (waterController == null || !waterController.TryBuildProjection(forecastSimulatedDuration, out Dev_WaterProjection forecast))
            return false;

        _forecastDirty = false;
        CurrentForecast = forecast;
        OnForecastReplaced?.Invoke(forecast);
        return true;
    }

    /// <summary>
    /// Deliberate no-op placeholder for future hazard classification.
    ///
    /// The completed version will accept an immutable water projection and hazard
    /// configuration, classify hazardous cells, and pass that separate result to
    /// the projection overlay renderer. Do not add threshold, icon, or renderer
    /// behavior here until the hazard design is defined.
    /// </summary>
    public void CalculateHazards()
    {
        Debug.LogWarning(
            "[Dev_ProjectionController] CalculateHazards is a deliberate placeholder. " +
            "No hazard classification or overlay rendering has been implemented.");
    }

    private void OnWaterChanged(Dev_WaterController _)
    {
        MarkForecastDirty();
    }

    private void OnWaterProfileChanged(Dev_WaterProfileStage _)
    {
        MarkForecastDirty();
    }

    private void OnWaterStepped(Dev_WaterStepSummary _)
    {
        MarkForecastDirty();
    }

    private void MarkForecastDirty()
    {
        _forecastDirty = true;
        if (_transactionDepth == 0)
            RefreshForecast();
    }
}
