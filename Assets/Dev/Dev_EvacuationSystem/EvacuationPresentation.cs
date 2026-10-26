using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Coordinates existing route visuals/tooltips without owning state or discovering components.</summary>
internal sealed class EvacuationPresentation
{
    internal void EnsureVisualizerConfigured(
        ref EvacuationRouteVisualizer routeVisualizer,
        Tilemap routeTilemap,
        bool debugLogs)
    {
        if (routeVisualizer == null)
            return;

        routeVisualizer.SetTilemap(routeTilemap);
        routeVisualizer.SetDebugLogs(debugLogs);
    }

    internal void ClearVisibleRouteVisuals(
        EvacuationState state,
        ref EvacuationRouteVisualizer routeVisualizer,
        ref MapHoverTooltipController mapHoverTooltip,
        bool debugLogs)
    {
        bool hadHover = !string.IsNullOrEmpty(state.HoveredRouteId);
        state.ClearVisibleRouteState();
        HideRouteTooltip(mapHoverTooltip, debugLogs);

        if (routeVisualizer != null)
            routeVisualizer.ClearRoute();

        if (debugLogs && hadHover)
            Debug.Log("[EvacuationController] Evacuation route hover cleared.");
    }

    internal void RedrawVisibleRoutes(
        EvacuationState state,
        bool previewModeActive,
        ref EvacuationRouteVisualizer routeVisualizer)
    {
        if (!previewModeActive || routeVisualizer == null)
            return;

        state.ClearVisibleRoutes();
        List<SimpleEvacuationRoute> routesToDraw = new();

        for (int i = 0; i < state.AvailableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = state.AvailableRoutes[i];

            if (route != null && route.isValid)
                routesToDraw.Add(route);

            if (ShouldDisplayRouteVisual(state, route, ref routeVisualizer))
                state.AddVisibleRoute(route);
        }

        state.BuildRouteCellLookup();
        routeVisualizer.DrawRoutes(routesToDraw, state.HoveredRouteId, state.SelectedRouteIds);
    }

    private bool ShouldDisplayRouteVisual(
        EvacuationState state,
        SimpleEvacuationRoute route,
        ref EvacuationRouteVisualizer routeVisualizer)
    {
        if (route == null || !route.isValid)
            return false;

        if (routeVisualizer == null ||
            !routeVisualizer.HideUnselectedRoutesWhenSelected ||
            state.SelectedRouteIds.Count == 0)
        {
            return true;
        }

        return !string.IsNullOrEmpty(route.routeId) && state.ContainsSelectedRouteId(route.routeId);
    }

    internal void UpdateRouteTooltipPosition(
        EvacuationState state,
        MapHoverTooltipController mapHoverTooltip,
        Vector2 screenPosition,
        bool debugLogs)
    {
        if (mapHoverTooltip == null || !mapHoverTooltip.IsEvacRouteTooltipVisible)
            return;

        SimpleEvacuationRoute route = state.GetRouteById(state.HoveredRouteId);

        if (route == null)
        {
            HideRouteTooltip(mapHoverTooltip, debugLogs);
            return;
        }

        mapHoverTooltip.ShowEvacRouteTooltip(route, screenPosition);
    }

    internal void HideRouteTooltip(MapHoverTooltipController mapHoverTooltip, bool debugLogs)
    {
        if (mapHoverTooltip == null || !mapHoverTooltip.IsEvacRouteTooltipVisible)
            return;

        mapHoverTooltip.HideEvacRouteTooltip();

        if (debugLogs)
            Debug.Log("[EvacuationController] Hiding evacuation route tooltip.");
    }

    internal void ShowRouteTooltip(
        MapHoverTooltipController mapHoverTooltip,
        SimpleEvacuationRoute route,
        Vector2 screenPosition,
        bool debugLogs)
    {
        mapHoverTooltip.ShowEvacRouteTooltip(route, screenPosition);

        if (debugLogs)
        {
            Debug.Log(
                $"[EvacuationController] Showing route tooltip for zone {route.sourceZoneGeoid} to shelter {route.destinationShelterId}.");
        }
    }

    internal void ShowOnlySelectedRoute(EvacuationRouteVisualizer routeVisualizer, string routeId)
    {
        routeVisualizer?.ShowOnlySelectedRoute(routeId);
    }

    internal void ClearRouteSelectionAndShowAll(EvacuationRouteVisualizer routeVisualizer)
    {
        routeVisualizer?.ClearRouteSelectionAndShowAll();
    }
}
