using UnityEngine;
using UnityEngine.Tilemaps;

public class FloodTilemapRenderer : MonoBehaviour
{
    [SerializeField] private FloodSimulationManager simulationManager;
    public Tilemap tilemap;
    public TileBase[] waterTiles; // index 0 = terrain, index 1 = dry water, index N = deep water

    public float maxWaterDepth = 1.0f; // normalize against this

    void Start()
    {
        // Try to find simulation manager if not assigned
        if (simulationManager == null)
        {
            simulationManager = FindObjectOfType<FloodSimulationManager>();
        }

        // Subscribe to the simulation step event
        if (simulationManager != null)
        {
            simulationManager.OnSimulationStep += UpdateTilemap;
            simulationManager.OnSimulationInitialized += UpdateTilemap;
        }
        
        // Initial update
        UpdateTilemap();
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (simulationManager != null)
        {
            simulationManager.OnSimulationStep -= UpdateTilemap;
            simulationManager.OnSimulationInitialized -= UpdateTilemap;
        }
    }

    public void UpdateTilemap()
    {
        if (simulationManager == null || !simulationManager.IsInitialized || tilemap == null) return;

        var simulationData = simulationManager.SimulationData;
        if (simulationData == null || simulationData.water == null) return;

        tilemap.ClearAllTiles();

        int N = simulationData.N;

        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                // Account for boundary cells - actual data is at [x+1, y+1]
                int simX = x + 1;
                int simY = y + 1;
                
                // Check if there's terrain at this position
                if (simulationData.terrain != null && simulationData.terrain[simX, simY] > 0f)
                {
                    // Use the first tile (index 0) for terrain
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(tilePos, waterTiles[0]);
                }
                else
                {
                    // Water tiles start at index 1, so add 1 to the calculation
                    float depth = simulationData.water[simX, simY];
                    int tileIndex = Mathf.Clamp(Mathf.FloorToInt(depth / maxWaterDepth * (waterTiles.Length - 2)) + 1, 1, waterTiles.Length - 1);

                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(tilePos, waterTiles[tileIndex]);
                }
            }
        }
    }
}
