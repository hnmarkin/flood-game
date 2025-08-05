using UnityEngine;
using UnityEngine.Tilemaps;

public class ElevationRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloodSimulationManager simulationManager;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Tilemap waterTilemap;
    
    [Header("Elevation Settings")]
    [SerializeField] [Range(1f, 50f)] private float elevationYMultiplier = 10f; // How much Y space per elevation unit
    [SerializeField] private bool autoUpdateFromSimulation = true;
    
    [Header("Terrain Data")]
    [SerializeField] private TerrainData terrainDataSource; // Will read terrain tiles from here
    [SerializeField] private TileBase fallbackTerrainTile; // Used if no terrain data or matching tile found
    
    [Header("Water Tiles")]
    [SerializeField] private TileBase transparentWaterTile;  // 0.00 - 0.33: shallow water
    [SerializeField] private TileBase lightWaterTile;        // 0.33 - 0.66: medium water  
    [SerializeField] private TileBase deepWaterTile;         // 0.66 - 1.00: deep water
    
    private void Awake()
    {
        // Try to find components if not assigned
        if (simulationManager == null)
            simulationManager = FindObjectOfType<FloodSimulationManager>();
        
        if (terrainTilemap == null)
            terrainTilemap = GetComponent<Tilemap>();
            
        // Try to find water tilemap (should be a child or sibling with higher sorting order)
        if (waterTilemap == null)
        {
            // Look for a tilemap with "water" in the name
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            foreach (var tm in tilemaps)
            {
                if (tm.name.ToLower().Contains("water"))
                {
                    waterTilemap = tm;
                    break;
                }
            }
        }
            
        // Try to find TerrainData if not assigned
        if (terrainDataSource == null)
            terrainDataSource = Resources.FindObjectsOfTypeAll<TerrainData>().Length > 0 ? 
                Resources.FindObjectsOfTypeAll<TerrainData>()[0] : null;
    }
    
    private void Start()
    {
        if (autoUpdateFromSimulation && simulationManager != null)
        {
            // Subscribe to simulation events
            simulationManager.OnSimulationStep += UpdateElevationVisualization;
            simulationManager.OnSimulationInitialized += UpdateElevationVisualization;
        }
        
        // Initial update
        UpdateElevationVisualization();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (simulationManager != null)
        {
            simulationManager.OnSimulationStep -= UpdateElevationVisualization;
            simulationManager.OnSimulationInitialized -= UpdateElevationVisualization;
        }
    }
    
    public void UpdateElevationVisualization()
    {
        if (!autoUpdateFromSimulation || simulationManager == null || !simulationManager.IsInitialized)
            return;
        
        var simulationData = simulationManager.SimulationData;
        if (simulationData == null || simulationData.water == null || simulationData.terrain == null)
            return;
        
        if (terrainTilemap == null)
        {
            Debug.LogWarning("[ElevationRenderer] No terrain tilemap assigned!");
            return;
        }
        
        if (waterTilemap == null)
        {
            Debug.LogWarning("[ElevationRenderer] No water tilemap assigned!");
            return;
        }
        
        // Clear existing tiles
        terrainTilemap.ClearAllTiles();
        waterTilemap.ClearAllTiles();
        
        int N = simulationData.N;
        
        // Render each cell
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                // Account for boundary cells - actual data is at [x+1, y+1]
                int simX = x + 1;
                int simY = y + 1;
                
                float terrainHeight = simulationData.terrain[simX, simY];
                float waterDepth = simulationData.water[simX, simY];
                
                RenderCellAtElevation(x, y, terrainHeight, waterDepth);
            }
        }
    }
    
    private void RenderCellAtElevation(int gridX, int gridY, float terrainHeight, float waterDepth)
    {
        // Calculate terrain elevation position using Z-as-Y approach
        int terrainYOffset = Mathf.RoundToInt(terrainHeight * elevationYMultiplier);
        Vector3Int terrainPosition = new Vector3Int(gridX, gridY + terrainYOffset, 0);
        
        // Place terrain tile on terrain tilemap
        TileBase terrainTile = GetTerrainTileForHeight(terrainHeight);
        if (terrainTile != null)
        {
            terrainTilemap.SetTile(terrainPosition, terrainTile);
        }
        
        // Place water tile on water tilemap at the same position as terrain (water overlay)
        if (waterDepth > 0.01f) // Only show significant water depth
        {
            // Water goes on the same position as terrain - it's just an overlay
            Vector3Int waterPosition = terrainPosition;
            
            TileBase waterTile = GetWaterTileForDepth(waterDepth);
            if (waterTile != null)
            {
                waterTilemap.SetTile(waterPosition, waterTile);
            }
        }
    }
    
    private TileBase GetTerrainTileForHeight(float height)
    {
        // Try to get tile from TerrainData first
        if (terrainDataSource != null && terrainDataSource.TerrainTypesList != null && terrainDataSource.TerrainTypesList.Count > 0)
        {
            TileBase bestTile = null;
            float closestDistance = float.MaxValue;
            
            // Find the terrain type with height closest to the input height
            foreach (var terrainType in terrainDataSource.TerrainTypesList)
            {
                if (terrainType.tile == null) continue;
                
                float distance = Mathf.Abs(terrainType.height - height);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTile = terrainType.tile;
                }
            }
            
            if (bestTile != null)
                return bestTile;
        }
        
        // Fallback to manual tile if TerrainData doesn't have a match
        return fallbackTerrainTile;
    }
    
    private TileBase GetWaterTileForDepth(float waterDepth)
    {
        // Normalize water depth and select appropriate tile
        float normalizedDepth = Mathf.Clamp01(waterDepth);
        
        if (normalizedDepth < 0.33f)
        {
            return transparentWaterTile; // Shallow water
        }
        else if (normalizedDepth < 0.66f)
        {
            return lightWaterTile; // Medium water
        }
        else
        {
            return deepWaterTile; // Deep water
        }
    }
    
    // Public methods for manual control
    public void ManualUpdate()
    {
        UpdateElevationVisualization();
    }
    
    public void EnableAutoUpdate()
    {
        autoUpdateFromSimulation = true;
    }
    
    public void DisableAutoUpdate()
    {
        autoUpdateFromSimulation = false;
    }
    
    public void SetElevationMultiplier(float multiplier)
    {
        elevationYMultiplier = Mathf.Max(1f, multiplier);
        UpdateElevationVisualization(); // Refresh with new multiplier
    }
    
    // Utility method to set a terrain tile at a specific grid position and height
    public void SetTerrainTileAtPosition(int gridX, int gridY, float height, TileBase tile = null)
    {
        int yOffset = Mathf.RoundToInt(height * elevationYMultiplier);
        Vector3Int position = new Vector3Int(gridX, gridY + yOffset, 0);
        
        // Use provided tile, or get appropriate tile for height
        TileBase tileToUse = tile ?? GetTerrainTileForHeight(height);
        terrainTilemap.SetTile(position, tileToUse);
    }
    
    // Utility method to set a water tile at a specific grid position and height
    public void SetWaterTileAtPosition(int gridX, int gridY, float height, float waterDepth)
    {
        int yOffset = Mathf.RoundToInt(height * elevationYMultiplier);
        Vector3Int position = new Vector3Int(gridX, gridY + yOffset, 0);
        
        TileBase waterTile = GetWaterTileForDepth(waterDepth);
        if (waterTile != null)
        {
            waterTilemap.SetTile(position, waterTile);
        }
    }
    
    // Utility method to clear all tiles in an area
    public void ClearArea(int startX, int startY, int width, int height)
    {
        BoundsInt area = new BoundsInt(startX, startY, 0, width, height, 1);
        terrainTilemap.SetTilesBlock(area, new TileBase[width * height]);
        waterTilemap.SetTilesBlock(area, new TileBase[width * height]);
    }
    
#if UNITY_EDITOR
    [ContextMenu("Update Elevation Visualization")]
    public void UpdateElevationVisualizationFromMenu()
    {
        UpdateElevationVisualization();
    }
    
    [ContextMenu("Clear All Tiles")]
    public void ClearAllTilesFromMenu()
    {
        if (terrainTilemap != null)
            terrainTilemap.ClearAllTiles();
        if (waterTilemap != null)
            waterTilemap.ClearAllTiles();
    }
#endif
}
