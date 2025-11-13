using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class MapLoader : MonoBehaviour
{
    // Initialization
    private Dictionary<TileBase, TileType> _spriteTileMapping = new Dictionary<TileBase, TileType>();
    private Dictionary<TileType, TileBase> _tileTypeSpriteMapping = new Dictionary<TileType, TileBase>();
    public IReadOnlyDictionary<TileBase, TileType> SpriteTileMapping => _spriteTileMapping;
    public IReadOnlyDictionary<TileType, TileBase> TileTypeSpriteMapping => _tileTypeSpriteMapping;

    [SerializeField] private TileType grass_tile;
    [SerializeField] private TileType beach_tile;
    [SerializeField] private TileType forest_tile;
    [SerializeField] private TileType mountain_tile;
    [SerializeField] private TileType infra_tile;
    [SerializeField] private TileType city_tile;
    [SerializeField] private TileType water_tile;

    public Tilemap terrainMap;
    public TileMapData tileMapData;

    private void Awake()
    {
        LoadSpriteTileMapping();
        LoadTerrainMap();
    }

    // Initialize dictionary mappings based on ScriptableObjects
    private void LoadSpriteTileMapping()
    {
        TileType[] tileTypes = new TileType[]
        {
            grass_tile,
            beach_tile,
            forest_tile,
            mountain_tile,
            infra_tile,
            city_tile,
            water_tile
        };

        foreach (TileType tileType in tileTypes)
        {
            foreach (var tileBaseRange in tileType.tileBases)
            {
                _spriteTileMapping[tileBaseRange.tileBase] = tileType;
            }
        }
    }

    private void LoadTerrainMap()
    {
        // Bounds and progress bar setup
        terrainMap.CompressBounds();
        BoundsInt bounds = terrainMap.cellBounds;
        // Correction to start at 0
        // if (bounds.xMin < 0) bounds.xMin = 0;
        // if (bounds.yMin < 0) bounds.yMin = 0;
        // if (bounds.zMin < 0) bounds.zMin = 0;

        Debug.Log($"Loading terrain map with bounds: {bounds}");
        // Iterate through each cell in the Tilemap
        tileMapData.rangeX = new Vector2Int(bounds.xMin, bounds.xMax);
        tileMapData.rangeY = new Vector2Int(bounds.yMin, bounds.yMax);
        tileMapData.rangeZ = new Vector2Int(bounds.zMin, bounds.zMax);

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    // // Progress bar
                    // if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                    // "Loading Terrain",
                    // $"Processing tile {current}/{total}",
                    // current / (float)total))
                    // {
                    //     break;
                    // }
                    // current++;

                    // Retrieve tile data
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (!terrainMap.GetTile(pos))
                    {
                        Debug.LogWarning($"No tile found at position {pos}");
                        continue; // No tile at this position
                    }
                    TileBase tile = terrainMap.GetTile(pos) as TileBase;

                    // Look up dictionary entry for this tileBase
                    if (LookupTileTypeForSprite(tile) != null)
                    {
                        Debug.Log($"Found tile at {pos} with tileBase {tile}");
                        TileType tileType = LookupTileTypeForSprite(tile);
                        TileBase sprite = LookupSpriteForTileType(tileType, 0); // Placeholder water height = 0

                        // Create TileInstance GameObject
                        TileInstance tileInstance = new TileInstance();
                        tileInstance.tileType = tileType;
                        tileInstance.x = x;
                        tileInstance.y = y;
                        tileInstance.elevation = z;
                        // Placeholder values
                        tileInstance.waterHeight = 0;
                        tileInstance.population = 1000;
                        tileInstance.econVal = 1;
                        tileInstance.damage = 0;
                        tileInstance.casualties = 0;

                        // Assign to tileMapData
                        tileMapData.SetTileInstanceAt(new Vector2Int(x,y), tileInstance);
                    }
                    else
                    {
                        Debug.LogWarning($"No TileType mapping found for tileBase {tile} at position {pos}");
                    }
                }
            }
        }
    }

    //Helper methods
    private TileBase LookupSpriteForTileType(TileType tileType, int water)
    {
        foreach (var spriteRange in tileType.tileBases)
        {
            if (water >= spriteRange.min && water <= spriteRange.max)
            {
                return spriteRange.tileBase;
            }
        }
        return null; // or a default tileBase
    }

    private TileType LookupTileTypeForSprite(TileBase tileBase)
    {
        if (_spriteTileMapping.TryGetValue(tileBase, out TileType tileType))
        {
            return tileType;
        }
        return null; // or a default TileType
    }
}

