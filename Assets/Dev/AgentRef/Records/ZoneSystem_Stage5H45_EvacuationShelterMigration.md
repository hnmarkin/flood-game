# ZoneSystem — Stage 5H4 + 5H5: Evacuation and shelter zone data

Evacuation and shelter now obtain zone data directly from ZoneController. Evacuation's stamp field was replaced; ShelterCandidateContext and its known construction/consumption sites were migrated together. ShelterController retains its stamp field, validation and barrier-mode controls. Both scenes explicitly reference the existing controller. Compilation passes with 203 runtime sources, zero errors and the same 11 warnings. Independent regressions pass 443 assertions; all 142 captured states match before/after, including negative-origin conversions, candidate scoring, placement, evacuation results and event order. Scene YAML validates with only intended reference changes. Unity import and Play Mode were not executed.

## Preflight and API mapping

Reviewed the Stage 5A, 5H1 and 5H23 records and current target sources. Inspected the stamp/controller query contracts, index/geometry and loader behavior; these shared sources are byte-identical to the previously inspected Stage 5H23 versions. The request supplies successful Unity visual verification after Stage 5H23.

Searched all ShelterCandidateContext construction and use sites before editing. Only ShelterController, ShelterTypes.cs and ShelterCandidateResolver use the context. No external consumer needs its stamp field, and no source-scope extension is required. An explicit mapping was recorded before edits in the temporary preflight evidence.

| Original dependency | Classification / return type | Equivalent or retained behavior |
| --- | --- | --- |
| Evacuation stamp.TryGetZoneTiles | Membership and anchor lookup; bool plus HashSet<Vector2Int> | ZoneController.TryGetZoneTiles, same shared membership set |
| Evacuation stamp.TerrainTilemap | Terrain fallback; Tilemap | ZoneController.TerrainTilemap; explicit route Tilemap retains priority |
| Evacuation stamp.NormalizedTileToCell | Forward conversion; Vector3Int | ZoneController.NormalizedTileToCell |
| Evacuation stamp.TileOrigin subtraction | Inverse conversion; Vector2Int followed by IsWithinMap | ZoneController.CellToNormalizedTile followed by the same bounds check; null provider retains zero-origin fallback |
| ShelterController stamp.TryGetZoneTiles | Fallback population membership; bool plus HashSet<Vector2Int> | ZoneController.TryGetZoneTiles |
| ShelterController stamp.TerrainTilemap | Terrain/placement fallback; Tilemap | ZoneController.TerrainTilemap; explicit target then terrain still take priority |
| Context stamp field, TerrainTilemap and EnsureZoneIndexReadyForExternalUse | Zone provider, terrain and readiness; Tilemap / bool | Context ZoneController field and corresponding existing controller APIs |
| Resolver context.GetAllZoneTiles | Grouping membership; IReadOnlyDictionary<string, HashSet<Vector2Int>> | context.ZoneController.GetAllZoneTiles |
| Resolver context.NormalizedTileToCell | Candidate cells, world centers and loader row/column lookup; Vector3Int | context.ZoneController.NormalizedTileToCell |
| Shelter stamp.IsZoneBoundaryModeActive and ExitBuildModeFromUI | Barrier state / control; bool / void | Retained verbatim, together with stamp field/discovery/validation |

Evacuation has no non-zone stamp use or stamp event subscription. The context and resolver use the stamp solely for zone data. ShelterController has the barrier uses above; its typed reference remains required. No equivalent zone API was missing.

Both scenes already configure controller 2070746514 and stamp 1046329326 with the same map, terrain and loader. Shelter's references match: map GUID 7e81a30567a6c1b44ae537c0db92678b, terrain 1822120955 and loader 1678446140. Evacuation uses the same map asset. Existing separate route/target Tilemaps remain assigned. Direct queries use the same ZoneIndex and mutable sets without a new index, cache or collection copy.

## Stage 5H4 — Evacuation

Source: Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationController.cs, explicitly authorized by this stage.

- Replaced the private stamp field with a serialized ZoneController field. Retargeted membership queries, terrain fallback, provider validation and forward conversion.
- Replaced explicit stamp-origin subtraction with the existing CellToNormalizedTile API. The null branch still returns cell XY unchanged, and the existing map bounds check remains.
- Updated the Inspector-reference comment and missing-provider diagnostic. Public APIs, route data, path search, anchor selection, costs, population/capacity allocation, mitigation, selection/confirmation, event handlers and rendering calls are unchanged.
- Controller resolution stays in the existing ResolveReferences path. Its fallback search is limited to one attempt; explicit serialized references avoid discovery in both scenes. No controller/object is created by the migration.

**Independent results:** 284 assertions before, 288 after; **70 matching states**. Compared complete routes and ordered path cells, anchors/endpoints, selected/confirmed IDs, population/mitigation totals, event order, preview/hover/tooltip/visualizer requests, and selection/confirmation limits. Hand-calculated capacity totals pass. Covered valid/unknown GEOIDs, empty multi-cell membership, nonzero/negative origins, outside-world lookup, shelter additions, inactive shelters, cleared shelter state, warm cache reuse, invalidation/unready recovery, missing-provider failure and explicit restoration. Additional migrated assertions cover bounded discovery and route calculation without a stamp component.

**Remaining direct stamp dependency:** none. Evacuation still consumes ShelterController, whose barrier compatibility remains separate.

## Stage 5H5 — Shelter

Modified only:

- Assets/Dev/Dev_Shelter/Scripts/ShelterController.cs
- Assets/Dev/Dev_Shelter/Scripts/ShelterTypes.cs
- Assets/Dev/Dev_Shelter/Scripts/ShelterCandidateResolver.cs

ShelterController adds a serialized ZoneController field, resolves it at the existing reference-resolution point with a one-attempt fallback, requires it during validation and uses it for zone population and terrain/placement access. Existing stamp validation remains, preserving the required barrier dependency. Context construction now assigns ZoneController instead of FloodDefense.

ShelterCandidateContext replaces `public FloodDefenseBoxStamp FloodDefense` with `public ZoneController ZoneController` in the same field position. Other context fields, lifetime, placement-Tilemap priority and readiness gates are unchanged. It has no constructor or Unity-serialized scene record to migrate. All known construction/consumption sites were updated; no obsolete context field remains.

ShelterCandidateResolver changes only provider names. Its entire source equals the original after those substitutions. Grouping, enabled/disabled-zone handling, candidate search, filters, scoring/tie order, fallbacks, centroid logic and result fields are unchanged. ShelterController's barrier state check and ExitBuildModeFromUI call remain verbatim. Placement, capacity, metrics, events and presentation/input call bodies remain unchanged.

**Independent results:** 155 assertions before and after; **64 matching shelter states plus eight matching context-failure states**. Compared candidate positions/world centers, associations/order, exact scores/debug reasons, context failure clearing, loader fallback, cached candidate identity and regeneration, index reuse/invalidation, placed-shelter data and mitigation, placement/change/mode event order, presentation arguments and evacuation refreshes after placement. Profiles cover grouping, single-zone shelters, disabled-but-served zones and deeply flooded host tiles. Barrier mode exits before shelter placement as before; the entry-frame click guard remains intact.

**Removal validation limit:** ShelterController exposes no public removal method. Tests used the existing isActive filtering and ShelterState.Clear/RebuildZoneMetrics followed by evacuation refresh. Inactive shelters retain the existing occupancy behavior; clearing state frees occupancy and removes routes/selections. No removal API or gameplay behavior was invented.

**Remaining direct stamp dependency:** ShelterController's barrier state/control, field/discovery and required-reference validation. The context and resolver have no stamp dependency.

## Coordinate and lifecycle equivalence

ZoneIndex stores normalized tiles relative to its captured terrain bounds origin. Forward conversion adds this origin once; CellToNormalizedTile subtracts the same TileOrigin that the stamp previously exposed. Both ignore cell Z for normalized XY. Evacuation's fallback from a shelter world position through routeTilemap.WorldToCell still uses the same inverse and bounds test. No simulation padding or new offset was added.

ShelterCandidateResolver's existing inverse uses **PlacementTilemap.cellBounds**, rather than stamp.TileOrigin. That independent behavior was preserved, including a test where target and terrain origins differ. Target/terrain precedence, absolute candidate cells, cell-center world positions and loader cell-to-row/column lookups are unchanged. Tests use origins **(0,0), (11,5), (-20,-10)** and variable data-driven map APIs; production dimensions/origin were not hardcoded.

GEOID ordinal indexing, consumer trimming and iteration order remain unchanged. Context readiness still requires payload, nonempty cell mapping, GEOID/category grids and N > 0 before controller index readiness. Evacuation retains lazy query readiness and its existing cached failure behavior; no stronger loader guard was introduced. Neither migration copies the stamp's query-time configure/create fallback. Shared controller setup and stamp startup remain unchanged.

**Documented resolution adjustment:** newly introduced controller discovery attempts once at the existing ResolveReferences call site, rather than repeatedly searching when a provider is absent. Both scenes supply explicit references. A controller created after an unsuccessful attempt needs explicit assignment; subsequent data queries can then recover without a new search loop. Awake/OnEnable paths and event/placement ordering otherwise remain in place.

## Scene changes

Applied identical minimal reference edits to Assets/Game/Content/Scenes/PlayableTilemap.unity and Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity:

| Existing component | Script GUID | Exact field change |
| --- | --- | --- |
| EvacuationController 315251714 | ef31d31705ac48f898e5746ce90d32fa | Remove floodDefenseBoxStamp = 1046329326; add zoneController = 2070746514 |
| ShelterController 1420635165 | 889273345757b49ba982c2c5ca64aa4b | Add zoneController = 2070746514; preserve floodDefense = 1046329326 |

Route Tilemap 36957482, shelter target Tilemap 628953066, terrain, loader, map, camera, presentation/input, baseline-risk references and all other Inspector values remain unchanged. Component GUIDs/identities, hierarchy and active states are preserved. The PlayableTilemap edit uses this request's explicit Assets/Game authorization. Dev_UINavigation received serialized compatibility changes only and was not run.

## Combined validation

- **Compilation:** installed Unity 6000.0.63f1 Roslyn with current project references; before/after both **203 runtime sources, 0 errors, 11 warnings**, with identical diagnostic paths, locations and messages.
- **Regression:** **439 baseline and 443 migrated assertions pass; all 142 captured states match.** Actual before/after target sources and actual unchanged stamp, ZoneSystem, shelter state, placement, metrics and shared data contracts were compiled. Temporary engine, baseline-data, presentation/input and route-visualizer stand-ins supply the external seams. Random shelter GUIDs were normalized by creation order for comparison; their generation/uniqueness was checked separately.
- Snapshots compare computed data and ordered events/calls, including shelter presentation arguments and route draw requests. They do not establish native Unity rendering, actual input delivery, deferred destruction or full scene execution. Unchanged ShelterPresentation/placement-input/route-visualizer source was not modified to support testing.
- Source comparisons verify **131 complete controller methods** unchanged and exact provider substitutions in context/resolver. Public evacuation/shelter APIs and placement/mitigation algorithms remain intact; only the explicitly authorized plain context provider contract changes.
- **YAML:** Ruby Psych parses 346 PlayableTilemap and 360 Dev_UINavigation records before/after. Only records 315251714 and 1420635165 change. All other records are byte-identical; no objects/components were added or removed. Nonzero local references resolve, with no duplicate records/component entries. Rendering root, eight controller renderer settings, hover/visibility, stamp and BarrierController records remain unchanged.
- Existing unrelated external script bindings documented in Stage 5FG remain untouched. No script GUID or new script binding changed; Unity import must assess any pre-existing missing-script issue separately.
- **Scope:** SHA-256 comparison against 6,185 starting Assets files confirms only the four authorized sources and two scenes changed, with this report and its normal Unity-generated metadata added. No files were deleted. Every other starting asset and all existing metadata are byte-identical; stage-only whitespace checks pass.
- Completion verification on 2026-10-21 recompiled the current sources with the same zero errors/11 warnings and reran the migrated harness: 443 assertions pass and all 142 states still match the saved baseline. Both scenes remain byte-identical to their parsed, validated outputs.
- Evidence is outside Assets at /private/tmp/zone-stage-5h45-zkcaivub. **Unity Editor/Play Mode was not executed.**

## Stage 5I readiness and remaining dependencies

Fresh source searches find no remaining external calls to the stamp's zone-query/conversion methods. Flood impact, baseline, evacuation and shelter context/resolver now use ZoneController for those responsibilities. The stamp is **not ready for deletion**:

| Remaining consumer/responsibility | Follow-up |
| --- | --- |
| ShelterController barrier-mode state and ExitBuildModeFromUI | Stage 5I control/state migration, including its required stamp validation |
| ActionsPanelController and PreparationCardsController | Barrier controls, mode state and mode-event wiring |
| AdvisorPanelController | Barrier-placement event subscription |
| GameStateSummaryBuilder | Static all-barriers event subscription; preserve event scope/timing |
| CommunicationTowerController | Retained placement-data MapData fallback and initialization anchor from Stage 5H3 |
| Stamp/BarrierController startup and dormant WaterSimulator provider assignment | Separate setup/provider migration before component/script retirement |

Stage 5I can begin with its own fresh audit. No missing equivalent zone API or external context consumer blocks this completed stage. Late/unassigned controller discovery and the absent public removal API are the limits described above, not reasons to expand source scope here.

## Manual Play Mode checklist — PlayableTilemap only

1. Import/compile. Verify both consumers reference ZoneController 2070746514. Evacuation has no stamp field; Shelter retains stamp 1046329326. Check unchanged route/target Tilemaps, map/loader, camera, presentation/input, baseline and renderer settings.
2. Regenerate/show shelter candidates and compare groups, IDs, cells, world centers, scores, ordering and eligibility with Stage 5H23. Check overrides, water/deep-flood exclusion and placement limits. Place a shelter; verify capacity, population mitigation, outlines and one placed/change notification.
3. Enter shelter placement while barrier mode is active. Confirm the same mode exit and placement behavior, entry-frame click guard, ghost and candidate highlights. Confirm barrier placement still works afterward.
4. Generate/select/confirm evacuation routes. Compare anchors, ordered paths, source populations, shelter capacity, evacuation counts/percentages and statuses. Toggle previews, hover routes, place another shelter and verify automatic route refresh and existing selection preservation/removal rules.
5. In disposable runtime/reference tests, check cold/unready data, cached index reuse, explicit invalidation, unknown/empty zones and nonzero/negative origins. Verify explicit provider restoration. Inspect inactive/cleared shelter state through existing debugging tools; there is no public removal API added by this stage. Do not save test mutations.
6. Check end-to-end shelter/evacuation mitigation interactions and existing overlays. Review and commit after manual verification. Do not run or delete Dev_UINavigation for this stage.
