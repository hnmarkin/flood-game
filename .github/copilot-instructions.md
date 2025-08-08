# Unity Flood Simulation Game - Copilot Instructions

This project is a Unity-based 2D flood simulation game that uses physics-based water simulation with terrain elevation data. The game simulates water flow using the Shallow Water Equations with a custom z-value terrain ### System Integration Points

#### Terrain Data Loading
1. Create TerrainData ScriptableObject asset
2. Add TerrainLoader component to GameObject
3. Assign Tilemap and TerrainData references
4. Use custom inspector buttons to load data from tilemap z-levels
5. Connect TerrainData to FloodSimData asset

#### Simulation Initialization
1. FloodSimulationManager checks for TerrainDataSource in FloodSimData
2. Finds TerrainLoader in scene to convert data to simulation grid
3. Calculates coordinate offsets to map tilemap positions to 0-based simulation grid
4. Sets up boundary walls and initial water levels
5. Fires initialization events for renderers to subscribe

#### Rendering Pipeline
1. Renderers subscribe to simulation events in Start()
2. OnSimulationStep events trigger visual updates
3. Water depth determines tile selection for FloodTilemapRenderer
4. Terrain elevation determines Z-position for water tiles
5. Coordinate conversion maps simulation grid back to world coordinatescoordinates represent elevation.

## Project Architecture

### Core Systems

#### 1. Z-Value Terrain System (Primary)
- **TerrainData.cs** - ScriptableObject storing 3D tile positions (Vector3Int) and integer elevations extracted from z-coordinates
- **TerrainLoader.cs** - Component that reads z-coordinates from Unity Tilemaps to populate TerrainData
- **Pattern**: Tile z-coordinate = elevation value (e.g., z=0 is sea level, z=2 is high elevation)  
- **Data Flow**: Tilemap → TerrainLoader → TerrainData → FloodSimulationManager
- **Coordinate Mapping**: Automatic offset calculation to map tilemap coordinates to simulation grid (0-based)

#### 2. Flood Simulation System
- **FloodSimulationManager.cs** - Core physics simulation using Shallow Water Equations with boundary cell management
- **FloodSimData.cs** - ScriptableObject containing simulation parameters, terrain references, and runtime data arrays
- **Pattern**: Event-driven architecture with OnSimulationStep and OnSimulationInitialized events
- **Physics**: Uses 2D grid arrays for water, terrain, flowX, and flowY data with +1 boundary padding
- **Boundary Management**: Keeps boundary cells completely dry (water = 0) to prevent flow off edges

#### 3. Rendering System
- **FloodTilemapRenderer.cs** - Renders water depth as different tile types at correct z-elevations based on terrain
- **ElevationRenderer.cs** - Legacy isometric renderer using Y-positioning (TileTypeSystem folder)
- **Pattern**: Subscribes to simulation events for automatic updates
- **Z-Positioning**: Water tiles placed at terrain elevation + z_Booster offset for proper depth sorting

### Key Architectural Patterns

#### ScriptableObject Data Pattern
```csharp
[CreateAssetMenu(fileName = "TerrainData", menuName = "Flood/Terrain Data")]
public class TerrainData : ScriptableObject
{
    [SerializeField] private List<Vector3Int> tilePositions;
    [SerializeField] private List<int> tileElevations;
    
    // Properties for external access with data validation
    public List<Vector3Int> TilePositions => tilePositions;
    
    // Elevation lookup with fallback
    public int GetElevationAt(int x, int y)
    {
        for (int i = 0; i < tilePositions.Count; i++)
        {
            if (tilePositions[i].x == x && tilePositions[i].y == y)
                return tileElevations[i];
        }
        return 0; // Default elevation
    }
}
```

#### Custom Editor Integration with Reflection
```csharp
[CustomEditor(typeof(TerrainLoader))]
public class TerrainLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        // Reflection-based validation
        bool hasTerrainData = target.GetType().GetField("terrainData", 
            BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(target) != null;
            
        EditorGUI.BeginDisabledGroup(!hasTerrainData);
        if (GUILayout.Button("Load Terrain from Tilemap"))
        {
            ((TerrainLoader)target).LoadTerrainFromTilemap();
        }
        EditorGUI.EndDisabledGroup();
    }
}
```

#### Event-Driven Simulation with Coordinate Mapping
```csharp
public class FloodSimulationManager : MonoBehaviour
{
    public event Action OnSimulationStep;
    public event Action OnSimulationInitialized;
    
    public void Initialize()
    {
        // Automatic coordinate offset calculation
        int offsetX = 0, offsetY = 0;
        if (terrainData.TilePositions.Count > 0)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var pos in terrainData.TilePositions)
            {
                minX = Mathf.Min(minX, pos.x);
                minY = Mathf.Min(minY, pos.y);
            }
            offsetX = -minX; // Shift to start at 0
            offsetY = -minY;
        }
        
        // Boundary cell management
        for (int i = 0; i < gridWidth; i++)
        {
            simulationData.terrain[i, 0] = 1.0f; // Bottom wall
            simulationData.water[i, 0] = 0.0f;   // No water on boundary
        }
        
        OnSimulationInitialized?.Invoke();
    }
}
```

#### Z-Positioning Water Rendering
```csharp
public class FloodTilemapRenderer : MonoBehaviour
{
    private void UpdateTilemap()
    {
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float waterDepth = simulationData.water[x + 1, y + 1]; // Account for boundary
                
                if (waterDepth > 0.01f)
                {
                    int terrainElevation = GetTerrainElevationAt(x, y);
                    Vector2Int worldCoords = SimulationToWorldCoordinates(x, y);
                    
                    // Place water at terrain elevation + offset for proper depth sorting
                    Vector3Int tilePos = new Vector3Int(worldCoords.x, worldCoords.y, 
                                                       terrainElevation + z_Booster);
                    tilemap.SetTile(tilePos, GetWaterTile(waterDepth));
                }
            }
        }
    }
}
```

## Unity-Specific Conventions

### Package Dependencies
- **2D Tilemap System**: `com.unity.2d.tilemap` for terrain representation
- **2D Tilemap Extras**: `com.unity.2d.tilemap.extras` for advanced tilemap features
- **Universal Render Pipeline**: `com.unity.render-pipelines.universal` for 2D rendering
- **Input System**: `com.unity.inputsystem` for modern input handling

### Project Structure Conventions
```
Assets/
  _Project/
    Scripts/
      Water System/
        FloodSimulationManager.cs (Core simulation engine)
        FloodTilemapRenderer.cs (Primary z-based water renderer)
        TerrainLoader.cs (Tilemap to data converter)
        MapGeneration.cs (Procedural terrain generation)
        Scriptable Object Scripts/
          TerrainData.cs (Z-value elevation data storage)
          FloodSimData.cs (Simulation parameters and runtime arrays)
          TileHeightConfiguration.cs (Legacy configuration)
        Editor/
          TerrainLoaderEditor.cs (Reflection-based custom inspector)
          TerrainDataEditor.cs (TerrainData workflow management)
          FloodSimulationManagerEditor.cs (Runtime simulation controls)
          FloodSimDataNewEditor.cs (Data visualization and validation)
        TileTypeSystem/ (Legacy - AVOID)
          ElevationRenderer.cs (Y-positioning isometric renderer)
        [Experimental Components - May be removed]
          IsometricWaterRenderer.cs, IsometricTerrainRenderer.cs
          TilemapLayerManager.cs
    Scenes/
```

### Naming Conventions
- **ScriptableObjects**: End with "Data" (TerrainData, FloodSimData)
- **Managers**: End with "Manager" (FloodSimulationManager)
- **Loaders**: End with "Loader" (TerrainLoader)
- **Renderers**: End with "Renderer" (FloodTilemapRenderer, ElevationRenderer)
- **Editor Scripts**: Mirror target class name + "Editor" (TerrainLoaderEditor)

## Coding Guidelines

### Data Validation Patterns
```csharp
private void OnValidate()
{
    // Ensure positive values
    N = Mathf.Max(1, N);
    dx = Mathf.Max(0.1f, dx);
    friction = Mathf.Clamp01(friction);
}
```

### Custom Inspector Attributes
```csharp
[Header("Simulation Parameters")]
public int N = 10;

[Range(0f, 1f)]
public float startingWaterDepth = 0.1f;

[SerializeField, ReadOnly] private bool isInitialized = false;

// Custom ReadOnly attribute implementation in FloodSimData.cs
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(position, property, label);
        EditorGUI.EndDisabledGroup();
    }
}
#endif
```

### Performance Considerations
- Use `[NonSerialized]` for large runtime arrays to avoid serialization overhead
- Log progress every 100 tiles for large tilemap operations
- Implement boundary walls to prevent water from flowing off grid edges
- Use integer elevations instead of floats for memory efficiency
- Cache terrain elevation lookups during rendering

### Error Handling Patterns
```csharp
if (terrainData == null)
{
    Debug.LogWarning("[TerrainLoader] TerrainData is null, returning empty grid");
    return new float[gridWidth, gridHeight];
}

// Validation with detailed feedback
if (tilePositions.Count != tileElevations.Count)
{
    Debug.LogWarning($"[TerrainData] Data mismatch: {tilePositions.Count} positions but {tileElevations.Count} elevations");
    lastOperationResult = "Data validation failed: position/elevation count mismatch";
    return;
}
```

### Editor Integration Best Practices
```csharp
// Always call EditorUtility.SetDirty(target) when modifying ScriptableObjects in custom editors
if (GUI.changed)
{
    EditorUtility.SetDirty(terrainData);
}

// Use EditorGUI.BeginDisabledGroup() to disable UI elements when requirements aren't met
EditorGUI.BeginDisabledGroup(!hasTerrainData || !hasTilemap);
if (GUILayout.Button("Load Terrain from Tilemap"))
{
    // Action logic
}
EditorGUI.EndDisabledGroup();

// Provide helpful error messages and warnings in custom inspectors
if (!hasTerrainData)
{
    EditorGUILayout.HelpBox("No TerrainData assigned. Please assign a TerrainData ScriptableObject.", MessageType.Warning);
}

// Include "Select" buttons to navigate between related objects
if (GUILayout.Button("Select TerrainData"))
{
    Selection.activeObject = terrainData;
}
```

## System Integration Points

### Terrain Data Loading
1. Create TerrainData ScriptableObject asset
2. Add TerrainLoader component to GameObject
3. Assign Tilemap and TerrainData references
4. Use custom inspector buttons to load data
5. Connect TerrainData to FloodSimData asset

### Simulation Initialization
1. FloodSimulationManager checks for TerrainDataSource in FloodSimData
2. Finds TerrainLoader in scene to convert data to simulation grid
3. Sets up boundary walls and initial water levels
4. Fires initialization events for renderers to subscribe

### Rendering Pipeline
1. Renderers subscribe to simulation events in Start()
2. OnSimulationStep events trigger visual updates
3. Water depth determines tile selection for FloodTilemapRenderer
4. Elevation determines Y-position for ElevationRenderer

## Development Workflow

### Adding New Features
1. Create ScriptableObject for data if needed
2. Implement core logic in manager/component
3. Create custom editor for user-friendly interface
4. Add event integration for rendering updates
5. Update relevant documentation and examples

### Debugging Tips
- Use Debug.Log with component prefixes: `[FloodSimulationManager]`
- Check console for terrain loading progress messages
- Verify TerrainData.DataLoaded property in inspector
- Use custom editor validation buttons to check data integrity
- Examine bounds and coordinate mapping during terrain loading

### Testing Patterns
- Use editor buttons for quick testing during development
- Implement ResetSimulation() methods for easy iteration
- Provide example usage scripts in documentation
- Include boundary condition tests for edge cases

## Important Notes

### Coordinate Systems
- **Tilemap Coordinates**: 3D with z representing elevation
- **Simulation Grid**: 2D array with +1 boundary padding
- **Conversion**: TerrainLoader handles coordinate space mapping with automatic offset calculation

### Physics Implementation
- Uses explicit finite difference method for Shallow Water Equations
- Includes friction damping and flow scaling for stability
- Boundary cells are kept completely dry (water = 0)
- Flow arrays (flowX, flowY) represent velocity * depth

### Rendering Approach
- **Primary System**: FloodTilemapRenderer uses Z-positioning for water tiles
- Water tiles placed at terrain elevation + z_Booster offset
- Hardcoded water depth ranges: 0-0.05, 0.05-0.35, 0.35-0.65, 0.65-0.95, 0.95-1.0
- Coordinate conversion from simulation grid to world coordinates for proper tile placement

### Legacy System
- Avoid TileTypeSystem folder - contains old tile-type based terrain system
- New z-value system takes priority and is fully implemented
- ElevationRenderer provides Y-positioning isometric alternative (legacy)

This documentation should guide AI coding agents to understand the project's specific patterns, maintain consistency, and extend the system following established conventions.