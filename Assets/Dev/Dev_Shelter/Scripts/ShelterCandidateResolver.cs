using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates shelter candidates from legacy map and zone data. It does not read
/// player input, spawn visuals, or mutate placed-shelter state.
/// </summary>
public sealed class ShelterCandidateResolver
{
    private sealed class ZoneContext
    {
        public string geoid;
        public HashSet<Vector2Int> tiles;
        public Vector2 normalizedCenter;
        public Vector3 worldCenter;
        public bool hasWorldCenter;
        public bool isEnabled;
    }

    private sealed class ShelterGroup
    {
        public readonly List<ZoneContext> hostZones = new();
        public readonly List<ZoneContext> associatedZones = new();
    }

    private readonly Dictionary<string, bool> _zoneEnabledLookup = new();

    private ShelterCandidateContext _context;
    private ShelterCandidateSettings _settings;

    public bool RefreshCandidates(
        ShelterCandidateContext context,
        ShelterCandidateSettings settings,
        List<ShelterCandidateResult> targetResults)
    {
        _context = context;
        _settings = settings;
        targetResults?.Clear();

        if (targetResults == null || context == null || settings == null)
            return false;

        settings.Validate();
        BuildZoneEnabledLookup();

        if (!context.IsMapReady())
            return false;

        List<ZoneContext> enabledZones = new();
        List<ZoneContext> disabledServableZones = new();

        if (!TryBuildZoneContexts(enabledZones, disabledServableZones))
            return false;

        GetElevationBounds(out float minElevation, out float maxElevation);

        int mapSize = context.TileMapData.N;
        int[,] supportDistanceMap = BuildDistanceMap(mapSize, IsSupportFeatureTile);
        int[,] waterDistanceMap = BuildDistanceMap(mapSize, IsWaterTile);

        List<ShelterGroup> groups = BuildGroups(enabledZones);

        if (settings.allowDisabledZonesToBeServedByNearbyShelters && disabledServableZones.Count > 0)
            AttachDisabledServedZones(disabledServableZones, groups);

        for (int i = 0; i < groups.Count; i++)
        {
            ShelterCandidateResult result = BuildCandidateResult(
                i,
                groups[i],
                supportDistanceMap,
                waterDistanceMap,
                minElevation,
                maxElevation);

            targetResults.Add(result);
        }

        return true;
    }

    public bool IsZoneEnabledForShelterPlacement(string geoid)
    {
        geoid = ShelterMetrics.NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return false;

        if (_zoneEnabledLookup.TryGetValue(geoid, out bool isEnabled))
            return isEnabled;

        return _settings == null || _settings.defaultZoneEnabled;
    }

    public bool TryFindValidCandidateAtCell(
        IReadOnlyList<ShelterCandidateResult> candidates,
        Vector3Int cell,
        out ShelterCandidateResult candidate)
    {
        candidate = null;

        if (candidates == null)
            return false;

        for (int i = 0; i < candidates.Count; i++)
        {
            ShelterCandidateResult result = candidates[i];

            if (result == null || !result.isValid || result.tileCell != cell)
                continue;

            candidate = result;
            return true;
        }

        return false;
    }

    public bool TryGetTileSnapshotFromCell(Vector3Int cell, out ShelterTileSnapshot snapshot)
    {
        snapshot = default;

        if (!TryCellToNormalizedTile(cell, out Vector2Int normalizedTile))
            return false;

        return TryGetTileSnapshot(normalizedTile, out snapshot);
    }

    public void GetElevationBounds(out float minElevation, out float maxElevation)
    {
        minElevation = float.MaxValue;
        maxElevation = float.MinValue;

        if (_context == null || _context.TileMapData == null)
        {
            minElevation = 0f;
            maxElevation = 1f;
            return;
        }

        for (int y = 0; y < _context.TileMapData.N; y++)
        {
            for (int x = 0; x < _context.TileMapData.N; x++)
            {
                TileInstance tile = _context.TileMapData.Get(new Vector2Int(x, y));

                if (tile == null)
                    continue;

                if (tile.elevation < minElevation)
                    minElevation = tile.elevation;

                if (tile.elevation > maxElevation)
                    maxElevation = tile.elevation;
            }
        }

        if (minElevation == float.MaxValue || maxElevation == float.MinValue)
        {
            minElevation = 0f;
            maxElevation = 1f;
            return;
        }

        if (Mathf.Approximately(minElevation, maxElevation))
            maxElevation = minElevation + 1f;
    }

    private void BuildZoneEnabledLookup()
    {
        _zoneEnabledLookup.Clear();

        if (_settings == null || _settings.zoneOverrides == null)
            return;

        for (int i = 0; i < _settings.zoneOverrides.Count; i++)
        {
            ShelterZoneOverride zoneOverride = _settings.zoneOverrides[i];
            string geoid = ShelterMetrics.NormalizeGeoid(zoneOverride != null ? zoneOverride.geoid : null);

            if (string.IsNullOrEmpty(geoid))
                continue;

            _zoneEnabledLookup[geoid] = zoneOverride.shelterPlacementEnabled;
        }
    }

    private bool TryBuildZoneContexts(List<ZoneContext> enabledZones, List<ZoneContext> disabledServableZones)
    {
        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZones = _context.ZoneController.GetAllZoneTiles();

        if (allZones == null || allZones.Count == 0)
            return false;

        foreach (KeyValuePair<string, HashSet<Vector2Int>> pair in allZones)
        {
            string geoid = ShelterMetrics.NormalizeGeoid(pair.Key);

            if (string.IsNullOrEmpty(geoid) || pair.Value == null || pair.Value.Count == 0)
                continue;

            ZoneContext zone = new()
            {
                geoid = geoid,
                tiles = pair.Value,
                isEnabled = IsZoneEnabledForShelterPlacement(geoid),
            };

            zone.normalizedCenter = CalculateNormalizedCenter(pair.Value);
            zone.hasWorldCenter = TryGetZoneCenterWorld(geoid, pair.Value, out zone.worldCenter);

            if (zone.isEnabled)
            {
                enabledZones.Add(zone);
            }
            else if (_settings.allowDisabledZonesToBeServedByNearbyShelters)
            {
                disabledServableZones.Add(zone);
            }
            else if (_context.DebugLogs)
            {
                Debug.Log($"[ShelterCandidateResolver] Skipping disabled zone '{geoid}' from shelter placement.");
            }
        }

        enabledZones.Sort(CompareZonesByCenter);
        disabledServableZones.Sort(CompareZonesByCenter);

        return enabledZones.Count > 0;
    }

    private List<ShelterGroup> BuildGroups(List<ZoneContext> enabledZones)
    {
        List<ShelterGroup> groups = new();
        List<ZoneContext> remaining = new(enabledZones);

        while (remaining.Count >= _settings.minZonesPerShelter)
        {
            List<ZoneContext> bestGroupMembers = null;
            float bestCompactness = float.MaxValue;
            int maxGroupSize = Mathf.Min(_settings.maxZonesPerShelter, remaining.Count);

            for (int groupSize = maxGroupSize; groupSize >= _settings.minZonesPerShelter; groupSize--)
            {
                List<ZoneContext> bestForSize = null;
                float bestForSizeCompactness = float.MaxValue;

                for (int zoneIndex = 0; zoneIndex < remaining.Count; zoneIndex++)
                {
                    ZoneContext seed = remaining[zoneIndex];
                    List<ZoneContext> candidateMembers = GetNearestZones(seed, remaining, groupSize);
                    float candidateCompactness = ComputeGroupCompactness(candidateMembers);

                    if (candidateCompactness + 0.001f < bestForSizeCompactness)
                    {
                        bestForSizeCompactness = candidateCompactness;
                        bestForSize = candidateMembers;
                    }
                }

                if (bestForSize != null)
                {
                    bestGroupMembers = bestForSize;
                    bestCompactness = bestForSizeCompactness;
                    break;
                }
            }

            if (bestGroupMembers == null)
                break;

            ShelterGroup group = new();
            group.hostZones.AddRange(bestGroupMembers);
            group.associatedZones.AddRange(bestGroupMembers);
            groups.Add(group);

            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                if (bestGroupMembers.Contains(remaining[i]))
                    remaining.RemoveAt(i);
            }

            if (_context.DebugLogs)
            {
                Debug.Log(
                    $"[ShelterCandidateResolver] Built shelter group with {bestGroupMembers.Count} enabled zones. " +
                    $"Compactness={bestCompactness:0.00}");
            }
        }

        DistributeRemainingZones(remaining, groups);
        return groups;
    }

    private void DistributeRemainingZones(List<ZoneContext> remaining, List<ShelterGroup> groups)
    {
        if (remaining.Count == 0)
            return;

        List<ZoneContext> stillUnassigned = new();

        for (int i = 0; i < remaining.Count; i++)
        {
            ZoneContext zone = remaining[i];
            ShelterGroup nearestGroup = FindNearestGroupWithCapacity(zone, groups);

            if (nearestGroup == null)
            {
                stillUnassigned.Add(zone);
                continue;
            }

            nearestGroup.hostZones.Add(zone);
            nearestGroup.associatedZones.Add(zone);
        }

        if (stillUnassigned.Count == 0)
            return;

        if (!_settings.allowUndersizedFallbackGroups)
        {
            if (_context.DebugLogs)
            {
                Debug.Log(
                    $"[ShelterCandidateResolver] Skipped {stillUnassigned.Count} leftover zones because undersized fallback groups are disabled.");
            }

            return;
        }

        int startIndex = 0;

        while (startIndex < stillUnassigned.Count)
        {
            int remainingCount = stillUnassigned.Count - startIndex;
            int groupSize = Mathf.Min(_settings.maxZonesPerShelter, remainingCount);

            if (groupSize == 1 && !_settings.allowSingleZoneShelters)
            {
                if (_context.DebugLogs)
                {
                    Debug.Log(
                        $"[ShelterCandidateResolver] Left zone '{stillUnassigned[startIndex].geoid}' without a shelter candidate because single-zone shelters are disabled.");
                }

                startIndex++;
                continue;
            }

            ShelterGroup group = new();

            for (int i = 0; i < groupSize; i++)
            {
                ZoneContext zone = stillUnassigned[startIndex + i];
                group.hostZones.Add(zone);
                group.associatedZones.Add(zone);
            }

            groups.Add(group);
            startIndex += groupSize;
        }
    }

    private void AttachDisabledServedZones(List<ZoneContext> disabledServableZones, List<ShelterGroup> groups)
    {
        if (groups.Count == 0)
            return;

        for (int i = 0; i < disabledServableZones.Count; i++)
        {
            ZoneContext disabledZone = disabledServableZones[i];
            ShelterGroup group = FindNearestGroupWithCapacity(disabledZone, groups);

            if (group == null)
            {
                if (_context.DebugLogs)
                {
                    Debug.Log(
                        $"[ShelterCandidateResolver] Disabled zone '{disabledZone.geoid}' could not be attached to a nearby shelter group.");
                }

                continue;
            }

            group.associatedZones.Add(disabledZone);
        }
    }

    private ShelterGroup FindNearestGroupWithCapacity(ZoneContext zone, List<ShelterGroup> groups)
    {
        ShelterGroup bestGroup = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < groups.Count; i++)
        {
            ShelterGroup group = groups[i];

            if (group.associatedZones.Count >= _settings.maxZonesPerShelter)
                continue;

            Vector2 groupCenter = CalculateGroupCenter(group.associatedZones);
            float sqrDistance = (groupCenter - zone.normalizedCenter).sqrMagnitude;

            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                bestGroup = group;
            }
        }

        return bestGroup;
    }

    private List<ZoneContext> GetNearestZones(ZoneContext seed, List<ZoneContext> availableZones, int count)
    {
        List<ZoneContext> orderedZones = new(availableZones);
        orderedZones.Sort((a, b) => CompareDistanceToSeed(seed, a, b));

        if (orderedZones.Count > count)
            orderedZones.RemoveRange(count, orderedZones.Count - count);

        return orderedZones;
    }

    private int CompareDistanceToSeed(ZoneContext seed, ZoneContext a, ZoneContext b)
    {
        float aDistance = (a.normalizedCenter - seed.normalizedCenter).sqrMagnitude;
        float bDistance = (b.normalizedCenter - seed.normalizedCenter).sqrMagnitude;
        int distanceCompare = aDistance.CompareTo(bDistance);

        if (distanceCompare != 0)
            return distanceCompare;

        return string.CompareOrdinal(a.geoid, b.geoid);
    }

    private float ComputeGroupCompactness(List<ZoneContext> zones)
    {
        if (zones == null || zones.Count == 0)
            return float.MaxValue;

        Vector2 center = CalculateGroupCenter(zones);
        float totalDistance = 0f;

        for (int i = 0; i < zones.Count; i++)
            totalDistance += Vector2.Distance(zones[i].normalizedCenter, center);

        return totalDistance / zones.Count;
    }

    private ShelterCandidateResult BuildCandidateResult(
        int groupIndex,
        ShelterGroup group,
        int[,] supportDistanceMap,
        int[,] waterDistanceMap,
        float minElevation,
        float maxElevation)
    {
        ShelterCandidateResult result = new()
        {
            candidateId = $"shelter_candidate_{groupIndex + 1:00}",
        };

        if (group == null || group.hostZones.Count == 0)
        {
            result.isValid = false;
            result.debugReason = "No enabled host zones were available for this shelter group.";
            return result;
        }

        for (int i = 0; i < group.associatedZones.Count; i++)
            result.associatedZoneGeoids.Add(group.associatedZones[i].geoid);

        result.zoneCount = result.associatedZoneGeoids.Count;

        Vector2 groupCenter = CalculateGroupCenter(group.associatedZones);
        Vector2Int? bestTile = null;
        float bestScore = float.MinValue;
        string bestReason = "No suitable dry land tile was found.";

        HashSet<Vector2Int> searchedTiles = new();

        for (int i = 0; i < group.hostZones.Count; i++)
        {
            foreach (Vector2Int tile in group.hostZones[i].tiles)
            {
                if (!searchedTiles.Add(tile))
                    continue;

                EvaluateCandidateTile(
                    tile,
                    groupCenter,
                    supportDistanceMap,
                    waterDistanceMap,
                    minElevation,
                    maxElevation,
                    ref bestTile,
                    ref bestScore,
                    ref bestReason);
            }
        }

        if (!bestTile.HasValue && _settings.fallbackSearchRadiusCells > 0)
        {
            Vector2Int centerTile = new(
                Mathf.RoundToInt(groupCenter.x),
                Mathf.RoundToInt(groupCenter.y));

            for (int offsetY = -_settings.fallbackSearchRadiusCells; offsetY <= _settings.fallbackSearchRadiusCells; offsetY++)
            {
                for (int offsetX = -_settings.fallbackSearchRadiusCells; offsetX <= _settings.fallbackSearchRadiusCells; offsetX++)
                {
                    Vector2Int tile = new(centerTile.x + offsetX, centerTile.y + offsetY);

                    if (!IsWithinMap(tile) || !searchedTiles.Add(tile))
                        continue;

                    EvaluateCandidateTile(
                        tile,
                        groupCenter,
                        supportDistanceMap,
                        waterDistanceMap,
                        minElevation,
                        maxElevation,
                        ref bestTile,
                        ref bestScore,
                        ref bestReason);
                }
            }
        }

        if (!bestTile.HasValue)
        {
            result.isValid = false;
            result.suitabilityScore = bestScore;
            result.debugReason = bestReason;
            return result;
        }

        Vector3Int cell = _context.ZoneController.NormalizedTileToCell(bestTile.Value);
        result.tileCell = cell;
        result.worldPosition = _context.PlacementTilemap != null
            ? _context.PlacementTilemap.GetCellCenterWorld(cell)
            : new Vector3(cell.x, cell.y, cell.z);
        result.suitabilityScore = bestScore;
        result.isValid = true;
        result.debugReason = bestReason;
        return result;
    }

    private void EvaluateCandidateTile(
        Vector2Int tile,
        Vector2 groupCenter,
        int[,] supportDistanceMap,
        int[,] waterDistanceMap,
        float minElevation,
        float maxElevation,
        ref Vector2Int? bestTile,
        ref float bestScore,
        ref string bestReason)
    {
        if (!TryGetTileSnapshot(tile, out ShelterTileSnapshot snapshot))
            return;

        if (snapshot.isWaterCategory || snapshot.waterHeight > _settings.deepFloodWaterThreshold)
            return;

        if (!string.IsNullOrEmpty(snapshot.geoid) && !IsZoneEnabledForShelterPlacement(snapshot.geoid))
            return;

        float elevationRange = Mathf.Max(0.0001f, maxElevation - minElevation);
        float elevationScore = Mathf.Clamp01((snapshot.elevation - minElevation) / elevationRange);
        float centerDistance = Vector2.Distance(new Vector2(tile.x, tile.y), groupCenter);
        float centerScore = 1f - Mathf.Clamp01(centerDistance / _settings.centerPreferenceDistanceCells);
        float supportScore = ScoreDistanceMap(supportDistanceMap, tile, _settings.supportFeatureDistanceForMaxScore);
        float waterClearanceScore = ScoreDistanceMap(waterDistanceMap, tile, _settings.waterClearanceDistanceForMaxScore);
        float categoryScore = GetCategorySuitabilityScore(snapshot.category);

        float score =
            categoryScore +
            (elevationScore * _settings.elevationWeight) +
            (centerScore * _settings.centerWeight) +
            (supportScore * _settings.supportFeatureWeight) +
            (waterClearanceScore * _settings.waterClearanceWeight) -
            (Mathf.Max(0f, snapshot.waterHeight) * _settings.waterPenaltyWeight);

        if (score <= bestScore)
            return;

        bestTile = tile;
        bestScore = score;
        bestReason = $"Category={snapshot.category}, Elev={snapshot.elevation:0.##}, Water={snapshot.waterHeight:0.##}, CenterScore={centerScore:0.##}";
    }

    private bool TryGetTileSnapshot(Vector2Int tile, out ShelterTileSnapshot snapshot)
    {
        snapshot = default;

        if (!IsWithinMap(tile))
            return false;

        TileInstance tileInstance = _context.TileMapData.Get(tile);
        Vector2Int rc = default;

        if (tileInstance == null && !TryGetRowColumn(tile, out rc))
            return false;

        snapshot.category = tileInstance != null ? tileInstance.category : _context.JsonMapLoader.catGrid[rc.x, rc.y];
        snapshot.geoid = tileInstance != null
            ? ShelterMetrics.NormalizeGeoid(tileInstance.geoid)
            : ShelterMetrics.NormalizeGeoid(_context.JsonMapLoader.geoidGrid[rc.x, rc.y]);
        snapshot.elevation = tileInstance != null ? tileInstance.elevation : 0f;
        snapshot.waterHeight = tileInstance != null ? tileInstance.waterHeight : 0f;
        snapshot.isWaterCategory = IsWaterCategory(snapshot.category) ||
                                   (tileInstance != null && tileInstance.tileType != null && tileInstance.tileType.isWater);
        return true;
    }

    private int[,] BuildDistanceMap(int mapSize, Func<Vector2Int, bool> isSourceTile)
    {
        int[,] distanceMap = new int[mapSize, mapSize];
        Queue<Vector2Int> frontier = new();

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                distanceMap[x, y] = -1;
                Vector2Int tile = new(x, y);

                if (!isSourceTile(tile))
                    continue;

                distanceMap[x, y] = 0;
                frontier.Enqueue(tile);
            }
        }

        Vector2Int[] directions =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
        };

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            int nextDistance = distanceMap[current.x, current.y] + 1;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];

                if (!IsWithinMap(next) || distanceMap[next.x, next.y] >= 0)
                    continue;

                distanceMap[next.x, next.y] = nextDistance;
                frontier.Enqueue(next);
            }
        }

        return distanceMap;
    }

    private bool IsSupportFeatureTile(Vector2Int tile)
    {
        if (!TryGetTileSnapshot(tile, out ShelterTileSnapshot snapshot))
            return false;

        string category = snapshot.category;

        if (string.IsNullOrEmpty(category))
            return false;

        switch (category.ToLowerInvariant())
        {
            case "road":
            case "highway":
            case "rail":
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

    private bool IsWaterTile(Vector2Int tile)
    {
        return TryGetTileSnapshot(tile, out ShelterTileSnapshot snapshot) && snapshot.isWaterCategory;
    }

    private float ScoreDistanceMap(int[,] distanceMap, Vector2Int tile, int maxDistanceForFullScore)
    {
        if (distanceMap == null || !IsWithinMap(tile))
            return 0f;

        int distance = distanceMap[tile.x, tile.y];

        if (distance < 0)
            return 0f;

        return 1f - Mathf.Clamp01((float)distance / Mathf.Max(1, maxDistanceForFullScore));
    }

    private float GetCategorySuitabilityScore(string category)
    {
        if (string.IsNullOrEmpty(category))
            return 0.1f;

        switch (category.ToLowerInvariant())
        {
            case "building":
            case "industrial":
            case "residential":
            case "commercial":
            case "city":
                return 0.55f;

            case "land":
            case "park":
            case "forest":
            case "mountain":
            case "beach":
                return 0.35f;

            case "road":
            case "highway":
            case "rail":
                return 0.15f;

            default:
                return 0.2f;
        }
    }

    private bool TryGetZoneCenterWorld(string geoid, HashSet<Vector2Int> zoneTiles, out Vector3 worldCenter)
    {
        if (_context.ZoneBaselineRiskController != null &&
            _context.ZoneBaselineRiskController.TryGetZoneCenterWorld(geoid, out worldCenter))
            return true;

        worldCenter = Vector3.zero;

        if (zoneTiles == null || zoneTiles.Count == 0 || _context.ZoneController == null || _context.PlacementTilemap == null)
            return false;

        Vector3 sum = Vector3.zero;
        int sampleCount = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            Vector3Int cell = _context.ZoneController.NormalizedTileToCell(tile);
            sum += _context.PlacementTilemap.GetCellCenterWorld(cell);
            sampleCount++;
        }

        if (sampleCount == 0)
            return false;

        worldCenter = sum / sampleCount;
        return true;
    }

    private bool TryGetRowColumn(Vector2Int normalizedTile, out Vector2Int rc)
    {
        rc = default;

        if (_context.ZoneController == null || _context.JsonMapLoader == null || _context.JsonMapLoader.cellToRC == null)
            return false;

        Vector3Int cell = _context.ZoneController.NormalizedTileToCell(normalizedTile);
        return _context.JsonMapLoader.cellToRC.TryGetValue(cell, out rc);
    }

    private bool TryCellToNormalizedTile(Vector3Int cell, out Vector2Int normalizedTile)
    {
        normalizedTile = default;

        if (_context == null || _context.PlacementTilemap == null || _context.TileMapData == null)
            return false;

        BoundsInt bounds = _context.PlacementTilemap.cellBounds;
        normalizedTile = new Vector2Int(cell.x - bounds.xMin, cell.y - bounds.yMin);
        return IsWithinMap(normalizedTile);
    }

    private bool IsWithinMap(Vector2Int tile)
    {
        return _context != null &&
               _context.TileMapData != null &&
               tile.x >= 0 &&
               tile.y >= 0 &&
               tile.x < _context.TileMapData.N &&
               tile.y < _context.TileMapData.N;
    }

    private bool IsWaterCategory(string category)
    {
        if (string.IsNullOrEmpty(category) || _settings == null || _settings.waterCategoryKeywords == null)
            return false;

        string normalized = category.ToLowerInvariant();

        for (int i = 0; i < _settings.waterCategoryKeywords.Count; i++)
        {
            string keyword = _settings.waterCategoryKeywords[i];

            if (!string.IsNullOrEmpty(keyword) && normalized.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static Vector2 CalculateNormalizedCenter(HashSet<Vector2Int> zoneTiles)
    {
        if (zoneTiles == null || zoneTiles.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        int count = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            sum += new Vector2(tile.x, tile.y);
            count++;
        }

        return count > 0 ? sum / count : Vector2.zero;
    }

    private static Vector2 CalculateGroupCenter(List<ZoneContext> zones)
    {
        if (zones == null || zones.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;

        for (int i = 0; i < zones.Count; i++)
            sum += zones[i].normalizedCenter;

        return sum / zones.Count;
    }

    private static int CompareZonesByCenter(ZoneContext a, ZoneContext b)
    {
        int yCompare = a.normalizedCenter.y.CompareTo(b.normalizedCenter.y);

        if (yCompare != 0)
            return yCompare;

        int xCompare = a.normalizedCenter.x.CompareTo(b.normalizedCenter.x);

        if (xCompare != 0)
            return xCompare;

        return string.CompareOrdinal(a.geoid, b.geoid);
    }
}
