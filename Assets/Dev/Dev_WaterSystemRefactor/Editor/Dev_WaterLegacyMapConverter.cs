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
public static class Dev_WaterLegacyMapConverter
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
        string mapName = legacyMap.name.Replace("TileMapData", "WaterMapData");
        if (string.IsNullOrWhiteSpace(mapName))
            mapName = "Dev_WaterMapData";

        if (TryConvert(legacyMap, folder, mapName, out Dev_WaterMapData convertedMap))
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
        out Dev_WaterMapData convertedMap)
    {
        convertedMap = null;

        if (legacyMap == null || !AssetDatabase.IsValidFolder(outputFolder))
        {
            Debug.LogError("[Dev_WaterLegacyMapConverter] A valid legacy map and output folder are required.");
            return false;
        }

        ResolveBounds(legacyMap, out Vector2Int origin, out int width, out int height);
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("[Dev_WaterLegacyMapConverter] Legacy map has invalid bounds.");
            return false;
        }

        var terrainByLegacyType = new Dictionary<TileType, Dev_WaterTerrainDefinition>();
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
                    Debug.LogWarning($"[Dev_WaterLegacyMapConverter] Cell {tileCell} has no TileType and will remain unmapped.");
                    continue;
                }

                if (!terrainByLegacyType.ContainsKey(legacyCell.tileType))
                {
                    string baseName = SanitizeAssetName(legacyCell.tileType.name);
                    Dev_WaterVisualDefinition visual = CreateVisualDefinition(
                        legacyCell.tileType,
                        outputFolder,
                        baseName);
                    Dev_WaterTerrainDefinition terrain = CreateTerrainDefinition(
                        legacyCell.tileType,
                        visual,
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
                "[Dev_WaterLegacyMapConverter] No runtime TileInstance data was found. " +
                "Populate the legacy map before conversion or use the new map authoring tool.");
            return false;
        }

        string mapPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{outputFolder}/{SanitizeAssetName(mapName)}.asset");
        convertedMap = ScriptableObject.CreateInstance<Dev_WaterMapData>();
        convertedMap.Configure(origin, width, height);

        foreach (LegacyCell source in cells)
        {
            Dev_WaterTerrainDefinition terrain = terrainByLegacyType[source.Cell.tileType];
            convertedMap.TryConfigureCell(
                source.Position,
                source.Cell.elevation,
                terrain,
                source.Cell.waterHeight,
                source.Cell.tileType.isWater);
        }

        AssetDatabase.CreateAsset(convertedMap, mapPath);
        EditorUtility.SetDirty(convertedMap);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }

    private static Dev_WaterVisualDefinition CreateVisualDefinition(
        TileType legacyType,
        string outputFolder,
        string baseName)
    {
        var bands = new List<Dev_WaterVisualBand>();
        TileBase dryTile = null;

        if (legacyType.tileBases != null)
        {
            for (int i = 0; i < legacyType.tileBases.Length; i++)
            {
                TilebaseRange range = legacyType.tileBases[i];
                TileBase tile = CreateStandardTile(range.sprite, outputFolder, $"{baseName}_Visual_{i}");
                if (dryTile == null || range.min <= 0)
                    dryTile = tile;

                bands.Add(new Dev_WaterVisualBand
                {
                    minimumDepth = Mathf.Max(0, range.min),
                    maximumDepth = Mathf.Max(range.min, range.max),
                    tile = tile,
                    tint = Color.white
                });
            }
        }

        string path = AssetDatabase.GenerateUniqueAssetPath(
            $"{outputFolder}/{baseName}_VisualDefinition.asset");
        var visual = ScriptableObject.CreateInstance<Dev_WaterVisualDefinition>();
        visual.Configure(dryTile, Color.white, bands.ToArray());
        AssetDatabase.CreateAsset(visual, path);
        EditorUtility.SetDirty(visual);
        return visual;
    }

    private static Dev_WaterTerrainDefinition CreateTerrainDefinition(
        TileType legacyType,
        Dev_WaterVisualDefinition visual,
        string outputFolder,
        string baseName)
    {
        string path = AssetDatabase.GenerateUniqueAssetPath(
            $"{outputFolder}/{baseName}_TerrainDefinition.asset");
        var terrain = ScriptableObject.CreateInstance<Dev_WaterTerrainDefinition>();
        terrain.Configure(
            string.IsNullOrWhiteSpace(legacyType.tileName) ? legacyType.name : legacyType.tileName,
            true,
            legacyType.isWater,
            1f,
            visual);
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
            Debug.LogError($"[Dev_WaterLegacyMapConverter] Could not read legacy cell {position}: {exception.Message}");
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
            return "Dev_WaterAsset";

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
