using UnityEngine;

/// <summary>
/// Read-only runtime access to persistent Dev_WaterMapData.
/// It is the boundary between stored map data and simulation/rendering code.
/// </summary>
public sealed class Dev_WaterMapAccessor
{
    private readonly Dev_WaterMapData _mapData;

    public Dev_WaterMapAccessor(Dev_WaterMapData mapData)
    {
        _mapData = mapData;
    }

    public Dev_WaterMapData MapData => _mapData;
    public Vector2Int Origin => _mapData != null ? _mapData.Origin : default;
    public int Width => _mapData != null ? _mapData.Width : 0;
    public int Height => _mapData != null ? _mapData.Height : 0;

    public bool TryGetCell(Vector2Int tileCell, out Dev_WaterMapCell cell)
    {
        cell = null;
        return _mapData != null && _mapData.TryGetCell(tileCell, out cell);
    }

    public bool TryGetCell(int simX, int simY, out Dev_WaterMapCell cell)
    {
        return TryGetCell(SimToTile(simX, simY), out cell);
    }

    public bool IsSimulationCell(int simX, int simY)
    {
        return TryGetCell(simX, simY, out _);
    }

    public float GetElevation(int simX, int simY)
    {
        return TryGetCell(simX, simY, out Dev_WaterMapCell cell) ? cell.Elevation : 0f;
    }

    public bool IsInitialWaterSource(int simX, int simY)
    {
        return TryGetCell(simX, simY, out Dev_WaterMapCell cell)
            && (cell.InitialWaterSource || (cell.Terrain != null && cell.Terrain.IsInitialWaterBody));
    }

    public Dev_WaterVisualDefinition GetVisualDefinition(int simX, int simY)
    {
        return TryGetCell(simX, simY, out Dev_WaterMapCell cell) && cell.Terrain != null
            ? cell.Terrain.VisualDefinition
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
