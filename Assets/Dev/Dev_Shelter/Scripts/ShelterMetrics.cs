using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Shelter mitigation calculations. This preserves the current formula:
/// Capacity Coverage x Shelter Safety x Route Accessibility.
/// </summary>
public static class ShelterMetrics
{
    public static PlacedShelterData BuildPlacedShelterData(
        ShelterTypeDefinition shelterType,
        Vector3Int tileCell,
        Vector3 worldPosition,
        IList<string> associatedZoneGeoids,
        IDictionary<string, int> associatedZonePopulations,
        float candidateTileSafety,
        float defaultRouteAccessibility)
    {
        List<string> normalizedGeoids = NormalizeGeoids(associatedZoneGeoids);
        int totalAssociatedPopulation = CalculateAssociatedPopulation(normalizedGeoids, associatedZonePopulations);
        int capacity = Mathf.Max(0, shelterType != null ? shelterType.capacity : 0);
        float capacityCoverage = totalAssociatedPopulation > 0
            ? Mathf.Clamp01((float)capacity / totalAssociatedPopulation)
            : 0f;
        float safetyMultiplier = shelterType != null ? shelterType.safetyMultiplier : 0f;
        float shelterSafety = Mathf.Clamp01(Mathf.Clamp01(candidateTileSafety) * Mathf.Max(0f, safetyMultiplier));
        float routeAccessibility = Mathf.Clamp01(defaultRouteAccessibility);
        float mitigationScore = Mathf.Clamp01(capacityCoverage * shelterSafety * routeAccessibility);
        int estimatedPeopleProtected = Mathf.RoundToInt(totalAssociatedPopulation * mitigationScore);
        int exposedPopulationAfterShelter = Mathf.Max(0, totalAssociatedPopulation - estimatedPeopleProtected);

        return new PlacedShelterData
        {
            shelterId = Guid.NewGuid().ToString("N"),
            shelterTypeName = shelterType == null || string.IsNullOrWhiteSpace(shelterType.shelterTypeName)
                ? "Shelter"
                : shelterType.shelterTypeName.Trim(),
            tileCell = tileCell,
            worldPosition = worldPosition,
            associatedZoneGeoids = normalizedGeoids,
            capacity = capacity,
            shelterSafety = shelterSafety,
            capacityCoverage = capacityCoverage,
            routeAccessibility = routeAccessibility,
            shelterMitigationScore = mitigationScore,
            estimatedPeopleProtected = estimatedPeopleProtected,
            exposedPopulationAfterShelter = exposedPopulationAfterShelter,
            exposureReductionPercent = mitigationScore,
            isActive = true,
        };
    }

    public static float CalculateShelterSafety(
        ShelterTileSnapshot snapshot,
        float minElevation,
        float maxElevation,
        float deepFloodWaterThreshold)
    {
        if (snapshot.isWaterCategory)
            return 0f;

        float elevationRange = Mathf.Max(0.0001f, maxElevation - minElevation);
        float elevationScore = Mathf.Clamp01((snapshot.elevation - minElevation) / elevationRange);
        float waterSafety = 1f - Mathf.Clamp01(snapshot.waterHeight / Mathf.Max(0.01f, deepFloodWaterThreshold));

        return Mathf.Clamp01((0.65f + (0.35f * elevationScore)) * waterSafety);
    }

    public static int CalculateAssociatedPopulation(
        IList<string> associatedGeoids,
        IDictionary<string, int> associatedZonePopulations)
    {
        int totalPopulation = 0;

        if (associatedGeoids == null)
            return totalPopulation;

        for (int i = 0; i < associatedGeoids.Count; i++)
        {
            string geoid = NormalizeGeoid(associatedGeoids[i]);

            if (associatedZonePopulations != null && associatedZonePopulations.TryGetValue(geoid, out int population))
                totalPopulation += Mathf.Max(0, population);
        }

        return totalPopulation;
    }

    public static void RebuildZoneMitigationCache(
        IList<PlacedShelterData> placedShelters,
        IReadOnlyDictionary<string, int> zonePopulationByGeoid,
        IDictionary<string, float> zoneMitigationByGeoid,
        IDictionary<string, int> protectedPopulationByGeoid)
    {
        zoneMitigationByGeoid.Clear();
        protectedPopulationByGeoid.Clear();

        if (placedShelters == null)
            return;

        for (int i = 0; i < placedShelters.Count; i++)
        {
            PlacedShelterData shelter = placedShelters[i];

            if (shelter == null || !shelter.isActive || shelter.associatedZoneGeoids == null)
                continue;

            for (int zoneIndex = 0; zoneIndex < shelter.associatedZoneGeoids.Count; zoneIndex++)
            {
                string geoid = NormalizeGeoid(shelter.associatedZoneGeoids[zoneIndex]);

                if (string.IsNullOrEmpty(geoid))
                    continue;

                float existingMitigation = zoneMitigationByGeoid.TryGetValue(geoid, out float mitigation)
                    ? mitigation
                    : 0f;
                float combinedMitigation = Mathf.Clamp01(existingMitigation + shelter.shelterMitigationScore);
                zoneMitigationByGeoid[geoid] = combinedMitigation;

                int zonePopulation = zonePopulationByGeoid != null &&
                                     zonePopulationByGeoid.TryGetValue(geoid, out int population)
                    ? Mathf.Max(0, population)
                    : 0;
                int addedProtectedPopulation = Mathf.RoundToInt(zonePopulation * shelter.shelterMitigationScore);
                int existingProtectedPopulation = protectedPopulationByGeoid.TryGetValue(geoid, out int protectedPopulation)
                    ? protectedPopulation
                    : 0;
                protectedPopulationByGeoid[geoid] = Mathf.Min(zonePopulation, existingProtectedPopulation + addedProtectedPopulation);
            }
        }
    }

    public static string GetShelterInfoText(PlacedShelterData shelter)
    {
        if (shelter == null)
            return "Shelter information unavailable.";

        StringBuilder builder = new();
        builder.AppendLine($"Shelter Type: {shelter.shelterTypeName}");
        builder.AppendLine($"Associated Zones: {string.Join(", ", shelter.associatedZoneGeoids)}");
        builder.AppendLine($"Shelter Capacity Coverage: {shelter.capacityCoverage:P0}");
        builder.AppendLine($"Estimated People Protected: {shelter.estimatedPeopleProtected}");
        builder.AppendLine($"Exposed Population: {shelter.exposedPopulationAfterShelter}");
        builder.AppendLine($"Exposure Reduced By: {shelter.exposureReductionPercent:P0}");
        builder.Append("Mitigation = Capacity Coverage x Shelter Safety x Route Accessibility");
        return builder.ToString();
    }

    public static List<string> NormalizeGeoids(IList<string> geoids)
    {
        List<string> normalized = new();
        HashSet<string> seen = new();

        if (geoids == null)
            return normalized;

        for (int i = 0; i < geoids.Count; i++)
        {
            string geoid = NormalizeGeoid(geoids[i]);

            if (string.IsNullOrEmpty(geoid) || !seen.Add(geoid))
                continue;

            normalized.Add(geoid);
        }

        return normalized;
    }

    public static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? string.Empty : geoid.Trim();
    }
}
