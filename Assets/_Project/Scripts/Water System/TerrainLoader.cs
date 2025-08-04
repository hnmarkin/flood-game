using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class TerrainLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TerrainData terrainData;
    [SerializeField] private Tilemap sourceTilemap;
    
    // Public setters for the editor
    public void SetTerrainData(TerrainData data) => terrainData = data;
    public void SetSourceTilemap(Tilemap tilemap) => sourceTilemap = tilemap;
    
    // Helper methods to access terrain data in convenient ways
    public List<TileBase> GetTerrainTiles()
    {
        if (terrainData == null) return new List<TileBase>();
        
        List<TileBase> tiles = new List<TileBase>();
        foreach (var terrainType in terrainData.TerrainTypesList)
        {
            tiles.Add(terrainType.tile);
        }
        return tiles;
    }
    
    public List<float> GetTerrainHeights()
    {
        if (terrainData == null) return new List<float>();
        
        List<float> heights = new List<float>();
        foreach (var terrainType in terrainData.TerrainTypesList)
        {
            heights.Add(terrainType.height);
        }
        return heights;
    }
    
    /// <summary>
    /// Loads terrain data from the assigned tilemap and stores it in the TerrainData ScriptableObject
    /// </summary>
    /// <returns>True if data was successfully loaded</returns>
    public bool LoadTerrainFromTilemap()
    {
        return LoadTerrainFromTilemap(sourceTilemap);
    }
    
    /// <summary>
    /// Loads terrain data from a specified tilemap and stores it in the TerrainData ScriptableObject
    /// </summary>
    /// <param name="tilemap">The tilemap to read from</param>
    /// <returns>True if data was successfully loaded</returns>
    public bool LoadTerrainFromTilemap(Tilemap tilemap)
    {
        if (terrainData == null)
        {
            Debug.LogError("[TerrainLoader] TerrainData reference is null");
            return false;
        }
        
        if (tilemap == null)
        {
            Debug.LogError("[TerrainLoader] Tilemap is null");
            return false;
        }
        
        // Clear existing data
        terrainData.TilePositions.Clear();
        terrainData.TileValues.Clear();
        
        int tilesProcessed = 0;
        BoundsInt bounds = tilemap.cellBounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(position);
                
                if (tile != null)
                {
                    // Store tile position and determine its type/value
                    Vector2Int pos = new Vector2Int(x, y);
                    terrainData.TilePositions.Add(pos);
                    
                    // Determine tile type based on naming convention
                    int tileValue = DetermineTileType(tile);
                    terrainData.TileValues.Add(tileValue);
                    
                    tilesProcessed++;
                }
            }
        }
        
        bool success = tilesProcessed > 0;
        if (success)
        {
            terrainData.DataLoaded = true;
            terrainData.TotalTilesWritten = tilesProcessed;
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(terrainData);
            #endif
        }
        
        return success;
    }
    
    /// <summary>
    /// Determines the terrain type based on the tile
    /// First tries direct tile comparison, then falls back to name-based detection
    /// </summary>
    /// <param name="tile">The tile to analyze</param>
    /// <returns>The terrain type index (0 to terrainTypes-1)</returns>
    private int DetermineTileType(TileBase tile)
    {
        if (tile == null) return 0;
        if (terrainData == null) return 0;
        
        // First: Try direct tile comparison with configured terrain tiles
        for (int i = 0; i < terrainData.TerrainTypesList.Count; i++)
        {
            if (terrainData.TerrainTypesList[i].tile == tile)
            {
                return i;
            }
        }
        
        // Fallback: Name-based detection (for backward compatibility)
        string tileName = tile.name.ToLower();
        
        if (tileName.Contains("water") || tileName.Contains("blue"))
            return 0; // Water/Low terrain
        else if (tileName.Contains("hill") || tileName.Contains("medium"))
            return Mathf.Min(1, terrainData.TerrainTypes - 1); // Medium terrain
        else if (tileName.Contains("mountain") || tileName.Contains("high"))
            return Mathf.Min(2, terrainData.TerrainTypes - 1); // High terrain
        
        // Default to first terrain type
        return 0;
    }
    
    /// <summary>
    /// Converts the loaded tile data to a 2D height array for use with flood simulation
    /// </summary>
    /// <param name="gridWidth">Width of the target grid</param>
    /// <param name="gridHeight">Height of the target grid</param>
    /// <param name="offsetX">X offset to apply when mapping tiles to grid</param>
    /// <param name="offsetY">Y offset to apply when mapping tiles to grid</param>
    /// <returns>2D array of terrain heights</returns>
    public float[,] ConvertToHeightArray(int gridWidth, int gridHeight, int offsetX = 0, int offsetY = 0)
    {
        return ConvertToHeightArray(terrainData, gridWidth, gridHeight, offsetX, offsetY);
    }
    
    /// <summary>
    /// Converts the specified terrain data to a 2D height array for use with flood simulation
    /// </summary>
    /// <param name="sourceTerrainData">The TerrainData to convert from</param>
    /// <param name="gridWidth">Width of the target grid</param>
    /// <param name="gridHeight">Height of the target grid</param>
    /// <param name="offsetX">X offset to apply when mapping tiles to grid</param>
    /// <param name="offsetY">Y offset to apply when mapping tiles to grid</param>
    /// <returns>2D array of terrain heights</returns>
    public float[,] ConvertToHeightArray(TerrainData sourceTerrainData, int gridWidth, int gridHeight, int offsetX = 0, int offsetY = 0)
    {
        if (sourceTerrainData == null)
        {
            Debug.LogWarning("[TerrainLoader] Provided TerrainData is null");
            return new float[gridWidth, gridHeight];
        }
        
        float[,] heightArray = new float[gridWidth, gridHeight];
        
        for (int i = 0; i < sourceTerrainData.TilePositions.Count && i < sourceTerrainData.TileValues.Count; i++)
        {
            Vector2Int tilePos = sourceTerrainData.TilePositions[i];
            int tileValue = sourceTerrainData.TileValues[i];
            
            // Apply offset and check bounds
            int gridX = tilePos.x + offsetX;
            int gridY = tilePos.y + offsetY;
            
            if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
            {
                // Ensure tileValue is within valid range
                if (tileValue >= 0 && tileValue < sourceTerrainData.TerrainTypesList.Count)
                {
                    heightArray[gridX, gridY] = sourceTerrainData.TerrainTypesList[tileValue].height;
                }
            }
        }
        
        return heightArray;
    }
}
