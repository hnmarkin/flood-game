using UnityEngine;
using UnityEngine.Tilemaps;

public class NewTerrainLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NewTerrainData newTerrainData;
    [SerializeField] private Tilemap sourceTilemap;
    
    [Header("Loading Settings")]
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private float elevationScale = 1.0f;
    
    // Public setters for editor and runtime use
    public void SetNewTerrainData(NewTerrainData data) => newTerrainData = data;
    public void SetSourceTilemap(Tilemap tilemap) => sourceTilemap = tilemap;
    
    private void Start()
    {
        if (loadOnStart && newTerrainData != null && sourceTilemap != null)
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
        if (newTerrainData == null)
        {
            Debug.LogError("[NewTerrainLoader] NewTerrainData reference is null");
            return false;
        }
        
        if (tilemap == null)
        {
            Debug.LogError("[NewTerrainLoader] Tilemap is null");
            return false;
        }
        
        // Clear existing data
        newTerrainData.ClearData();
        
        int tilesProcessed = 0;
        BoundsInt bounds = tilemap.cellBounds;
        
        Debug.Log($"[NewTerrainLoader] Processing tilemap bounds: {bounds}");
        
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
                        newTerrainData.AddTile(position, elevation);
                        tilesProcessed++;
                        
                        if (tilesProcessed % 100 == 0)
                        {
                            Debug.Log($"[NewTerrainLoader] Processed {tilesProcessed} tiles...");
                        }
                    }
                }
            }
        }
        
        bool success = tilesProcessed > 0;
        if (success)
        {
            newTerrainData.DataLoaded = true;
            newTerrainData.ValidateData();
            
            Debug.Log($"[NewTerrainLoader] Successfully loaded {tilesProcessed} tiles from tilemap");
            Debug.Log($"[NewTerrainLoader] Elevation range: [{newTerrainData.MinElevation}, {newTerrainData.MaxElevation}]");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(newTerrainData);
            #endif
        }
        else
        {
            Debug.LogWarning("[NewTerrainLoader] No tiles found in tilemap");
            newTerrainData.LastOperationResult = "No tiles found in tilemap";
        }
        
        return success;
    }
    
    /// <summary>
    /// Loads terrain data from tilemap but only considers tiles at a specific z-level
    /// </summary>
    /// <param name="tilemap">The tilemap to read from</param>
    /// <param name="zLevel">The z-level to read tiles from</param>
    /// <param name="useZAsElevation">If true, uses the z-level as elevation. If false, uses a fixed elevation value</param>
    /// <param name="fixedElevation">Fixed elevation value to use when useZAsElevation is false</param>
    /// <returns>True if data was successfully loaded</returns>
    public bool LoadTerrainFromTilemapAtZ(Tilemap tilemap, int zLevel, bool useZAsElevation = true, int fixedElevation = 0)
    {
        if (newTerrainData == null)
        {
            Debug.LogError("[NewTerrainLoader] NewTerrainData reference is null");
            return false;
        }
        
        if (tilemap == null)
        {
            Debug.LogError("[NewTerrainLoader] Tilemap is null");
            return false;
        }
        
        // Clear existing data
        newTerrainData.ClearData();
        
        int tilesProcessed = 0;
        BoundsInt bounds = tilemap.cellBounds;
        
        Debug.Log($"[NewTerrainLoader] Processing tilemap at z-level {zLevel}, bounds: {bounds}");
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, zLevel);
                TileBase tile = tilemap.GetTile(position);
                
                if (tile != null)
                {
                    // Use either the z-level or a fixed elevation value
                    int elevation = useZAsElevation ? zLevel : fixedElevation;
                    newTerrainData.AddTile(position, elevation);
                    tilesProcessed++;
                    
                    if (tilesProcessed % 100 == 0)
                    {
                        Debug.Log($"[NewTerrainLoader] Processed {tilesProcessed} tiles...");
                    }
                }
            }
        }
        
        bool success = tilesProcessed > 0;
        if (success)
        {
            newTerrainData.DataLoaded = true;
            newTerrainData.ValidateData();
            
            Debug.Log($"[NewTerrainLoader] Successfully loaded {tilesProcessed} tiles from z-level {zLevel}");
            Debug.Log($"[NewTerrainLoader] Elevation range: [{newTerrainData.MinElevation}, {newTerrainData.MaxElevation}]");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(newTerrainData);
            #endif
        }
        else
        {
            Debug.LogWarning($"[NewTerrainLoader] No tiles found at z-level {zLevel}");
            newTerrainData.LastOperationResult = $"No tiles found at z-level {zLevel}";
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
        if (newTerrainData == null)
        {
            Debug.LogWarning("[NewTerrainLoader] NewTerrainData is null, returning empty grid");
            return new float[gridWidth, gridHeight];
        }
        
        return newTerrainData.ConvertToHeightArray(gridWidth, gridHeight, offsetX, offsetY, elevationScale);
    }
    
    /// <summary>
    /// Helper method to get tile bounds from the current terrain data
    /// </summary>
    /// <returns>Bounds of all loaded tiles</returns>
    public BoundsInt GetLoadedTileBounds()
    {
        if (newTerrainData == null)
            return new BoundsInt(0, 0, 0, 0, 0, 0);
            
        return newTerrainData.GetTileBounds();
    }
    
    /// <summary>
    /// Debug method to log information about loaded terrain
    /// </summary>
    public void LogTerrainInfo()
    {
        if (newTerrainData == null)
        {
            Debug.Log("[NewTerrainLoader] No terrain data loaded");
            return;
        }
        
        Debug.Log($"[NewTerrainLoader] Terrain Info:");
        Debug.Log($"  - Tiles loaded: {newTerrainData.TotalTilesWritten}");
        Debug.Log($"  - Data loaded: {newTerrainData.DataLoaded}");
        Debug.Log($"  - Elevation range: [{newTerrainData.MinElevation}, {newTerrainData.MaxElevation}]");
        Debug.Log($"  - Tile bounds: {newTerrainData.GetTileBounds()}");
        Debug.Log($"  - Last operation: {newTerrainData.LastOperationResult}");
    }
}
