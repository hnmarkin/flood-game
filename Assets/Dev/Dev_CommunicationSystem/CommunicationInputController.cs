using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Legacy pointer interpretation, polled only by the public controller.</summary>
internal sealed class CommunicationInputController
{
    internal readonly struct PointerSample
    {
        internal readonly CommunicationTowerData tower;
        internal readonly Vector2 screenPosition;

        internal PointerSample(CommunicationTowerData tower, Vector2 screenPosition)
        {
            this.tower = tower;
            this.screenPosition = screenPosition;
        }
    }

    internal PointerSample SamplePointer(
        Camera mainCamera, Tilemap communicationTilemap, CommunicationState state)
    {
        Vector2 mouseScreen = new(Input.mousePosition.x, Input.mousePosition.y);
        TryGetTowerUnderMouse(mainCamera, communicationTilemap, state, out CommunicationTowerData tower);
        return new PointerSample(tower, mouseScreen);
    }

    // Poll after hover presentation, retaining the original short-circuit when
    // no tower is hovered. Do not sample this button with the pointer above.
    internal bool IsTowerClickRequested(CommunicationTowerData hoveredTower)
    {
        return hoveredTower != null && Input.GetMouseButtonDown(0);
    }

    private bool TryGetTowerUnderMouse(
        Camera mainCamera, Tilemap communicationTilemap, CommunicationState state,
        out CommunicationTowerData tower)
    {
        tower = null;

        if (mainCamera == null || communicationTilemap == null)
            return false;

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        Vector3Int cell = communicationTilemap.WorldToCell(world);
        return state.TryGetTowerAtCell(cell, out tower) && tower != null;
    }
}
