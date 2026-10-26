using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shelter placement rules and state mutation. It has no input or rendering responsibility.
/// </summary>
public sealed class ShelterPlacement
{
    private readonly ShelterState _state;

    public ShelterPlacement(ShelterState state)
    {
        _state = state;
    }

    public bool CanPlaceMoreShelters(bool limitPlacedShelters, int maxPlacedShelters)
    {
        if (_state == null)
            return false;

        if (!limitPlacedShelters)
            return true;

        return _state.GetActiveShelterCount() < Mathf.Max(1, maxPlacedShelters);
    }

    public bool CanPlaceShelterAtTile(Vector3Int tileCell, bool allowMultipleSheltersPerCandidate)
    {
        return _state != null &&
               (allowMultipleSheltersPerCandidate || !_state.HasShelterAtTile(tileCell));
    }

    public bool TryRegisterShelter(
        ShelterTypeDefinition shelterType,
        Vector3Int tileCell,
        Vector3 worldPosition,
        IList<string> associatedZoneGeoids,
        IDictionary<string, int> associatedZonePopulations,
        float candidateTileSafety,
        float defaultRouteAccessibility,
        bool allowMultipleSheltersPerCandidate,
        bool limitPlacedShelters,
        int maxPlacedShelters,
        out PlacedShelterData shelterData,
        out string failureReason)
    {
        shelterData = null;
        failureReason = null;

        if (_state == null)
        {
            failureReason = "Shelter state is not initialized.";
            return false;
        }

        if (shelterType == null)
        {
            failureReason = "No shelter type was provided.";
            return false;
        }

        if (!CanPlaceShelterAtTile(tileCell, allowMultipleSheltersPerCandidate))
        {
            failureReason = $"A shelter already exists at {tileCell}.";
            return false;
        }

        if (!CanPlaceMoreShelters(limitPlacedShelters, maxPlacedShelters))
        {
            failureReason = $"Max placed shelters reached: {Mathf.Max(1, maxPlacedShelters)}.";
            return false;
        }

        List<string> normalizedGeoids = ShelterMetrics.NormalizeGeoids(associatedZoneGeoids);

        if (normalizedGeoids.Count == 0)
        {
            failureReason = "Shelter has no associated zones.";
            return false;
        }

        shelterData = ShelterMetrics.BuildPlacedShelterData(
            shelterType,
            tileCell,
            worldPosition,
            normalizedGeoids,
            associatedZonePopulations,
            candidateTileSafety,
            defaultRouteAccessibility);

        _state.CacheZonePopulations(normalizedGeoids, associatedZonePopulations);

        if (!_state.TryAddShelter(shelterData))
        {
            shelterData = null;
            failureReason = "Shelter could not be added to runtime state.";
            return false;
        }

        _state.RebuildZoneMetrics();
        return true;
    }
}
