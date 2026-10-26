using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BarrierPlacementState
{
    private readonly HashSet<string> _committedZones = new HashSet<string>(StringComparer.Ordinal);

    private float[,] _barrierHeightX;
    private float[,] _barrierHeightY;
    private float[,] _seepageX;
    private float[,] _seepageY;
    private int[,] _blockedX;
    private int[,] _blockedY;

    public int CommittedZoneCount => _committedZones.Count;
    public bool HasLegacyGrid => _blockedX != null && _blockedY != null;

    public bool TryGetLegacyGridDimensions(out int gridWidth, out int gridHeight)
    {
        gridWidth = HasLegacyGrid ? _blockedX.GetLength(0) : 0;
        gridHeight = HasLegacyGrid ? _blockedX.GetLength(1) : 0;
        return HasLegacyGrid;
    }

    public bool InitializeLegacyGrid(int gridWidth, int gridHeight)
    {
        if (gridWidth <= 0 || gridHeight <= 0)
            return false;

        if (HasLegacyGrid &&
            _blockedX.GetLength(0) == gridWidth &&
            _blockedX.GetLength(1) == gridHeight)
        {
            return true;
        }

        _barrierHeightX = new float[gridWidth, gridHeight];
        _barrierHeightY = new float[gridWidth, gridHeight];
        _seepageX = new float[gridWidth, gridHeight];
        _seepageY = new float[gridWidth, gridHeight];
        _blockedX = new int[gridWidth, gridHeight];
        _blockedY = new int[gridWidth, gridHeight];
        _committedZones.Clear();
        return true;
    }

    public bool HasCommittedZone(string geoid)
    {
        return !string.IsNullOrEmpty(geoid) && _committedZones.Contains(geoid);
    }

    public bool CanStoreBoundaryEdges(
        IReadOnlyList<ZoneBoundaryEdge> boundaryEdges,
        out string error)
    {
        error = null;

        if (!HasLegacyGrid)
        {
            error = "Legacy barrier grid is not initialized.";
            return false;
        }

        if (boundaryEdges == null || boundaryEdges.Count == 0)
        {
            error = "Zone boundary has no edges.";
            return false;
        }

        foreach (ZoneBoundaryEdge edge in boundaryEdges)
        {
            Vector2Int legacyCoordinate = edge.ToLegacyBarrierCoordinate();
            if (!IsInBounds(legacyCoordinate.x, legacyCoordinate.y))
            {
                error = $"Legacy barrier coordinate {legacyCoordinate} is outside the compatibility grid.";
                return false;
            }
        }

        return true;
    }

    public bool TryRecordCommittedZone(
        string geoid,
        IReadOnlyList<ZoneBoundaryEdge> boundaryEdges,
        float barrierHeight,
        float seepage,
        out int blockedEdges,
        out string error)
    {
        blockedEdges = 0;
        error = null;

        if (string.IsNullOrEmpty(geoid))
        {
            error = "GEOID is empty.";
            return false;
        }

        if (_committedZones.Contains(geoid))
        {
            error = "This zone already has a boundary barrier.";
            return false;
        }

        if (!IsFinitePositive(barrierHeight) || !IsFiniteNonNegative(seepage))
        {
            error = "Barrier height must be positive and seepage must be nonnegative.";
            return false;
        }

        if (!CanStoreBoundaryEdges(boundaryEdges, out error))
            return false;

        foreach (ZoneBoundaryEdge edge in boundaryEdges)
        {
            Vector2Int legacyCoordinate = edge.ToLegacyBarrierCoordinate();

            if (edge.Orientation == ZoneBoundaryEdgeOrientation.X)
                SetBlockedX(legacyCoordinate.x, legacyCoordinate.y, true, barrierHeight, seepage);
            else
                SetBlockedY(legacyCoordinate.x, legacyCoordinate.y, true, barrierHeight, seepage);

            blockedEdges++;
        }

        _committedZones.Add(geoid);
        return true;
    }

    public bool IsBlockedX(int x, int y)
    {
        return IsInBounds(x, y) && _blockedX[x, y] > 0;
    }

    public bool IsBlockedY(int x, int y)
    {
        return IsInBounds(x, y) && _blockedY[x, y] > 0;
    }

    public float GetBarrierHeightX(int x, int y)
    {
        return IsBlockedX(x, y) ? _barrierHeightX[x, y] : 0f;
    }

    public float GetBarrierHeightY(int x, int y)
    {
        return IsBlockedY(x, y) ? _barrierHeightY[x, y] : 0f;
    }

    public float GetSeepageX(int x, int y)
    {
        return IsBlockedX(x, y) ? _seepageX[x, y] : 0f;
    }

    public float GetSeepageY(int x, int y)
    {
        return IsBlockedY(x, y) ? _seepageY[x, y] : 0f;
    }

    private void SetBlockedX(int x, int y, bool blocked, float barrierHeight, float seepage)
    {
        _blockedX[x, y] = blocked ? 1 : 0;
        _barrierHeightX[x, y] = blocked ? barrierHeight : 0f;
        _seepageX[x, y] = blocked ? seepage : 0f;
    }

    private void SetBlockedY(int x, int y, bool blocked, float barrierHeight, float seepage)
    {
        _blockedY[x, y] = blocked ? 1 : 0;
        _barrierHeightY[x, y] = blocked ? barrierHeight : 0f;
        _seepageY[x, y] = blocked ? seepage : 0f;
    }

    private bool IsInBounds(int x, int y)
    {
        return HasLegacyGrid &&
               x >= 0 &&
               y >= 0 &&
               x < _blockedX.GetLength(0) &&
               y < _blockedX.GetLength(1);
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
