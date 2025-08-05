using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Example script showing how to set up and use the new NewTerrainData system
/// </summary>
public class NewTerrainDataExample : MonoBehaviour
{
    [Header("Setup References")]
    [SerializeField] private Tilemap sourceTilemap;
    [SerializeField] private NewTerrainData newTerrainData;
    [SerializeField] private FloodSimData floodSimData;
    [SerializeField] private FloodSimulationManager floodSimulationManager;
    
    [Header("Settings")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool loadFromSpecificZLevel = false;
    [SerializeField] private int targetZLevel = 0;
    [SerializeField] private float elevationScale = 1.0f;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupNewTerrainSystem();
        }
    }
    
    /// <summary>
    /// Sets up the complete new terrain system pipeline
    /// </summary>
    [ContextMenu("Setup New Terrain System")]
    public void SetupNewTerrainSystem()
    {
        if (!ValidateReferences())
            return;
            
        Debug.Log("[NewTerrainDataExample] Setting up new terrain system...");
        
        // Step 1: Create and configure NewTerrainLoader
        NewTerrainLoader loader = GetOrCreateNewTerrainLoader();
        
        // Step 2: Load terrain data from tilemap
        bool loadSuccess = LoadTerrainData(loader);
        
        if (!loadSuccess)
        {
            Debug.LogError("[NewTerrainDataExample] Failed to load terrain data");
            return;
        }
        
        // Step 3: Connect NewTerrainData to FloodSimData
        ConnectToFloodSimulation();
        
        // Step 4: Initialize or restart the simulation
        InitializeSimulation();
        
        Debug.Log("[NewTerrainDataExample] New terrain system setup complete!");
    }
    
    /// <summary>
    /// Validates that all required references are assigned
    /// </summary>
    private bool ValidateReferences()
    {
        if (sourceTilemap == null)
        {
            Debug.LogError("[NewTerrainDataExample] Source tilemap is not assigned");
            return false;
        }
        
        if (newTerrainData == null)
        {
            Debug.LogError("[NewTerrainDataExample] NewTerrainData ScriptableObject is not assigned");
            return false;
        }
        
        if (floodSimData == null)
        {
            Debug.LogError("[NewTerrainDataExample] FloodSimData is not assigned");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets existing NewTerrainLoader or creates a new one
    /// </summary>
    private NewTerrainLoader GetOrCreateNewTerrainLoader()
    {
        NewTerrainLoader loader = GetComponent<NewTerrainLoader>();
        
        if (loader == null)
        {
            loader = gameObject.AddComponent<NewTerrainLoader>();
            Debug.Log("[NewTerrainDataExample] Created new NewTerrainLoader component");
        }
        
        // Configure the loader
        loader.SetNewTerrainData(newTerrainData);
        loader.SetSourceTilemap(sourceTilemap);
        
        return loader;
    }
    
    /// <summary>
    /// Loads terrain data using the specified method
    /// </summary>
    private bool LoadTerrainData(NewTerrainLoader loader)
    {
        bool success;
        
        if (loadFromSpecificZLevel)
        {
            Debug.Log($"[NewTerrainDataExample] Loading terrain from Z-level {targetZLevel}");
            success = loader.LoadTerrainFromTilemapAtZ(sourceTilemap, targetZLevel, true, 0);
        }
        else
        {
            Debug.Log("[NewTerrainDataExample] Loading terrain from all Z-levels");
            success = loader.LoadTerrainFromTilemap(sourceTilemap);
        }
        
        if (success)
        {
            Debug.Log($"[NewTerrainDataExample] Loaded {newTerrainData.TotalTilesWritten} tiles");
            Debug.Log($"[NewTerrainDataExample] Elevation range: [{newTerrainData.MinElevation}, {newTerrainData.MaxElevation}]");
        }
        
        return success;
    }
    
    /// <summary>
    /// Connects the NewTerrainData to the flood simulation system
    /// </summary>
    private void ConnectToFloodSimulation()
    {
        floodSimData.NewTerrainDataSource = newTerrainData;
        
        // Clear old terrain data source to ensure new system takes priority
        floodSimData.TerrainDataSource = null;
        
        Debug.Log("[NewTerrainDataExample] Connected NewTerrainData to FloodSimData");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(floodSimData);
        #endif
    }
    
    /// <summary>
    /// Initializes or restarts the flood simulation
    /// </summary>
    private void InitializeSimulation()
    {
        if (floodSimulationManager == null)
        {
            floodSimulationManager = FindObjectOfType<FloodSimulationManager>();
        }
        
        if (floodSimulationManager != null)
        {
            floodSimulationManager.SimulationData = floodSimData;
            floodSimulationManager.ResetSimulation();
            Debug.Log("[NewTerrainDataExample] Initialized flood simulation with new terrain data");
        }
        else
        {
            Debug.LogWarning("[NewTerrainDataExample] No FloodSimulationManager found in scene");
        }
    }
    
    /// <summary>
    /// Creates a sample NewTerrainData asset (for testing purposes)
    /// </summary>
    [ContextMenu("Create Sample NewTerrainData Asset")]
    public void CreateSampleNewTerrainDataAsset()
    {
        #if UNITY_EDITOR
        NewTerrainData asset = ScriptableObject.CreateInstance<NewTerrainData>();
        
        string path = "Assets/NewTerrainData_Sample.asset";
        UnityEditor.AssetDatabase.CreateAsset(asset, path);
        UnityEditor.AssetDatabase.SaveAssets();
        
        Debug.Log($"[NewTerrainDataExample] Created sample NewTerrainData asset at {path}");
        
        // Assign it to this example
        newTerrainData = asset;
        #endif
    }
    
    /// <summary>
    /// Logs detailed information about the current terrain setup
    /// </summary>
    [ContextMenu("Log Terrain Setup Info")]
    public void LogTerrainSetupInfo()
    {
        Debug.Log("[NewTerrainDataExample] Current Setup:");
        Debug.Log($"  - Source Tilemap: {(sourceTilemap != null ? sourceTilemap.name : "None")}");
        Debug.Log($"  - NewTerrainData: {(newTerrainData != null ? newTerrainData.name : "None")}");
        Debug.Log($"  - FloodSimData: {(floodSimData != null ? floodSimData.name : "None")}");
        Debug.Log($"  - FloodSimulationManager: {(floodSimulationManager != null ? floodSimulationManager.name : "None")}");
        
        if (newTerrainData != null)
        {
            Debug.Log($"  - Terrain Data Loaded: {newTerrainData.DataLoaded}");
            Debug.Log($"  - Total Tiles: {newTerrainData.TotalTilesWritten}");
            Debug.Log($"  - Elevation Range: [{newTerrainData.MinElevation}, {newTerrainData.MaxElevation}]");
        }
        
        if (sourceTilemap != null)
        {
            var bounds = sourceTilemap.cellBounds;
            Debug.Log($"  - Tilemap Bounds: {bounds}");
            Debug.Log($"  - Tilemap Cell Count: {bounds.size.x * bounds.size.y * bounds.size.z}");
        }
    }
}
