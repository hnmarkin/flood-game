using UnityEngine;

/// <summary>
/// Read-only runtime access to persistent Dev_MapDef.
/// It is the boundary between stored map data and simulation/rendering code.
/// </summary>
public sealed class Dev_MapAccessor
{
    private readonly Dev_MapDef _mapDef;

    public Dev_MapAccessor(Dev_MapDef mapDef)
    {
        _mapDef = mapDef;
    }

    public Dev_MapDef MapDef => _mapDef;
    public Vector2Int Origin => _mapDef != null ? _mapDef.Origin : default;
    public int Width => _mapDef != null ? _mapDef.Width : 0;
    public int Height => _mapDef != null ? _mapDef.Height : 0;

    public bool TryGetCell(Vector2Int tileCell, out Dev_MapCellDef cell)
    {
        cell = null;
        return _mapDef != null && _mapDef.TryGetCell(tileCell, out cell);
    }

    public bool TryGetCell(int simX, int simY, out Dev_MapCellDef cell)
    {
        return TryGetCell(SimToTile(simX, simY), out cell);
    }

    public bool IsSimulationCell(int simX, int simY)
    {
        return TryGetCell(simX, simY, out _);
    }

    public float GetElevation(int simX, int simY)
    {
        return TryGetCell(simX, simY, out Dev_MapCellDef cell) ? cell.Elevation : 0f;
    }

    public bool IsInitialWaterBody(int simX, int simY)
    {
        return TryGetCell(simX, simY, out Dev_MapCellDef cell) && cell.IsInitialWaterBody;
    }

    public Dev_RendererDef GetRendererDefinition(int simX, int simY)
    {
        return TryGetCell(simX, simY, out Dev_MapCellDef cell) && cell.Terrain != null
            ? cell.Terrain.RendererDefinition
            : null;
    }

    public Vector2Int SimToTile(int simX, int simY)
    {
        return new Vector2Int(Origin.x + simX - 1, Origin.y + simY - 1);
    }

    public bool TryTileToSim(Vector2Int tileCell, out int simX, out int simY)
    {
        simX = tileCell.x - Origin.x + 1;
        simY = tileCell.y - Origin.y + 1;
        return simX >= 1 && simY >= 1 && simX <= Width && simY <= Height;
    }
}
