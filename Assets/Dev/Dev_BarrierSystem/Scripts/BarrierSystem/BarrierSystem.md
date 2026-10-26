# BarrierSystem

Temporary Dev system for zone-boundary barrier placement.

## Architecture

- `BarrierController` owns placement mode, limits, events, HUD/toast updates, and committed placement state.
- `BarrierPlacementState` stores committed GEOIDs and the legacy padded edge grids used by `IBarrierProvider`.
- `BarrierPlacementInput` reads mouse/escape input only while placement mode is active.
- `BarrierController` owns barrier settings, preview settings, outline settings, and placement debug logging.
- `BarrierVisuals` draws hover previews and committed zone outlines from controller-owned settings.
- `BarrierPhysicsAdapter` converts `ZoneBoundaryEdge` data into `WaterController.TrySetBarrierX/Y` calls.
- `FloodDefenseBoxStamp` remains a forwarding adapter for old UI callbacks and legacy water compatibility.

## Placement Flow

1. UI calls `FloodDefenseBoxStamp.EnablePlaceBarrierMode()` or `BarrierController.EnablePlaceBarrierMode()`.
2. The controller waits for zone data, rebuilds the zone index, and enables input.
3. Hovered zones use `ZoneController` for deterministic boundary previews.
4. On click, the controller validates the whole boundary and legacy grid first.
5. Interior edges are registered with WaterController before state is recorded.
6. Exterior map edges are counted as protected by the simulation boundary and no invalid coordinates are registered.
7. Success events and committed outlines are raised after state recording succeeds.

## Inspector Setup

- Keep `FloodDefenseBoxStamp` on the existing scene object for legacy callers.
- Add or allow runtime creation of `BarrierController`, `BarrierPlacementInput`, `BarrierVisuals`, and `BarrierPhysicsAdapter` on that object.
- Assign map, camera, loader, zone, water, limits, barrier values, visuals, and debug settings on `BarrierController`.
- `FloodDefenseBoxStamp` only forwards legacy UI calls, events, zone helpers, and `IBarrierProvider`.

## Known Limitations

- `WaterController` has no public rollback API; unexpected mid-registration failure can leave partial water barriers.
- Barriers are not automatically reapplied after `WaterController` reset or reinitialization.
- Source/Sink boundary effects can still add or remove water at map edges; player exterior barrier physics are not exposed.
- BarrierSystem never starts, resets, pauses, or advances flood simulation.
