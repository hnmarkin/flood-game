using UnityEngine;

/// <summary>
/// Persistent, static map data consumed by the Dev Water System.
/// It contains no live flow arrays or simulation history.
/// </summary>
[CreateAssetMenu(fileName = "Dev_WaterMapData", menuName = "Dev/Water System/Map Data")]
public sealed class Dev_WaterMapData : ScriptableObject
{
    [SerializeField] private Vector2Int origin;
    [Min(1)]
    [SerializeField] private int width = 1;
    [Min(1)]
    [SerializeField] private int height = 1;
    [SerializeField] private Dev_WaterMapCell[] cells;

    public Vector2Int Origin => origin;
    public int Width => Mathf.Max(0, width);
    public int Height => Mathf.Max(0, height);
    public int CellCount => Width * Height;

    public void Configure(Vector2Int mapOrigin, int mapWidth, int mapHeight)
    {
        origin = mapOrigin;
        width = Mathf.Max(1, mapWidth);
        height = Mathf.Max(1, mapHeight);
        EnsureCellCapacity();
    }

    public void EnsureCellCapacity()
    {
        int required = Mathf.Max(0, Width * Height);
        if (cells == null || cells.Length != required)
        {
            Dev_WaterMapCell[] previous = cells;
            cells = new Dev_WaterMapCell[required];

            if (previous != null)
            {
                int copyCount = Mathf.Min(previous.Length, cells.Length);
                for (int i = 0; i < copyCount; i++)
                    cells[i] = previous[i];
            }
        }

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
                cells[i] = new Dev_WaterMapCell();
        }
    }

    public bool TryGetCell(Vector2Int tileCell, out Dev_WaterMapCell cell)
    {
        cell = null;
        if (!TryGetIndex(tileCell, out int index) || cells == null || index >= cells.Length)
            return false;

        cell = cells[index];
        return cell != null && cell.Exists;
    }

    public bool TrySetCell(Vector2Int tileCell, Dev_WaterMapCell cell)
    {
        if (!TryGetIndex(tileCell, out int index))
            return false;

        EnsureCellCapacity();
        cells[index] = cell;
        return true;
    }

    public bool TryConfigureCell(
        Vector2Int tileCell,
        int elevation,
        Dev_WaterTerrainDefinition terrain,
        float initialWaterDepth,
        bool initialWaterSource,
        bool exists = true)
    {
        if (!TryGetIndex(tileCell, out int index))
            return false;

        EnsureCellCapacity();
        cells[index].Configure(
            exists,
            elevation,
            terrain,
            initialWaterDepth,
            initialWaterSource);
        return true;
    }

    private bool TryGetIndex(Vector2Int tileCell, out int index)
    {
        int localX = tileCell.x - origin.x;
        int localY = tileCell.y - origin.y;
        if (localX < 0 || localY < 0 || localX >= Width || localY >= Height)
        {
            index = -1;
            return false;
        }

        index = localY * Width + localX;
        return true;
    }
}
