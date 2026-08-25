using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mutable simulation state for one initialized map run. Static map information is
/// supplied by Dev_WaterMapAccessor; this object owns only live simulation values.
/// </summary>
public sealed class Dev_WaterRuntimeState
{
    private readonly Dev_WaterMapAccessor _map;
    private readonly HashSet<Vector2Int> _dirtyCells = new HashSet<Vector2Int>();

    public Dev_WaterRuntimeState(Dev_WaterMapAccessor map)
    {
        _map = map;
        Width = Mathf.Max(0, map != null ? map.Width : 0);
        Height = Mathf.Max(0, map != null ? map.Height : 0);
        Origin = map != null ? map.Origin : default;

        GridWidth = Width + 2;
        GridHeight = Height + 2;

        Terrain = new float[GridWidth, GridHeight];
        Water = new float[GridWidth, GridHeight];
        FlowX = new float[GridWidth, GridHeight];
        FlowY = new float[GridWidth, GridHeight];
        Active = new bool[GridWidth, GridHeight];

        LoadMapSnapshot();
    }

    public int Width { get; }
    public int Height { get; }
    public int GridWidth { get; }
    public int GridHeight { get; }
    public Vector2Int Origin { get; }

    // Terrain is an immutable runtime cache copied from the persistent map.
    public float[,] Terrain { get; }
    public float[,] Water { get; }
    public float[,] FlowX { get; }
    public float[,] FlowY { get; }
    public bool[,] Active { get; }

    public IReadOnlyCollection<Vector2Int> DirtyCells => _dirtyCells;

    public bool IsSimCellInBounds(int simX, int simY)
    {
        return simX >= 0 && simY >= 0 && simX < GridWidth && simY < GridHeight;
    }

    public bool IsLogicalSimCell(int simX, int simY)
    {
        return simX >= 1 && simY >= 1 && simX <= Width && simY <= Height;
    }

    public bool HasMapCellAtSim(int simX, int simY)
    {
        return IsLogicalSimCell(simX, simY) && _map != null && _map.IsSimulationCell(simX, simY);
    }

    public bool TryTileToSim(Vector2Int tileCell, out int simX, out int simY)
    {
        simX = tileCell.x - Origin.x + 1;
        simY = tileCell.y - Origin.y + 1;
        return IsLogicalSimCell(simX, simY);
    }

    public Vector2Int SimToTile(int simX, int simY)
    {
        return new Vector2Int(Origin.x + simX - 1, Origin.y + simY - 1);
    }

    public float GetWaterDepth(Vector2Int tileCell)
    {
        if (!TryTileToSim(tileCell, out int simX, out int simY))
            return 0f;

        return HasMapCellAtSim(simX, simY) ? Water[simX, simY] : 0f;
    }

    public bool TrySetWaterDepth(Vector2Int tileCell, float depth)
    {
        if (!TryTileToSim(tileCell, out int simX, out int simY) || !HasMapCellAtSim(simX, simY))
            return false;

        Water[simX, simY] = Mathf.Max(0f, depth);
        MarkDirtyBySim(simX, simY);
        return true;
    }

    public void MarkDirtyBySim(int simX, int simY)
    {
        if (!HasMapCellAtSim(simX, simY))
            return;

        _dirtyCells.Add(SimToTile(simX, simY));
    }

    public void MarkAllExistingDirty()
    {
        for (int y = 1; y <= Height; y++)
        {
            for (int x = 1; x <= Width; x++)
            {
                if (HasMapCellAtSim(x, y))
                    _dirtyCells.Add(SimToTile(x, y));
            }
        }
    }

    public void ClearDirty()
    {
        _dirtyCells.Clear();
    }

    internal Dev_WaterRuntimeState Clone()
    {
        Dev_WaterRuntimeState clone = new Dev_WaterRuntimeState(_map);
        CopyGrid(Terrain, clone.Terrain);
        CopyGrid(Water, clone.Water);
        CopyGrid(FlowX, clone.FlowX);
        CopyGrid(FlowY, clone.FlowY);
        CopyGrid(Active, clone.Active);

        foreach (Vector2Int dirtyCell in _dirtyCells)
            clone._dirtyCells.Add(dirtyCell);

        return clone;
    }

    internal float[] CopyLogicalWaterDepths()
    {
        float[] depths = new float[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
                depths[y * Width + x] = Water[x + 1, y + 1];
        }

        return depths;
    }

    public void ReplaceActiveGrid(bool[,] active)
    {
        if (active == null || active.GetLength(0) != GridWidth || active.GetLength(1) != GridHeight)
            return;

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
                Active[x, y] = active[x, y];
        }
    }

    private void LoadMapSnapshot()
    {
        if (_map == null)
            return;

        for (int y = 1; y <= Height; y++)
        {
            for (int x = 1; x <= Width; x++)
            {
                if (!_map.TryGetCell(x, y, out Dev_WaterMapCell cell))
                    continue;

                Terrain[x, y] = cell.Elevation;
                Water[x, y] = cell.InitialWaterDepth;
            }
        }
    }

    private static void CopyGrid<T>(T[,] source, T[,] destination)
    {
        for (int y = 0; y < source.GetLength(1); y++)
        {
            for (int x = 0; x < source.GetLength(0); x++)
                destination[x, y] = source[x, y];
        }
    }
}
