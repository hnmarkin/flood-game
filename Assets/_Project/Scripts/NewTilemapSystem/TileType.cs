using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct TileBaseRange {
    public TileBase tileBase;
    public int min;
    public int max;
}

[CreateAssetMenu(fileName = "New Tile Type", menuName = "Tile Type")]
public class TileType : ScriptableObject
{
    public string tileName;
    public TileBaseRange[] tileBases;
    public int soilCapacity;

    public TileBase GetTileForWaterHeight(float h) {
        foreach (var r in tileBases)
            if (h >= r.min && h <= r.max)
                return r.tileBase;
        return null;
    }
}
