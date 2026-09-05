# Dev Water Automated Test Report

Date: 2026-09-05  
Unity: `6000.0.63f1`  
Unity Test Framework: `1.6.0`

## Discovery

- EditMode assembly: 57 deterministic unit/scenario cases
- PlayMode assembly: 20 lifecycle/rendering cases
- Unit and scenario cases are no longer exposed as PlayMode tests.

## Results

- EditMode: 57 passed, 0 failed
- PlayMode: 18 passed, 2 failed

Failing cases:

1. `Renderer_Initialize_RendersEveryExistingCellWithResolvedTileAndTint`
2. `Renderer_DirtyCellUpdate_ChangesOnlyRequestedProjectionWithoutWritingSimulation`

Both failures select the expected tile but observe a white Tilemap cell instead of the resolved water-band tint. An isolated Unity probe reproduced the behavior: `Tilemap.SetColor` stored the requested tint, and the following `Tilemap.RefreshTile` reset it to white even when the test tile used `TileFlags.None`. This implicates the ordering in `Dev_WaterRenderer.Refresh`. Per the tests-only scope, runtime behavior was not changed and the failing tests remain as regression evidence.

The NUnit XML CLI commands are documented in `Dev_WaterTestRunbook.md`. They were not executed during this session because the project was open in the Unity Editor; Unity cannot open the same project concurrently in batch mode.
