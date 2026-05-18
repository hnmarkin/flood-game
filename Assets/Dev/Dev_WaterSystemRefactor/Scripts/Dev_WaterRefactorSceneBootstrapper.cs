using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class Dev_WaterRefactorSceneBootstrapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private TileType groundTileType;
    [SerializeField] private TileType waterTileType;
    [SerializeField] private DynamicTile groundDynamicTile;
    [SerializeField] private DynamicTile waterDynamicTile;

    [Header("Layout")]
    [Min(4)]
    [SerializeField] private int width = 20;
    [Min(4)]
    [SerializeField] private int height = 20;
    [SerializeField] private Vector2Int origin = Vector2Int.zero;

    [Header("Terrain Shape")]
    [SerializeField] private int rimHeight = 8;
    [SerializeField] private int basinDepth = 3;
    [SerializeField] private int channelDepth = 2;
    [SerializeField] private bool createWaterBody = true;
    [Min(0f)]
    [SerializeField] private float initialWaterBodyDepth = 10f;

    [Header("Editor Behavior")]
    [SerializeField] private bool rebuildOnEnable;

    private void OnEnable()
    {
        if (!Application.isPlaying && rebuildOnEnable)
            RebuildScene();
    }

    [ContextMenu("Rebuild Scene")]
    public void RebuildScene()
    {
        if (!ValidateReferences())
            return;

        ConfigureTileMapData();
        terrainTilemap.ClearAllTiles();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int logical = new Vector2Int(origin.x + x, origin.y + y);
                int elevation = ComputeElevation(x, y);
                bool isWaterBody = createWaterBody && IsWaterBodyCell(x, y);

                TileType tileType = isWaterBody ? waterTileType : groundTileType;
                DynamicTile dynamicTile = isWaterBody ? waterDynamicTile : groundDynamicTile;
                float waterDepth = isWaterBody ? initialWaterBodyDepth : 0f;

                var tile = new TileInstance
                {
                    tileType = tileType,
                    x = logical.x,
                    y = logical.y,
                    elevation = elevation,
                    waterHeight = waterDepth,
                    sprite = ResolveSprite(tileType, waterDepth),
                    population = isWaterBody ? 0 : 1000,
                    econVal = isWaterBody ? 0 : 1,
                    damage = 0,
                    casualties = 0,
                    category = isWaterBody ? "water" : "land",
                    tint = Color.white
                };

                tileMapData.SetTileInstanceAt(logical, tile);
                terrainTilemap.SetTile(new Vector3Int(logical.x, logical.y, 0), dynamicTile);
            }
        }

        terrainTilemap.RefreshAllTiles();
        TileManager.Instance?.RefreshAll();
    }

    private bool ValidateReferences()
    {
        if (tileMapData == null)
        {
            Debug.LogError("[Dev_WaterRefactorSceneBootstrapper] TileMapData is missing.");
            return false;
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("[Dev_WaterRefactorSceneBootstrapper] Terrain Tilemap is missing.");
            return false;
        }

        if (groundTileType == null || waterTileType == null)
        {
            Debug.LogError("[Dev_WaterRefactorSceneBootstrapper] TileType references are missing.");
            return false;
        }

        if (groundDynamicTile == null || waterDynamicTile == null)
        {
            Debug.LogError("[Dev_WaterRefactorSceneBootstrapper] DynamicTile references are missing.");
            return false;
        }

        return true;
    }

    private void ConfigureTileMapData()
    {
        tileMapData.sizeX = Mathf.Max(tileMapData.sizeX, origin.x + width);
        tileMapData.sizeY = Mathf.Max(tileMapData.sizeY, origin.y + height);
        tileMapData.sizeZ = Mathf.Max(tileMapData.sizeZ, rimHeight + 4);
        tileMapData.rangeX = new Vector2Int(origin.x, origin.x + width);
        tileMapData.rangeY = new Vector2Int(origin.y, origin.y + height);
        tileMapData.rangeZ = new Vector2Int(0, tileMapData.sizeZ);
        tileMapData.N = width;
        tileMapData.simInitialized = false;
    }

    private int ComputeElevation(int x, int y)
    {
        int edgeDistance = Mathf.Min(x, y, width - 1 - x, height - 1 - y);

        int baseHeight = rimHeight;
        if (edgeDistance >= 2)
            baseHeight -= 2;
        if (edgeDistance >= 4)
            baseHeight -= 2;

        int basinCenterX = width / 2;
        int basinCenterY = height / 2;
        int basinDistance = Mathf.Abs(x - basinCenterX) + Mathf.Abs(y - basinCenterY);
        int basinFalloff = basinDistance / 3;

        int elevation = baseHeight - basinDepth + basinFalloff;

        // Cut a drainage channel toward the southeast edge.
        if (x >= width / 2 && y <= height / 3)
            elevation -= channelDepth;

        // Small ridge to force more interesting spreading.
        if (x == width / 3 && y > height / 3 && y < height - 3)
            elevation += 2;

        return Mathf.Max(0, elevation);
    }

    private bool IsWaterBodyCell(int x, int y)
    {
        return x <= 2 && y >= height - 6 && y <= height - 2;
    }

    private static Sprite ResolveSprite(TileType tileType, float waterDepth)
    {
        if (tileType == null)
            return null;

        Sprite sprite = tileType.GetTileForWaterHeight(waterDepth);
        if (sprite != null)
            return sprite;

        if (tileType.isAnimated && tileType.animationFrames != null && tileType.animationFrames.Length > 0)
            return tileType.animationFrames[0];

        return null;
    }
}
