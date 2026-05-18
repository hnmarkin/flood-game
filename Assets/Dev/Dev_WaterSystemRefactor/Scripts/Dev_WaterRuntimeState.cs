using System.Collections.Generic;
using UnityEngine;

public sealed class Dev_WaterRuntimeState
{
    private readonly HashSet<Vector2Int> _dirtyCells = new HashSet<Vector2Int>();

    public Dev_WaterRuntimeState(int width, int height, Vector2Int origin)
    {
        Width = Mathf.Max(0, width);
        Height = Mathf.Max(0, height);
        Origin = origin;

        GridWidth = Width + 2;
        GridHeight = Height + 2;

        Terrain = new float[GridWidth, GridHeight];
        Water = new float[GridWidth, GridHeight];
        FlowX = new float[GridWidth, GridHeight];
        FlowY = new float[GridWidth, GridHeight];
        Active = new bool[GridWidth, GridHeight];
        HasTile = new bool[GridWidth, GridHeight];
        IsWaterBody = new bool[GridWidth, GridHeight];
    }

    public int Width { get; }
    public int Height { get; }
    public int GridWidth { get; }
    public int GridHeight { get; }
    public Vector2Int Origin { get; }

    public float[,] Terrain { get; }
    public float[,] Water { get; }
    public float[,] FlowX { get; }
    public float[,] FlowY { get; }
    public bool[,] Active { get; set; }
    public bool[,] HasTile { get; }
    public bool[,] IsWaterBody { get; }

    public IReadOnlyCollection<Vector2Int> DirtyCells => _dirtyCells;

    public bool IsSimCellInBounds(int simX, int simY)
    {
        return simX >= 0 && simY >= 0 && simX < GridWidth && simY < GridHeight;
    }

    public bool IsLogicalSimCell(int simX, int simY)
    {
        return simX >= 1 && simY >= 1 && simX <= Width && simY <= Height;
    }

    public bool HasTileAtSim(int simX, int simY)
    {
        return IsLogicalSimCell(simX, simY) && HasTile[simX, simY];
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

        return HasTile[simX, simY] ? Water[simX, simY] : 0f;
    }

    public bool TrySetWaterDepth(Vector2Int tileCell, float depth)
    {
        if (!TryTileToSim(tileCell, out int simX, out int simY))
            return false;

        if (!HasTile[simX, simY])
            return false;

        Water[simX, simY] = Mathf.Max(0f, depth);
        MarkDirtyBySim(simX, simY);
        return true;
    }

    public void MarkDirtyBySim(int simX, int simY)
    {
        if (!HasTileAtSim(simX, simY))
            return;

        _dirtyCells.Add(SimToTile(simX, simY));
    }

    public void MarkAllExistingDirty()
    {
        for (int y = 1; y <= Height; y++)
        {
            for (int x = 1; x <= Width; x++)
            {
                if (HasTile[x, y])
                    _dirtyCells.Add(SimToTile(x, y));
            }
        }
    }

    public void ClearDirty()
    {
        _dirtyCells.Clear();
    }
}
