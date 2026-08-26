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

Those types may be referenced by editor-only conversion tooling, but never by the retained controller, accessor, water state, physics, or renderer.

## Data Flow

```text
Legacy TileMapData / TileType / DynamicTile
        |
        v
Dev_WaterLegacyMapConverter (Editor only)
        |
        +--> Dev_MapDef
        +--> Dev_TerrainTypeDef assets
        +--> Dev_RendererDef assets
                    |
                    v
              Dev_MapAccessor
                    |
          +---------+----------+
          |                    |
          v                    v
     Dev_WaterState      Dev_WaterPhysics
          |                    |
          +---------+----------+
                    v
           Dev_WaterRenderer
```

The conversion step is one-way. After conversion, runtime systems load only the new assets.

## New Data Model

### `Dev_MapDef`

Authored map layout and starting conditions. It owns the map bounds and its `Dev_MapCellDef` grid, including where initial water bodies exist; it never holds live water or flow history.

### `Dev_MapCellDef`

Authored data for one map location: whether it exists, elevation, terrain type, starting water depth, and initial-water-body status. Its coordinate is its position in `Dev_MapDef`.

### `Dev_TerrainTypeDef`

Reusable terrain behavior shared by map cells. It defines whether terrain participates in water physics, its drainage multiplier, and the `Dev_RendererDef` used to display it; it does not define map layout or starting water bodies.

### `Dev_RendererDef`

Renderer data used to map dry and water-depth states to standard Unity tiles and tint values. It has no legacy tile-management dependency.

### `Dev_MapAccessor`

The read-only runtime map interface. It provides bounds checks, coordinate conversion, cell and elevation lookup, initial-water-body lookup, and renderer-definition lookup; it does not own mutable water state.

### `Dev_WaterState`

Mutable state for one simulation run. It owns current water depth, flow, active-region state, dirty cells, and cached terrain values copied from the persistent map. It is recreated when the controller resets.

### `Dev_ScenarioDef`

Scenario-owned simulation inputs: storm profiles, initial sources, continuous sources, and optional preliminary flooding. It does not duplicate map layout.

## Runtime Responsibilities

`Dev_WaterController` is the public interface and orchestration layer. It creates the accessor, water state, physics, and runtime barrier store, then controls initialization, stepping, pausing, resetting, and public water queries.

`Dev_WaterPhysics` performs water calculations using only the new accessor, water state, settings, modifiers, sources, and barrier data. It has no tilemap or legacy-data responsibility.

`Dev_WaterRenderer` reads renderer definitions and water depths, then writes directly to configured Unity tilemaps. It never writes simulation values back into persistent map data.

`Dev_WaterPhysicsBarrier` is runtime data owned by the controller/physics. It is not a serialized scene component or compatibility provider.

## Legacy Conversion

`Editor/Dev_WaterLegacyMapConverter.cs` is the only legacy bridge. It reads populated legacy map data and creates new map, terrain-type, renderer, and standard Unity tile assets. It validates missing cells and terrain types and reports maps that contain no runtime `TileInstance` data.

The converter is not part of the runtime dependency graph. If a legacy map needs to be updated, run conversion again and review the generated assets.

The deterministic `RefactorScene` bootstrapper now writes `Dev_MapDef` directly for test-map generation. It does not populate or refresh legacy tile data.

## Basic Setup

1. Assign a `Dev_MapDef` asset to `Dev_WaterController`.
2. Assign a `Dev_ScenarioDef` asset if the scenario needs non-default profiles or sources.
3. Assign `Dev_WaterRenderer` and its target tilemaps.
4. Ensure every `Dev_MapCellDef` references a `Dev_TerrainTypeDef`, which references a `Dev_RendererDef`.
5. Call `Dev_WaterController.BeginSimulation()` from the new UI or gameplay interface.

## Migration Rule

Legacy types belong only in editor conversion code. Any retained runtime script that references `TileMapData`, `TileInstance`, `TileType`, `DynamicTile`, or `TileManager` is a migration failure and must be removed or moved into the conversion/tooling seam.
