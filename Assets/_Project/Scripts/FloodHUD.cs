using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

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
    public float floodThreshold = 0.6f;                      // 60% by default
    public UnityEvent onFloodThresholdReached;               // hook popup here

    [Tooltip("Ignore tiny puddles below this depth when counting flooded tiles.")]
    public float minFloodDepth = 0.01f;

    private bool _thresholdFired = false;
    private float _currentFloodFraction = 0f;

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

        _currentFloodFraction = (float)flooded / total;
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
        if (_thresholdFired)
            return;

        if (_currentFloodFraction >= floodThreshold)
        {
            _thresholdFired = true;
            onFloodThresholdReached?.Invoke();
        }
    }

    // Optional helper if you want to reset between runs:
    public void ResetAlert()
    {
        _thresholdFired = false;
    }
}
