#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One-way editor conversion from the legacy tile model to new Dev Water assets.
/// This is the only script in the refactor that is allowed to reference legacy
/// TileMapData, TileInstance, TileType, or DynamicTile types.
/// </summary>
public static class WaterLegacyMapConverter
{
    [MenuItem("Dev/Water System/Convert Selected Legacy TileMapData")]
    private static void ConvertSelected()
    {
        TileMapData legacyMap = Selection.activeObject as TileMapData;
        if (legacyMap == null)
        {
            EditorUtility.DisplayDialog(
                "Water Map Conversion",
                "Select a legacy TileMapData asset before running the conversion.",
                "OK");
            return;
        }

        string legacyPath = AssetDatabase.GetAssetPath(legacyMap);
        string folder = Path.GetDirectoryName(legacyPath)?.Replace('\\', '/');
        string mapName = legacyMap.name.Replace("TileMapData", "MapDef");
        if (string.IsNullOrWhiteSpace(mapName))
            mapName = "MapDef";

        if (TryConvert(legacyMap, folder, mapName, out MapDef convertedMap))
        {
            Selection.activeObject = convertedMap;
            EditorGUIUtility.PingObject(convertedMap);
            EditorUtility.DisplayDialog(
                "Water Map Conversion",
                $"Converted {legacyMap.name} into {convertedMap.name} and generated new terrain and visual assets.",
                "OK");
        }
    }

    public static bool TryConvert(
        TileMapData legacyMap,
        string outputFolder,
        string mapName,
        out MapDef convertedMap)
    {
        convertedMap = null;

        if (legacyMap == null || !AssetDatabase.IsValidFolder(outputFolder))
        {
            Debug.LogError("[WaterLegacyMapConverter] A valid legacy map and output folder are required.");
            return false;
        }

        ResolveBounds(legacyMap, out Vector2Int origin, out int width, out int height);
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("[WaterLegacyMapConverter] Legacy map has invalid bounds.");
            return false;
        }

        var terrainByLegacyType = new Dictionary<TileType, TerrainTypeDef>();
        var cells = new List<LegacyCell>(width * height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int tileCell = new Vector2Int(origin.x + x, origin.y + y);
                if (!TryReadLegacyCell(legacyMap, tileCell, out TileInstance legacyCell))
                    continue;

                if (legacyCell.tileType == null)
                {
                    Debug.LogWarning($"[WaterLegacyMapConverter] Cell {tileCell} has no TileType and will remain unmapped.");
                    continue;
                }

                if (!terrainByLegacyType.ContainsKey(legacyCell.tileType))
                {
                    string baseName = SanitizeAssetName(legacyCell.tileType.name);
                    RendererDef renderer = CreateRendererDefinition(
                        legacyCell.tileType,
                        outputFolder,
                        baseName);
                    TerrainTypeDef terrain = CreateTerrainTypeDef(
                        legacyCell.tileType,
                        renderer,
                        outputFolder,
                        baseName);

                    terrainByLegacyType.Add(legacyCell.tileType, terrain);
                }

                cells.Add(new LegacyCell(tileCell, legacyCell));
            }
        }

        if (cells.Count == 0)
        {
            Debug.LogError(
                "[WaterLegacyMapConverter] No runtime TileInstance data was found. " +
                "Populate the legacy map before conversion or use the new map authoring tool.");
            return false;
        }

        string mapPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{outputFolder}/{SanitizeAssetName(mapName)}.asset");
        convertedMap = ScriptableObject.CreateInstance<MapDef>();
        var authoredCells = new WaterMapCellAuthoringData[width * height];

        foreach (LegacyCell source in cells)
        {
            TerrainTypeDef terrain = terrainByLegacyType[source.Cell.tileType];
            int x = source.Position.x - origin.x;
            int y = source.Position.y - origin.y;
            authoredCells[y * width + x] = new WaterMapCellAuthoringData(
                true,
                source.Cell.elevation,
                terrain,
                source.Cell.waterHeight,
                source.Cell.tileType.isWater);
        }

        if (!WaterMapDefAuthoring.TryOverwrite(convertedMap, origin, width, height, authoredCells, out string error))
        {
            Debug.LogError($"[WaterLegacyMapConverter] Could not author converted map: {error}");
            Object.DestroyImmediate(convertedMap);
            convertedMap = null;
            return false;
        }

        AssetDatabase.CreateAsset(convertedMap, mapPath);
        EditorUtility.SetDirty(convertedMap);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }

    private static RendererDef CreateRendererDefinition(
        TileType legacyType,
        string outputFolder,
        string baseName)
    {
        var bands = new List<WaterVisualBand>();
        TileBase dryTile = null;

        if (legacyType.tileBases != null)
        {
            for (int i = 0; i < legacyType.tileBases.Length; i++)
            {
                TilebaseRange range = legacyType.tileBases[i];
                TileBase tile = CreateStandardTile(range.sprite, outputFolder, $"{baseName}_Visual_{i}");
                if (dryTile == null || range.min <= 0)
                    dryTile = tile;

                bands.Add(new WaterVisualBand
                {
                    minimumDepth = Mathf.Max(0, range.min),
                    maximumDepth = Mathf.Max(range.min, range.max),
                    tile = tile,
                    tint = Color.white
                });
            }
        }

        string path = AssetDatabase.GenerateUniqueAssetPath(
            $"{outputFolder}/{baseName}_RendererDef.asset");
        var renderer = ScriptableObject.CreateInstance<RendererDef>();
        renderer.Configure(dryTile, Color.white, bands.ToArray());
        AssetDatabase.CreateAsset(renderer, path);
        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static TerrainTypeDef CreateTerrainTypeDef(
        TileType legacyType,
        RendererDef renderer,
        string outputFolder,
        string baseName)
    {
        string path = AssetDatabase.GenerateUniqueAssetPath(
            $"{outputFolder}/{baseName}_TerrainTypeDef.asset");
        var terrain = ScriptableObject.CreateInstance<TerrainTypeDef>();
        terrain.Configure(
            string.IsNullOrWhiteSpace(legacyType.tileName) ? legacyType.name : legacyType.tileName,
            true,
            1f,
            renderer);
        AssetDatabase.CreateAsset(terrain, path);
        EditorUtility.SetDirty(terrain);
        return terrain;
    }

    private static TileBase CreateStandardTile(Sprite sprite, string outputFolder, string baseName)
    {
        if (sprite == null)
            return null;

        string path = AssetDatabase.GenerateUniqueAssetPath(
            $"{outputFolder}/{SanitizeAssetName(baseName)}.asset");
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.flags = TileFlags.None;
        tile.colliderType = Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    private static bool TryReadLegacyCell(TileMapData legacyMap, Vector2Int position, out TileInstance cell)
    {
        cell = null;
        if (position.x < 0 || position.y < 0 || position.x >= legacyMap.sizeX || position.y >= legacyMap.sizeY)
            return false;

        try
        {
            cell = legacyMap.Get(position);
            return cell != null;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[WaterLegacyMapConverter] Could not read legacy cell {position}: {exception.Message}");
            return false;
        }
    }

    private static void ResolveBounds(
        TileMapData legacyMap,
        out Vector2Int origin,
        out int width,
        out int height)
    {
        int xMin = legacyMap.rangeX.x;
        int xMax = legacyMap.rangeX.y;
        int yMin = legacyMap.rangeY.x;
        int yMax = legacyMap.rangeY.y;

        if (xMax <= xMin)
        {
            xMin = 0;
            xMax = legacyMap.N > 0 ? legacyMap.N : legacyMap.sizeX;
        }

        if (yMax <= yMin)
        {
            yMin = 0;
            yMax = legacyMap.N > 0 ? legacyMap.N : legacyMap.sizeY;
        }

        origin = new Vector2Int(xMin, yMin);
        width = Mathf.Max(0, xMax - xMin);
        height = Mathf.Max(0, yMax - yMin);
    }

    private static string SanitizeAssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "WaterAsset";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid.ToString(), "_");

        return value.Replace(' ', '_');
    }

    private readonly struct LegacyCell
    {
        public LegacyCell(Vector2Int position, TileInstance cell)
        {
            Position = position;
            Cell = cell;
        }

        public Vector2Int Position { get; }
        public TileInstance Cell { get; }
    }
}
#endif
