using System.Collections.Generic;
using UnityEngine;

public sealed class ZoneGeometry
{
    public static bool IsBoundaryTile(Vector2Int tile, HashSet<Vector2Int> zoneTiles)
    {
        if (zoneTiles == null || !zoneTiles.Contains(tile))
            return false;

        if (!zoneTiles.Contains(new Vector2Int(tile.x - 1, tile.y))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x + 1, tile.y))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x, tile.y - 1))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x, tile.y + 1))) return true;

        return false;
    }

    public static List<Vector2Int> CalculateBoundaryTiles(HashSet<Vector2Int> zoneTiles)
    {
        List<Vector2Int> boundaryTiles = new List<Vector2Int>();

        if (zoneTiles == null || zoneTiles.Count == 0)
            return boundaryTiles;

        foreach (Vector2Int tile in zoneTiles)
        {
            if (IsBoundaryTile(tile, zoneTiles))
                boundaryTiles.Add(tile);
        }

        boundaryTiles.Sort(CompareTiles);
        return boundaryTiles;
    }

    public static List<ZoneBoundaryEdge> CalculateBoundaryEdges(
        HashSet<Vector2Int> zoneTiles,
        Vector2Int mapSize)
    {
        HashSet<ZoneBoundaryEdge> edges = new HashSet<ZoneBoundaryEdge>();

        if (zoneTiles == null || zoneTiles.Count == 0)
            return new List<ZoneBoundaryEdge>();

        foreach (Vector2Int tile in zoneTiles)
        {
            Vector2Int left = new Vector2Int(tile.x - 1, tile.y);
            Vector2Int right = new Vector2Int(tile.x + 1, tile.y);
            Vector2Int down = new Vector2Int(tile.x, tile.y - 1);
            Vector2Int up = new Vector2Int(tile.x, tile.y + 1);

            if (!zoneTiles.Contains(left))
            {
                edges.Add(new ZoneBoundaryEdge(
                    ZoneBoundaryEdgeOrientation.X,
                    new Vector2Int(tile.x, tile.y),
                    tile,
                    left,
                    !IsInsideMap(left, mapSize)));
            }

            if (!zoneTiles.Contains(right))
            {
                edges.Add(new ZoneBoundaryEdge(
                    ZoneBoundaryEdgeOrientation.X,
                    new Vector2Int(tile.x + 1, tile.y),
                    tile,
                    right,
                    !IsInsideMap(right, mapSize)));
            }

            if (!zoneTiles.Contains(down))
            {
                edges.Add(new ZoneBoundaryEdge(
                    ZoneBoundaryEdgeOrientation.Y,
                    new Vector2Int(tile.x, tile.y),
                    tile,
                    down,
                    !IsInsideMap(down, mapSize)));
            }

            if (!zoneTiles.Contains(up))
            {
                edges.Add(new ZoneBoundaryEdge(
                    ZoneBoundaryEdgeOrientation.Y,
                    new Vector2Int(tile.x, tile.y + 1),
                    tile,
                    up,
                    !IsInsideMap(up, mapSize)));
            }
        }

        List<ZoneBoundaryEdge> orderedEdges = new List<ZoneBoundaryEdge>(edges);
        orderedEdges.Sort();
        return orderedEdges;
    }

    private static bool IsInsideMap(Vector2Int tile, Vector2Int mapSize)
    {
        return mapSize.x > 0 &&
               mapSize.y > 0 &&
               tile.x >= 0 &&
               tile.y >= 0 &&
               tile.x < mapSize.x &&
               tile.y < mapSize.y;
    }

    private static int CompareTiles(Vector2Int a, Vector2Int b)
    {
        int xCompare = a.x.CompareTo(b.x);
        if (xCompare != 0)
            return xCompare;

        return a.y.CompareTo(b.y);
    }
}
