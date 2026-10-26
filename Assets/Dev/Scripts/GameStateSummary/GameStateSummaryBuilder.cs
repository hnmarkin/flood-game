using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameStateSummaryBuilder : MonoBehaviour
{
    [Header("Source Controllers")]
    [SerializeField] private ShelterController shelterController;
    [SerializeField] private ZoneBaselineRiskController zoneBaselineRiskController;
    [SerializeField] private FloodImpactController floodImpactController;
    [SerializeField] private EvacuationController evacuationController;
    [SerializeField] private CommunicationController communicationTowerController;

    [Header("Optional Event Sources")]
    [SerializeField] private HighRiskManager highRiskManager;

    [Header("Game Info Fallbacks")]
    [SerializeField] private string currentGamePhase = "PreCrisis";
    [SerializeField, Min(0)] private int currentTurn = 1;

    [Header("Summary Settings")]
    [SerializeField, Min(1)] private int topRiskZoneCount = 5;
    [SerializeField, Min(1)] private int topFloodImpactZoneCount = 5;

    [Header("Debug")]
    [SerializeField] private bool logJsonToConsole = true;
    [SerializeField] private bool prettyPrintJson = true;
    [SerializeField] private bool logDisplayIdMappings = true;
    [SerializeField] private UIDocument debugUIDocument;
    [SerializeField] private string debugJsonLabelName = "game_state_json_label";

    private readonly HashSet<string> _missingReferenceWarnings = new(StringComparer.Ordinal);

    private VisualElement _debugJsonElement;
    private GameStateSummary _latestSummary;
    private string _latestSummaryJson = string.Empty;

    private bool _subscribedToShelterController;
    private bool _subscribedToZoneBaselineRiskController;
    private bool _subscribedToFloodImpactController;
    private bool _subscribedToEvacuationController;
    private bool _subscribedToCommunicationTowerController;
    private bool _subscribedToHighRiskManager;
    private bool _subscribedToBarrierEvents;

    public GameStateSummary LatestSummary => _latestSummary;
    public string LatestSummaryJson => _latestSummaryJson;

    private void Reset()
    {
        ResolveReferences();

        if (debugUIDocument == null)
            debugUIDocument = GetComponent<UIDocument>();
    }

    private void Awake()
    {
        ResolveReferences();
        BindDebugJsonLabel();
        Debug.Log("[GameStateSummaryBuilder] Initialized.");
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindDebugJsonLabel();
        SubscribeToControllerEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromControllerEvents();
    }

    private void OnValidate()
    {
        currentTurn = Mathf.Max(0, currentTurn);
        topRiskZoneCount = Mathf.Max(1, topRiskZoneCount);
        topFloodImpactZoneCount = Mathf.Max(1, topFloodImpactZoneCount);

        if (string.IsNullOrWhiteSpace(currentGamePhase))
            currentGamePhase = "PreCrisis";

        if (string.IsNullOrWhiteSpace(debugJsonLabelName))
            debugJsonLabelName = "game_state_json_label";
    }

    public void SetGameInfo(string phase, int turn)
    {
        currentGamePhase = string.IsNullOrWhiteSpace(phase) ? "Unknown" : phase.Trim();
        currentTurn = Mathf.Max(0, turn);
    }

    public void RefreshSummary(string lastPlayerAction)
    {
        _latestSummaryJson = BuildSummaryJson(lastPlayerAction);

        if (logJsonToConsole)
            Debug.Log(_latestSummaryJson);

        DisplayLatestJson();
        Debug.Log($"[GameStateSummaryBuilder] Summary refreshed. Last action: {GetSafeAction(lastPlayerAction)}.");
    }

    public string BuildSummaryJson(string lastPlayerAction)
    {
        _latestSummary = BuildSummary(lastPlayerAction);
        _latestSummaryJson = JsonUtility.ToJson(_latestSummary, prettyPrintJson);
        Debug.Log("[GameStateSummaryBuilder] JSON summary generated.");
        return _latestSummaryJson;
    }

    public GameStateSummary BuildSummary(string lastPlayerAction)
    {
        ResolveReferences();
        LogMissingReferenceWarnings();

        return new GameStateSummary
        {
            game = BuildGameInfoSummary(lastPlayerAction),
            risk = BuildRiskSummary(),
            floodImpact = BuildFloodImpactSummary(),
            shelters = BuildShelterSummary(),
            evacuation = BuildEvacuationSummary(),
            communication = BuildCommunicationSummary(),
        };
    }

    private void ResolveReferences()
    {
        if (shelterController == null)
            shelterController = FindFirstObjectByType<ShelterController>();

        if (zoneBaselineRiskController == null)
            zoneBaselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (floodImpactController == null)
            floodImpactController = FindFirstObjectByType<FloodImpactController>();

        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (communicationTowerController == null)
            communicationTowerController = FindFirstObjectByType<CommunicationController>();

        if (highRiskManager == null)
            highRiskManager = FindFirstObjectByType<HighRiskManager>();

        if (debugUIDocument == null)
            debugUIDocument = GetComponent<UIDocument>();
    }

    private void SubscribeToControllerEvents()
    {
        if (shelterController != null && !_subscribedToShelterController)
        {
            shelterController.OnSheltersChanged += OnSheltersChanged;
            _subscribedToShelterController = true;
        }

        if (zoneBaselineRiskController != null && !_subscribedToZoneBaselineRiskController)
        {
            zoneBaselineRiskController.BaselineRiskCalculated += OnBaselineRiskCalculated;
            zoneBaselineRiskController.ShelterAdjustedRiskRefreshed += OnRiskRefreshed;
            _subscribedToZoneBaselineRiskController = true;
        }

        if (floodImpactController != null && !_subscribedToFloodImpactController)
        {
            floodImpactController.FloodImpactRefreshed += OnFloodImpactRefreshed;
            _subscribedToFloodImpactController = true;
        }

        if (evacuationController != null && !_subscribedToEvacuationController)
        {
            evacuationController.OnEvacuationRoutesBuilt += OnEvacuationRoutesBuilt;
            evacuationController.OnEvacuationRoutesHidden += OnEvacuationRoutesHidden;
            evacuationController.OnEvacuationRouteSelected += OnEvacuationRouteSelected;
            evacuationController.OnEvacuationRouteDeselected += OnEvacuationRouteDeselected;
            _subscribedToEvacuationController = true;
        }

        if (communicationTowerController != null && !_subscribedToCommunicationTowerController)
        {
            communicationTowerController.CommunicationModeChanged += OnCommunicationModeChanged;
            communicationTowerController.OnTowerActivated += OnTowerActivated;
            communicationTowerController.OnZoneWarned += OnZoneWarned;
            _subscribedToCommunicationTowerController = true;
        }

        if (highRiskManager != null && !_subscribedToHighRiskManager)
        {
            highRiskManager.BaselineRiskInspectionShown += OnBaselineRiskInspectionShown;
            _subscribedToHighRiskManager = true;
        }

        if (!_subscribedToBarrierEvents)
        {
            BarrierController.OnAllZoneBarriersPlaced += OnAllZoneBarriersPlaced;
            _subscribedToBarrierEvents = true;
        }

        // Future hooks: call RefreshSummary("BarrierPlaced") from a per-barrier event,
        // and RefreshSummary("FloodTurnAdvanced") from the future flood-turn controller.
    }

    private void UnsubscribeFromControllerEvents()
    {
        if (shelterController != null && _subscribedToShelterController)
        {
            shelterController.OnSheltersChanged -= OnSheltersChanged;
            _subscribedToShelterController = false;
        }

        if (zoneBaselineRiskController != null && _subscribedToZoneBaselineRiskController)
        {
            zoneBaselineRiskController.BaselineRiskCalculated -= OnBaselineRiskCalculated;
            zoneBaselineRiskController.ShelterAdjustedRiskRefreshed -= OnRiskRefreshed;
            _subscribedToZoneBaselineRiskController = false;
        }

        if (floodImpactController != null && _subscribedToFloodImpactController)
        {
            floodImpactController.FloodImpactRefreshed -= OnFloodImpactRefreshed;
            _subscribedToFloodImpactController = false;
        }

        if (evacuationController != null && _subscribedToEvacuationController)
        {
            evacuationController.OnEvacuationRoutesBuilt -= OnEvacuationRoutesBuilt;
            evacuationController.OnEvacuationRoutesHidden -= OnEvacuationRoutesHidden;
            evacuationController.OnEvacuationRouteSelected -= OnEvacuationRouteSelected;
            evacuationController.OnEvacuationRouteDeselected -= OnEvacuationRouteDeselected;
            _subscribedToEvacuationController = false;
        }

        if (communicationTowerController != null && _subscribedToCommunicationTowerController)
        {
            communicationTowerController.CommunicationModeChanged -= OnCommunicationModeChanged;
            communicationTowerController.OnTowerActivated -= OnTowerActivated;
            communicationTowerController.OnZoneWarned -= OnZoneWarned;
            _subscribedToCommunicationTowerController = false;
        }

        if (highRiskManager != null && _subscribedToHighRiskManager)
        {
            highRiskManager.BaselineRiskInspectionShown -= OnBaselineRiskInspectionShown;
            _subscribedToHighRiskManager = false;
        }

        if (_subscribedToBarrierEvents)
        {
            BarrierController.OnAllZoneBarriersPlaced -= OnAllZoneBarriersPlaced;
            _subscribedToBarrierEvents = false;
        }
    }

    private GameInfoSummary BuildGameInfoSummary(string lastPlayerAction)
    {
        return new GameInfoSummary
        {
            phase = string.IsNullOrWhiteSpace(currentGamePhase) ? "Unknown" : currentGamePhase.Trim(),
            turn = Mathf.Max(0, currentTurn),
            lastPlayerAction = GetSafeAction(lastPlayerAction),
            summaryGeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        };
    }

    private RiskSummary BuildRiskSummary()
    {
        RiskSummary summary = new();

        if (zoneBaselineRiskController == null)
            return summary;

        IReadOnlyList<ZoneBaselineRiskData> riskResults = zoneBaselineRiskController.GetAllRiskResults();

        if (riskResults == null)
            return summary;

        List<ZoneBaselineRiskData> sortedRiskResults = new();
        summary.totalZones = riskResults.Count;

        for (int i = 0; i < riskResults.Count; i++)
        {
            ZoneBaselineRiskData data = riskResults[i];

            switch (data.adjustedRiskLevel)
            {
                case RiskLevel.Critical:
                    summary.criticalZones++;
                    break;

                case RiskLevel.High:
                    summary.highRiskZones++;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(data.geoid))
                sortedRiskResults.Add(data);
        }

        sortedRiskResults.Sort(CompareBaselineRiskDescending);

        int limit = Mathf.Min(topRiskZoneCount, sortedRiskResults.Count);

        for (int i = 0; i < limit; i++)
        {
            ZoneBaselineRiskData data = sortedRiskResults[i];

            summary.topRiskZones.Add(new ZoneRiskSummary
            {
                displayId = GetZoneDisplayId(data.geoid),
                geoid = NormalizeId(data.geoid),
                population = Mathf.Max(0, data.rawPopulation),
                riskLevel = data.adjustedRiskLevel.ToString(),
                riskScore = RoundScore(data.adjustedBaselineRiskScore),
            });
        }

        return summary;
    }

    private FloodImpactSummary BuildFloodImpactSummary()
    {
        FloodImpactSummary summary = new();

        if (floodImpactController == null)
            return summary;

        IReadOnlyList<ZoneFloodImpactResult> impactResults = floodImpactController.GetAllFloodImpactResults();
        summary.floodActive = floodImpactController.HasCalculatedLiveImpact;

        if (impactResults == null)
            return summary;

        summary.totalZones = impactResults.Count;

        for (int i = 0; i < impactResults.Count; i++)
        {
            ZoneFloodImpactResult result = impactResults[i];

            if (result.floodedTileCount > 0 || result.floodedFraction > 0f)
                summary.floodedZones++;

            summary.affectedPopulation += Mathf.RoundToInt(Mathf.Max(0f, result.affectedPopulation));
            summary.estimatedDamage += Mathf.Max(0f, result.estimatedDamage);
        }

        summary.estimatedDamage = RoundCurrency(summary.estimatedDamage);

        List<ZoneFloodImpactResult> topLiveRiskZones = floodImpactController.GetTopFloodImpactZones(topFloodImpactZoneCount);

        for (int i = 0; i < topLiveRiskZones.Count; i++)
        {
            ZoneFloodImpactResult result = topLiveRiskZones[i];

            if (string.IsNullOrWhiteSpace(result.geoid))
                continue;

            summary.topLiveRiskZones.Add(new LiveFloodZoneSummary
            {
                displayId = GetZoneDisplayId(result.geoid),
                geoid = NormalizeId(result.geoid),
                population = Mathf.Max(0, result.rawPopulation),
                riskLevel = result.riskLevel.ToString(),
                liveFloodRisk = RoundScore(result.liveFloodRisk),
                floodedFraction = RoundScore(result.floodedFraction),
                affectedPopulation = Mathf.RoundToInt(Mathf.Max(0f, result.affectedPopulation)),
                estimatedDamage = RoundCurrency(result.estimatedDamage),
            });
        }

        return summary;
    }

    private ShelterSummary BuildShelterSummary()
    {
        ShelterSummary summary = new();

        if (shelterController == null)
            return summary;

        List<PlacedShelterData> activeShelters = shelterController.GetActiveShelters();
        summary.activeShelters = activeShelters != null ? activeShelters.Count : shelterController.GetActiveShelterCount();
        summary.maxShelters = shelterController.GetMaxPlacedShelters();

        if (activeShelters == null)
            return summary;

        for (int i = 0; i < activeShelters.Count; i++)
        {
            PlacedShelterData shelter = activeShelters[i];

            if (shelter == null)
                continue;

            ShelterStateSummary shelterSummary = new()
            {
                displayId = GetShelterDisplayId(shelter.shelterId),
                shelterId = NormalizeId(shelter.shelterId),
                shelterType = string.IsNullOrWhiteSpace(shelter.shelterTypeName) ? "Shelter" : shelter.shelterTypeName.Trim(),
                capacity = Mathf.Max(0, shelter.capacity),
            };

            AddZoneLists(shelter.associatedZoneGeoids, shelterSummary.associatedZones, shelterSummary.associatedZoneGeoids);
            summary.shelters.Add(shelterSummary);
        }

        return summary;
    }

    private EvacuationSummary BuildEvacuationSummary()
    {
        EvacuationSummary summary = new();

        if (evacuationController == null)
            return summary;

        IReadOnlyList<SimpleEvacuationRoute> availableRoutes = evacuationController.GetAvailableRoutes();
        IReadOnlyList<SimpleEvacuationRoute> selectedRoutes = evacuationController.GetSelectedRoutes();

        summary.previewActive = evacuationController.IsPreviewActive();
        summary.availableRoutes = CountValidRoutes(availableRoutes);
        summary.selectedRoutes = CountValidRoutes(selectedRoutes);
        summary.totalEvacuatedPopulation = evacuationController.GetTotalEvacuatedPopulation();

        if (availableRoutes == null)
            return summary;

        for (int i = 0; i < availableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = availableRoutes[i];

            if (route == null || !route.isValid)
                continue;

            string sourceZoneGeoid = NormalizeId(route.sourceZoneGeoid);
            bool isSelected = evacuationController.IsRouteSelected(route.routeId);

            summary.routes.Add(new EvacuationRouteSummary
            {
                displayId = GetEvacuationRouteDisplayId(route.routeId),
                routeId = NormalizeId(route.routeId),
                sourceZone = GetZoneDisplayId(route.sourceZoneGeoid),
                sourceZoneGeoid = sourceZoneGeoid,
                sourceZonePopulation = Mathf.Max(0, route.sourceZonePopulation),
                destinationShelter = GetShelterDisplayId(route.destinationShelterId),
                destinationShelterId = NormalizeId(route.destinationShelterId),
                destinationShelterCapacity = Mathf.Max(0, route.destinationShelterCapacity),
                isSelected = isSelected,
                distanceTiles = Mathf.RoundToInt(Mathf.Max(0f, route.distanceTiles)),
                evacuatedPopulation = evacuationController.GetEvacuatedPopulationForZone(sourceZoneGeoid),
                evacuationMitigation = RoundScore(evacuationController.GetEvacuationMitigationForZone(sourceZoneGeoid)),
                remainingPopulationAfterEvacuation = evacuationController.GetRemainingPopulationAfterEvacuation(sourceZoneGeoid),
            });
        }

        return summary;
    }

    private CommunicationSummary BuildCommunicationSummary()
    {
        CommunicationSummary summary = new();

        if (communicationTowerController == null)
            return summary;

        IReadOnlyList<CommunicationTowerData> towers = communicationTowerController.GetAllTowers();
        HashSet<string> zoneGeoids = new(StringComparer.Ordinal);

        if (towers != null)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                CommunicationTowerData tower = towers[i];

                if (tower == null)
                    continue;

                if (tower.isActive)
                    summary.activeTowers++;

                CommunicationTowerSummary towerSummary = new()
                {
                    displayId = GetTowerDisplayId(tower.towerId),
                    towerId = NormalizeId(tower.towerId),
                    isActive = tower.isActive,
                };

                AddZoneLists(tower.associatedZoneGeoids, towerSummary.associatedZones, towerSummary.associatedZoneGeoids, zoneGeoids);
                summary.towers.Add(towerSummary);
            }
        }

        Dictionary<string, ZoneCommunicationStatus> statusByGeoid = communicationTowerController.GetAllZoneCommunicationStatuses();

        foreach (string geoid in statusByGeoid.Keys)
        {
            string normalizedGeoid = NormalizeId(geoid);

            if (!string.IsNullOrEmpty(normalizedGeoid))
                zoneGeoids.Add(normalizedGeoid);
        }

        List<string> sortedGeoids = new(zoneGeoids);
        sortedGeoids.Sort(StringComparer.Ordinal);

        for (int i = 0; i < sortedGeoids.Count; i++)
        {
            string geoid = sortedGeoids[i];
            ZoneCommunicationStatus status = communicationTowerController.GetZoneCommunicationStatus(geoid);

            switch (status)
            {
                case ZoneCommunicationStatus.Warned:
                    summary.warnedZones++;
                    break;

                case ZoneCommunicationStatus.Pending:
                    summary.pendingZones++;
                    break;

                default:
                    summary.unreachedZones++;
                    break;
            }

            summary.zoneStatuses.Add(new ZoneCommunicationSummary
            {
                zone = GetZoneDisplayId(geoid),
                geoid = geoid,
                status = status.ToString(),
            });
        }

        return summary;
    }

    private void BindDebugJsonLabel()
    {
        _debugJsonElement = null;

        if (debugUIDocument == null ||
            debugUIDocument.rootVisualElement == null ||
            string.IsNullOrWhiteSpace(debugJsonLabelName))
        {
            return;
        }

        _debugJsonElement = debugUIDocument.rootVisualElement.Q(debugJsonLabelName.Trim());
    }

    private void DisplayLatestJson()
    {
        if (_debugJsonElement == null)
            BindDebugJsonLabel();

        if (_debugJsonElement is TextField textField)
            textField.SetValueWithoutNotify(_latestSummaryJson);
        else if (_debugJsonElement is TextElement textElement)
            textElement.text = _latestSummaryJson;
    }

    private void LogMissingReferenceWarnings()
    {
        LogMissingControllerWarning(nameof(ShelterController), shelterController != null);
        LogMissingControllerWarning(nameof(ZoneBaselineRiskController), zoneBaselineRiskController != null);
        LogMissingControllerWarning(nameof(FloodImpactController), floodImpactController != null);
        LogMissingControllerWarning(nameof(EvacuationController), evacuationController != null);
        LogMissingControllerWarning(nameof(CommunicationController), communicationTowerController != null);
    }

    private void LogMissingControllerWarning(string controllerName, bool isAssigned)
    {
        if (isAssigned)
        {
            _missingReferenceWarnings.Remove(controllerName);
            return;
        }

        if (_missingReferenceWarnings.Add(controllerName))
        {
            Debug.LogWarning(
                $"[GameStateSummaryBuilder] {controllerName} reference is missing. " +
                "Summary generation will continue with empty fallback data for that section.");
        }
    }

    private void OnSheltersChanged()
    {
        RefreshSummary("ShelterChanged");
    }

    private void OnBaselineRiskCalculated()
    {
        RefreshSummary("RiskCalculated");
    }

    private void OnRiskRefreshed()
    {
        RefreshSummary("RiskRefreshed");
    }

    private void OnBaselineRiskInspectionShown()
    {
        RefreshSummary("RiskInspected");
    }

    private void OnFloodImpactRefreshed()
    {
        RefreshSummary("FloodImpactUpdated");
    }

    private void OnEvacuationRoutesBuilt()
    {
        RefreshSummary("EvacuationRoutesBuilt");
    }

    private void OnEvacuationRoutesHidden()
    {
        RefreshSummary("EvacuationRoutesHidden");
    }

    private void OnEvacuationRouteSelected(SimpleEvacuationRoute route)
    {
        RefreshSummary("EvacuationRouteSelected");
    }

    private void OnEvacuationRouteDeselected(SimpleEvacuationRoute route)
    {
        RefreshSummary("EvacuationRouteDeselected");
    }

    private void OnCommunicationModeChanged(bool isActive)
    {
        RefreshSummary(isActive ? "CommunicationModeEnabled" : "CommunicationModeDisabled");
    }

    private void OnTowerActivated(CommunicationTowerData tower)
    {
        RefreshSummary("ActivatedCommunicationTower");
    }

    private void OnZoneWarned(string geoid)
    {
        RefreshSummary("ZoneWarningSent");
    }

    private void OnAllZoneBarriersPlaced()
    {
        RefreshSummary("AllZoneBarriersPlaced");
    }

    private void AddZoneLists(
        IList<string> sourceGeoids,
        List<string> displayGeoids,
        List<string> originalGeoids,
        HashSet<string> uniqueGeoids = null)
    {
        if (sourceGeoids == null)
            return;

        HashSet<string> seenGeoids = new(StringComparer.Ordinal);

        for (int i = 0; i < sourceGeoids.Count; i++)
        {
            string geoid = NormalizeId(sourceGeoids[i]);

            if (string.IsNullOrEmpty(geoid) || !seenGeoids.Add(geoid))
                continue;

            displayGeoids.Add(GetZoneDisplayId(geoid));
            originalGeoids.Add(geoid);
            uniqueGeoids?.Add(geoid);
        }
    }

    private int CountValidRoutes(IReadOnlyList<SimpleEvacuationRoute> routes)
    {
        if (routes == null)
            return 0;

        int count = 0;

        for (int i = 0; i < routes.Count; i++)
        {
            SimpleEvacuationRoute route = routes[i];

            if (route != null && route.isValid)
                count++;
        }

        return count;
    }

    private int CompareBaselineRiskDescending(ZoneBaselineRiskData a, ZoneBaselineRiskData b)
    {
        int riskCompare = b.adjustedBaselineRiskScore.CompareTo(a.adjustedBaselineRiskScore);

        if (riskCompare != 0)
            return riskCompare;

        return string.CompareOrdinal(a.geoid, b.geoid);
    }

    private string GetZoneDisplayId(string geoid)
    {
        return DemoDisplayIdFormatter.GetZoneDisplayId(geoid);
    }

    private string GetShelterDisplayId(string shelterId)
    {
        return DemoDisplayIdFormatter.GetShelterDisplayId(shelterId);
    }

    private string GetEvacuationRouteDisplayId(string routeId)
    {
        return DemoDisplayIdFormatter.GetEvacuationRouteDisplayId(routeId);
    }

    private string GetTowerDisplayId(string towerId)
    {
        return DemoDisplayIdFormatter.GetTowerDisplayId(towerId);
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }

    private static string GetSafeAction(string lastPlayerAction)
    {
        return string.IsNullOrWhiteSpace(lastPlayerAction) ? "Unknown" : lastPlayerAction.Trim();
    }

    private static float RoundScore(float value)
    {
        return Mathf.Round(Mathf.Clamp01(value) * 1000f) / 1000f;
    }

    private static float RoundCurrency(float value)
    {
        return Mathf.Round(Mathf.Max(0f, value) * 100f) / 100f;
    }
}

[Serializable]
public class GameStateSummary
{
    public GameInfoSummary game = new();
    public RiskSummary risk = new();
    public FloodImpactSummary floodImpact = new();
    public ShelterSummary shelters = new();
    public EvacuationSummary evacuation = new();
    public CommunicationSummary communication = new();
}

[Serializable]
public class GameInfoSummary
{
    public string phase;
    public int turn;
    public string lastPlayerAction;
    public string summaryGeneratedAt;
}

[Serializable]
public class RiskSummary
{
    public int totalZones;
    public int criticalZones;
    public int highRiskZones;
    public List<ZoneRiskSummary> topRiskZones = new();
}

[Serializable]
public class ZoneRiskSummary
{
    public string displayId;
    public string geoid;
    public int population;
    public string riskLevel;
    public float riskScore;
}

[Serializable]
public class FloodImpactSummary
{
    public bool floodActive;
    public int totalZones;
    public int floodedZones;
    public int affectedPopulation;
    public float estimatedDamage;
    public List<LiveFloodZoneSummary> topLiveRiskZones = new();
}

[Serializable]
public class LiveFloodZoneSummary
{
    public string displayId;
    public string geoid;
    public int population;
    public string riskLevel;
    public float liveFloodRisk;
    public float floodedFraction;
    public int affectedPopulation;
    public float estimatedDamage;
}

[Serializable]
public class ShelterSummary
{
    public int activeShelters;
    public int maxShelters;
    public List<ShelterStateSummary> shelters = new();
}

[Serializable]
public class ShelterStateSummary
{
    public string displayId;
    public string shelterId;
    public string shelterType;
    public int capacity;
    public List<string> associatedZones = new();
    public List<string> associatedZoneGeoids = new();
}

[Serializable]
public class EvacuationSummary
{
    public bool previewActive;
    public int availableRoutes;
    public int selectedRoutes;
    public int totalEvacuatedPopulation;
    public List<EvacuationRouteSummary> routes = new();
}

[Serializable]
public class EvacuationRouteSummary
{
    public string displayId;
    public string routeId;
    public string sourceZone;
    public string sourceZoneGeoid;
    public int sourceZonePopulation;
    public string destinationShelter;
    public string destinationShelterId;
    public int destinationShelterCapacity;
    public bool isSelected;
    public int distanceTiles;
    public int evacuatedPopulation;
    public float evacuationMitigation;
    public int remainingPopulationAfterEvacuation;
}

[Serializable]
public class CommunicationSummary
{
    public int activeTowers;
    public int warnedZones;
    public int pendingZones;
    public int unreachedZones;
    public List<CommunicationTowerSummary> towers = new();
    public List<ZoneCommunicationSummary> zoneStatuses = new();
}

[Serializable]
public class CommunicationTowerSummary
{
    public string displayId;
    public string towerId;
    public bool isActive;
    public List<string> associatedZones = new();
    public List<string> associatedZoneGeoids = new();
}

[Serializable]
public class ZoneCommunicationSummary
{
    public string zone;
    public string geoid;
    public string status;
}
