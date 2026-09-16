using UnityEngine;

/// <summary>
/// Read-only runtime access to persistent MapDef.
/// It is the boundary between stored map data and simulation/rendering code.
/// </summary>
public sealed class MapAccessor
{
    private readonly MapDef _mapDef;

    public MapAccessor(MapDef mapDef)
    {
        _mapDef = mapDef;
    }

    public Vector2Int Origin => _mapDef != null ? _mapDef.Origin : default;
    public int Width => _mapDef != null ? _mapDef.Width : 0;
    public int Height => _mapDef != null ? _mapDef.Height : 0;

    public bool TryGetCell(Vector2Int tileCell, out MapCellDef cell)
    {
        cell = null;
        return _mapDef != null && _mapDef.TryGetCell(tileCell, out cell);
    }

    public bool TryGetCell(int simX, int simY, out MapCellDef cell)
    {
        return TryGetCell(SimToTile(simX, simY), out cell);
    }

    public bool IsSimulationCell(int simX, int simY)
    {
        return TryGetCell(simX, simY, out MapCellDef cell)
            && cell.Terrain != null
            && cell.Terrain.ParticipatesInSimulation;
    }

    public float GetElevation(int simX, int simY)
    {
        return TryGetCell(simX, simY, out MapCellDef cell) ? cell.Elevation : 0f;
    }

    public bool IsInitialWaterBody(int simX, int simY)
    {
        return TryGetCell(simX, simY, out MapCellDef cell) && cell.IsInitialWaterBody;
    }

    public RendererDef GetRendererDefinition(int simX, int simY)
    {
        return TryGetCell(simX, simY, out MapCellDef cell) && cell.Terrain != null
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
