using UnityEngine;
using System;

[CreateAssetMenu(fileName = "FloodSimData", menuName = "Flood/Flood Simulation")]
public class FloodSimData : ScriptableObject
{
    public int N = 10;
    public float dx = 1f, dy = 1f, dt = 1f;
    public float g = 9.81f;
    public float friction = 0.02f;

    [Header("Terrain Data Source")]
    [SerializeField] private TerrainData terrainDataSource;

    public float[,] water;
    public float[,] terrain;
    public float[,] flowX;
    public float[,] flowY;

    // Event that fires when simulation steps
    public event Action OnSimulationStep;
    
    // Property to access terrain data source
    public TerrainData TerrainDataSource 
    { 
        get => terrainDataSource; 
        set => terrainDataSource = value; 
    }

    public void Initialize()
    {
        // Determine grid dimensions
        int gridWidth = N + 2;
        int gridHeight = N + 2;
        
        // If we have terrain data source, try to match its dimensions
        if (terrainDataSource != null && terrainDataSource.DataLoaded)
        {
            // Find a TerrainLoader to help us convert the data
            TerrainLoader terrainLoader = FindObjectOfType<TerrainLoader>();
            if (terrainLoader != null)
            {
                float[,] terrainFromData = terrainLoader.ConvertToHeightArray(N, N);
                if (terrainFromData != null)
                {
                    // Initialize arrays
                    water = new float[gridWidth, gridHeight];
                    terrain = new float[gridWidth, gridHeight];
                    flowX = new float[gridWidth, gridHeight];
                    flowY = new float[gridWidth, gridHeight];
                    
                    // Copy terrain data from TerrainData (offset by 1 for boundary)
                    for (int y = 0; y < N; y++)
                    {
                        for (int x = 0; x < N; x++)
                        {
                            terrain[x + 1, y + 1] = terrainFromData[x, y];
                            // Water should be a thin layer on top of terrain, not a separate height
                            water[x + 1, y + 1] = 0.1f; // Small amount of water on top of terrain
                        }
                    }
                    
                    Debug.Log($"[FloodSimData] Initialized with terrain data from TerrainData source");
                    OnSimulationStep?.Invoke();
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[FloodSimData] TerrainData source found but no TerrainLoader in scene to convert data");
            }
        }
        
        // Fallback: Initialize with empty terrain
        water = new float[gridWidth, gridHeight];
        terrain = new float[gridWidth, gridHeight];
        flowX = new float[gridWidth, gridHeight];
        flowY = new float[gridWidth, gridHeight];

        // Fill with some water everywhere
        for (int y = 1; y <= N; y++)
            for (int x = 1; x <= N; x++)
                water[x, y] = 0.1f; // Small amount of water on the ground

        Debug.Log($"[FloodSimData] Initialized with empty terrain (no TerrainData source or data not loaded)");
        OnSimulationStep?.Invoke(); // Notify subscribers that initialization is complete
    }

    public void StepSimulation()
    {
        if (water == null) Initialize();

        float frictionFactor = Mathf.Pow(1 - friction, dt);

        // Boundary (you can customize later)
        for (int i = 1; i <= N; ++i) {
            flowX[0, i] = flowX[N + 1, i] = 0f;
            flowY[i, 0] = flowY[i, N + 1] = 0f;
        }

        // Accelerate X
        for (int y = 1; y <= N; ++y)
            for (int x = 1; x <= N; ++x)
                flowX[x, y] = flowX[x, y] * frictionFactor
                    + ((water[x - 1, y] + terrain[x - 1, y]) - (water[x, y] + terrain[x, y])) * g * dt / dx;

        // Accelerate Y
        for (int y = 1; y <= N; ++y)
            for (int x = 1; x <= N; ++x)
                flowY[x, y] = flowY[x, y] * frictionFactor
                    + ((water[x, y - 1] + terrain[x, y - 1]) - (water[x, y] + terrain[x, y])) * g * dt / dy;

        // Scale outflows
        for (int y = 1; y <= N; ++y)
        {
            for (int x = 1; x <= N; ++x)
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
        for (int y = 1; y <= N; ++y)
            for (int x = 1; x <= N; ++x)
                water[x, y] += (
                    flowX[x, y] + flowY[x, y]
                  - flowX[x + 1, y] - flowY[x, y + 1]
                ) * dt / dx / dy;
        
        // Fire the event to notify subscribers that simulation has stepped
        OnSimulationStep?.Invoke();
    }
}
