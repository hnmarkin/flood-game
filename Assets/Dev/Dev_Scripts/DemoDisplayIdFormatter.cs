using System;
using System.Collections.Generic;

public static class DemoDisplayIdFormatter
{
    private static readonly Dictionary<string, string> ZoneDisplayIds = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> ShelterDisplayIds = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> EvacuationRouteDisplayIds = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> TowerDisplayIds = new(StringComparer.Ordinal);

    public static string GetZoneDisplayId(string geoid)
    {
        return GetDisplayId(ZoneDisplayIds, geoid, "Z", "Unknown Zone");
    }

    public static string GetShelterDisplayId(string shelterId)
    {
        return GetDisplayId(ShelterDisplayIds, shelterId, "S", "Unknown Shelter");
    }

    public static string GetEvacuationRouteDisplayId(string routeId)
    {
        return GetDisplayId(EvacuationRouteDisplayIds, routeId, "E", "Unknown Route");
    }

    public static string GetTowerDisplayId(string towerId)
    {
        return GetDisplayId(TowerDisplayIds, towerId, "T", "Unknown Tower");
    }

    private static string GetDisplayId(
        Dictionary<string, string> displayIdsByOriginalId,
        string originalId,
        string prefix,
        string fallback)
    {
        originalId = NormalizeId(originalId);

        if (string.IsNullOrEmpty(originalId))
            return fallback;

        if (IsPlaceholder(originalId))
            return fallback;

        if (IsDisplayId(originalId, prefix))
            return originalId.ToUpperInvariant();

        if (displayIdsByOriginalId.TryGetValue(originalId, out string displayId))
            return displayId;

        displayId = $"{prefix}-{displayIdsByOriginalId.Count + 1:00}";
        displayIdsByOriginalId.Add(originalId, displayId);
        return displayId;
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }

    private static bool IsPlaceholder(string id)
    {
        return id == "--" || string.Equals(id, "N/A", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDisplayId(string id, string prefix)
    {
        return id.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase);
    }
}
