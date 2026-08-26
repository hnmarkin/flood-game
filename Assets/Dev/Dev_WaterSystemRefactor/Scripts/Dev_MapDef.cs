using UnityEngine;

/// <summary>
/// Authored map layout and initial water conditions consumed by the Dev Water System.
/// It owns the map bounds and cell definitions, but never live flow or simulation history.
/// </summary>
[CreateAssetMenu(fileName = "Dev_MapDef", menuName = "Dev/Water System/Map Definition")]
public sealed class Dev_MapDef : ScriptableObject
{
    [SerializeField] private Vector2Int origin;
    [Min(1)]
    [SerializeField] private int width = 1;
    [Min(1)]
    [SerializeField] private int height = 1;
    [SerializeField] private Dev_MapCellDef[] cells;

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
            Dev_MapCellDef[] previous = cells;
            cells = new Dev_MapCellDef[required];

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
                cells[i] = new Dev_MapCellDef();
        }
    }

    public bool TryGetCell(Vector2Int tileCell, out Dev_MapCellDef cell)
    {
        cell = null;
        if (!TryGetIndex(tileCell, out int index) || cells == null || index >= cells.Length)
            return false;

        cell = cells[index];
        return cell != null && cell.Exists;
    }

    public bool TrySetCell(Vector2Int tileCell, Dev_MapCellDef cell)
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
        Dev_TerrainTypeDef terrain,
        float initialWaterDepth,
        bool initialWaterBody,
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
            initialWaterBody);
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
