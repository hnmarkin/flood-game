using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class ResourceBarRuntimeController : MonoBehaviour
{
    private const string DefaultTurnValueName = "TurnValue";
    private const string DefaultPopulationLabelName = "pop_label";
    private const string DefaultStormValueName = "StormValue";
    private const string DefaultStormGaugeName = "StormGauge";

    public static ResourceBarRuntimeController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UIDocument resourceBarDocument;
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private WaterSimulator waterSimulator;
    [SerializeField] private ShelterController shelterController;
    [SerializeField] private CommunicationController communicationTowerController;
    [SerializeField] private EvacuationController evacuationController;

    [Header("UI Element Names")]
    [SerializeField] private string turnValueName = DefaultTurnValueName;
    [SerializeField] private string populationLabelName = DefaultPopulationLabelName;
    [SerializeField] private string stormValueName = DefaultStormValueName;
    [SerializeField] private string stormGaugeName = DefaultStormGaugeName;

    [Header("Turns")]
    [SerializeField, Min(1)] private int maxTurns = 8;
    [SerializeField, Min(1)] private int currentTurn = 1;
    [SerializeField, Min(0f)] private float mitigationActionCooldownSeconds = 0.1f;
    [SerializeField] private bool advanceTurnOnShelterPlaced = true;
    [SerializeField] private bool advanceTurnOnCommunicationTowerActivated = true;
    [SerializeField] private bool advanceTurnOnWarningSent = true;
    [SerializeField] private bool advanceTurnOnEvacuationRouteSelected = true;

    [Header("Population")]
    [SerializeField] private int totalPopulation = 2000;
    [SerializeField] private bool showTotalPopulationWhenNoZoneHovered = true;
    [SerializeField] private bool autoCalculateTotalPopulationFromTileMapData = true;

    [Header("Storm")]
    [SerializeField, Range(0f, 100f)] private float startingStormPercent = 70f;
    [SerializeField] private bool stormCanOnlyIncrease = true;
    [SerializeField, Min(1)] private int stormGaugeSegmentCount = 7;
    [SerializeField] private bool autoUpdateStormFromFlooding = true;
    [SerializeField, Min(0.05f)] private float stormUpdateInterval = 0.25f;
    [SerializeField, Min(0.01f)] private float maxWaterHeightForFullStorm = 1.0f;
    [SerializeField, Min(0f)] private float floodedTileWaterThreshold = 0.01f;

    [Header("Events")]
    [SerializeField] private UnityEvent<int> onTurnChanged;
    [SerializeField] private UnityEvent<float> onStormChanged;

    [Header("Debug")]
    [SerializeField] private bool autoFindMissingReferences = true;
    [SerializeField] private bool debugLogs = true;

    private readonly List<VisualElement> _stormGaugeSegments = new();
    private readonly HashSet<string> _warningKeys = new();

    private Label _turnValueLabel;
    private Label _populationLabel;
    private Label _stormValueLabel;
    private VisualElement _stormGauge;
    private ProgressBar _stormProgressBar;

    private Coroutine _stormUpdateRoutine;
    private bool _hasHoveredZonePopulation;
    private int _hoveredZonePopulation;
    private float _displayedStormPercent;
    private float _lastMitigationActionRealtime = -999f;
    private string _lastMitigationActionName;
    private ShelterController _subscribedShelterController;
    private bool _shelterControllerLookupAttempted;
    private bool _subscribedToCommunication;
    private bool _subscribedToEvacuation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveResourceBar()
    {
        if (FindExistingRuntimeController() != null)
            return;

        UIDocument document = FindResourceBarDocumentByNames(
            DefaultTurnValueName,
            DefaultPopulationLabelName,
            DefaultStormValueName,
            DefaultStormGaugeName);

        if (document == null)
            return;

        document.gameObject.AddComponent<ResourceBarRuntimeController>();
        Debug.Log("[ResourceBarRuntimeController] Added runtime controller to ResourceBar UIDocument.");
    }

    private void Reset()
    {
        resourceBarDocument = GetComponent<UIDocument>();
    }

    private void Awake()
    {
        ResolveReferences();
        _displayedStormPercent = Mathf.Clamp(startingStormPercent, 0f, 100f);
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this && debugLogs)
            Debug.LogWarning("[ResourceBarRuntimeController] Multiple runtime resource bar controllers are enabled. The newest one will receive static hover updates.");

        Instance = this;

        ResolveReferences();
        BindUI();

        if (autoCalculateTotalPopulationFromTileMapData)
            RefreshTotalPopulationFromTileMapData();

        SetTurn(currentTurn);
        SetStormPercent(startingStormPercent, forceRefresh: true);
        UpdatePopulationLabel();
        SubscribeToGameplayEvents();
        StartStormUpdates();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameplayEvents();

        if (_stormUpdateRoutine != null)
        {
            StopCoroutine(_stormUpdateRoutine);
            _stormUpdateRoutine = null;
        }

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        // Reconcile assigned/destroyed instances without repeating scene discovery.
        RefreshShelterSubscription();
    }

    private void OnValidate()
    {
        maxTurns = Mathf.Max(1, maxTurns);
        currentTurn = Mathf.Clamp(currentTurn, 1, maxTurns);
        totalPopulation = Mathf.Max(0, totalPopulation);
        startingStormPercent = Mathf.Clamp(startingStormPercent, 0f, 100f);
        stormGaugeSegmentCount = Mathf.Max(1, stormGaugeSegmentCount);
        stormUpdateInterval = Mathf.Max(0.05f, stormUpdateInterval);
        maxWaterHeightForFullStorm = Mathf.Max(0.01f, maxWaterHeightForFullStorm);
        floodedTileWaterThreshold = Mathf.Max(0f, floodedTileWaterThreshold);
        mitigationActionCooldownSeconds = Mathf.Max(0f, mitigationActionCooldownSeconds);
    }

    public void RefreshNow()
    {
        ResolveReferences();
        RefreshShelterSubscription();
        BindUI();

        if (autoCalculateTotalPopulationFromTileMapData)
            RefreshTotalPopulationFromTileMapData();

        SetTurn(currentTurn);
        SetStormPercent(CalculateStormPercentFromFlooding(), forceRefresh: true);
        UpdatePopulationLabel();
    }

    public void SetTurn(int turn)
    {
        int clampedTurn = Mathf.Clamp(turn, 1, maxTurns);
        bool changed = currentTurn != clampedTurn;

        currentTurn = clampedTurn;
        UpdateTurnLabel();

        if (changed)
            onTurnChanged?.Invoke(currentTurn);
    }

    public void AdvanceTurn()
    {
        SetTurn(Mathf.Min(maxTurns, currentTurn + 1));
    }

    public void RegisterMitigationAction(string actionName)
    {
        string safeActionName = string.IsNullOrWhiteSpace(actionName)
            ? "MitigationAction"
            : actionName.Trim();

        float now = Time.unscaledTime;

        if (mitigationActionCooldownSeconds > 0f &&
            now - _lastMitigationActionRealtime < mitigationActionCooldownSeconds)
        {
            if (debugLogs)
            {
                Debug.Log(
                    $"[ResourceBarRuntimeController] Ignored duplicate mitigation action '{safeActionName}' shortly after '{_lastMitigationActionName}'.");
            }

            return;
        }

        _lastMitigationActionRealtime = now;
        _lastMitigationActionName = safeActionName;

        int previousTurn = currentTurn;
        AdvanceTurn();

        if (debugLogs)
            Debug.Log($"[ResourceBarRuntimeController] Registered mitigation action '{safeActionName}'. Turn {previousTurn} -> {currentTurn}.");
    }

    public void SetHoveredZonePopulation(int population)
    {
        _hasHoveredZonePopulation = true;
        _hoveredZonePopulation = Mathf.Max(0, population);
        UpdatePopulationLabel();
    }

    public void SetHoveredZoneInfo(string zoneId, int population)
    {
        SetHoveredZonePopulation(population);
    }

    public void ClearHoveredZonePopulation()
    {
        _hasHoveredZonePopulation = false;
        _hoveredZonePopulation = 0;
        UpdatePopulationLabel();
    }

    public void SetTotalPopulation(int population)
    {
        totalPopulation = Mathf.Max(0, population);

        if (!_hasHoveredZonePopulation)
            UpdatePopulationLabel();
    }

    public void SetStormPercent(float stormPercent)
    {
        SetStormPercent(stormPercent, forceRefresh: false);
    }

    private void SetStormPercent(float stormPercent, bool forceRefresh)
    {
        float clampedStormPercent = Mathf.Clamp(stormPercent, 0f, 100f);

        if (stormCanOnlyIncrease)
            clampedStormPercent = Mathf.Max(_displayedStormPercent, clampedStormPercent);

        bool changed = !Mathf.Approximately(_displayedStormPercent, clampedStormPercent);
        _displayedStormPercent = clampedStormPercent;
        UpdateStormUI();

        if (changed || forceRefresh)
            onStormChanged?.Invoke(_displayedStormPercent);
    }

    private void ResolveReferences()
    {
        if (!autoFindMissingReferences)
            return;

        if (resourceBarDocument == null)
            resourceBarDocument = GetComponent<UIDocument>();

        if (resourceBarDocument == null)
            resourceBarDocument = FindResourceBarDocument();

        if (waterSimulator == null)
            waterSimulator = FindFirstObjectByType<WaterSimulator>();

        ResolveShelterController();

        if (communicationTowerController == null)
            communicationTowerController = FindFirstObjectByType<CommunicationController>();

        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (tileMapData == null)
            tileMapData = FindTileMapDataAsset();
    }

    private void ResolveShelterController()
    {
        if (shelterController != null || _shelterControllerLookupAttempted)
            return;

        _shelterControllerLookupAttempted = true;

        ShelterController localController = GetComponent<ShelterController>();
        if (localController != null && localController.isActiveAndEnabled)
        {
            shelterController = localController;
            return;
        }

        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            return;

        ShelterController match = null;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (ShelterController candidate in root.GetComponentsInChildren<ShelterController>())
            {
                if (!candidate.isActiveAndEnabled)
                    continue;

                if (match != null)
                {
                    WarnOnce("shelter-controller", "[ResourceBarRuntimeController] Multiple active ShelterControllers exist in this scene. Assign shelterController explicitly; shelter placement turns are disconnected.");
                    return;
                }

                match = candidate;
            }
        }

        shelterController = match;
    }

    private void BindUI()
    {
        if (resourceBarDocument == null)
        {
            WarnOnce("resource-document", "[ResourceBarRuntimeController] ResourceBar UIDocument is not assigned.");
            return;
        }

        VisualElement root = resourceBarDocument.rootVisualElement;

        if (root == null)
        {
            WarnOnce("resource-root", "[ResourceBarRuntimeController] ResourceBar UIDocument rootVisualElement is null.");
            return;
        }

        _turnValueLabel = root.Q<Label>(turnValueName);
        _populationLabel = root.Q<Label>(populationLabelName);
        _stormValueLabel = root.Q<Label>(stormValueName);
        _stormGauge = root.Q<VisualElement>(stormGaugeName);
        _stormProgressBar = root.Q<ProgressBar>(stormGaugeName);

        CacheStormGaugeSegments();
        LogMissingElement(_turnValueLabel, turnValueName);
        LogMissingElement(_populationLabel, populationLabelName);
        LogMissingElement(_stormValueLabel, stormValueName);
        LogMissingElement(_stormGauge, stormGaugeName);
    }

    private void CacheStormGaugeSegments()
    {
        _stormGaugeSegments.Clear();

        if (_stormGauge == null)
            return;

        foreach (VisualElement child in _stormGauge.Children())
        {
            if (child == null)
                continue;

            if (child.ClassListContains("gauge-segment") || _stormGauge.childCount <= stormGaugeSegmentCount)
                _stormGaugeSegments.Add(child);
        }
    }

    private void UpdateTurnLabel()
    {
        if (_turnValueLabel != null)
            _turnValueLabel.text = $"{currentTurn} / {maxTurns}";
    }

    private void UpdatePopulationLabel()
    {
        if (_populationLabel == null)
            return;

        if (_hasHoveredZonePopulation)
        {
            _populationLabel.text = $"Population: {_hoveredZonePopulation:N0}";
            return;
        }

        _populationLabel.text = showTotalPopulationWhenNoZoneHovered
            ? $"Population: {totalPopulation:N0}"
            : "Population: --";
    }

    private void UpdateStormUI()
    {
        int roundedPercent = Mathf.RoundToInt(_displayedStormPercent);

        if (_stormValueLabel != null)
            _stormValueLabel.text = $"{roundedPercent}%";

        if (_stormProgressBar != null)
        {
            _stormProgressBar.lowValue = 0f;
            _stormProgressBar.highValue = 100f;
            _stormProgressBar.value = _displayedStormPercent;
            _stormProgressBar.title = $"{roundedPercent}%";
        }

        int segmentCount = _stormGaugeSegments.Count > 0
            ? _stormGaugeSegments.Count
            : stormGaugeSegmentCount;

        int filledCount = Mathf.RoundToInt((_displayedStormPercent / 100f) * segmentCount);

        for (int i = 0; i < _stormGaugeSegments.Count; i++)
        {
            bool filled = i < filledCount;
            VisualElement segment = _stormGaugeSegments[i];

            segment.EnableInClassList("gauge-segment--filled", filled);
            segment.EnableInClassList("gauge-segment--empty", !filled);
        }
    }

    private void StartStormUpdates()
    {
        if (!autoUpdateStormFromFlooding)
            return;

        if (_stormUpdateRoutine != null)
            StopCoroutine(_stormUpdateRoutine);

        _stormUpdateRoutine = StartCoroutine(StormUpdateRoutine());
    }

    private IEnumerator StormUpdateRoutine()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(stormUpdateInterval);

        while (enabled)
        {
            SetStormPercent(CalculateStormPercentFromFlooding());
            yield return wait;
        }
    }

    private float CalculateStormPercentFromFlooding()
    {
        if (!autoUpdateStormFromFlooding || tileMapData == null)
            return _displayedStormPercent;

        int totalTiles = 0;
        int floodedTiles = 0;
        float maxObservedWaterHeight = 0f;

        int width = ResolveTileMapWidth();
        int height = ResolveTileMapHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!TryGetTileInstance(x, y, out TileInstance tile) || tile == null)
                    continue;

                totalTiles++;

                float waterHeight = GetWaterHeightAt(x, y, tile);
                maxObservedWaterHeight = Mathf.Max(maxObservedWaterHeight, waterHeight);

                if (waterHeight > floodedTileWaterThreshold)
                    floodedTiles++;
            }
        }

        if (totalTiles == 0)
            return _displayedStormPercent;

        float floodedFraction = Mathf.Clamp01((float)floodedTiles / totalTiles);
        float maxWaterFraction = Mathf.Clamp01(maxObservedWaterHeight / maxWaterHeightForFullStorm);
        float floodSeverity = Mathf.Max(floodedFraction, maxWaterFraction);

        return Mathf.Clamp(startingStormPercent + floodSeverity * (100f - startingStormPercent), 0f, 100f);
    }

    private void RefreshTotalPopulationFromTileMapData()
    {
        if (tileMapData == null)
            return;

        Dictionary<string, int> populationByGeoid = new();
        int unzonedPopulation = 0;

        int width = ResolveTileMapWidth();
        int height = ResolveTileMapHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!TryGetTileInstance(x, y, out TileInstance tile) || tile == null || tile.population <= 0)
                    continue;

                string geoid = string.IsNullOrWhiteSpace(tile.geoid) ? null : tile.geoid.Trim();

                if (string.IsNullOrEmpty(geoid))
                {
                    unzonedPopulation += Mathf.Max(0, tile.population);
                    continue;
                }

                if (populationByGeoid.TryGetValue(geoid, out int currentPopulation))
                    populationByGeoid[geoid] = Mathf.Max(currentPopulation, tile.population);
                else
                    populationByGeoid.Add(geoid, tile.population);
            }
        }

        int calculatedTotalPopulation = unzonedPopulation;

        foreach (KeyValuePair<string, int> pair in populationByGeoid)
            calculatedTotalPopulation += Mathf.Max(0, pair.Value);

        if (calculatedTotalPopulation > 0)
            SetTotalPopulation(calculatedTotalPopulation);
    }

    private float GetWaterHeightAt(int x, int y, TileInstance tile)
    {
        float waterHeight = tile != null ? Mathf.Max(0f, tile.waterHeight) : 0f;

        if (tileMapData.water == null)
            return waterHeight;

        int simX = x + 1;
        int simY = y + 1;

        if (simX < 0 ||
            simY < 0 ||
            simX >= tileMapData.water.GetLength(0) ||
            simY >= tileMapData.water.GetLength(1))
        {
            return waterHeight;
        }

        return Mathf.Max(waterHeight, tileMapData.water[simX, simY]);
    }

    private bool TryGetTileInstance(int x, int y, out TileInstance tile)
    {
        tile = null;

        if (tileMapData == null || x < 0 || y < 0 || x >= tileMapData.sizeX || y >= tileMapData.sizeY)
            return false;

        tile = tileMapData.Get(new Vector2Int(x, y));
        return true;
    }

    private int ResolveTileMapWidth()
    {
        if (tileMapData == null)
            return 0;

        if (tileMapData.N > 0)
            return Mathf.Min(tileMapData.N, tileMapData.sizeX);

        if (tileMapData.rangeX.y > 0)
            return Mathf.Min(tileMapData.rangeX.y, tileMapData.sizeX);

        return Mathf.Max(0, tileMapData.sizeX);
    }

    private int ResolveTileMapHeight()
    {
        if (tileMapData == null)
            return 0;

        if (tileMapData.N > 0)
            return Mathf.Min(tileMapData.N, tileMapData.sizeY);

        if (tileMapData.rangeY.y > 0)
            return Mathf.Min(tileMapData.rangeY.y, tileMapData.sizeY);

        return Mathf.Max(0, tileMapData.sizeY);
    }

    private void SubscribeToGameplayEvents()
    {
        RefreshShelterSubscription();

        if (!_subscribedToCommunication && communicationTowerController != null)
        {
            communicationTowerController.OnTowerActivated += OnTowerActivated;
            communicationTowerController.OnZoneWarned += OnZoneWarned;
            _subscribedToCommunication = true;

            if (debugLogs)
                Debug.Log("[ResourceBarRuntimeController] Hooked communication tower events.");
        }

        if (!_subscribedToEvacuation && evacuationController != null)
        {
            evacuationController.OnEvacuationRouteSelected += OnEvacuationRouteSelected;
            _subscribedToEvacuation = true;

            if (debugLogs)
                Debug.Log("[ResourceBarRuntimeController] Hooked EvacuationController.OnEvacuationRouteSelected.");
        }
    }

    private void UnsubscribeFromGameplayEvents()
    {
        UnsubscribeFromShelterEvents();

        if (_subscribedToCommunication && communicationTowerController != null)
        {
            communicationTowerController.OnTowerActivated -= OnTowerActivated;
            communicationTowerController.OnZoneWarned -= OnZoneWarned;
        }

        if (_subscribedToEvacuation && evacuationController != null)
            evacuationController.OnEvacuationRouteSelected -= OnEvacuationRouteSelected;

        _subscribedToCommunication = false;
        _subscribedToEvacuation = false;
    }

    private void RefreshShelterSubscription()
    {
        ShelterController controller = isActiveAndEnabled && shelterController != null
            ? shelterController
            : null;

        if (controller == null && isActiveAndEnabled)
        {
            WarnOnce("shelter-controller", "[ResourceBarRuntimeController] No ShelterController is assigned or uniquely resolved in this scene. Assign shelterController explicitly; shelter placement turns are disconnected.");
        }

        if (ReferenceEquals(_subscribedShelterController, controller))
            return;

        UnsubscribeFromShelterEvents();

        if (controller == null)
            return;

        controller.OnShelterPlaced += OnShelterPlaced;
        _subscribedShelterController = controller;

        if (debugLogs)
            Debug.Log("[ResourceBarRuntimeController] Hooked ShelterController.OnShelterPlaced.");
    }

    private void UnsubscribeFromShelterEvents()
    {
        // C# event cleanup also applies to Unity objects whose native instance was destroyed.
        if (!ReferenceEquals(_subscribedShelterController, null))
            _subscribedShelterController.OnShelterPlaced -= OnShelterPlaced;

        _subscribedShelterController = null;
    }

    private void OnShelterPlaced(PlacedShelterData shelter)
    {
        if (advanceTurnOnShelterPlaced)
            RegisterMitigationAction("ShelterPlaced");
    }

    private void OnTowerActivated(CommunicationTowerData tower)
    {
        if (advanceTurnOnCommunicationTowerActivated)
            RegisterMitigationAction("CommunicationActivated");
    }

    private void OnZoneWarned(string geoid)
    {
        if (advanceTurnOnWarningSent)
            RegisterMitigationAction("CommunicationWarningSent");
    }

    private void OnEvacuationRouteSelected(SimpleEvacuationRoute route)
    {
        if (advanceTurnOnEvacuationRouteSelected)
            RegisterMitigationAction("EvacuationRouteSelected");
    }

    private UIDocument FindResourceBarDocument()
    {
        return FindResourceBarDocumentByNames(
            turnValueName,
            populationLabelName,
            stormValueName,
            stormGaugeName);
    }

    private static ResourceBarRuntimeController FindExistingRuntimeController()
    {
#if UNITY_2023_1_OR_NEWER
        ResourceBarRuntimeController[] controllers = Object.FindObjectsByType<ResourceBarRuntimeController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        ResourceBarRuntimeController[] controllers = Object.FindObjectsOfType<ResourceBarRuntimeController>(true);
#pragma warning restore CS0618
#endif

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
                return controllers[i];
        }

        return null;
    }

    private static UIDocument FindResourceBarDocumentByNames(
        string turnName,
        string populationName,
        string stormName,
        string gaugeName)
    {
#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        UIDocument[] documents = Object.FindObjectsOfType<UIDocument>(true);
#pragma warning restore CS0618
#endif

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];

            if (document == null || document.rootVisualElement == null)
                continue;

            VisualElement root = document.rootVisualElement;

            if (root.Q<Label>(turnName) != null ||
                root.Q<Label>(populationName) != null ||
                root.Q<Label>(stormName) != null ||
                root.Q<VisualElement>(gaugeName) != null)
            {
                return document;
            }
        }

        return null;
    }

    private TileMapData FindTileMapDataAsset()
    {
        TileMapData[] tileMaps = Resources.FindObjectsOfTypeAll<TileMapData>();

        for (int i = 0; i < tileMaps.Length; i++)
        {
            if (tileMaps[i] != null)
                return tileMaps[i];
        }

        return null;
    }

    private void LogMissingElement(VisualElement element, string elementName)
    {
        if (element != null || string.IsNullOrEmpty(elementName))
            return;

        WarnOnce(
            "missing-element-" + elementName,
            $"[ResourceBarRuntimeController] Could not find UI element named '{elementName}'.");
    }

    private void WarnOnce(string key, string message)
    {
        if (_warningKeys.Add(key))
            Debug.LogWarning(message);
    }
}
