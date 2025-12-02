# Water System

## Overview

The Water System takes a painted tilemap as input, loads terrain and tile type data into a simulation grid, runs Shallow Water Equation calculations to simulate water flow and depth, and drives visual updates through a dynamic tile renderer that selects sprites based on water level.

## Architecture

The architecture consists of two systems: Tilemap loading (runs during `Awake()`) and Simulation (runs during `Update()`).

### Tilemap Loading | `Awake()`

Map data is stored in TileMapData, a ScriptableObject, which itself uses a custom data type called TileInstance. These are the logical tiles. If a sprite in a TileInstance is changed, it automatically refreshes the actual corresponding grid tile (using `DynamicTile.cs` and `TileManager.cs`).

TileMapData logical data can be viewed in the Unity Editor using the custom window TileCellInspector (see the TileCellInspector `README`.

Different possible tile types are currently hard-coded into `MapLoader.cs`. Also, `population`, `econVal`, `damage`, and `casualties` are assigned arbitrary placeholder values.

*Note: Currently, TileMapData is reloaded every time you enter Play Mode!*

**Painted Tilemap → MapLoader → TileMapData (TileInstances)**

### Simulation | `Update()`

**TileMapData → WaterSimulator → TileManager**

---

## Key Components

---

## Data Flow

---

## Setup & Integration

---

## Extension Points

---
