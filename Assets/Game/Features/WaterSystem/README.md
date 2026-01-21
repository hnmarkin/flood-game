# Water System

## Overview

The Water System takes a painted tilemap as input, loads terrain and tile type data into a simulation grid, runs Shallow Water Equation calculations to simulate water flow and depth, and drives visual updates through a dynamic tile renderer that selects sprites based on water level.

## Architecture

The architecture consists of two systems: Tilemap loading (runs during `Awake()`) and Simulation (runs during `Update()`).

### Tilemap Loading | `Awake()`
---

Map data is stored in TileMapData, a ScriptableObject, which itself uses a custom data type called TileInstance. These are the logical tiles. If a sprite in a TileInstance is changed, it automatically refreshes the actual corresponding grid tile (using `DynamicTile.cs` and `TileManager.cs`).

TileMapData logical data can be viewed in the Unity Editor using the custom window TileCellInspector (see the TileCellInspector `README`).

Different possible tile types are currently hard-coded into `MapLoader.cs`. Also, `population`, `econVal`, `damage`, and `casualties` are assigned arbitrary placeholder values.

*Note: Currently, TileMapData is reloaded every time you enter Play Mode!*

**Painted Tilemap → MapLoader → TileMapData (TileInstances)**

### Simulation | `Update()`
---

The simulation is within WaterSimulator. There are four grids (2D arrays) involved, contained on the TileMapData ScriptableObject:
1. water
2. terrain
3. flowX
4. flowY

`water` and `terrain` are loaded from TileMapData. `flowX` and `flowY` contain the velocity of water on the edges between grid cells. Water flow is calculated based on height differences between water cells and adjacent cells, then added/subtracted from existing flow. Then the water is updated and `SetWater()` is called to update TileMapData.

There are two sets of settings--the water blanket settings and the Simulation Stepping Trigger. These control how much water is spawned in and where (for testing purposes) and whether the simulation steps manually or automatically.

A detailed explanation of the simulation can be found at [Simulating water over terrain | lisyarus blog](https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html#section-virtual-pipes-method).

Simulation Logic: **TileMapData → WaterSimulator → TileMapData**

Tile Updates: **TileMapData (TileInstances) → TileManager → Painted Tilemap (Tiles)**

---

## Components
`MapLoader.cs` - loads tilemap painted in Unity Editor into TileMapData.

`TileInstance.cs` - custom data type for tiles.

`TileManager.cs` - handles automatic refreshing of tiles to update sprites

`WaterSimulator.cs` - the physics-based water simulation

**Scriptable Objects**

`DynamicTile.cs` - modifies the built-in grid tile to makes the in-game Tilemap display the sprite in our logical grid (in TileMapData)

`TileMapData.cs`- holds the logical data (grid of TileInstances) and the water simulation settings

`TileType.cs` - the data structure for each terrain type, holding the sprites of each color.

---

## Setup & Integration

To have a working tile system, add these:

**In-Hierarchy:** Map Loader, Tile Manager, Water Simulator

**ScriptableObjects:** TileMapData, TileTypes (including Dynamic Tiles and Sprites)

Connecting them from here is self-explanatory.

---

## Extension Points

1. Map loading functionality needs to be connected to Ashley's data pipeline. This could be added to the MapLoader script or could be a new script entirely.
2. The visuals for tile z changes are dramatic. Either this must be fixed or the elevation should be scaled to 3 or 4 height levels (z=1:elev=1-5, z=2:elev=6-10, etc.)
3. Currently, sprites, Dynamic Tiles, and TileTypes do not exist for:
    1. Forest
    2. Mountain
    3. Infra
    4. City
    5. Water
    
    Also, the TileType references should probably be moved to a ScriptableObject and loaded from there.

---
