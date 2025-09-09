using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TileRuleMap", menuName = "ScriptableObjects/TileRuleMap", order = 1)]
public class TileRuleMap : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public TerrainType terrainType;
        public Vector2Int waterRange;
        public byte elevation;
        public TileBase tile;
    }
    public List<Entry> entries;

    public TileBase Resolve(TerrainType t, int h)
    {
        // Find the first matching rule and return that tile type
        foreach (var e in entries)
        {
            if (e.terrainType == t && h >= e.waterRange.x && h <= e.waterRange.y)
            {
                return e.tile;
            }
        }
        Debug.LogError($"[TileRuleMap] No matching rule for terrain type {t} with water level {h}");
        return null; // Default to input terrain type if no match found
    }
}
