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
- PlayMode: 20 passed, 0 failed

## Resolved renderer regression

The initial PlayMode run passed 18/20. These cases caught the defect:

1. `Renderer_Initialize_RendersEveryExistingCellWithResolvedTileAndTint`
2. `Renderer_DirtyCellUpdate_ChangesOnlyRequestedProjectionWithoutWritingSimulation`

An isolated Unity probe showed that `Tilemap.SetColor` stored the requested tint and the following `Tilemap.RefreshTile` reset it to white, even with `TileFlags.None`. `Dev_WaterRenderer.Refresh` now refreshes the tile before applying the final per-cell tint. The focused rendering category then passed 5/5, followed by the complete 57/57 EditMode and 20/20 PlayMode runs.

The NUnit XML CLI commands are documented in `Dev_WaterTestRunbook.md`. They were not executed during this session because the project was open in the Unity Editor; Unity cannot open the same project concurrently in batch mode.
