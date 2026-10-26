using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns evacuation route state; the facade controls transition and publication timing.</summary>
internal sealed class EvacuationState
{
    private readonly List<SimpleEvacuationRoute> _availableRoutes = new();
    private readonly List<SimpleEvacuationRoute> _visibleRoutes = new();
    private readonly Dictionary<Vector3Int, List<string>> routeIdsByCell = new();
    private readonly HashSet<string> selectedRouteIds = new(StringComparer.Ordinal);
    private string hoveredRouteId;
    private bool hasConfirmedEvacuationRoute;
    private string confirmedRouteId;

    internal List<SimpleEvacuationRoute> AvailableRoutes => _availableRoutes;
    internal List<SimpleEvacuationRoute> VisibleRoutes => _visibleRoutes;
    internal HashSet<string> SelectedRouteIds => selectedRouteIds;
    internal int RouteCellCount => routeIdsByCell.Count;
    internal string HoveredRouteId => hoveredRouteId;
    internal bool HasConfirmedEvacuationRoute => hasConfirmedEvacuationRoute;
    internal string ConfirmedRouteId => confirmedRouteId;
    internal SimpleEvacuationRoute CurrentRoute => _availableRoutes.Count > 0 ? _availableRoutes[0] : null;

    internal void ClearAvailableRoutes()
    {
        _availableRoutes.Clear();
    }

    internal void ClearVisibleRoutes()
    {
        _visibleRoutes.Clear();
    }

    internal void AddVisibleRoute(SimpleEvacuationRoute route)
    {
        _visibleRoutes.Add(route);
    }

    internal void ClearVisibleRouteState()
    {
        hoveredRouteId = null;
        _visibleRoutes.Clear();
        routeIdsByCell.Clear();
    }

    internal void SetHoveredRouteId(string routeId)
    {
        hoveredRouteId = routeId;
    }

    // Mutation arguments are already normalized by the facade. Read-only route
    // styling/membership probes must keep the original exact-ID comparisons.
    internal bool ContainsSelectedRouteId(string routeId)
    {
        return selectedRouteIds.Contains(routeId);
    }

    internal void AddSelectedRoute(string routeId)
    {
        selectedRouteIds.Add(routeId);
    }

    internal void RemoveSelectedRoute(string routeId)
    {
        selectedRouteIds.Remove(routeId);
    }

    internal void ClearSelectedRoutes()
    {
        selectedRouteIds.Clear();
    }

    internal void StoreConfirmation(string routeId)
    {
        confirmedRouteId = routeId;
        hasConfirmedEvacuationRoute = true;
    }

    internal void ClearConfirmation()
    {
        confirmedRouteId = null;
        hasConfirmedEvacuationRoute = false;
    }

    internal List<string> GetSelectedRouteIdsExcept(string routeIdToKeep)
    {
        List<string> routesToDeselect = new();

        foreach (string selectedRouteId in selectedRouteIds)
        {
            if (selectedRouteId != routeIdToKeep)
                routesToDeselect.Add(selectedRouteId);
        }

        return routesToDeselect;
    }

    internal IReadOnlyList<SimpleEvacuationRoute> GetSelectedRoutes()
    {
        List<SimpleEvacuationRoute> routes = new();

        for (int i = 0; i < _availableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = _availableRoutes[i];

            if (route == null ||
                !route.isValid ||
                string.IsNullOrEmpty(route.routeId) ||
                !selectedRouteIds.Contains(route.routeId))
            {
                continue;
            }

            routes.Add(route);
        }

        return routes;
    }

    internal bool IsRouteSelected(string routeId)
    {
        routeId = NormalizeGeoid(routeId);
        return !string.IsNullOrEmpty(routeId) && selectedRouteIds.Contains(routeId);
    }

    internal bool HasEvacuationRouteForZone(string geoid)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return false;

        for (int i = 0; i < _availableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = _availableRoutes[i];

            if (route == null || !route.isValid)
                continue;

            if (string.Equals(NormalizeGeoid(route.sourceZoneGeoid), geoid, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    internal bool HasSelectedEvacuationRouteForZone(string geoid)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return false;

        for (int i = 0; i < _availableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = _availableRoutes[i];

            if (route == null ||
                !route.isValid ||
                string.IsNullOrEmpty(route.routeId) ||
                !selectedRouteIds.Contains(route.routeId))
            {
                continue;
            }

            if (string.Equals(NormalizeGeoid(route.sourceZoneGeoid), geoid, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    internal string GetHoverRouteIdForCell(Vector3Int cell)
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

    internal void BuildRouteCellLookup()
    {
        routeIdsByCell.Clear();

        for (int i = 0; i < _visibleRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = _visibleRoutes[i];

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

    internal SimpleEvacuationRoute GetRouteById(string routeId)
    {
        routeId = NormalizeGeoid(routeId);

        if (string.IsNullOrEmpty(routeId))
            return null;

        for (int i = 0; i < _availableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = _availableRoutes[i];

            if (route != null && route.routeId == routeId)
                return route;
        }

        return null;
    }

    internal void RemoveInvalidSelectedRouteIds(bool debugLogs)
    {
        if (selectedRouteIds.Count == 0)
            return;

        HashSet<string> validRouteIds = new(StringComparer.Ordinal);

        for (int i = 0; i < _availableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = _availableRoutes[i];

            if (route == null || !route.isValid || string.IsNullOrEmpty(route.routeId))
                continue;

            validRouteIds.Add(route.routeId);
        }

        List<string> staleRouteIds = new();

        foreach (string selectedRouteId in selectedRouteIds)
        {
            if (string.IsNullOrEmpty(selectedRouteId) || !validRouteIds.Contains(selectedRouteId))
            {
                staleRouteIds.Add(selectedRouteId);
                continue;
            }

            if (debugLogs)
                Debug.Log($"[EvacuationController] Preserved selected route: {selectedRouteId}.");
        }

        for (int i = 0; i < staleRouteIds.Count; i++)
        {
            string staleRouteId = staleRouteIds[i];
            selectedRouteIds.Remove(staleRouteId);

            if (string.Equals(staleRouteId, confirmedRouteId, StringComparison.Ordinal))
                ClearConfirmedRouteSelection(debugLogs);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Removed stale selected route because route no longer exists: {staleRouteId}.");
        }
    }

    private void ClearConfirmedRouteSelection(bool debugLogs)
    {
        if (!hasConfirmedEvacuationRoute)
            return;

        if (debugLogs)
            Debug.Log($"[EvacuationController] Cleared confirmed evacuation route because it is no longer available: {confirmedRouteId}.");

        confirmedRouteId = null;
        hasConfirmedEvacuationRoute = false;
    }

    internal static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid)
            ? string.Empty
            : geoid.Trim();
    }
}
