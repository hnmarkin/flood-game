using UnityEngine;
using UnityEngine.Tilemaps;

public class FloodTilemapRenderer : MonoBehaviour
{
    public FloodSimData simulation;
    public Tilemap tilemap;
    public TileBase[] waterTiles; // index 0 = terrain, index 1 = dry water, index N = deep water

    public float maxWaterDepth = 1.0f; // normalize against this

    void Start()
    {
        // Subscribe to the simulation step event
        if (simulation != null)
        {
            simulation.OnSimulationStep += UpdateTilemap;
        }
        
        // Initial update
        UpdateTilemap();
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (simulation != null)
        {
            simulation.OnSimulationStep -= UpdateTilemap;
        }
    }

    public void UpdateTilemap()
    {
        if (simulation == null || tilemap == null || simulation.water == null) return;

        tilemap.ClearAllTiles();

        int N = simulation.N;

        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                // Check if there's terrain at this position
                if (simulation.terrain != null && simulation.terrain[x, y] > 0f)
                {
                    // Use the first tile (index 0) for terrain
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(tilePos, waterTiles[0]);
                }
                else
                {
                    // Water tiles start at index 1, so add 1 to the calculation
                    float depth = simulation.water[x, y];
                    int tileIndex = Mathf.Clamp(Mathf.FloorToInt(depth / maxWaterDepth * (waterTiles.Length - 2)) + 1, 1, waterTiles.Length - 1);

                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(tilePos, waterTiles[tileIndex]);
                }
            }
        }
    }
}
