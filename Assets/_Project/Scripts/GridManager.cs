using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using UnityEngine.XR;
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
    [SerializeField] GameObject urdone;
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
    [SerializeField] GameObject redness;
    public int wallnum;
    public int budget = 10;
    public int population;
    [SerializeField] InspectorController ic;
    float totalHomes = 0;
    float totalBiz = 0;
    public float hurtBiz = 0;
    public float hurtHome = 0;
    public int totalDeathes;
    public int totalCost;
    GameObject highlight;

    private void Start()
    {
        Instance = this;
        waterGrid = new WaterBlock[gridWidth, gridHeight]; //init empty grid

        SlashFillWater(0, 0, 14, 0, 1); // river


        AddGroundBlk(6, 1, 1); //river wall bot
        AddGroundBlk(5, 1, 1);
        AddGroundBlk(4, 1, 1);
        AddGroundBlk(3, 1, 1);
        AddGroundBlk(2, 1, 1);
        AddGroundBlk(1, 1, 1);
        AddGroundBlk(0, 1, 0);

        SlashFillWall(0, 5, 6, 5, 1); //bot to low border
        SlashFillHome(0, 2, 6, 4, 100); // low homes

        SlashFillWall(7, 1, 7, 14, 2); // low to mid border
        SlashFillBiz(0, 6, 6, 14, 10000); // low biz

        SlashFillWall(7, 1, 14, 1, 2); //river wall mid

        SlashFillHome(12, 2, 14, 10, 1000); // mid homes
        SlashFillBiz(8, 2, 11, 10, 10000); // mid biz
        SlashFillBiz(8, 2, 10, 14, 10000); // mid biz back slit

        SlashFillWall(11, 11, 14, 14, 3);// top border
        SlashFillHome(12, 12, 14, 14, 10000);// top homes



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
        //Debug.Log(waterGrid[x, y].type);
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
    IEnumerator EndFlood()
    {
        setScore();
        floodStart = false;
        yield return new WaitForSeconds(2f);
        LeanTween.moveLocalX(urdone, 0, 0.7f).setEaseOutBack();
        yield return new WaitForSeconds(4f);
        SceneManager.LoadScene("ScoringScene");
        SceneManager.UnloadScene("WaterTest");
    }
    float tickTimer = 0f;
    int count = 0;
    bool floodStart = false;
    private void Update()
    {

        tickTimer += Time.deltaTime;
        if (!floodStart ) tickTimer = 0f;
        //Debug.Log(tickTimer);
        if (tickTimer >= tickTimerMax && count < 24 && floodStart)
        {
            tickTimer -= tickTimerMax;
            StartCoroutine(tick());
            count++;
        }
        else if(tickTimer >= tickTimerMax && count >= 24 && floodStart)
        {
            StartCoroutine(EndFlood());
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPoint;
            worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var tpos = tileMap.WorldToCell(worldPoint);
            tpos.x += 4;
            tpos.y += 13;
            //Debug.Log(tpos);
            if (tpos.x > -1 && tpos.y > -1 && tpos.x <= 14 && tpos.y <= 14)
            {
                if (waterGrid[tpos.x, tpos.y] != null)
                {
                    if (waterGrid[tpos.x, tpos.y].type == TileType.Wall && waterGrid[tpos.x, tpos.y].height <= 1 && wallnum > 0)
                    {
                        AddWallBlk(tpos.x, tpos.y, waterGrid[tpos.x, tpos.y].height+1);
                        wallnum--;
                        UpdateStats();
                    }
                }
                else if(wallnum > 0)
                {
                    AddWallBlk(tpos.x, tpos.y, 100);
                    wallnum--;
                    UpdateStats();
                }
                else
                {
                    Debug.Log(wallnum > 0);
                }
            }
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
                    switch (waterGrid[tpos.x, tpos.y].type)
                    {
                        case TileType.Wall:
                            ic.setInspector("Wall:", "Height: " + waterGrid[tpos.x, tpos.y].height, TileType.Wall);
                            Debug.Log("Height: " + waterGrid[tpos.x, tpos.y].height);
                            break;
                        case TileType.Water:
                            ic.setInspector("Water:", "Water Level: " + waterlevel, TileType.Water);
                            Debug.Log("Water Level: " + waterlevel);
                            break;
                        case TileType.Home:
                            ic.setInspector("Home:", "Cost: " + waterGrid[tpos.x, tpos.y].cost + '\n' + " Population: " + waterGrid[tpos.x, tpos.y].population, TileType.Home);
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
            UpdateStats();
           // Debug.Log("Water " + waterAmt);
           // Debug.Log("Connected " + connectedWater.Count);
           // Debug.Log("Level " + waterlevel);
        }
        else
        {
            yield return null;
        }

    }
    public void BuyWalls()
    {
        if (budget >= 1)
        {
            budget--;
            wallnum++;
        }
        UpdateStats();
    }
    public void riskCheck()
    {

    }   
    public void buyLevee()
    {

    }
    public void UpdateStats()
    {
        ic.waterlevel.text = "Water Level: " + waterlevel.ToString();
        ic.walls.text = "Walls: " + wallnum.ToString();
        ic.pop.text = "Population: " + population.ToString();
        ic.budget.text = "Budget: " + budget.ToString() + 'k';
    }
    public void FloodBegin()
    {

        floodStart = true;
        ic.GoAwayShop();
  
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
                AddGroundBlk(x, y, h);
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
    public void AddGroundBlk(int x, int y, int h)
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
        tile.wall = true;
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
        tile.population = 1000;
        population += 1000;
        tile.rend.sprite = null;
        UpdateStats();
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
        tile.population = 100;
        population += 100;
        tile.rend.sprite = null;
        UpdateStats();
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
        //tileObj.GetComponent<SpriteRenderer>().sortingOrder = waterGrid[x, y].rend.sortingOrder;
        return tileObj;
    }
}
