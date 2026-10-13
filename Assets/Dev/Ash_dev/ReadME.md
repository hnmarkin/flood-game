# Unity Flood Game — Controller Refactor Overview

## Purpose
- Separate zone lookup, barrier placement, and flood simulation into clear systems.

## 1. ZoneSystem — `ZoneController`
- **Use for:** GEOID lookup, zone tiles, zone boundaries, and coordinate conversion.
- **Supporting scripts:** `ZoneIndex` indexes census zones; `ZoneGeometry` calculates boundaries; `ZoneBoundaryEdge` describes edges.
- **Inputs:** Existing map data from `JsonMapLoader` / `TileMapData`.
- **Key APIs:** Zone tile/boundary queries (including `TryGetBoundaryEdges`) and `NormalizedTileToCell`.
- **Coordinate rule:** Zone tiles use normalized grid coordinates; convert to Tilemap cells before calling water APIs.
- **Do not:** Build a second GEOID index or duplicate boundary calculations.

## 2. BarrierSystem — `BarrierController`
- **Use for:** Entering/exiting placement mode, placing zone barriers, reading placement state, and reacting to placement events.
- **Supporting scripts:**
  - `BarrierPlacementInput`: Mouse hover, selection, and click requests.
  - `BarrierVisuals`: Boundary previews and committed outlines.
  - `BarrierPlacementState`: Committed GEOIDs, limits, and legacy compatibility state.
  - `BarrierPhysicsAdapter`: Converts boundary edges and calls `WaterController.TrySetBarrierX/Y`.
- **Placement flow:** Select zone → preview boundary → validate → register supported physics edges → commit → update HUD/toasts/events.
- **Inspector owner:** Placement mode, max barriers, height, seepage, previews, outlines, and debug options.
- **Map edges:** Internal edges use water barrier APIs; exterior edges rely on the simulation's boundary behavior.
- **Caution:** `Wall` boundaries differ from `Source`/`Sink`; exterior edges do not receive player-defined height/seepage.

## 3. WaterSystem — `WaterController`
- **Use for:** Water initialization, simulation lifecycle, water-depth queries, and barrier registration via public APIs.
- **Physics barrier APIs:** `TrySetBarrierX(rightTileCell, height, seepage)` and `TrySetBarrierY(upperTileCell, height, seepage)`.
- **Water depth:** Query `GetWaterDepth(tileCell)` using actual Tilemap cell coordinates.
- **Ownership:** `WaterController` and its physics/lifecycle helpers remain maintained separately; the refactor did not modify them.
- **Flood startup:** Barrier placement does not start the simulation; the game lifecycle controls when it begins.

## 4. Flood Impact and Validation
- `FloodImpactController` reads depths from the new `WaterController` for zone impact calculations.
- `FloodImpactValidationWindow` is an Editor-only tool for comparing independently sampled depths with reported zone metrics.
- A sampled seven-tile zone matched reported flooded tile count and average depth; broader testing is still needed.

## 5. Legacy Compatibility — `FloodDefenseBoxStamp`
- Remains attached where needed as a forwarding adapter for older UI calls, events, and zone/barrier queries.
- Retains `IBarrierProvider` compatibility for the legacy `WaterSimulator` path.
- Old duplicate Inspector settings are hidden; **configure barriers in `BarrierController`, not here**.
- Existing scenes and consumers should migrate gradually; do not delete the adapter yet.

## 6. Setup and Known Limitations
- Assign required `ZoneController`, `BarrierController`, `WaterController`, Tilemap, HUD, and toast references in the relevant Inspectors.
- Copy any customized old barrier settings into `BarrierController` manually; no automatic migration was added.
- `WaterController` has no batch rollback: a rare mid-registration failure may leave partial physics edges.
- Water reset/reinitialization may clear registered barriers while placement state still lists committed zones.
- Verify interior and map-edge placement, HUD/visuals, and flood behavior before treating integration as complete.
