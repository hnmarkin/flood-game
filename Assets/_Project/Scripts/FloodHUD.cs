using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Runtime;

public class FloodHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaterSimulator waterSimulator;
    [SerializeField] private TileMapData tileMapData;

    [Header("UI")]
    [SerializeField] private Slider floodSlider;             // 0..1
    [SerializeField] private TextMeshProUGUI floodLabel;     // "XX% Flooded"

    [Header("Alert Settings")]
    [Range(0f, 1f)]
    public float floodThreshold = 0.6f;                      // critical threshold (60% by default)
    [Range(0f, 1f)]
    public float warningThreshold = 0.2f;                    // warning threshold (20% by default)

    public UnityEvent onFloodThresholdReached;               // hook popup here (critical)
    public UnityEvent onFloodWarningReached;                 // hook popup here (warning)

    [Tooltip("Ignore tiny puddles below this depth when counting flooded tiles.")]
    public float minFloodDepth = 0.01f;

    private bool _criticalFired = false;
    private bool _warningFired = false;
    private float _currentFloodFraction = 0f;
    // Initial counts used to compute progress relative to initial land tiles
    private int _initialLandTiles = 0;
    private int _initialWaterTiles = 0;
    private bool _initialCountsSet = false;

    private void OnEnable()
    {
        if (waterSimulator != null)
        {
            waterSimulator.OnSimulationStep += HandleSimulationStep;
        }
    }

    private void OnDisable()
    {
        if (waterSimulator != null)
        {
            waterSimulator.OnSimulationStep -= HandleSimulationStep;
        }
    }

    private void HandleSimulationStep()
    {
        // Compute initial counts on first step when simulation is ready
        if (!_initialCountsSet && tileMapData != null && tileMapData.simInitialized)
        {
            ComputeInitialCounts();
        }

        UpdateFloodFraction();
        UpdateUI();
        CheckThreshold();
    }

    private void UpdateFloodFraction()
    {
        if (tileMapData == null || !tileMapData.simInitialized)
        {
            _currentFloodFraction = 0f;
            return;
        }

        int N = tileMapData.N;
        if (N <= 0)
        {
            _currentFloodFraction = 0f;
            return;
        }

        int flooded = 0;
        int total = N * N;

        // water[,] is (N+2)x(N+2) with boundary walls, so use 1..N
        for (int y = 1; y <= N; y++)
        {
            for (int x = 1; x <= N; x++)
            {
                if (tileMapData.water[x, y] > minFloodDepth)
                {
                    flooded++;
                }
            }
        }

        // If we have initial counts available, compute progress relative to
        // initially dry land: (current water - initial water) / initial land
        if (_initialCountsSet && _initialLandTiles > 0)
        {
            int newlyFlooded = flooded - _initialWaterTiles;
            float frac = (float)newlyFlooded / (float)_initialLandTiles;
            _currentFloodFraction = Mathf.Clamp01(frac);
        }
        else
        {
            // Fallback to simple fraction until initial counts are available
            Debug.LogWarning("[FloodHUD] Initial counts not set; using simple flooded/total fraction.");
            _currentFloodFraction = (float)flooded / total;
        }
    }

    private void ComputeInitialCounts()
    {
        if (tileMapData == null)
        {
            Debug.LogWarning("[FloodHUD] TileMapData is null, cannot compute initial counts.");
            _initialCountsSet = true;
            return;
        }

        int N = tileMapData.N;
        if (N <= 0)
        {
            _initialWaterTiles = 0;
            _initialLandTiles = 0;
            _initialCountsSet = true;
            return;
        }

        int water = 0;
        for (int y = 1; y <= N; y++)
        {
            for (int x = 1; x <= N; x++)
            {
                if (tileMapData.water[x, y] > minFloodDepth)
                    water++;
            }
        }

        _initialWaterTiles = water;
        _initialLandTiles = N * N - _initialWaterTiles;
        _initialCountsSet = true;

        Debug.Log($"[FloodHUD] Initial counts computed: water={_initialWaterTiles}, land={_initialLandTiles}");
    }

    private void UpdateUI()
    {
        if (floodSlider != null)
        {
            floodSlider.value = _currentFloodFraction;  // slider min=0 max=1
        }

        if (floodLabel != null)
        {
            float pct = _currentFloodFraction * 100f;
            floodLabel.text = $"{pct:0}% Flooded";
        }
    }

    private void CheckThreshold()
    {
        if (_warningFired && _criticalFired)
            return;

        // Warning threshold
        if (!_warningFired && _currentFloodFraction >= warningThreshold)
        {
            _warningFired = true;
            onFloodWarningReached?.Invoke();
            var warnData = new AlertData
            {
                type = AlertType.Warning,
                message = $"Flood level has reached {warningThreshold * 100f:0}% (Warning)."
            };
            AlertBus.RaiseAlert(warnData);
        }

        // Critical threshold
        if (!_criticalFired && _currentFloodFraction >= floodThreshold)
        {
            _criticalFired = true;
            onFloodThresholdReached?.Invoke();
            var critData = new AlertData
            {
                type = AlertType.Critical,
                message = $"Flood level has reached {floodThreshold * 100f:0}% (Critical)!"
            };
            AlertBus.RaiseAlert(critData);
        }
    }

    // Optional helper if you want to reset between runs:
    public void ResetAlert()
    {
        _criticalFired = false;
        _warningFired = false;
    }
}
