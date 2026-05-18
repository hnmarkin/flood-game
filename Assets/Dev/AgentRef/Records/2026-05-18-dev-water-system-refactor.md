# Dev Water System Refactor

- Added `Assets/Dev/Dev_WaterSystemRefactor`.
- Kept `Assets/Game` untouched.
- Split old monolith into:
  - `Dev_WaterController`: public facade / UI entry point.
  - `Dev_WaterSimulationEngine`: flow, sources, drainage, wind, spread gating.
  - `Dev_WaterRuntimeState`: runtime grid state.
  - `Dev_WaterTilemapRenderer`: TileMapData visual bridge.
  - Provider adapters: barrier and modifier hooks.
- Preserved `BeginSimulationFromUI()` plug-in path.
- Verified with normal build plus temp compile-check including new files.
