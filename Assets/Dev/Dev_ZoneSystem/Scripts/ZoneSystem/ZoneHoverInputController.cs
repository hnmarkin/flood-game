using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ZoneHoverInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ZoneController zoneController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ZoneOutlineVisibilityController visibilityPolicy;

    [Header("Hover")]
    [SerializeField] private Color lineColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private bool outliningEnabled = true;

    private string _currentGeoid;
    private HashSet<Vector2Int> _currentZoneTiles;

    internal bool IsHoverInputAvailable =>
        isActiveAndEnabled && outliningEnabled && mainCamera != null;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    // The policy owner calls once at the original point in its Update. Keeping
    // one coordinated tick preserves barrier hide/show timing and policy disable.
    internal void ProcessHover(
        ZoneOutlineVisibilityController source,
        ZoneController resolvedController,
        bool suppressed)
    {
        if (source == null || source != visibilityPolicy ||
            !source.isActiveAndEnabled || !isActiveAndEnabled)
            return;

        // Adopt a controller supplied by the policy when our reference is missing,
        // without scene searches or renderer initialization.
        if (zoneController == null)
            zoneController = resolvedController;

        if (suppressed || !outliningEnabled || mainCamera == null ||
            zoneController == null || zoneController != resolvedController ||
            !zoneController.IsOutlineRenderingAvailable ||
            !zoneController.IsZoneLoaderReady())
        {
            ClearOutline();
            return;
        }

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        string geoid;
        HashSet<Vector2Int> zoneTiles;

        bool hit = zoneController.TryGetZoneTilesAtWorld(
            world,
            out geoid,
            out zoneTiles
        );

        if (!hit || string.IsNullOrEmpty(geoid) || zoneTiles == null)
        {
            ClearOutline();
            return;
        }

        if (_currentGeoid == geoid && _currentZoneTiles == zoneTiles)
            return;

        if (!zoneController.SetZoneOutline(ZoneOutlineChannel.Hover, geoid, lineColor))
        {
            ClearOutline();
            return;
        }

        // Commit only a successful draw so initialization failures can retry.
        _currentGeoid = geoid;
        _currentZoneTiles = zoneTiles;
    }

    internal void InvalidateHoverCache()
    {
        _currentGeoid = null;
        _currentZoneTiles = null;
    }

    private void ClearOutline()
    {
        InvalidateHoverCache();

        if (zoneController != null && zoneController.IsOutlineRenderingAvailable)
            zoneController.ClearOutlines(ZoneOutlineChannel.Hover);
    }

    private void OnDisable()
    {
        ClearOutline();
    }

    private void OnDestroy()
    {
        ClearOutline();
    }
}
