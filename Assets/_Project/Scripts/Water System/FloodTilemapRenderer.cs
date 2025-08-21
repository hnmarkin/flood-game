using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Water renderer that places water tiles at the correct Z-elevation based on terrain data.
/// This renderer uses the z-value terrain system and positions water tiles at the same
/// Z-coordinate as the terrain beneath them.
/// </summary>
public class FloodTilemapRenderer : MonoBehaviour
{
    [SerializeField] private FloodSimulationManager simulationManager;
    public Tilemap tilemap;

    [Header("Terrain Data")]
    [SerializeField] private TerrainData terrainDataSource; // Reference to get elevation data

    [SerializeField] private int z_Booster;

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

        // Try to find TerrainData if not assigned
        if (terrainDataSource == null)
        {
            // Look for TerrainData in the FloodSimData first
            var floodSimData = simulationManager?.SimulationData;
            if (floodSimData != null && floodSimData.TerrainDataSource != null)
            {
                terrainDataSource = floodSimData.TerrainDataSource;
                Debug.Log("[FloodTilemapRenderer] Found TerrainData from FloodSimData: " + terrainDataSource.name);
            }
            else
            {
                // Fallback: find any TerrainData in the project
                var terrainDataAssets = Resources.FindObjectsOfTypeAll<TerrainData>();
                if (terrainDataAssets.Length > 0)
                {
                    terrainDataSource = terrainDataAssets[0];
                    Debug.LogWarning("[FloodTilemapRenderer] No TerrainData assigned, using first found: " + terrainDataSource.name);
                }
                else
                {
                    Debug.LogError("[FloodTilemapRenderer] No TerrainData found! Water will be placed at Z=0.");
                }
            }
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
                
                // Only render water if there's a significant amount
                if (waterDepth > 0.01f)
                {
                    // Select tile based on hardcoded water depth ranges
                    int tileIndex = GetWaterTileIndex(waterDepth);
                    
                    // Get terrain elevation for this position to place water at correct Z level
                    int terrainElevation = GetTerrainElevationAt(x, y);
                    
                    // Convert simulation coordinates to world coordinates for tile placement
                    Vector2Int worldCoords = SimulationToWorldCoordinates(x, y);
                    // Place water one Z-level above the terrain since water tiles are thin planes
                    Vector3Int tilePos = new Vector3Int(worldCoords.x, worldCoords.y, terrainElevation + z_Booster);
                    
                    // Simple debug - just log one tile to see if Z-coordinates are correct
                    if (x == 0 && y == 0)
                    {
                        Debug.Log($"First tile Z: {tilePos.z} (terrain: {terrainElevation} + booster: {z_Booster})");
                    }
                    
                    // Only place a tile if we have a valid tile for this depth
                    if (tileIndex >= 0 && tileIndex < waterTiles.Length && waterTiles[tileIndex] != null)
                    {
                        tilemap.SetTile(tilePos, waterTiles[tileIndex]);
                    }
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
    
    /// <summary>
    /// Gets the terrain elevation at a specific grid position using TerrainData
    /// </summary>
    /// <param name="gridX">Simulation grid X coordinate (0-based)</param>
    /// <param name="gridY">Simulation grid Y coordinate (0-based)</param>
    /// <returns>Terrain elevation (z-value), or 0 if no terrain data available</returns>
    private int GetTerrainElevationAt(int gridX, int gridY)
    {
        if (terrainDataSource == null || !terrainDataSource.DataLoaded)
        {
            return 0; // Default to Z=0 if no terrain data
        }
        
        // Convert simulation grid coordinates to world tilemap coordinates
        // We need to reverse the offset that was applied during terrain loading
        Vector2Int worldCoords = SimulationToWorldCoordinates(gridX, gridY);
        return terrainDataSource.GetElevationAt(worldCoords.x, worldCoords.y);
    }
    
    /// <summary>
    /// Converts simulation grid coordinates to world tilemap coordinates
    /// This reverses the offset applied during terrain data loading
    /// </summary>
    /// <param name="simX">Simulation X coordinate (0-based)</param>
    /// <param name="simY">Simulation Y coordinate (0-based)</param>
    /// <returns>World coordinates that match the original tilemap positions</returns>
    private Vector2Int SimulationToWorldCoordinates(int simX, int simY)
    {
        if (terrainDataSource == null || !terrainDataSource.DataLoaded)
        {
            return new Vector2Int(simX, simY); // Fallback to simulation coords
        }
        
        // Get the bounds of the original terrain data
        BoundsInt bounds = terrainDataSource.GetTileBounds();
        
        // The offset used was to shift the minimum coordinates to 0
        // So to reverse it: worldCoord = simCoord + minCoord
        int worldX = simX + bounds.min.x;
        int worldY = simY + bounds.min.y;
        
        return new Vector2Int(worldX, worldY);
    }

    /// <summary>
    /// Public method to manually set the TerrainData source
    /// </summary>
    /// <param name="terrainData">The TerrainData to use for elevation information</param>
    public void SetTerrainDataSource(TerrainData terrainData)
    {
        terrainDataSource = terrainData;
        UpdateTilemap(); // Refresh with new terrain data
    }

    /// <summary>
    /// Force an update of the tilemap rendering
    /// </summary>
    [ContextMenu("Update Tilemap")]
    public void ForceUpdate()
    {
        UpdateTilemap();
    }
}
