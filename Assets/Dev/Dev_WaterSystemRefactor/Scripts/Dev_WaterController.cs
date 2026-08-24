using System;
using UnityEngine;

/// <summary>
/// Public interface for the new Dev Water System. It owns runtime state and
/// coordinates the map accessor, simulation engine, and renderer.
/// </summary>
public class Dev_WaterController : MonoBehaviour
{
    [Header("Map Data")]
    [SerializeField] private Dev_WaterMapData mapData;

    [Header("Scenario Configuration")]
    [SerializeField] private Dev_WaterScenarioConfig scenarioConfig;

    [Header("Rendering")]
    [SerializeField] private Dev_WaterTilemapRenderer waterRenderer;

    [Header("Lifecycle")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool startOnPlay;

    [Header("Simulation Stepping")]
    [SerializeField] private Dev_WaterStepMode stepMode = Dev_WaterStepMode.Automatic;
    [Min(0.05f)]
    [SerializeField] private float autoStepInterval = 0.5f;
    [Tooltip("Dev-only test input until the project input system exists.")]
    [SerializeField] private bool spaceKeyStepsWhenManual = true;

    private Dev_WaterMapAccessor _mapAccessor;
    private Dev_WaterRuntimeState _runtimeState;
    private Dev_WaterSimulationEngine _engine;
    private Dev_WaterBarrierGrid _barrierGrid;
    private Dev_WaterSimulationSettings _resolvedSettings;
    private Dev_WaterSourceSpec[] _resolvedInitialSources = Array.Empty<Dev_WaterSourceSpec>();
    private Dev_WaterSourceSpec[] _resolvedContinuousSources = Array.Empty<Dev_WaterSourceSpec>();
    private float _autoStepTimer;
    private bool _initialized;
    private bool _sourcesApplied;
    private bool _simulationRunning;
    private Dev_WaterStepSummary _lastSummary;

    public event Action<Dev_WaterController> OnWaterInitialized;
    public event Action<Dev_WaterController> OnWaterSimulationStarted;
    public event Action<Dev_WaterController> OnWaterSimulationPaused;
    public event Action<Dev_WaterController> OnWaterSimulationReset;
    public event Action<Dev_WaterStepSummary> OnWaterSimulationStepped;

    public bool IsInitialized => _initialized;
    public bool IsSimulationRunning => _simulationRunning;

    private void Start()
    {
        if (initializeOnStart)
            InitializeRuntimeState();

        if (startOnPlay)
            BeginSimulation();
    }

    private void Update()
    {
        if (!_simulationRunning)
            return;

        _engine.TickSpreadGate(Time.deltaTime);

        if (stepMode == Dev_WaterStepMode.Manual)
        {
            if (spaceKeyStepsWhenManual && Input.GetKeyDown(KeyCode.Space))
                StepSimulation();

            return;
        }

        _autoStepTimer += Time.deltaTime;
        if (_autoStepTimer < autoStepInterval)
            return;

        _autoStepTimer = 0f;
        StepSimulation();
    }

    private void OnValidate()
    {
        autoStepInterval = Mathf.Max(0.05f, autoStepInterval);
    }

    public bool CanStartSimulation()
    {
        return mapData != null;
    }

    public bool BeginSimulation()
    {
        if (_simulationRunning)
        {
            Debug.LogWarning("[Dev_WaterController] Simulation is already running.");
            return false;
        }

        if (!EnsureInitialized())
            return false;

        ApplyInitialSourcesIfNeeded();
        _simulationRunning = true;
        _autoStepTimer = 0f;

        OnWaterSimulationStarted?.Invoke(this);
        return true;
    }

    public void PauseSimulation()
    {
        if (!_simulationRunning)
            return;

        _simulationRunning = false;
        OnWaterSimulationPaused?.Invoke(this);
    }

    public bool ResumeSimulation()
    {
        if (!EnsureInitialized())
            return false;

        ApplyInitialSourcesIfNeeded();
        _simulationRunning = true;
        return true;
    }

    public bool ResetSimulation()
    {
        _simulationRunning = false;
        _sourcesApplied = false;
        _autoStepTimer = 0f;

        bool initialized = InitializeRuntimeState();
        if (initialized)
            OnWaterSimulationReset?.Invoke(this);

        return initialized;
    }

    public bool StepSimulation()
    {
        if (!EnsureInitialized())
            return false;

        ApplyInitialSourcesIfNeeded();
        _lastSummary = _engine.Step(_resolvedContinuousSources, Dev_WaterModifierSnapshot.Defaults());
        waterRenderer?.ApplyDirty();

        OnWaterSimulationStepped?.Invoke(_lastSummary);
        return true;
    }

    public Dev_WaterStepSummary GetLastStepSummary()
    {
        return _lastSummary;
    }

    public float GetWaterDepth(Vector2Int tileCell)
    {
        return _runtimeState != null ? _runtimeState.GetWaterDepth(tileCell) : 0f;
    }

    public bool TrySetWaterDepth(Vector2Int tileCell, float depth)
    {
        if (_runtimeState == null)
        {
            Debug.LogWarning("[Dev_WaterController] Cannot set water depth before runtime state is initialized.");
            return false;
        }

        if (float.IsNaN(depth) || float.IsInfinity(depth))
        {
            Debug.LogWarning("[Dev_WaterController] Water depth must be a finite value.");
            return false;
        }

        float clampedDepth = Mathf.Max(0f, depth);
        if (_resolvedSettings != null && _resolvedSettings.maxWaterDepth > 0f)
            clampedDepth = Mathf.Min(clampedDepth, _resolvedSettings.maxWaterDepth);

        if (!_runtimeState.TrySetWaterDepth(tileCell, clampedDepth))
        {
            Debug.LogWarning($"[Dev_WaterController] Cannot set water depth at invalid tile cell {tileCell}.");
            return false;
        }

        waterRenderer?.ApplyDirty();
        return true;
    }

    public bool InitializeRuntimeState()
    {
        if (mapData == null)
        {
            Debug.LogError("[Dev_WaterController] Cannot initialize: Dev_WaterMapData is not assigned.");
            _initialized = false;
            return false;
        }

        ResolveConfiguration();
        _mapAccessor = new Dev_WaterMapAccessor(mapData);
        _runtimeState = new Dev_WaterRuntimeState(_mapAccessor);

        _barrierGrid = new Dev_WaterBarrierGrid();
        if (!_barrierGrid.InitializeForSimulation(_runtimeState.GridWidth, _runtimeState.GridHeight))
        {
            _initialized = false;
            return false;
        }

        _engine = new Dev_WaterSimulationEngine(_mapAccessor, _barrierGrid);
        _engine.Initialize(_runtimeState, _resolvedSettings);

        if (waterRenderer != null)
            waterRenderer.Initialize(_runtimeState, _mapAccessor);

        _initialized = true;
        _sourcesApplied = false;
        _lastSummary = default;

        OnWaterInitialized?.Invoke(this);
        return true;
    }

    private bool EnsureInitialized()
    {
        if (_initialized && _runtimeState != null && _engine != null)
            return true;

        return InitializeRuntimeState();
    }

    private void ApplyInitialSourcesIfNeeded()
    {
        if (_sourcesApplied)
            return;

        _engine.ApplyInitialSources(_resolvedInitialSources, Dev_WaterModifierSnapshot.Defaults());
        _engine.InitializeActiveRegion();
        waterRenderer?.ApplyDirty();
        _sourcesApplied = true;
    }

    private void ResolveConfiguration()
    {
        if (scenarioConfig == null)
        {
            _resolvedSettings = new Dev_WaterSimulationSettings();
            _resolvedSettings.Sanitize();
            _resolvedInitialSources = Array.Empty<Dev_WaterSourceSpec>();
            _resolvedContinuousSources = Array.Empty<Dev_WaterSourceSpec>();
            return;
        }

        _resolvedSettings = scenarioConfig.CreateSettingsInstance();
        _resolvedInitialSources = scenarioConfig.CreateInitialSourceInstances();
        _resolvedContinuousSources = scenarioConfig.CreateContinuousSourceInstances();
    }
}
