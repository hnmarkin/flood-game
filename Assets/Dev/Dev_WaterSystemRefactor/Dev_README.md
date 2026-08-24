# Dev Water System Refactor

This is the development implementation of the new water-system data architecture.

## Direction

Legacy water assets are conversion inputs only. The retained runtime system does not provide hot-swappable compatibility with the old water model.

The runtime must not depend on:

- `TileMapData`
- `TileInstance`
- `TileType`
- `DynamicTile`
- `TileManager`

Those types may be referenced by the editor-only conversion tooling, but never by the retained controller, accessor, runtime state, simulation engine, or renderer.

## Data Flow

```text
Legacy TileMapData / TileType / DynamicTile
        |
        v
Dev_WaterLegacyMapConverter (Editor only)
        |
        +--> Dev_WaterMapData
        +--> Dev_WaterTerrainDefinition assets
        +--> Dev_WaterVisualDefinition assets
                    |
                    v
          Dev_WaterMapAccessor
                    |
          +---------+----------+
          |                    |
          v                    v
Dev_WaterRuntimeState   Dev_WaterSimulationEngine
          |                    |
          +---------+----------+
                    v
          Dev_WaterTilemapRenderer
```

The conversion step is one-way. After conversion, runtime systems load only the new assets.

## New Data Model

### `Dev_WaterMapData`

Persistent ScriptableObject containing map origin, dimensions, and `Dev_WaterMapCell` values. It contains static map information only; it does not contain live flow arrays or simulation history.

### `Dev_WaterMapCell`

Persistent per-cell data containing elevation, terrain definition, initial water depth, and initial water-source metadata. Coordinates are implicit in the cell's position in the map.

### `Dev_WaterTerrainDefinition`

Persistent logical terrain data used by simulation. It identifies terrain behavior, whether it participates in the simulation, whether it represents an initial water body, and which visual definition represents it.

### `Dev_WaterVisualDefinition`

Persistent visual data used by the renderer. It maps dry and water-depth states to standard Unity tile assets and tint values. It does not depend on `DynamicTile` or `TileManager`.

### `Dev_WaterMapAccessor`

The runtime data accessor. It provides bounds checks, coordinate conversion, cell lookup, terrain/elevation lookup, initial source lookup, and visual-definition lookup. It does not own mutable simulation state.

### `Dev_WaterRuntimeState`

Mutable state for one simulation run. It owns current water depth, flow, active-region state, dirty cells, and cached terrain values copied from the persistent map. It is recreated when the controller resets.

### `Dev_WaterScenarioConfig`

Scenario-level simulation inputs: settings, initial sources, and continuous sources. It does not duplicate map data and is not a fallback container for the controller.

## Runtime Responsibilities

`Dev_WaterController` is the public interface and orchestration layer. It creates the accessor, runtime state, engine, and runtime barrier grid, then controls initialization, stepping, pausing, resetting, and public water queries.

`Dev_WaterSimulationEngine` performs water calculations using only the new accessor, runtime state, settings, modifiers, sources, and barrier data. It has no tilemap or legacy-data responsibility.

`Dev_WaterTilemapRenderer` reads the new map visual definitions and runtime water depths, then writes directly to configured Unity tilemaps. It never writes simulation values back into persistent map data.

`Dev_WaterBarrierGrid` is runtime data owned by the controller/engine. It is not a serialized scene component or compatibility provider.

## Legacy Conversion

`Editor/Dev_WaterLegacyMapConverter.cs` is the only legacy bridge. It reads populated legacy map data and creates new map, terrain, visual, and standard Unity tile assets. It validates missing cells and terrain types and reports maps that contain no runtime `TileInstance` data.

The converter is not part of the runtime dependency graph. If a legacy map needs to be updated, run conversion again and review the generated assets.

The deterministic `RefactorScene` bootstrapper now writes `Dev_WaterMapData` directly for test-map generation. It does not populate or refresh legacy tile data.

## Basic Setup

1. Assign a `Dev_WaterMapData` asset to `Dev_WaterController`.
2. Assign a `Dev_WaterScenarioConfig` asset if the scenario needs non-default settings or sources.
3. Assign `Dev_WaterTilemapRenderer` and its target tilemaps.
4. Ensure every map terrain references a `Dev_WaterTerrainDefinition` and visual definition.
5. Call `Dev_WaterController.BeginSimulation()` from the new UI or gameplay interface.

## Migration Rule

Legacy types belong only in editor conversion code. Any retained runtime script that references `TileMapData`, `TileInstance`, `TileType`, `DynamicTile`, or `TileManager` is a migration failure and must be removed or moved into the conversion/tooling boundary.
