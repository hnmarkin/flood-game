using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns population/mitigation caches and preserves ordered greedy shelter allocation.</summary>
internal sealed class EvacuationMitigationCalculator
{
    private readonly Dictionary<string, int> evacuatedPopulationByZone = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> evacuationMitigationByZone = new(StringComparer.Ordinal);
    private int totalEvacuatedPopulation;

    internal float GetEvacuationMitigationForZone(string geoid)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return 0f;

        return evacuationMitigationByZone.TryGetValue(geoid, out float mitigation)
            ? Mathf.Clamp01(mitigation)
            : 0f;
    }

    internal int GetEvacuatedPopulationForZone(string geoid)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return 0;

        return evacuatedPopulationByZone.TryGetValue(geoid, out int evacuatedPopulation)
            ? Mathf.Max(0, evacuatedPopulation)
            : 0;
    }

    internal int GetRemainingPopulationAfterEvacuation(
        string geoid,
        IReadOnlyList<SimpleEvacuationRoute> availableRoutes,
        Func<string, int> getSourceZonePopulation)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return 0;

        int zonePopulation = GetKnownZonePopulation(geoid, availableRoutes, getSourceZonePopulation);
        int evacuatedPopulation = GetEvacuatedPopulationForZone(geoid);
        return Mathf.Max(0, zonePopulation - evacuatedPopulation);
    }

    internal int GetTotalEvacuatedPopulation()
    {
        return Mathf.Max(0, totalEvacuatedPopulation);
    }

    internal void RecalculateEvacuationMitigation(
        IReadOnlyList<SimpleEvacuationRoute> availableRoutes,
        HashSet<string> selectedRouteIds,
        Func<string, int> getSourceZonePopulation,
        bool debugLogs)
    {
        evacuatedPopulationByZone.Clear();
        evacuationMitigationByZone.Clear();
        totalEvacuatedPopulation = 0;

        Dictionary<string, int> remainingCapacityByShelter = new(StringComparer.Ordinal);
        Dictionary<string, int> zonePopulationByGeoid = new(StringComparer.Ordinal);

        for (int i = 0; i < availableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = availableRoutes[i];

            if (route == null ||
                !route.isValid ||
                string.IsNullOrEmpty(route.routeId) ||
                !selectedRouteIds.Contains(route.routeId))
            {
                continue;
            }

            string geoid = NormalizeGeoid(route.sourceZoneGeoid);
            string shelterId = NormalizeGeoid(route.destinationShelterId);

            if (string.IsNullOrEmpty(geoid) || string.IsNullOrEmpty(shelterId))
                continue;

            int zonePopulation = Mathf.Max(0, route.sourceZonePopulation);
            int alreadyEvacuatedFromZone = evacuatedPopulationByZone.TryGetValue(geoid, out int currentZoneEvacuation)
                ? currentZoneEvacuation
                : 0;
            int remainingZonePopulation = Mathf.Max(0, zonePopulation - alreadyEvacuatedFromZone);

            if (!zonePopulationByGeoid.TryGetValue(geoid, out int cachedZonePopulation) || zonePopulation > cachedZonePopulation)
                zonePopulationByGeoid[geoid] = zonePopulation;

            if (!remainingCapacityByShelter.TryGetValue(shelterId, out int remainingCapacity))
                remainingCapacity = Mathf.Max(0, route.destinationShelterCapacity);

            int evacuatedPopulation = Mathf.Min(remainingZonePopulation, remainingCapacity);
            remainingCapacityByShelter[shelterId] = Mathf.Max(0, remainingCapacity - evacuatedPopulation);

            if (evacuatedPopulation <= 0)
                continue;

            evacuatedPopulationByZone[geoid] = alreadyEvacuatedFromZone + evacuatedPopulation;
            totalEvacuatedPopulation += evacuatedPopulation;
        }

        foreach (KeyValuePair<string, int> pair in evacuatedPopulationByZone)
        {
            string geoid = pair.Key;
            int evacuatedPopulation = Mathf.Max(0, pair.Value);
            int zonePopulation = zonePopulationByGeoid.TryGetValue(geoid, out int cachedPopulation)
                ? Mathf.Max(0, cachedPopulation)
                : GetKnownZonePopulation(geoid, availableRoutes, getSourceZonePopulation);

            evacuationMitigationByZone[geoid] = zonePopulation > 0
                ? Mathf.Clamp01((float)evacuatedPopulation / zonePopulation)
                : 0f;
        }

        if (debugLogs)
        {
            Debug.Log($"[EvacuationController] Recalculated evacuation mitigation. Total evacuated population={totalEvacuatedPopulation}.");
            Debug.Log($"[EvacuationController] Total evacuated population after selected routes: {totalEvacuatedPopulation}.");
        }
    }

    private int GetKnownZonePopulation(
        string geoid,
        IReadOnlyList<SimpleEvacuationRoute> availableRoutes,
        Func<string, int> getSourceZonePopulation)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return 0;

        int bestPopulation = 0;

        for (int i = 0; i < availableRoutes.Count; i++)
        {
            SimpleEvacuationRoute route = availableRoutes[i];

            if (route == null || NormalizeGeoid(route.sourceZoneGeoid) != geoid)
                continue;

            bestPopulation = Mathf.Max(bestPopulation, route.sourceZonePopulation);
        }

        return bestPopulation > 0 ? bestPopulation : getSourceZonePopulation(geoid);
    }

    private static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid)
            ? string.Empty
            : geoid.Trim();
    }
}
