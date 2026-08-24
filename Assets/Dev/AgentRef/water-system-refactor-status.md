# Refactored Water System: Remaining Work

The `RefactorScene` prototype now initializes, renders, and advances water through the new runtime boundary. It is not yet a replacement for the Legacy system: the playable scene still uses `WaterSimulator`, and several integrations depend on Game State, Modifiers, Scenario Data, and the New Input System. Unit testing is separate work and is not included below.

## At a Glance

| Work item | Parity or refactor change | Legacy system | Refactor status and remaining dependency |
|---|---|---|---|
| Production integration | Parity | `PlayableTilemap` is wired to `WaterSimulator`. | Wire the playable/scenario path and UI to `Dev_WaterController`, then retire the Legacy runtime path. Depends on Scenario and Content Data. |
| Lifecycle and stepping | Parity, with architectural integration | `WaterSimulator` owns `Start`, `Update`, automatic stepping, and Space-key stepping. | The controller has begin, pause, resume, reset, and step operations. Connect them to Game Flow/Game Phase events; temporary Space-key input remains until the New Input System exists. |
| Map loading and runtime data | Parity, with decoupling | `MapLoader`, `TileMapData`, and `TileInstance` provide map data, but simulation and visual state are coupled to them. | Conversion and new map assets exist. Finish the production scenario/data pipeline and keep Legacy types editor-only. |
| Initial water sources | Parity for core blanket modes | Supports full, edge, corner, and existing-water-body blankets through an assigned `waterTileType`. | Equivalent source kinds and scenario configuration exist. Validate/tune the production source data; `isWater` remains authoritative. |
| Simulation behavior | Parity validation | Shallow-water flow and spread logic exist inside `WaterSimulator`. | Flow runs in `Dev_WaterSimulationEngine`. Compare intended behavior, boundaries, and tuning before integration; defer stress testing until the frame is complete. |
| Barriers | Parity, with decoupling | Uses `IBarrierProvider` through an arbitrary serialized `MonoBehaviour`. | `Dev_WaterBarrierGrid` is the concrete runtime barrier store. Connect real defense placement and verify blocked-flow/seepage behavior when those systems are ready. |
| Rendering | Parity, with decoupling | `TileMapData`, `TileManager`, and `DynamicTile` update visuals from the Legacy data model. | `Dev_WaterTilemapRenderer` reads runtime state and new visual definitions. Finish production visual definitions and integration. |
| Test-map bootstrap | Development support | Legacy loads a painted map through `MapLoader`. | The hard-coded `RefactorScene` generator is sufficient for prototype testing and is now working. It is not a production map-loading solution. |
| Authoritative runtime state | Refactor-only decoupling | Water is split between `TileMapData`, simulation arrays, tile instances, and visual arrays. | `Dev_WaterRuntimeState` is authoritative and private behind the controller. Preserve this boundary during integration. |
| Visual persistence/interpolation | Refactor-only correction | `persistentFloodVisuals` retains a visual maximum and interpolation can diverge from simulation truth. | Removed. Rendering should mirror current runtime state directly. Historical peak rendering is a separate future feature. |
| Modifier-driven water behavior | Refactor addition planned by architecture | No modifier controller or water modifier integration exists. | The engine accepts a modifier snapshot but currently receives defaults. Connect it to `ModifierController` after the modifier system and scenario initialization exist. |
| Peak-water/scoring history | Novel future feature | No proper historical water state exists; Legacy persistence is only a visual workaround. | Add later as a separate recorded state/snapshot that the scoring screen can deliberately display. It must not become renderer persistence. Depends on scoring/result design. |
| Architecture documentation | Documentation prerequisite | The Legacy README describes the monolithic system. | Complete the `World Simulation: Tile Map & Water System` section in `architecture.md` after the runtime contract is settled. |

## Parity Work: Replacing the Legacy System

### Production integration and lifecycle

The Legacy system already provides the basic gameplay-facing behavior: initialize, begin, step automatically or manually, and update the tilemap. The refactor has the corresponding controller operations, but they currently work in `RefactorScene` rather than through the actual playable scenario.

The next integration step is to make scenario initialization create/configure the refactored controller and have the UI call `BeginSimulation()` through that controller. Game Flow should then determine whether the simulation is active during Gameplay, Pause, and Loading, while Game Phase should determine when crisis-time simulation begins. Those FSMs do not yet exist, so the controller’s local lifecycle controls remain the temporary bridge.

### Map data, sources, and simulation behavior

Legacy map loading uses `MapLoader` to populate `TileMapData` with `TileInstance` objects. `WaterSimulator` then reads and writes the same data while `TileManager` refreshes the display. The refactor’s converter, `Dev_WaterMapData`, accessor, runtime state, and renderer replace that path; the remaining work is connecting the new assets to the eventual Scenario and Content Data pipeline and validating the conversion of production maps.

The core blanket behavior has parity coverage through `FullMap`, `Edges`, `Corners`, and `ExistingWaterBodies`. Unlike Legacy’s explicitly assigned `waterTileType`, the refactor uses the terrain definition’s `isWater` value for existing bodies. Rainfall, boundary sources, and modifier scaling are represented by the new configuration, but are not required to replace the Legacy prototype until the corresponding scenario/modifier design is ready.

The engine now owns the flow calculation and spread gating that were previously mixed into `WaterSimulator`. Remaining parity work is behavioral comparison and tuning: confirm source initialization, flow across map edges and barriers, drainage defaults, and the intended spread timing. Stress testing can follow once this frame is stable.

### Barriers and rendering

Legacy already has a barrier hook through `IBarrierProvider`, but it accepts any `MonoBehaviour` and casts it at runtime. The refactor deliberately removes that swappable compatibility layer and uses `Dev_WaterBarrierGrid` as the final runtime representation. The remaining work is feeding that grid from the eventual defense-placement system and checking the intended blocked-flow and seepage rules.

Legacy rendering is coupled to `TileMapData` and includes separate visual water arrays. The refactor renderer reads `Dev_WaterRuntimeState` and writes only to the configured tilemaps, so the remaining parity work is production tile/visual setup rather than another state path.

## Refactor-Only Changes and Dependencies

These changes are not required because the Legacy system lacks a user-visible feature; they are required so the replacement fits the planned architecture.

- **Authoritative state and separation of concerns:** `Dev_WaterRuntimeState`, `Dev_WaterSimulationEngine`, `Dev_WaterController`, and `Dev_WaterTilemapRenderer` separate mutable simulation state, calculation, public access, and display. Do not reintroduce Legacy runtime references or public state access.
- **Modifier integration:** the water engine’s modifier snapshot is currently populated with defaults. Real values must come through the planned `ModifierController`/resolver system and be initialized from scenario data. This depends on `ModifierInitializer` and `ScenarioBootstrapper`.
- **Game State integration:** water should subscribe to the planned Game Flow/Game Phase events rather than inventing a second phase system. This depends on the FSM/event contracts in `architecture.md`.
- **New Input System integration:** the current Space-key path is explicitly Dev-only test input. Replace it only when the project’s New Input System and Tool FSM define the step/action contract; Legacy’s input path must not be carried forward as production input.
- **Peak-state display:** if scoring needs worst-case water, record a separate historical snapshot and let scoring deliberately select it for display. Do not restore `persistentFloodVisuals` or visual interpolation. Save/Load integration can wait until its data contract exists.

The minimal replacement target is therefore: production map/scenario integration, parity validation of sources/flow/barriers/rendering, and Game State hookup when those core systems are available. The modifier, input, scoring-history, and save/load additions should follow their architectural dependencies rather than expanding the water prototype prematurely.
