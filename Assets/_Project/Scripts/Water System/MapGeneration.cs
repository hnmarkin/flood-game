using UnityEngine;

public class MapGeneration : MonoBehaviour
{
    [Header("Map Generation Settings")]
    [SerializeField] private float basTerrainProbability = 0.05f; // 5% base chance
    [SerializeField] private float adjacentTerrainProbability = 0.10f; // 10% adjacent chance
    
    /// <summary>
    /// Generates terrain using probability-based approach
    /// </summary>
    /// <param name="width">Width of the terrain grid</param>
    /// <param name="height">Height of the terrain grid</param>
    /// <returns>2D array of terrain heights</returns>
    public float[,] GenerateTerrain(int width, int height)
    {
        float[,] terrain = new float[width, height];
        
        // First pass: base chance for each tile
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (UnityEngine.Random.Range(0f, 1f) < basTerrainProbability)
                {
                    terrain[x, y] = 1f;
                }
            }
        }

        // Second pass: check for adjacency and give higher chance to adjacent tiles
        bool[,] wasAdjacent = new bool[width, height]; // Track which tiles were adjacent in first pass
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (terrain[x, y] == 0f) // Only check non-terrain tiles
                {
                    bool isAdjacent = false;
                    
                    // Check all 8 adjacent positions (including diagonals)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue; // Skip center tile
                            
                            int adjX = x + dx;
                            int adjY = y + dy;
                            
                            // Check bounds and if adjacent tile has terrain
                            if (adjX >= 0 && adjX < width && adjY >= 0 && adjY < height && terrain[adjX, adjY] > 0f)
                            {
                                isAdjacent = true;
                                break;
                            }
                        }
                        if (isAdjacent) break;
                    }
                    
                    if (isAdjacent)
                    {
                        wasAdjacent[x, y] = true;
                    }
                }
            }
        }
        
        // Apply adjacent chance to adjacent tiles
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (wasAdjacent[x, y] && UnityEngine.Random.Range(0f, 1f) < adjacentTerrainProbability)
                {
                    terrain[x, y] = 1f;
                }
            }
        }
        
        return terrain;
    }
    
    /// <summary>
    /// Generates terrain with custom probabilities
    /// </summary>
    /// <param name="width">Width of the terrain grid</param>
    /// <param name="height">Height of the terrain grid</param>
    /// <param name="baseProbability">Base probability for terrain generation</param>
    /// <param name="adjacentProbability">Probability for adjacent terrain generation</param>
    /// <returns>2D array of terrain heights</returns>
    public float[,] GenerateTerrain(int width, int height, float baseProbability, float adjacentProbability)
    {
        float originalBase = basTerrainProbability;
        float originalAdjacent = adjacentTerrainProbability;
        
        basTerrainProbability = baseProbability;
        adjacentTerrainProbability = adjacentProbability;
        
        float[,] result = GenerateTerrain(width, height);
        
        // Restore original values
        basTerrainProbability = originalBase;
        adjacentTerrainProbability = originalAdjacent;
        
        return result;
    }
}
