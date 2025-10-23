using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class MapLoader : MonoBehaviour
{
    // Initialization
    public Dictionary<Sprite, TileType> spriteTileMapping {
        get { return spriteTileMapping; }
        private set { spriteTileMapping = value; } 
    }
    public Dictionary<TileType, Sprite> tileTypeSpriteMapping
    {
        get { return tileTypeSpriteMapping; }
        private set { tileTypeSpriteMapping = value; }
    }
    public IReadOnlyDictionary<Sprite, TileType> SpriteTileMapping => spriteTileMapping;

    [SerializeField] private TileType grass_tile;
    [SerializeField] private TileType beach_tile;
    [SerializeField] private TileType forest_tile;
    [SerializeField] private TileType mountain_tile;
    [SerializeField] private TileType infra_tile;
    [SerializeField] private TileType city_tile;
    [SerializeField] private TileType water_tile;

    public Tilemap terrainMap;

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
            foreach (var spriteRange in tileType.sprites)
            {
                spriteTileMapping[spriteRange.sprite] = tileType;
            }
        }
    }

    private void LoadTerrainMap()
    {
        // Bounds and progress bar setup
        BoundsInt bounds = terrainMap.cellBounds;
        int total = (bounds.xMax - bounds.xMin) * (bounds.yMax - bounds.yMin);
        int current = 0;

        // Iterate through each cell in the Tilemap
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    // Progress bar
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                    "Loading Terrain",
                    $"Processing tile {current}/{total}",
                    current / (float)total))
                    {
                        break;
                    }
                    current++;

                    // Retrieve tile data
                    Vector3Int pos = new Vector3Int(x, y, z);
                    Tile tile = terrainMap.GetTile(pos) as Tile;
                    if (tile == null) {
                        Debug.LogWarning($"No tile found at position {pos}");
                        continue;
                    }

                    // Look up dictionary entry for this sprite
                    if (LookupTileTypeForSprite(tile.sprite) != null)
                    {
                        TileType tileType = LookupTileTypeForSprite(tile.sprite);

                        // Create TileInstance GameObject
                        GameObject tileGO = new GameObject($"Tile_{x}_{y}");
                        TileInstance tileInstance = tileGO.AddComponent<TileInstance>();
                        tileInstance.tileType = tileType;
                        tileInstance.x = x;
                        tileInstance.y = y;
                        // tileInstance.elevation = tile.z;
                        // tileInstance.water = tile.water;
                        // tileInstance.population = tile.population;
                        // tileInstance.econVal = tile.econVal;
                        // tileInstance.damage = tile.damage;
                        // tileInstance.casualties = tile.casualties;

                        // Position the TileInstance in the world
                        tileGO.transform.position = new Vector3(x, y, 0);
                    }
                }
            }
        }
        UnityEditor.EditorUtility.ClearProgressBar();
    }

    //Helper methods
    private Sprite LookupSpriteForTileType(TileType tileType, int elevation)
    {
        foreach (var spriteRange in tileType.sprites)
        {
            if (elevation >= spriteRange.min && elevation <= spriteRange.max)
            {
                return spriteRange.sprite;
            }
        }
        return null; // or a default sprite
    }

    private TileType LookupTileTypeForSprite(Sprite sprite)
    {
        if (spriteTileMapping.TryGetValue(sprite, out TileType tileType))
        {
            return tileType;
        }
        return null; // or a default TileType
    }
}

