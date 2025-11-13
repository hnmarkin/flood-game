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
        return grid.Get(pos);
    }

    public void SetTileType(Vector3Int pos, TileInstance type)
    {
        grid.Set(pos, type);

        // Tell Unity to redraw this tile
        tilemap.RefreshTile(pos);
    }

    public void RefreshAll()
    {
        tilemap.RefreshAllTiles();
    }
}
