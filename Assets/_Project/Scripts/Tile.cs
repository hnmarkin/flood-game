using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Grid Coordinates")]
    public int gridX;
    public int gridY;

    [Header("Terrain and Water Settings")]
    public float terrainHeight = 1f;
    public float waterVolume = 0f;  // meter·miles²
    public float tileArea = 1f;     // miles²

    [Header("Status Flags")]
    public bool isOverflowing = false;

    // Derived property: water height = terrain + (waterVolume / tileArea)
    public float WaterHeight => terrainHeight + (waterVolume / tileArea);

    // Convenience property for checking if water is present
    public bool HasWater => waterVolume > 0f;

    // Optionally, store references to neighbors for quick lookup
    public Tile[] neighbors;

    // You could add methods here to apply water modifications,
    // update visuals, etc.
}
