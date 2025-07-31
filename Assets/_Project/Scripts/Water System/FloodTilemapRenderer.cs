using UnityEngine;
using UnityEngine.Tilemaps;

public class FloodTilemapRenderer : MonoBehaviour
{
    [SerializeField] private FloodSimulationManager simulationManager;
    public Tilemap tilemap;

    [Header("Water Tiles: 5 Depth Levels")]
    public TileBase transparentTile;  // 0.00 - 0.05: most transparent
    public TileBase lightTile;        // 0.05 - 0.35: light water
    public TileBase mediumTile;       // 0.35 - 0.65: medium water
    public TileBase deepTile;         // 0.65 - 0.95: deep water
    public TileBase deepestTile;      // 0.95 - 1.00: deepest water

    // Hardcoded water depth ranges for 5 tiles
    private readonly float[] waterRanges = { 0.05f, 0.35f, 0.65f, 0.95f, 1.0f };
    
    // Build tile array from individual fields
    private TileBase[] waterTiles => new TileBase[] { transparentTile, lightTile, mediumTile, deepTile, deepestTile };
    
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
                
                // Get water depth at this position
                float waterDepth = simulationData.water[simX, simY];
                
                // Select tile based on hardcoded water depth ranges
                int tileIndex = GetWaterTileIndex(waterDepth);
                
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                
                // Only place a tile if there's water or terrain
                if (tileIndex >= 0 && tileIndex < waterTiles.Length && waterTiles[tileIndex] != null)
                {
                    tilemap.SetTile(tilePos, waterTiles[tileIndex]);
                }
            }
        }
    }
    
    /// <summary>
    /// Returns the appropriate tile index based on water depth
    /// Uses hardcoded ranges: 0-0.05, 0.05-0.35, 0.35-0.65, 0.65-0.95, 0.95-1.0
    /// </summary>
    private int GetWaterTileIndex(float waterDepth)
    {
        // Clamp depth to valid range
        waterDepth = Mathf.Clamp01(waterDepth);
        
        // Find appropriate tile based on hardcoded ranges
        if (waterDepth <= waterRanges[0]) return 0;      // 0.00 - 0.05: most transparent
        else if (waterDepth <= waterRanges[1]) return 1; // 0.05 - 0.35: light water
        else if (waterDepth <= waterRanges[2]) return 2; // 0.35 - 0.65: medium water
        else if (waterDepth <= waterRanges[3]) return 3; // 0.65 - 0.95: deep water
        else return 4;                                   // 0.95 - 1.00: deepest water
    }
}
