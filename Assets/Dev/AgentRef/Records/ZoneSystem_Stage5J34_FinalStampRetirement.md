# ZoneSystem — Stage 5J3 + 5J4: Final stamp retirement gate

**Status: blocked; retirement is not complete.** Investigated 2026-10-21. The requested resize safety gate fails: changing compatibility-grid dimensions clears committed placement/provider state and resets HUD progress while leaving live water barriers and committed outline tiles intact. A focused diagnostic confirms the inconsistency. The task explicitly requires stopping in this situation. No startup correction, scene cleanup or source deletion was applied. FloodDefenseBoxStamp, its metadata and both scene components remain unchanged. Current runtime compilation passes with 203 sources, zero errors and the same 11 warnings. Only this report was added. Unity Play Mode was not run.

## Applicable stop condition

Section 2 of the supplied Stage 5J3–J4 request states:

> If the existing resize operation leaves water barriers, outlines or other state inconsistent, stop and report the required additional scope rather than introducing an incomplete repair.

Adding a readiness-time InitializePlacementState call alone would repair an empty cold-start grid, but would also invoke this inconsistent reset when dimensions change after a successful placement. The task requires testing and preserving reset behavior with existing committed state, so the empty-grid case alone cannot pass its retirement gate.

## Preflight inventory and preserved identities

Read the Stage 5I34 and Stage 5J12 reports. Inspected the requested controller/state/physics/zone/loader/map/water/provider/summary/communication/legacy-water implementations, plus BarrierVisuals and WaterPhysicsBarrier to check reset effects. Verified paths:

- `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs`.
- `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierController.cs`, `BarrierPlacementState.cs`, `BarrierPhysicsAdapter.cs`, `BarrierVisuals.cs`.
- `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneController.cs`.
- `Assets/Dev/Scripts/MapLoader/JsonMapLoader.cs`.
- `Assets/Game/Features/LegacyWaterSystem/ScriptableObjects/TileMapData.cs`.
- `Assets/Dev/WaterSystemRefactor/Scripts/WaterController.cs` and `WaterPhysicsBarrier.cs`.
- `Assets/Dev/Scripts/Flood/Interface/IBarrierProvider.cs`.
- `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs`.
- `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs`.
- `Assets/Game/Features/LegacyWaterSystem/WaterSimulator.cs`.

Fresh Assets/Packages C# searches find no external FloodDefenseBoxStamp type references, component lookups, event subscriptions or runtime creation paths. Summary uses BarrierController's authoritative static event; Communication no longer has a stamp field/fallback; legacy WaterSimulator's provider is BarrierController. The stamp file declares only its MonoBehaviour; IBarrierProvider remains independently declared.

Fresh serialized/GUID searches find stamp bindings only in the authorized scenes:

| Scene | Retained component / owning GameObject | Remaining local reference |
| --- | --- | --- |
| `Assets/Game/Content/Scenes/PlayableTilemap.unity` | **1046329326 / 1046329324** | Its own component-list entry only |
| `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity` | **1046329326 / 1046329324** | Its own component-list entry only |

Script GUID remains **a99f55ae89eac4deca4c7d4c65418c4e**. Its non-documentation GUID occurrences are the two scene script bindings and its metadata. No prefab, other scene, persistent UnityEvent or external serialized consumer requires it. Historical audit mentions are preserved documentation.

Both WaterSimulator components **1818902683** retain provider **420333452**. The full scenes are byte-identical to their starting snapshots. BarrierController **420333452**, ZoneController **2070746514**, hover input **704415077**, visibility **704415078**, all GameObjects/other components and renderer settings remain unchanged. Maximum **4**, required count **3**, height **40**, seepage **0.001**, all preview/outline references/colors and inactive legacy-water settings are preserved.

The blocker is reset consistency, not an external stamp consumer.

## Startup-grid problem and proposed insertion point

Fresh inspection confirms the saved map asset `Assets/Game/Features/LegacyWaterSystem/TileMapDataTest.asset` has **N=5**. Both scenes load `Assets/StreamingAssets/tilemap_with_population_elev.json` with **64 rows × 64 columns**. JsonMapLoader metadata has execution order **100** and PaintFromJson updates the same map's N after default-order controller initialization.

BarrierController can therefore initialize a provisional **7×7** padded compatibility grid before the map requires **66×66**. Its entry routine waits for loader readiness and rebuilds zones, but existing-grid validation checks presence rather than current dimensions. The unchanged cold-start diagnostic still sees grid 7 after N becomes 64 and rejects a valid distant boundary.

The smallest intended location for dimension validation is the existing readiness/entry path after ready-map sizing and before placement is enabled, with HUD refreshed after any successful resize. Same-size initialization already preserves committed state. This is a proposed correction only: it was not inserted because a dimension-changing reset fails the additional consistency requirement below. No map asset, script order, polling loop, cache or hardcoded production dimension was changed.

## Reproduced resize/reset inconsistency

The diagnostic performs a successful placement using the actual BarrierController, BarrierPlacementState, BarrierPhysicsAdapter, BarrierVisuals, ZoneSystem and WaterPhysicsBarrier implementations. It then exercises the existing public ConfigureReferencesFromLegacyAdapter path, which invokes grid initialization and HUD refresh.

| Observation | Successful placement | Same dimensions reconfigured | Changed dimensions reconfigured |
| --- | --- | --- | --- |
| Compatibility grid | 10×10 | 10×10 | 11×11 |
| Committed placement count | 1 | 1 | **0** |
| HUD placed count | 1 | 1 | **0** |
| IBarrierProvider at committed edge | Blocked, height 40 | Unchanged | **Unblocked, height 0** |
| Live water barrier edges | 4 | 4 | **4 remain** |
| Painted committed outline tiles | 4 | 4 | **4 remain** |

The same-size case behaves correctly. The dimension-changing case produces incompatible views of the same barrier: the compatibility provider/HUD say it is gone, while active water physics and the committed outline retain it. No stamp configuration is used to establish the successful placement in this fixture.

Code evidence:

- `BarrierPlacementState.InitializeLegacyGrid` replaces compatibility arrays and clears `_committedZones` when dimensions differ. It has no reference to the water grid or visuals.
- `BarrierController.ConfigureReferencesFromLegacyAdapter` calls that initializer, configures children and updates HUD. Child configuration does not remove installed water barriers or committed tiles.
- `BarrierPhysicsAdapter.TryRegisterZoneBoundary` writes edges through WaterController. Its `_requests` list is temporary/current-registration data, not a complete durable collection of committed controller-owned writes, and it exposes no unregister/reset operation.
- `BarrierVisuals.PaintCommittedZoneOutline` paints tiles. Its only clearing API is **ClearPreview**; it has no committed-outline reset operation.
- `WaterController` exposes TrySetBarrierX/Y, but no targeted public edge-clear methods. Setting height zero does not remove a barrier: WaterPhysicsBarrier rejects non-positive heights, which the diagnostic also verifies.
- Lower-level WaterPhysicsBarrier already has TryClearBarrierX/Y, but it is private to the authoritative water path. Bypassing WaterController to mutate it would violate controller ownership.
- WaterController.ResetSimulation is not a targeted substitute: it stops simulation, creates new water/runtime/barrier state, reinitializes sources/rendering and raises the reset event. Calling it for compatibility-grid resizing would change the active flood simulation beyond this task.

## Required additional scope

Maintaining the requested post-placement dimension-change reset semantics needs a coordinated reset across placement/provider state, installed water edges, committed visuals and HUD. The present APIs cannot perform that transaction safely through BarrierController alone.

The smallest missing capabilities to authorize/design are:

1. **BarrierPlacementState.cs:** a side-effect-free way to inspect/match existing grid dimensions so the controller can plan a reset before it destroys committed information. Keep the existing same-size preservation and changed-size reset semantics.
2. **WaterController.cs:** targeted, validated removal of the affected barrier writes through the public water controller, using the existing lower-level clear operations without resetting water, sources or lifecycle state.
3. **BarrierPhysicsAdapter.cs:** a reset/unregistration contract retaining enough information about installed writes to remove the correct old edges, including old coordinate mapping. Specify shared-edge/other-controller ownership before clearing; its temporary last-request list is insufficient.
4. **BarrierVisuals.cs:** an explicit committed-outline cleanup operation with appropriate Tilemap ownership, in addition to preview clearing.

BarrierController, already authorized for modification, can then coordinate validation/cleanup, compatibility resizing and HUD refresh. Avoid clearing unrelated controllers' barriers or entire shared Tilemaps blindly. The exact ownership/transaction policy must be settled before implementing removal; adding clear wrappers alone is not sufficient proof of consistency. WaterPhysicsBarrier already supplies primitive clear operations, so this finding does not by itself require modifying that class.

All four listed source files are outside this task's allowed source modification list. No changes to them or an incomplete controller-only repair were made. An alternative policy that permits resizing only before any barriers are committed and rejects later dimension changes would need an explicit change to the task's required post-placement reset behavior; it was not silently substituted.

## Verification performed and gate results

- **Current runtime compilation:** Unity 6000.0.63f1 Roslyn/current project references, **203 runtime sources, zero errors, 11 warnings**. No gameplay source was changed. The expected reduction to 202 sources was not attempted because deletion is blocked.
- **Focused diagnostic: 19 assertions pass**, confirming the unchanged cold-size failure, successful stamp-independent ready-map placement/preview, same-size state preservation, changed-size mismatch and rejection of zero-height clearing. These passing diagnostic assertions reproduce the blocker; they are not a passing startup-correction or retirement acceptance test.
- The harness compiles actual controller, placement state, physics adapter, visuals, ZoneSystem and WaterPhysicsBarrier source. A temporary WaterController facade forwards writes into the actual water barrier grid, and temporary engine/Tilemap/HUD seams record rendering/progress. Actual WaterController reset/setter implementations were independently inspected and are included in full runtime compilation. Unity-native physics/rendering, loader execution order, input and scene lifecycle were not executed.
- Source/GUID/PPtr inventory passes the external-dependency check. Stamp source/metadata and both entire scenes remain unchanged; no component or GameObject was removed, no GUID was deleted and no new missing component was introduced.
- Cold-start **correction gate: not passed**. Committed-state resize **consistency gate: failed**. Final independent-operation/retirement regression suite: **not attempted after the mandatory stop**.

Evidence is outside Assets at `/private/tmp/zone-stage-5j34-vy6ah0u6`: starting asset hashes/status/source/scene snapshots, fresh dependency/GUID inventory, runtime compile log, diagnostic sources/executable/log and gate results.

## Independent ownership and remaining work

ZoneController already owns map/terrain/loader references, index/query/geometry and renderer lifecycle; BarrierController owns mode/input configuration, placement/provider state, limits, HUD/preview/physics requests and instance/global events. The diagnostic directly exercises readiness, boundary geometry and actual preview/committed visual methods without stamp configuration. Previous Stage 5J12 regression/Play Mode results remain historical evidence; they were not represented as fresh full-suite validation here.

The outstanding reset transaction prevents verification of all required resize/lifecycle scenarios. Hover/persistent rendering cleanup, complete consumer interactions, active water simulation, reload and final stamp-free scene operation remain acceptance checks after a safe correction is authorized and implemented.

Normal cancellation remains BarrierController.OnDisable's responsibility. Retirement should not introduce a synthetic replacement for stamp-disable cancellation. This future policy is unchanged: the stamp still performs its existing one-time exit on disable because it remains attached.

**Stage 5J4 was not started.** Retained:

- Both stamp component records **1046329326** and component-list entries on GameObject **1046329324**.
- `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs`.
- `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs.meta` and GUID **a99f55ae89eac4deca4c7d4c65418c4e**.
- Every other source, scene, prefab, asset, existing metadata file and historical report.

Only this report is added; Unity may generate its ordinary metadata. The task's startup correction, scene component removal and script/metadata deletion remain unfinished pending the reset-safety scope decision.

## Manual Play Mode checklist after the blocker is resolved

1. Use **PlayableTilemap only**. Verify N=5 before a fresh startup, loader growth to N=64, a 66×66 ready compatibility grid and valid distant placement without stamp configuration. Do not use an already-mutated in-memory map as the only cold-start check.
2. Verify map-ready-before-Awake and map-ready-after-Awake, invalid/unavailable data and temporary zone unavailability. Repeated same-size entry must preserve committed state and produce no extra completion/reset events.
3. Change dimensions with committed barriers in an unsaved fixture. Confirm compatibility/provider state, targeted live water barriers, committed outlines, HUD and reset notifications agree, while water/source state and other controllers' writes are preserved under the approved policy.
4. Recheck nonzero/negative origins, padded coordinates, previews, cancellation, four-barrier limit, Advisor notifications, Summary completion, Communication/Shelter/Evacuation/FloodImpact behavior, active WaterController simulation and dormant WaterSimulator provider compatibility. Check controller disable/re-enable and scene reload; do not initialize/resize during normal water steps.
5. Only after the automated and manual gates pass, repeat dependency/GUID/PPtr/UnityEvent checks, remove the two component records/list entries while preserving their GameObjects, then delete the legacy source/metadata. Compile the expected 202 runtime sources and validate both scene YAML files without repairing unrelated historical missing-script bindings.
6. Do not run or delete Dev_UINavigation, do not save test mutations, and do not retire the stamp while this reset gate remains blocked. Review the recorded blocker before approving the next implementation scope.
