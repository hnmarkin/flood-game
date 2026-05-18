using System;
using UnityEngine;

public class Dev_WaterController : MonoBehaviour
{
    [Header("Compatibility Inputs")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private TileType waterTileType;

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

    [Header("External Providers")]
    [SerializeField] private MonoBehaviour barrierProviderBehaviour;
    [SerializeField] private MonoBehaviour modifierProviderBehaviour;

    [Header("Lifecycle")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool startOnPlay;

    [Header("Simulation Stepping")]
    [SerializeField] private Dev_WaterStepMode stepMode = Dev_WaterStepMode.Automatic;
    [Min(0.05f)]
    [SerializeField] private float autoStepInterval = 0.5f;
    [SerializeField] private bool spaceKeyStepsWhenManual = true;

    private Dev_WaterRuntimeState _runtimeState;
    private Dev_WaterSimulationEngine _engine;
    private Dev_IWaterBarrierProvider _barrierProvider;
    private Dev_IWaterModifierProvider _modifierProvider;
    private Dev_WaterSimulationSettings _resolvedSettings;
    private Dev_WaterSourceSpec[] _resolvedInitialSources = Array.Empty<Dev_WaterSourceSpec>();
    private Dev_WaterSourceSpec[] _resolvedContinuousSources = Array.Empty<Dev_WaterSourceSpec>();
    private TileType _resolvedWaterTileType;
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
    public Dev_WaterRuntimeState RuntimeState => _runtimeState;

    private void Awake()
    {
        BindProviders();
    }

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
        return tileMapData != null;
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

        Dev_WaterModifierSnapshot modifiers = GetModifierSnapshot();
        _lastSummary = _engine.Step(_resolvedContinuousSources, _resolvedWaterTileType, modifiers);
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
            return false;

        if (!_runtimeState.TrySetWaterDepth(tileCell, depth))
            return false;

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

        BindProviders();
        ResolveConfiguration();

        _runtimeState = Dev_WaterTileMapDataAdapter.CreateRuntimeState(tileMapData, _resolvedWaterTileType);
        if (_runtimeState == null)
        {
            _initialized = false;
            return false;
        }

        _engine = new Dev_WaterSimulationEngine(_barrierProvider);
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

        Dev_WaterModifierSnapshot modifiers = GetModifierSnapshot();
        _engine.ApplyInitialSources(_resolvedInitialSources, _resolvedWaterTileType, modifiers);
        _engine.InitializeActiveRegion();
        waterRenderer?.ApplyDirty();
        _sourcesApplied = true;
    }

    private void BindProviders()
    {
        var barrierAdapter = new Dev_WaterBarrierProviderAdapter(barrierProviderBehaviour);
        _barrierProvider = barrierAdapter.HasProvider ? barrierAdapter : null;
        _modifierProvider = new Dev_WaterModifierProviderAdapter(modifierProviderBehaviour);
    }

    private void ResolveConfiguration()
    {
        if (scenarioConfig != null)
        {
            _resolvedSettings = scenarioConfig.CreateSettingsInstance();
            _resolvedInitialSources = scenarioConfig.CreateInitialSourceInstances();
            _resolvedContinuousSources = scenarioConfig.CreateContinuousSourceInstances();
            _resolvedWaterTileType = scenarioConfig.WaterTileType != null ? scenarioConfig.WaterTileType : waterTileType;
            return;
        }

        _resolvedSettings = simulationSettings != null
            ? simulationSettings.Clone()
            : new Dev_WaterSimulationSettings();
        _resolvedSettings.Sanitize();
        _resolvedInitialSources = CloneSources(initialSources);
        _resolvedContinuousSources = CloneSources(continuousSources);
        _resolvedWaterTileType = waterTileType;
    }

    private Dev_WaterModifierSnapshot GetModifierSnapshot()
    {
        return _modifierProvider != null
            ? _modifierProvider.GetWaterModifierSnapshot()
            : Dev_WaterModifierSnapshot.Defaults();
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
