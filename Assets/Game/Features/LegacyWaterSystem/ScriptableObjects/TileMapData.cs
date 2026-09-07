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

    [NonSerialized] public float[,] water;
    [NonSerialized] public float[,] terrain;
    [NonSerialized] public float[,] flowX;
    [NonSerialized] public float[,] flowY;

    public bool simInitialized = false;

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
        int idx = Idx(pos.x, pos.y);

        if (tiles == null || idx < 0 || idx >= tiles.Length)
        {
            Debug.LogWarning($"[TileMapData] SetWater: position {pos} is out of range.");
            return;
        }

        TileInstance ti = tiles[idx];
        if (ti == null)
        {
            // No TileInstance at this position – nothing to update
            // Debug.LogWarning($"[TileMapData] SetWater: no TileInstance at {pos}.");
            return;
        }

        ti.waterHeight = water;

        // depth->tint mapping
        float t = Mathf.InverseLerp(0f, 1.0f, water); // choose your "deep" max
        Color shallow = new Color(0.70f, 0.85f, 1.00f, 1f);
        Color deep    = new Color(0.10f, 0.25f, 0.50f, 1f);

        // If you want even darker for severe flooding, clamp higher max, etc.
        ti.tint = Color.Lerp(shallow, deep, t);

        // ensure visuals update when tint changes
        TileManager.Instance?.RefreshAt(new Vector3Int(pos.x, pos.y, 0));

        // Only update sprite if we have a TileType
        if (ti.tileType != null && !(ti.tileType.isWater && ti.tileType.isAnimated))
        {
            var sprite = ti.tileType.GetTileForWaterHeight(water);
            if (sprite != null) ti.sprite = sprite;
        }
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
