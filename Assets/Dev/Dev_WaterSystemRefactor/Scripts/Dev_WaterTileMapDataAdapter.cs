using UnityEngine;

public static class Dev_WaterTileMapDataAdapter
{
    public static Dev_WaterRuntimeState CreateRuntimeState(TileMapData tileMapData, TileType fallbackWaterTileType)
    {
        if (tileMapData == null)
        {
            Debug.LogError("[Dev_WaterTileMapDataAdapter] Cannot create runtime state without TileMapData.");
            return null;
        }

        ResolveBounds(tileMapData, out Vector2Int origin, out int width, out int height);

        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"[Dev_WaterTileMapDataAdapter] Invalid runtime bounds. width={width}, height={height}.");
            return null;
        }

        var state = new Dev_WaterRuntimeState(width, height, origin);

        for (int tileY = origin.y; tileY < origin.y + height; tileY++)
        {
            for (int tileX = origin.x; tileX < origin.x + width; tileX++)
            {
                var tileCell = new Vector2Int(tileX, tileY);
                if (!TryGetTile(tileMapData, tileCell, out TileInstance tile))
                    continue;

                if (!state.TryTileToSim(tileCell, out int simX, out int simY))
                    continue;

                state.HasTile[simX, simY] = true;
                state.Terrain[simX, simY] = tile.elevation;
                state.Water[simX, simY] = Mathf.Max(0f, tile.waterHeight);
                state.IsWaterBody[simX, simY] = IsWaterBodyTile(tile, fallbackWaterTileType);
            }
        }

        state.MarkAllExistingDirty();
        return state;
    }

    public static bool TryGetTile(TileMapData tileMapData, Vector2Int tileCell, out TileInstance tile)
    {
        tile = null;

        if (tileMapData == null)
            return false;

        if (tileCell.x < 0 || tileCell.y < 0 || tileCell.x >= tileMapData.sizeX || tileCell.y >= tileMapData.sizeY)
            return false;

        tile = tileMapData.Get(tileCell);
        return tile != null;
    }

    private static void ResolveBounds(TileMapData tileMapData, out Vector2Int origin, out int width, out int height)
    {
        int xMin = tileMapData.rangeX.x;
        int xMax = tileMapData.rangeX.y;
        int yMin = tileMapData.rangeY.x;
        int yMax = tileMapData.rangeY.y;

        if (xMax <= xMin)
        {
            xMin = 0;
            xMax = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeX;
        }

        if (yMax <= yMin)
        {
            yMin = 0;
            yMax = tileMapData.N > 0 ? tileMapData.N : tileMapData.sizeY;
        }

        width = Mathf.Max(0, xMax - xMin);
        height = Mathf.Max(0, yMax - yMin);
        origin = new Vector2Int(xMin, yMin);
    }

    private static bool IsWaterBodyTile(TileInstance tile, TileType fallbackWaterTileType)
    {
        if (tile == null || tile.tileType == null)
            return false;

        if (fallbackWaterTileType != null && tile.tileType == fallbackWaterTileType)
            return true;

        if (tile.tileType.isWater)
            return true;

        return !string.IsNullOrWhiteSpace(tile.tileType.tileName)
            && tile.tileType.tileName.ToLowerInvariant().Contains("water");
    }
}
