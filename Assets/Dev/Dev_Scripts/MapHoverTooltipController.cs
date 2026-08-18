/*
Required Inspector references:
- Tooltip UI Document: UIDocument that hosts map hover tooltips. Prefer the Global HUD UIDocument.
- Evac Route Tooltip UXML: optional VisualTreeAsset for EvacRouteTooltip.uxml. Loaded by path in the Unity Editor if unassigned.
- Communication Tower Tooltip UXML: optional VisualTreeAsset for CommunicationTowerTooltip.uxml. Loaded by path in the Unity Editor if unassigned.
- Communication Tower Controller: optional status source for tower tooltip counts.

Current behavior:
- Manages UI Toolkit hover tooltips for evacuation routes and communication towers.
- Uses existing UXML roots if they are already inside the UIDocument.
- Clones the assigned tooltip UXML assets into the UIDocument when the roots are missing.
- Populates tooltip labels, follows the mouse cursor, clamps to the UI panel, and ignores pointer input.
- Does not calculate evacuation routes, communication tower placement, or gameplay scores.
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapHoverTooltipController : MonoBehaviour
{
    private const string EvacTooltipRootName = "evac_tooltip_root";
    private const string CommunicationTooltipRootName = "communication_tooltip_root";

    private const string EvacZoneLabelName = "zone_id_label";
    private const string EvacPopulationLabelName = "population_label";
    private const string EvacShelterIdLabelName = "shelter_id_label";
    private const string EvacShelterTypeLabelName = "shelter_type_label";
    private const string EvacShelterCapacityLabelName = "shelter_capacity_label";

    private const string TowerIdLabelName = "tower_id_label";
    private const string TowerRangeLabelName = "tower_range_label";
    private const string TowerStatusLabelName = "tower_status_label";
    private const string TowerWarnedCountLabelName = "warned_count_label";
    private const string TowerPendingCountLabelName = "pending_count_label";
    private const string TowerUnreachedCountLabelName = "unreached_count_label";
    private const string TowerServedZonesLabelName = "served_zones_label";

    [Header("References")]
    [SerializeField] private UIDocument tooltipUIDocument;
    [SerializeField] private VisualTreeAsset evacRouteTooltipUxml;
    [SerializeField] private VisualTreeAsset communicationTowerTooltipUxml;
    [SerializeField] private CommunicationTowerController communicationTowerController;

    [Header("Tooltip Settings")]
    [SerializeField] private Vector2 tooltipOffset = new(18f, 18f);
    [SerializeField, Min(0f)] private float screenEdgePadding = 8f;
    [SerializeField, Min(80f)] private float fallbackTooltipWidth = 260f;
    [SerializeField, Min(40f)] private float fallbackTooltipHeight = 120f;
    [SerializeField] private bool debugLogs = true;

    private readonly HashSet<string> _missingLabelWarnings = new();

    private VisualElement _root;
    private VisualElement _evacTooltipRoot;
    private VisualElement _communicationTooltipRoot;

    private Label _evacZoneLabel;
    private Label _evacPopulationLabel;
    private Label _evacShelterIdLabel;
    private Label _evacShelterTypeLabel;
    private Label _evacShelterCapacityLabel;

    private Label _towerIdLabel;
    private Label _towerRangeLabel;
    private Label _towerStatusLabel;
    private Label _towerWarnedCountLabel;
    private Label _towerPendingCountLabel;
    private Label _towerUnreachedCountLabel;
    private Label _towerServedZonesLabel;

    private string _visibleEvacRouteId;
    private string _visibleTowerId;
    private bool _evacInitialized;
    private bool _communicationInitialized;
    private bool _isEvacTooltipVisible;
    private bool _isCommunicationTooltipVisible;

    public bool IsEvacRouteTooltipVisible => _isEvacTooltipVisible;

    public bool IsCommunicationTowerTooltipVisible => _isCommunicationTooltipVisible;

    private void OnEnable()
    {
        EnsureTooltipRoots();
        HideAllTooltips();
    }

    private void OnDisable()
    {
        HideAllTooltips();
    }

    private void OnValidate()
    {
        screenEdgePadding = Mathf.Max(0f, screenEdgePadding);
        fallbackTooltipWidth = Mathf.Max(80f, fallbackTooltipWidth);
        fallbackTooltipHeight = Mathf.Max(40f, fallbackTooltipHeight);
    }

    public void SetTooltipUIDocument(UIDocument uiDocument)
    {
        if (tooltipUIDocument == uiDocument)
            return;

        tooltipUIDocument = uiDocument;
        ResetCachedElements();
        EnsureTooltipRoots();
    }

    public void SetCommunicationTowerController(CommunicationTowerController controller)
    {
        communicationTowerController = controller;
    }

    public void SetDebugLogs(bool enableDebugLogs)
    {
        debugLogs = enableDebugLogs;
    }

    public void ShowEvacRouteTooltip(SimpleEvacuationRoute route, Vector2 screenPosition)
    {
        if (route == null)
        {
            HideEvacRouteTooltip();
            return;
        }

        if (!EnsureEvacTooltip())
            return;

        string routeId = string.IsNullOrWhiteSpace(route.routeId) ? "Unknown Route" : route.routeId.Trim();
        SetLabelText(_evacZoneLabel, $"Zone: {SafeText(route.sourceZoneGeoid, "Unknown Zone")}");
        SetLabelText(_evacPopulationLabel, $"Population: {Mathf.Max(0, route.sourceZonePopulation)}");
        SetLabelText(_evacShelterIdLabel, $"Shelter ID: {SafeText(route.destinationShelterId, "Unknown Shelter")}");
        SetLabelText(_evacShelterTypeLabel, $"Shelter Type: {SafeText(route.destinationShelterType, "Shelter")}");
        SetLabelText(_evacShelterCapacityLabel, $"Shelter Capacity: {Mathf.Max(0, route.destinationShelterCapacity)}");

        bool isNewTooltipTarget = _visibleEvacRouteId != routeId;
        _visibleEvacRouteId = routeId;
        _isEvacTooltipVisible = true;

        ShowTooltipRoot(_evacTooltipRoot, screenPosition);

        if (debugLogs && isNewTooltipTarget)
            Debug.Log($"[MapHoverTooltipController] Showing evacuation tooltip for route {routeId}.");
    }

    public void HideEvacRouteTooltip()
    {
        if (_evacTooltipRoot != null)
            _evacTooltipRoot.style.display = DisplayStyle.None;

        if (!string.IsNullOrEmpty(_visibleEvacRouteId) && debugLogs)
            Debug.Log("[MapHoverTooltipController] Hiding evacuation route tooltip.");

        _visibleEvacRouteId = null;
        _isEvacTooltipVisible = false;
    }

    public void ShowCommunicationTowerTooltip(CommunicationTowerData tower, Vector2 screenPosition)
    {
        if (tower == null)
        {
            HideCommunicationTowerTooltip();
            return;
        }

        if (!EnsureCommunicationTooltip())
            return;

        string towerId = string.IsNullOrWhiteSpace(tower.towerId) ? "Unknown Tower" : tower.towerId.Trim();
        int zoneRange = tower.associatedZoneGeoids != null ? tower.associatedZoneGeoids.Count : 0;
        GetTowerStatusSummary(
            tower,
            out string statusText,
            out int warnedCount,
            out int pendingCount,
            out int unreachedCount);

        SetLabelText(_towerIdLabel, $"Tower ID: {towerId}");
        SetLabelText(_towerRangeLabel, $"Tower Zone Range: {zoneRange}");
        SetLabelText(_towerStatusLabel, $"Tower Status: {statusText}");
        SetLabelText(_towerWarnedCountLabel, $"Warned: {warnedCount}");
        SetLabelText(_towerPendingCountLabel, $"Pending: {pendingCount}");
        SetLabelText(_towerUnreachedCountLabel, $"Unreached: {unreachedCount}");
        SetLabelText(_towerServedZonesLabel, BuildServedZonesText(tower));

        bool isNewTooltipTarget = _visibleTowerId != towerId;
        _visibleTowerId = towerId;
        _isCommunicationTooltipVisible = true;

        ShowTooltipRoot(_communicationTooltipRoot, screenPosition);

        if (debugLogs && isNewTooltipTarget)
            Debug.Log($"[MapHoverTooltipController] Showing communication tower tooltip for tower {towerId}.");
    }

    public void HideCommunicationTowerTooltip()
    {
        if (_communicationTooltipRoot != null)
            _communicationTooltipRoot.style.display = DisplayStyle.None;

        if (!string.IsNullOrEmpty(_visibleTowerId) && debugLogs)
            Debug.Log("[MapHoverTooltipController] Hiding communication tower tooltip.");

        _visibleTowerId = null;
        _isCommunicationTooltipVisible = false;
    }

    public void HideAllTooltips()
    {
        HideEvacRouteTooltip();
        HideCommunicationTowerTooltip();
    }

    private bool EnsureTooltipRoots()
    {
        ResolveReferences();

        if (tooltipUIDocument == null || tooltipUIDocument.rootVisualElement == null)
        {
            if (debugLogs)
                Debug.LogWarning("[MapHoverTooltipController] No UIDocument is available for map hover tooltips.");

            return false;
        }

        _root = tooltipUIDocument.rootVisualElement;
        EnsureEvacTooltip();
        EnsureCommunicationTooltip();
        return _evacTooltipRoot != null || _communicationTooltipRoot != null;
    }

    private bool EnsureEvacTooltip()
    {
        if (_evacTooltipRoot != null)
            return true;

        if (!ResolveRoot())
            return false;

        _evacTooltipRoot = _root.Q<VisualElement>(EvacTooltipRootName);

        if (_evacTooltipRoot == null && evacRouteTooltipUxml != null)
        {
            VisualElement clone = evacRouteTooltipUxml.CloneTree();
            SetPickingModeRecursive(clone);
            _root.Add(clone);
            _evacTooltipRoot = clone.Q<VisualElement>(EvacTooltipRootName);
        }

        if (_evacTooltipRoot == null)
        {
            if (debugLogs)
                Debug.LogWarning($"[MapHoverTooltipController] Could not find or clone VisualElement '{EvacTooltipRootName}'.");

            return false;
        }

        ConfigureTooltipRoot(_evacTooltipRoot);
        _evacZoneLabel = QueryRequiredLabel(_evacTooltipRoot, EvacZoneLabelName, "Evacuation route tooltip");
        _evacPopulationLabel = QueryRequiredLabel(_evacTooltipRoot, EvacPopulationLabelName, "Evacuation route tooltip");
        _evacShelterIdLabel = QueryRequiredLabel(_evacTooltipRoot, EvacShelterIdLabelName, "Evacuation route tooltip");
        _evacShelterTypeLabel = QueryRequiredLabel(_evacTooltipRoot, EvacShelterTypeLabelName, "Evacuation route tooltip");
        _evacShelterCapacityLabel = QueryRequiredLabel(_evacTooltipRoot, EvacShelterCapacityLabelName, "Evacuation route tooltip");

        if (!_evacInitialized && debugLogs)
        {
            _evacInitialized = true;
            Debug.Log("[MapHoverTooltipController] Evacuation route tooltip initialized.");
        }

        return true;
    }

    private bool EnsureCommunicationTooltip()
    {
        if (_communicationTooltipRoot != null)
            return true;

        if (!ResolveRoot())
            return false;

        _communicationTooltipRoot = _root.Q<VisualElement>(CommunicationTooltipRootName);

        if (_communicationTooltipRoot == null && communicationTowerTooltipUxml != null)
        {
            VisualElement clone = communicationTowerTooltipUxml.CloneTree();
            SetPickingModeRecursive(clone);
            _root.Add(clone);
            _communicationTooltipRoot = clone.Q<VisualElement>(CommunicationTooltipRootName);
        }

        if (_communicationTooltipRoot == null)
        {
            if (debugLogs)
                Debug.LogWarning($"[MapHoverTooltipController] Could not find or clone VisualElement '{CommunicationTooltipRootName}'.");

            return false;
        }

        ConfigureTooltipRoot(_communicationTooltipRoot);
        _towerIdLabel = QueryRequiredLabel(_communicationTooltipRoot, TowerIdLabelName, "Communication tower tooltip");
        _towerRangeLabel = QueryRequiredLabel(_communicationTooltipRoot, TowerRangeLabelName, "Communication tower tooltip");
        _towerStatusLabel = QueryRequiredLabel(_communicationTooltipRoot, TowerStatusLabelName, "Communication tower tooltip");
        _towerWarnedCountLabel = _communicationTooltipRoot.Q<Label>(TowerWarnedCountLabelName);
        _towerPendingCountLabel = _communicationTooltipRoot.Q<Label>(TowerPendingCountLabelName);
        _towerUnreachedCountLabel = _communicationTooltipRoot.Q<Label>(TowerUnreachedCountLabelName);
        _towerServedZonesLabel = _communicationTooltipRoot.Q<Label>(TowerServedZonesLabelName);

        if (!_communicationInitialized && debugLogs)
        {
            _communicationInitialized = true;
            Debug.Log("[MapHoverTooltipController] Communication tower tooltip initialized.");
        }

        return true;
    }

    private void ResolveReferences()
    {
        if (tooltipUIDocument == null)
            tooltipUIDocument = ResolveTooltipUIDocument();

        if (communicationTowerController == null)
            communicationTowerController = FindFirstObjectByType<CommunicationTowerController>();

        ResolveTooltipAssets();
    }

    private bool ResolveRoot()
    {
        if (_root != null)
            return true;

        ResolveReferences();

        if (tooltipUIDocument == null || tooltipUIDocument.rootVisualElement == null)
            return false;

        _root = tooltipUIDocument.rootVisualElement;
        return true;
    }

    private UIDocument ResolveTooltipUIDocument()
    {
        if (GlobalHUDController.Instance != null &&
            GlobalHUDController.Instance.TryGetComponent(out UIDocument globalDocument))
        {
            return globalDocument;
        }

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        UIDocument[] documents = UnityEngine.Object.FindObjectsOfType<UIDocument>(true);
#endif

        UIDocument fallback = null;

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];

            if (document == null || document.rootVisualElement == null)
                continue;

            fallback ??= document;

            VisualElement root = document.rootVisualElement;

            if (root.Q<Label>("money_label") != null ||
                root.Q<Label>("pop_label") != null ||
                root.Q<Label>("zone_label") != null ||
                root.Q<VisualElement>(EvacTooltipRootName) != null ||
                root.Q<VisualElement>(CommunicationTooltipRootName) != null)
            {
                return document;
            }
        }

        return fallback;
    }

    private void ResolveTooltipAssets()
    {
#if UNITY_EDITOR
        if (evacRouteTooltipUxml == null)
        {
            evacRouteTooltipUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Dev/Dev_Scripts/UI_UXML_USS/UI Editor/EvacRouteTooltip.uxml");
        }

        if (communicationTowerTooltipUxml == null)
        {
            communicationTowerTooltipUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Dev/Dev_Scripts/UI_UXML_USS/UI Editor/CommunicationTowerTooltip.uxml");
        }
#endif
    }

    private void ConfigureTooltipRoot(VisualElement tooltipRoot)
    {
        tooltipRoot.pickingMode = PickingMode.Ignore;
        SetPickingModeRecursive(tooltipRoot);
        tooltipRoot.style.position = Position.Absolute;
        tooltipRoot.style.display = DisplayStyle.None;
        tooltipRoot.style.width = StyleKeyword.Auto;
        tooltipRoot.style.height = StyleKeyword.Auto;
        tooltipRoot.style.maxWidth = fallbackTooltipWidth;
    }

    private void SetPickingModeRecursive(VisualElement element)
    {
        if (element == null)
            return;

        element.pickingMode = PickingMode.Ignore;

        foreach (VisualElement child in element.Children())
            SetPickingModeRecursive(child);
    }

    private Label QueryRequiredLabel(VisualElement root, string labelName, string tooltipName)
    {
        Label label = root != null ? root.Q<Label>(labelName) : null;

        if (label == null && debugLogs && _missingLabelWarnings.Add($"{tooltipName}:{labelName}"))
            Debug.LogWarning($"[MapHoverTooltipController] {tooltipName} is missing required Label '{labelName}'.");

        return label;
    }

    private void ShowTooltipRoot(VisualElement tooltipRoot, Vector2 screenPosition)
    {
        if (tooltipRoot == null)
            return;

        tooltipRoot.style.display = DisplayStyle.Flex;
        tooltipRoot.BringToFront();
        PositionTooltip(tooltipRoot, screenPosition);
    }

    private void PositionTooltip(VisualElement tooltipRoot, Vector2 screenPosition)
    {
        if (tooltipRoot == null || !ResolveRoot())
            return;

        Vector2 panelPosition = ScreenToPanelPosition(screenPosition);
        float rootWidth = GetElementWidth(_root, Screen.width);
        float rootHeight = GetElementHeight(_root, Screen.height);
        float tooltipWidth = GetElementWidth(tooltipRoot, fallbackTooltipWidth);
        float tooltipHeight = GetElementHeight(tooltipRoot, fallbackTooltipHeight);

        float maxLeft = Mathf.Max(screenEdgePadding, rootWidth - tooltipWidth - screenEdgePadding);
        float maxTop = Mathf.Max(screenEdgePadding, rootHeight - tooltipHeight - screenEdgePadding);
        float left = Mathf.Clamp(panelPosition.x + tooltipOffset.x, screenEdgePadding, maxLeft);
        float top = Mathf.Clamp(panelPosition.y + tooltipOffset.y, screenEdgePadding, maxTop);

        tooltipRoot.style.left = left;
        tooltipRoot.style.top = top;
    }

    private Vector2 ScreenToPanelPosition(Vector2 screenPosition)
    {
        if (_root != null && _root.panel != null)
            return RuntimePanelUtils.ScreenToPanel(_root.panel, screenPosition);

        return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
    }

    private float GetElementWidth(VisualElement element, float fallback)
    {
        if (element != null && element.resolvedStyle.width > 0f && !float.IsNaN(element.resolvedStyle.width))
            return element.resolvedStyle.width;

        return Mathf.Max(1f, fallback);
    }

    private float GetElementHeight(VisualElement element, float fallback)
    {
        if (element != null && element.resolvedStyle.height > 0f && !float.IsNaN(element.resolvedStyle.height))
            return element.resolvedStyle.height;

        return Mathf.Max(1f, fallback);
    }

    private void GetTowerStatusSummary(
        CommunicationTowerData tower,
        out string statusText,
        out int warnedCount,
        out int pendingCount,
        out int unreachedCount)
    {
        warnedCount = 0;
        pendingCount = 0;
        unreachedCount = 0;

        int zoneCount = tower != null && tower.associatedZoneGeoids != null
            ? tower.associatedZoneGeoids.Count
            : 0;

        if (tower == null || !tower.isActive)
        {
            unreachedCount = zoneCount;
            statusText = "Unreached";
            return;
        }

        if (communicationTowerController == null)
            communicationTowerController = FindFirstObjectByType<CommunicationTowerController>();

        for (int i = 0; i < zoneCount; i++)
        {
            string geoid = tower.associatedZoneGeoids[i];

            if (communicationTowerController == null ||
                !communicationTowerController.TryGetCommunicationStatus(geoid, out ZoneCommunicationStatus status))
            {
                status = ZoneCommunicationStatus.Pending;
            }

            switch (status)
            {
                case ZoneCommunicationStatus.Warned:
                    warnedCount++;
                    break;

                case ZoneCommunicationStatus.Pending:
                    pendingCount++;
                    break;

                default:
                    unreachedCount++;
                    break;
            }
        }

        statusText = zoneCount > 0 && warnedCount == zoneCount
            ? "Warned"
            : "Pending";
    }

    private string BuildServedZonesText(CommunicationTowerData tower)
    {
        if (tower == null || tower.associatedZoneGeoids == null || tower.associatedZoneGeoids.Count == 0)
            return "Served Zones: 0";

        if (tower.associatedZoneGeoids.Count > 4)
            return $"Served Zones: {tower.associatedZoneGeoids.Count}";

        return $"Served Zones: {string.Join(", ", tower.associatedZoneGeoids)}";
    }

    private string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private void SetLabelText(Label label, string text)
    {
        if (label != null)
            label.text = text;
    }

    private void ResetCachedElements()
    {
        _root = null;
        _evacTooltipRoot = null;
        _communicationTooltipRoot = null;
        _evacZoneLabel = null;
        _evacPopulationLabel = null;
        _evacShelterIdLabel = null;
        _evacShelterTypeLabel = null;
        _evacShelterCapacityLabel = null;
        _towerIdLabel = null;
        _towerRangeLabel = null;
        _towerStatusLabel = null;
        _towerWarnedCountLabel = null;
        _towerPendingCountLabel = null;
        _towerUnreachedCountLabel = null;
        _towerServedZonesLabel = null;
        _visibleEvacRouteId = null;
        _visibleTowerId = null;
        _isEvacTooltipVisible = false;
        _isCommunicationTooltipVisible = false;
    }
}
