using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Builds routes in provider order into the facade's existing destination list.</summary>
internal sealed class EvacuationRouteGenerator
{
    private struct ZoneAnchor
    {
        public Vector2Int normalizedTile;
        public Vector3Int cell;
        public string preferenceLabel;
    }

    private readonly ShelterController shelterController;
    private readonly ZoneController zoneController;
    private readonly ZoneBaselineRiskController baselineRiskController;
    private readonly Tilemap routeTilemap;
    private readonly EvacuationPathfinder pathfinder;
    private readonly bool skipShortRoutes;
    private readonly int minimumVisibleRouteLengthTiles;
    private readonly bool debugLogs;

    internal EvacuationRouteGenerator(
        ShelterController shelterController,
        ZoneController zoneController,
        ZoneBaselineRiskController baselineRiskController,
        Tilemap routeTilemap,
        EvacuationPathfinder pathfinder,
        bool skipShortRoutes,
        int minimumVisibleRouteLengthTiles,
        bool debugLogs)
    {
        this.shelterController = shelterController;
        this.zoneController = zoneController;
        this.baselineRiskController = baselineRiskController;
        this.routeTilemap = routeTilemap;
        this.pathfinder = pathfinder;
        this.skipShortRoutes = skipShortRoutes;
        this.minimumVisibleRouteLengthTiles = minimumVisibleRouteLengthTiles;
        this.debugLogs = debugLogs;
    }

    internal void GenerateRoutes(List<SimpleEvacuationRoute> destination)
    {
        List<PlacedShelterData> activeShelters = shelterController.GetActiveShelters();
        int activeShelterCount = activeShelters != null ? activeShelters.Count : 0;

        if (debugLogs)
            Debug.Log($"[EvacuationController] Active shelters found: {activeShelterCount}.");

        if (activeShelterCount == 0)
        {
            if (debugLogs)
                Debug.Log("[EvacuationController] Routes built for all active shelters: 0.");

            return;
        }

        if (debugLogs)
            Debug.Log("[EvacuationController] Building evacuation routes for all active shelters.");

        BuildRoutesForAllActiveShelters(activeShelters, destination);

        if (debugLogs)
            Debug.Log($"[EvacuationController] Routes built for all active shelters: {destination.Count}.");
    }

    private void BuildRoutesForAllActiveShelters(List<PlacedShelterData> activeShelters, List<SimpleEvacuationRoute> destination)
    {
        if (activeShelters == null)
            return;

        for (int i = 0; i < activeShelters.Count; i++)
        {
            PlacedShelterData shelter = activeShelters[i];

            if (shelter == null || !shelter.isActive)
                continue;

            if (!TryGetShelterNormalizedTile(shelter, out Vector2Int shelterTile))
            {
                Debug.LogWarning($"[EvacuationController] Could not build routes for shelter {shelter.shelterId} because its tile cell is invalid.");
                continue;
            }

            int associatedZoneCount = shelter.associatedZoneGeoids != null ? shelter.associatedZoneGeoids.Count : 0;
            int routeCountBeforeShelter = destination.Count;

            if (debugLogs)
                Debug.Log($"[EvacuationController] Shelter {shelter.shelterId} serves {associatedZoneCount} zones.");

            BuildRoutesForShelter(shelter, shelterTile, destination);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Routes built for shelter {shelter.shelterId}: {destination.Count - routeCountBeforeShelter}.");
        }
    }

    private void BuildRoutesForShelter(PlacedShelterData shelter, Vector2Int shelterTile, List<SimpleEvacuationRoute> destination)
    {
        if (shelter == null)
            return;

        if (shelter.associatedZoneGeoids == null || shelter.associatedZoneGeoids.Count == 0)
        {
            Debug.LogWarning($"[EvacuationController] Shelter {shelter.shelterId} has no associated zones.");
            return;
        }

        HashSet<string> processedGeoids = new();
        Vector3Int shelterCell = pathfinder.NormalizedTileToCell(shelterTile);

        for (int i = 0; i < shelter.associatedZoneGeoids.Count; i++)
        {
            string geoid = NormalizeGeoid(shelter.associatedZoneGeoids[i]);

            if (string.IsNullOrEmpty(geoid) || !processedGeoids.Add(geoid))
                continue;

            string destinationShelterId = GetSafeShelterId(shelter);

            if (IsShelterInsideZone(geoid, shelterTile))
            {
                if (debugLogs)
                {
                    Debug.Log(
                        $"[EvacuationController] Skipping route for zone {geoid} because shelter {destinationShelterId} is located inside that zone.");
                }

                continue;
            }

            if (debugLogs)
                Debug.Log($"[EvacuationController] Building route from zone {geoid} to shelter {shelter.shelterId}.");

            if (!TryFindZoneCenterAnchor(geoid, out ZoneAnchor anchor))
            {
                Debug.LogWarning($"[EvacuationController] Could not build route. Zone={geoid}, Shelter={shelter.shelterId}.");
                continue;
            }

            if (debugLogs)
                Debug.Log($"[EvacuationController] Zone {geoid} anchor cell selected: {anchor.cell}. Tile preference: {anchor.preferenceLabel}.");

            if (!pathfinder.TryBuildPath(anchor.normalizedTile, shelterTile, out List<Vector3Int> pathCells, out float distanceTiles))
            {
                Debug.LogWarning($"[EvacuationController] Could not build route. Zone={geoid}, Shelter={shelter.shelterId}.");
                continue;
            }

            if (skipShortRoutes && distanceTiles < minimumVisibleRouteLengthTiles)
            {
                if (debugLogs)
                {
                    Debug.Log(
                        $"[EvacuationController] Skipping short route. Zone={geoid}, Shelter={destinationShelterId}, Distance={distanceTiles:0.##}, Minimum={minimumVisibleRouteLengthTiles}.");
                }

                continue;
            }

            SimpleEvacuationRoute route = new()
            {
                routeId = BuildRouteId(geoid, destinationShelterId),
                sourceZoneGeoid = geoid,
                sourceZonePopulation = GetSourceZonePopulation(geoid, baselineRiskController),
                destinationShelterId = destinationShelterId,
                destinationShelterType = GetSafeShelterType(shelter),
                destinationShelterCapacity = GetSafeShelterCapacity(shelter),
                startCell = anchor.cell,
                shelterCell = shelterCell,
                pathCells = pathCells,
                distanceTiles = distanceTiles,
                isValid = pathCells.Count > 0,
            };

            destination.Add(route);

            if (debugLogs)
                Debug.Log($"[EvacuationController] Route built. Zone={geoid}, Shelter={shelter.shelterId}, Cells={pathCells.Count}, Distance={distanceTiles:0.##}.");
        }
    }

    private bool IsShelterInsideZone(string geoid, Vector2Int shelterTile)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid) || zoneController == null)
            return false;

        return zoneController.TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles) &&
               zoneTiles != null &&
               zoneTiles.Contains(shelterTile);
    }

    private bool TryFindZoneCenterAnchor(string geoid, out ZoneAnchor anchor)
    {
        anchor = default;

        if (!zoneController.TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles) || zoneTiles == null || zoneTiles.Count == 0)
            return false;

        Vector2 center = CalculateZoneCenter(zoneTiles);
        bool hasCandidate = false;
        Vector2Int bestTile = default;
        int bestPriority = int.MaxValue;
        float bestDistance = float.MaxValue;
        string bestLabel = "other";

        foreach (Vector2Int tile in zoneTiles)
        {
            if (!TryGetAnchorTilePreference(tile, out string label, out int priority))
                continue;

            float distance = (new Vector2(tile.x, tile.y) - center).sqrMagnitude;

            if (hasCandidate && (distance > bestDistance || (Mathf.Approximately(distance, bestDistance) && priority >= bestPriority)))
                continue;

            bestTile = tile;
            bestPriority = priority;
            bestDistance = distance;
            bestLabel = label;
            hasCandidate = true;
        }

        if (!hasCandidate)
            return false;

        anchor = new ZoneAnchor
        {
            normalizedTile = bestTile,
            cell = pathfinder.NormalizedTileToCell(bestTile),
            preferenceLabel = bestLabel,
        };
        return true;
    }

    private Vector2 CalculateZoneCenter(HashSet<Vector2Int> zoneTiles)
    {
        Vector2 center = Vector2.zero;
        int count = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            center += new Vector2(tile.x, tile.y);
            count++;
        }

        return count > 0 ? center / count : center;
    }

    private bool TryGetAnchorTilePreference(Vector2Int normalizedTile, out string preferenceLabel, out int priority)
    {
        preferenceLabel = "other";
        priority = int.MaxValue;

        if (!pathfinder.TryGetRouteTileSnapshot(normalizedTile, out EvacuationPathfinder.RouteTileSnapshot snapshot) || !EvacuationPathfinder.IsPassable(snapshot))
            return false;

        if (EvacuationPathfinder.IsRoadCategory(snapshot.category))
        {
            preferenceLabel = "road";
            priority = 0;
            return true;
        }

        if (IsBuildingCategory(snapshot.category))
        {
            preferenceLabel = "building";
            priority = 1;
            return true;
        }

        if (IsLandCategory(snapshot.category))
        {
            preferenceLabel = "land";
            priority = 2;
            return true;
        }

        preferenceLabel = "other";
        priority = 3;
        return true;
    }

    private bool TryGetShelterNormalizedTile(PlacedShelterData shelter, out Vector2Int normalizedTile)
    {
        normalizedTile = default;

        if (shelter == null)
            return false;

        if (pathfinder.TryCellToNormalizedTile(shelter.tileCell, out normalizedTile))
            return true;

        if (routeTilemap == null)
            return false;

        Vector3Int fallbackCell = routeTilemap.WorldToCell(shelter.worldPosition);
        return pathfinder.TryCellToNormalizedTile(fallbackCell, out normalizedTile);
    }

    internal static int GetSourceZonePopulation(string geoid, ZoneBaselineRiskController baselineRiskController)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid) || baselineRiskController == null)
            return 0;

        if (!baselineRiskController.HasCalculatedBaselineRisk)
            baselineRiskController.EnsureBaselineRiskCalculated();

        return baselineRiskController.TryGetRiskData(geoid, out ZoneBaselineRiskData riskData)
            ? Mathf.Max(0, riskData.rawPopulation)
            : 0;
    }

    private static string GetSafeShelterId(PlacedShelterData shelter)
    {
        return shelter != null && !string.IsNullOrWhiteSpace(shelter.shelterId)
            ? shelter.shelterId.Trim()
            : "Unknown Shelter";
    }

    private static string GetSafeShelterType(PlacedShelterData shelter)
    {
        return shelter != null && !string.IsNullOrWhiteSpace(shelter.shelterTypeName)
            ? shelter.shelterTypeName.Trim()
            : "Shelter";
    }

    private static int GetSafeShelterCapacity(PlacedShelterData shelter)
    {
        return shelter != null ? Mathf.Max(0, shelter.capacity) : 0;
    }

    private string BuildRouteId(string geoid, string shelterId)
    {
        return $"{NormalizeGeoid(geoid)}_to_{NormalizeGeoid(shelterId)}";
    }

    private static bool IsBuildingCategory(string category)
    {
        return category.Contains("building") ||
               category.Contains("residential") ||
               category.Contains("commercial") ||
               category.Contains("industrial") ||
               category.Contains("city") ||
               category.Contains("shelter");
    }

    private static bool IsLandCategory(string category)
    {
        return category.Contains("land") ||
               category.Contains("grass") ||
               category.Contains("field") ||
               category.Contains("park") ||
               category.Contains("forest") ||
               category.Contains("soil") ||
               category.Contains("terrain");
    }

    private static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid)
            ? string.Empty
            : geoid.Trim();
    }
}
