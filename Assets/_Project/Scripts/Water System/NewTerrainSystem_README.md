# New Terrain Data System - Z-Value Based Elevation

This document explains the new terrain data system that reads elevation directly from tile z-values instead of tile types.

## Overview

The new system consists of:
- **NewTerrainData** - ScriptableObject that stores tile positions and elevations (integers)
- **NewTerrainLoader** - Component that reads z-values from tilemaps and populates NewTerrainData
- **Updated FloodSimulationManager** - Now supports both old and new terrain data systems
- **Custom Editors** - User-friendly interfaces for managing the new system

## Key Differences from Old System

| Aspect | Old System (TerrainData) | New System (NewTerrainData) |
|--------|-------------------------|----------------------------|
| Elevation Source | Tile type determines height | Tile z-coordinate determines elevation |
| Data Structure | List of terrain types with heights | Direct position→elevation mapping |
| Flexibility | Limited to predefined tile types | Any z-value can represent elevation |
| Precision | Float heights per type | Integer elevations per tile |

## Setup Instructions

### 1. Create NewTerrainData Asset
1. Right-click in Project window
2. Go to Create → Flood → New Terrain Data
3. Name your asset (e.g., "MyNewTerrainData")

### 2. Set Up NewTerrainLoader
1. Add `NewTerrainLoader` component to a GameObject
2. Assign your NewTerrainData asset to the "New Terrain Data" field
3. Assign your tilemap to the "Source Tilemap" field
4. Click "Load Terrain from Tilemap" in the inspector

### 3. Connect to FloodSimulationManager
1. Open your FloodSimData asset
2. Assign your NewTerrainData to the "New Terrain Data Source" field
3. The FloodSimulationManager will automatically use the new system

## Using the System

### Loading All Z-Levels
```csharp
NewTerrainLoader loader = GetComponent<NewTerrainLoader>();
loader.LoadTerrainFromTilemap(myTilemap);
```

### Loading Specific Z-Level
```csharp
NewTerrainLoader loader = GetComponent<NewTerrainLoader>();
// Load tiles at z=2, using z-value as elevation
loader.LoadTerrainFromTilemapAtZ(myTilemap, 2, true);
```

### Converting to Simulation Grid
```csharp
float[,] heightGrid = newTerrainData.ConvertToHeightArray(
    gridWidth: 50, 
    gridHeight: 50, 
    offsetX: 0, 
    offsetY: 0, 
    elevationScale: 1.0f
);
```

## Priority System

When both terrain data sources are assigned to FloodSimData:
1. **NewTerrainData** (z-value based) takes priority
2. **TerrainData** (tile type based) is used as fallback
3. Empty terrain is used if neither is available

## Data Structure

### NewTerrainData Properties
- `TilePositions` - List of Vector3Int positions
- `TileElevations` - List of integer elevations
- `MinElevation` / `MaxElevation` - Elevation range
- `DataLoaded` - Whether data has been loaded
- `TotalTilesWritten` - Number of tiles stored

### Key Methods
- `AddTile(Vector3Int position, int elevation)` - Add a tile
- `GetElevationAt(int x, int y)` - Get elevation at position
- `ConvertToHeightArray(...)` - Convert to simulation grid
- `ClearData()` - Clear all stored data
- `ValidateData()` - Validate and update status

## Editor Features

### NewTerrainData Inspector
- Shows terrain information (tiles, elevation range, bounds)
- Validate/Clear data buttons
- Lists associated loaders in scene
- Status information and warnings

### NewTerrainLoader Inspector
- Load terrain from tilemap buttons
- Z-level specific loading options
- Tilemap information display
- Easy access to terrain info logging

### FloodSimData Inspector
- Shows both old and new terrain source status
- Indicates which system will be used
- Terrain data statistics

## Example Usage

```csharp
public class TerrainSetupExample : MonoBehaviour
{
    [SerializeField] private Tilemap sourceTilemap;
    [SerializeField] private NewTerrainData newTerrainData;
    [SerializeField] private FloodSimData floodSimData;
    
    void Start()
    {
        // Set up loader
        NewTerrainLoader loader = gameObject.AddComponent<NewTerrainLoader>();
        loader.SetNewTerrainData(newTerrainData);
        loader.SetSourceTilemap(sourceTilemap);
        
        // Load terrain data
        if (loader.LoadTerrainFromTilemap())
        {
            // Connect to simulation
            floodSimData.NewTerrainDataSource = newTerrainData;
            
            // Initialize simulation
            FindObjectOfType<FloodSimulationManager>().ResetSimulation();
        }
    }
}
```

## Tilemap Requirements

For the new system to work properly:
1. Your tilemap should have tiles placed at different z-levels
2. Each z-level represents a different elevation
3. Higher z-values = higher elevation
4. Example: z=0 (sea level), z=1 (hills), z=2 (mountains)

## Migration from Old System

1. Keep your existing TerrainData assets as backup
2. Create new NewTerrainData assets for new functionality
3. Both systems can coexist - NewTerrainData takes priority
4. Update your tilemap to use z-values for elevation
5. Test the new system before removing old terrain data

## Troubleshooting

### No Tiles Loaded
- Check that your tilemap has tiles at various z-levels
- Verify tilemap is assigned to NewTerrainLoader
- Check console for detailed error messages

### Simulation Not Using New Data
- Ensure NewTerrainData is assigned to FloodSimData
- Check that NewTerrainData.DataLoaded is true
- Verify FloodSimulationManager is finding NewTerrainLoader in scene

### Elevation Values Wrong
- Check z-values in your tilemap
- Adjust elevationScale in NewTerrainLoader if needed
- Use ConvertToHeightArray with appropriate scaling

## Performance Notes

- Integer elevations are more memory efficient than float heights
- Direct position→elevation mapping is faster than tile type lookup
- Large tilemaps may take time to process - progress is logged every 100 tiles
- Consider loading specific z-levels for better performance with large tilemaps
