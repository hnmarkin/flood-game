using System;
using System.Collections.Generic;
using UnityEngine;

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
