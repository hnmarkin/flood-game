using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

/*
Inspector references required by EvacuationController:
- Baseline Risk Controller: optional helper for existing risk systems; route anchors are calculated from zone tiles.
- Shelter Controller: public interface to the Shelter System; supplies active placed shelters, associated zone GEOIDs, and shelter placement state.
- Zone Controller: supplies GEOID-to-tile lookup and normalized-tile/Tilemap-cell conversion.
- Tile Map Data: supplies tile categories, water depth, and map bounds for simple route traversal.
- Route Tilemap: supplies the Unity Tilemap cells sent to EvacuationRouteVisualizer.
- Route Visualizer: draws already-calculated route path cells as tile-border outlines.
- Map Hover Tooltip: optional UI Toolkit tooltip controller for hovered route metadata.
- Route Tooltip UI Document: optional HUD UIDocument host for the tooltip UXML.
- Main Camera: converts mouse position to route tile cells for hover/select interaction.

Current behavior:
EvacuationController owns the simple shelter-route prototype. It toggles evacuation preview,
builds one SimpleEvacuationRoute from each associated zone center anchor to each active placed
shelter, caches those routes, tracks route hover/selection ids, and sends already-calculated paths
to EvacuationRouteVisualizer for preview drawing. When a route is hovered, it sends route metadata
to MapHoverTooltipController for a small cursor-following UXML tooltip.
It does not draw visuals directly, route to city exits, color danger segments,
draw white overlays, directly edit risk formulas, or score route danger.
*/

[Serializable]
public class SimpleEvacuationRoute
{
    public string routeId;
    public string sourceZoneGeoid;
    public int sourceZonePopulation;
    public string destinationShelterId;
    public string destinationShelterType;
    public int destinationShelterCapacity;
    public Vector3Int startCell;
    public Vector3Int shelterCell;
    public List<Vector3Int> pathCells = new();
    public float distanceTiles;
    public bool isValid;
}

public class EvacuationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ZoneBaselineRiskController baselineRiskController;
    [SerializeField] private ShelterController shelterController;
    [SerializeField] private ZoneController zoneController;
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap routeTilemap;
    [SerializeField] private EvacuationRouteVisualizer routeVisualizer;
    [SerializeField] private MapHoverTooltipController mapHoverTooltip;
    [SerializeField] private UIDocument routeTooltipUIDocument;
    [SerializeField] private Camera mainCamera;

    [Header("Inspector Settings")]
    [SerializeField] private Color routeColor = new(0f, 0.75f, 1f, 1f);
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool previewModeActive;
    [SerializeField, Min(1)] private int maxPathSearchIterations = 6000;
    [SerializeField, Min(0)] private int minimumVisibleRouteLengthTiles = 8;
    [SerializeField] private bool skipShortRoutes = true;

    [Header("Simple Tile Movement Costs")]
    [SerializeField, Min(0.01f)] private float roadTileCost = 1f;
    [SerializeField, Min(0.01f)] private float landTileCost = 2f;
    [SerializeField, Min(0.01f)] private float waterTileCost = 999f;
    [SerializeField, Min(0f)] private float deepFloodWaterThreshold = 0.5f;

    [Header("Route Selection Prototype")]
    [SerializeField] private bool allowRouteSelection = true;
    [SerializeField] private bool limitSelectedRoutes = true;
    [SerializeField, Min(1)] private int maxSelectedRoutes = 1;
    [SerializeField] private bool lockRouteAfterSelection = false;

    private readonly EvacuationState _state = new();
    private readonly EvacuationMitigationCalculator _mitigation = new();
    private readonly EvacuationInputController _input = new();
    private readonly EvacuationPresentation _presentation = new();
    private bool _isSubscribedToShelters;
    private bool _isSubscribedToShelterPlacement;
    private bool _hasAttemptedZoneControllerResolution;

    public bool IsPreviewModeActive => previewModeActive;
    public SimpleEvacuationRoute CurrentRoute => _state.CurrentRoute;
    public IReadOnlyList<SimpleEvacuationRoute> CurrentRoutes => _state.AvailableRoutes;
    public bool HasConfirmedEvacuationRoute => _state.HasConfirmedEvacuationRoute;
    public string ConfirmedRouteId => _state.ConfirmedRouteId;
    public event Action OnEvacuationRoutesChanged;
    public event Action OnEvacuationRoutesBuilt;
    public event Action OnEvacuationRoutesHidden;
    public event Action<SimpleEvacuationRoute> OnEvacuationRouteSelected;
    public event Action<SimpleEvacuationRoute> OnEvacuationRouteDeselected;
    public event Action<SimpleEvacuationRoute> OnEvacuationRouteConfirmed;

    public bool IsPreviewActive()
    {
        return previewModeActive;
    }

    public IReadOnlyList<SimpleEvacuationRoute> GetAvailableRoutes()
    {
        return _state.AvailableRoutes;
    }

    public IReadOnlyList<SimpleEvacuationRoute> GetSelectedRoutes()
    {
        return _state.GetSelectedRoutes();
    }

    public bool IsRouteSelected(string routeId)
    {
        return _state.IsRouteSelected(routeId);
    }

    public void ShowOnlySelectedRoute(string routeId)
    {
        if (!TrySelectRoute(routeId))
            return;

        _presentation.ShowOnlySelectedRoute(routeVisualizer, routeId);
    }

    public void ShowOnlySelectedRoute(int routeIndex)
    {
        if (routeIndex < 0 || routeIndex >= _state.AvailableRoutes.Count)
        {
            Debug.LogWarning($"[EvacuationController] Cannot select route index {routeIndex}; available route count is {_state.AvailableRoutes.Count}.");
            return;
        }

        SimpleEvacuationRoute route = _state.AvailableRoutes[routeIndex];

        if (route == null || string.IsNullOrEmpty(route.routeId))
        {
            Debug.LogWarning($"[EvacuationController] Cannot select route index {routeIndex}; route data is missing.");
            return;
        }

        ShowOnlySelectedRoute(route.routeId);
    }

    public void ClearRouteSelectionAndShowAll()
    {
        if (_state.SelectedRouteIds.Count == 0 && !_state.HasConfirmedEvacuationRoute)
        {
            _presentation.ClearRouteSelectionAndShowAll(routeVisualizer);
            return;
        }

        List<SimpleEvacuationRoute> deselectedRoutes = new(GetSelectedRoutes());

        _state.ClearSelectedRoutes();
        _state.ClearConfirmation();

        RecalculateEvacuationMitigation();

        if (previewModeActive)
            RedrawVisibleRoutes();
        else
            _presentation.ClearRouteSelectionAndShowAll(routeVisualizer);

        if (deselectedRoutes != null)
        {
            for (int i = 0; i < deselectedRoutes.Count; i++)
                OnEvacuationRouteDeselected?.Invoke(deselectedRoutes[i]);
        }

        if (debugLogs)
            Debug.Log("[EvacuationController] Cleared selected evacuation routes and restored all route previews.");

        OnEvacuationRoutesChanged?.Invoke();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureVisualizerConfigured();
        EnsureTooltipConfigured();
        previewModeActive = false;
        ClearVisibleRouteVisuals();

        if (debugLogs)
            Debug.Log("[EvacuationController] Preview starts disabled.");
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureVisualizerConfigured();
        EnsureTooltipConfigured();
        SubscribeToShelterChanges();
        SubscribeToShelterPlacementChanges();
    }

    private void OnDisable()
    {
        UnsubscribeFromShelterChanges();
        UnsubscribeFromShelterPlacementChanges();
        HideEvacuationRoutes();
    }

    private void OnValidate()
    {
        maxPathSearchIterations = Mathf.Max(1, maxPathSearchIterations);
        roadTileCost = Mathf.Max(0.01f, roadTileCost);
        landTileCost = Mathf.Max(0.01f, landTileCost);
        waterTileCost = Mathf.Max(0.01f, waterTileCost);
        deepFloodWaterThreshold = Mathf.Max(0f, deepFloodWaterThreshold);
        maxSelectedRoutes = Mathf.Max(1, maxSelectedRoutes);
        minimumVisibleRouteLengthTiles = Mathf.Max(0, minimumVisibleRouteLengthTiles);
    }

    private void Update()
    {
        if (!previewModeActive)
            return;

        UpdateRouteHoverAndSelection();
    }

    public void ToggleEvacuationPreview()
    {
        if (previewModeActive)
        {
            HideEvacuationRoutes();
            return;
        }

        ShowEvacuationRoutes();
    }

    public void ToggleEvacuationPreviewMode()
    {
        ToggleEvacuationPreview();
    }

    public void ShowEvacuationRoutes()
    {
        if (IsShelterPlacementModeActive())
        {
            previewModeActive = false;
            RefreshEvacuationRoutes();

            if (debugLogs)
                Debug.Log("[EvacuationController] Shelter placement mode active. Evacuation visuals hidden, selected routes preserved.");

            return;
        }

        previewModeActive = true;

        if (debugLogs)
            Debug.Log("[EvacuationController] Evacuation preview enabled by player.");

        RefreshEvacuationRoutes();

        if (debugLogs)
            Debug.Log($"[EvacuationController] Selected routes preserved after preview toggle: {_state.SelectedRouteIds.Count}.");
    }

    public void HideEvacuationRoutes()
    {
        bool hadRouteState = previewModeActive || _state.VisibleRoutes.Count > 0 || !string.IsNullOrEmpty(_state.HoveredRouteId);

        previewModeActive = false;
        ClearVisibleRouteVisuals();

        if (!hadRouteState)
            return;

        if (debugLogs)
            Debug.Log("[EvacuationController] Evacuation preview hidden. Route data and selected routes preserved.");

        OnEvacuationRoutesHidden?.Invoke();
        OnEvacuationRoutesChanged?.Invoke();
    }

    public void RefreshEvacuationRoutes()
    {
        ResolveReferences();
        EnsureVisualizerConfigured();

        bool rebuiltRoutes = RebuildAvailableRouteData();

        if (rebuiltRoutes)
        {
            RemoveInvalidSelectedRouteIds();
            RecalculateEvacuationMitigation();
        }

        if (previewModeActive && IsShelterPlacementModeActive())
        {
            previewModeActive = false;
            ClearVisibleRouteVisuals();

            if (debugLogs)
                Debug.Log("[EvacuationController] Shelter placement mode active. Evacuation visuals hidden, selected routes preserved.");

            OnEvacuationRoutesHidden?.Invoke();
        }
        else if (previewModeActive)
        {
            RedrawVisibleRoutes();
        }
        else
        {
            ClearVisibleRouteVisuals();
        }

        if (rebuiltRoutes)
            OnEvacuationRoutesBuilt?.Invoke();

        OnEvacuationRoutesChanged?.Invoke();
    }

    public void ShowSingleEvacuationRoute()
    {
        ShowEvacuationRoutes();
    }

    public void HideEvacuationRoute()
    {
        HideEvacuationRoutes();
    }

    public void RefreshSingleRoute()
    {
        RefreshEvacuationRoutes();
    }

    public bool HasEvacuationRouteForZone(string geoid)
    {
        return _state.HasEvacuationRouteForZone(geoid);
    }

    public bool HasSelectedEvacuationRouteForZone(string geoid)
    {
        return _state.HasSelectedEvacuationRouteForZone(geoid);
    }

    public bool TrySelectRoute(string routeId)
    {
        if (!allowRouteSelection)
        {
            if (debugLogs)
                Debug.Log("[EvacuationController] Route selection is disabled. Routes are preview-only.");

            return false;
        }

        routeId = NormalizeGeoid(routeId);

        if (string.IsNullOrEmpty(routeId))
        {
            Debug.LogWarning("[EvacuationController] Cannot select an evacuation route because the route id is empty.");
            return false;
        }

        if (IsRouteSelectionLockedAgainst(routeId))
            return false;

        if (_state.ContainsSelectedRouteId(routeId))
            return true;

        SimpleEvacuationRoute route = GetRouteById(routeId);

        if (route == null || !route.isValid)
        {
            Debug.LogWarning($"[EvacuationController] Could not select route '{routeId}' because it is not in the available route cache.");
            return false;
        }

        if (!PrepareSelectionSlot(routeId))
            return false;

        _state.AddSelectedRoute(routeId);
        bool routeConfirmed = TryConfirmRouteSelection(routeId);

        if (debugLogs)
            Debug.Log($"[EvacuationController] Selected evacuation route: {routeId}");

        RecalculateEvacuationMitigation();
        RedrawVisibleRoutes();
        OnEvacuationRouteSelected?.Invoke(route);
        if (routeConfirmed)
            OnEvacuationRouteConfirmed?.Invoke(route);

        OnEvacuationRoutesChanged?.Invoke();
        return true;
    }

    private void UpdateRouteHoverAndSelection()
    {
        if (_input.ShouldClearHover(_state))
        {
            SetHoveredRoute(null, Vector2.zero);
            return;
        }

        if (routeTilemap == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        _input.ProcessInput(
            _state,
            routeTilemap,
            mainCamera,
            SetHoveredRoute,
            UpdateRouteTooltipPosition,
            TryToggleRouteSelection);
    }

    private void SetHoveredRoute(string routeId, Vector2 screenPosition)
    {
        if (!_input.TryGetHoverChange(_state, routeId, out routeId))
            return;

        bool hadHover = !string.IsNullOrEmpty(_state.HoveredRouteId);
        _state.SetHoveredRouteId(routeId);

        if (debugLogs)
        {
            if (string.IsNullOrEmpty(_state.HoveredRouteId))
            {
                if (hadHover)
                    Debug.Log("[EvacuationController] Evacuation route hover cleared.");
            }
            else
            {
                Debug.Log($"[EvacuationController] Hovered evacuation route: {_state.HoveredRouteId}");
            }
        }

        if (string.IsNullOrEmpty(_state.HoveredRouteId))
        {
            HideRouteTooltip();
        }
        else
        {
            ShowRouteTooltip(_state.HoveredRouteId, screenPosition);
        }

        RedrawVisibleRoutes();
        OnEvacuationRoutesChanged?.Invoke();
    }

    private void ShowRouteTooltip(string routeId, Vector2 screenPosition)
    {
        SimpleEvacuationRoute route = GetRouteById(routeId);

        if (route == null)
        {
            HideRouteTooltip();
            return;
        }

        EnsureTooltipConfigured();

        if (mapHoverTooltip == null)
            return;

        _presentation.ShowRouteTooltip(mapHoverTooltip, route, screenPosition, debugLogs);
    }

    private void UpdateRouteTooltipPosition(Vector2 screenPosition)
    {
        _presentation.UpdateRouteTooltipPosition(_state, mapHoverTooltip, screenPosition, debugLogs);
    }

    private void HideRouteTooltip()
    {
        _presentation.HideRouteTooltip(mapHoverTooltip, debugLogs);
    }

    private bool TryToggleRouteSelection(string routeId)
    {
        if (!allowRouteSelection)
        {
            if (debugLogs)
                Debug.Log("[EvacuationController] Route selection is disabled. Routes are preview-only.");

            return false;
        }

        routeId = NormalizeGeoid(routeId);

        if (string.IsNullOrEmpty(routeId))
        {
            Debug.LogWarning("[EvacuationController] Cannot select an evacuation route because the route id is empty.");
            return false;
        }

        if (IsRouteSelectionLockedAgainst(routeId))
            return false;

        if (_state.ContainsSelectedRouteId(routeId))
        {
            if (lockRouteAfterSelection &&
                _state.HasConfirmedEvacuationRoute &&
                string.Equals(_state.ConfirmedRouteId, routeId, StringComparison.Ordinal))
            {
                LogRouteSelectionLocked();
                return false;
            }

            SimpleEvacuationRoute route = GetRouteById(routeId);
            _state.RemoveSelectedRoute(routeId);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Deselected evacuation route: {routeId}");

            RecalculateEvacuationMitigation();
            RedrawVisibleRoutes();
            if (route != null)
                OnEvacuationRouteDeselected?.Invoke(route);

            OnEvacuationRoutesChanged?.Invoke();
            return true;
        }

        return TrySelectRoute(routeId);
    }

    private bool PrepareSelectionSlot(string routeId)
    {
        if (!limitSelectedRoutes || _state.SelectedRouteIds.Count < maxSelectedRoutes)
            return true;

        if (maxSelectedRoutes == 1)
        {
            DeselectSelectedRoutesExcept(routeId);
            return true;
        }

        Debug.LogWarning($"[EvacuationController] Route selection blocked. Max selected routes reached: {maxSelectedRoutes}.");
        return false;
    }

    private bool IsRouteSelectionLockedAgainst(string routeId)
    {
        if (!lockRouteAfterSelection || !_state.HasConfirmedEvacuationRoute)
            return false;

        if (string.Equals(_state.ConfirmedRouteId, routeId, StringComparison.Ordinal))
            return false;

        LogRouteSelectionLocked();
        return true;
    }

    private bool TryConfirmRouteSelection(string routeId)
    {
        if (!lockRouteAfterSelection || _state.HasConfirmedEvacuationRoute)
            return false;

        _state.StoreConfirmation(routeId);

        LogRouteSelectionLocked();

        return true;
    }

    private void LogRouteSelectionLocked()
    {
        if (debugLogs)
            Debug.Log($"[EvacuationController] Route selection locked. Confirmed route: {_state.ConfirmedRouteId}.");
    }

    private void DeselectSelectedRoutesExcept(string routeIdToKeep)
    {
        if (_state.SelectedRouteIds.Count == 0)
            return;

        List<string> routesToDeselect = _state.GetSelectedRouteIdsExcept(routeIdToKeep);

        List<SimpleEvacuationRoute> deselectedRoutes = new();

        for (int i = 0; i < routesToDeselect.Count; i++)
        {
            string routeId = routesToDeselect[i];
            SimpleEvacuationRoute route = GetRouteById(routeId);
            _state.RemoveSelectedRoute(routeId);

            if (route != null)
                deselectedRoutes.Add(route);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Deselected evacuation route: {routeId}");
        }

        if (routesToDeselect.Count == 0)
            return;

        RecalculateEvacuationMitigation();
        RedrawVisibleRoutes();

        for (int i = 0; i < deselectedRoutes.Count; i++)
            OnEvacuationRouteDeselected?.Invoke(deselectedRoutes[i]);

        OnEvacuationRoutesChanged?.Invoke();
    }

    private SimpleEvacuationRoute GetRouteById(string routeId)
    {
        return _state.GetRouteById(routeId);
    }

    private void ClearVisibleRouteVisuals()
    {
        _presentation.ClearVisibleRouteVisuals(_state, ref routeVisualizer, ref mapHoverTooltip, debugLogs);
    }

    private void RedrawVisibleRoutes()
    {
        _presentation.RedrawVisibleRoutes(_state, previewModeActive, ref routeVisualizer);
    }

    public float GetEvacuationMitigationForZone(string geoid)
    {
        return _mitigation.GetEvacuationMitigationForZone(geoid);
    }

    public int GetEvacuatedPopulationForZone(string geoid)
    {
        return _mitigation.GetEvacuatedPopulationForZone(geoid);
    }

    public int GetRemainingPopulationAfterEvacuation(string geoid)
    {
        return _mitigation.GetRemainingPopulationAfterEvacuation(geoid, _state.AvailableRoutes, GetSourceZonePopulation);
    }

    public int GetTotalEvacuatedPopulation()
    {
        return _mitigation.GetTotalEvacuatedPopulation();
    }

    private void ResolveReferences()
    {
        if (baselineRiskController == null)
            baselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (shelterController == null)
            shelterController = FindFirstObjectByType<ShelterController>();

        if (zoneController == null && !_hasAttemptedZoneControllerResolution)
        {
            _hasAttemptedZoneControllerResolution = true;
            zoneController = FindFirstObjectByType<ZoneController>();
        }

        if (routeTilemap == null && zoneController != null)
            routeTilemap = zoneController.TerrainTilemap;

        if (routeVisualizer == null)
            routeVisualizer = GetComponent<EvacuationRouteVisualizer>();

        if (routeVisualizer == null)
            routeVisualizer = GetComponentInChildren<EvacuationRouteVisualizer>(true);

        if (routeVisualizer == null)
        {
            GameObject visualizerObject = new("EvacuationRouteVisualizer");
            visualizerObject.transform.SetParent(transform, false);
            routeVisualizer = visualizerObject.AddComponent<EvacuationRouteVisualizer>();
        }

        if (mapHoverTooltip == null)
            mapHoverTooltip = FindFirstObjectByType<MapHoverTooltipController>();

        if (routeTooltipUIDocument == null)
            routeTooltipUIDocument = ResolveTooltipUIDocument();

        if (mapHoverTooltip == null)
        {
            GameObject tooltipObject = new("MapHoverTooltipController");
            tooltipObject.transform.SetParent(transform, false);
            mapHoverTooltip = tooltipObject.AddComponent<MapHoverTooltipController>();
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (tileMapData == null)
            tileMapData = FindTileMapDataAsset();
    }

    private void EnsureVisualizerConfigured()
    {
        _presentation.EnsureVisualizerConfigured(ref routeVisualizer, routeTilemap, debugLogs);
    }

    private void EnsureTooltipConfigured()
    {
        if (mapHoverTooltip == null)
            return;

        if (routeTooltipUIDocument == null)
            routeTooltipUIDocument = ResolveTooltipUIDocument();

        mapHoverTooltip.SetTooltipUIDocument(routeTooltipUIDocument);
        mapHoverTooltip.SetDebugLogs(debugLogs);
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
                root.Q<Label>("zone_label") != null)
            {
                return document;
            }
        }

        return fallback;
    }

    private void SubscribeToShelterChanges()
    {
        if (_isSubscribedToShelters || shelterController == null)
            return;

        shelterController.OnSheltersChanged += OnSheltersChanged;
        _isSubscribedToShelters = true;
    }

    private void UnsubscribeFromShelterChanges()
    {
        if (!_isSubscribedToShelters || shelterController == null)
            return;

        shelterController.OnSheltersChanged -= OnSheltersChanged;
        _isSubscribedToShelters = false;
    }

    private void SubscribeToShelterPlacementChanges()
    {
        if (_isSubscribedToShelterPlacement || shelterController == null)
            return;

        shelterController.ShelterPlacementModeChanged += OnShelterPlacementModeChanged;
        _isSubscribedToShelterPlacement = true;
    }

    private void UnsubscribeFromShelterPlacementChanges()
    {
        if (!_isSubscribedToShelterPlacement || shelterController == null)
            return;

        shelterController.ShelterPlacementModeChanged -= OnShelterPlacementModeChanged;
        _isSubscribedToShelterPlacement = false;
    }

    private void OnSheltersChanged()
    {
        RefreshEvacuationRoutes();
    }

    private void OnShelterPlacementModeChanged(bool isActive)
    {
        if (!isActive || (!previewModeActive && _state.VisibleRoutes.Count == 0))
            return;

        HideEvacuationRoutes();

        if (debugLogs)
            Debug.Log("[EvacuationController] Shelter placement mode active. Evacuation visuals hidden, selected routes preserved.");
    }

    private bool IsShelterPlacementModeActive()
    {
        if (shelterController == null)
            shelterController = FindFirstObjectByType<ShelterController>();

        return shelterController != null && shelterController.IsShelterPlacementModeActive;
    }

    private bool RebuildAvailableRouteData()
    {
        _state.ClearAvailableRoutes();

        if (!ValidateRouteReferences())
            return false;

        EvacuationPathfinder pathfinder = CreatePathfinder();
        EvacuationRouteGenerator generator = new(
            shelterController,
            zoneController,
            baselineRiskController,
            routeTilemap,
            pathfinder,
            skipShortRoutes,
            minimumVisibleRouteLengthTiles,
            debugLogs);
        generator.GenerateRoutes(_state.AvailableRoutes);
        return true;
    }

    private EvacuationPathfinder CreatePathfinder()
    {
        return new EvacuationPathfinder(
            tileMapData,
            routeTilemap,
            zoneController,
            roadTileCost,
            landTileCost,
            deepFloodWaterThreshold,
            maxPathSearchIterations,
            debugLogs);
    }

    private bool ValidateRouteReferences()
    {
        bool isValid = true;

        if (shelterController == null)
        {
            Debug.LogWarning("[EvacuationController] ShelterController is not assigned.");
            isValid = false;
        }

        if (zoneController == null)
        {
            Debug.LogWarning("[EvacuationController] ZoneController is not assigned.");
            isValid = false;
        }

        if (tileMapData == null)
        {
            Debug.LogWarning("[EvacuationController] TileMapData is not assigned.");
            isValid = false;
        }

        if (routeTilemap == null)
        {
            Debug.LogWarning("[EvacuationController] Route Tilemap is not assigned.");
            isValid = false;
        }

        if (routeVisualizer == null)
        {
            Debug.LogWarning("[EvacuationController] EvacuationRouteVisualizer is not assigned.");
            isValid = false;
        }

        return isValid;
    }

    private void RemoveInvalidSelectedRouteIds()
    {
        _state.RemoveInvalidSelectedRouteIds(debugLogs);
    }

    private void RecalculateEvacuationMitigation()
    {
        _mitigation.RecalculateEvacuationMitigation(_state.AvailableRoutes, _state.SelectedRouteIds, GetSourceZonePopulation, debugLogs);
    }

    private TileMapData FindTileMapDataAsset()
    {
        TileMapData[] tileMaps = Resources.FindObjectsOfTypeAll<TileMapData>();

        for (int i = 0; i < tileMaps.Length; i++)
        {
            TileMapData candidate = tileMaps[i];

            if (candidate != null && (candidate.N > 0 || candidate.sizeX > 0))
                return candidate;
        }

        return null;
    }

    private int GetSourceZonePopulation(string geoid)
    {
        return EvacuationRouteGenerator.GetSourceZonePopulation(geoid, baselineRiskController);
    }

    private static string NormalizeGeoid(string geoid)
    {
        return EvacuationState.NormalizeGeoid(geoid);
    }
}
