using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Public interface for live Dev water. It owns runtime state, profile transitions,
/// simulated-time stepping, modifier resolution, and immutable projection inputs.
/// </summary>
public class WaterController : MonoBehaviour
{
    [Header("Map Definition")]
    [FormerlySerializedAs("mapData")]
    [SerializeField] private MapDef mapDef;

    [Header("Scenario Definition")]
    [FormerlySerializedAs("scenarioConfig")]
    [SerializeField] private ScenarioDef scenarioDef;
    [SerializeField] private WaterConfigurationMode configurationMode = WaterConfigurationMode.DevDefaultsWithWarnings;

    [Header("Modifier Contract")]
    [Tooltip("Must implement IWaterModifierProvider. Production refuses defaults.")]
    [SerializeField] private MonoBehaviour modifierProviderBehaviour;

    [Header("Rendering")]
    [SerializeField] private WaterRenderer waterRenderer;

    [Header("Lifecycle")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool startOnPlay;

    [Header("Simulation Stepping")]
    [SerializeField] private WaterStepMode stepMode = WaterStepMode.Automatic;
    [Min(0.05f)] [SerializeField] private float autoStepInterval = 0.5f;
    [Tooltip("Dev-only test input until the project input system exists.")]
    [SerializeField] private bool spaceKeyStepsWhenManual = true;

    private MapAccessor _mapAccessor;
    private WaterState _runtimeState;
    private WaterPhysics _engine;
    private WaterPhysicsBarrier _barrierGrid;
    private WaterSimulationSettings _resolvedSettings;
    private WaterSourceSpec[] _resolvedInitialSources = Array.Empty<WaterSourceSpec>();
    private WaterSourceSpec[] _resolvedContinuousSources = Array.Empty<WaterSourceSpec>();
    private WaterProfileStage _activeProfileStage;
    private WaterGameFlow _gameFlow = WaterGameFlow.Loading;
    private WaterGamePhase _gamePhase = WaterGamePhase.Preparation;
    private float _autoStepTimer;
    private bool _initialized;
    private bool _simulationRunning;
    private WaterStepSummary _lastSummary;

    public event Action<WaterController> OnWaterInitialized;
    public event Action<WaterController> OnWaterSimulationStarted;
    public event Action<WaterController> OnWaterSimulationPaused;
    public event Action<WaterController> OnWaterSimulationReset;
    public event Action<WaterProfileStage> OnWaterProfileChanged;
    public event Action<WaterStepSummary> OnWaterSimulationStepped;

    public bool IsInitialized => _initialized;
    public bool IsSimulationRunning => _simulationRunning;
    public WaterProfileStage ActiveProfileStage => _activeProfileStage;
    public WaterGameFlow GameFlow => _gameFlow;
    public WaterGamePhase GamePhase => _gamePhase;

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

        if (stepMode == WaterStepMode.Manual)
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
        return mapDef != null && (scenarioDef != null || configurationMode == WaterConfigurationMode.DevDefaultsWithWarnings);
    }

    /// <summary>Dev/manual entry point. Production callers should drive SetGameFlow and SetGamePhase.</summary>
    public bool BeginSimulation()
    {
        if (_simulationRunning)
            return false;

        if (configurationMode == WaterConfigurationMode.Production &&
            (_gameFlow != WaterGameFlow.Gameplay || _gamePhase != WaterGamePhase.Crisis))
        {
            Debug.LogError("[WaterController] Production automatic stepping requires Gameplay and Crisis lifecycle states.");
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
    public void SetGameFlow(WaterGameFlow gameFlow)
    {
        _gameFlow = gameFlow;
        RefreshLifecycleStepping();
    }

    /// <summary>Receives Game Phase changes; it does not create or own the Game Phase state machine.</summary>
    public bool SetGamePhase(WaterGamePhase gamePhase)
    {
        if (gamePhase == WaterGamePhase.Crisis && _activeProfileStage != WaterProfileStage.Crisis)
        {
            if (!TryApplyProfile(WaterProfileStage.Crisis))
                return false;
        }

        _gamePhase = gamePhase;
        RefreshLifecycleStepping();
        return true;
    }

    /// <summary>Runs normal physics for an explicit amount of simulated time using the preliminary profile.</summary>
    public bool RunPreliminaryFlooding(float simulatedDuration)
    {
        if (!IsFinitePositive(simulatedDuration))
        {
            Debug.LogError("[WaterController] Preliminary flooding requires a finite positive simulated duration.");
            return false;
        }

        if (!EnsureInitialized() ||
            !TryResolveProfile(WaterProfileStage.Preliminary, out WaterSimulationSettings settings,
                out WaterSourceSpec[] sources))
            return false;

        WaterState projectionState = _runtimeState.Clone();
        WaterPhysicsBarrier projectionBarrier = _barrierGrid.Clone();
        WaterPhysics projectionEngine = _engine.CloneForState(projectionState, projectionBarrier);
        projectionEngine.Reconfigure(settings);

        List<WaterStepSummary> summaries = new List<WaterStepSummary>();
        if (!RunSimulationForDuration(projectionEngine, sources, simulatedDuration, summaries))
            return false;

        _runtimeState = projectionState;
        _barrierGrid = projectionBarrier;
        _engine = projectionEngine;
        _resolvedSettings = settings;
        _resolvedContinuousSources = sources;
        _activeProfileStage = WaterProfileStage.Preliminary;

        waterRenderer?.Initialize(_runtimeState, _mapAccessor);
        OnWaterProfileChanged?.Invoke(_activeProfileStage);
        foreach (WaterStepSummary summary in summaries)
        {
            _lastSummary = summary;
            OnWaterSimulationStepped?.Invoke(summary);
        }

        return true;
    }

    public bool TryGetPreliminaryFlooding(out WaterPreliminaryFloodingConfig configuration)
    {
        configuration = null;
        return scenarioDef != null && scenarioDef.TryGetPreliminaryFlooding(out configuration, out _);
    }

    /// <summary>Runs complete normal microsteps without changing Game State turn or action progress.</summary>
    public bool RunSimulationForDuration(float simulatedDuration)
    {
        if (!EnsureInitialized() || !IsFinitePositive(simulatedDuration))
            return false;

        return RunSimulationForDuration(_engine, _resolvedContinuousSources, simulatedDuration, null);
    }

    public bool StepSimulation()
    {
        if (!EnsureInitialized())
            return false;

        if (!TryResolveModifiers(out WaterModifierSnapshot modifiers))
            return false;

        return StepSimulation(_engine.GetSimulationStepDuration(modifiers), modifiers);
    }

    public WaterStepSummary GetLastStepSummary()
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
    public bool TryBuildProjection(float simulatedDuration, out WaterProjection projection)
    {
        projection = null;
        if (!_initialized || _runtimeState == null || _engine == null || !IsFiniteNonNegative(simulatedDuration) ||
            !TryResolveModifiers(out WaterModifierSnapshot modifiers))
            return false;

        WaterState projectionState = _runtimeState.Clone();
        WaterPhysics projectionEngine = _engine.CloneForState(projectionState, _barrierGrid.Clone());

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

        projection = new WaterProjection(
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
        if (mapDef == null)
        {
            Debug.LogError("[WaterController] Cannot initialize: MapDef is not assigned.");
            _initialized = false;
            return false;
        }

        if (configurationMode == WaterConfigurationMode.Production &&
            !mapDef.IsValidForProduction(out string mapError))
        {
            Debug.LogError($"[WaterController] Production configuration rejected map data: {mapError}");
            _initialized = false;
            return false;
        }

        string scenarioError = scenarioDef == null ? "scenario definition is missing" : null;
        if (configurationMode == WaterConfigurationMode.Production &&
            (scenarioDef == null || !scenarioDef.IsValidForProduction(out scenarioError)))
        {
            Debug.LogError($"[WaterController] Production configuration rejected scenario data: {scenarioError ?? "scenario definition is missing"}");
            _initialized = false;
            return false;
        }

        if (!TryResolveProfile(WaterProfileStage.Baseline, out _resolvedSettings, out _resolvedContinuousSources))
        {
            _initialized = false;
            return false;
        }

        if (!TryResolveModifiers(out WaterModifierSnapshot modifiers))
        {
            _initialized = false;
            return false;
        }

        _resolvedInitialSources = scenarioDef != null
            ? scenarioDef.CreateInitialSourceInstances()
            : Array.Empty<WaterSourceSpec>();
        _mapAccessor = new MapAccessor(mapDef);
        _runtimeState = new WaterState(_mapAccessor);
        _barrierGrid = new WaterPhysicsBarrier();

        if (!_barrierGrid.InitializeForSimulation(_runtimeState.GridWidth, _runtimeState.GridHeight))
        {
            _initialized = false;
            return false;
        }

        _engine = new WaterPhysics(_mapAccessor, _barrierGrid);
        _engine.Initialize(_runtimeState, _resolvedSettings);
        _engine.ApplyInitialSources(_resolvedInitialSources, modifiers);
        _engine.InitializeActiveRegion();

        if (waterRenderer != null)
            waterRenderer.Initialize(_runtimeState, _mapAccessor);

        _activeProfileStage = WaterProfileStage.Baseline;
        _initialized = true;
        _lastSummary = default;
        OnWaterInitialized?.Invoke(this);
        return true;
    }

    private bool EnsureInitialized()
    {
        return _initialized && _runtimeState != null && _engine != null || InitializeRuntimeState();
    }

    private bool TryApplyProfile(WaterProfileStage stage)
    {
        if (!_initialized && !EnsureInitialized())
            return false;

        if (!TryResolveProfile(stage, out WaterSimulationSettings settings, out WaterSourceSpec[] sources))
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
        WaterProfileStage stage,
        out WaterSimulationSettings settings,
        out WaterSourceSpec[] sources)
    {
        settings = null;
        sources = Array.Empty<WaterSourceSpec>();
        string error = scenarioDef == null ? "scenario definition is missing" : null;
        if (scenarioDef != null && scenarioDef.TryCreateProfile(stage, out settings, out sources, out error))
            return true;

        if (configurationMode == WaterConfigurationMode.Production)
        {
            Debug.LogError($"[WaterController] Production configuration rejected {stage} profile: {error ?? "scenario definition is missing"}");
            return false;
        }

        Debug.LogWarning($"[WaterController] Dev defaults are active because the {stage} profile is unavailable: {error ?? "scenario definition is missing"}");
        settings = new WaterSimulationSettings();
        settings.Sanitize();
        return true;
    }

    private bool TryResolveModifiers(out WaterModifierSnapshot modifiers)
    {
        IWaterModifierProvider provider = modifierProviderBehaviour as IWaterModifierProvider;
        string error = provider == null ? "provider is missing" : null;
        if (provider != null && provider.TryGetResolvedWaterModifiers(out modifiers, out error))
        {
            if (!modifiers.IsValid(out error))
            {
                if (configurationMode == WaterConfigurationMode.Production)
                {
                    Debug.LogError($"[WaterController] Production modifier provider returned invalid values: {error}");
                    modifiers = default;
                    return false;
                }

                Debug.LogWarning($"[WaterController] Dev modifier defaults are active: {error}");
                modifiers = WaterModifierSnapshot.Defaults();
                return true;
            }

            modifiers.Sanitize();
            return true;
        }

        if (configurationMode == WaterConfigurationMode.Production)
        {
            Debug.LogError($"[WaterController] Production simulation requires a resolved modifier provider: {error ?? "provider is missing or invalid"}");
            modifiers = default;
            return false;
        }

        Debug.LogWarning($"[WaterController] Dev modifier defaults are active: {error ?? "provider is missing"}");
        modifiers = WaterModifierSnapshot.Defaults();
        return true;
    }

    private bool StepSimulation(float simulatedDeltaTime, WaterModifierSnapshot modifiers)
    {
        _lastSummary = _engine.Step(_resolvedContinuousSources, modifiers, simulatedDeltaTime);
        waterRenderer?.ApplyDirty();
        OnWaterSimulationStepped?.Invoke(_lastSummary);
        return true;
    }

    private bool RunSimulationForDuration(
        WaterPhysics engine,
        WaterSourceSpec[] continuousSources,
        float simulatedDuration,
        List<WaterStepSummary> summaries)
    {
        float remaining = simulatedDuration;
        int safetyLimit = 100000;
        while (remaining > 0.0001f && safetyLimit-- > 0)
        {
            if (!TryResolveModifiers(out WaterModifierSnapshot modifiers))
                return false;

            float deltaTime = Mathf.Min(remaining, engine.GetSimulationStepDuration(modifiers));
            WaterStepSummary summary = engine.Step(continuousSources, modifiers, deltaTime);
            remaining -= deltaTime;

            if (summaries != null)
            {
                summaries.Add(summary);
            }
            else
            {
                _lastSummary = summary;
                waterRenderer?.ApplyDirty();
                OnWaterSimulationStepped?.Invoke(summary);
            }
        }

        if (safetyLimit <= 0)
        {
            Debug.LogError("[WaterController] Simulation duration exceeded the simulation safety limit.");
            return false;
        }

        return true;
    }

    private void RefreshLifecycleStepping()
    {
        bool shouldRun = _gameFlow == WaterGameFlow.Gameplay && _gamePhase == WaterGamePhase.Crisis;
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
