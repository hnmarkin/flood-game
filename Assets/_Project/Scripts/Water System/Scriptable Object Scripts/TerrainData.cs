using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TerrainData", menuName = "Flood/Terrain Data")]
public class TerrainData : ScriptableObject
{
    [Header("Loaded Tile Data")]
    [SerializeField] private List<Vector3Int> tilePositions = new List<Vector3Int>();
    [SerializeField] private List<int> tileElevations = new List<int>();
    
    [Header("Debug Status")]
    [SerializeField] private bool dataLoaded = false;
    [SerializeField] private int totalTilesWritten = 0;
    [SerializeField] private string lastOperationResult = "No operation performed";
    [SerializeField] private int minElevation = int.MaxValue;
    [SerializeField] private int maxElevation = int.MinValue;
    
    // Properties for data access
    public List<Vector3Int> TilePositions => tilePositions;
    public List<int> TileElevations => tileElevations;
    
    public bool DataLoaded 
    { 
        get => dataLoaded; 
        set => dataLoaded = value; 
    }
    
    public int TotalTilesWritten 
    { 
        get => totalTilesWritten; 
        set => totalTilesWritten = value; 
    }
    
    public string LastOperationResult 
    { 
        get => lastOperationResult; 
        set => lastOperationResult = value; 
    }
    
    public int MinElevation => minElevation;
    public int MaxElevation => maxElevation;
    
    /// <summary>
    /// Clears all stored tile data
    /// </summary>
    public void ClearData()
    {
        tilePositions.Clear();
        tileElevations.Clear();
        dataLoaded = false;
        totalTilesWritten = 0;
        minElevation = int.MaxValue;
        maxElevation = int.MinValue;
        lastOperationResult = "Data cleared";
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// Adds a tile with its position and elevation (z-value)
    /// </summary>
    /// <param name="position">The 3D position of the tile (including z for elevation)</param>
    /// <param name="elevation">The elevation value (typically the z component)</param>
    public void AddTile(Vector3Int position, int elevation)
    {
        tilePositions.Add(position);
        tileElevations.Add(elevation);
        
        // Update min/max elevation tracking
        if (elevation < minElevation) minElevation = elevation;
        if (elevation > maxElevation) maxElevation = elevation;
        
        totalTilesWritten++;
    }
    
    /// <summary>
    /// Gets the elevation at a specific 2D position (ignoring z)
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>Elevation value, or 0 if no tile found at that position</returns>
    public int GetElevationAt(int x, int y)
    {
        for (int i = 0; i < tilePositions.Count; i++)
        {
            if (tilePositions[i].x == x && tilePositions[i].y == y)
            {
                return tileElevations[i];
            }
        }
        return 0; // Default elevation if no tile found
    }
    
    /// <summary>
    /// Gets the elevation at a specific 2D position using Vector2Int
    /// </summary>
    /// <param name="position">2D position to check</param>
    /// <returns>Elevation value, or 0 if no tile found at that position</returns>
    public int GetElevationAt(Vector2Int position)
    {
        return GetElevationAt(position.x, position.y);
    }
    
    /// <summary>
    /// Converts the stored elevation data to a 2D float array for simulation use
    /// </summary>
    /// <param name="gridWidth">Target grid width</param>
    /// <param name="gridHeight">Target grid height</param>
    /// <param name="offsetX">X offset to apply when mapping positions</param>
    /// <param name="offsetY">Y offset to apply when mapping positions</param>
    /// <param name="elevationScale">Scale factor to convert integer elevations to float heights</param>
    /// <returns>2D array of terrain heights</returns>
    public float[,] ConvertToHeightArray(int gridWidth, int gridHeight, int offsetX = 0, int offsetY = 0, float elevationScale = 1.0f)
    {
        float[,] heightArray = new float[gridWidth, gridHeight];
        
        for (int i = 0; i < tilePositions.Count && i < tileElevations.Count; i++)
        {
            Vector3Int tilePos = tilePositions[i];
            int elevation = tileElevations[i];
            
            // Apply offset and check bounds
            int gridX = tilePos.x + offsetX;
            int gridY = tilePos.y + offsetY;
            
            if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
            {
                heightArray[gridX, gridY] = elevation * elevationScale;
            }
        }
        
        return heightArray;
    }
    
    /// <summary>
    /// Gets bounds of all stored tile positions
    /// </summary>
    /// <returns>BoundsInt representing the area covered by all tiles</returns>
    public BoundsInt GetTileBounds()
    {
        if (tilePositions.Count == 0)
            return new BoundsInt(0, 0, 0, 0, 0, 0);
            
        int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;
        
        foreach (var pos in tilePositions)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;
        }
        
        return new BoundsInt(minX, minY, minZ, maxX - minX + 1, maxY - minY + 1, maxZ - minZ + 1);
    }
    
    /// <summary>
    /// Validates and updates the data status
    /// </summary>
    public void ValidateData()
    {
        if (tilePositions.Count != tileElevations.Count)
        {
            Debug.LogWarning($"[NewTerrainData] Data mismatch: {tilePositions.Count} positions but {tileElevations.Count} elevations");
            lastOperationResult = "Data validation failed: position/elevation count mismatch";
            return;
        }
        
        dataLoaded = tilePositions.Count > 0;
        totalTilesWritten = tilePositions.Count;
        
        if (dataLoaded)
        {
            // Recalculate min/max elevations
            minElevation = int.MaxValue;
            maxElevation = int.MinValue;
            
            foreach (int elevation in tileElevations)
            {
                if (elevation < minElevation) minElevation = elevation;
                if (elevation > maxElevation) maxElevation = elevation;
            }
            
            lastOperationResult = $"Data validated: {totalTilesWritten} tiles, elevation range [{minElevation}, {maxElevation}]";
        }
        else
        {
            minElevation = 0;
            maxElevation = 0;
            lastOperationResult = "No data loaded";
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}
