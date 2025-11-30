using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/TileMapData")]
public class TileMapData : ScriptableObject
{
    [Header("Grid Size")]
    public int sizeX = 256, sizeY = 256, sizeZ = 10;
    public Vector2Int rangeX;
    public Vector2Int rangeY;
    public Vector2Int rangeZ;

    [Header("Simulation Parameters")]
    public int N;
    public float dx = 1f, dy = 1f, dt = 1f;
    public float g = 9.81f;
    public float friction = 0.02f;

    [NonSerialized] public float[,] flowX;
    [NonSerialized] public float[,] flowY;

    public int GridWidth => N + 2;
    public int GridHeight => N + 2;

    [SerializeField, HideInInspector]
    private TileInstance[] tiles;

    void OnEnable()
    {
        int total = sizeX * sizeY * sizeZ;
        if (tiles == null || tiles.Length != total)
            tiles = new TileInstance[total];

        N = rangeX.y;
    }
    // Helpers

    // Compute a unique index for (x,y,z)
    private int Idx(int x, int y)
        => (x * sizeY) + y;

    public TileInstance Get(Vector2Int pos)
        => tiles[Idx(pos.x, pos.y)];

    public void Set(Vector2Int pos, TileInstance t)
        => tiles[Idx(pos.x, pos.y)] = t;

    public void SetWater(Vector2Int pos, float water)
    {
        TileInstance ti = tiles[Idx(pos.x, pos.y)];
        ti.waterHeight = water;
        // Change Sprite based on water height
        ti.sprite = ti.tileType.GetTileForWaterHeight(water);
    }

    public void SetSprite(Vector2Int pos, Sprite sprite)
    {
        tiles[Idx(pos.x, pos.y)].sprite = sprite;
    }

    public void SetTileInstanceAt(Vector2Int pos, TileInstance tileInstance)
    {
        // For simplicity, we set z = 0 for 2D tilemaps
        Set(pos, tileInstance);
        Debug.Log("Successfully assigned TileInstance to TileMapData at position " + pos);

    }

    public int CountNonNullTiles()
    {
        //if (tiles == null) return 0;

        int count = 0;
        foreach (var tile in tiles)
        {
            if (tile != null) count++;
        }
        return count;
    }
    
    public void ShowHeaderInfo()
    {
        Debug.Log($"TileMapData Size: {sizeX} x {sizeY} x {sizeZ}");
    }
}
