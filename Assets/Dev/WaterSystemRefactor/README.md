# Dev Water System Refactor

This folder contains the development implementation of the refactored water simulation, its one-way legacy converter, and its automated tests. The runtime assembly is `WaterSystemRefactor.Runtime`.

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
WaterLegacyMapConverter (Editor only)
        |
        +--> MapDef
        +--> TerrainTypeDef assets
        +--> RendererDef assets
        +--> standard Unity Tile assets

MapDef + ScenarioDef + IWaterModifierProvider
                         |
                         v
Game State --> WaterLifecycleCoordinator --> WaterController
                                                   |
                           +-----------------------+--------------------+
                           |                       |                    |
                           v                       v                    v
                    MapAccessor       WaterState       WaterPhysics
                                                                  + barriers
                                                   |
                                                   v
                                          WaterRenderer --> Tilemaps

ProjectionController --> WaterController --> cloned simulation
                                                     |
                                                     v
                                          immutable WaterProjection
```

The conversion step is one-way. After conversion, runtime systems load only the new assets. Gameplay systems must use `WaterController`, `WaterLifecycleCoordinator`, or `ProjectionController`; they must not reach into the accessor, mutable state, physics engine, or barrier grid.

## New Data Model

### `MapDef`

Authored map layout and starting conditions. It owns the map bounds and its `MapCellDef` grid, including where initial water bodies exist; it never holds live water or flow history.

### `MapCellDef`

Authored data for one map location: whether it exists, elevation, terrain type, starting water depth, and initial-water-body status. Its coordinate is its position in `MapDef`.

### `TerrainTypeDef`

Reusable terrain behavior shared by map cells. It defines whether terrain participates in water physics, its drainage multiplier, and the `RendererDef` used to display it; it does not define map layout or starting water bodies.

### `RendererDef`

Renderer data used to map dry and water-depth states to standard Unity tiles and tint values. It has no legacy tile-management dependency.

### `MapAccessor`

The read-only runtime map interface. It provides bounds checks, coordinate conversion, cell and elevation lookup, initial-water-body lookup, and renderer-definition lookup; it does not own mutable water state.

### `WaterState`

Mutable state for one simulation run. It owns current water depth, flow, active-region state, dirty cells, and cached terrain values copied from the persistent map. It is recreated when the controller resets.

### `ScenarioDef`

Scenario-owned simulation inputs: storm profiles, initial sources, continuous sources, and optional preliminary flooding. It does not duplicate map layout.

Each scenario contains Baseline, Preliminary, and Crisis storm profiles. Each profile owns cloned simulation settings and continuous sources and defines the four map edges independently through `WaterBoundarySettings`. A `Wall` blocks the edge and may remove a configured seepage depth per simulated second, a `Source` injects its configured depth per simulated second, and a `Sink` removes water reaching that edge. This models the limitless world beyond the authored map: use a source for an upstream river edge, a sink for an ocean edge, and a wall for land barriers. Source and sink edges are applied to logical edge cells; corners are processed once.

## Runtime Responsibilities

`WaterController` is the public interface and orchestration layer. It creates the accessor, water state, physics engine, and runtime barrier store, then controls initialization, profile changes, stepping, pausing, resetting, terrain/water/barrier changes, public water queries, and immutable projection creation. It raises C# events only after successful changes.

`WaterPhysics` performs water calculations using only the new accessor, water state, settings, modifiers, sources, and barrier data. It has no tilemap or legacy-data responsibility.

`WaterRenderer` receives runtime state and the map accessor from the controller, resolves tiles and tints, and updates only dirty cells on its configured Unity tilemaps. It never writes simulation values back into the map asset or runtime state.

`WaterPhysicsBarrier` is runtime data owned by the controller/physics. It is not a serialized scene component or compatibility provider.

`WaterLifecycleCoordinator` is the Game State integration seam. It forwards loading, flow, and phase notifications to the controller and applies configured preliminary flooding once when the completed-preparation-turn threshold is reached. Game State must commit completed actions, terrain changes, modifier changes, and barriers before sending that turn notification. A failed preliminary run is transactional and may be retried; `NotifyNewRun()` clears the one-run marker and resets water.

`ProjectionController` owns forecast replacement. It asks the water controller to simulate a cloned snapshot for a configured duration, exposes the immutable `WaterProjection`, and can coalesce several Game State changes into one forecast event with its transaction methods. `CalculateHazards()` remains an intentional no-op until hazard classification and overlay design are defined.

`IWaterModifierProvider` is required in Production mode. Development mode may use sanitized defaults with warnings; Production rejects missing or invalid maps, scenarios, profiles, and modifier values.

## Legacy Conversion

`Editor/WaterLegacyMapConverter.cs` is the only legacy bridge. It reads populated legacy map data and creates new map, terrain-type, renderer, and standard Unity tile assets. It validates missing cells and terrain types and reports maps that contain no runtime `TileInstance` data.

The converter is not part of the runtime dependency graph. If a legacy map needs to be updated, run conversion again and review the generated assets.

The deterministic `RefactorScene` bootstrapper now writes `MapDef` directly for test-map generation. It does not populate or refresh legacy tile data.

## Basic Setup

1. Create a `MapDef`, configure its origin and dimensions, and populate every grid position with an existing `MapCellDef`; Production validation currently rejects sparse maps. Every cell needs a valid `TerrainTypeDef`. Assign a `RendererDef` to each terrain that needs authored tile and tint selection; otherwise the renderer uses its fallback tint and does not replace the tile.
2. Create a `ScenarioDef`. Production requires valid Baseline, Preliminary, and Crisis profiles; configure initial sources, continuous sources, and optional preliminary flooding as needed.
3. Configure North, East, South, and West as `Wall`, `Source`, or `Sink` in every profile. Source edges require a positive rate; wall height padding is used only for wall edges.
4. Add `WaterController`, assign the map and scenario, select the configuration mode, and assign a component implementing `IWaterModifierProvider`. The provider is mandatory in Production mode.
5. Optionally add `WaterRenderer`, configure its target tilemaps, and assign it to the water controller. A controller without a renderer can still simulate.
6. Add `WaterLifecycleCoordinator`, assign the controller, and have Game State call its notification methods. In Production, simulation runs only while Game Flow is `Gameplay` and Game Phase is `Crisis`; `BeginSimulation()` is the direct development/manual entry point.
7. Optionally add `ProjectionController` for forecasts. Assign the same water controller and notify it after game-time, completed-defense, or water-affecting modifier changes.

The controller supports automatic interval stepping and explicit manual stepping. The Space-key path is a development-only input until the project input integration exists.

## Automated Tests

Tests are isolated under `AutomatedTesting` and reference the runtime assembly without adding test-only APIs to gameplay code.

- `WaterSystemRefactor.EditModeTests` is Editor-only and contains deterministic `WaterUnit` and `WaterScenario` coverage for map/state behavior, configuration, physics, boundaries, sources, projections, and hand-calculable scenarios.
- `WaterSystemRefactor.PlayModeTests` contains only `WaterLifecycle` and `WaterRendering` integration coverage that requires Unity components, lifecycle, frames, or Tilemaps.
- Reusable fixture construction and any serialized-field reflection stay inside the test assemblies. Test actions and assertions use public runtime seams.

Latest verified result on 2026-09-05 with Unity `6000.0.63f1` and Unity Test Framework `1.6.0`:

- EditMode: **57 passed, 0 failed**
- PlayMode: **20 passed, 0 failed**
- Total: **77 passed, 0 failed**

The PlayMode suite caught and verified the fix for Tilemap tint loss caused by applying color before `RefreshTile`. See [`AutomatedTesting/WaterTestRunbook.md`](AutomatedTesting/WaterTestRunbook.md) for Test Runner categories and CLI/NUnit XML commands, and [`AutomatedTesting/WaterTestReport.md`](AutomatedTesting/WaterTestReport.md) for the recorded run and regression details. The recorded success is from Unity Test Runner assembly runs; the documented batch-mode XML commands were not run in that session because the project was already open in the Editor.

## Migration Rule

Legacy types belong only in editor conversion code. Any retained runtime script that references `TileMapData`, `TileInstance`, `TileType`, `DynamicTile`, or `TileManager` is a migration failure and must be removed or moved into the conversion/tooling seam.
