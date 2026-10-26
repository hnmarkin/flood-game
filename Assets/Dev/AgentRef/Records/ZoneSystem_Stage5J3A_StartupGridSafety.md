# ZoneSystem — Stage 5J3A: Startup grid safety

Completed 2026-10-21. BarrierController now validates compatibility dimensions after loader readiness and before mode activation or a placement commit. Empty startup grids can resize; committed grids cannot. Same-size calls preserve state. Legacy adapter configuration obeys the same guard. Runtime compilation passes: 203 sources, zero errors, the same 11 warnings. Temporary regressions pass 787 assertions; 13 normal-gameplay snapshots match baseline. Stamp, scenes, Inspector values and all existing metadata remain unchanged. Unity Play Mode was not run. The startup code blocker is resolved under the approved fixed-map policy; native verification and separately authorized retirement remain outstanding.

## Preflight and original failure

Read `ZoneSystem_Stage5J12_FinalDependencies.md` and `ZoneSystem_Stage5J34_FinalStampRetirement.md`. Inspected BarrierController, BarrierPlacementState, JsonMapLoader, ZoneController and FloodDefenseBoxStamp, plus the existing physics adapter, visuals and water barrier implementation used for validation.

JsonMapLoader.Awake calls PaintFromJson; its execution order is 100. It sets TileMapData.N from the loaded map. The saved map starts at N=5, while the current JSON is 64×64. TileMapData.GridWidth/GridHeight already supply the existing N+2 padding. Default-order BarrierController.Awake can therefore create a provisional 7×7 grid before the required dimensions become 66×66.

Previously entry waited up to 30 frames, rebuilt the zone index, then enabled placement without updating an existing compatibility grid. A distant boundary could be rejected against the stale grid. Separately, the legacy adapter initializer could resize a committed grid, clearing provider/placement state and HUD while leaving water barriers and committed outlines installed.

The new request explicitly establishes a fixed map for each gameplay session. This supersedes the previous stage's requirement to preserve a destructive post-placement resize/reset. No coordinated runtime map-replacement/reset feature was implemented.

## Exact source changes

Only these existing source files changed:

| File | Change |
| --- | --- |
| `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierPlacementState.cs` | Add `TryGetLegacyGridDimensions(out int gridWidth, out int gridHeight)`. It reads existing array lengths, returns false/zero dimensions when absent, and exposes no arrays or mutable state. All prior source, including InitializeLegacyGrid and its reset semantics, is byte-identical. |
| `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierController.cs` | Change only InitializePlacementState, EnterZoneBoundaryPlacementModeRoutine, TryCommitZoneBoundaryBarrier and ConfigureReferencesFromLegacyAdapter as described below. No fields, serialized defaults, event declarations, provider getters or other methods changed. |

InitializePlacementState obtains required dimensions from the current TileMapData properties. Matching dimensions return successfully without allocation or reset. Different/absent dimensions can call the existing InitializeLegacyGrid only when BarriersPlaced is zero. With any committed placement, it logs an error containing existing dimensions, required dimensions, committed count and the blocked-placement policy, then returns false before initialization.

Entry retains its existing reference validation, mode/preview cleanup and bounded 30-frame readiness wait. It now refuses to proceed if the loader is still unavailable, even with a previously cached zone index. After readiness, it validates/initializes dimensions **before** index rebuilding, HUD refresh and mode activation. The final commit path independently checks readiness and the same initializer before duplicate/limit/boundary checks, water writes, state recording, HUD/outline changes or placement/completion events. This prevents an already active mode or direct commit call from bypassing the guard. No dimension check was added to Update or normal water stepping.

The common initializer also protects the existing Awake, missing-grid validation and legacy adapter paths. ConfigureReferencesFromLegacyAdapter returns before child/HUD configuration when initialization fails with committed placements. Its original missing-map/no-placement setup behavior is retained. The stamp itself is unchanged.

## Policy and preserved behavior

| Situation | Result |
| --- | --- |
| Awake before map load | Provisional grid remains allowed. |
| Ready map, same dimensions | Preserve all six arrays and committed zones; no reset. |
| Ready map, changed dimensions, zero commits | Use existing initializer to size the empty grid before enabling placement. |
| Changed dimensions after one or more commits | Reject entry/commit/configuration reset; preserve compatibility grids, placement count, installed water barriers, committed outlines and HUD progress. |
| Loader missing/unready | Reference validation or bounded wait refuses entry; a commit cannot use cached zones to bypass readiness. |

Mismatch rejection does not publish placement, instance completion or global completion events, and therefore sends no false Advisor/Summary notification. It does not refresh HUD. Existing entry/cancellation preview cleanup and mode transitions retain their established behavior. A rejected commit does not itself force the active mode off; subsequent commits remain blocked while the mismatch persists. Returning to the original dimensions makes the guard pass without resetting state; this is defensive recovery, not support for loading a replacement map during gameplay. Same-size map-content replacement is outside the fixed-map contract and is not detected by this dimension check.

Saved maximum 4, required count 3, height 40, seepage 0.001, eligibility, physics registration, preview/outline implementation, placement arguments and completion ordering remain unchanged. No archived stamp settings were copied. No new grid owner, cache, gameplay state, per-frame polling, assembly or execution-order adjustment was introduced.

## Compilation and regression results

Fresh baseline snapshots/hash inventory covered **6,243 starting Assets files**; tests and compiler outputs are outside Assets at `/private/tmp/zone-stage-5j3a-27fifmml`.

- Installed Unity 6000.0.63f1 Roslyn compiler with current project references: before/after **203 runtime sources, zero errors, 11 warnings**. Sorted diagnostic paths, locations and messages match exactly. Targeted whitespace/diff checks pass. Repository-wide diff checking still reports unrelated pre-existing scene/layout whitespace; those files were not changed.
- Temporary harness: **299 baseline / 787 updated assertions pass**. The baseline separately reproduces stale-grid distant rejection and the destructive adapter resize. Updated cases assert their corrected outcomes; these intentional differences are excluded from parity comparisons.
- **13 comparable normal-gameplay snapshots match exactly**, covering placed/HUD counts, Advisor messages and callback counts, water write coordinates/heights/seepage, committed tile coordinates, Summary data/action and event order.
- Cold startup runs at origins (0,0), (11,5) and (-20,-10): actual controller Awake at N=5 creates 7×7, entry waits for readiness, loader fixture supplies a 64×64 map, all six empty arrays become 66×66, and a distant zone at normalized tiles (60–61,60–61) commits without stamp configuration. Provider padding and world-origin water requests are checked.
- Already-ready initialization and repeated same-size mode entry/adapter configuration preserve all six array identities and values, existing commits, HUD, actual water-barrier values and committed outline cells. Duplicate/unknown zones emit no success notifications.
- Post-placement mismatch cases use one and three commits with unexpected N=5, 9 and 64. Direct final commits, normal input commits while mode is active, repeated re-entry, legacy adapter setup, stamp OnEnable and direct initializer calls all preserve committed state and reject further placement. Checks cover six provider getters over the full tested grid and out-of-range coordinates, actual water data/write counts, outline cells, HUD values/calls, Advisor notices, Summary refreshes and both completion events. Diagnostics include both sizes and count.
- Readiness tests cover missing payload/geoid grid/cell mapping/loader/map, cached indexes, timeout, recovery, cancellation while waiting and an unready loader during active placement. No empty provisional resize occurs on timeout before readiness.
- Escape, controller disable/re-enable, same-size recovery and maximum-four behavior pass. Rejected/partial water registration retains existing notification behavior. Actual Advisor receives one callback per successful commit with the existing placed/required arguments. Required-three does not complete the operation: on placement four the trace remains **placement/Advisor → instance completion → global completion/Summary → mode exit**.

The harness compiles actual BarrierController, BarrierPlacementState, BarrierPhysicsAdapter, BarrierVisuals, ZoneController/index/geometry, WaterPhysicsBarrier, AdvisorPanelController and GameStateSummaryBuilder, plus supporting shelter/evacuation/provider code. A temporary WaterController facade forwards writes into the actual water barrier grid, preserving the existing origin-to-padded-coordinate mapping. Engine/input/Tilemap/UI, loader data, map and other data-service seams are temporary stand-ins. Loader readiness and the N change are driven explicitly; JsonMapLoader's native Awake/JSON execution, full active WaterController simulation, Unity lifecycle scheduling/input/rendering and scene reload were not executed. Full runtime compilation includes those unchanged production implementations. **No Unity Play Mode verification is claimed.**

## Scope, serialization and retirement dependencies

SHA-256 comparison against the fresh starting inventory confirms only the two approved existing sources changed. No starting asset was deleted. This report is the only added deliverable; Unity can generate its ordinary report metadata. Both complete scenes, all prefabs, map assets/JSON, UI assets, project settings and every existing .meta file remain unchanged. Source and GUID of FloodDefenseBoxStamp are preserved: **a99f55ae89eac4deca4c7d4c65418c4e**. Its component **1046329326** remains attached in both scenes. BarrierController **420333452** retains its entire saved Inspector configuration.

Fresh Assets/Packages C# search finds no external FloodDefenseBoxStamp type consumers. Its serialized script GUID still occurs only in its metadata and the two retained scenes. Previous dependency migrations remain intact: Summary uses BarrierController's global completion event; Communication no longer needs stamp fallback; dormant WaterSimulator providers point to BarrierController.

**Stage 5J4 assessment:** the startup sizing and post-commit preservation code gates now pass under the approved fixed-map policy. Removal was not attempted. Before retiring the stamp, complete native PlayableTilemap startup/independent-operation checks, then in a separately authorized stage repeat GUID/PPtr/UnityEvent checks, remove only the stamp component records/list entries while retaining their GameObjects, and finally delete its source/metadata. Its remaining lifecycle effects include configuration/relays and the one-time placement cancellation on stamp OnDisable. BarrierController already owns normal cancellation; do not add a synthetic retirement cancellation mechanism. No new API extension outside this stage's scope is required for the approved startup policy.

## Manual Unity checklist — PlayableTilemap only

1. Import/compile and inspect BarrierController **420333452**: maximum 4, required 3, height 40, seepage 0.001, unchanged preview/outline and map/loader/zone/water references. Confirm stamp **1046329326** remains attached. Do not run or delete Dev_UINavigation; do not save test mutations or modify either scene.
2. Start fresh with the saved N=5 asset, rather than relying only on an already-mutated in-memory N=64. Attach a debugger and watch `_placementState._blockedX.GetLength(0/1)` and the other five arrays: provisional 7×7 is allowed; after JSON load and before mode activation require 66×66. Hover and place a distant boundary. Verify live water blocking, outline, HUD 1 and one Advisor message. Repeat with data ready before controller initialization in an unsaved controlled fixture.
3. Cancel/re-enter repeatedly at the same size; disable/re-enable BarrierController. Verify commits, provider values, water barriers, committed tiles and HUD remain. Check pending-entry cancellation, unavailable map/loader, readiness timeout and later retry. No placement may begin from a cached index while loader data is unavailable.
4. In an unsaved runtime fixture/debugger only, commit one or three barriers, change map N, and try both re-entry and a click while placement mode was already active. Require the dimensions/count diagnostic, unchanged existing grids/water/outlines/HUD and no placement, completion, Advisor or Summary notification. Also exercise retained stamp setup/re-enable. Restore the original dimensions without saving.
5. Test nonzero/negative origins and padded coordinates. Place four valid zones: Advisor may show 3/3 on placement three; Summary must refresh once on placement four before mode exit. Check failed/duplicate placement, maximum rejection and preview clearing. Confirm Communication, Shelter, Evacuation, flood overlay and active flood simulation still operate normally; keep the saved legacy WaterSimulator dormant with provider BarrierController.
6. For the separately authorized retirement gate, use an unsaved PlayableTilemap test setup without stamp configuration and verify cold startup, input, placement, lifecycle and scene reload. Do not interpret this source-level harness as native stamp-free scene acceptance. Review and commit Stage 5J3A before the final removal stage.
