using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
<<<<<<< HEAD
//using UnityEditor.SearchService;
=======
using UnityEditor;
using UnityEditor.SearchService;
>>>>>>> 875c40a1cd95ff6cf7542c2ba28969f40c2a1a89
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static WaterBlock;

public class GridManager : MonoBehaviour
{
    [SerializeField] InspectorController ic;
    GameObject highlight;
    public int gridWidth = 15;
    public int gridHeight = 15;
    public GameObject waterPrefab;  // Prefab with the waterBlock script attached
    public List<WaterBlock> connectedWater = new List<WaterBlock>();
    [SerializeField] Tilemap tileMap;
    float tileWidth = 3f; 
    float tileHeight = 1.5f;
    public int waterlevel;
    int waterAmt;
    [SerializeField] public static WaterBlock[,] waterGrid;
    public static GridManager Instance;
    float tickTimerMax = 1f;
    [SerializeField] public FloodData floodData;
    [SerializeField] GameObject redness;


    int totalHomes = 0;
    int totalBiz = 0;
    int hurtBiz = 0;
    int hurtHome = 0;
    int totalDeathes;
    int totalCost;

    private void Start()
    {
        Instance = this;
        waterGrid = new WaterBlock[gridWidth, gridHeight]; //init empty grid

        SlashFillWater(0, 0, 14, 0, 1);
        //AddWaterBlk(15, 0, 1);
        
        AddWallBlk(6, 1, 1);
        AddWallBlk(5, 1, 1);
        AddWallBlk(4, 1, 1);
        AddWallBlk(3, 1, 1);
        AddWallBlk(2, 1, 1);
        AddWallBlk(1, 1, 1);


        SlashFillWall(0, 5, 6, 5, 1);
        SlashFillWall(7, 1, 14, 1, 1);
        SlashFillWall(7, 7, 7, 14, 1);
        SlashFillWall(6, 2, 6, 6, 1);
        AddWallBlk(0, 1, 0);
        
        //SlashFillHome()
        AddBizBlk(4, 6, 10);
        AddBizBlk(4, 2, 10);
        AddBizBlk(4, 3, 10);
        AddHomeBlk(4, 4, 10);

        SlashFillWall(11, 11, 14, 14, 2);

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
            totalDeathes += waterGrid[x, y].population;
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
            totalDeathes += waterGrid[x, y].population;
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
    
    void SpreadTick()
    {
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
            //FIXME: spead multiplue times per tick
            for (int x = 0; x < gridWidth; x++) //Spread boarders
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (waterGrid[x, y] != null)
                    {
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
            if (waterAmt > connectedWater.Count)//if no more spread raise level
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

    void setScore()
    {
        floodData.businessesAffectedPercent = (hurtBiz / totalBiz)*100;
        floodData.homesFloodedPercent = (hurtHome / totalHomes)*100;
        floodData.casualties = totalDeathes;
        floodData.economicLosses = totalCost;
    }
    float tickTimer = 0f;
    int count = 0;
    bool floodStart = false;
    private void Update()
    {

        tickTimer += Time.deltaTime;
        if (!floodStart ) tickTimer = 0f;
        //Debug.Log(tickTimer);
        if (tickTimer >= tickTimerMax && count < 20 && floodStart)
        {
            tickTimer -= tickTimerMax;
            StartCoroutine(tick());
            count++;
        }
        else if(tickTimer >= tickTimerMax && count >= 20 && floodStart)
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
            //Debug.Log(tpos);
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
        if (Input.GetKeyDown(KeyCode.S))
        {
            floodStart = true;
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            Vector2 worldPoint;
            worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var tpos = tileMap.WorldToCell(worldPoint);
            tpos.x += 4;
            tpos.y += 13;


            if (tpos.x > -1 && tpos.y > -1 && tpos.x <= 14 && tpos.y <= 14)
            {

                if (waterGrid[tpos.x, tpos.y] != null)
                {
                    Destroy(highlight);
                    highlight = PlaceHighlight(tpos.x, tpos.y);
                    switch(waterGrid[tpos.x, tpos.y].type)
                    {
                        case TileType.Wall:
                            ic.setInspector("Wall:", "Height: " + waterGrid[tpos.x, tpos.y].height, TileType.Wall);
                            Debug.Log("Height: "+waterGrid[tpos.x, tpos.y].height);
                            break;
                        case TileType.Water:
                            ic.setInspector("Water:", "Water Level: " + waterlevel, TileType.Water);
                            Debug.Log("Water Level: " + waterlevel);
                            break;
                        case TileType.Home:
                            ic.setInspector("Home:", "Cost: " + waterGrid[tpos.x, tpos.y].cost + '\n'+" Population: " + waterGrid[tpos.x, tpos.y].population, TileType.Home);
                            Debug.Log("Home: Cost: " + waterGrid[tpos.x, tpos.y].cost + " Population: " + waterGrid[tpos.x, tpos.y].population);
                            break;
                        case TileType.Biz:
                            ic.setInspector("Biz:", "Cost: " + waterGrid[tpos.x, tpos.y].cost + '\n' + " Population: " + waterGrid[tpos.x, tpos.y].population, TileType.Biz);
                            Debug.Log("Biz: Cost: " + waterGrid[tpos.x, tpos.y].cost + " Population: " + waterGrid[tpos.x, tpos.y].population);
                            break;
                    }
                }
                else
                {
                    ic.GoAwayUI();
                    Destroy(highlight);
                }

            }
            else
            {
                ic.GoAwayUI();
                Destroy(highlight);
            }
        }
    }
    IEnumerator tick()
    {
        if(floodStart)
        {
            yield return new WaitForSeconds(2f);
            //WorldTick();
            SpreadTick();
            Debug.Log("Water " + waterAmt);
            Debug.Log("Connected " + connectedWater.Count);
            Debug.Log("Level " + waterlevel);
        }
        else
        {
            yield return null;
        }

    }

    void SlashFillWater(int xstart, int ystart, int xend, int yend)
    {
        int x = xstart;

        while (x <= xend)
        {
            int y = ystart;
            while (y <= yend)
            {
                AddWaterBlk(x, y);
                y++;
            }
            x++;
        }
    }
    void SlashFillWater(int xstart, int ystart, int xend, int yend, int amt)
    {
        int x = xstart;
        
        while (x <= xend)
        {
            int y = ystart;
            while (y <= yend)
            {
                AddWaterBlk(x, y, amt);
                y++;
            }
            x++;
        }
    }
    void SlashFillWall(int xstart, int ystart, int xend, int yend, int h)
    {
        int x = xstart;

        while (x <= xend)
        {
            int y = ystart;
            while (y <= yend)
            {
                AddWallBlk(x, y, h);
                y++;
            }
            x++;
        }
    }
    void SlashFillHome(int xstart, int ystart, int xend, int yend, int cost)
    {
        int x = xstart;

        while (x <= xend)
        {
            int y = ystart;
            while (y <= yend)
            {
                AddHomeBlk(x, y, cost);
                y++;
            }
            x++;
        }
    }
    void SlashFillBiz(int xstart, int ystart, int xend, int yend, int cost)
    {
        int x = xstart;

        while (x <= xend)
        {
            int y = ystart;
            while (y <= yend)
            {
                AddBizBlk(x, y, cost);
                y++;
            }
            x++;
        }
    }
    //FIXME: /fill helper function
    public void AddWallBlk(int x, int y, int h)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;
        //spawn wall
        Vector3 position = new Vector3(isoX, isoY, 0);
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        //Set stats
        tile.xloc = x;
        tile.yloc = y;
        tile.height = h;
        waterGrid[x, y] = tile;
        tile.type = TileType.Wall;
        //debug
        //tile.rend.color = Color.black;
    }
    public void AddBizBlk(int x, int y, int cost)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;
        //spawn Biz
        Vector3 position = new Vector3(isoX, isoY, 0);
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        //Set stats
        tile.xloc = x;
        tile.yloc = y;
        waterGrid[x, y] = tile;
        tile.type = TileType.Biz;
        tile.cost = cost;
        totalBiz++;
        tile.population = 100;
    }
    public void AddHomeBlk(int x, int y, int cost)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;
        //spawn home
        Vector3 position = new Vector3(isoX, isoY, 0);
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        //set stats
        tile.xloc = x;
        tile.yloc = y;
        waterGrid[x, y] = tile;
        tile.type = TileType.Home;
        tile.cost = cost;
        totalHomes++;
        tile.population = 10;   
    }
    public void AddWaterBlk(int x, int y)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;
        //spawn water
        Vector3 position = new Vector3(isoX, isoY, 0);
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        //Set stats
        connectedWater.Add(tile);
        tile.xloc = x;
        tile.yloc = y;
        waterGrid[x, y] = tile;
        tile.type = TileType.Water;

    }
    private void AddWaterBlk(int x, int y, int amt)
    {
        // Isometric conversion formula
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;
        //spawn water
        Vector3 position = new Vector3(isoX, isoY, 0);
        GameObject tileObj = Instantiate(waterPrefab, position, Quaternion.identity, transform);
        WaterBlock tile = tileObj.GetComponent<WaterBlock>();
        //Set stats
        connectedWater.Add(tile);
        tile.xloc = x;
        tile.yloc = y;
        waterGrid[x, y] = tile;
        tile.src = true;
        tile.amt = amt;
        tile.type = TileType.Water;
    }
   GameObject PlaceHighlight(int x, int y)
    {
        float isoX = (x - y) * tileWidth * 0.5f;
        float isoY = (x + y) * tileHeight * 0.5f;
        Vector3 position = new Vector3(isoX, isoY, 0);
        GameObject tileObj = Instantiate(redness, position, Quaternion.identity);
        tileObj.GetComponent<SpriteRenderer>().sortingOrder = waterGrid[x, y].rend.sortingOrder;
        return tileObj;
    }
}
