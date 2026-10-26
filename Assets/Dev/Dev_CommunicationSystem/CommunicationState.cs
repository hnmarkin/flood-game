using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Single runtime owner of communication towers, lookups and statuses.</summary>
internal sealed class CommunicationState
{
    private readonly List<CommunicationTowerData> _communicationTowers = new();
    private readonly Dictionary<Vector3Int, CommunicationTowerData> _towerByCell = new();
    private readonly Dictionary<string, ZoneCommunicationStatus> _zoneStatusByGeoid = new();

    internal bool HasGeneratedTowers { get; set; }
    internal bool IsCommunicationModeActive { get; set; }
    internal IReadOnlyList<CommunicationTowerData> Towers => _communicationTowers;

    // Deliberately retain statuses and mode; generation can fail after this clear.
    internal void ClearTowers()
    {
        _communicationTowers.Clear();
        _towerByCell.Clear();
    }

    internal void AddTower(CommunicationTowerData tower)
    {
        _communicationTowers.Add(tower);
        _towerByCell[tower.tileCell] = tower;
    }

    internal bool TryGetTowerAtCell(Vector3Int cell, out CommunicationTowerData tower)
    {
        return _towerByCell.TryGetValue(cell, out tower);
    }

    internal void InitializeZoneStatuses(List<string> geoids)
    {
        _zoneStatusByGeoid.Clear();

        for (int i = 0; i < geoids.Count; i++)
        {
            string geoid = NormalizeGeoid(geoids[i]);

            if (!string.IsNullOrEmpty(geoid) && !_zoneStatusByGeoid.ContainsKey(geoid))
                _zoneStatusByGeoid.Add(geoid, ZoneCommunicationStatus.Unreached);
        }
    }

    internal bool TryGetCommunicationStatus(string geoid, out ZoneCommunicationStatus status)
    {
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid))
        {
            status = ZoneCommunicationStatus.Unreached;
            return false;
        }

        return _zoneStatusByGeoid.TryGetValue(geoid, out status);
    }

    internal IReadOnlyList<CommunicationTowerData> GetActiveTowers()
    {
        List<CommunicationTowerData> activeTowers = new();

        for (int i = 0; i < _communicationTowers.Count; i++)
        {
            CommunicationTowerData tower = _communicationTowers[i];

            if (tower != null && tower.isActive)
                activeTowers.Add(tower);
        }

        return activeTowers;
    }

    internal ZoneCommunicationStatus GetZoneCommunicationStatus(string geoid)
    {
        return TryGetCommunicationStatus(geoid, out ZoneCommunicationStatus status)
            ? status
            : ZoneCommunicationStatus.Unreached;
    }

    internal Dictionary<string, ZoneCommunicationStatus> GetAllZoneCommunicationStatuses()
    {
        return new Dictionary<string, ZoneCommunicationStatus>(_zoneStatusByGeoid, StringComparer.Ordinal);
    }

    internal bool TryActivateTower(CommunicationTowerData tower)
    {
        if (tower == null || tower.isActive)
            return false;

        tower.isActive = true;
        return true;
    }

    // Apply each transition before yielding its diagnostic outcome. The facade
    // retains the original per-zone log ordering without owning the state loop.
    internal IEnumerable<string> SetTowerZonesPending(CommunicationTowerData tower)
    {
        for (int i = 0; i < tower.associatedZoneGeoids.Count; i++)
        {
            string geoid = NormalizeGeoid(tower.associatedZoneGeoids[i]);

            if (string.IsNullOrEmpty(geoid))
                continue;

            if (!_zoneStatusByGeoid.TryGetValue(geoid, out ZoneCommunicationStatus previousStatus))
                previousStatus = ZoneCommunicationStatus.Unreached;

            if (previousStatus != ZoneCommunicationStatus.Unreached)
                continue;

            _zoneStatusByGeoid[geoid] = ZoneCommunicationStatus.Pending;
            yield return geoid;
        }
    }

    internal bool TryMarkZoneWarned(
        string geoid,
        out string normalizedGeoid,
        out ZoneCommunicationStatus previousStatus)
    {
        normalizedGeoid = NormalizeGeoid(geoid);
        previousStatus = ZoneCommunicationStatus.Unreached;

        if (string.IsNullOrEmpty(normalizedGeoid))
            return false;

        _zoneStatusByGeoid.TryGetValue(normalizedGeoid, out previousStatus);
        _zoneStatusByGeoid[normalizedGeoid] = ZoneCommunicationStatus.Warned;
        return previousStatus != ZoneCommunicationStatus.Warned;
    }

    private string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? null : geoid.Trim();
    }
}
