using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int gridWidth = 10;
    public int gridHeight = 10;
    public GameObject tilePrefab;  // Prefab with the Tile script attached

    private Tile[,] tiles;

    void Awake()
    {
        GenerateGrid();
    }
    void GenerateGrid()
    {
        float tileWidth = 1f;  // Adjust based on your tile's actual width
        float tileHeight = 0.5f; // Adjust for isometric spacing

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
}
