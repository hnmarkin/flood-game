using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class TerrainLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TerrainData terrainData;
    [SerializeField] private Tilemap sourceTilemap;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
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
            LogOperation(false, "TerrainData reference is null", 0);
            return false;
        }
        
        if (tilemap == null)
        {
            LogOperation(false, "Tilemap is null", 0);
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
        LogOperation(success, success ? "Successfully loaded terrain data" : "No tiles found in tilemap", tilesProcessed);
        
        return success;
    }
    
    /// <summary>
    /// Determines the terrain type based on the tile
    /// Override this method or expand it based on your specific tile system
    /// </summary>
    /// <param name="tile">The tile to analyze</param>
    /// <returns>The terrain type index (0 to terrainTypes-1)</returns>
    private int DetermineTileType(TileBase tile)
    {
        if (tile == null) return 0;
        
        // Simple implementation based on tile name
        // You can expand this based on your specific tile system
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
        if (terrainData == null)
        {
            Debug.LogWarning("[TerrainLoader] TerrainData reference is null");
            return new float[gridWidth, gridHeight];
        }
        
        float[,] heightArray = new float[gridWidth, gridHeight];
        
        for (int i = 0; i < terrainData.TilePositions.Count && i < terrainData.TileValues.Count; i++)
        {
            Vector2Int tilePos = terrainData.TilePositions[i];
            int tileValue = terrainData.TileValues[i];
            
            // Apply offset and check bounds
            int gridX = tilePos.x + offsetX;
            int gridY = tilePos.y + offsetY;
            
            if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
            {
                // Ensure tileValue is within valid range
                if (tileValue >= 0 && tileValue < terrainData.TerrainHeights.Count)
                {
                    heightArray[gridX, gridY] = terrainData.TerrainHeights[tileValue];
                }
            }
        }
        
        return heightArray;
    }
    
    /// <summary>
    /// Debug function to communicate operation results
    /// </summary>
    /// <param name="success">Whether the operation was successful</param>
    /// <param name="message">Result message</param>
    /// <param name="tilesWritten">Number of tiles processed</param>
    public void LogOperation(bool success, string message, int tilesWritten)
    {
        if (terrainData != null)
        {
            terrainData.DataLoaded = success;
            terrainData.TotalTilesWritten = tilesWritten;
            terrainData.LastOperationResult = $"{(success ? "SUCCESS" : "FAILED")}: {message} (Tiles: {tilesWritten})";
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(terrainData);
            #endif
        }
        
        // Also log to Unity console for immediate feedback
        if (success)
        {
            Debug.Log($"[TerrainLoader] {message} (Tiles: {tilesWritten})");
        }
        else
        {
            Debug.LogWarning($"[TerrainLoader] {message} (Tiles: {tilesWritten})");
        }
    }
    
    /// <summary>
    /// Debug function for external classes to check data status
    /// </summary>
    /// <returns>Formatted string with current data status</returns>
    public string GetDataStatus()
    {
        if (terrainData == null) return "TerrainData reference is null";
        
        return $"Data Loaded: {terrainData.DataLoaded}\n" +
               $"Terrain Types: {terrainData.TerrainTypes}\n" +
               $"Total Tiles: {terrainData.TotalTilesWritten}\n" +
               $"Last Operation: {terrainData.LastOperationResult}\n" +
               $"Heights: [{string.Join(", ", terrainData.TerrainHeights)}]";
    }
    
    /// <summary>
    /// Clears all loaded terrain data
    /// </summary>
    public void ClearData()
    {
        if (terrainData != null)
        {
            terrainData.TilePositions.Clear();
            terrainData.TileValues.Clear();
            LogOperation(false, "Data cleared manually", 0);
        }
    }
    
    /// <summary>
    /// Validates that all data is consistent and ready for use
    /// </summary>
    /// <returns>True if data is valid and ready to use</returns>
    public bool ValidateData()
    {
        if (terrainData == null)
        {
            Debug.LogWarning("[TerrainLoader] TerrainData reference is null");
            return false;
        }
        
        bool isValid = terrainData.DataLoaded && 
                      terrainData.TilePositions.Count == terrainData.TileValues.Count && 
                      terrainData.TerrainHeights.Count == terrainData.TerrainTypes &&
                      terrainData.TotalTilesWritten > 0;
        
        if (!isValid)
        {
            string reason = "Unknown validation error";
            if (!terrainData.DataLoaded) reason = "No data loaded";
            else if (terrainData.TilePositions.Count != terrainData.TileValues.Count) reason = "Tile position/value count mismatch";
            else if (terrainData.TerrainHeights.Count != terrainData.TerrainTypes) reason = "Terrain heights count doesn't match terrain types";
            else if (terrainData.TotalTilesWritten <= 0) reason = "No tiles were written";
            
            LogOperation(false, $"Data validation failed: {reason}", terrainData.TotalTilesWritten);
        }
        
        return isValid;
    }
    
    // Unity Events for Inspector buttons
    [ContextMenu("Load Terrain from Tilemap")]
    public void LoadTerrainFromTilemapContextMenu()
    {
        LoadTerrainFromTilemap();
    }
    
    [ContextMenu("Validate Data")]
    public void ValidateDataContextMenu()
    {
        bool isValid = ValidateData();
        Debug.Log($"[TerrainLoader] Validation {(isValid ? "PASSED" : "FAILED")}");
    }
    
    [ContextMenu("Clear Data")]
    public void ClearDataContextMenu()
    {
        ClearData();
    }
    
    [ContextMenu("Print Data Status")]
    public void PrintDataStatusContextMenu()
    {
        Debug.Log($"[TerrainLoader] Data Status:\n{GetDataStatus()}");
    }
    
    private void OnValidate()
    {
        // Show debug info in inspector if enabled
        if (showDebugInfo && terrainData != null)
        {
            Debug.Log($"[TerrainLoader] {GetDataStatus()}");
        }
    }
}
