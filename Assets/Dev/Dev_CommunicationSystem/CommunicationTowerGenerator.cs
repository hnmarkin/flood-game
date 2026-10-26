using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Ordered tower placement using existing map, zone and shelter providers.</summary>
internal sealed class CommunicationTowerGenerator
{
    private readonly TileMapData tileMapData;
    private readonly Tilemap communicationTilemap;
    private readonly ZoneController zoneController;
    private readonly ShelterController shelterController;
    private readonly int zonesPerTower;
    private readonly int towerSearchRadius;
    private readonly bool avoidShelterTiles;

    internal CommunicationTowerGenerator(
        TileMapData tileMapData,
        Tilemap communicationTilemap,
        ZoneController zoneController,
        ShelterController shelterController,
        int zonesPerTower,
        int towerSearchRadius,
        bool avoidShelterTiles)
    {
        this.tileMapData = tileMapData;
        this.communicationTilemap = communicationTilemap;
        this.zoneController = zoneController;
        this.shelterController = shelterController;
        this.zonesPerTower = zonesPerTower;
        this.towerSearchRadius = towerSearchRadius;
        this.avoidShelterTiles = avoidShelterTiles;
    }

    internal static List<string> GetOrderedGeoids(
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles)
    {
        List<string> geoids = new(allZoneTiles.Keys);
        geoids.Sort(StringComparer.Ordinal);
        return geoids;
    }

    // Yield each result so the coordinator can store and spawn it before the
    // next group is evaluated, retaining the original generation ordering.
    internal IEnumerable<CommunicationTowerData> GenerateTowers(
        List<string> geoids,
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles)
    {
        HashSet<Vector3Int> occupiedTowerCells = new();
        int towerIndex = 1;
        List<List<string>> towerGroups = BuildZoneClusters(geoids, allZoneTiles);

        for (int groupIndex = 0; groupIndex < towerGroups.Count; groupIndex++)
        {
            List<string> groupGeoids = towerGroups[groupIndex];

            if (!TryGetGroupCenter(groupGeoids, allZoneTiles, out Vector2 groupCenter))
                continue;

            if (!TryFindTowerTile(groupCenter, occupiedTowerCells, out Vector2Int normalizedTowerTile))
            {
                Debug.LogWarning($"[CommunicationTowerController] No valid communication tower tile found near zone group {string.Join(", ", groupGeoids)}.");
                continue;
            }

            Vector3Int tileCell = zoneController.NormalizedTileToCell(normalizedTowerTile);
            Vector3 worldPosition = communicationTilemap.GetCellCenterWorld(tileCell);
            CommunicationTowerData tower = new()
            {
                towerId = $"tower_{towerIndex:00}",
                tileCell = tileCell,
                worldPosition = worldPosition,
                associatedZoneGeoids = groupGeoids,
                isActive = false,
            };

            occupiedTowerCells.Add(tileCell);
            yield return tower;
            towerIndex++;
        }
    }

    private readonly struct TileSnapshot
    {
        public readonly string category;
        public readonly bool isWater;

        public TileSnapshot(string category, bool isWater)
        {
            this.category = category;
            this.isWater = isWater;
        }
    }

    private sealed class ZoneCandidate
    {
        public string geoid;
        public Vector2 normalizedCenter;
    }

    private List<List<string>> BuildZoneClusters(
        List<string> geoids,
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles)
    {
        List<List<string>> groups = new();
        List<ZoneCandidate> unassignedZones = new();

        for (int i = 0; i < geoids.Count; i++)
        {
            string geoid = NormalizeGeoid(geoids[i]);

            if (string.IsNullOrEmpty(geoid) ||
                !TryGetZoneCenter(geoid, allZoneTiles, out Vector2 center))
            {
                continue;
            }

            unassignedZones.Add(new ZoneCandidate
            {
                geoid = geoid,
                normalizedCenter = center,
            });
        }

        while (unassignedZones.Count > 0)
        {
            ZoneCandidate seed = unassignedZones[0];
            unassignedZones.RemoveAt(0);

            List<ZoneCandidate> cluster = new() { seed };

            while (cluster.Count < zonesPerTower && unassignedZones.Count > 0)
            {
                Vector2 clusterCenter = GetClusterCenter(cluster);
                int nearestIndex = FindNearestZoneIndex(unassignedZones, clusterCenter);

                cluster.Add(unassignedZones[nearestIndex]);
                unassignedZones.RemoveAt(nearestIndex);
            }

            List<string> groupGeoids = new();

            for (int i = 0; i < cluster.Count; i++)
                groupGeoids.Add(cluster[i].geoid);

            groups.Add(groupGeoids);
        }

        return groups;
    }

    private bool TryGetGroupCenter(
        List<string> groupGeoids,
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles,
        out Vector2 groupCenter)
    {
        groupCenter = Vector2.zero;

        if (groupGeoids == null || groupGeoids.Count == 0)
            return false;

        Vector2 sum = Vector2.zero;
        int tileCount = 0;

        for (int i = 0; i < groupGeoids.Count; i++)
        {
            string geoid = groupGeoids[i];

            if (!allZoneTiles.TryGetValue(geoid, out HashSet<Vector2Int> zoneTiles) || zoneTiles == null)
                continue;

            foreach (Vector2Int tile in zoneTiles)
            {
                sum += tile;
                tileCount++;
            }
        }

        if (tileCount == 0)
            return false;

        groupCenter = sum / tileCount;
        return true;
    }

    private bool TryGetZoneCenter(
        string geoid,
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZoneTiles,
        out Vector2 center)
    {
        center = Vector2.zero;

        if (string.IsNullOrEmpty(geoid) ||
            allZoneTiles == null ||
            !allZoneTiles.TryGetValue(geoid, out HashSet<Vector2Int> zoneTiles) ||
            zoneTiles == null ||
            zoneTiles.Count == 0)
        {
            return false;
        }

        Vector2 sum = Vector2.zero;
        int tileCount = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            sum += tile;
            tileCount++;
        }

        if (tileCount == 0)
            return false;

        center = sum / tileCount;
        return true;
    }

    private Vector2 GetClusterCenter(List<ZoneCandidate> cluster)
    {
        if (cluster == null || cluster.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;

        for (int i = 0; i < cluster.Count; i++)
            sum += cluster[i].normalizedCenter;

        return sum / cluster.Count;
    }

    private int FindNearestZoneIndex(List<ZoneCandidate> candidates, Vector2 center)
    {
        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            float distance = Vector2.SqrMagnitude(candidates[i].normalizedCenter - center);

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestIndex = i;
        }

        return nearestIndex;
    }

    private bool TryFindTowerTile(
        Vector2 groupCenter,
        HashSet<Vector3Int> occupiedTowerCells,
        out Vector2Int bestTile)
    {
        bestTile = default;
        int maxRadius = Mathf.Max(towerSearchRadius, GetMapLimit());
        float bestScore = float.MaxValue;
        bool found = false;

        Vector2Int roundedCenter = new(
            Mathf.RoundToInt(groupCenter.x),
            Mathf.RoundToInt(groupCenter.y)
        );

        for (int radius = 0; radius <= maxRadius; radius++)
        {
            bool foundAtRadius = false;

            for (int y = roundedCenter.y - radius; y <= roundedCenter.y + radius; y++)
            {
                for (int x = roundedCenter.x - radius; x <= roundedCenter.x + radius; x++)
                {
                    if (Mathf.Abs(x - roundedCenter.x) != radius && Mathf.Abs(y - roundedCenter.y) != radius)
                        continue;

                    Vector2Int candidate = new(x, y);

                    if (!TryScoreTowerTile(candidate, groupCenter, occupiedTowerCells, out float score))
                        continue;

                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    bestTile = candidate;
                    found = true;
                    foundAtRadius = true;
                }
            }

            if (foundAtRadius)
                return true;
        }

        return found;
    }

    private bool TryScoreTowerTile(
        Vector2Int tile,
        Vector2 groupCenter,
        HashSet<Vector3Int> occupiedTowerCells,
        out float score)
    {
        score = float.MaxValue;

        if (!TryGetTileSnapshot(tile, out TileSnapshot snapshot) || snapshot.isWater)
            return false;

        Vector3Int cell = zoneController.NormalizedTileToCell(tile);

        if (occupiedTowerCells != null && occupiedTowerCells.Contains(cell))
            return false;

        if (avoidShelterTiles && IsShelterTile(cell))
            return false;

        int priority = GetTowerTilePriority(tile, snapshot);
        float distance = Vector2.Distance(new Vector2(tile.x, tile.y), groupCenter);
        score = (priority * 1000f) + distance;
        return true;
    }

    private bool TryGetTileSnapshot(Vector2Int tile, out TileSnapshot snapshot)
    {
        snapshot = default;

        if (!IsWithinMap(tile) || tileMapData == null)
            return false;

        TileInstance tileInstance = tileMapData.Get(tile);

        if (tileInstance == null)
            return false;

        string category = tileInstance.category;
        bool isWater = IsWaterCategory(category) ||
                       (tileInstance.tileType != null && tileInstance.tileType.isWater);

        snapshot = new TileSnapshot(category, isWater);
        return true;
    }

    private int GetTowerTilePriority(Vector2Int tile, TileSnapshot snapshot)
    {
        if (IsBuildingCategory(snapshot.category))
            return 0;

        if (IsRoadAdjacentUsableTile(tile, snapshot))
            return 1;

        if (IsLandCategory(snapshot.category))
            return 2;

        return 3;
    }

    private bool IsRoadAdjacentUsableTile(Vector2Int tile, TileSnapshot snapshot)
    {
        if (snapshot.isWater)
            return false;

        if (!IsLandCategory(snapshot.category) && !IsBuildingCategory(snapshot.category))
            return false;

        Vector2Int[] directions =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
        };

        for (int i = 0; i < directions.Length; i++)
        {
            if (!TryGetTileSnapshot(tile + directions[i], out TileSnapshot neighborSnapshot))
                continue;

            if (IsRoadCategory(neighborSnapshot.category))
                return true;
        }

        return false;
    }

    private bool IsShelterTile(Vector3Int cell)
    {
        if (shelterController != null && shelterController.TryGetShelterAtTile(cell, out PlacedShelterData placedShelter))
        {
            if (placedShelter == null || placedShelter.isActive)
                return true;
        }

        if (shelterController != null && shelterController.IsValidShelterCandidateTile(cell))
            return true;

        return false;
    }

    private bool IsWithinMap(Vector2Int tile)
    {
        if (tileMapData == null)
            return false;

        int width = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeX;
        int height = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeY;
        return tile.x >= 0 && tile.y >= 0 && tile.x < width && tile.y < height;
    }

    private int GetMapLimit()
    {
        if (tileMapData == null)
            return towerSearchRadius;

        int width = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeX;
        int height = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeY;
        return Mathf.Max(width, height, towerSearchRadius);
    }

    private bool IsWaterCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        string normalized = category.ToLowerInvariant();
        return normalized.Contains("water") ||
               normalized.Contains("river") ||
               normalized.Contains("stream") ||
               normalized.Contains("lake") ||
               normalized.Contains("ocean") ||
               normalized.Contains("flood");
    }

    private bool IsBuildingCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        switch (category.ToLowerInvariant())
        {
            case "building":
            case "industrial":
            case "residential":
            case "commercial":
            case "city":
                return true;

            default:
                return false;
        }
    }

    private bool IsRoadCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        switch (category.ToLowerInvariant())
        {
            case "road":
            case "highway":
            case "rail":
                return true;

            default:
                return false;
        }
    }

    private bool IsLandCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        switch (category.ToLowerInvariant())
        {
            case "land":
            case "grass":
            case "park":
            case "forest":
            case "mountain":
            case "beach":
            case "field":
            case "soil":
                return true;

            default:
                return false;
        }
    }

    private string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? null : geoid.Trim();
    }
}
