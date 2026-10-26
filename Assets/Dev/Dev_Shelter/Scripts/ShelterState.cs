using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime state for the staged Dev Shelter refactor. It stores placed shelters
/// and derived lookup caches only; it does not read input or own presentation.
/// </summary>
public sealed class ShelterState
{
    private readonly List<PlacedShelterData> _placedShelters = new();
    private readonly Dictionary<Vector3Int, PlacedShelterData> _shelterByTile = new();
    private readonly Dictionary<string, float> _zoneMitigationByGeoid = new();
    private readonly Dictionary<string, int> _protectedPopulationByGeoid = new();
    private readonly Dictionary<string, int> _zonePopulationByGeoid = new();

    public IReadOnlyList<PlacedShelterData> PlacedShelters => _placedShelters;

    public void Clear()
    {
        _placedShelters.Clear();
        _shelterByTile.Clear();
        _zoneMitigationByGeoid.Clear();
        _protectedPopulationByGeoid.Clear();
        _zonePopulationByGeoid.Clear();
    }

    public bool HasShelterAtTile(Vector3Int tileCell)
    {
        return _shelterByTile.ContainsKey(tileCell);
    }

    public bool TryGetShelterAtTile(Vector3Int tileCell, out PlacedShelterData shelter)
    {
        return _shelterByTile.TryGetValue(tileCell, out shelter);
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

    public bool TryGetActiveShelterForZone(string geoid, out PlacedShelterData shelter)
    {
        shelter = null;
        geoid = ShelterMetrics.NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return false;

        for (int i = 0; i < _placedShelters.Count; i++)
        {
            PlacedShelterData candidate = _placedShelters[i];

            if (candidate == null ||
                !candidate.isActive ||
                candidate.associatedZoneGeoids == null)
            {
                continue;
            }

            for (int zoneIndex = 0; zoneIndex < candidate.associatedZoneGeoids.Count; zoneIndex++)
            {
                if (ShelterMetrics.NormalizeGeoid(candidate.associatedZoneGeoids[zoneIndex]) != geoid)
                    continue;

                shelter = candidate;
                return true;
            }
        }

        return false;
    }

    public bool TryAddShelter(PlacedShelterData shelterData)
    {
        if (shelterData == null)
            return false;

        _placedShelters.Add(shelterData);

        if (!_shelterByTile.ContainsKey(shelterData.tileCell))
            _shelterByTile.Add(shelterData.tileCell, shelterData);

        return true;
    }

    public void CacheZonePopulations(
        IList<string> associatedGeoids,
        IDictionary<string, int> associatedZonePopulations)
    {
        if (associatedGeoids == null || associatedZonePopulations == null)
            return;

        for (int i = 0; i < associatedGeoids.Count; i++)
        {
            string geoid = ShelterMetrics.NormalizeGeoid(associatedGeoids[i]);

            if (string.IsNullOrEmpty(geoid) || !associatedZonePopulations.TryGetValue(geoid, out int population))
                continue;

            int safePopulation = Mathf.Max(0, population);

            if (_zonePopulationByGeoid.TryGetValue(geoid, out int existingPopulation))
                _zonePopulationByGeoid[geoid] = Mathf.Max(existingPopulation, safePopulation);
            else
                _zonePopulationByGeoid.Add(geoid, safePopulation);
        }
    }

    public void RebuildZoneMetrics()
    {
        ShelterMetrics.RebuildZoneMitigationCache(
            _placedShelters,
            _zonePopulationByGeoid,
            _zoneMitigationByGeoid,
            _protectedPopulationByGeoid);
    }

    public float GetShelterMitigationForZone(string geoid)
    {
        geoid = ShelterMetrics.NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return 0f;

        return _zoneMitigationByGeoid.TryGetValue(geoid, out float mitigation)
            ? Mathf.Clamp01(mitigation)
            : 0f;
    }

    public int GetProtectedPopulationForZone(string geoid)
    {
        geoid = ShelterMetrics.NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
            return 0;

        return _protectedPopulationByGeoid.TryGetValue(geoid, out int protectedPopulation)
            ? Mathf.Max(0, protectedPopulation)
            : 0;
    }
}
