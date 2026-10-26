using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Interprets legacy mouse input using borrowed state and synchronous facade actions.</summary>
internal sealed class EvacuationInputController
{
    internal bool ShouldClearHover(EvacuationState state)
    {
        return state.VisibleRoutes.Count == 0 || state.RouteCellCount == 0;
    }

    internal bool TryGetHoverChange(EvacuationState state, string routeId, out string normalizedRouteId)
    {
        normalizedRouteId = EvacuationState.NormalizeGeoid(routeId);

        if (string.IsNullOrEmpty(normalizedRouteId))
            normalizedRouteId = null;

        return state.HoveredRouteId != normalizedRouteId;
    }

    internal void ProcessInput(
        EvacuationState state,
        Tilemap routeTilemap,
        Camera mainCamera,
        Action<string, Vector2> setHoveredRoute,
        Action<Vector2> updateRouteTooltipPosition,
        Func<string, bool> toggleRouteSelection)
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 mouseScreen = new(Input.mousePosition.x, Input.mousePosition.y);

        Vector3Int hoveredCell = routeTilemap.WorldToCell(mouseWorld);
        string routeId = state.GetHoverRouteIdForCell(hoveredCell);
        setHoveredRoute(routeId, mouseScreen);

        if (!string.IsNullOrEmpty(state.HoveredRouteId))
            updateRouteTooltipPosition(mouseScreen);

        // Sample the click after hover/tooltip callbacks, as in the facade's
        // original loop. Re-entrant listeners can change input or route state.
        if (!string.IsNullOrEmpty(routeId) && Input.GetMouseButtonDown(0))
            toggleRouteSelection(routeId);
    }
}
