using System;
using UnityEngine;

/// <summary>
/// Public interface for live Dev water. It owns runtime state, profile transitions,
/// simulated-time stepping, modifier resolution, and immutable projection inputs.
/// </summary>
public class Dev_WaterController : MonoBehaviour
{
    [Header("Map Data")]
    [SerializeField] private Dev_WaterMapData mapData;

    [Header("Scenario Configuration")]
    [SerializeField] private Dev_WaterScenarioConfig scenarioConfig;
    [SerializeField] private Dev_WaterConfigurationMode configurationMode = Dev_WaterConfigurationMode.DevDefaultsWithWarnings;

    [Header("Modifier Contract")]
    [Tooltip("Must implement IDev_WaterModifierProvider. Production refuses defaults.")]
    [SerializeField] private MonoBehaviour modifierProviderBehaviour;

    [Header("Rendering")]
    [SerializeField] private Dev_WaterTilemapRenderer waterRenderer;

    [Header("Lifecycle")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool startOnPlay;

    [Header("Simulation Stepping")]
    [SerializeField] private Dev_WaterStepMode stepMode = Dev_WaterStepMode.Automatic;
    [Min(0.05f)] [SerializeField] private float autoStepInterval = 0.5f;
    [Tooltip("Dev-only test input until the project input system exists.")]
    [SerializeField] private bool spaceKeyStepsWhenManual = true;

    private Dev_WaterMapAccessor _mapAccessor;
    private Dev_WaterRuntimeState _runtimeState;
    private Dev_WaterSimulationEngine _engine;
    private Dev_WaterBarrierGrid _barrierGrid;
    private Dev_WaterSimulationSettings _resolvedSettings;
    private Dev_WaterSourceSpec[] _resolvedInitialSources = Array.Empty<Dev_WaterSourceSpec>();
    private Dev_WaterSourceSpec[] _resolvedContinuousSources = Array.Empty<Dev_WaterSourceSpec>();
    private Dev_WaterProfileStage _activeProfileStage;
    private Dev_WaterGameFlow _gameFlow = Dev_WaterGameFlow.Loading;
    private Dev_WaterGamePhase _gamePhase = Dev_WaterGamePhase.Preparation;
    private float _autoStepTimer;
    private bool _initialized;
    private bool _simulationRunning;
    private Dev_WaterStepSummary _lastSummary;

    public event Action<Dev_WaterController> OnWaterInitialized;
    public event Action<Dev_WaterController> OnWaterSimulationStarted;
    public event Action<Dev_WaterController> OnWaterSimulationPaused;
    public event Action<Dev_WaterController> OnWaterSimulationReset;
    public event Action<Dev_WaterProfileStage> OnWaterProfileChanged;
    public event Action<Dev_WaterStepSummary> OnWaterSimulationStepped;

    public bool IsInitialized => _initialized;
    public bool IsSimulationRunning => _simulationRunning;
    public Dev_WaterProfileStage ActiveProfileStage => _activeProfileStage;

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
        return mapData != null && (scenarioConfig != null || configurationMode == Dev_WaterConfigurationMode.DevDefaultsWithWarnings);
    }

    /// <summary>Dev/manual entry point. Production callers should drive SetGameFlow and SetGamePhase.</summary>
    public bool BeginSimulation()
    {
        if (_simulationRunning)
            return false;

        if (configurationMode == Dev_WaterConfigurationMode.Production &&
            (_gameFlow != Dev_WaterGameFlow.Gameplay || _gamePhase != Dev_WaterGamePhase.Crisis))
        {
            Debug.LogError("[Dev_WaterController] Production automatic stepping requires Gameplay and Crisis lifecycle states.");
            return false;
        }

        if (!EnsureInitialized())
            return false;

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
        return BeginSimulation();
    }

    public bool ResetSimulation()
    {
        _simulationRunning = false;
        _autoStepTimer = 0f;

        bool initialized = InitializeRuntimeState();
        if (initialized)
            OnWaterSimulationReset?.Invoke(this);

        return initialized;
    }

    /// <summary>Receives Game Flow changes; it does not create or own the Game Flow state machine.</summary>
    public void SetGameFlow(Dev_WaterGameFlow gameFlow)
    {
        _gameFlow = gameFlow;
        RefreshLifecycleStepping();
    }

    /// <summary>Receives Game Phase changes; it does not create or own the Game Phase state machine.</summary>
    public bool SetGamePhase(Dev_WaterGamePhase gamePhase)
    {
        _gamePhase = gamePhase;
        if (gamePhase == Dev_WaterGamePhase.Crisis && _activeProfileStage != Dev_WaterProfileStage.Crisis)
        {
            if (!TryApplyProfile(Dev_WaterProfileStage.Crisis))
                return false;
        }

        RefreshLifecycleStepping();
        return true;
    }

    /// <summary>Runs normal physics for an explicit amount of simulated time using the preliminary profile.</summary>
    public bool RunPreliminaryFlooding(float simulatedDuration)
    {
        if (!IsFinitePositive(simulatedDuration))
        {
            Debug.LogError("[Dev_WaterController] Preliminary flooding requires a finite positive simulated duration.");
            return false;
        }

        if (!EnsureInitialized() || !TryApplyProfile(Dev_WaterProfileStage.Preliminary))
            return false;

        return RunSimulationForDuration(simulatedDuration);
    }

    public bool TryGetPreliminaryFlooding(out Dev_WaterPreliminaryFloodingConfig configuration)
    {
        configuration = null;
        return scenarioConfig != null && scenarioConfig.TryGetPreliminaryFlooding(out configuration, out _);
    }

    /// <summary>Runs complete normal microsteps without changing Game State turn or action progress.</summary>
    public bool RunSimulationForDuration(float simulatedDuration)
    {
        if (!EnsureInitialized() || !IsFinitePositive(simulatedDuration))
            return false;

        float remaining = simulatedDuration;
        int safetyLimit = 100000;
        while (remaining > 0.0001f && safetyLimit-- > 0)
        {
            Dev_WaterModifierSnapshot modifiers;
            if (!TryResolveModifiers(out modifiers))
                return false;

            float deltaTime = Mathf.Min(remaining, _engine.GetSimulationStepDuration(modifiers));
            StepSimulation(deltaTime, modifiers);
            remaining -= deltaTime;
        }

        if (safetyLimit <= 0)
        {
            Debug.LogError("[Dev_WaterController] Preliminary flooding exceeded the simulation safety limit.");
            return false;
        }

        return true;
    }

    public bool StepSimulation()
    {
        if (!EnsureInitialized())
            return false;

        if (!TryResolveModifiers(out Dev_WaterModifierSnapshot modifiers))
            return false;

        return StepSimulation(_engine.GetSimulationStepDuration(modifiers), modifiers);
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
        if (_runtimeState == null || !IsFiniteNonNegative(depth))
            return false;

        float clampedDepth = _resolvedSettings != null && _resolvedSettings.maxWaterDepth > 0f
            ? Mathf.Min(depth, _resolvedSettings.maxWaterDepth)
            : depth;

        if (!_runtimeState.TrySetWaterDepth(tileCell, clampedDepth))
            return false;

        waterRenderer?.ApplyDirty();
        return true;
    }

    public bool TrySetTerrainElevation(Vector2Int tileCell, float elevation)
    {
        if (_runtimeState == null || !IsFiniteNonNegative(elevation) ||
            !_runtimeState.TryTileToSim(tileCell, out int simX, out int simY) ||
            !_runtimeState.HasMapCellAtSim(simX, simY))
            return false;

        _runtimeState.Terrain[simX, simY] = elevation;
        _runtimeState.MarkDirtyBySim(simX, simY);
        waterRenderer?.ApplyDirty();
        return true;
    }

    public bool TrySetBarrierX(Vector2Int rightTileCell, float height, float seepage = 0f)
    {
        return _runtimeState != null && _runtimeState.TryTileToSim(rightTileCell, out int simX, out int simY) &&
               _barrierGrid.TrySetBarrierX(simX, simY, height, seepage);
    }

    public bool TrySetBarrierY(Vector2Int upperTileCell, float height, float seepage = 0f)
    {
        return _runtimeState != null && _runtimeState.TryTileToSim(upperTileCell, out int simX, out int simY) &&
               _barrierGrid.TrySetBarrierY(simX, simY, height, seepage);
    }

    /// <summary>Projects a cloned current state with the active profile only; live state is never mutated.</summary>
    public bool TryBuildProjection(float simulatedDuration, out Dev_WaterProjection projection)
    {
        projection = null;
        if (!EnsureInitialized() || !IsFiniteNonNegative(simulatedDuration) ||
            !TryResolveModifiers(out Dev_WaterModifierSnapshot modifiers))
            return false;

        Dev_WaterRuntimeState projectionState = _runtimeState.Clone();
        Dev_WaterSimulationEngine projectionEngine = new Dev_WaterSimulationEngine(_mapAccessor, _barrierGrid.Clone());
        projectionEngine.InitializeProjection(projectionState, _resolvedSettings);

        float remaining = simulatedDuration;
        int safetyLimit = 100000;
        while (remaining > 0.0001f && safetyLimit-- > 0)
        {
            float deltaTime = Mathf.Min(remaining, projectionEngine.GetSimulationStepDuration(modifiers));
            projectionEngine.Step(_resolvedContinuousSources, modifiers, deltaTime);
            remaining -= deltaTime;
        }

        if (safetyLimit <= 0)
            return false;

        projection = new Dev_WaterProjection(
            projectionState.Origin,
            projectionState.Width,
            projectionState.Height,
            _activeProfileStage,
            simulatedDuration,
            projectionState.CopyLogicalWaterDepths());
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

        string scenarioError = scenarioConfig == null ? "scenario config is missing" : null;
        if (configurationMode == Dev_WaterConfigurationMode.Production &&
            (scenarioConfig == null || !scenarioConfig.IsValidForProduction(out scenarioError)))
        {
            Debug.LogError($"[Dev_WaterController] Production configuration rejected scenario data: {scenarioError ?? "scenario config is missing"}");
            _initialized = false;
            return false;
        }

        if (!TryResolveProfile(Dev_WaterProfileStage.Baseline, out _resolvedSettings, out _resolvedContinuousSources))
        {
            _initialized = false;
            return false;
        }

        if (!TryResolveModifiers(out Dev_WaterModifierSnapshot modifiers))
        {
            _initialized = false;
            return false;
        }

        _resolvedInitialSources = scenarioConfig != null
            ? scenarioConfig.CreateInitialSourceInstances()
            : Array.Empty<Dev_WaterSourceSpec>();
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
        _engine.ApplyInitialSources(_resolvedInitialSources, modifiers);
        _engine.InitializeActiveRegion();

        if (waterRenderer != null)
            waterRenderer.Initialize(_runtimeState, _mapAccessor);

        _activeProfileStage = Dev_WaterProfileStage.Baseline;
        _initialized = true;
        _lastSummary = default;
        OnWaterInitialized?.Invoke(this);
        return true;
    }

    private bool EnsureInitialized()
    {
        return _initialized && _runtimeState != null && _engine != null || InitializeRuntimeState();
    }

    private bool TryApplyProfile(Dev_WaterProfileStage stage)
    {
        if (!_initialized && !EnsureInitialized())
            return false;

        if (!TryResolveProfile(stage, out Dev_WaterSimulationSettings settings, out Dev_WaterSourceSpec[] sources))
            return false;

        if (!TryResolveModifiers(out _))
            return false;

        _resolvedSettings = settings;
        _resolvedContinuousSources = sources;
        _engine.Reconfigure(_resolvedSettings);
        _activeProfileStage = stage;
        OnWaterProfileChanged?.Invoke(stage);
        return true;
    }

    private bool TryResolveProfile(
        Dev_WaterProfileStage stage,
        out Dev_WaterSimulationSettings settings,
        out Dev_WaterSourceSpec[] sources)
    {
        settings = null;
        sources = Array.Empty<Dev_WaterSourceSpec>();
        string error = scenarioConfig == null ? "scenario config is missing" : null;
        if (scenarioConfig != null && scenarioConfig.TryCreateProfile(stage, out settings, out sources, out error))
            return true;

        if (configurationMode == Dev_WaterConfigurationMode.Production)
        {
            Debug.LogError($"[Dev_WaterController] Production configuration rejected {stage} profile: {error ?? "scenario config is missing"}");
            return false;
        }

        Debug.LogWarning($"[Dev_WaterController] Dev defaults are active because the {stage} profile is unavailable: {error ?? "scenario config is missing"}");
        settings = new Dev_WaterSimulationSettings();
        settings.Sanitize();
        return true;
    }

    private bool TryResolveModifiers(out Dev_WaterModifierSnapshot modifiers)
    {
        IDev_WaterModifierProvider provider = modifierProviderBehaviour as IDev_WaterModifierProvider;
        string error = provider == null ? "provider is missing" : null;
        if (provider != null && provider.TryGetResolvedWaterModifiers(out modifiers, out error))
        {
            modifiers.Sanitize();
            return true;
        }

        if (configurationMode == Dev_WaterConfigurationMode.Production)
        {
            Debug.LogError($"[Dev_WaterController] Production simulation requires a resolved modifier provider: {error ?? "provider is missing or invalid"}");
            modifiers = default;
            return false;
        }

        Debug.LogWarning($"[Dev_WaterController] Dev modifier defaults are active: {error ?? "provider is missing"}");
        modifiers = Dev_WaterModifierSnapshot.Defaults();
        return true;
    }

    private bool StepSimulation(float simulatedDeltaTime, Dev_WaterModifierSnapshot modifiers)
    {
        _lastSummary = _engine.Step(_resolvedContinuousSources, modifiers, simulatedDeltaTime);
        waterRenderer?.ApplyDirty();
        OnWaterSimulationStepped?.Invoke(_lastSummary);
        return true;
    }

    private void RefreshLifecycleStepping()
    {
        bool shouldRun = _gameFlow == Dev_WaterGameFlow.Gameplay && _gamePhase == Dev_WaterGamePhase.Crisis;
        if (shouldRun)
        {
            if (!_simulationRunning)
                BeginSimulation();
        }
        else
        {
            PauseSimulation();
        }
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
