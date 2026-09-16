using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [Header("Visual Tilemaps to refresh (ground, roads, buildings)")]
    [SerializeField] private Tilemap[] tilemaps;   
    [SerializeField] private TileMapData grid;

    private void Awake()
    {
        Instance = this;
    }

    public TileInstance GetTileTypeAt(Vector3Int pos)
    {
        // Same as before: TileMapData is the single source of truth
        return grid.Get(new Vector2Int(pos.x, pos.y));
    }

    public void SetTileType(Vector2Int pos, TileInstance type)
    {
        grid.Set(pos, type);

        // We store the elevation, but our visible tiles are all on z = 0
        var cell = new Vector3Int(pos.x, pos.y, 0);
        RefreshAt(cell);
    }

    public void RefreshAt(Vector3Int cell)
    {
        var fixedCell = new Vector3Int(cell.x, cell.y, 0);

        if (tilemaps == null) return;

        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;
            tm.RefreshTile(fixedCell);
        }

        // Debug.Log("Refreshed tile at " + fixedCell);
    }

    public void RefreshAll()
    {
        if (tilemaps == null) return;

        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;
            tm.RefreshAllTiles();
        }
    }
}
