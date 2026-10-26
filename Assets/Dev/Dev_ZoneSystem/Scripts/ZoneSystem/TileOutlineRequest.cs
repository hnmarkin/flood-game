using UnityEngine;

public struct TileOutlineRequest
{
    public readonly Vector3Int Cell;
    public readonly Color Color;

    public TileOutlineRequest(Vector3Int cell, Color color)
    {
        Cell = cell;
        Color = color;
    }
}
