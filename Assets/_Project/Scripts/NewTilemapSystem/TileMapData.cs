using UnityEngine;

[CreateAssetMenu(menuName = "Tiles/TileMapData")]
public class TileMapData : ScriptableObject
{
    [Header("Grid Size")]
    public int sizeX = 256, sizeY = 256, sizeZ = 10;
    public Vector2Int rangeX;
    public Vector2Int rangeY;
    public Vector2Int rangeZ;

    [SerializeField, HideInInspector]
    private TileInstance[] tiles;

    void OnEnable()
    {
        int total = sizeX * sizeY * sizeZ;
        if (tiles == null || tiles.Length != total)
            tiles = new TileInstance[total];
    }
    // Helpers

    // Compute a unique index for (x,y,z)
    private int Idx(int x, int y, int z)
        => (x * sizeY * sizeZ) + (y * sizeZ) + z;

    public TileInstance Get(int x, int y, int z)
        => tiles[Idx(x, y, z)];

    public void Set(int x, int y, int z, TileInstance t)
        => tiles[Idx(x, y, z)] = t;

    public void SetTileInstanceAt(int x, int y, TileInstance tileInstance)
    {
        // For simplicity, we set z = 0 for 2D tilemaps
        Set(x, y, 0, tileInstance);
        Debug.Log("Successfully assigned TileInstance to TileMapData at position " + new Vector3Int(x, y, 0));

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
