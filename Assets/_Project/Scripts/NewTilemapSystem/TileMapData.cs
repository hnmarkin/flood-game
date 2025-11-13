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

    public TileInstance Get(Vector3Int pos)
        => tiles[Idx(pos.x, pos.y, pos.z)];

    public void Set(Vector3Int pos, TileInstance t)
        => tiles[Idx(pos.x, pos.y, pos.z)] = t;

    public void SetTileInstanceAt(Vector3Int pos, TileInstance tileInstance)
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
