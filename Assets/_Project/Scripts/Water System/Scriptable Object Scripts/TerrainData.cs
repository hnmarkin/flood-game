using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TerrainTypeData
{
    [SerializeField] public TileBase tile;
    [SerializeField] public float height;
    
    public TerrainTypeData()
    {
        tile = null;
        height = 0f;
    }
    
    public TerrainTypeData(TileBase tile, float height)
    {
        this.tile = tile;
        this.height = height;
    }
}

[CreateAssetMenu(fileName = "TerrainData", menuName = "Flood/Terrain Data")]
public class TerrainData : ScriptableObject
{
    [Header("Terrain Configuration")]
    [SerializeField] private int terrainTypes = 3;
    
    [Header("Terrain Types")]
    [SerializeField] private List<TerrainTypeData> terrainTypesList = new List<TerrainTypeData>();
    
    [Header("Tile Values (Debug/Visual Inspection)")]
    [SerializeField] private List<Vector2Int> tilePositions = new List<Vector2Int>();
    [SerializeField] private List<int> tileValues = new List<int>();
    
    [Header("Data Status")]
    [SerializeField] private bool dataLoaded = false;
    [SerializeField] private int totalTilesWritten = 0;
    [SerializeField] private string lastOperationResult = "No operation performed";
    
    // Properties for external access
    public int TerrainTypes 
    { 
        get => terrainTypes; 
        set 
        { 
            terrainTypes = value;
            ValidateTerrainHeights();
        } 
    }
    
    public List<TerrainTypeData> TerrainTypesList => terrainTypesList;
    
    // Backward compatibility properties
    public List<TileBase> TerrainTiles 
    {
        get
        {
            List<TileBase> tiles = new List<TileBase>();
            foreach (var terrainType in terrainTypesList)
            {
                tiles.Add(terrainType.tile);
            }
            return tiles;
        }
    }
    
    public List<float> TerrainHeights 
    {
        get
        {
            List<float> heights = new List<float>();
            foreach (var terrainType in terrainTypesList)
            {
                heights.Add(terrainType.height);
            }
            return heights;
        }
    }
    
    public List<Vector2Int> TilePositions => tilePositions;
    public List<int> TileValues => tileValues;
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
    
    private void OnValidate()
    {
        ValidateTerrainHeights();
    }

    private void ValidateTerrainHeights()
    {
        // Ensure the terrain types list matches the terrain types count
        while (terrainTypesList.Count < terrainTypes)
        {
            terrainTypesList.Add(new TerrainTypeData());
        }
        
        while (terrainTypesList.Count > terrainTypes)
        {
            terrainTypesList.RemoveAt(terrainTypesList.Count - 1);
        }
    }
}
