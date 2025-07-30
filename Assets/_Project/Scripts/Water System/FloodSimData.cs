using UnityEngine;
using System;

[CreateAssetMenu(fileName = "FloodSimData", menuName = "Flood/Flood Simulation")]
public class FloodSimData : ScriptableObject
{
    public int N = 10;
    public float dx = 1f, dy = 1f, dt = 1f;
    public float g = 9.81f;
    public float friction = 0.02f;

    [SerializeField] private float basTerrainProbability = 0.05f; // 5% base chance
    [SerializeField] private float adjacentTerrainProbability = 0.10f; // 10% adjacent chance

    public float[,] water;
    public float[,] terrain;
    public float[,] flowX;
    public float[,] flowY;

    // Event that fires when simulation steps
    public event Action OnSimulationStep;

    public void Initialize()
    {
        water = new float[N + 2, N + 2];
        terrain = new float[N + 2, N + 2];
        flowX = new float[N + 2, N + 2];
        flowY = new float[N + 2, N + 2];

        // Generate terrain with probability-based approach
        GenerateTerrain();

        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                water[x, y] = 1f; // some water everywhere

        OnSimulationStep?.Invoke(); // Notify subscribers that initialization is complete
    }

    private void GenerateTerrain()
    {
        // First pass: base chance for each tile
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                if (UnityEngine.Random.Range(0f, 1f) < basTerrainProbability)
                {
                    terrain[x, y] = 1f;
                }
            }
        }

        // Second pass: check for adjacency and give higher chance to adjacent tiles
        bool[,] wasAdjacent = new bool[N, N]; // Track which tiles were adjacent in first pass
        
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
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
                            if (adjX >= 0 && adjX < N && adjY >= 0 && adjY < N && terrain[adjX, adjY] > 0f)
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
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                if (wasAdjacent[x, y] && UnityEngine.Random.Range(0f, 1f) < adjacentTerrainProbability)
                {
                    terrain[x, y] = 1f;
                }
            }
        }
    }

    public void StepSimulation()
    {
        if (water == null) Initialize();

        float frictionFactor = Mathf.Pow(1 - friction, dt);

        // Boundary (you can customize later)
        for (int i = 0; i < N; ++i) {
            flowX[0, i] = flowX[N, i] = 0f;
            flowY[i, 0] = flowY[i, N] = 0f;
        }

        // Accelerate X
        for (int y = 0; y < N; ++y)
            for (int x = 1; x < N; ++x)
                flowX[x, y] = flowX[x, y] * frictionFactor
                    + ((water[x - 1, y] + terrain[x - 1, y]) - (water[x, y] + terrain[x, y])) * g * dt / dx;

        // Accelerate Y
        for (int y = 1; y < N; ++y)
            for (int x = 0; x < N; ++x)
                flowY[x, y] = flowY[x, y] * frictionFactor
                    + ((water[x, y - 1] + terrain[x, y - 1]) - (water[x, y] + terrain[x, y])) * g * dt / dy;

        // Scale outflows
        for (int y = 0; y < N; ++y)
        {
            for (int x = 0; x < N; ++x)
            {
                float totalOutflow = 0f;
                totalOutflow += Mathf.Max(0f, -flowX[x, y]);
                totalOutflow += Mathf.Max(0f, -flowY[x, y]);
                totalOutflow += Mathf.Max(0f, flowX[x + 1, y]);
                totalOutflow += Mathf.Max(0f, flowY[x, y + 1]);

                float maxOutflow = water[x, y] * dx * dy / dt;

                if (totalOutflow > 0f)
                {
                    float scale = Mathf.Min(1f, maxOutflow / totalOutflow);
                    if (flowX[x, y] < 0f) flowX[x, y] *= scale;
                    if (flowY[x, y] < 0f) flowY[x, y] *= scale;
                    if (flowX[x + 1, y] > 0f) flowX[x + 1, y] *= scale;
                    if (flowY[x, y + 1] > 0f) flowY[x, y + 1] *= scale;
                }
            }
        }

        // Update water
        for (int y = 0; y < N; ++y)
            for (int x = 0; x < N; ++x)
                water[x, y] += (
                    flowX[x, y] + flowY[x, y]
                  - flowX[x + 1, y] - flowY[x, y + 1]
                ) * dt / dx / dy;
        
        // Fire the event to notify subscribers that simulation has stepped
        OnSimulationStep?.Invoke();
    }
}
