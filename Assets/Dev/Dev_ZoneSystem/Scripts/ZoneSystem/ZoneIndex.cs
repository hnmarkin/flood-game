using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class ZoneIndex
{
    private readonly Dictionary<string, HashSet<Vector2Int>> _geoidToTiles =
        new Dictionary<string, HashSet<Vector2Int>>(StringComparer.Ordinal);

    private readonly Dictionary<Vector2Int, string> _tileToGeoid =
        new Dictionary<Vector2Int, string>();

    public bool IsInitialized { get; private set; }
    public Vector2Int TileOrigin { get; private set; }
    public int GridSize { get; private set; }
    public int ZoneCount => _geoidToTiles.Count;
    public Vector2Int MapSize => new Vector2Int(GridSize, GridSize);
    public IReadOnlyDictionary<string, HashSet<Vector2Int>> AllZoneTiles => _geoidToTiles;

    public static bool IsLoaderReady(JsonMapLoader jsonMapLoader)
    {
        if (jsonMapLoader == null)
            return false;

        if (jsonMapLoader.payload == null)
            return false;

        if (jsonMapLoader.cellToRC == null || jsonMapLoader.cellToRC.Count == 0)
            return false;

        if (jsonMapLoader.geoidGrid == null)
            return false;

        return true;
    }

    public void Clear()
    {
        _geoidToTiles.Clear();
        _tileToGeoid.Clear();
        TileOrigin = Vector2Int.zero;
        GridSize = 0;
        IsInitialized = false;
    }

    public bool TryBuild(
        JsonMapLoader jsonMapLoader,
        TileMapData tileMapData,
        Tilemap terrainTilemap,
        bool debugLogs)
    {
        Clear();

        if (jsonMapLoader == null)
        {
            Debug.LogError("[ZoneIndex] Build failed: JsonMapLoader is null.");
            return false;
        }

        if (tileMapData == null)
        {
            Debug.LogError("[ZoneIndex] Build failed: TileMapData is null.");
            return false;
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("[ZoneIndex] Build failed: Terrain Tilemap is null.");
            return false;
        }

        if (jsonMapLoader.payload == null)
        {
            Debug.LogWarning("[ZoneIndex] Build skipped: JsonMapLoader payload is null.");
            return false;
        }

        if (jsonMapLoader.cellToRC == null || jsonMapLoader.cellToRC.Count == 0)
        {
            Debug.LogWarning("[ZoneIndex] Build skipped: JsonMapLoader cellToRC is empty.");
            return false;
        }

        if (jsonMapLoader.geoidGrid == null)
        {
            Debug.LogWarning("[ZoneIndex] Build skipped: JsonMapLoader geoidGrid is null.");
            return false;
        }

        terrainTilemap.CompressBounds();
        BoundsInt bounds = terrainTilemap.cellBounds;
        TileOrigin = new Vector2Int(bounds.xMin, bounds.yMin);
        GridSize = tileMapData.N;

        foreach (KeyValuePair<Vector3Int, Vector2Int> kvp in jsonMapLoader.cellToRC)
        {
            Vector3Int cell = kvp.Key;
            Vector2Int rc = kvp.Value;

            int r = rc.x;
            int c = rc.y;

            string geoid = jsonMapLoader.geoidGrid[r, c];

            if (string.IsNullOrEmpty(geoid))
                continue;

            int tx = cell.x - TileOrigin.x;
            int ty = cell.y - TileOrigin.y;

            if (tx < 0 || ty < 0 || tx >= GridSize || ty >= GridSize)
                continue;

            if (!_geoidToTiles.TryGetValue(geoid, out HashSet<Vector2Int> zoneTiles))
            {
                zoneTiles = new HashSet<Vector2Int>();
                _geoidToTiles[geoid] = zoneTiles;
            }

            Vector2Int normalizedTile = new Vector2Int(tx, ty);
            zoneTiles.Add(normalizedTile);
            _tileToGeoid[normalizedTile] = geoid;
        }

        IsInitialized = true;

        if (debugLogs)
            Debug.Log($"[ZoneIndex] Build OK. Zones found={ZoneCount}. Tile origin={TileOrigin}.");

        return true;
    }

    public bool TryGetZoneTiles(string geoid, out HashSet<Vector2Int> zoneTiles)
    {
        zoneTiles = null;

        if (!IsInitialized || string.IsNullOrEmpty(geoid))
            return false;

        return _geoidToTiles.TryGetValue(geoid, out zoneTiles);
    }

    public bool TryGetGeoid(Vector2Int normalizedTile, out string geoid)
    {
        geoid = null;

        if (!IsInitialized)
            return false;

        return _tileToGeoid.TryGetValue(normalizedTile, out geoid);
    }

    public bool ContainsTile(string geoid, Vector2Int normalizedTile)
    {
        return TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles) &&
               zoneTiles.Contains(normalizedTile);
    }
}
