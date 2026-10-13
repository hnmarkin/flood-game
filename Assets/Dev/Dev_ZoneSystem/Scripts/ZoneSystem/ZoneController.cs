using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class ZoneController : MonoBehaviour
{
    [Header("Legacy Map Sources")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private JsonMapLoader jsonMapLoader;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly ZoneIndex _zoneIndex = new ZoneIndex();

    public bool IsInitialized => _zoneIndex.IsInitialized;
    public int ZoneCount => _zoneIndex.ZoneCount;
    public Tilemap TerrainTilemap => terrainTilemap;

    public Vector2Int TileOrigin
    {
        get
        {
            if (_zoneIndex.IsInitialized)
                return _zoneIndex.TileOrigin;

            if (terrainTilemap == null)
                return Vector2Int.zero;

            terrainTilemap.CompressBounds();
            BoundsInt bounds = terrainTilemap.cellBounds;
            return new Vector2Int(bounds.xMin, bounds.yMin);
        }
    }

    public void Configure(
        TileMapData mapData,
        Tilemap terrain,
        JsonMapLoader loader,
        bool enableDebugLogs)
    {
        bool referencesChanged =
            tileMapData != mapData ||
            terrainTilemap != terrain ||
            jsonMapLoader != loader;

        tileMapData = mapData;
        terrainTilemap = terrain;
        jsonMapLoader = loader;
        debugLogs = enableDebugLogs;

        if (referencesChanged)
            _zoneIndex.Clear();
    }

    public bool IsZoneLoaderReady()
    {
        if (tileMapData == null || terrainTilemap == null)
            return false;

        return ZoneIndex.IsLoaderReady(jsonMapLoader);
    }

    public bool EnsureZoneIndexReadyForExternalUse()
    {
        if (_zoneIndex.IsInitialized)
            return _zoneIndex.ZoneCount > 0;

        return RebuildZoneIndex();
    }

    public bool RebuildZoneIndex()
    {
        if (!IsZoneLoaderReady())
        {
            if (debugLogs)
                Debug.LogWarning("[ZoneController] Zone index is not ready to build.");

            _zoneIndex.Clear();
            return false;
        }

        bool built = _zoneIndex.TryBuild(jsonMapLoader, tileMapData, terrainTilemap, debugLogs);
        return built && _zoneIndex.ZoneCount > 0;
    }

    public void InvalidateZoneIndex()
    {
        _zoneIndex.Clear();
    }

    /// <summary>
    /// Compatibility API. Returns the mutable legacy tile set until callers migrate
    /// to read-only zone queries.
    /// </summary>
    public bool TryGetZoneTiles(string geoid, out HashSet<Vector2Int> zoneTiles)
    {
        zoneTiles = null;

        if (string.IsNullOrEmpty(geoid))
            return false;

        if (!EnsureZoneIndexReadyForExternalUse())
            return false;

        return _zoneIndex.TryGetZoneTiles(geoid, out zoneTiles);
    }

    /// <summary>
    /// Compatibility API. Returns the mutable legacy lookup until callers migrate
    /// to read-only zone queries.
    /// </summary>
    public IReadOnlyDictionary<string, HashSet<Vector2Int>> GetAllZoneTiles()
    {
        if (!EnsureZoneIndexReadyForExternalUse())
            return null;

        return _zoneIndex.AllZoneTiles;
    }

    public bool TryGetZoneTilesAtWorld(
        Vector3 world,
        out string geoid,
        out HashSet<Vector2Int> zoneTiles)
    {
        geoid = null;
        zoneTiles = null;

        if (!EnsureZoneIndexReadyForExternalUse())
            return false;

        if (jsonMapLoader == null)
            return false;

        world.z = 0f;

        bool hit = jsonMapLoader.TryGetTileInfoAtWorld(
            world,
            out _,
            out _,
            out _,
            out geoid,
            out _);

        if (!hit || string.IsNullOrEmpty(geoid))
            return false;

        return _zoneIndex.TryGetZoneTiles(geoid, out zoneTiles);
    }

    public Vector3Int NormalizedTileToCell(Vector2Int normalizedTile)
    {
        Vector2Int origin = TileOrigin;
        return new Vector3Int(
            normalizedTile.x + origin.x,
            normalizedTile.y + origin.y,
            0);
    }

    public Vector2Int CellToNormalizedTile(Vector3Int cell)
    {
        Vector2Int origin = TileOrigin;
        return new Vector2Int(cell.x - origin.x, cell.y - origin.y);
    }

    public bool TryGetGeoidAtNormalizedTile(Vector2Int normalizedTile, out string geoid)
    {
        geoid = null;

        if (!EnsureZoneIndexReadyForExternalUse())
            return false;

        return _zoneIndex.TryGetGeoid(normalizedTile, out geoid);
    }

    public bool ContainsTile(string geoid, Vector2Int normalizedTile)
    {
        if (!EnsureZoneIndexReadyForExternalUse())
            return false;

        return _zoneIndex.ContainsTile(geoid, normalizedTile);
    }

    public bool TryGetBoundaryTiles(string geoid, out IReadOnlyList<Vector2Int> boundaryTiles)
    {
        boundaryTiles = null;

        if (!TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles))
            return false;

        boundaryTiles = ZoneGeometry.CalculateBoundaryTiles(zoneTiles);
        return true;
    }

    public bool TryGetBoundaryEdges(string geoid, out IReadOnlyList<ZoneBoundaryEdge> boundaryEdges)
    {
        boundaryEdges = null;

        if (!TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles))
            return false;

        boundaryEdges = ZoneGeometry.CalculateBoundaryEdges(zoneTiles, _zoneIndex.MapSize);
        return true;
    }
}
