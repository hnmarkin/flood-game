using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TileComposer
{
    private readonly Tilemap _tilemap;
    private readonly TileRuleMap _rules;

    public TileComposer(Tilemap tilemap, TileRuleMap rules)
    {
        _tilemap = tilemap;
        _rules = rules;
    }

    //Apply to all tiles
    public void ApplyFull(CellState[] grid, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var cell = grid[y * width + x];
                var tile = _rules.Resolve(cell.terrainType, cell.waterLevel);
                _tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    //Only apply to changed tiles
    public void ApplyChanges(CellState[] grid, int width, int height, IList<int> changedIndices)
    {
        foreach (var idx in changedIndices)
        {
            int x = idx % width;
            int y = idx / width;
            var cell = grid[idx];
            var tile = _rules.Resolve(cell.terrainType, cell.waterLevel);
            _tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }

}
