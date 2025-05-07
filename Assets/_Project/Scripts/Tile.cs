using UnityEngine;
using UnityEngine.Tilemaps;

public class Tile : MonoBehaviour
{
    [SerializeField] Sprite big;
    
    [Header("Grid Coordinates")]
    public int gridX;
    public int gridY;

    [Header("Terrain and Water Settings")]
    public float terrainHeight = 1f;
    public float waterVolume = 0f;  // meter·miles²
    public float tileArea = 1f;     // miles²

    [Header("Status Flags")]
    public bool isOverflowing = false;
    public bool spread = true;

    public bool ishalf = true;
    // Derived property: water height = terrain + (waterVolume / tileArea)
    public float WaterHeight => terrainHeight + (waterVolume / tileArea);

    // Convenience property for checking if water is present
    public bool HasWater => waterVolume > 0f;

    // Optionally, store references to neighbors for quick lookup
    public Tile[] neighbors = new Tile[3];
    
    public SpriteRenderer rend;
    private void Start()
    {
        rend = GetComponent<SpriteRenderer>();

    }
    public void upgradeWater()
    {
        if (!isOverflowing)
        {
            rend.sprite = big;
            isOverflowing = true;
        }


    }
    public void addWater(float water)
    {
        waterVolume += water;
        if (waterVolume >= 3f)
        {
            isOverflowing = true;
            foreach (var neighbor in neighbors)
            {
                //addWater(waterVolume - 3f);
            }
            waterVolume = 3f;
        }
    }   
    
    
    // You could add methods here to apply water modifications,
    // update visuals, etc.
}
