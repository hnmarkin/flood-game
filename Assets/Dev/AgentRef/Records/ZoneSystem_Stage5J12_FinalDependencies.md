# ZoneSystem — Stage 5J1 + 5J2: Final stamp dependencies

Completed 2026-10-21. Summary now subscribes to BarrierController's authoritative global completion event. Communication no longer references the stamp, and both dormant WaterSimulator providers point to BarrierController, which now implements IBarrierProvider using unchanged getters. Stamp remains attached and unchanged. All 203 runtime sources compile with zero errors and the same 11 warnings. Temporary regressions pass 1,232 assertions; 78 comparable states match baseline, with approved event/fallback differences tested separately. Both scenes validate. A pre-existing cold-start grid-sizing hazard remains a blocker for retirement. Unity Play Mode was not run; prior Stage 5I3–I4 verification is user-reported.

## Preflight and fresh inventory

Read the Stage 5I12 and Stage 5I34 reports and inspected the current nine requested implementations. Verified paths:

| Responsibility | Source |
| --- | --- |
| Stamp | `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs` |
| BarrierController, BarrierPlacementState, BarrierPhysicsAdapter | `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/` |
| Summary | `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs` |
| Communication | `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs` |
| Legacy WaterSimulator | `Assets/Game/Features/LegacyWaterSystem/WaterSimulator.cs` |
| Provider interface | `Assets/Dev/Scripts/Flood/Interface/IBarrierProvider.cs` |
| ZoneController | `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneController.cs` |

Fresh source searches covered Assets/Packages; serialized searches covered Assets scenes, prefabs, assets and persistent UnityEvents. Findings were captured before editing with hashes of **6,199 starting Assets files**.

| Starting dependency | Migration |
| --- | --- |
| Summary static `FloodDefenseBoxStamp.OnAllZoneBarriersPlaced` subscription/unsubscription | Change the declaring owner to BarrierController; retain callback, guard and lifecycle sites. |
| Communication private stamp field, first-object discovery, `stamp.MapData` fallback and stamp-local ZoneController anchor | Remove these dependencies; retain explicit map/controller fields and independent existing fallbacks. |
| Both WaterSimulator components: serialized MonoBehaviour provider **1046329326** | Rebind to existing BarrierController **420333452**, after verifying interface compatibility. |
| Stamp Awake/OnEnable setup, relays and OnDisable exit | Preserve unchanged; audit independent ownership and retirement risks. |

Before editing, the stamp had three local references in each scene: Communication's field, WaterSimulator's provider and its owning GameObject component-list entry. Only the two authorized scenes contain these relevant script bindings; no relevant prefab binding or UnityEvent targeting the stamp was found. The inactive StartFloodBtn still targets WaterSimulator.BeginSimulationFromUI and is preserved.

ActionsPanelController can create a communication controller only if one is absent. Both saved scenes already contain an active, explicitly referenced CommunicationTowerController, so that creation branch does not depend on stamp recovery in the supported scene setup. If reached in an unconfigured setup, it now uses the approved independent Resources fallback. No other active consumer requiring the old stamp-backed map contract was found.

## Global completion contract and Summary migration

Added `public static event Action OnAllZoneBarriersPlaced` to BarrierController. Every controller can publish; enabled Summary subscribers receive completion from any participating controller. No sender/arguments, event manager, singleton, state cache or completion latch was added.

The existing successful-commit branch is a single publication point. Invalid/empty/duplicate zones, exhausted limits, unresolved boundaries, incompatible grids, rejected/partial water registration and failed state recording return before it. State recording and HUD/outline updates precede placement notifications. The completion branch is reached only when a newly committed placement reaches `ActualMaxZoneBarriers`.

The resulting sequence is:

1. Record committed state, update HUD, paint committed outline and show placement feedback.
2. Invoke the existing `ZoneBarrierPlaced(placed, required)` instance event.
3. Clear hover/preview.
4. At the maximum, show completion feedback and invoke the existing `AllZoneBarriersPlaced` instance event.
5. Invoke the new authoritative static completion event once.
6. Exit placement mode through the existing path, including its mode event and preview cleanup.

The existing instance event, consumers and relative position before mode exit are unchanged. Static publication is after all instance completion listeners return and before exit. Previously Summary ran inside the stamp relay's instance callback; it now runs immediately after the instance dispatch in the same synchronous completion phase. Raw before/after traces assert this precise ordering rather than claim identical positions within the instance invocation list.

Saved maximum remains **4**, required count **3**, height **40**, seepage **0.001**. Advisor can show 3/3 before the maximum. Summary still refreshes on the **fourth successful placement**, not the third.

Summary changes are exactly its two event-owner references. `OnAllZoneBarriersPlaced()` still calls `RefreshSummary("AllZoneBarriersPlaced")`; data calculations, formatting, action identifier, subscription guard, OnEnable subscription and OnDisable cleanup remain unchanged. Static subscriptions retain normal C# behavior across controllers/scenes; disabling one controller does not clear other subscribers. No publication occurs on subscription, enable/disable, failed placement or blocked re-entry. Existing placement-state reinitialization permits a later successful completion, without a new reset API or permanent latch.

**Approved normalization:** multiple redundant stamp relays can still raise their unchanged legacy static event multiple times, but Summary no longer subscribes to it. The authoritative event publishes **once per controller completion** regardless of stamp relay count. Tests demonstrate historical two-relay/two-refresh behavior becoming one authoritative publication/refresh. Legitimate completions from different controllers still each publish globally.

## Communication migration

Removed the private stamp field, its obsolete Inspector-documentation line, stamp discovery, MapData recovery and stamp-based ZoneController anchor. No other Communication code changed.

Both scenes retain explicit map GUID **7e81a30567a6c1b44ae537c0db92678b**, ZoneController **2070746514** and communication Tilemap **36957482**. Their existing values are authoritative; the separately assigned communication Tilemap remains preferred over terrain fallback.

Map resolution is now explicit `tileMapData` → existing `Resources.FindObjectsOfTypeAll<TileMapData>()` first eligible asset. The eligibility test and iteration order are unchanged. Removing the intervening stamp.MapData step **intentionally changes null-map recovery**. Resources is not constrained to the stamp's scene/map and can select a different asset. A regression test places another eligible map first and verifies this approved difference.

Zone resolution is now explicit controller → local component → unique active same-scene controller. Existing ambiguity handling is unchanged. Awake resolution, the 120-frame startup retry, readiness guards, generation, groups, coordinates, category priorities, coverage/statuses, activation, tooltips, panel interactions, visuals and CommunicationZones outline requests remain intact. No ZoneController.MapData API or new search loop was introduced; the removed stamp lookup reduces discovery work during retries. Eligible hover/update adds no component discovery.

## IBarrierProvider and dormant water migration

BarrierController now declares `: MonoBehaviour, IBarrierProvider`. Each existing implementation remains byte-identical:

| Interface signature | Existing authoritative behavior |
| --- | --- |
| `bool IsBlockedX(int x, int y)` | Read placement-state X compatibility grid. |
| `bool IsBlockedY(int x, int y)` | Read placement-state Y compatibility grid. |
| `float GetSeepageX(int x, int y)` | Read X seepage when blocked, otherwise zero. |
| `float GetSeepageY(int x, int y)` | Read Y seepage when blocked, otherwise zero. |
| `float GetBarrierHeightX(int x, int y)` | Read X height when blocked, otherwise zero. |
| `float GetBarrierHeightY(int x, int y)` | Read Y height when blocked, otherwise zero. |

The stamp previously delegated these exact calls to this same controller. No wrapper or new provider component is needed. Tests compare all six results across **1,014 coordinate/state cases**, including empty/committed grids, blocked/unblocked edges, out-of-bounds inputs and three world origins. Legacy padded coordinates remain `GridLine + (1,1)`; world/tilemap origins do not shift provider indexing. Existing blocked values remain height 40 and seepage 0.001.

Tests compile the actual unchanged WaterSimulator and invoke its actual Awake method with the new provider: the `as IBarrierProvider` cast succeeds and caches BarrierController. Changing the serialized reference afterward does not change the cached interface, confirming the need to configure it before Awake. This verifies C# behavior through a temporary engine seam, not Unity's native inactive-object lifecycle.

Both saved WaterSimulator GameObjects remain inactive, with enabled components, `startOnPlay: 0` and `showDebugFloodButton: 0`. The debug-button UnityEvent and all references from Advisor, ResourceBar and stamp remain unchanged. The active WaterController **1876854016**, its renderer/scenario/map setup and BarrierPhysicsAdapter references are byte-identical. No active simulation or legacy stepping logic changed.

## Exact source and scene changes

Modified source files:

- `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierController.cs`: add interface declaration, one static Action declaration and one invocation after the instance completion event.
- `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs`: change two static subscription owners.
- `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs`: remove the documented stamp field/setup branches.

Modified scenes: `Assets/Game/Content/Scenes/PlayableTilemap.unity` and `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity`. Each has exactly two changed records:

| Existing component | Exact serialized delta |
| --- | --- |
| CommunicationTowerController **810699005**, GUID **98e604977a2f4ef5ac4190852d01e7fa** | Remove only `floodDefenseBoxStamp: {fileID: 1046329326}`. |
| WaterSimulator **1818902683**, GUID **beaf81aa4aedd3645bc54e051c493715** | Change only `barrierProviderBehaviour` from **1046329326** to **420333452**. |

BarrierController **420333452**, GUID **9d52216790784401b854340a1b65e9eb**, already exists; its entire serialized record is unchanged. Stamp **1046329326**, owning GameObject **1046329324**, ZoneController **2070746514**, hover input **704415077**, visibility **704415078**, Summary and Advisor records are byte-identical. Zone renderer settings, all other fields, hierarchy and activation states are preserved. No objects/components were added or removed. All existing metadata, including stamp and controller GUIDs, remains unchanged. Game script/scene edits use this task's explicit authorization. Dev_UINavigation was not run or deleted.

## Startup and lifecycle independence audit

| Responsibility | Independent owner / finding |
| --- | --- |
| Required references | ZoneController and BarrierController have explicit current-scene map/terrain/loader references; BarrierController also has explicit camera/zone/water/toast/child references. Stamp assignments agree, so reference transfer supplies no unique configured resource. |
| Zone initialization/index | ZoneController owns the index, lazy readiness/build and renderer initialization in Awake/one bounded Start retry. Barrier mode waits for loader readiness and rebuilds zones. Stamp owns no independent zone cache. |
| Child configuration | BarrierController Awake resolves/configures its input, visuals and physics adapter; entry validates/configures them again. No second initializer was added. |
| Placement state/grids | BarrierController owns BarrierPlacementState and initializes its padded grid. Same-size initialization preserves committed state; resizing clears it. **Late sizing issue below remains.** |
| HUD/preview/physics | Controller owns progress updates, preview clearing, state recording and physics calls. Stamp's repeated HUD/child setup is redundant with correctly sized explicit setup. |
| Events | Controller owns instance notifications and now global completion independently. Legacy stamp relays have no remaining external project subscribers. |
| Controller disable/re-enable | Existing BarrierController OnDisable exits/cancels placement and clears preview; committed state remains. It has no OnEnable reset/publication. |
| Zone disable/re-enable/destruction | ZoneController has no OnEnable/OnDisable index-reset hook. Disabling/re-enabling does not rebuild or discard index state by itself; readiness remains explicit. OnDestroy owns renderer disposal. |
| Reload | A new controller starts with new placement state; Summary balances static subscriptions on disable/enable. Native reload/cold-start ordering remains a manual gate. |
| Stamp lifecycle | Awake/OnEnable still configure references/grids/children/HUD, bind legacy relays and force startup mode off. OnDisable still cancels the authoritative mode before detaching relays. Start/OnDestroy remain absent. No stamp lifecycle behavior changed. |

An independent fixture with correctly sized, explicit references initializes and places through actual BarrierController/ZoneSystem/physics-adapter code **without any stamp configuration**. This establishes controller ownership for ready data, not cold startup safety for the saved asset.

### Retirement blocker: early compatibility grid versus late loader size

Fresh source/asset inspection found:

- The shared map asset `Assets/Game/Features/LegacyWaterSystem/TileMapDataTest.asset` saves **N=5**; its padded grid dimensions are **7×7**.
- Both scenes use JsonMapLoader **1678446140** with execution order **100**, loading `Assets/StreamingAssets/tilemap_with_population_elev.json`, which contains **64 rows × 64 columns**. PaintFromJson changes the same asset's N to 64, requiring **66×66** compatibility grids.
- BarrierController/stamp default-order Awake can initialize the grid from N=5 before that loader runs. Required-reference/runtime validation initializes only an absent grid; it does not resize an existing one. Waiting for loader readiness/rebuilding the zone index does not resize the barrier grid.
- If the asset is still N=5 at startup, distant boundaries can be rejected against the stale 7×7 grid after loading. A later stamp configuration/OnEnable can repair dimensions because ConfigureReferencesFromLegacyAdapter calls InitializePlacementState. Normal early stamp startup does not guarantee such a late repair.

The temporary harness reproduces this **before and after this migration**: initialize at N=5, change to N=64, enter mode and attempt the same distant zone; the grid stays 7×7 and rejects it. Explicit late configuration resizes to 66×66 and the same zone succeeds. Native scene execution was not performed; an already-mutated in-memory map can hide this ordering hazard, so prior warm Play Mode results do not establish fresh-start safety.

This is a **pre-existing initialization hazard and Stage 5J3–J4 blocker**, not a regression introduced by the event/interface migration. It is not evidence that the stamp has correct automatic cold-start behavior. Its late configuration is simply an existing repair path that retirement must replace or make unnecessary.

Smallest recommended next correction: refresh/validate the authoritative compatibility-grid dimensions after loader readiness and before allowing placement, preserving same-size committed state. Validate the existing reset semantics when dimensions change. Do this in a separately authorized startup stage; no extra initialization, map-asset change, script-order change or grid repair was implemented here.

Stamp OnDisable is another remaining coupling to decide during retirement: disabling/removing the live stamp cancels a still-enabled controller once. Removing the stamp from a saved scene eliminates future stamp-disable transitions; test the intended controller-owned cancellation afterward. It is not a remaining external consumer reference.

## Compilation and regression results

- **Runtime compilation:** installed Unity 6000.0.63f1 Roslyn with current project references; before/after **203 sources, zero errors, 11 warnings**. Sorted diagnostic paths, locations and messages match exactly.
- **Event/provider/startup harness:** **1,086 baseline / 1,091 migrated assertions**, **23 comparable state/provider/startup snapshots match**. Uses actual before/after BarrierController/Summary and actual unchanged stamp, WaterSimulator, IBarrierProvider, placement state, physics adapter, ZoneSystem and related consumer/data code. Water writes, engine/input/drawing/UI/data-source services are supplied by temporary stand-ins.
- Covers successful placements 1–4, no completion at required-three, final publication, failed/partial/duplicate placement, blocked re-entry, static scope across controllers/scenes, Summary disable/re-enable and unload cleanup, existing state reinitialization, all six provider getters, actual Awake-time cast/cache, correct-size stamp-free initialization, lifecycle state preservation and the late-size hazard.
- Raw traces separately assert original nested-relay ordering and new **instance → authoritative global → mode exit** ordering. Approved two-relay normalization is asserted as two old broadcasts versus one new publication. Migrated-only checks verify Summary completion and the cached provider without a stamp.
- **Communication harness:** **140 baseline / 141 migrated assertions**, **55 comparable snapshots match**. Uses actual before/after Communication and actual ZoneSystem/renderer with temporary mitigation/input/UI/engine seams. Compares tower IDs, groups, cells/world positions, coverage/statuses, activation/events, sprite/color/visibility arguments and outlines across group sizes 1/2/3 and three origins. Covers unready data, 120-frame timeout, retry recovery, controller/map/terrain fallbacks and no discovery during eligible hover.
- The null-map test deliberately supplies a different first Resources candidate and checks stamp-map selection before versus independent Resources selection after. This approved difference is not included in parity snapshots. Stamp-free tower generation is also tested.
- **Combined:** **1,232 migrated assertions pass**, **78 comparable snapshots match**. Summary comparisons use deterministic field serialization excluding generated timestamps. Tests explicitly drive lifecycle/coroutine methods; they do not establish Unity-native JSON serialization, waits, input delivery, rendering, inactive-object Awake timing or active-water simulation execution. Source/scene identity checks support unchanged native paths; manual verification remains required.
- **YAML:** Ruby Psych parses **346 PlayableTilemap / 360 Dev_UINavigation records** before/after. Only the two intended records change in each. All **833 / 864 nonzero local PPtrs resolve**; IDs/component lists remain valid, with no new duplicates or components. The same **8 / 9 pre-existing unresolved external script bindings**, excluding Unity built-ins, remain in untouched records. No new unresolved binding was introduced; native import must assess those historical issues separately.
- **Scope:** exact source-transform checks prove no other code changes and all six getter bodies are identical. SHA-256 checks preserve every other starting asset and all existing metadata. No prefabs, UI assets, water/zone/state/physics scripts, settings or archived gameplay values changed; no files deleted. Only the five authorized files and this report are part of this stage, with ordinary report metadata allowed to be generated by Unity.

Evidence: `/private/tmp/zone-stage-5j12-nd_7u_pf`, including starting hashes/source/scene snapshots, pre-edit inventory/map, before/after compiler and test logs/executables, source diffs, parsed scenes and startup audit. **Unity Editor/Play Mode was not run for this stage.**

## Remaining stamp references and Stage 5J3–J4 readiness

Fresh post-migration C# searches find no external references to FloodDefenseBoxStamp or subscriptions to its legacy static event. Both scenes retain only its own component-list reference to **1046329326**, its own script binding/GUID and the intact component record. No external serialized consumer/UnityEvent or relevant prefab binding remains. The stamp still implements its legacy interface/forwarding APIs and performs setup/relay/disable work, but the migrated consumers no longer require it.

The external-dependency migrations are complete. **Do not retire the stamp yet:** first resolve the late-grid sizing gate and verify a fresh PlayableTilemap startup with the stamp absent. Preserve active controller configuration and decide the one-time stamp-disable cancellation explicitly. Then a separately authorized stage can remove only the stamp component records/list entries, verify zero GUID/PPtr/source consumers, and finally remove its script/metadata. No deletion is performed here.

## Manual Play Mode checklist — PlayableTilemap only

1. Import/compile. Confirm Summary uses BarrierController's static event in source; inspect Communication **810699005** with unchanged explicit map/zone/Tilemap and no stamp field. Inspect legacy WaterSimulator **1818902683** provider **420333452**, inactive root, enabled component and both debug/start flags false. Confirm the existing debug-button binding and active WaterController setup remain unchanged.
2. Check maximum 4, required 3, height 40, seepage 0.001 and unchanged preview/outline settings. Place through four zones: Advisor may show 3/3 on the third; one Summary `AllZoneBarriersPlaced` refresh occurs on the fourth. Check rejected/duplicate placement, limit re-entry and completion ordering before mode exit.
3. In an unsaved controlled fixture, test multiple controllers, Summary disable/re-enable/unload, duplicate stamp relays and legitimate existing state reinitialization. Expect one authoritative publication per controller completion; no duplicate Summary refresh solely from relays. Do not save extra objects/settings.
4. Compare communication towers, groups, world centers, coverage, colors/sprites, activation/tooltips/panels and outlines. Check retry/unready behavior. With serialized map absent in an unsaved test, expect the retained Resources policy, not guaranteed recovery of the former stamp map.
5. Test provider assignment **before Awake** in an unsaved legacy-water fixture; inspect cached IBarrierProvider and six getter values at blocked/unblocked/out-of-bounds padded coordinates. Do not enable or replace the saved dormant simulator. Reassigning the Inspector field after Awake does not update its cache.
6. Before approving retirement, reproduce fresh startup from the saved N=5 asset, inspect compatibility-grid dimensions before/after JSON load and attempt distant-zone placement with stamp absent. Verify the future authorized sizing correction produces 66×66 at readiness while preserving same-size state. Include controller/zone disable/re-enable and scene reload. Existing in-memory N=64 is not a substitute for this cold-start check.
7. Keep stamp attached for this stage. Do not save test mutations; do not run or delete Dev_UINavigation. Review and commit the migrations, then address the documented startup blocker before Stage 5J3–J4 removal.
