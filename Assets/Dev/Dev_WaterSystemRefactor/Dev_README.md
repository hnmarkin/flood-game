# Dev Water System Refactor

Dev-only prototype of the current water simulation with cleaner boundaries between runtime state, simulation, rendering, and barrier data.

## Goals

- Keep the current plug-and-play workflow: assign `TileMapData`, assign tilemaps, call `BeginSimulationFromUI()`.
- Move simulation state out of `TileMapData` arrays and into `Dev_WaterRuntimeState`.
- Keep compatibility with current `TileInstance`, `TileType`, `DynamicTile`, and `TileManager` visuals.
- Keep `Dev_WaterRuntimeState` authoritative while the renderer directly mirrors its current water depths.
- Provide a concrete barrier grid for future barrier tools, plus scenario settings, continuous sources, drainage, and wind.

## Basic Setup

1. Add `Dev_WaterController` to a scene object.
2. Assign the same `TileMapData` used by the existing map loader.
3. Add `Dev_WaterTilemapRenderer` and assign the visual tilemaps that should refresh.
4. Add `Dev_WaterBarrierGrid` to the same object and assign it to the controller.
5. Rebind the flood UI button to `Dev_WaterController.BeginSimulationFromUI()`.

## Refactor Scene Test Map

`RefactorScene` uses `Dev_WaterRefactorSceneBootstrapper` for its deterministic 20×20 test map. In edit mode, use the component's **Rebuild Scene** context-menu action before a fresh test run; it clears the prior generated layout and recreates it. `ExistingWaterBodies` currently seeds every imported `TileType` marked `isWater`; selective source categories are intentionally deferred.

## Configuration

Use `Dev_WaterScenarioConfig` to move settings and source definitions into a ScriptableObject. Until a real modifier controller exists, the refactor runs with default modifier values. Manual Space-bar stepping is Dev-only test input.

## Notes

This package intentionally lives in `Assets/Dev` and does not modify `Assets/Game`. The renderer writes water/tint/sprite values back to existing `TileInstance` objects because the current dynamic tile renderer reads from that model; those values are always set from the current runtime depth.
