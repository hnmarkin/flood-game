using UnityEngine;

[CreateAssetMenu(fileName = "New Tile Type", menuName = "Tile Type")]
public class TileType : ScriptableObject
{
    public string tileName;
    public Sprite[] sprites;
    public int soilCapacity;
}
