using UnityEngine;

[System.Serializable]
public struct SpriteRange {
    public Sprite sprite;
    public int min;
    public int max;
}

[CreateAssetMenu(fileName = "New Tile Type", menuName = "Tile Type")]
public class TileType : ScriptableObject
{
    public string tileName;
    public SpriteRange[] sprites;
    public int soilCapacity;
}
