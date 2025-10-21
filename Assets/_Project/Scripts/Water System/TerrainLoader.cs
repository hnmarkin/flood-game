using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TerrainData terrainData;
    [SerializeField] private Tilemap sourceTilemap;
    
    [Header("Loading Settings")]
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private float elevationScale = 1.0f;
    
    // Public setters for editor and runtime use
    public void SetTerrainData(TerrainData data) => terrainData = data;
    public void SetSourceTilemap(Tilemap tilemap) => sourceTilemap = tilemap;
    
    private void Start()
    {
        if (loadOnStart && terrainData != null && sourceTilemap != null)
        {
            LoadTerrainFromTilemap();
        }
    }
    
    /// <summary>
    /// Loads terrain data from the assigned tilemap, reading z-values as elevation
    /// </summary>
    /// <returns>True if data was successfully loaded</returns>
    public bool LoadTerrainFromTilemap()
    {
        return LoadTerrainFromTilemap(sourceTilemap);
    }
    
    /// <summary>
    /// Loads terrain data from a specified tilemap, reading z-values as elevation
    /// </summary>
    /// <param name="tilemap">The tilemap to read from</param>
    /// <returns>True if data was successfully loaded</returns>
    public bool LoadTerrainFromTilemap(Tilemap tilemap)
    {
        if (terrainData == null) {
            Debug.LogError("[TerrainLoader] TerrainData reference is null");
            return false;
        }
        
        if (tilemap == null) {
            Debug.LogError("[TerrainLoader] Tilemap is null");
            return false;
        }
        
        // Clear existing data
        terrainData.ClearData();
        
        int tilesProcessed = 0;
        BoundsInt bounds = tilemap.cellBounds;
        
        Debug.Log($"[TerrainLoader] Processing tilemap bounds: {bounds}");
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    Vector3Int position = new Vector3Int(x, y, z);
                    TileBase tile = tilemap.GetTile(position);
                    
                    if (tile != null)
                    {
                        // Store the full 3D position and use z-value as elevation
                        int elevation = z;
                        terrainData.AddTile(position, elevation);
                        tilesProcessed++;
                        
                        if (tilesProcessed % 100 == 0)
                        {
                            Debug.Log($"[TerrainLoader] Processed {tilesProcessed} tiles...");
                        }
                    }
                }
            }
        }
        
        bool success = tilesProcessed > 0;
        if (success)
        {
            terrainData.DataLoaded = true;
            terrainData.ValidateData();
            
            Debug.Log($"[TerrainLoader] Successfully loaded {tilesProcessed} tiles from tilemap");
            Debug.Log($"[TerrainLoader] Elevation range: [{terrainData.MinElevation}, {terrainData.MaxElevation}]");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(terrainData);
            #endif
        }
        else
        {
            Debug.LogWarning("[TerrainLoader] No tiles found in tilemap");
            terrainData.LastOperationResult = "No tiles found in tilemap";
        }
        
        return success;
    }
    
    /// <summary>
    /// Converts the loaded terrain data to a format suitable for the flood simulation
    /// </summary>
    /// <param name="gridWidth">Target simulation grid width</param>
    /// <param name="gridHeight">Target simulation grid height</param>
    /// <param name="offsetX">X offset for coordinate mapping</param>
    /// <param name="offsetY">Y offset for coordinate mapping</param>
    /// <returns>2D float array of terrain heights</returns>
    public float[,] ConvertToSimulationGrid(int gridWidth, int gridHeight, int offsetX = 0, int offsetY = 0)
    {
        if (terrainData == null)
        {
            Debug.LogWarning("[TerrainLoader] TerrainData is null, returning empty grid");
            return new float[gridWidth, gridHeight];
        }
        
        return terrainData.ConvertToHeightArray(gridWidth, gridHeight, offsetX, offsetY, elevationScale);
    }
    
    /// <summary>
    /// Helper method to get tile bounds from the current terrain data
    /// </summary>
    /// <returns>Bounds of all loaded tiles</returns>
    public BoundsInt GetLoadedTileBounds()
    {
        if (terrainData == null)
            return new BoundsInt(0, 0, 0, 0, 0, 0);
            
        return terrainData.GetTileBounds();
    }
    
    /// <summary>
    /// Debug method to log information about loaded terrain
    /// </summary>
    public void LogTerrainInfo()
    {
        if (terrainData == null)
        {
            Debug.Log("[TerrainLoader] No terrain data loaded");
            return;
        }
        
        Debug.Log($"[TerrainLoader] Terrain Info:");
        Debug.Log($"  - Tiles loaded: {terrainData.TotalTilesWritten}");
        Debug.Log($"  - Data loaded: {terrainData.DataLoaded}");
        Debug.Log($"  - Elevation range: [{terrainData.MinElevation}, {terrainData.MaxElevation}]");
        Debug.Log($"  - Tile bounds: {terrainData.GetTileBounds()}");
        Debug.Log($"  - Last operation: {terrainData.LastOperationResult}");
    }
}
