using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Generates the deterministic new-format map used by RefactorScene.
/// This is Dev test tooling and writes only MapDef, never legacy tile data.
/// </summary>
[ExecuteAlways]
public class WaterRefactorSceneBootstrapper : MonoBehaviour
{
    [Header("New Water Data")]
    [FormerlySerializedAs("mapData")]
    [SerializeField] private MapDef mapDef;
    [SerializeField] private TerrainTypeDef groundTerrain;
    [SerializeField] private TerrainTypeDef waterTerrain;

    [Header("Preview Tilemap")]
    [SerializeField] private Tilemap terrainTilemap;

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

    private void Awake()
    {
        if (Application.isPlaying)
            RebuildScene();
    }

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

        mapDef.Configure(origin, width, height);
        terrainTilemap.ClearAllTiles();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int logical = new Vector2Int(origin.x + x, origin.y + y);
                int elevation = ComputeElevation(x, y);
                bool isWaterBody = createWaterBody && IsWaterBodyCell(x, y);
                TerrainTypeDef terrain = isWaterBody ? waterTerrain : groundTerrain;
                float waterDepth = isWaterBody ? initialWaterBodyDepth : 0f;

                mapDef.TryConfigureCell(
                    logical,
                    elevation,
                    terrain,
                    waterDepth,
                    isWaterBody);

                RendererDef renderer = terrain != null ? terrain.RendererDefinition : null;
                TileBase tile = renderer != null ? renderer.ResolveTile(waterDepth) : null;
                if (tile != null)
                    terrainTilemap.SetTile(new Vector3Int(logical.x, logical.y, 0), tile);
            }
        }

        terrainTilemap.RefreshAllTiles();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(mapDef);
            AssetDatabase.SaveAssetIfDirty(mapDef);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private bool ValidateReferences()
    {
        if (mapDef == null)
        {
            Debug.LogError("[WaterRefactorSceneBootstrapper] MapDef is missing.");
            return false;
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("[WaterRefactorSceneBootstrapper] Terrain Tilemap is missing.");
            return false;
        }

        if (groundTerrain == null || waterTerrain == null)
        {
            Debug.LogError("[WaterRefactorSceneBootstrapper] New terrain definitions are missing.");
            return false;
        }

        return true;
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

        if (x >= width / 2 && y <= height / 3)
            elevation -= channelDepth;

        if (x == width / 3 && y > height / 3 && y < height - 3)
            elevation += 2;

        return Mathf.Max(0, elevation);
    }

    private bool IsWaterBodyCell(int x, int y)
    {
        return x <= 2 && y >= height - 6 && y <= height - 2;
    }
}
