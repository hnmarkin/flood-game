using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class ShelterZoneOverride
{
    public string geoid;
    public bool shelterPlacementEnabled = true;
}

[Serializable]
public class ShelterCandidateResult
{
    public string candidateId;
    public Vector3Int tileCell;
    public Vector3 worldPosition;
    public List<string> associatedZoneGeoids = new();
    public int zoneCount;
    public float suitabilityScore;
    public bool isValid;
    public string debugReason;
}

[Serializable]
public sealed class ShelterCandidateSettings
{
    [Header("Grouping")]
    [Min(1)] public int minZonesPerShelter = 3;
    [Min(1)] public int maxZonesPerShelter = 5;
    public bool allowSingleZoneShelters;
    public bool allowUndersizedFallbackGroups = true;

    [Header("Zone Overrides")]
    public bool defaultZoneEnabled = true;
    public bool allowDisabledZonesToBeServedByNearbyShelters;
    public List<ShelterZoneOverride> zoneOverrides = new();

    [Header("Candidate Suitability")]
    [Min(0f)] public float deepFloodWaterThreshold = 0.25f;
    [Min(0)] public int fallbackSearchRadiusCells = 4;
    [Min(1)] public int supportFeatureDistanceForMaxScore = 4;
    [Min(1)] public int waterClearanceDistanceForMaxScore = 6;
    [Min(0.01f)] public float centerPreferenceDistanceCells = 10f;
    [Min(0f)] public float elevationWeight = 1.1f;
    [Min(0f)] public float centerWeight = 1.0f;
    [Min(0f)] public float supportFeatureWeight = 0.45f;
    [Min(0f)] public float waterClearanceWeight = 0.55f;
    [Min(0f)] public float waterPenaltyWeight = 0.8f;
    public List<string> waterCategoryKeywords = new()
    {
        "water",
        "river",
        "stream",
        "lake",
        "ocean",
        "flood_source",
    };

    public void Validate()
    {
        minZonesPerShelter = Mathf.Max(1, minZonesPerShelter);
        maxZonesPerShelter = Mathf.Max(minZonesPerShelter, maxZonesPerShelter);
        fallbackSearchRadiusCells = Mathf.Max(0, fallbackSearchRadiusCells);
        supportFeatureDistanceForMaxScore = Mathf.Max(1, supportFeatureDistanceForMaxScore);
        waterClearanceDistanceForMaxScore = Mathf.Max(1, waterClearanceDistanceForMaxScore);
        centerPreferenceDistanceCells = Mathf.Max(0.01f, centerPreferenceDistanceCells);
        deepFloodWaterThreshold = Mathf.Max(0f, deepFloodWaterThreshold);
        elevationWeight = Mathf.Max(0f, elevationWeight);
        centerWeight = Mathf.Max(0f, centerWeight);
        supportFeatureWeight = Mathf.Max(0f, supportFeatureWeight);
        waterClearanceWeight = Mathf.Max(0f, waterClearanceWeight);
        waterPenaltyWeight = Mathf.Max(0f, waterPenaltyWeight);

        if (zoneOverrides == null)
            zoneOverrides = new List<ShelterZoneOverride>();

        if (waterCategoryKeywords == null)
            waterCategoryKeywords = new List<string>();
    }
}

public struct ShelterTileSnapshot
{
    public string geoid;
    public string category;
    public float elevation;
    public float waterHeight;
    public bool isWaterCategory;
}

public sealed class ShelterCandidateContext
{
    public ZoneController ZoneController;
    public JsonMapLoader JsonMapLoader;
    public TileMapData TileMapData;
    public Tilemap TerrainTilemap;
    public Tilemap TargetTilemap;
    public ZoneBaselineRiskController ZoneBaselineRiskController;
    public bool DebugLogs;

    public Tilemap PlacementTilemap
    {
        get
        {
            if (TargetTilemap != null)
                return TargetTilemap;

            if (TerrainTilemap != null)
                return TerrainTilemap;

            return ZoneController != null ? ZoneController.TerrainTilemap : null;
        }
    }

    public bool IsMapReady()
    {
        if (ZoneController == null || JsonMapLoader == null || TileMapData == null)
            return false;

        if (JsonMapLoader.payload == null)
            return false;

        if (JsonMapLoader.cellToRC == null || JsonMapLoader.cellToRC.Count == 0)
            return false;

        if (JsonMapLoader.geoidGrid == null || JsonMapLoader.catGrid == null)
            return false;

        if (TileMapData.N <= 0)
            return false;

        return ZoneController.EnsureZoneIndexReadyForExternalUse();
    }
}
