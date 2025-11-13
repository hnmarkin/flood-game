using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [SerializeField] private Tilemap tilemap;

    // Your simulation data (2D/3D array, dictionary, etc.)
    private TileMapData grid;

    private void Awake()
    {
        Instance = this;
    }

    public TileInstance GetTileTypeAt(Vector3Int pos)
    {
        // Convert tilemap cell position → array coords as needed
        return grid.Get(new Vector2Int(pos.x, pos.y));
    }

    public void SetTileType(Vector2Int pos, TileInstance type)
    {
        grid.Set(pos, type);
        int z = grid.Get(pos).elevation;

        // Tell Unity to redraw this tile
        tilemap.RefreshTile(new Vector3Int(pos.x, pos.y, z));
    }

    public void RefreshAt(Vector3Int cell)
    {
        tilemap.RefreshTile(cell);
    }


    public void RefreshAll()
    {
        tilemap.RefreshAllTiles();
    }
}
