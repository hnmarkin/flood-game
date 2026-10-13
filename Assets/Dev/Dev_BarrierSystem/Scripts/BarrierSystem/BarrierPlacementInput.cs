using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BarrierPlacementInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    public bool EscapePressed => Input.GetKeyDown(KeyCode.Escape);
    public bool CommitPressed => Input.GetMouseButtonDown(0);

    private void Awake()
    {
        ResolveCamera();
    }

    public void Configure(Camera camera)
    {
        mainCamera = camera != null ? camera : mainCamera;
        ResolveCamera();
    }

    public bool TryGetHoveredZone(
        ZoneController zoneController,
        out string geoid,
        out HashSet<Vector2Int> zoneTiles)
    {
        geoid = null;
        zoneTiles = null;

        ResolveCamera();

        if (mainCamera == null || zoneController == null)
            return false;

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        return zoneController.TryGetZoneTilesAtWorld(world, out geoid, out zoneTiles);
    }

    private void ResolveCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
}
