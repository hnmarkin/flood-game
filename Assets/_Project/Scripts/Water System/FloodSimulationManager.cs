using UnityEngine;
using System;

public class FloodSimulationManager : MonoBehaviour
{
    [Header("Simulation Data")]
    [SerializeField] private FloodSimDataNew simulationData;

    [Header("Runtime Settings")]
    [SerializeField] private bool autoStep = false;
    [SerializeField] private float stepInterval = 0.1f;

    // Events
    public event Action OnSimulationInitialized;
    public event Action OnSimulationStep;

    // Runtime variables
    private float stepTimer = 0f;

    // Properties
    public FloodSimDataNew SimulationData 
    { 
        get => simulationData; 
        set => simulationData = value; 
    }

    public bool IsInitialized => simulationData != null && simulationData.IsInitialized;

    private void Start()
    {
        if (simulationData != null)
        {
            Initialize();
        }
        else
        {
            Debug.LogError("[FloodSimulationManager] No simulation data assigned!");
        }
    }

    private void Update()
    {
        if (autoStep && IsInitialized)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer -= stepInterval;
                StepSimulation();
            }
        }
    }

    public void Initialize()
    {
        if (simulationData == null)
        {
            Debug.LogError("[FloodSimulationManager] Cannot initialize: No simulation data assigned!");
            return;
        }

        // Determine grid dimensions
        int gridWidth = simulationData.GridWidth;
        int gridHeight = simulationData.GridHeight;
        int N = simulationData.N;
        
        // If we have terrain data source, try to match its dimensions
        if (simulationData.TerrainDataSource != null && simulationData.TerrainDataSource.DataLoaded)
        {
            // Find a TerrainLoader to help us convert the data
            TerrainLoader terrainLoader = FindObjectOfType<TerrainLoader>();
            
            if (terrainLoader != null)
            {
                // Calculate offsets to shift terrain coordinates into valid [0, N-1] range
                int offsetX = 0, offsetY = 0;
                if (simulationData.TerrainDataSource.TilePositions.Count > 0)
                {
                    int minX = int.MaxValue, minY = int.MaxValue;
                    foreach (var pos in simulationData.TerrainDataSource.TilePositions)
                    {
                        minX = Mathf.Min(minX, pos.x);
                        minY = Mathf.Min(minY, pos.y);
                    }
                    // Shift negative coordinates to start at 0
                    offsetX = -minX;
                    offsetY = -minY;
                }
                
                float[,] terrainFromData = terrainLoader.ConvertToHeightArray(simulationData.TerrainDataSource, N, N, offsetX, offsetY);
                if (terrainFromData != null)
                {
                    // Initialize arrays
                    simulationData.water = new float[gridWidth, gridHeight];
                    simulationData.terrain = new float[gridWidth, gridHeight];
                    simulationData.flowX = new float[gridWidth, gridHeight];
                    simulationData.flowY = new float[gridWidth, gridHeight];

                    // Copy terrain data from TerrainData (offset by 1 for boundary)
                    for (int y = 0; y < N; y++)
                    {
                        for (int x = 0; x < N; x++)
                        {
                            simulationData.terrain[x + 1, y + 1] = terrainFromData[x, y];
                            // Water should be a thin layer on top of terrain, not a separate height
                            simulationData.water[x + 1, y + 1] = simulationData.startingWaterDepth; // Configurable starting water depth
                        }
                    }

                    // Add boundary walls to prevent water from flowing off edges
                    for (int i = 0; i < gridWidth; i++)
                    {
                        simulationData.terrain[i, 0] = 1.0f;           // Bottom wall
                        simulationData.terrain[i, gridHeight - 1] = 1.0f; // Top wall
                        simulationData.water[i, 0] = 0.0f;             // No water on boundary
                        simulationData.water[i, gridHeight - 1] = 0.0f; // No water on boundary
                    }
                    for (int i = 0; i < gridHeight; i++)
                    {
                        simulationData.terrain[0, i] = 1.0f;           // Left wall
                        simulationData.terrain[gridWidth - 1, i] = 1.0f;  // Right wall
                        simulationData.water[0, i] = 0.0f;             // No water on boundary
                        simulationData.water[gridWidth - 1, i] = 0.0f; // No water on boundary
                    }

                    simulationData.IsInitialized = true;
                    
                    OnSimulationInitialized?.Invoke();
                    OnSimulationStep?.Invoke();
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[FloodSimulationManager] TerrainData source found but no TerrainLoader in scene to convert data");
            }
        }
        
        // Fallback: Initialize with empty terrain
        simulationData.water = new float[gridWidth, gridHeight];
        simulationData.terrain = new float[gridWidth, gridHeight];
        simulationData.flowX = new float[gridWidth, gridHeight];
        simulationData.flowY = new float[gridWidth, gridHeight];

        // Fill with some water everywhere
        for (int y = 1; y <= N; y++)
            for (int x = 1; x <= N; x++)
                simulationData.water[x, y] = simulationData.startingWaterDepth; // Configurable starting water depth

        // Add boundary walls to prevent water from flowing off edges
        for (int i = 0; i < gridWidth; i++)
        {
            simulationData.terrain[i, 0] = 1.0f;           // Bottom wall
            simulationData.terrain[i, gridHeight - 1] = 1.0f; // Top wall
            simulationData.water[i, 0] = 0.0f;             // No water on boundary
            simulationData.water[i, gridHeight - 1] = 0.0f; // No water on boundary
        }
        for (int i = 0; i < gridHeight; i++)
        {
            simulationData.terrain[0, i] = 1.0f;           // Left wall
            simulationData.terrain[gridWidth - 1, i] = 1.0f;  // Right wall
            simulationData.water[0, i] = 0.0f;             // No water on boundary
            simulationData.water[gridWidth - 1, i] = 0.0f; // No water on boundary
        }

        simulationData.IsInitialized = true;
        
        OnSimulationInitialized?.Invoke();
        OnSimulationStep?.Invoke(); // Notify subscribers that initialization is complete
    }

    public void StepSimulation()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[FloodSimulationManager] Cannot step simulation: Not initialized!");
            return;
        }

        int N = simulationData.N;
        float dx = simulationData.dx;
        float dy = simulationData.dy;
        float dt = simulationData.dt;
        float g = simulationData.g;
        float friction = simulationData.friction;

        float frictionFactor = Mathf.Pow(1 - friction, dt);

        // Boundary (you can customize later)
        for (int i = 1; i <= N; ++i) {
            simulationData.flowX[0, i] = simulationData.flowX[N + 1, i] = 0f;
            simulationData.flowY[i, 0] = simulationData.flowY[i, N + 1] = 0f;
        }

        // Accelerate X
        for (int y = 1; y <= N; ++y)
            for (int x = 1; x <= N; ++x)
                simulationData.flowX[x, y] = simulationData.flowX[x, y] * frictionFactor
                    + ((simulationData.water[x - 1, y] + simulationData.terrain[x - 1, y]) - (simulationData.water[x, y] + simulationData.terrain[x, y])) * g * dt / dx;

        // Accelerate Y
        for (int y = 1; y <= N; ++y)
            for (int x = 1; x <= N; ++x)
                simulationData.flowY[x, y] = simulationData.flowY[x, y] * frictionFactor
                    + ((simulationData.water[x, y - 1] + simulationData.terrain[x, y - 1]) - (simulationData.water[x, y] + simulationData.terrain[x, y])) * g * dt / dy;

        // Scale outflows
        for (int y = 1; y <= N; ++y)
        {
            for (int x = 1; x <= N; ++x)
            {
                float totalOutflow = 0f;
                totalOutflow += Mathf.Max(0f, -simulationData.flowX[x, y]);
                totalOutflow += Mathf.Max(0f, -simulationData.flowY[x, y]);
                totalOutflow += Mathf.Max(0f, simulationData.flowX[x + 1, y]);
                totalOutflow += Mathf.Max(0f, simulationData.flowY[x, y + 1]);

                float maxOutflow = simulationData.water[x, y] * dx * dy / dt;

                if (totalOutflow > 0f)
                {
                    float scale = Mathf.Min(1f, maxOutflow / totalOutflow);
                    if (simulationData.flowX[x, y] < 0f) simulationData.flowX[x, y] *= scale;
                    if (simulationData.flowY[x, y] < 0f) simulationData.flowY[x, y] *= scale;
                    if (simulationData.flowX[x + 1, y] > 0f) simulationData.flowX[x + 1, y] *= scale;
                    if (simulationData.flowY[x, y + 1] > 0f) simulationData.flowY[x, y + 1] *= scale;
                }
            }
        }

        // Update water
        for (int y = 1; y <= N; ++y)
            for (int x = 1; x <= N; ++x)
                simulationData.water[x, y] += (
                    simulationData.flowX[x, y] + simulationData.flowY[x, y]
                  - simulationData.flowX[x + 1, y] - simulationData.flowY[x, y + 1]
                ) * dt / dx / dy;
        
        // Keep boundary cells completely dry
        for (int i = 0; i < simulationData.GridWidth; i++)
        {
            simulationData.water[i, 0] = 0.0f;
            simulationData.water[i, simulationData.GridHeight - 1] = 0.0f;
        }
        for (int i = 0; i < simulationData.GridHeight; i++)
        {
            simulationData.water[0, i] = 0.0f;
            simulationData.water[simulationData.GridWidth - 1, i] = 0.0f;
        }
        
        // Fire the event to notify subscribers that simulation has stepped
        OnSimulationStep?.Invoke();
    }

    public void ResetSimulation()
    {
        if (simulationData != null)
        {
            simulationData.IsInitialized = false;
            Initialize();
        }
    }

    // Public methods for external control
    public void StartAutoStepping()
    {
        autoStep = true;
    }

    public void StopAutoStepping()
    {
        autoStep = false;
    }

    public void SetStepInterval(float interval)
    {
        stepInterval = Mathf.Max(0.01f, interval);
    }
}
