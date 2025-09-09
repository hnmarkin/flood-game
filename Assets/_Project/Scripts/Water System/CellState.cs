using UnityEngine;
using System;

public enum TerrainType : byte { SAND, GRASS, ROCK, MOUNTAIN }

[Serializable]
public struct CellState
{
    public TerrainType terrainType; // e.g., SAND, GRASS, ROCK, MOUNTAIN
    public byte waterLevel;          // 0..N
    public byte elevation;
}