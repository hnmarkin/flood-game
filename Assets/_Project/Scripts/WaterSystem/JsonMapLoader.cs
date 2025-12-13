using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Newtonsoft.Json;

public class JsonMapLoader : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap roadTilemap;
    public Tilemap buildingTilemap;

    [Header("Tiles")]
    public TileBase landTile;
    public TileBase waterTile;
    public TileBase roadTile;
    public TileBase highwayTile;
    public TileBase buildingTile;
    public TileBase fallbackTile;

    [Header("Simulation Data")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private TileType grass_tile;
    [SerializeField] private TileType beach_tile;
    [SerializeField] private TileType forest_tile;
    [SerializeField] private TileType mountain_tile;
    [SerializeField] private TileType infra_tile;
    [SerializeField] private TileType city_tile;
    [SerializeField] private TileType waterTileType;

    [System.Serializable]
    public class TileCell {
        public int r;
        public int c;
        public string category;    // "land","water","building","road",...
        public string GEOID;       // may be null
        public int pop_total;      // may be 0
        public int elev_level_1_10; // NEW: 1..10
    }

    [System.Serializable]
    public class TilemapDoc {
        public int rows;
        public int cols;
        public double[] bbox_wgs84; // [south, west, north, east]
        public TileCell[] tiles;
        public string elevation_note; // optional
    }

    [Header("Elevation (stacked)")]
    [Tooltip("Provide 10 Tilemaps ordered back-to-front (or top-to-bottom) for levels 1..10.")]
    public Tilemap[] elevationLayers;     // size 10 recommended
    public TileBase elevationTile;        // the block used for each elevation level
    public bool capOnly = false;          // if true, only place the top-most tile instead of full stack

    [Header("File")]
    public string fileName = "tilemap_with_population_elev.json"; // under Assets/StreamingAssets
    [Tooltip("Optional: assign a JSON TextAsset to override fileName/path.")]
    public TextAsset jsonOverride;


    [Header("Tint by Population (optional)")]
    public bool tintByPopulation = false;
    [Tooltip("Tiles with pop_total==minPop will get minColor; ==maxPop get maxColor; linear in between.")]
    public Color minColor = new Color(1f, 1f, 1f, 1f);
    public Color maxColor = new Color(0.2f, 0.7f, 0.2f, 1f);

    [Header("Camera")]
    public Camera sceneCamera;

    // Exposed for other scripts
   [NonSerialized] public TilemapDoc payload;
    [NonSerialized] public int[,] popGrid;
    [NonSerialized] public string[,] geoidGrid;
    [NonSerialized] public string[,] catGrid;
    [NonSerialized] public int rows, cols;
    [NonSerialized] public Dictionary<Vector3Int, Vector2Int> cellToRC
    = new Dictionary<Vector3Int, Vector2Int>();

    private TileType CategoryToTileType(string cat)
            {
                if (string.IsNullOrEmpty(cat)) return grass_tile;

                switch (cat.ToLowerInvariant())
                {
                    case "water":
                        return waterTileType != null ? waterTileType : grass_tile;
                    case "road":
                    case "highway":
                    case "rail":
                        return infra_tile != null ? infra_tile : grass_tile;
                    case "building":
                    case "industrial":
                    case "residential":
                    case "commercial":
                    case "city":
                        return city_tile != null ? city_tile : infra_tile;
                    case "beach":
                        return beach_tile != null ? beach_tile : grass_tile;
                    case "forest":
                    case "park":
                        return forest_tile != null ? forest_tile : grass_tile;
                    case "mountain":
                        return mountain_tile != null ? mountain_tile : grass_tile;
                    case "land":
                    default:
                        return grass_tile;
                }
            }

    void Awake()
    {
        PaintFromJson();
    }

    [ContextMenu("Paint From JSON (Editor)")]
    public void PaintFromJson()
    {
        try
        {
            // 0) Sanity checks for required refs
            if (groundTilemap == null) { Debug.LogError("groundTilemap is not assigned."); return; }

            // 1) Load JSON (TextAsset override OR StreamingAssets path)
            string json;
            string sourceLabel;

            if (jsonOverride != null)
            {
                json = jsonOverride.text;
                sourceLabel = $"TextAsset '{jsonOverride.name}'";
            }
            else
            {
                string path = Path.Combine(Application.streamingAssetsPath, fileName);
                if (!File.Exists(path))
                {
                    Debug.LogError(
                        $"JSON not found at: {path}\n" +
                        "• Ensure the file is at Assets/StreamingAssets/\n" +
                        "• Ensure the component's 'fileName' in the Inspector matches the actual filename\n" +
                        "• Remember Inspector overrides the default value in code."
                    );
                    return;
                }
                json = File.ReadAllText(path);
                var ts = File.GetLastWriteTimeUtc(path);
                sourceLabel = $"StreamingAssets '{path}' (modified UTC {ts:yyyy-MM-dd HH:mm:ss})";
            }

            Debug.Log($"[JsonMapLoader] Loading map JSON from {sourceLabel}.\n" +
                    $"Preview: {(json.Length > 160 ? json.Substring(0,160) + "..." : json)}");

            // 2) Deserialize
            payload = JsonConvert.DeserializeObject<TilemapDoc>(json);
            if (payload == null || payload.tiles == null)
            {
                Debug.LogError("Failed to deserialize payload or payload.tiles is null.");
                return;
            }

            rows = payload.rows;
            cols = payload.cols;

           // ---- NEW: configure tileMapData for WaterSimulator ----
            if (tileMapData != null)
            {
                // Unity grid coords will run from 0..cols-1, 0..rows-1
                tileMapData.rangeX = new Vector2Int(0, cols);
                tileMapData.rangeY = new Vector2Int(0, rows);
                tileMapData.rangeZ = new Vector2Int(0, 1);   // flat for now, unless you use z

                tileMapData.sizeX = cols;
                tileMapData.sizeY = rows;
                tileMapData.sizeZ = 1;

                // Let the simulation size (N) match your JSON, as long as it's within TileMapData capacity.
                tileMapData.N = Mathf.Min(rows, cols);

            }
            else
            {
                Debug.LogError("[JsonMapLoader] tileMapData reference is not assigned!");
            }


            if (rows <= 0 || cols <= 0)
            {
                Debug.LogError($"Invalid rows/cols in JSON. rows={rows} cols={cols}");
                return;
            }

            // 3) Allocate grids
            popGrid  = new int[rows, cols];
            geoidGrid = new string[rows, cols];
            catGrid   = new string[rows, cols];

            // 4) Clear tilemaps
            groundTilemap.ClearAllTiles();
            // roadTilemap?.ClearAllTiles();
            // buildingTilemap?.ClearAllTiles();

            if (elevationLayers != null)
            {
                foreach (var tm in elevationLayers)
                    tm?.ClearAllTiles();
            }

            cellToRC.Clear();

            // 5) Population min/max (if tinting)
            int minPop = int.MaxValue, maxPop = int.MinValue;
            if (tintByPopulation)
            {
                foreach (var t in payload.tiles)
                {
                    if (t.pop_total < minPop) minPop = t.pop_total;
                    if (t.pop_total > maxPop) maxPop = t.pop_total;
                }
                if (minPop == int.MaxValue) { minPop = 0; maxPop = 0; }
            }


            // 6) Paint tiles
            int painted = 0;
            foreach (var t in payload.tiles)
            {
                if (t == null) continue;

                if (t.r < 0 || t.r >= rows || t.c < 0 || t.c >= cols)
                {
                    Debug.LogWarning($"Skipping tile with out-of-range r/c: r={t.r} c={t.c}");
                    continue;
                }

                popGrid[t.r, t.c]  = t.pop_total;
                geoidGrid[t.r, t.c] = t.GEOID;
                catGrid[t.r, t.c]   = t.category;

                var cell = new Vector3Int(t.c, (rows - 1 - t.r), 0); // flip Y so row 0 is top visually

                TileBase tile = CategoryToTile(t.category);
                if (tile == null) tile = (fallbackTile != null ? fallbackTile : landTile);

                // Always paint to groundTilemap now
                groundTilemap.SetTile(cell, tile);
                painted++;


                // 🔹 NEW: create TileInstance for the water simulation grid
                if (tileMapData != null)
                {
                    TileType tileType = CategoryToTileType(t.category);

                    var tileInstance = new TileInstance();
                    tileInstance.tileType    = tileType;
                    tileInstance.x           = cell.x;
                    tileInstance.y           = cell.y;

                    // Use your 1–10 elevation directly, or scale if you like:
                    tileInstance.elevation   = t.elev_level_1_10;
                    tileInstance.waterHeight = 0f;        // initial water = 0, blanket will update this
                    tileInstance.population  = t.pop_total;
                    tileInstance.econVal     = 1;         // placeholder, adjust if you have better data
                    tileInstance.damage      = 0;
                    tileInstance.casualties  = 0;

                    if (tileType != null)
                        tileInstance.sprite = tileType.GetTileForWaterHeight(0f);


                    // This is exactly what your teammate's MapLoader does:
                    tileMapData.SetTileInstanceAt(new Vector2Int(cell.x, cell.y), tileInstance);
                }
            
            
                // Elevation stacking
                int h = Mathf.Clamp(t.elev_level_1_10, 0, (elevationLayers != null ? elevationLayers.Length : 0));
                if (h > 0 && elevationLayers != null)
                {
                    if (elevationTile == null)
                    {
                        // Fallback: stack same tile if no elevationTile assigned
                        int upper = Mathf.Min(h, elevationLayers.Length);
                        for (int i = 0; i < upper; i++)
                            elevationLayers[i]?.SetTile(cell, tile);
                    }
                    else
                    {
                        int upper = Mathf.Min(h, elevationLayers.Length);
                        if (capOnly)
                        {
                            int topIdx = Mathf.Clamp(upper - 1, 0, elevationLayers.Length - 1);
                            elevationLayers[topIdx]?.SetTile(cell, elevationTile);
                        }
                        else
                        {
                            for (int i = 0; i < upper; i++)
                                elevationLayers[i]?.SetTile(cell, elevationTile);
                        }
                    }
                }

                cellToRC[cell] = new Vector2Int(t.r, t.c);

                if (tintByPopulation && maxPop > minPop)
                {
                    float a = Mathf.InverseLerp(minPop, maxPop, t.pop_total);
                    Color tint = Color.Lerp(minColor, maxColor, a);
                    groundTilemap.SetColor(cell, tint);   // 👈 use groundTilemap directly
                }

            }

            // 7) Refresh + frame
            groundTilemap.RefreshAllTiles();
            groundTilemap.CompressBounds();
            FrameCameraToTilemap(1.0f);

            Debug.Log($"[JsonMapLoader] Painted {painted} / {payload.tiles.Length} tiles. " +
                    $"Rows={rows} Cols={cols}. ElevLayers={(elevationLayers!=null?elevationLayers.Length:0)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to paint: {ex.Message}\n{ex.StackTrace}");
        }
    }


    void FrameCameraToTilemap(float padding = 0.5f)
    {
        if (sceneCamera == null || groundTilemap == null) return;

        var b = groundTilemap.localBounds; // local
        var center = groundTilemap.transform.TransformPoint(b.center);
        var size = groundTilemap.transform.TransformVector(b.size);

        var cam = sceneCamera;
        cam.orthographic = true;
        cam.transform.rotation = Quaternion.identity;
        cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);

        float aspect = (float)Screen.width / Screen.height;
        float halfWidth = Mathf.Abs(size.x) * 0.5f;
        float halfHeight = Mathf.Abs(size.y) * 0.5f;
        float sizeForWidth = halfWidth / aspect;
        float sizeForHeight = halfHeight;
        cam.orthographicSize = Mathf.Max(sizeForWidth, sizeForHeight) + padding;
    }

    TileBase CategoryToTile(string cat)
    {
        if (string.IsNullOrEmpty(cat)) return fallbackTile ?? landTile;

        switch (cat.ToLowerInvariant())
        {
            case "land":        return landTile;
            case "water":       return waterTile;
            case "road":        return roadTile;
            case "highway":     return highwayTile != null ? highwayTile : roadTile;
            case "building":    return buildingTile;
            case "park":
            case "forest":      return landTile;
            case "rail":        return roadTile;
            case "industrial":
            case "residential":
            case "commercial":  return buildingTile;
            default:            return fallbackTile != null ? fallbackTile : landTile;
        }
    }

    Tilemap TargetTilemapForCategory(string cat)
    {
        if (string.IsNullOrEmpty(cat)) return groundTilemap;

        switch (cat.ToLowerInvariant())
        {
            case "water":
            case "land":
            case "park":
            case "forest":
                return groundTilemap;

            case "road":
            case "highway":
            case "rail":
                return roadTilemap != null ? roadTilemap : groundTilemap;

            case "building":
            case "industrial":
            case "residential":
            case "commercial":
                return buildingTilemap != null ? buildingTilemap : groundTilemap;

            default:
                return groundTilemap;
        }
    }

    public bool TryGetTileInfoAtWorld(
        Vector3 worldPos, out int r, out int c,
        out string category, out string geoid, out int pop)
    {
        var cell = groundTilemap.WorldToCell(worldPos);

        if (!cellToRC.TryGetValue(cell, out var rc))
        {
            category = geoid = null; pop = 0; r = c = -1;
            return false;
        }

        r = rc.x;   // we stored (r,c) in Vector2Int(x=r, y=c)
        c = rc.y;

        category = catGrid[r, c];
        geoid    = geoidGrid[r, c];
        pop      = popGrid[r, c];
        return true;
    }

}
