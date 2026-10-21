# ZoneSystem — Stage 3 Hover Migration

Modified only `Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs`; created this report. Existing ZoneController APIs fully support the migration, so no controller, renderer, geometry, scene, prefab, Inspector assignment or consumer changes were needed. Stage 3 implementation and automated checks are complete; actual Play Mode verification remains pending.

Hover flow before: `mouse → FloodDefenseBoxStamp.TryGetZoneTilesAtWorld → local boundary calculation/stamp coordinate conversion → renderer`.

Hover flow after: `mouse → ZoneController.TryGetZoneTilesAtWorld → SetZoneOutline(Hover, geoid, color) → existing ZoneGeometry/ZoneBoundaryEdge and controller coordinate conversion → registered ZoneOutlineRenderer`. The old private hover drawing method was removed. Raw-set/compatibility boundary builders remain exclusively for other legacy paths.

The GEOID/tile-set identity cache avoids rebuilding unchanged hover geometry and detects replacement tile sets after index rebuilding. Cache values are committed only after successful drawing. An unavailable controller, registration, renderer or map clears only Hover and resets its cache for retry; no scene search, controller creation or fallback stamp lookup occurs in the hover path. Appearance, renderer ownership, lifecycle and barrier policy remain unchanged, including cached persistent hide/restore and existing flag quirks.

Remaining FloodDefenseBoxStamp dependencies:

- Serialized assignment, initial discovery and existing missing-stamp visibility behavior remain for compatibility.
- `IsZoneBoundaryModeActive` still supplies barrier state; registration still uses its terrain reference to match the existing controller.
- Legacy public show guards, raw normalized-set conversion, and non-hover startup fallback queries/conversions remain. External consumers and their own stamp dependencies are deferred to Stage 4.

Validation performed:

- Unity 6000.0.63f1 runtime assembly compilation passed using project references: zero errors, 12 warnings in untouched files.
- Preserved all 19 public signatures/overloads, 12 serialized fields/types/defaults, both request structs and script GUID. Registration, appearance/suppression configuration, disposal, compatibility methods and geometry fallbacks remain unchanged.
- Temporary tests passed 1,842 Stage 2/3 rendered-output comparisons and 2,061 assertions using Unity stand-ins with the actual controller/index/geometry/renderer sources. Coverage includes all seven channels, candidate paired clear, hover miss isolation, barrier flag combinations, materials, negative origins, irregular zones/holes/seams, readiness recovery, same-GEOID index replacement, duplicate registration and destruction.
- Focused counters confirmed zero stamp hover lookups/conversions. Across 100 steady hover updates, world lookup continued without geometry conversions, line writes or new render objects. Static inspection found no explicit allocation or scene search in Update; this does not replace native Unity profiling.
- All 3,811 protected files matched their starting hashes, including ZoneController, ZoneOutlineRenderer, ZoneIndex/ZoneGeometry, scenes, prefabs, metadata, consumers and Barrier/Water systems. Test/build artifacts stayed outside Assets.

Manual Play Mode checks in BOTH Dev_UINavigation and PlayableTilemap:

1. Hover adjacent/irregular zones, holes, exterior edges and empty terrain. Verify immediate transitions, negative-origin alignment, yellow color, authored width `0.08`, template/material, transparency, Z and sorting. Leaving a zone must clear only hover.
2. Move within one zone repeatedly, then return after leaving it. Confirm stable pooled child counts after capacity growth and no repeated boundary generation/line writes for unchanged hover; use the Unity Profiler for actual allocation checks.
3. Exercise baseline/live overlays and their existing UI mutual exclusion, shelter candidates, placed coverage and tower hover. Zone hover/misses must preserve unrelated channels; candidate clear must preserve placed coverage, and tower clear must preserve ordinary hover.
4. Enter Place Barriers and Escape with persistent overlays cached. Verify all legacy flag combinations, including the `(hover=false,persistent=true)` show/next-Update quirk. With `outliningEnabled=false`, only ordinary hover stops; cached persistent behavior remains unchanged.
5. Using an attached debugger, test an unready loader and recovery, then rebuild the zone index while remaining over the same GEOID: hover should retry/redraw without clearing other channels. Recheck component/GameObject disable/re-enable and owner destruction; pools and assigned assets must follow Stage 2 behavior. Do not save runtime Inspector changes.

No technical blockers were found. Unity was not launched for this validation; runtime visual parity remains unverified. Review the changes, complete the manual checks and commit before Stage 4 consumer migration.
