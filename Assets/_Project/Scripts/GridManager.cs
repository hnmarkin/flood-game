using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    //public List<WaterObststacle> obststacles = new List<WaterObststacle>();
    public int gridWidth = 15;
    public int gridHeight = 15;
    public GameObject waterPrefab;  // Prefab with the waterBlock script attached
    public List<WaterBlock> connectedWater = new List<WaterBlock>();
    //List<Tile> tilesToAdd = new List<Tile>();
   // [SerializeField] Tilemap tilemap;
    //[SerializeField] Tilemap water;
    //WaterTile waterTile;
    [SerializeField] float tileWidth = 3f; 
    [SerializeField] float tileHeight = 1.5f;
    public int waterlevel;
    public int waterAmt;
    [SerializeField] public static WaterBlock[,] waterGrid;
    public static GridManager Instance;
    [SerializeField] float tickTimerMax = 1f;


    private void Start()
    {
        Instance = this;
        waterGrid = new WaterBlock[gridWidth, gridHeight]; //init empty grid
        /*
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Isometric conversion formula
                float isoX = (x - y) * tileWidth * 0.5f;
                float isoY = (x + y) * tileHeight * 0.5f;

                Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric view
                GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
                WaterBlock tile = tileObj.GetComponent<WaterBlock>();

                waterGrid[x, y] = tile;
            }
        }
        */
        
        AddWaterBlk(0, 0, 1);
        AddWaterBlk(1, 0, 1);
        AddWaterBlk(2, 0, 1);
        AddWaterBlk(3, 0, 1);
        AddWaterBlk(4, 0, 1);
        AddWaterBlk(5, 0, 1);
        AddWaterBlk(6, 0, 1);
        AddWaterBlk(7, 0, 1);
        AddWaterBlk(8, 0, 1);
        AddWaterBlk(9, 0, 1);
        AddWaterBlk(10, 0, 1);
        AddWaterBlk(11, 0, 1);
        AddWaterBlk(12, 0, 1);
        AddWaterBlk(13, 0, 1);
        AddWaterBlk(14, 0, 1);
        //AddWaterBlk(15, 0, 1);
        
        AddWallBlk(6, 1, 1);
        AddWallBlk(5, 1, 1);
        AddWallBlk(4, 1, 1);
        AddWallBlk(3, 1, 1);
        AddWallBlk(2, 1, 1);
        AddWallBlk(1, 1, 1);

        AddWallBlk(6, 5, 1);
        AddWallBlk(5, 5, 1);
        AddWallBlk(4, 5, 1);
        AddWallBlk(3, 5, 1);
        AddWallBlk(2, 5, 1);
        AddWallBlk(1, 5, 1);
        AddWallBlk(0, 5, 1);

        AddWallBlk(14, 1, 1);
        AddWallBlk(13, 1, 1);
        AddWallBlk(12, 1, 1);
        AddWallBlk(11, 1, 1);
        AddWallBlk(10, 1, 1);
        AddWallBlk(9, 1, 1);
        AddWallBlk(8, 1, 1);
        AddWallBlk(7, 1, 1);
        /*
        AddWallBlk(6, 1, 0);
        AddWallBlk(5, 1, 0);
        AddWallBlk(4, 1, 0);
        AddWallBlk(3, 1, 0);
        AddWallBlk(2, 1, 0);
        AddWallBlk(1, 1, 0);
        */
        AddWallBlk(0, 1, 0);

        AddWallBlk(7, 14, 1);
        AddWallBlk(7, 13, 1);
        AddWallBlk(7, 12, 1);
        AddWallBlk(7, 11, 1);
        AddWallBlk(7, 10, 1);
        AddWallBlk(7, 9, 1);
        AddWallBlk(7, 8, 1);
        AddWallBlk(7, 7, 1);
        
        AddWallBlk(6, 6, 1);
        AddWallBlk(6, 5, 1);
        AddWallBlk(6, 4, 1);
        AddWallBlk(6, 3, 1);
        AddWallBlk(6, 2, 1);


        AddWallBlk(14, 11, 2);
        AddWallBlk(11, 14, 2);
        AddWallBlk(13, 11, 2);
        AddWallBlk(11, 13, 2);
        AddWallBlk(12, 11, 2);
        AddWallBlk(11, 12, 2);
        AddWallBlk(11, 11, 2);
       // AddWallBlk(7, 7, 2);
        // AddWallBlk(1, 0);
        // AddWallBlk(3, 0);


    }
    private void Awake()
    {
        
    }
    public void AddWallBlk(int x, int y, int h)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;

        Vector3 position = new Vector3(isoX, isoY, 0); //spawn water
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        tile.xloc = x;
        tile.yloc = y;
        tile.ground = true;
        tile.height = h;
        if (waterGrid[x, y] != null) { Debug.Log("Tried to make wall on top of existing water!"); }
        waterGrid[x, y] = tile;
        //tile.rend.color = Color.black;
    }
    public void AddWaterBlk(int x, int y)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;

        Vector3 position = new Vector3(isoX, isoY, 0); //spawn water
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        connectedWater.Add(tile);
        tile.xloc = x;
        tile.yloc = y;
        if (waterGrid[x,y] != null) { Debug.Log("Tried to make water on top of existing water!"); }
        waterGrid[x, y] = tile;
    }
    private void AddWaterBlk(int x, int y, int amt)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;

        Vector3 position = new Vector3(isoX, isoY, 0); //spawn water
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        connectedWater.Add(tile);
        tile.xloc = x;
        tile.yloc = y;
        if (waterGrid[x, y] != null) 
        {
            Debug.Log("Tried to make water on top of existing water!"); 
        }
        waterGrid[x, y] = tile;
        tile.src = true;
        tile.amt = amt;
    }
    void WorldTick()
    {
 
        for (int x = 0; x < gridWidth; x++) //calc borders
        {
            for (int y = 0; y < gridHeight; y++)
            {
               // Debug.Log(waterGrid[x, y]);
                if (waterGrid[x, y] != null)
                {
                    waterGrid[x, y].checkBorder();
                }
            }
        }
        ///FIXME: calculate barriers

    }
    void SpreadTick()
    {
        //Calc water mass changes
        foreach (WaterBlock tile in connectedWater)
        {
            if (tile.src == true)
            {
                waterAmt += tile.amt;
            }
        }
        //FIXME: calc level
        if (connectedWater.Count < waterAmt)
        {
            for (int x = 0; x < gridWidth; x++) //calc borders
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (waterGrid[x, y] != null)
                    {
                        if (!waterGrid[x, y].ground)
                        {
                            waterGrid[x, y].checkBorder();
                        }

                    }
                }
            }

            for (int x = 0; x < gridWidth; x++) //Spread boarders
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (waterGrid[x, y] != null)
                    {
                        if (waterGrid[x, y].border && connectedWater.Count < waterAmt && !waterGrid[x, y].ground)
                        {
                            waterGrid[x, y].spread();
                        }
                    }

                }
            }
            for (int x = 0; x < gridWidth; x++) //calc borders
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (waterGrid[x, y] != null)
                    {
                        if (!waterGrid[x, y].ground)
                        {
                            waterGrid[x, y].checkBorder();
                        }
                    }
                }
            }
            //if no more spread
            if (waterAmt > connectedWater.Count)
            {
                foreach (WaterBlock tile in connectedWater)
                {
                    if(tile.border)
                    {
                        return;
                    }
                }
                waterlevel++;
                waterAmt -= connectedWater.Count;
            }

        }

    }
    float tickTimer = 0f;
    private void Update()
    {

        tickTimer += Time.deltaTime;
        //Debug.Log(tickTimer);
        while (tickTimer >= tickTimerMax)
        {
            tickTimer -= tickTimerMax;
            StartCoroutine(tick());
        }
    }
    IEnumerator tick()
    {
        yield return new WaitForSeconds(2f);
        //WorldTick();
        SpreadTick();
        Debug.Log("Water "+waterAmt);
        Debug.Log("Connected "+connectedWater.Count);
        Debug.Log("Level " + waterlevel);
    }
    /*
    private void Start()
    {
    }
    void Awake()
    {
        //GenerateGrid();
        //GenerateDemo();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var tpos = tilemap.WorldToCell(worldPoint);

            // Try to get a tile from cell position
            var tile = tilemap.GetTile(tpos);
            //Debug.Log(tile);
            Debug.Log(tpos);
            //Debug.Log(worldPoint);

        }
    }
    public Tile GenerateDemo()
    {/*
        WaterTile tile = new WaterTile();
        //Vector3Int zero = new Vector3Int(0, 0, -1);
        water.SetTile(new Vector3Int(0, 0, -1), tile);
        */
        /*

            int x = 2;
            int y = 2;
            float tileWidth = 3f;  // Adjust based on your tile's actual width
            float tileHeight = 1.5f; // Adjust for isometric spacing

            float isoX = (x - y) * tileWidth * 0.5f;
            float isoY = (x + y) * tileHeight * 0.5f;
            tiles = new Tile[gridWidth, gridHeight];

            Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric view
            GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
            Tile tile = tileObj.GetComponent<Tile>();
            tile.upgradeWater();
            /*
            tile.gridX = x;
            tile.gridY = y;
            tiles[x, y] = tile;
            *//*
            tilesToAdd.Add(tile);

            return tile;


        }
        void GenerateGrid()
        {
            float tileWidth = 3f;  // Adjust based on your tile's actual width
            float tileHeight = 1.5f; // Adjust for isometric spacing

            tiles = new Tile[gridWidth, gridHeight];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Isometric conversion formula
                    float isoX = (x - y) * tileWidth * 0.5f;
                    float isoY = (x + y) * tileHeight * 0.5f;

                    Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric view
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    Tile tile = tileObj.GetComponent<Tile>();

                    tile.gridX = x;
                    tile.gridY = y;
                    tiles[x, y] = tile;
                }
            }

            // After instantiation, assign neighbor references to each tile
            AssignNeighbors();
        }


        void AssignNeighbors()
        {
            // For simplicity, assume 4-neighbor connectivity (N, S, E, W)
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Tile current = tiles[x, y];
                    // Create a list (or fixed-size array) for neighbors
                    var neighborList = new System.Collections.Generic.List<Tile>();

                    if (x > 0) neighborList.Add(tiles[x - 1, y]);       // West
                    if (x < gridWidth - 1) neighborList.Add(tiles[x + 1, y]); // East
                    if (y > 0) neighborList.Add(tiles[x, y - 1]);       // South
                    if (y < gridHeight - 1) neighborList.Add(tiles[x, y + 1]); // North

                    current.neighbors = neighborList.ToArray();
                }
            }
        }

        // Optional: Provide helper methods to fetch tiles or perform spatial queries.
        public Tile GetTileAt(int x, int y)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                return tiles[x, y];
            return null;
        }

        public void DoWaterTick(Tile source)
        {
            source.addWater(10f);
            Debug.Log(allWater.Count);
           // Debug.Log(0 < allWater.Count);
            for (int i = 0; i < allWater.Count; i++)
            {
                if (allWater[i].isOverflowing && allWater[i].spread == true)
                {
                    AddNeighbors(allWater[i]);
                    allWater[i].spread = false;
                }
                else
                {
                    allWater[i].upgradeWater();
                }
            }
            Debug.Log(tilesToAdd.Count);
            for (int i = 0; i < tilesToAdd.Count; i++)
            {
                allWater.Add(tilesToAdd[i]);
            }
            tilesToAdd.Clear();
        }

        public void resolveConflicts()
        {
            for (int i = 0; i < allWater.Count; i++)
            {
                for (int j = 0; j < allWater.Count;j++)
                {
                    if (allWater[i].transform.position == allWater[j].transform.position && allWater[i] != allWater[j])
                    {
                        for(int k = 0; k < allWater[i].neighbors.Length; k++)
                        {
                            for (int t = 0; t < allWater[j].neighbors.Length; t++)
                            {
                                if (allWater[j].neighbors[t] != allWater[i].neighbors[k])
                                {
                                    allWater[i].neighbors[k] = allWater[j].neighbors[t];
                                }
                            }
                        }
                        Destroy(allWater[j]);
                        allWater.RemoveAt(j);
                    }

                }
            }
        }
        public void AddNeighbors(Tile tile)
        {
            Debug.Log(tile.neighbors);

            bool missingL = true;
            bool missingR = true;
            bool missingU = true;
            bool missingD = true;

            //if (tile.neighbors.Length == 4) return;
            foreach(Tile n in tile.neighbors)
            {
                /*
                Debug.Log(missingD);
                Debug.Log(missingU);
                Debug.Log(missingL);
                Debug.Log(missingR);
                *//*
                if(n == null) { continue; }
                if (tile.neighbors[0] != null) { missingU = false; }
                if (tile.neighbors[1] != null) { missingR = false; }
                if (tile.neighbors[2] != null) { missingD = false; }
                if (tile.neighbors[3] != null) { missingL = false; }
            }
            if (missingU)
            {
                // Isometric conversion formula
                float isoX = (tile.transform.position.x + 1.5f);
                float isoY = (tile.transform.position.y + 0.8f);

                Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric view
                if (obststacles[0].transform.position != position && obststacles[0].ticks <= 4)
                {
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    Tile l = tileObj.GetComponent<Tile>();

                    l.gridX = tile.gridX;
                    l.gridY = tile.gridY + 1;
                    tile.neighbors[0] = l;
                    l.neighbors[2] = tile;
                    tilesToAdd.Add(l);
                }
                else
                {
                    obststacles[0].tick();
                }

            }
            if (missingR)
            {

                // Isometric conversion formula
                float isoX = (tile.transform.position.x + 1.5f);
                float isoY = (tile.transform.position.y + -0.8f);

                Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric
                if (obststacles[0].transform.position != position && obststacles[0].ticks <= 4)
                {
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    Tile l = tileObj.GetComponent<Tile>();

                    l.gridX = tile.gridX + 1;
                    l.gridY = tile.gridY;
                    tile.neighbors[1] = l;
                    l.neighbors[3] = tile;
                    tilesToAdd.Add(l);
                }
                else
                {
                    obststacles[0].tick();
                }
            }

            if (missingD)
            {
                // Isometric conversion formula
                float isoX = (tile.transform.position.x + -1.5f);
                float isoY = (tile.transform.position.y + -0.8f);

                Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric view
                if (obststacles[0].transform.position != position && obststacles[0].ticks <= 4)
                {
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    Tile l = tileObj.GetComponent<Tile>();

                    l.gridX = tile.gridX;
                    l.gridY = tile.gridY - 1;
                    tile.neighbors[2] = l;
                    l.neighbors[0] = tile;
                    tilesToAdd.Add(l);
                }
                else
                {
                    obststacles[0].tick();
                }
            }
            if (missingL)
            {
                // Isometric conversion formula
                float isoX = (tile.transform.position.x + -1.5f);
                float isoY = (tile.transform.position.y + 0.8f);

                Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric view
                if (obststacles[0].transform.position != position && obststacles[0].ticks <=4)
                {
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    Tile l = tileObj.GetComponent<Tile>();

                    l.gridX = tile.gridX - 1;
                    l.gridY = tile.gridY;
                    tile.neighbors[3] = l;
                    l.neighbors[1] = tile;
                    //tiles[l.gridX, l.gridY] = l;
                    tilesToAdd.Add(l);
                }
                else
                {
                    obststacles[0].tick();
                }
            }

        }
        */

    }
