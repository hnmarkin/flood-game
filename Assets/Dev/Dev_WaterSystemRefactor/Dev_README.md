# Dev Water System Refactor

Dev-only copycat of the current water simulation with cleaner boundaries between runtime state, simulation, rendering, and external game systems.

## Goals

- Keep the current plug-and-play workflow: assign `TileMapData`, assign tilemaps, call `BeginSimulationFromUI()`.
- Move simulation state out of `TileMapData` arrays and into `Dev_WaterRuntimeState`.
- Keep compatibility with current `TileInstance`, `TileType`, `DynamicTile`, and `TileManager` visuals.
- Provide clear hooks for scenario modifiers, continuous water sources, drainage, wind, and barriers.

## Basic Setup

1. Add `Dev_WaterController` to a scene object.
2. Assign the same `TileMapData` used by the existing map loader.
3. Add `Dev_WaterTilemapRenderer` and assign the visual tilemaps that should refresh.
4. Assign the same Water `TileType` if using water-body seeding.
5. Rebind the flood UI button to `Dev_WaterController.BeginSimulationFromUI()`.

## Optional Hooks

- Assign a barrier provider object to `barrierProviderBehaviour`. The adapter accepts either `Dev_IWaterBarrierProvider` or methods named like the current `IBarrierProvider`.
- Assign a modifier provider object to `modifierProviderBehaviour`. The adapter accepts `Dev_IWaterModifierProvider`, `GetWaterModifierSnapshot()`, or `GetModifierValue(string)`.
- Use `Dev_WaterScenarioConfig` to move settings and source definitions into a ScriptableObject.

## Notes

This package intentionally lives in `Assets/Dev` and does not modify `Assets/Game`. The renderer still writes water/tint/sprite values back to existing `TileInstance` objects because the current dynamic tile renderer reads from that model.
