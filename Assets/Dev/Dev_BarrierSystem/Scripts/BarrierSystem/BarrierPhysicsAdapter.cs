using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum BarrierPhysicsRegistrationStatus
{
    Success,
    Rejected,
    PartialCommit
}

public readonly struct BarrierPhysicsRegistrationResult
{
    public BarrierPhysicsRegistrationResult(
        BarrierPhysicsRegistrationStatus status,
        int registeredEdges,
        int protectedExteriorEdges,
        int totalEdges,
        string message)
    {
        Status = status;
        RegisteredEdges = registeredEdges;
        ProtectedExteriorEdges = protectedExteriorEdges;
        TotalEdges = totalEdges;
        Message = message;
    }

    public BarrierPhysicsRegistrationStatus Status { get; }
    public int RegisteredEdges { get; }
    public int ProtectedExteriorEdges { get; }
    public int TotalEdges { get; }
    public string Message { get; }
    public bool Succeeded => Status == BarrierPhysicsRegistrationStatus.Success;
}

[DisallowMultipleComponent]
public sealed class BarrierPhysicsAdapter : MonoBehaviour
{
    [SerializeField] private WaterController waterController;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private bool debugLogs = true;

    private readonly List<BarrierWriteRequest> _requests = new List<BarrierWriteRequest>();
    private int _protectedExteriorEdges;

    public WaterController WaterController => waterController;

    private void Awake()
    {
        ResolveWaterController();
    }

    public void Configure(
        WaterController controller,
        Tilemap terrain,
        bool enableDebugLogs)
    {
        waterController = controller != null ? controller : waterController;
        terrainTilemap = terrain != null ? terrain : terrainTilemap;
        debugLogs = enableDebugLogs;
        ResolveWaterController();
    }

    public BarrierPhysicsRegistrationResult TryRegisterZoneBoundary(
        string geoid,
        IReadOnlyList<ZoneBoundaryEdge> boundaryEdges,
        ZoneController zoneController,
        float barrierHeight,
        float seepage)
    {
        if (!TryBuildValidatedRequests(
                geoid,
                boundaryEdges,
                zoneController,
                barrierHeight,
                seepage,
                out string validationError))
        {
            return new BarrierPhysicsRegistrationResult(
                BarrierPhysicsRegistrationStatus.Rejected,
                0,
                _protectedExteriorEdges,
                boundaryEdges != null ? boundaryEdges.Count : 0,
                validationError);
        }

        int registered = 0;

        foreach (BarrierWriteRequest request in _requests)
        {
            bool ok = request.Orientation == ZoneBoundaryEdgeOrientation.X
                ? waterController.TrySetBarrierX(request.WaterTileCell, barrierHeight, seepage)
                : waterController.TrySetBarrierY(request.WaterTileCell, barrierHeight, seepage);

            if (!ok)
            {
                BarrierPhysicsRegistrationStatus status = registered > 0
                    ? BarrierPhysicsRegistrationStatus.PartialCommit
                    : BarrierPhysicsRegistrationStatus.Rejected;

                string message = registered > 0
                    ? $"WaterController rejected edge {registered + 1}/{_requests.Count} after {registered} edge(s) were already registered. WaterController has no rollback API."
                    : $"WaterController rejected the first barrier edge for zone {geoid}.";

                return new BarrierPhysicsRegistrationResult(
                    status,
                    registered,
                    _protectedExteriorEdges,
                    boundaryEdges.Count,
                    message);
            }

            registered++;
        }

        if (debugLogs)
        {
            Debug.Log($"[BarrierPhysicsAdapter] Zone={geoid}. Registered {registered} interior WaterController barrier edge(s); " +
                      $"treated {_protectedExteriorEdges} exterior map edge(s) as protected by the simulation boundary.");
        }

        return new BarrierPhysicsRegistrationResult(
            BarrierPhysicsRegistrationStatus.Success,
            registered,
            _protectedExteriorEdges,
            boundaryEdges.Count,
            $"Registered {registered} interior edge(s); exterior protected edge(s): {_protectedExteriorEdges}.");
    }

    private bool TryBuildValidatedRequests(
        string geoid,
        IReadOnlyList<ZoneBoundaryEdge> boundaryEdges,
        ZoneController zoneController,
        float barrierHeight,
        float seepage,
        out string error)
    {
        error = null;
        _requests.Clear();
        _protectedExteriorEdges = 0;

        ResolveWaterController();

        if (string.IsNullOrEmpty(geoid))
        {
            error = "GEOID is empty.";
            return false;
        }

        if (waterController == null)
        {
            error = "WaterController is not assigned.";
            return false;
        }

        if (!waterController.IsInitialized)
        {
            error = "WaterController is not initialized.";
            return false;
        }

        if (zoneController == null)
        {
            error = "ZoneController is not assigned.";
            return false;
        }

        if (boundaryEdges == null || boundaryEdges.Count == 0)
        {
            error = "Zone boundary has no edges.";
            return false;
        }

        if (!IsFinitePositive(barrierHeight) || !IsFiniteNonNegative(seepage))
        {
            error = "Barrier height must be positive and seepage must be nonnegative.";
            return false;
        }

        Tilemap map = terrainTilemap != null ? terrainTilemap : zoneController.TerrainTilemap;
        if (map == null)
        {
            error = "Terrain Tilemap is not assigned.";
            return false;
        }

        foreach (ZoneBoundaryEdge edge in boundaryEdges)
        {
            if (edge.TouchesMapExterior)
            {
                // WaterPhysics does not create player-barrier write points outside the logical map.
                _protectedExteriorEdges++;
                continue;
            }

            Vector3Int waterCell = zoneController.NormalizedTileToCell(edge.GridLine);
            if (!map.HasTile(waterCell))
            {
                error = $"Interior water barrier target cell {waterCell} for edge {edge} has no terrain tile. " +
                        $"Exterior edge(s) already accounted for by the map boundary: {_protectedExteriorEdges}.";
                return false;
            }

            _requests.Add(new BarrierWriteRequest(
                edge.Orientation,
                new Vector2Int(waterCell.x, waterCell.y)));
        }

        return true;
    }

    private void ResolveWaterController()
    {
        if (waterController == null)
            waterController = FindFirstObjectByType<WaterController>();
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }

    private readonly struct BarrierWriteRequest
    {
        public BarrierWriteRequest(ZoneBoundaryEdgeOrientation orientation, Vector2Int waterTileCell)
        {
            Orientation = orientation;
            WaterTileCell = waterTileCell;
        }

        public ZoneBoundaryEdgeOrientation Orientation { get; }
        public Vector2Int WaterTileCell { get; }
    }
}
