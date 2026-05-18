using UnityEngine;
using UnityEngine.Tilemaps;

public class Dev_WaterTilemapRenderer : MonoBehaviour
{
    [SerializeField] private TileMapData tileMapData;

    [Header("Tilemaps to Refresh")]
    [SerializeField] private Tilemap[] tilemaps;

    [Header("Visual Flood Persistence")]
    [SerializeField] private bool persistentFloodVisuals = true;

    [Header("Visual Water Transition")]
    [Min(0f)]
    [SerializeField] private float visualWaterChangePerStep = 1f;

    [Header("Tint")]
    [SerializeField] private float depthForDeepColor = 1f;
    [SerializeField] private Color shallowWaterColor = new Color(0.70f, 0.85f, 1.00f, 1f);
    [SerializeField] private Color deepWaterColor = new Color(0.10f, 0.25f, 0.50f, 1f);

    private Dev_WaterRuntimeState _state;
    private float[,] _visualWater;
    private float[,] _visualTargetWater;

    public void SetTileMapData(TileMapData value)
    {
        tileMapData = value;
    }

    public void SetTilemaps(Tilemap[] value)
    {
        tilemaps = value;
    }

    public void Initialize(Dev_WaterRuntimeState state)
    {
        _state = state;
        if (_state == null)
            return;

        _visualWater = new float[_state.GridWidth, _state.GridHeight];
        _visualTargetWater = new float[_state.GridWidth, _state.GridHeight];

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasTile[x, y])
                    continue;

                _visualWater[x, y] = _state.Water[x, y];
                _visualTargetWater[x, y] = _state.Water[x, y];
            }
        }

        _state.MarkAllExistingDirty();
        ApplyDirty();
    }

    public void ApplyDirty()
    {
        if (_state == null || tileMapData == null || _visualWater == null || _visualTargetWater == null)
            return;

        foreach (Vector2Int tileCell in _state.DirtyCells)
        {
            if (!_state.TryTileToSim(tileCell, out int simX, out int simY))
                continue;

            ApplyCell(tileCell, simX, simY);
        }

        _state.ClearDirty();
    }

    public void ApplyAll()
    {
        if (_state == null)
            return;

        _state.MarkAllExistingDirty();
        ApplyDirty();
    }

    private void ApplyCell(Vector2Int tileCell, int simX, int simY)
    {
        if (!Dev_WaterTileMapDataAdapter.TryGetTile(tileMapData, tileCell, out TileInstance tile))
            return;

        float logicalDepth = Mathf.Max(0f, _state.Water[simX, simY]);
        float targetDepth = persistentFloodVisuals
            ? Mathf.Max(_visualTargetWater[simX, simY], logicalDepth)
            : logicalDepth;

        _visualTargetWater[simX, simY] = targetDepth;

        if (visualWaterChangePerStep <= 0f)
        {
            _visualWater[simX, simY] = targetDepth;
        }
        else
        {
            _visualWater[simX, simY] = Mathf.MoveTowards(
                _visualWater[simX, simY],
                targetDepth,
                visualWaterChangePerStep);
        }

        float visualDepth = _visualWater[simX, simY];
        tile.waterHeight = visualDepth;
        tile.tint = Color.Lerp(
            shallowWaterColor,
            deepWaterColor,
            Mathf.InverseLerp(0f, Mathf.Max(0.0001f, depthForDeepColor), visualDepth));

        if (tile.tileType != null && !(tile.tileType.isWater && tile.tileType.isAnimated))
        {
            Sprite sprite = tile.tileType.GetTileForWaterHeight(visualDepth);
            if (sprite != null)
                tile.sprite = sprite;
        }

        Refresh(tileCell);
    }

    private void Refresh(Vector2Int tileCell)
    {
        var cell = new Vector3Int(tileCell.x, tileCell.y, 0);

        bool refreshed = false;
        if (tilemaps != null)
        {
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap == null)
                    continue;

                tilemap.RefreshTile(cell);
                refreshed = true;
            }
        }

        if (!refreshed)
            TileManager.Instance?.RefreshAt(cell);
    }
}
