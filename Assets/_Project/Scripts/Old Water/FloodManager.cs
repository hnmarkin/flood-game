using UnityEngine;
using System.Collections;

public class OldFloodSimulationManager : MonoBehaviour
{
    /*
    public GridManager gridManager;
    public float updateInterval = 30f;
    [Range(0f, 1f)]
    public float transferFraction = 0.5f;  // Fraction of the height difference to equalize

    // Define events for pre- and post-flood updates.
    public delegate void FloodEventHandler();
    public static event FloodEventHandler OnPreFloodUpdate;
    public static event FloodEventHandler OnPostFloodUpdate;
    public Tile t;
    
    void Start()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        StartCoroutine(FloodRoutine());
    }

    private void Awake()
    {
        t = gridManager.GenerateDemo();
    }
    IEnumerator FloodRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            RunFloodUpdate();
            gridManager.resolveConflicts();
        }
    }

    void RunFloodUpdate()
    {

        gridManager.DoWaterTick(t);
        // Fire the pre-update event for subscribers.
        OnPreFloodUpdate?.Invoke();

        int width = gridManager.gridWidth;
        int height = gridManager.gridHeight;
        float[,] waterDelta = new float[width, height];

        // First pass: calculate water transfers.
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile current = gridManager.GetTileAt(x, y);
                foreach (Tile neighbor in current.neighbors)
                {
                    // Calculate height difference; only transfer water from higher to lower.
                    float heightDiff = current.WaterHeight - neighbor.WaterHeight;
                    if (heightDiff > 0)
                    {
                        float volumeToTransfer = heightDiff * current.tileArea * transferFraction;
                        volumeToTransfer = Mathf.Min(volumeToTransfer, current.waterVolume);

                        waterDelta[x, y] -= volumeToTransfer;

                        // Assume neighbor stores its grid coordinates (e.g., neighbor.gridX, neighbor.gridY)
                        // so we can update its delta accordingly:
                        waterDelta[neighbor.gridX, neighbor.gridY] += volumeToTransfer;
                    }
                }
            }
        
        }
        
        // Second pass: apply water delta and update flags.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile current = gridManager.GetTileAt(x, y);
                current.waterVolume += waterDelta[x, y];
                current.isOverflowing = current.WaterHeight > (current.terrainHeight + 1f); // Example threshold.
            }
        }
        */
        // Fire the post-update event for subscribers.
        //OnPostFloodUpdate?.Invoke();
    //}
}
