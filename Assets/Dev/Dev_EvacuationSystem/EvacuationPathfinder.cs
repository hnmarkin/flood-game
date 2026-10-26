using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Existing A* traversal over live map data; no scene discovery or route state.</summary>
internal sealed class EvacuationPathfinder
{
    internal struct RouteTileSnapshot
    {
        public string category;
        public bool isWater;
        public bool isDeepFlooded;
    }

    private static readonly Vector2Int[] NeighborOffsets =
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
    };

    private readonly TileMapData tileMapData;
    private readonly Tilemap routeTilemap;
    private readonly ZoneController zoneController;
    private readonly float roadTileCost;
    private readonly float landTileCost;
    private readonly float deepFloodWaterThreshold;
    private readonly int maxPathSearchIterations;
    private readonly bool debugLogs;

    internal EvacuationPathfinder(
        TileMapData tileMapData,
        Tilemap routeTilemap,
        ZoneController zoneController,
        float roadTileCost,
        float landTileCost,
        float deepFloodWaterThreshold,
        int maxPathSearchIterations,
        bool debugLogs)
    {
        this.tileMapData = tileMapData;
        this.routeTilemap = routeTilemap;
        this.zoneController = zoneController;
        this.roadTileCost = roadTileCost;
        this.landTileCost = landTileCost;
        this.deepFloodWaterThreshold = deepFloodWaterThreshold;
        this.maxPathSearchIterations = maxPathSearchIterations;
        this.debugLogs = debugLogs;
    }

    internal bool TryBuildPath(
        Vector2Int startTile,
        Vector2Int destinationTile,
        out List<Vector3Int> pathCells,
        out float distanceTiles)
    {
        pathCells = new List<Vector3Int>();
        distanceTiles = 0f;

        if (!IsWithinMap(startTile) || !IsWithinMap(destinationTile))
            return false;

        if (!TryGetMovementCost(startTile, out _) || !TryGetMovementCost(destinationTile, out _))
            return false;

        List<Vector2Int> openSet = new() { startTile };
        HashSet<Vector2Int> closedSet = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, float> gScore = new()
        {
            [startTile] = 0f,
        };
        Dictionary<Vector2Int, float> fScore = new()
        {
            [startTile] = GetHeuristicCost(startTile, destinationTile),
        };

        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxPathSearchIterations)
        {
            iterations++;
            Vector2Int current = RemoveLowestScoreTile(openSet, fScore);

            if (current == destinationTile)
            {
                List<Vector2Int> normalizedPath = ReconstructPath(cameFrom, current);
                pathCells = ConvertNormalizedPathToCells(normalizedPath);
                distanceTiles = Mathf.Max(0, pathCells.Count - 1);
                return pathCells.Count > 0;
            }

            closedSet.Add(current);

            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                Vector2Int neighbor = current + NeighborOffsets[i];

                if (!IsWithinMap(neighbor) || closedSet.Contains(neighbor))
                    continue;

                if (!TryGetMovementCost(neighbor, out float movementCost))
                    continue;

                float tentativeGScore = gScore[current] + movementCost;

                if (gScore.TryGetValue(neighbor, out float existingScore) && tentativeGScore >= existingScore)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = tentativeGScore + GetHeuristicCost(neighbor, destinationTile);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        if (debugLogs && iterations >= maxPathSearchIterations)
            Debug.LogWarning($"[EvacuationController] Path search stopped at max iterations={maxPathSearchIterations}.");

        return false;
    }

    private bool TryGetMovementCost(Vector2Int normalizedTile, out float movementCost)
    {
        movementCost = landTileCost;

        if (!TryGetRouteTileSnapshot(normalizedTile, out RouteTileSnapshot snapshot) || !IsPassable(snapshot))
            return false;

        movementCost = IsRoadCategory(snapshot.category) ? roadTileCost : landTileCost;
        return true;
    }

    internal bool TryGetRouteTileSnapshot(Vector2Int normalizedTile, out RouteTileSnapshot snapshot)
    {
        snapshot = default;

        if (!IsWithinMap(normalizedTile))
            return false;

        TileInstance tile = TryGetTileInstance(normalizedTile);
        Vector3Int cell = NormalizedTileToCell(normalizedTile);
        bool hasRouteTile = routeTilemap == null || routeTilemap.HasTile(cell);

        if (tile == null && !hasRouteTile)
            return false;

        string category = NormalizeCategory(tile != null ? tile.category : null);
        float waterDepth = tile != null ? Mathf.Max(0f, tile.waterHeight) : 0f;

        if (TryGetWaterDepthFromGrid(normalizedTile, out float gridWaterDepth))
            waterDepth = Mathf.Max(waterDepth, gridWaterDepth);

        snapshot = new RouteTileSnapshot
        {
            category = category,
            isWater = IsWaterCategory(category) || (tile != null && tile.tileType != null && tile.tileType.isWater),
            isDeepFlooded = waterDepth > deepFloodWaterThreshold,
        };

        return true;
    }

    internal static bool IsPassable(RouteTileSnapshot snapshot)
    {
        return !snapshot.isWater && !snapshot.isDeepFlooded;
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new() { current };

        while (cameFrom.TryGetValue(current, out Vector2Int previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private List<Vector3Int> ConvertNormalizedPathToCells(List<Vector2Int> normalizedPath)
    {
        List<Vector3Int> cells = new();

        if (normalizedPath == null)
            return cells;

        for (int i = 0; i < normalizedPath.Count; i++)
            cells.Add(NormalizedTileToCell(normalizedPath[i]));

        return cells;
    }

    internal Vector3Int NormalizedTileToCell(Vector2Int normalizedTile)
    {
        return zoneController != null
            ? zoneController.NormalizedTileToCell(normalizedTile)
            : new Vector3Int(normalizedTile.x, normalizedTile.y, 0);
    }

    internal bool TryCellToNormalizedTile(Vector3Int cell, out Vector2Int normalizedTile)
    {
        normalizedTile = zoneController != null
            ? zoneController.CellToNormalizedTile(cell)
            : new Vector2Int(cell.x, cell.y);
        return IsWithinMap(normalizedTile);
    }

    private TileInstance TryGetTileInstance(Vector2Int normalizedTile)
    {
        if (tileMapData == null || !IsWithinMap(normalizedTile))
            return null;

        try
        {
            return tileMapData.Get(normalizedTile);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private bool TryGetWaterDepthFromGrid(Vector2Int normalizedTile, out float waterDepth)
    {
        waterDepth = 0f;

        if (tileMapData == null || tileMapData.water == null)
            return false;

        if (normalizedTile.x < 0 || normalizedTile.y < 0 ||
            normalizedTile.x >= tileMapData.water.GetLength(0) ||
            normalizedTile.y >= tileMapData.water.GetLength(1))
        {
            return false;
        }

        waterDepth = Mathf.Max(0f, tileMapData.water[normalizedTile.x, normalizedTile.y]);
        return true;
    }

    private Vector2Int RemoveLowestScoreTile(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
    {
        int bestIndex = 0;
        float bestScore = GetScore(openSet[0], fScore);

        for (int i = 1; i < openSet.Count; i++)
        {
            float score = GetScore(openSet[i], fScore);

            if (score >= bestScore)
                continue;

            bestIndex = i;
            bestScore = score;
        }

        Vector2Int result = openSet[bestIndex];
        openSet.RemoveAt(bestIndex);
        return result;
    }

    private float GetHeuristicCost(Vector2Int a, Vector2Int b)
    {
        return ManhattanDistance(a, b) * Mathf.Min(roadTileCost, landTileCost);
    }

    private static float GetScore(Vector2Int tile, Dictionary<Vector2Int, float> scores)
    {
        return scores.TryGetValue(tile, out float score) ? score : float.MaxValue;
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool IsWithinMap(Vector2Int tile)
    {
        if (tileMapData == null)
            return false;

        int width = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeX;
        int height = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeY;

        return tile.x >= 0 && tile.y >= 0 && tile.x < width && tile.y < height;
    }

    internal static bool IsRoadCategory(string category)
    {
        return category.Contains("road") ||
               category.Contains("street") ||
               category.Contains("highway");
    }

    private static bool IsWaterCategory(string category)
    {
        return category.Contains("water") ||
               category.Contains("river") ||
               category.Contains("stream") ||
               category.Contains("lake") ||
               category.Contains("ocean") ||
               category.Contains("flood_source");
    }

    private static string NormalizeCategory(string category)
    {
        return string.IsNullOrWhiteSpace(category)
            ? string.Empty
            : category.Trim().ToLowerInvariant();
    }
}
