using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

/*
Inspector references required by EvacuationController:
- Baseline Risk Controller: optional helper for existing risk systems; route anchors are calculated from zone tiles.
- Shelter Manager: supplies active placed shelters and their associated zone GEOIDs.
- Flood Defense Box Stamp: supplies GEOID-to-tile lookup and normalized-tile to Tilemap-cell conversion.
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
It does not draw visuals directly, score routes, route to city exits, color danger segments,
draw white overlays, or apply evacuation mitigation.
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
    private struct ZoneAnchor
    {
        public Vector2Int normalizedTile;
        public Vector3Int cell;
        public string preferenceLabel;
    }

    private struct RouteTileSnapshot
    {
        public string category;
        public bool isWater;
        public bool isDeepFlooded;
    }

    private static readonly Vector2Int[] NeighborOffsets =
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
    };

    [Header("References")]
    [SerializeField] private ZoneBaselineRiskController baselineRiskController;
    [SerializeField] private ShelterManager shelterManager;
    [SerializeField] private FloodDefenseBoxStamp floodDefenseBoxStamp;
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

    [Header("Simple Tile Movement Costs")]
    [SerializeField, Min(0.01f)] private float roadTileCost = 1f;
    [SerializeField, Min(0.01f)] private float landTileCost = 2f;
    [SerializeField, Min(0.01f)] private float waterTileCost = 999f;
    [SerializeField, Min(0f)] private float deepFloodWaterThreshold = 0.5f;

    [Header("Route Selection Prototype")]
    [SerializeField] private bool allowRouteSelection = true;
    [SerializeField] private bool limitSelectedRoutes = true;
    [SerializeField, Min(1)] private int maxSelectedRoutes = 1;

    private readonly List<SimpleEvacuationRoute> _currentRoutes = new();
    private readonly Dictionary<Vector3Int, List<string>> routeIdsByCell = new();
    private string hoveredRouteId;
    private HashSet<string> selectedRouteIds = new HashSet<string>();
    private bool _isSubscribedToShelters;

    public bool IsPreviewModeActive => previewModeActive;
    public SimpleEvacuationRoute CurrentRoute => _currentRoutes.Count > 0 ? _currentRoutes[0] : null;
    public IReadOnlyList<SimpleEvacuationRoute> CurrentRoutes => _currentRoutes;
    public event Action OnEvacuationRoutesChanged;

    private void Awake()
    {
        ResolveReferences();
        EnsureVisualizerConfigured();
        EnsureTooltipConfigured();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureVisualizerConfigured();
        EnsureTooltipConfigured();
        SubscribeToShelterChanges();
    }

    private void OnDisable()
    {
        UnsubscribeFromShelterChanges();
        HideEvacuationRoute();
    }

    private void OnValidate()
    {
        maxPathSearchIterations = Mathf.Max(1, maxPathSearchIterations);
        roadTileCost = Mathf.Max(0.01f, roadTileCost);
        landTileCost = Mathf.Max(0.01f, landTileCost);
        waterTileCost = Mathf.Max(0.01f, waterTileCost);
        deepFloodWaterThreshold = Mathf.Max(0f, deepFloodWaterThreshold);
        maxSelectedRoutes = Mathf.Max(1, maxSelectedRoutes);
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
            HideEvacuationRoute();
            return;
        }

        previewModeActive = true;

        if (debugLogs)
            Debug.Log("[EvacuationController] Evacuation preview enabled.");

        ShowSingleEvacuationRoute();
    }

    public void ToggleEvacuationPreviewMode()
    {
        ToggleEvacuationPreview();
    }

    public void ShowSingleEvacuationRoute()
    {
        previewModeActive = true;
        RefreshSingleRoute();
    }

    public void HideEvacuationRoute()
    {
        bool hadRouteState = previewModeActive || _currentRoutes.Count > 0;

        previewModeActive = false;
        _currentRoutes.Clear();
        ClearRouteInteractionState(true);

        if (routeVisualizer != null)
            routeVisualizer.ClearRoute();

        if (hadRouteState && debugLogs)
            Debug.Log("[EvacuationController] Evacuation preview disabled.");

        OnEvacuationRoutesChanged?.Invoke();
    }

    public void RefreshSingleRoute()
    {
        ResolveReferences();
        EnsureVisualizerConfigured();

        _currentRoutes.Clear();
        ClearRouteInteractionState(true);

        if (routeVisualizer != null)
            routeVisualizer.ClearRoute();

        if (!previewModeActive)
        {
            OnEvacuationRoutesChanged?.Invoke();
            return;
        }

        if (!ValidateRouteReferences())
        {
            OnEvacuationRoutesChanged?.Invoke();
            return;
        }

        List<PlacedShelterData> activeShelters = shelterManager.GetActiveShelters();
        int activeShelterCount = activeShelters != null ? activeShelters.Count : 0;

        if (debugLogs)
            Debug.Log($"[EvacuationController] Active shelters found: {activeShelterCount}.");

        if (activeShelterCount == 0)
        {
            Debug.LogWarning("[EvacuationController] No active shelters placed. Cannot draw evacuation routes yet.");
            OnEvacuationRoutesChanged?.Invoke();
            return;
        }

        if (debugLogs)
            Debug.Log("[EvacuationController] Building evacuation routes for all active shelters.");

        BuildRoutesForAllActiveShelters(activeShelters);
        BuildRouteCellLookup();

        if (_currentRoutes.Count > 0)
            RedrawVisibleRoutes();

        if (debugLogs)
            Debug.Log($"[EvacuationController] Total evacuation routes built: {_currentRoutes.Count}.");

        OnEvacuationRoutesChanged?.Invoke();
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

        if (selectedRouteIds.Contains(routeId))
            return true;

        if (!HasVisibleRoute(routeId))
        {
            Debug.LogWarning($"[EvacuationController] Could not select route '{routeId}' because it is not in the current preview cache.");
            return false;
        }

        if (!PrepareSelectionSlot(routeId))
            return false;

        selectedRouteIds.Add(routeId);

        if (debugLogs)
            Debug.Log($"[EvacuationController] Selected evacuation route: {routeId}");

        RedrawVisibleRoutes();
        OnEvacuationRoutesChanged?.Invoke();
        return true;
    }

    private void UpdateRouteHoverAndSelection()
    {
        if (_currentRoutes.Count == 0 || routeIdsByCell.Count == 0)
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

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 mouseScreen = new(Input.mousePosition.x, Input.mousePosition.y);

        Vector3Int hoveredCell = routeTilemap.WorldToCell(mouseWorld);
        string routeId = GetHoverRouteIdForCell(hoveredCell);
        SetHoveredRoute(routeId, mouseScreen);

        if (!string.IsNullOrEmpty(hoveredRouteId))
            UpdateRouteTooltipPosition(mouseScreen);

        if (!string.IsNullOrEmpty(routeId) && Input.GetMouseButtonDown(0))
            TryToggleRouteSelection(routeId);
    }

    private string GetHoverRouteIdForCell(Vector3Int cell)
    {
        if (!routeIdsByCell.TryGetValue(cell, out List<string> routeIds) || routeIds == null || routeIds.Count == 0)
            return null;

        for (int i = 0; i < routeIds.Count; i++)
        {
            string routeId = routeIds[i];

            if (!string.IsNullOrEmpty(routeId) && selectedRouteIds.Contains(routeId))
                return routeId;
        }

        return routeIds[0];
    }

    private void SetHoveredRoute(string routeId, Vector2 screenPosition)
    {
        routeId = NormalizeGeoid(routeId);

        if (string.IsNullOrEmpty(routeId))
            routeId = null;

        if (hoveredRouteId == routeId)
            return;

        bool hadHover = !string.IsNullOrEmpty(hoveredRouteId);
        hoveredRouteId = routeId;

        if (debugLogs)
        {
            if (string.IsNullOrEmpty(hoveredRouteId))
            {
                if (hadHover)
                    Debug.Log("[EvacuationController] Evacuation route hover cleared.");
            }
            else
            {
                Debug.Log($"[EvacuationController] Hovered evacuation route: {hoveredRouteId}");
            }
        }

        if (string.IsNullOrEmpty(hoveredRouteId))
        {
            HideRouteTooltip();
        }
        else
        {
            ShowRouteTooltip(hoveredRouteId, screenPosition);
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

        mapHoverTooltip.ShowEvacRouteTooltip(route, screenPosition);

        if (debugLogs)
        {
            Debug.Log(
                $"[EvacuationController] Showing route tooltip for zone {route.sourceZoneGeoid} to shelter {route.destinationShelterId}.");
        }
    }

    private void UpdateRouteTooltipPosition(Vector2 screenPosition)
    {
        if (mapHoverTooltip == null || !mapHoverTooltip.IsEvacRouteTooltipVisible)
            return;

        SimpleEvacuationRoute route = GetRouteById(hoveredRouteId);

        if (route == null)
        {
            HideRouteTooltip();
            return;
        }

        mapHoverTooltip.ShowEvacRouteTooltip(route, screenPosition);
    }

    private void HideRouteTooltip()
    {
        if (mapHoverTooltip == null || !mapHoverTooltip.IsEvacRouteTooltipVisible)
            return;

        mapHoverTooltip.HideEvacRouteTooltip();

        if (debugLogs)
            Debug.Log("[EvacuationController] Hiding evacuation route tooltip.");
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

        if (selectedRouteIds.Contains(routeId))
        {
            selectedRouteIds.Remove(routeId);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Deselected evacuation route: {routeId}");

            RedrawVisibleRoutes();
            OnEvacuationRoutesChanged?.Invoke();
            return true;
        }

        return TrySelectRoute(routeId);
    }

    private bool PrepareSelectionSlot(string routeId)
    {
        if (!limitSelectedRoutes || selectedRouteIds.Count < maxSelectedRoutes)
            return true;

        if (maxSelectedRoutes == 1)
        {
            DeselectSelectedRoutesExcept(routeId);
            return true;
        }

        Debug.LogWarning($"[EvacuationController] Route selection blocked. Max selected routes reached: {maxSelectedRoutes}.");
        return false;
    }

    private void DeselectSelectedRoutesExcept(string routeIdToKeep)
    {
        if (selectedRouteIds.Count == 0)
            return;

        List<string> routesToDeselect = new();

        foreach (string selectedRouteId in selectedRouteIds)
        {
            if (selectedRouteId != routeIdToKeep)
                routesToDeselect.Add(selectedRouteId);
        }

        for (int i = 0; i < routesToDeselect.Count; i++)
        {
            selectedRouteIds.Remove(routesToDeselect[i]);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Deselected evacuation route: {routesToDeselect[i]}");
        }
    }

    private void BuildRouteCellLookup()
    {
        routeIdsByCell.Clear();

        for (int i = 0; i < _currentRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = _currentRoutes[i];

            if (route == null || string.IsNullOrEmpty(route.routeId) || route.pathCells == null)
                continue;

            for (int cellIndex = 0; cellIndex < route.pathCells.Count; cellIndex++)
            {
                Vector3Int cell = route.pathCells[cellIndex];

                if (!routeIdsByCell.TryGetValue(cell, out List<string> routeIds))
                {
                    routeIds = new List<string>();
                    routeIdsByCell[cell] = routeIds;
                }

                if (!routeIds.Contains(route.routeId))
                    routeIds.Add(route.routeId);
            }
        }
    }

    private bool HasVisibleRoute(string routeId)
    {
        return GetRouteById(routeId) != null;
    }

    private SimpleEvacuationRoute GetRouteById(string routeId)
    {
        routeId = NormalizeGeoid(routeId);

        if (string.IsNullOrEmpty(routeId))
            return null;

        for (int i = 0; i < _currentRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = _currentRoutes[i];

            if (route != null && route.routeId == routeId)
                return route;
        }

        return null;
    }

    private void ClearRouteInteractionState(bool clearSelection)
    {
        bool hadHover = !string.IsNullOrEmpty(hoveredRouteId);
        hoveredRouteId = null;
        routeIdsByCell.Clear();
        HideRouteTooltip();

        if (clearSelection)
            selectedRouteIds.Clear();

        if (debugLogs && hadHover)
            Debug.Log("[EvacuationController] Evacuation route hover cleared.");
    }

    private void RedrawVisibleRoutes()
    {
        if (!previewModeActive || routeVisualizer == null)
            return;

        routeVisualizer.DrawRoutes(_currentRoutes, hoveredRouteId, selectedRouteIds);
    }

    public float GetEvacuationMitigationForZone(string geoid)
    {
        return 0f;
    }

    public int GetEvacuatedPopulationForZone(string geoid)
    {
        return 0;
    }

    private void ResolveReferences()
    {
        if (baselineRiskController == null)
            baselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (shelterManager == null)
            shelterManager = FindFirstObjectByType<ShelterManager>();

        if (floodDefenseBoxStamp == null)
            floodDefenseBoxStamp = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (routeTilemap == null && floodDefenseBoxStamp != null)
            routeTilemap = floodDefenseBoxStamp.TerrainTilemap;

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
        if (routeVisualizer == null)
            return;

        routeVisualizer.SetTilemap(routeTilemap);
        routeVisualizer.SetDebugLogs(debugLogs);
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
        if (_isSubscribedToShelters || shelterManager == null)
            return;

        shelterManager.OnSheltersChanged += OnSheltersChanged;
        _isSubscribedToShelters = true;
    }

    private void UnsubscribeFromShelterChanges()
    {
        if (!_isSubscribedToShelters || shelterManager == null)
            return;

        shelterManager.OnSheltersChanged -= OnSheltersChanged;
        _isSubscribedToShelters = false;
    }

    private void OnSheltersChanged()
    {
        if (previewModeActive)
            RefreshSingleRoute();
    }

    private bool ValidateRouteReferences()
    {
        bool isValid = true;

        if (shelterManager == null)
        {
            Debug.LogWarning("[EvacuationController] ShelterManager is not assigned.");
            isValid = false;
        }

        if (floodDefenseBoxStamp == null)
        {
            Debug.LogWarning("[EvacuationController] FloodDefenseBoxStamp is not assigned.");
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

    private void BuildRoutesForAllActiveShelters(List<PlacedShelterData> activeShelters)
    {
        if (activeShelters == null)
            return;

        for (int i = 0; i < activeShelters.Count; i++)
        {
            PlacedShelterData shelter = activeShelters[i];

            if (shelter == null || !shelter.isActive)
                continue;

            if (!TryGetShelterNormalizedTile(shelter, out Vector2Int shelterTile))
            {
                Debug.LogWarning($"[EvacuationController] Could not build routes for shelter {shelter.shelterId} because its tile cell is invalid.");
                continue;
            }

            int associatedZoneCount = shelter.associatedZoneGeoids != null ? shelter.associatedZoneGeoids.Count : 0;
            int routeCountBeforeShelter = _currentRoutes.Count;

            if (debugLogs)
                Debug.Log($"[EvacuationController] Shelter {shelter.shelterId} serves {associatedZoneCount} zones.");

            BuildRoutesForShelter(shelter, shelterTile);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Routes built for shelter {shelter.shelterId}: {_currentRoutes.Count - routeCountBeforeShelter}.");
        }
    }

    private void BuildRoutesForShelter(PlacedShelterData shelter, Vector2Int shelterTile)
    {
        if (shelter == null)
            return;

        if (shelter.associatedZoneGeoids == null || shelter.associatedZoneGeoids.Count == 0)
        {
            Debug.LogWarning($"[EvacuationController] Shelter {shelter.shelterId} has no associated zones.");
            return;
        }

        HashSet<string> processedGeoids = new();
        Vector3Int shelterCell = NormalizedTileToCell(shelterTile);

        for (int i = 0; i < shelter.associatedZoneGeoids.Count; i++)
        {
            string geoid = NormalizeGeoid(shelter.associatedZoneGeoids[i]);

            if (string.IsNullOrEmpty(geoid) || !processedGeoids.Add(geoid))
                continue;

            if (debugLogs)
                Debug.Log($"[EvacuationController] Building route from zone {geoid} to shelter {shelter.shelterId}.");

            if (!TryFindZoneCenterAnchor(geoid, out ZoneAnchor anchor))
            {
                Debug.LogWarning($"[EvacuationController] Could not build route. Zone={geoid}, Shelter={shelter.shelterId}.");
                continue;
            }

            if (debugLogs)
                Debug.Log($"[EvacuationController] Zone {geoid} anchor cell selected: {anchor.cell}. Tile preference: {anchor.preferenceLabel}.");

            if (!TryBuildPath(anchor.normalizedTile, shelterTile, out List<Vector3Int> pathCells, out float distanceTiles))
            {
                Debug.LogWarning($"[EvacuationController] Could not build route. Zone={geoid}, Shelter={shelter.shelterId}.");
                continue;
            }

            string destinationShelterId = GetSafeShelterId(shelter);

            SimpleEvacuationRoute route = new()
            {
                routeId = BuildRouteId(geoid, destinationShelterId),
                sourceZoneGeoid = geoid,
                sourceZonePopulation = GetSourceZonePopulation(geoid),
                destinationShelterId = destinationShelterId,
                destinationShelterType = GetSafeShelterType(shelter),
                destinationShelterCapacity = GetSafeShelterCapacity(shelter),
                startCell = anchor.cell,
                shelterCell = shelterCell,
                pathCells = pathCells,
                distanceTiles = distanceTiles,
                isValid = pathCells.Count > 0,
            };

            _currentRoutes.Add(route);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Route built. Zone={geoid}, Shelter={shelter.shelterId}, Cells={pathCells.Count}, Distance={distanceTiles:0.##}.");
        }
    }

    private bool TryFindZoneCenterAnchor(string geoid, out ZoneAnchor anchor)
    {
        anchor = default;

        if (!floodDefenseBoxStamp.TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles) || zoneTiles == null || zoneTiles.Count == 0)
            return false;

        Vector2 center = CalculateZoneCenter(zoneTiles);
        bool hasCandidate = false;
        Vector2Int bestTile = default;
        int bestPriority = int.MaxValue;
        float bestDistance = float.MaxValue;
        string bestLabel = "other";

        foreach (Vector2Int tile in zoneTiles)
        {
            if (!TryGetAnchorTilePreference(tile, out string label, out int priority))
                continue;

            float distance = (new Vector2(tile.x, tile.y) - center).sqrMagnitude;

            if (hasCandidate && (distance > bestDistance || (Mathf.Approximately(distance, bestDistance) && priority >= bestPriority)))
                continue;

            bestTile = tile;
            bestPriority = priority;
            bestDistance = distance;
            bestLabel = label;
            hasCandidate = true;
        }

        if (!hasCandidate)
            return false;

        anchor = new ZoneAnchor
        {
            normalizedTile = bestTile,
            cell = NormalizedTileToCell(bestTile),
            preferenceLabel = bestLabel,
        };
        return true;
    }

    private Vector2 CalculateZoneCenter(HashSet<Vector2Int> zoneTiles)
    {
        Vector2 center = Vector2.zero;
        int count = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            center += new Vector2(tile.x, tile.y);
            count++;
        }

        return count > 0 ? center / count : center;
    }

    private bool TryGetAnchorTilePreference(Vector2Int normalizedTile, out string preferenceLabel, out int priority)
    {
        preferenceLabel = "other";
        priority = int.MaxValue;

        if (!TryGetRouteTileSnapshot(normalizedTile, out RouteTileSnapshot snapshot) || !IsPassable(snapshot))
            return false;

        if (IsRoadCategory(snapshot.category))
        {
            preferenceLabel = "road";
            priority = 0;
            return true;
        }

        if (IsBuildingCategory(snapshot.category))
        {
            preferenceLabel = "building";
            priority = 1;
            return true;
        }

        if (IsLandCategory(snapshot.category))
        {
            preferenceLabel = "land";
            priority = 2;
            return true;
        }

        preferenceLabel = "other";
        priority = 3;
        return true;
    }

    private bool TryGetShelterNormalizedTile(PlacedShelterData shelter, out Vector2Int normalizedTile)
    {
        normalizedTile = default;

        if (shelter == null)
            return false;

        if (TryCellToNormalizedTile(shelter.tileCell, out normalizedTile))
            return true;

        if (routeTilemap == null)
            return false;

        Vector3Int fallbackCell = routeTilemap.WorldToCell(shelter.worldPosition);
        return TryCellToNormalizedTile(fallbackCell, out normalizedTile);
    }

    private bool TryBuildPath(
        Vector2Int startTile,
        Vector2Int destinationTile,
        out List<Vector3Int> pathCells,
        out float distanceTiles)
    {
        pathCells = new List<Vector3Int>();
        distanceTiles = 0f;

        if (!IsWithinMap(startTile) || !IsWithinMap(destinationTile))
            return false;

        if (!TryGetMovementCost(startTile, out _) || !TryGetMovementCost(destinationTile, out _))
            return false;

        List<Vector2Int> openSet = new() { startTile };
        HashSet<Vector2Int> closedSet = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, float> gScore = new()
        {
            [startTile] = 0f,
        };
        Dictionary<Vector2Int, float> fScore = new()
        {
            [startTile] = GetHeuristicCost(startTile, destinationTile),
        };

        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxPathSearchIterations)
        {
            iterations++;
            Vector2Int current = RemoveLowestScoreTile(openSet, fScore);

            if (current == destinationTile)
            {
                List<Vector2Int> normalizedPath = ReconstructPath(cameFrom, current);
                pathCells = ConvertNormalizedPathToCells(normalizedPath);
                distanceTiles = Mathf.Max(0, pathCells.Count - 1);
                return pathCells.Count > 0;
            }

            closedSet.Add(current);

            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                Vector2Int neighbor = current + NeighborOffsets[i];

                if (!IsWithinMap(neighbor) || closedSet.Contains(neighbor))
                    continue;

                if (!TryGetMovementCost(neighbor, out float movementCost))
                    continue;

                float tentativeGScore = gScore[current] + movementCost;

                if (gScore.TryGetValue(neighbor, out float existingScore) && tentativeGScore >= existingScore)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = tentativeGScore + GetHeuristicCost(neighbor, destinationTile);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        if (debugLogs && iterations >= maxPathSearchIterations)
            Debug.LogWarning($"[EvacuationController] Path search stopped at max iterations={maxPathSearchIterations}.");

        return false;
    }

    private bool TryGetMovementCost(Vector2Int normalizedTile, out float movementCost)
    {
        movementCost = landTileCost;

        if (!TryGetRouteTileSnapshot(normalizedTile, out RouteTileSnapshot snapshot) || !IsPassable(snapshot))
            return false;

        movementCost = IsRoadCategory(snapshot.category) ? roadTileCost : landTileCost;
        return true;
    }

    private bool TryGetRouteTileSnapshot(Vector2Int normalizedTile, out RouteTileSnapshot snapshot)
    {
        snapshot = default;

        if (!IsWithinMap(normalizedTile))
            return false;

        TileInstance tile = TryGetTileInstance(normalizedTile);
        Vector3Int cell = NormalizedTileToCell(normalizedTile);
        bool hasRouteTile = routeTilemap == null || routeTilemap.HasTile(cell);

        if (tile == null && !hasRouteTile)
            return false;

        string category = NormalizeCategory(tile != null ? tile.category : null);
        float waterDepth = tile != null ? Mathf.Max(0f, tile.waterHeight) : 0f;

        if (TryGetWaterDepthFromGrid(normalizedTile, out float gridWaterDepth))
            waterDepth = Mathf.Max(waterDepth, gridWaterDepth);

        snapshot = new RouteTileSnapshot
        {
            category = category,
            isWater = IsWaterCategory(category) || (tile != null && tile.tileType != null && tile.tileType.isWater),
            isDeepFlooded = waterDepth > deepFloodWaterThreshold,
        };

        return true;
    }

    private bool IsPassable(RouteTileSnapshot snapshot)
    {
        return !snapshot.isWater && !snapshot.isDeepFlooded;
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new() { current };

        while (cameFrom.TryGetValue(current, out Vector2Int previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private List<Vector3Int> ConvertNormalizedPathToCells(List<Vector2Int> normalizedPath)
    {
        List<Vector3Int> cells = new();

        if (normalizedPath == null)
            return cells;

        for (int i = 0; i < normalizedPath.Count; i++)
            cells.Add(NormalizedTileToCell(normalizedPath[i]));

        return cells;
    }

    private Vector3Int NormalizedTileToCell(Vector2Int normalizedTile)
    {
        return floodDefenseBoxStamp != null
            ? floodDefenseBoxStamp.NormalizedTileToCell(normalizedTile)
            : new Vector3Int(normalizedTile.x, normalizedTile.y, 0);
    }

    private bool TryCellToNormalizedTile(Vector3Int cell, out Vector2Int normalizedTile)
    {
        Vector2Int origin = floodDefenseBoxStamp != null ? floodDefenseBoxStamp.TileOrigin : Vector2Int.zero;
        normalizedTile = new Vector2Int(cell.x - origin.x, cell.y - origin.y);
        return IsWithinMap(normalizedTile);
    }

    private TileInstance TryGetTileInstance(Vector2Int normalizedTile)
    {
        if (tileMapData == null || !IsWithinMap(normalizedTile))
            return null;

        try
        {
            return tileMapData.Get(normalizedTile);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private bool TryGetWaterDepthFromGrid(Vector2Int normalizedTile, out float waterDepth)
    {
        waterDepth = 0f;

        if (tileMapData == null || tileMapData.water == null)
            return false;

        if (normalizedTile.x < 0 || normalizedTile.y < 0 ||
            normalizedTile.x >= tileMapData.water.GetLength(0) ||
            normalizedTile.y >= tileMapData.water.GetLength(1))
        {
            return false;
        }

        waterDepth = Mathf.Max(0f, tileMapData.water[normalizedTile.x, normalizedTile.y]);
        return true;
    }

    private Vector2Int RemoveLowestScoreTile(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
    {
        int bestIndex = 0;
        float bestScore = GetScore(openSet[0], fScore);

        for (int i = 1; i < openSet.Count; i++)
        {
            float score = GetScore(openSet[i], fScore);

            if (score >= bestScore)
                continue;

            bestIndex = i;
            bestScore = score;
        }

        Vector2Int result = openSet[bestIndex];
        openSet.RemoveAt(bestIndex);
        return result;
    }

    private float GetHeuristicCost(Vector2Int a, Vector2Int b)
    {
        return ManhattanDistance(a, b) * Mathf.Min(roadTileCost, landTileCost);
    }

    private static float GetScore(Vector2Int tile, Dictionary<Vector2Int, float> scores)
    {
        return scores.TryGetValue(tile, out float score) ? score : float.MaxValue;
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool IsWithinMap(Vector2Int tile)
    {
        if (tileMapData == null)
            return false;

        int width = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeX;
        int height = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeY;

        return tile.x >= 0 && tile.y >= 0 && tile.x < width && tile.y < height;
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
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid) || baselineRiskController == null)
            return 0;

        if (!baselineRiskController.HasCalculatedBaselineRisk)
            baselineRiskController.EnsureBaselineRiskCalculated();

        return baselineRiskController.TryGetRiskData(geoid, out ZoneBaselineRiskData riskData)
            ? Mathf.Max(0, riskData.rawPopulation)
            : 0;
    }

    private static string GetSafeShelterId(PlacedShelterData shelter)
    {
        return shelter != null && !string.IsNullOrWhiteSpace(shelter.shelterId)
            ? shelter.shelterId.Trim()
            : "Unknown Shelter";
    }

    private static string GetSafeShelterType(PlacedShelterData shelter)
    {
        return shelter != null && !string.IsNullOrWhiteSpace(shelter.shelterTypeName)
            ? shelter.shelterTypeName.Trim()
            : "Shelter";
    }

    private static int GetSafeShelterCapacity(PlacedShelterData shelter)
    {
        return shelter != null ? Mathf.Max(0, shelter.capacity) : 0;
    }

    private string BuildRouteId(string geoid, string shelterId)
    {
        return $"{NormalizeGeoid(geoid)}_to_{NormalizeGeoid(shelterId)}";
    }

    private static bool IsRoadCategory(string category)
    {
        return category.Contains("road") ||
               category.Contains("street") ||
               category.Contains("highway");
    }

    private static bool IsBuildingCategory(string category)
    {
        return category.Contains("building") ||
               category.Contains("residential") ||
               category.Contains("commercial") ||
               category.Contains("industrial") ||
               category.Contains("city") ||
               category.Contains("shelter");
    }

    private static bool IsLandCategory(string category)
    {
        return category.Contains("land") ||
               category.Contains("grass") ||
               category.Contains("field") ||
               category.Contains("park") ||
               category.Contains("forest") ||
               category.Contains("soil") ||
               category.Contains("terrain");
    }

    private static bool IsWaterCategory(string category)
    {
        return category.Contains("water") ||
               category.Contains("river") ||
               category.Contains("stream") ||
               category.Contains("lake") ||
               category.Contains("ocean") ||
               category.Contains("flood_source");
    }

    private static string NormalizeCategory(string category)
    {
        return string.IsNullOrWhiteSpace(category)
            ? string.Empty
            : category.Trim().ToLowerInvariant();
    }

    private static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid)
            ? string.Empty
            : geoid.Trim();
    }
}
