# Terrain Data System - Z-Value Based Elevation

This document explains the terrain data system that reads elevation directly from tile z-values.

## Overview

The system consists of:
- **TerrainData** - ScriptableObject that stores tile positions and elevations (integers)
- **TerrainLoader** - Component that reads z-values from tilemaps and populates TerrainData
- **FloodSimulationManager** - Uses TerrainData for flood simulation
- **Custom Editors** - User-friendly interfaces for managing the system

## Key Features

| Aspect | Implementation |
|--------|----------------|
| Elevation Source | Tile z-coordinate determines elevation |
| Data Structure | Direct position→elevation mapping |
| Flexibility | Any z-value can represent elevation |
| Precision | Integer elevations per tile |

## Setup Instructions

### 1. Create TerrainData Asset
1. Right-click in Project window
2. Go to Create → Flood → Terrain Data
3. Name your asset (e.g., "MyTerrainData")

### 2. Set Up TerrainLoader
1. Add `TerrainLoader` component to a GameObject
2. Assign your TerrainData asset to the "Terrain Data" field
3. Assign your tilemap to the "Source Tilemap" field
4. Click "Load Terrain from Tilemap" in the inspector

### 3. Connect to FloodSimulationManager
1. Open your FloodSimData asset
2. Assign your TerrainData to the "Terrain Data Source" field
3. The FloodSimulationManager will automatically use the system

## Using the System

### Loading All Z-Levels
```csharp
TerrainLoader loader = GetComponent<TerrainLoader>();
loader.LoadTerrainFromTilemap(myTilemap);
```

### Loading Specific Z-Level
```csharp
TerrainLoader loader = GetComponent<TerrainLoader>();
// Load tiles at z=2, using z-value as elevation
loader.LoadTerrainFromTilemapAtZ(myTilemap, 2, true);
```

### Converting to Simulation Grid
```csharp
float[,] heightGrid = terrainData.ConvertToHeightArray(
    gridWidth: 50, 
    gridHeight: 50, 
    offsetX: 0, 
    offsetY: 0, 
    elevationScale: 1.0f
);
```

## Data Structure

### TerrainData Properties
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

### TerrainData Inspector
- Shows terrain information (tiles, elevation range, bounds)
- Validate/Clear data buttons
- Lists associated loaders in scene
- Status information and warnings

### TerrainLoader Inspector
- Load terrain from tilemap buttons
- Z-level specific loading options
- Tilemap information display
- Easy access to terrain info logging

### FloodSimData Inspector
- Shows terrain source status
- Terrain data statistics

## Example Usage

```csharp
public class TerrainSetupExample : MonoBehaviour
{
    [SerializeField] private Tilemap sourceTilemap;
    [SerializeField] private TerrainData terrainData;
    [SerializeField] private FloodSimData floodSimData;
    
    void Start()
    {
        // Set up loader
        TerrainLoader loader = gameObject.AddComponent<TerrainLoader>();
        loader.SetTerrainData(terrainData);
        loader.SetSourceTilemap(sourceTilemap);
        
        // Load terrain data
        if (loader.LoadTerrainFromTilemap())
        {
            // Connect to simulation
            floodSimData.TerrainDataSource = terrainData;
            
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

### Simulation Not Using Data
- Ensure TerrainData is assigned to FloodSimData
- Check that TerrainData.DataLoaded is true
- Verify FloodSimulationManager is finding TerrainLoader in scene

### Elevation Values Wrong
- Check z-values in your tilemap
- Adjust elevationScale in NewTerrainLoader if needed
- Use ConvertToHeightArray with appropriate scaling

## Performance Notes

- Integer elevations are more memory efficient than float heights
- Direct position→elevation mapping is faster than tile type lookup
- Large tilemaps may take time to process - progress is logged every 100 tiles
- Consider loading specific z-levels for better performance with large tilemaps
