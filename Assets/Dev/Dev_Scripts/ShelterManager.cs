using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class ShelterTypeDefinition
{
    public string shelterTypeName = "Community Shelter";
    public Sprite shelterSprite;
    public int capacity = 250;
    public int cost = 100;
    [Range(0f, 2f)] public float safetyMultiplier = 1f;
    [TextArea] public string description;
}

[Serializable]
public class PlacedShelterData
{
    public string shelterId;
    public string shelterTypeName;
    public Vector3Int tileCell;
    public Vector3 worldPosition;
    public List<string> associatedZoneGeoids = new();
    public int capacity;
    public float shelterSafety;
    public float capacityCoverage;
    public float routeAccessibility;
    public float shelterMitigationScore;
    public int estimatedPeopleProtected;
    public int exposedPopulationAfterShelter;
    public float exposureReductionPercent;
    public bool isActive;
}

public class ShelterManager : MonoBehaviour
{
    [Header("Shelter Types")]
    [SerializeField] private List<ShelterTypeDefinition> shelterTypes = new();

    [Header("Placement Rules")]
    [SerializeField] private bool allowMultipleSheltersPerCandidate = false;
    [SerializeField] private bool limitPlacedShelters = true;
    [SerializeField, Min(1)] private int maxPlacedShelters = 3;

    [Header("Mitigation Defaults")]
    [SerializeField, Range(0f, 1f)] private float defaultRouteAccessibility = 1.0f;
    [SerializeField, Range(0f, 1f)] private float defaultShelterSafety = 0.9f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<PlacedShelterData> _placedShelters = new();
    private readonly Dictionary<Vector3Int, PlacedShelterData> _shelterByTile = new();
    private readonly Dictionary<string, float> _zoneMitigationByGeoid = new();
    private readonly Dictionary<string, int> _protectedPopulationByGeoid = new();
    private readonly Dictionary<string, int> _zonePopulationByGeoid = new();

    public event Action OnSheltersChanged;

    public IReadOnlyList<ShelterTypeDefinition> ShelterTypes => shelterTypes;
    public bool AllowMultipleSheltersPerCandidate => allowMultipleSheltersPerCandidate;
    public float DefaultRouteAccessibility => defaultRouteAccessibility;
    public float DefaultShelterSafety => defaultShelterSafety;

    private void Awake()
    {
        EnsureDefaultShelterTypes();
    }

    private void Reset()
    {
        EnsureDefaultShelterTypes();
    }

    private void OnValidate()
    {
        defaultRouteAccessibility = Mathf.Clamp01(defaultRouteAccessibility);
        defaultShelterSafety = Mathf.Clamp01(defaultShelterSafety);
        maxPlacedShelters = Mathf.Max(1, maxPlacedShelters);

        EnsureDefaultShelterTypes();
    }

    public bool TryGetDefaultShelterType(out ShelterTypeDefinition shelterType)
    {
        EnsureDefaultShelterTypes();

        shelterType = shelterTypes.Count > 0 ? shelterTypes[0] : null;
        return shelterType != null;
    }

    public IReadOnlyList<PlacedShelterData> GetAllPlacedShelters()
    {
        return _placedShelters;
    }

    public List<PlacedShelterData> GetActiveShelters()
    {
        List<PlacedShelterData> activeShelters = new();

        for (int i = 0; i < _placedShelters.Count; i++)
        {
            PlacedShelterData shelter = _placedShelters[i];

            if (shelter != null && shelter.isActive)
                activeShelters.Add(shelter);
        }

        return activeShelters;
    }

    public bool HasActiveShelters()
    {
        for (int i = 0; i < _placedShelters.Count; i++)
        {
            PlacedShelterData shelter = _placedShelters[i];

            if (shelter != null && shelter.isActive)
                return true;
        }

        return false;
    }

    public bool CanPlaceMoreShelters()
    {
        if (!limitPlacedShelters)
            return true;

        return GetActiveShelterCount() < maxPlacedShelters;
    }

    public int GetActiveShelterCount()
    {
        int activeShelterCount = 0;

        for (int i = 0; i < _placedShelters.Count; i++)
        {
            PlacedShelterData shelter = _placedShelters[i];

            if (shelter != null && shelter.isActive)
                activeShelterCount++;
        }

        return activeShelterCount;
    }

    public int GetMaxPlacedShelters()
    {
        return maxPlacedShelters;
    }

    public bool HasShelterAtTile(Vector3Int tileCell)
    {
        return _shelterByTile.ContainsKey(tileCell);
    }

    public bool CanPlaceShelterAtTile(Vector3Int tileCell)
    {
        return allowMultipleSheltersPerCandidate || !HasShelterAtTile(tileCell);
    }

    public bool TryGetShelterAtTile(Vector3Int tileCell, out PlacedShelterData shelter)
    {
        return _shelterByTile.TryGetValue(tileCell, out shelter);
    }

    public bool RegisterShelter(
        ShelterTypeDefinition shelterType,
        Vector3Int tileCell,
        Vector3 worldPosition,
        IList<string> associatedZoneGeoids,
        IDictionary<string, int> associatedZonePopulations,
        float candidateTileSafety,
        out PlacedShelterData shelterData)
    {
        shelterData = null;

        if (shelterType == null)
        {
            Debug.LogWarning("[ShelterManager] Cannot register shelter because no shelter type was provided.");
            return false;
        }

        if (!CanPlaceShelterAtTile(tileCell))
        {
            Debug.LogWarning($"[ShelterManager] Cannot register shelter at {tileCell}; a shelter already exists there.");
            return false;
        }

        if (!CanPlaceMoreShelters())
        {
            Debug.LogWarning($"[ShelterManager] Shelter placement blocked. Max placed shelters reached: {maxPlacedShelters}.");
            return false;
        }

        List<string> normalizedGeoids = NormalizeGeoids(associatedZoneGeoids);

        if (normalizedGeoids.Count == 0)
        {
            Debug.LogWarning("[ShelterManager] Cannot register shelter because it has no associated zones.");
            return false;
        }

        int totalAssociatedPopulation = CalculateAssociatedPopulation(normalizedGeoids, associatedZonePopulations);
        int capacity = Mathf.Max(0, shelterType.capacity);
        float capacityCoverage = totalAssociatedPopulation > 0
            ? Mathf.Clamp01((float)capacity / totalAssociatedPopulation)
            : 0f;
        float shelterSafety = Mathf.Clamp01(Mathf.Clamp01(candidateTileSafety) * Mathf.Max(0f, shelterType.safetyMultiplier));

        // Route accessibility is intentionally a placeholder until evacuation-route safety exists.
        float routeAccessibility = Mathf.Clamp01(defaultRouteAccessibility);
        float mitigationScore = Mathf.Clamp01(capacityCoverage * shelterSafety * routeAccessibility);
        int estimatedPeopleProtected = Mathf.RoundToInt(totalAssociatedPopulation * mitigationScore);
        int exposedPopulationAfterShelter = Mathf.Max(0, totalAssociatedPopulation - estimatedPeopleProtected);

        shelterData = new PlacedShelterData
        {
            shelterId = Guid.NewGuid().ToString("N"),
            shelterTypeName = string.IsNullOrWhiteSpace(shelterType.shelterTypeName)
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

        CacheZonePopulations(normalizedGeoids, associatedZonePopulations);
        _placedShelters.Add(shelterData);

        if (!_shelterByTile.ContainsKey(tileCell))
            _shelterByTile.Add(tileCell, shelterData);

        RebuildZoneMitigationCache();

        if (debugLogs)
        {
            Debug.Log($"[ShelterManager] Shelter registered in ShelterManager. Active shelters={GetActiveShelters().Count}.");
            Debug.Log(
                $"[ShelterManager] Shelter placed at {tileCell}. Type={shelterData.shelterTypeName}, " +
                $"Mitigation={shelterData.shelterMitigationScore:P0}, Zones={string.Join(", ", shelterData.associatedZoneGeoids)}");
            Debug.Log("[ShelterManager] " + GetShelterInfoText(shelterData));
        }

        OnSheltersChanged?.Invoke();
        return true;
    }

    public float GetShelterMitigationForZone(string geoid)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return 0f;

        return _zoneMitigationByGeoid.TryGetValue(geoid, out float mitigation)
            ? Mathf.Clamp01(mitigation)
            : 0f;
    }

    public int GetProtectedPopulationForZone(string geoid)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return 0;

        return _protectedPopulationByGeoid.TryGetValue(geoid, out int protectedPopulation)
            ? Mathf.Max(0, protectedPopulation)
            : 0;
    }

    public string GetShelterInfoText(PlacedShelterData shelter)
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

    private void EnsureDefaultShelterTypes()
    {
        if (shelterTypes == null)
            shelterTypes = new List<ShelterTypeDefinition>();

        if (shelterTypes.Count > 0)
            return;

        shelterTypes.Add(new ShelterTypeDefinition
        {
            shelterTypeName = "Community Shelter",
            capacity = 250,
            cost = 100,
            safetyMultiplier = 1f,
            description = "General-purpose emergency shelter."
        });
        shelterTypes.Add(new ShelterTypeDefinition
        {
            shelterTypeName = "Storm Shelter",
            capacity = 150,
            cost = 150,
            safetyMultiplier = 1f,
            description = "Smaller high-safety shelter option."
        });
        shelterTypes.Add(new ShelterTypeDefinition
        {
            shelterTypeName = "School Shelter",
            capacity = 400,
            cost = 200,
            safetyMultiplier = 1f,
            description = "Large-capacity shelter option."
        });
    }

    private int CalculateAssociatedPopulation(
        List<string> associatedGeoids,
        IDictionary<string, int> associatedZonePopulations)
    {
        int totalPopulation = 0;

        for (int i = 0; i < associatedGeoids.Count; i++)
        {
            string geoid = associatedGeoids[i];

            if (associatedZonePopulations != null && associatedZonePopulations.TryGetValue(geoid, out int population))
                totalPopulation += Mathf.Max(0, population);
        }

        return totalPopulation;
    }

    private void CacheZonePopulations(
        List<string> associatedGeoids,
        IDictionary<string, int> associatedZonePopulations)
    {
        if (associatedZonePopulations == null)
            return;

        for (int i = 0; i < associatedGeoids.Count; i++)
        {
            string geoid = associatedGeoids[i];

            if (!associatedZonePopulations.TryGetValue(geoid, out int population))
                continue;

            int safePopulation = Mathf.Max(0, population);

            if (_zonePopulationByGeoid.TryGetValue(geoid, out int existingPopulation))
                _zonePopulationByGeoid[geoid] = Mathf.Max(existingPopulation, safePopulation);
            else
                _zonePopulationByGeoid.Add(geoid, safePopulation);
        }
    }

    private void RebuildZoneMitigationCache()
    {
        _zoneMitigationByGeoid.Clear();
        _protectedPopulationByGeoid.Clear();

        for (int i = 0; i < _placedShelters.Count; i++)
        {
            PlacedShelterData shelter = _placedShelters[i];

            if (shelter == null || !shelter.isActive || shelter.associatedZoneGeoids == null)
                continue;

            for (int zoneIndex = 0; zoneIndex < shelter.associatedZoneGeoids.Count; zoneIndex++)
            {
                string geoid = NormalizeGeoid(shelter.associatedZoneGeoids[zoneIndex]);

                if (string.IsNullOrEmpty(geoid))
                    continue;

                float existingMitigation = _zoneMitigationByGeoid.TryGetValue(geoid, out float mitigation)
                    ? mitigation
                    : 0f;
                float combinedMitigation = Mathf.Clamp01(existingMitigation + shelter.shelterMitigationScore);
                _zoneMitigationByGeoid[geoid] = combinedMitigation;

                int zonePopulation = _zonePopulationByGeoid.TryGetValue(geoid, out int population)
                    ? Mathf.Max(0, population)
                    : 0;
                int addedProtectedPopulation = Mathf.RoundToInt(zonePopulation * shelter.shelterMitigationScore);
                int existingProtectedPopulation = _protectedPopulationByGeoid.TryGetValue(geoid, out int protectedPopulation)
                    ? protectedPopulation
                    : 0;
                _protectedPopulationByGeoid[geoid] = Mathf.Min(zonePopulation, existingProtectedPopulation + addedProtectedPopulation);
            }
        }
    }

    private List<string> NormalizeGeoids(IList<string> geoids)
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

    private static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? string.Empty : geoid.Trim();
    }
}
