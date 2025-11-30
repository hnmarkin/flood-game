using System.Numerics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

enum BlanketTypes
{
    Full,
    Edges,
    Corners
}

public class WaterSimulator : MonoBehaviour
{
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainMap;
    [SerializeField] private float waterHeight;
    [SerializeField] private BlanketTypes blanketType;

    private void Start()
    {
        if (tileMapData != null)
        {
            ApplyWaterBlanket(tileMapData.rangeX, tileMapData.rangeY, waterHeight, blanketType);
            Initialize();        
        }
        else { Debug.LogError("[FloodSimulationManager] No simulation data assigned!"); }
    }

    public void Initialize()
    {
        if (tileMapData == null)
        {
            Debug.LogError("[TileMapData] No TileMapData assigned!");
            return;
        }
        int gridWidth = tileMapData.GridWidth;
        int gridHeight = tileMapData.GridHeight;
        int N = tileMapData.N;

        tileMapData.terrain = new float[gridWidth, gridHeight];
        tileMapData.water = new float[gridWidth, gridHeight];        
        tileMapData.flowX = new float[gridWidth, gridHeight];
        tileMapData.flowY = new float[gridWidth, gridHeight];

        // Terrain and Water Initialization
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                TileInstance tile = tileMapData.Get(new Vector2Int(x,y));
                tileMapData.terrain[x+1,y+1] = tile.elevation;
                tileMapData.water[x+1,y+1] = tile.waterHeight;
            }
        }
        SetupBoundaryWalls(gridWidth, gridHeight);
        tileMapData.simInitialized = true;
        Debug.Log("[WaterSimulator] Initialized simulation data");
    }

    /// <summary>
    /// Sets up boundary walls to prevent water from flowing off edges
    /// </summary>
    private void SetupBoundaryWalls(int gridWidth, int gridHeight)
    {
        // Add boundary walls to prevent water from flowing off edges
        for (int i = 0; i < gridWidth; i++)
        {
            tileMapData.terrain[i, 0] = 1.0f;           // Bottom wall
            tileMapData.terrain[i, gridHeight - 1] = 1.0f; // Top wall
            tileMapData.water[i, 0] = 0.0f;             // No water on boundary
            tileMapData.water[i, gridHeight - 1] = 0.0f; // No water on boundary
        }
        for (int i = 0; i < gridHeight; i++)
        {
            tileMapData.terrain[0, i] = 1.0f;           // Left wall
            tileMapData.terrain[gridWidth - 1, i] = 1.0f;  // Right wall
            tileMapData.water[0, i] = 0.0f;             // No water on boundary
            tileMapData.water[gridWidth - 1, i] = 0.0f; // No water on boundary
        }
    }

    void ApplyWaterBlanket(Vector2Int rangeX, Vector2Int rangeY, float waterHeight, BlanketTypes blanketType)
    {
        // Local helper to declutter elsewhere
        bool TileExists (Vector2Int p)
        {
            var t = tileMapData.Get(p);   // <-- can use tileMapData from outer method
            if (t == null) {
                Debug.LogWarning($"TileInstance at {p} is null, skipping assignment.");
                return false;
            }
            return true;
        }

        Debug.Log($"Applying {blanketType} water blanket with water height {waterHeight}");
        // Implementation for applying water blanket to the tilemap
        switch (blanketType)
        {
            case BlanketTypes.Full:
                for (int x = 0; x < rangeX.y; x++)
                {
                    for (int y = 0; y < rangeY.y; y++)
                    {
                        //Placeholder
                        Vector2Int posF = new Vector2Int(x, y);
                        if (TileExists(posF) == false) continue;
                        tileMapData.SetWater(posF, waterHeight);
                        Debug.Log($"Set water height at ({x},{y}) to {waterHeight}");
                    }
                }
                break;
            case BlanketTypes.Edges:
                int maxX = rangeX.y-1;
                int maxY = rangeY.y-1;
                for (int x = 0; x < rangeX.y; x++)
                {
                    if (x == 0 || x == maxX) {
                        for (int y = 0; y < rangeY.y; y++)
                        {
                            Vector2Int posE = new Vector2Int(x, y);
                            if (TileExists(posE) == false) continue;
                            tileMapData.SetWater(posE, waterHeight);
                            Debug.Log($"Set water height at ({x},{y}) to {waterHeight}");
                        }
                    }
                    else
                    {
                        Vector2Int posE = new Vector2Int(x,0);
                        Vector2Int posE2 = new Vector2Int(x,maxY);
                        if (TileExists(posE) == false || TileExists(posE2) == false) continue;
                        tileMapData.SetWater(posE,waterHeight);
                        tileMapData.SetWater(posE2,waterHeight);
                        Debug.Log($"Set water height at ({x},{0}) to {waterHeight}");
                        Debug.Log($"Set water height at ({x},{maxY}) to {waterHeight}");
                    }
                }
                break;
            case BlanketTypes.Corners:
                maxX = rangeX.y - 1;
                maxY = rangeY.y - 1;
                var corners = new[] 
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, maxY),
                    new Vector2Int(maxX, 0),
                    new Vector2Int(maxX, maxY),
                };
                foreach (var c in corners) 
                {
                    if (!TileExists(c)) { break; }       // abort if any missing
                    tileMapData.SetWater(c, waterHeight);
                }
                break;
            default:
                Debug.LogError("Invalid BlanketType");
                break;
        }
    }
}
