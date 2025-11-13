using System.Numerics;
using UnityEngine;
using UnityEngine.Tilemaps;

enum BlanketTypes
{
    Full,
    Edges,
    Corners
}

public class WaterSimulator : MonoBehaviour
{
    [SerializeField] private FloodSimData simulationData;
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainMap;
    [SerializeField] private float waterHeight;
    [SerializeField] private BlanketTypes blanketType;

    private void Start()
    {
        if (simulationData != null) {
            //ApplyWaterBlanket(tileMapData.rangeX, tileMapData.rangeY, waterHeight, blanketType);
        }
        else
        {
            Debug.LogError("[FloodSimulationManager] No simulation data assigned!");
        }
    }


    // Update is called once per frame
    void Update()
    {

    }

    void ApplyWaterBlanket(Vector2Int rangeX, Vector2Int rangeY, float waterHeight, BlanketTypes blanketType)
    {
        // Implementation for applying water blanket to the tilemap
        switch (blanketType)
        {
            case BlanketTypes.Full:
                for (int x = 0; x < rangeX.y; x++)
                {
                    for (int y = 0; y < rangeY.y; y++)
                    {
                        //Placeholder
                        Vector2Int pos = new Vector2Int(x, y);
                        tileMapData.SetWater(pos, waterHeight);
                        //tileMapData.SetSprite(pos, );
                        Debug.Log($"Set water height at ({x},{y}) to {waterHeight}");
                    }
                }
                break;
            case BlanketTypes.Edges:
                Debug.LogWarning("BlanketType Edges not yet implemented!");
                break;
            case BlanketTypes.Corners:
                Debug.LogWarning("BlanketType Corners not yet implemented!");
                break;
            default:
                Debug.LogError("Invalid BlanketType");
                break;
        }

        Debug.Log($"Applying {blanketType} water blanket with water height {waterHeight}");
    }
}
