using UnityEngine;

/// <summary>
/// Authored map layout and initial water conditions consumed by the Dev Water System.
/// It owns the map bounds and cell definitions, but never live flow or simulation history.
/// </summary>
[CreateAssetMenu(fileName = "MapDef", menuName = "Dev/Water System/Map Definition")]
public sealed class MapDef : ScriptableObject
{
    [SerializeField] private Vector2Int origin;
    [Min(1)]
    [SerializeField] private int width = 1;
    [Min(1)]
    [SerializeField] private int height = 1;
    [SerializeField] private MapCellDef[] cells;

    public Vector2Int Origin => origin;
    public int Width => Mathf.Max(0, width);
    public int Height => Mathf.Max(0, height);
    public int CellCount => Width * Height;

    public bool IsValidForProduction(out string error)
    {
        if (width <= 0 || height <= 0)
        {
            error = "Map width and height must be positive.";
            return false;
        }

        long expectedCellCount = (long)width * height;
        if (expectedCellCount > int.MaxValue)
        {
            error = "Map dimensions are too large.";
            return false;
        }

        if (cells == null || cells.Length != expectedCellCount)
        {
            error = $"Map must contain exactly {expectedCellCount} cell definitions.";
            return false;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
            {
                error = $"Map cell {i} is missing.";
                return false;
            }

            if (!cells[i].IsValidForProduction(out error))
            {
                error = $"Map cell {i} is invalid: {error}";
                return false;
            }
        }

        error = null;
        return true;
    }

    public bool TryGetCell(Vector2Int tileCell, out MapCellDef cell)
    {
        cell = null;
        if (!TryGetIndex(tileCell, out int index) || cells == null || index >= cells.Length)
            return false;

        cell = cells[index];
        return cell != null && cell.Exists;
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
