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

    private void Start()
    {
        if (simulationData != null) {
            // Nothing
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

    void ApplyWaterBlanket(int centerX, int centerY, int radius, int waterHeight, BlanketTypes blanketType)
    {
        // Implementation for applying water blanket to the tilemap
        // This is a placeholder for the actual logic
        Debug.Log($"Applying {blanketType} water blanket at ({centerX}, {centerY}) with radius {radius} and water height {waterHeight}");
    }
}
