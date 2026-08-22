using System;
using UnityEngine;

/// <summary>
/// Public entry point for the Dev Water System. It owns the live runtime state,
/// coordinates simulation and rendering, and prevents other systems from reaching into water state directly.
/// </summary>
public class Dev_WaterController : MonoBehaviour
{
    [Header("Map Input")]
    [SerializeField] private TileMapData tileMapData;

    [Header("Scenario Config")]
    [SerializeField] private Dev_WaterScenarioConfig scenarioConfig;

    [Header("Fallback Settings")]
    [SerializeField] private Dev_WaterSimulationSettings simulationSettings = new Dev_WaterSimulationSettings();

    [SerializeField] private Dev_WaterSourceSpec[] initialSources =
    {
        new Dev_WaterSourceSpec
        {
            kind = Dev_WaterSourceKind.ExistingWaterBodies,
            depth = 10f,
            scaleByExternalWaterLoad = true
        }
    };

    [SerializeField] private Dev_WaterSourceSpec[] continuousSources;

    [Header("Rendering")]
    [SerializeField] private Dev_WaterTilemapRenderer waterRenderer;

    [Header("Barrier Data")]
    [SerializeField] private Dev_WaterBarrierGrid barrierGrid;

    [Header("Lifecycle")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool startOnPlay;

    [Header("Simulation Stepping")]
    [SerializeField] private Dev_WaterStepMode stepMode = Dev_WaterStepMode.Automatic;
    [Min(0.05f)]
    [SerializeField] private float autoStepInterval = 0.5f;
    [Tooltip("Dev-only test input until the project input system exists.")]
    [SerializeField] private bool spaceKeyStepsWhenManual = true;

    private Dev_WaterRuntimeState _runtimeState;
    private Dev_WaterSimulationEngine _engine;
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

        _engine?.TickSpreadGate(Time.deltaTime);

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
        simulationSettings?.Sanitize();
    }

    public bool CanStartSimulation()
    {
        return tileMapData != null && barrierGrid != null;
    }

    public void BeginSimulationFromUI()
    {
        BeginSimulation();
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
        if (tileMapData == null)
        {
            Debug.LogError("[Dev_WaterController] Cannot initialize: TileMapData is not assigned.");
            _initialized = false;
            return false;
        }

        ResolveConfiguration();

        _runtimeState = Dev_WaterTileMapDataAdapter.CreateRuntimeState(tileMapData);
        if (_runtimeState == null)
        {
            _initialized = false;
            return false;
        }

        if (barrierGrid == null)
        {
            Debug.LogError("[Dev_WaterController] Cannot initialize: Dev_WaterBarrierGrid is not assigned.");
            _initialized = false;
            return false;
        }

        if (!barrierGrid.InitializeForSimulation(_runtimeState.GridWidth, _runtimeState.GridHeight))
        {
            _initialized = false;
            return false;
        }

        _engine = new Dev_WaterSimulationEngine(barrierGrid);
        _engine.Initialize(_runtimeState, _resolvedSettings);

        if (waterRenderer != null)
        {
            waterRenderer.SetTileMapData(tileMapData);
            waterRenderer.Initialize(_runtimeState);
        }

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
        if (scenarioConfig != null)
        {
            _resolvedSettings = scenarioConfig.CreateSettingsInstance();
            _resolvedInitialSources = scenarioConfig.CreateInitialSourceInstances();
            _resolvedContinuousSources = scenarioConfig.CreateContinuousSourceInstances();
            return;
        }

        _resolvedSettings = simulationSettings != null
            ? simulationSettings.Clone()
            : new Dev_WaterSimulationSettings();
        _resolvedSettings.Sanitize();
        _resolvedInitialSources = CloneSources(initialSources);
        _resolvedContinuousSources = CloneSources(continuousSources);
    }

    private static Dev_WaterSourceSpec[] CloneSources(Dev_WaterSourceSpec[] sources)
    {
        if (sources == null || sources.Length == 0)
            return Array.Empty<Dev_WaterSourceSpec>();

        Dev_WaterSourceSpec[] clones = new Dev_WaterSourceSpec[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            clones[i] = sources[i] != null ? sources[i].Clone() : null;

        return clones;
    }
}
