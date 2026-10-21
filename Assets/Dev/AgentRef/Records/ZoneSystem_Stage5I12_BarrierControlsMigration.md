# ZoneSystem — Stage 5I1 + 5I2: Barrier controls and UI coordination

Completed 2026-10-21. ShelterController, ActionsPanelController and PreparationCardsController now use the existing BarrierController for their barrier controls, state and mode notifications. Six serialized assignments across the two authorized scenes now reference component 420333452. Barrier configuration, gameplay ownership, public consumer APIs and other Inspector values are unchanged. All 203 runtime sources compile with zero errors and the same 11 warnings. Temporary regressions pass 108 assertions; all 29 baseline state/event snapshots match. Both scene YAML files validate. Unity Editor/Play Mode was not executed. The stamp remains for the dependencies listed below.

## Preflight and exact dependency map

Read the Stage 5A, Stage 5H23 and Stage 5H45 reports. Inspected all three target consumers, FloodDefenseBoxStamp, BarrierController, BarrierPlacementState, BarrierPhysicsAdapter, the BarrierSystem documentation, IBarrierProvider and ZoneOutlineVisibilityController. Searched the project for stamp and barrier-mode event consumers and located all serialized instances of the three target scripts. A dependency map was captured outside Assets before any edits.

Verified implementation paths:

- Stamp: `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs`.
- Barrier controller/state/physics: `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/`.
- Shelter: `Assets/Dev/Dev_Shelter/Scripts/ShelterController.cs`.
- Actions: `Assets/Dev/Scripts/PanelController/ActionsPanelController.cs`.
- Cards: `Assets/Dev/Scripts/Main_UI/UI_Controller_Scripts/PreparationCardsController.cs`.
- Visibility: `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneOutlineVisibilityController.cs`.

| Exact original dependency | Existing replacement | Behavior, effects and initialization |
| --- | --- | --- |
| Shelter `floodDefense.IsZoneBoundaryModeActive` | `barrierController.IsZoneBoundaryModeActive` | Pure boolean read. The stamp already delegates this getter to the authoritative controller; no events or state mutation. |
| Shelter `floodDefense.ExitBuildModeFromUI()` | `barrierController.ExitBuildModeFromUI()` | Full cancellation/cleanup chain verified below. Retains the original active-only guard and executes before shelter mode entry. |
| Actions `floodDefenseBoxStamp.EnablePlaceBarrierMode()` | `barrierController.EnablePlaceBarrierMode()` | Requests the same controller entry routine. Active/pending requests are ignored; successful entry changes mode and emits its event. |
| Actions `floodDefenseBoxStamp.ToggleBuildModeFromUI()` | `barrierController.ToggleBuildModeFromUI()` | Cancels active or pending entry; otherwise enables. Existing immediate `RefreshButtonStates()` remains after the request. |
| Actions `floodDefenseBoxStamp.IsZoneBoundaryModeActive` | `barrierController.IsZoneBoundaryModeActive` | Reads actual mode for button highlighting. No additional cache or action-availability calculation. |
| Actions `floodDefenseBoxStamp.ZoneBoundaryModeChanged +=/-= OnZoneBoundaryModeChanged` | Same subscription on `barrierController.ZoneBoundaryModeChanged` | Instance `Action<bool>`, synchronously delivered after authoritative state changes, before preview cleanup. Same subscription/unsubscription sites and duplicate-subscription guard. No added emissions. |
| Cards `floodDefenseBoxStamp.EnablePlaceBarrierMode()` | `barrierController.EnablePlaceBarrierMode()` | Same enable path. `OnCardClicked` still hides cards and emits any visibility change before requesting barrier mode. No barrier-event subscription exists in this controller. |
| All three serialized stamp fields and `FindFirstObjectByType<FloodDefenseBoxStamp>()`; Shelter's required-provider validation | Private serialized `BarrierController barrierController`, existing-site controller discovery, controller validation | All stamp uses in these consumers are covered. Explicit scene references are preferred. Missing-provider handling retains its original return behavior and now names BarrierController. No new searches, initialization hooks or search loops. |

### Exit chain and adapter side effects

Original shelter exit: stamp `ExitBuildModeFromUI` → stamp `DisablePlaceBarrierMode` → `EnsureBarrierControllerConfigured` → controller `DisablePlaceBarrierMode` → `ExitZoneBoundaryPlacementMode`.

Replacement: controller `ExitBuildModeFromUI` → controller `DisablePlaceBarrierMode` → the same `ExitZoneBoundaryPlacementMode`.

The common endpoint stops and clears a pending entry coroutine, changes the authoritative boolean to false, emits one mode event only if the boolean changed, then clears hover GEOID, hover tile-set reference and preview. It does not change selection, release a separate UI lock, erase committed barriers or perform additional compatibility cleanup. Shelter's own highlights, ghost, entry-frame suppression and placement events continue afterward in their original order.

The stamp's extra configuration step refreshes map/terrain/camera/loader/zone/water/toast references, configures child components, initializes or reuses the compatibility grid, refreshes HUD progress and ensures its relay subscriptions. Those are adapter setup effects, not a second exit implementation. In both scenes the stamp and controller reference the same map, terrain, camera, loader, zone and water components. BarrierController already initializes its state/children in Awake; entry validates/configures them again and initializes a missing grid. Direct calls preserve the authoritative scene setup and omit the redundant adapter refresh/HUD write. No archived stamp gameplay settings are copied. This stage does not redesign map replacement or legacy bootstrap behavior.

### Entry and event parity

Entry validates references, waits up to 30 frames for zone readiness, rebuilds the index, checks zone count and the existing placement limit, then activates mode. State is assigned before the instance mode event, and preview cleanup follows it. Repeated activation, unsuccessful entry and cancelling a still-pending inactive entry emit no spurious mode change.

The stamp subscribes to this same controller event and synchronously forwards the same bool after updating its archived mode field. Actions was the only external subscriber to the stamp's mode event found in the project. It now receives the authoritative event directly, in the same state-change/preview-cleanup phase. C# listeners retain ordinary subscription-order semantics; no new event or ordering mechanism was introduced. ZoneOutlineVisibilityController already polls BarrierController and remains unchanged.

Rejected or partially registered placements retain active mode and emit no success or mode event. A successful placement records state and paints its outline before `ZoneBarrierPlaced`. At the limit, the existing sequence is placement notification → preview clearing → all-placed notification → mode false notification → exit preview clearing. The stamp's placement/static relays remain intact for unmigrated consumers.

## Stage 5I1 — Shelter

Replaced the private stamp field, its resolver and required-provider diagnostic, plus the state read and exit receiver. The complete source equals its starting snapshot after only those type/name/message substitutions. No candidate, placement, metrics, capacity, input-guard, presentation or ZoneController code changed.

The original shelter guard exits barriers only when mode is already active. It does not cancel an entry that is merely pending; this behavior was retained and tested. The controller's exit API itself still cancels pending entry when called.

## Stage 5I2 — Actions and preparation cards

Actions now requests barrier activation/toggling, reads mode and subscribes/unsubscribes through BarrierController. OnEnable still resolves, binds, subscribes and refreshes; OnDisable still stops binding, unsubscribes and unbinds. Root injection, button callbacks, highlighting, communication/evacuation/shelter actions and navigation subscriptions are otherwise identical.

Cards now resolves BarrierController and requests its existing enable API for sandbag cards. Card availability/overrides, generated content, picking modes, labels, IDs/title aliases, visibility events and lifecycle cleanup are unchanged. Non-sandbag cards still only hide the card UI. Disabled cards still have no click callback. Cards does not currently implement a barrier-mode subscription, selected-card mode cache or separate cancellation action; none was added.

No public consumer methods changed. BarrierController remains the only owner of mode, placement state, limits, height, seepage and placement events. No new manager, state cache, event, cost calculation or mitigation behavior was introduced.

## Scene serialization and preserved configuration

Modified only `Assets/Game/Content/Scenes/PlayableTilemap.unity` and `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity`. The PlayableTilemap edit uses the task's explicit authorization for that Game scene.

| Scene / existing component | Exact assignment change |
| --- | --- |
| Both: ShelterController **1420635165** | `floodDefense: 1046329326` → `barrierController: 420333452` |
| Both: ActionsPanelController **1389987227** | `floodDefenseBoxStamp: 1046329326` → `barrierController: 420333452` |
| Dev_UINavigation only: PreparationCardsController **1819329177**, **2147339104** | Each `floodDefenseBoxStamp: 1046329326` → `barrierController: 420333452` |

Verified component 420333452 has BarrierController script GUID `9d52216790784401b854340a1b65e9eb`. There is no serialized PreparationCardsController instance in PlayableTilemap; its Actions preparation-cards reference remains zero. The two existing Dev_UINavigation card components and their GameObject active states are preserved. No other serialized instances of the three scripts were found in project scenes, prefabs or assets.

The entire BarrierController record remains byte-identical in both scenes: saved mode 1, maximum **4**, required count **3**, height **40**, seepage **0.001**, all reference assignments, preview/outline Tilemaps and tiles, colors, outline toggle and logging. Awake still forces actual runtime mode off. Archived stamp values remain untouched and are not authoritative.

Stamp **1046329326**, ZoneController **2070746514**, hover input **704415077** and visibility policy **704415078** also remain byte-identical. Existing script GUIDs, hierarchy, GameObjects, other components, materials, UI assets and Inspector values are unchanged. Dev_UINavigation received serialization edits only; it was not run or deleted.

## Validation

- **Compilation:** installed Unity 6000.0.63f1 Roslyn with the project's current response file and references. Before/after: **203 runtime sources, zero errors, 11 warnings**. Sorted diagnostic paths, locations and messages are identical; no new errors or warnings.
- **Temporary regression:** **104 baseline / 108 migrated assertions pass; all 29 state/event snapshots match.** Compiled actual before/after target sources with actual unchanged stamp, BarrierController, BarrierPlacementState, BarrierPhysicsAdapter, ZoneSystem, shelter resolver/state/placement/metrics and evacuation sources. Temporary stand-ins supply engine scheduling, input, drawing, water writes, other UI services and parsed card data. Tests explicitly drive coroutine ticks/lifecycle callbacks; they do not establish native Unity coroutine timing, input delivery, rendering, JSON import or scene execution.
- Covered inactive/active barrier-to-shelter transitions, exit-event ordering, hover-preview cleanup, candidate identity/associations, shelter capacity/placement, entry-frame click guard, shelter disable/re-enable and preserved pending-only guard.
- Covered button activation/toggle, Escape, repeated requests, pending/waiting cancellation, unready-map timeout, duplicate handler prevention, unsubscribe/re-enable refresh, switching other mitigation actions and scene-unload cleanup callbacks.
- Covered first-edge water rejection, partial registration failure, duplicate zones, four successful commits, configured height/seepage/required count, final placement/all-placed/mode-event ordering and blocked re-entry at the limit. Water behavior itself was supplied by the test seam; partial-write rollback remains outside this stage.
- Covered sandbag selection and aliases, other-card switching, hide-before-entry ordering, closing cards, disabled cards hidden/shown, no callback on unavailable cards and repeated card disable/re-enable. Added migrated-only checks prove actions, card activation, button synchronization and shelter takeover work with the stamp disabled or removed.
- **Source comparison:** all three sources are byte-identical to their pre-edit snapshots after the documented type/receiver/provider-name substitutions. All **109 existing method declarations** remain at their original sites. This also preserves all previous ZoneController query and gameplay calculations.
- **YAML:** Ruby Psych parses **346 PlayableTilemap / 360 Dev_UINavigation records** before/after. Only the two/four consumer records above differ; each difference is exactly the one reference key/target replacement. All **834 / 865 nonzero local PPtrs resolve**, object IDs are unique and component lists are valid without new duplicates. No objects or components were added or removed. Unrelated pre-existing external script bindings from earlier reports are untouched; this is not a claim that Unity import has cleared every historical missing-script issue.
- **Scope:** SHA-256 comparison against **6,187 starting Assets files** confirms only the three authorized sources and two scenes changed, plus this report. No deletions, existing metadata changes or unrelated asset changes. Unity may generate ordinary metadata for the new report. Existing unrelated working-tree changes were preserved.

Evidence: `/private/tmp/zone-stage-5i12-y5t5ypqz` contains starting snapshots/hashes, the pre-edit map, compiler logs, before/after test executables and logs, source diffs and parsed scene checks. **Unity Editor and Play Mode were not executed.**

## Remaining stamp dependencies and readiness

| Remaining responsibility | Follow-up |
| --- | --- |
| AdvisorPanelController: serialized stamp and instance `ZoneBarrierPlaced(int placed, int required)` subscription | Stage 5I3: audit lifecycle and placement-event relay parity before migration. Untouched here. |
| GameStateSummaryBuilder: static `FloodDefenseBoxStamp.OnAllZoneBarriersPlaced` subscription | Stage 5I4: explicitly preserve static/global versus per-controller event scope, arguments and timing. Untouched here. |
| CommunicationTowerController: placement-map asset fallback and initialization anchor | Retained from Stage 5H3; requires its separately scoped setup decision. |
| Stamp Awake/OnEnable reference configuration, event relays and OnDisable barrier exit | Remains present. Disabling the stamp still immediately exits an existing barrier mode; subsequent migrated requests work independently. Retirement must account for these lifecycle effects. |
| Legacy WaterSimulator's serialized MonoBehaviour provider and `IBarrierProvider` compatibility | Separate provider/setup migration. BarrierController does not implement that interface. |

No missing BarrierSystem API blocks this stage. All stamp dependencies in the three target consumers are removed. Stages 5I3–I4 can proceed with fresh event audits; the stamp is not ready for deletion. The existing PlayableTilemap card-controller wiring gap and native UI/rendering behavior remain manual integration checks, not changes introduced here.

## Manual Play Mode checklist — PlayableTilemap only

1. Import/compile. Inspect Shelter **1420635165** and Actions **1389987227**: Barrier Controller points to **420333452**, with no stamp field. Verify that component's map/terrain/camera/loader/zone/water and child-component assignments, maximum 4, required 3, height 40, seepage 0.001 and unchanged preview/outline settings. Confirm stamp and all ZoneSystem components remain attached.
2. Enter/toggle barrier mode from Actions repeatedly. Check highlight follows actual mode, hover preview appears, Escape/toggle clears it, cancelled pending entry does not later activate, failed placements keep mode and successful placements retain physics/outlines. Place through the fourth zone; verify progress, notifications, one final mode exit and no re-entry beyond the existing limit.
3. Enter shelter placement with barriers inactive and active. Verify barrier preview/Actions highlight clear before shelter mode, candidates/groups and ghost match prior visuals, entry click does not place immediately, later click places once, and capacity, mitigation, placed outlines and evacuation behavior remain correct. Check consumer disable/re-enable and UI-root reopen without duplicate callbacks.
4. For card interaction testing, first inspect runtime wiring: PlayableTilemap currently has no saved PreparationCardsController and Actions has a null cards-controller reference. If no runtime card controller is supplied, document that existing gap; do not save new wiring as part of this stage. With an unsaved runtime test fixture using the existing cards controller/template/data, verify sandbag activation, hide-before-entry, aliases, other cards, disabled cards, close/cancel behavior and repeated panel/controller reopen. Automated card-path checks above do not substitute for this native integration check.
5. In unsaved runtime tests, disable the stamp, then issue new Actions/card requests and enter shelter placement. Expect its existing OnDisable to cancel the currently active barrier mode once. Subsequent migrated control/highlight paths should work. Restore the stamp when checking Advisor/Summary notifications, which still depend on its relays.
6. Check temporary missing references/unready map, explicit provider restoration, scene exit/reload, event counts and Console output. Verify zone hover/persistent-outline visibility still follows barrier mode. Do not save test mutations. Do not run or delete Dev_UINavigation. Review and commit after manual verification.
