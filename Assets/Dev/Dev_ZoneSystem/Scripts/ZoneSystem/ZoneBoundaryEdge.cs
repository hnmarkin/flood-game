using System;
using UnityEngine;

public enum ZoneBoundaryEdgeOrientation
{
    X = 0,
    Y = 1
}

/// <summary>
/// Boundary edge in normalized tile coordinates.
/// X edges are vertical grid lines between (x - 1, y) and (x, y).
/// Y edges are horizontal grid lines between (x, y - 1) and (x, y).
/// Legacy padded simulator coordinates are derived as GridLine + (1, 1).
/// </summary>
public readonly struct ZoneBoundaryEdge : IEquatable<ZoneBoundaryEdge>, IComparable<ZoneBoundaryEdge>
{
    public ZoneBoundaryEdge(
        ZoneBoundaryEdgeOrientation orientation,
        Vector2Int gridLine,
        Vector2Int interiorTile,
        Vector2Int exteriorTile,
        bool touchesMapExterior)
    {
        Orientation = orientation;
        GridLine = gridLine;
        InteriorTile = interiorTile;
        ExteriorTile = exteriorTile;
        TouchesMapExterior = touchesMapExterior;
    }

    public ZoneBoundaryEdgeOrientation Orientation { get; }
    public Vector2Int GridLine { get; }
    public Vector2Int InteriorTile { get; }
    public Vector2Int ExteriorTile { get; }
    public bool TouchesMapExterior { get; }

    public Vector2Int ToLegacyBarrierCoordinate()
    {
        return new Vector2Int(GridLine.x + 1, GridLine.y + 1);
    }

    public bool Equals(ZoneBoundaryEdge other)
    {
        return Orientation == other.Orientation &&
               GridLine == other.GridLine;
    }

    public override bool Equals(object obj)
    {
        return obj is ZoneBoundaryEdge other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Orientation * 397) ^ GridLine.GetHashCode();
        }
    }

    public int CompareTo(ZoneBoundaryEdge other)
    {
        int orientationCompare = Orientation.CompareTo(other.Orientation);
        if (orientationCompare != 0)
            return orientationCompare;

        int xCompare = GridLine.x.CompareTo(other.GridLine.x);
        if (xCompare != 0)
            return xCompare;

        return GridLine.y.CompareTo(other.GridLine.y);
    }

    public override string ToString()
    {
        return $"{Orientation} GridLine={GridLine} Interior={InteriorTile} Exterior={ExteriorTile} MapExterior={TouchesMapExterior}";
    }
}
