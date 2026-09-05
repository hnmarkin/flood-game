using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Projects renderer definitions and authoritative water depths onto Unity tilemaps.
/// It has no dependency on the former water-map model.
/// </summary>
public class Dev_WaterRenderer : MonoBehaviour
{
    [Header("Tilemaps to Refresh")]
    [SerializeField] private Tilemap[] tilemaps;

    [Header("Fallback Water Tint")]
    [SerializeField] private float depthForDeepColor = 1f;
    [SerializeField] private Color shallowWaterColor = new Color(0.70f, 0.85f, 1.00f, 1f);
    [SerializeField] private Color deepWaterColor = new Color(0.10f, 0.25f, 0.50f, 1f);

    private Dev_WaterState _state;
    private Dev_MapAccessor _map;

    public void Initialize(Dev_WaterState state, Dev_MapAccessor map)
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
        if (!_map.TryGetCell(simX, simY, out Dev_MapCellDef cell))
            return;

        float visualDepth = Mathf.Max(0f, _state.Water[simX, simY]);
        Dev_RendererDef renderer = cell.Terrain != null ? cell.Terrain.RendererDefinition : null;
        TileBase tile = renderer != null ? renderer.ResolveTile(visualDepth) : null;
        Color tint = renderer != null
            ? renderer.ResolveTint(visualDepth)
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

            tilemap.RefreshTile(cell);
            tilemap.SetColor(cell, tint);
        }
    }
}
