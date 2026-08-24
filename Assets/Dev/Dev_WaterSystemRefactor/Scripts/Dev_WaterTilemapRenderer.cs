using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Projects new map visual definitions and authoritative runtime depths onto Unity tilemaps.
/// It has no dependency on the former water-map model.
/// </summary>
public class Dev_WaterTilemapRenderer : MonoBehaviour
{
    [Header("Tilemaps to Refresh")]
    [SerializeField] private Tilemap[] tilemaps;

    [Header("Fallback Water Tint")]
    [SerializeField] private float depthForDeepColor = 1f;
    [SerializeField] private Color shallowWaterColor = new Color(0.70f, 0.85f, 1.00f, 1f);
    [SerializeField] private Color deepWaterColor = new Color(0.10f, 0.25f, 0.50f, 1f);

    private Dev_WaterRuntimeState _state;
    private Dev_WaterMapAccessor _map;

    public void Initialize(Dev_WaterRuntimeState state, Dev_WaterMapAccessor map)
    {
        _state = state;
        _map = map;
        if (_state == null || _map == null)
            return;

        _state.MarkAllExistingDirty();
        ApplyDirty();
    }

    public void ApplyDirty()
    {
        if (_state == null || _map == null)
            return;

        foreach (Vector2Int tileCell in _state.DirtyCells)
        {
            if (!_state.TryTileToSim(tileCell, out int simX, out int simY))
                continue;

            ApplyCell(tileCell, simX, simY);
        }

        _state.ClearDirty();
    }

    private void ApplyCell(Vector2Int tileCell, int simX, int simY)
    {
        if (!_map.TryGetCell(simX, simY, out Dev_WaterMapCell cell))
            return;

        float visualDepth = Mathf.Max(0f, _state.Water[simX, simY]);
        Dev_WaterVisualDefinition visual = cell.Terrain != null ? cell.Terrain.VisualDefinition : null;
        TileBase tile = visual != null ? visual.ResolveTile(visualDepth) : null;
        Color tint = visual != null
            ? visual.ResolveTint(visualDepth)
            : Color.Lerp(
                shallowWaterColor,
                deepWaterColor,
                Mathf.InverseLerp(0f, Mathf.Max(0.0001f, depthForDeepColor), visualDepth));

        Refresh(tileCell, tile, tint);
    }

    private void Refresh(Vector2Int tileCell, TileBase tile, Color tint)
    {
        Vector3Int cell = new Vector3Int(tileCell.x, tileCell.y, 0);
        if (tilemaps == null)
            return;

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null)
                continue;

            if (tile != null)
                tilemap.SetTile(cell, tile);

            tilemap.SetColor(cell, tint);
            tilemap.RefreshTile(cell);
        }
    }
}
