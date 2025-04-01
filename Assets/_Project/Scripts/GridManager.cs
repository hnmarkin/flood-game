using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static WaterBlock;

public class GridManager : MonoBehaviour
{
    //public List<WaterObststacle> obststacles = new List<WaterObststacle>();
    public int gridWidth = 15;
    public int gridHeight = 15;
    public GameObject waterPrefab;  // Prefab with the waterBlock script attached
    public List<WaterBlock> connectedWater = new List<WaterBlock>();
    //List<Tile> tilesToAdd = new List<Tile>();
    [SerializeField] Tilemap tileMap;
    //[SerializeField] Tilemap water;
    //WaterTile waterTile;
    [SerializeField] float tileWidth = 3f; 
    [SerializeField] float tileHeight = 1.5f;
    public int waterlevel;
    public int waterAmt;
    [SerializeField] public static WaterBlock[,] waterGrid;
    public static GridManager Instance;
    [SerializeField] float tickTimerMax = 1f;
    [SerializeField] public FloodData floodData;
    [SerializeField] int maxTicks = 20;
    int totalHomes = 0;
    int totalBiz = 0;
    public int hurtBiz = 0;
    public int hurtHome = 0;
    public int totalDeathes;
    public int totalCost;

    private void Start()
    {
        Instance = this;
        waterGrid = new WaterBlock[gridWidth, gridHeight]; //init empty grid
        
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

        AddBizBlk(4, 6, 10);
        AddBizBlk(4, 2, 10);
        AddBizBlk(4, 3, 10);
        AddHomeBlk(4, 4, 10);
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
        floodData.businessesAffectedPercent = 0;
        floodData.homesFloodedPercent = 0;
        floodData.infrastructureDamagePercent = 0;
        floodData.utilityDowntimeHours = 0;
        floodData.economicLosses = 0;
        floodData.casualties = 0;
    }
    public void OvertakeWater(int x, int y)
    {
        if(waterGrid[x, y] == null)
        {
            AddWaterBlk(x, y);
        }
        else if (waterGrid[x, y].type == TileType.Home)
        {
            hurtHome++;
            totalCost += waterGrid[x, y].cost;
            AddWaterBlk(x, y);
        }
        else if (waterGrid[x, y].type == TileType.Water)
        {
            return;
        }
        else if (waterGrid[x, y].type == TileType.Biz)
        {
            hurtBiz++;
            totalCost += waterGrid[x, y].cost;
            AddWaterBlk(x, y);
        }
        else if (waterGrid[x, y].type == TileType.Wall)
        {
            AddWaterBlk(x, y);
        }
        else
        {
            Debug.Log("Anomally occured when spreading water");
            AddWaterBlk(x, y);
        }
    }
    //FIXME: /fill helper function
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
        tile.height = h;
        if (waterGrid[x, y] != null) { Debug.Log("Tried to make wall on top of existing water!"); }
        waterGrid[x, y] = tile;
        tile.type = TileType.Wall;
        //tile.rend.color = Color.black;
    }
    public void AddBizBlk(int x, int y, int cost)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;

        Vector3 position = new Vector3(isoX, isoY, 0); //spawn water
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        tile.xloc = x;
        tile.yloc = y;
        if (waterGrid[x, y] != null) { Debug.Log("Tried to make water on top of existing water!"); }
        waterGrid[x, y] = tile;
        tile.type = TileType.Biz;
        tile.cost = cost;
        totalBiz++;
    }
    public void AddHomeBlk(int x, int y, int cost)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;

        Vector3 position = new Vector3(isoX, isoY, 0); //spawn water
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        tile.xloc = x;
        tile.yloc = y;
        if (waterGrid[x, y] != null) { Debug.Log("Tried to make water on top of existing water!"); }
        waterGrid[x, y] = tile;
        tile.type = TileType.Home;
        tile.cost = cost;
        totalHomes++;
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
        tile.type = TileType.Water;
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
        tile.type = TileType.Water;
    }

    /*
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
    */
    void SpreadTick() // FIXME: BRUH WE GOTTA FIX THIS MESS
    {
        //FIXME: Calc water mass changes
        foreach (WaterBlock tile in connectedWater)
        {
            if (tile.src == true)
            {
                waterAmt += tile.amt;
            }
        }

        if (connectedWater.Count < waterAmt)
        {
            for (int x = 0; x < gridWidth; x++) //calc borders
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (waterGrid[x, y] != null)
                    {
                        if (waterGrid[x, y].type == TileType.Water)
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
                        //Debug.Log(waterGrid[x, y].border);
                        //Debug.Log(waterGrid[x, y].type);
                        if (waterGrid[x, y].border && connectedWater.Count < waterAmt && waterGrid[x, y].type == TileType.Water)
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
                        if (waterGrid[x, y].type != TileType.Wall)
                        {
                            waterGrid[x, y].checkBorder();
                        }
                    }
                }
            }
            //if no more spread raise level
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
    int count = 0;
    void setScore()
    {
        floodData.businessesAffectedPercent = (hurtBiz / totalBiz)*100;
        floodData.homesFloodedPercent = (hurtHome / totalHomes)*100;
        floodData.casualties = totalDeathes;
        floodData.economicLosses = totalCost;
    }
    private void Update()
    {

        tickTimer += Time.deltaTime;
        //Debug.Log(tickTimer);
        if (tickTimer >= tickTimerMax && count < 20)
        {
            tickTimer -= tickTimerMax;
            StartCoroutine(tick());
            count++;
        }
        else if(tickTimer >= tickTimerMax && count >= 20)
        {
            setScore();
            SceneManager.LoadScene("ScoringScene");
            SceneManager.UnloadScene("WaterTest");
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPoint;
            worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var tpos = tileMap.WorldToCell(worldPoint);
            tpos.x += 4;
            tpos.y += 13;
            Debug.Log(tpos);
            if (tpos.x > 0 && tpos.y > 0 && tpos.x <= 14 && tpos.y <= 14)
            {
                if (waterGrid[tpos.x, tpos.y] != null)
                {
                    if (waterGrid[tpos.x, tpos.y].type == TileType.Wall && waterGrid[tpos.x, tpos.y].height <= 1)
                    {
                        AddWallBlk(tpos.x, tpos.y, 100);
                    }
                }
                else
                {
                    AddWallBlk(tpos.x, tpos.y, 100);
                }
            }
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

}
