using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    public List<WaterObststacle> obststacles = new List<WaterObststacle>();
    public int gridWidth = 10;
    public int gridHeight = 10;
    public GameObject tilePrefab;  // Prefab with the Tile script attached
    List<Tile> allWater = new List<Tile>();
    List<Tile> tilesToAdd = new List<Tile>();


    private Tile[,] tiles;

    void Awake()
    {
        //GenerateGrid();
        //GenerateDemo();
    }
    public Tile GenerateDemo()
    {
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

        tile.gridX = x;
        tile.gridY = y;
        tiles[x, y] = tile;
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
    public void DoWaterTick()
    {
        Debug.Log(allWater.Count);
       // Debug.Log(0 < allWater.Count);
        for (int i = 0; i < allWater.Count; i++)
        {
            Tile t = allWater[i];
            AddNeighbors(t);
        }
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
        //Debug.Log(tile.neighbors[0]);
        
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
            */
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
            if (obststacles[0].transform.position != position)
            {
                GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                Tile l = tileObj.GetComponent<Tile>();

                l.gridX = tile.gridX;
                l.gridY = tile.gridY + 1;
                tile.neighbors[0] = l;
                l.neighbors[2] = tile;
                tilesToAdd.Add(l);
            }

        }
        if (missingR)
        {

            // Isometric conversion formula
            float isoX = (tile.transform.position.x + 1.5f);
            float isoY = (tile.transform.position.y + -0.8f);

            Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric
            if (obststacles[0].transform.position != position)
            {
                GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                Tile l = tileObj.GetComponent<Tile>();

                l.gridX = tile.gridX + 1;
                l.gridY = tile.gridY;
                tile.neighbors[1] = l;
                l.neighbors[3] = tile;
                tilesToAdd.Add(l);
            }

        }

        if (missingD)
        {
            // Isometric conversion formula
            float isoX = (tile.transform.position.x + -1.5f);
            float isoY = (tile.transform.position.y + -0.8f);

            Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric view
            if (obststacles[0].transform.position != position)
            {
                GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                Tile l = tileObj.GetComponent<Tile>();

                l.gridX = tile.gridX;
                l.gridY = tile.gridY - 1;
                tile.neighbors[2] = l;
                l.neighbors[0] = tile;
                tilesToAdd.Add(l);
            }

        }
        if (missingL)
        {
            // Isometric conversion formula
            float isoX = (tile.transform.position.x + -1.5f);
            float isoY = (tile.transform.position.y + 0.8f);

            Vector3 position = new Vector3(isoX, isoY, 0); // Adjust based on your isometric view
            if (obststacles[0].transform.position != position)
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

        }

    }
}
