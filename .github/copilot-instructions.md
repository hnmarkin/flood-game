# Unity Flood Simulation Game - Copilot Instructions

This project is a Unity-based 2D flood simulation game that uses physics-based water simulation with terrain elevation data. The game simulates water flow using the Shallow Water Equations with a custom terrain system based on tilemap z-values.

## Project Architecture

### Core Systems

#### 1. Z-Value Terrain System (Primary)
- **TerrainData.cs** - ScriptableObject storing tile positions (Vector3Int) and integer elevations
- **TerrainLoader.cs** - Component that reads z-coordinates from Unity Tilemaps to populate TerrainData
- **Pattern**: Tile z-coordinate = elevation value (e.g., z=0 is sea level, z=2 is high elevation)
- **Data Flow**: Tilemap → TerrainLoader → TerrainData → FloodSimulationManager

#### 2. Flood Simulation System
- **FloodSimulationManager.cs** - Core physics simulation using Shallow Water Equations
- **FloodSimData.cs** - ScriptableObject containing simulation parameters and terrain references
- **Pattern**: Event-driven architecture with OnSimulationStep and OnSimulationInitialized events
- **Physics**: Uses 2D grid arrays for water, terrain, flowX, and flowY data

#### 3. Rendering System
- **FloodTilemapRenderer.cs** - Renders water depth as different tile types
- **ElevationRenderer.cs** - Renders terrain elevation visually using 3D positioning
- **Pattern**: Subscribes to simulation events for automatic updates

### Key Architectural Patterns

#### ScriptableObject Data Pattern
```csharp
[CreateAssetMenu(fileName = "TerrainData", menuName = "Flood/Terrain Data")]
public class TerrainData : ScriptableObject
{
    [SerializeField] private List<Vector3Int> tilePositions;
    [SerializeField] private List<int> tileElevations;
    // Properties for external access
    public List<Vector3Int> TilePositions => tilePositions;
}
```

#### Custom Editor Integration
```csharp
[CustomEditor(typeof(TerrainLoader))]
public class TerrainLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        // Custom buttons and validation
        if (GUILayout.Button("Load Terrain from Tilemap"))
        {
            // Action logic
        }
    }
}
```

#### Event-Driven Simulation
```csharp
public class FloodSimulationManager : MonoBehaviour
{
    public event Action OnSimulationStep;
    public event Action OnSimulationInitialized;
    
    public void StepSimulation()
    {
        // Physics calculations
        OnSimulationStep?.Invoke();
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
        Scriptable Object Scripts/
        Editor/
        TileTypeSystem/ (legacy, avoid)
    Scenes/
```

### Naming Conventions
- **ScriptableObjects**: End with "Data" (TerrainData, FloodSimData)
- **Managers**: End with "Manager" (FloodSimulationManager)
- **Loaders**: End with "Loader" (TerrainLoader)
- **Renderers**: End with "Renderer" (FloodTilemapRenderer, ElevationRenderer)

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

// Custom ReadOnly attribute implementation
public class ReadOnlyAttribute : PropertyAttribute { }
```

### Performance Considerations
- Use `[NonSerialized]` for large runtime arrays to avoid serialization overhead
- Log progress every 100 tiles for large tilemap operations
- Implement boundary walls to prevent water from flowing off grid edges
- Use integer elevations instead of floats for memory efficiency

### Error Handling Patterns
```csharp
if (terrainData == null)
{
    Debug.LogWarning("[TerrainLoader] TerrainData is null, returning empty grid");
    return new float[gridWidth, gridHeight];
}
```

### Editor Integration Best Practices
- Always call `EditorUtility.SetDirty(target)` when modifying ScriptableObjects in custom editors
- Use `EditorGUI.BeginDisabledGroup()` to disable UI elements when requirements aren't met
- Provide helpful error messages and warnings in custom inspectors
- Include "Select" buttons to navigate between related objects

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

### Testing Patterns
- Use editor buttons for quick testing during development
- Implement ResetSimulation() methods for easy iteration
- Provide example usage scripts in documentation
- Include boundary condition tests for edge cases

## Important Notes

### Coordinate Systems
- **Tilemap Coordinates**: 3D with z representing elevation
- **Simulation Grid**: 2D array with +1 boundary padding
- **Conversion**: TerrainLoader handles coordinate space mapping

### Physics Implementation
- Uses explicit finite difference method for Shallow Water Equations
- Includes friction damping and flow scaling for stability
- Boundary cells are kept completely dry (water = 0)
- Flow arrays (flowX, flowY) represent velocity * depth

### Legacy System
- Avoid TileTypeSystem folder - contains old tile-type based terrain system
- New z-value system takes priority and is fully implemented
- Old system removed for cleaner architecture

This documentation should guide AI coding agents to understand the project's specific patterns, maintain consistency, and extend the system following established conventions.