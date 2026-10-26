# ZoneSystem — Stage 5I3 + 5I4: Barrier events and retirement audit

Completed 2026-10-21. Advisor now receives BarrierController's placement event directly; both scenes reference the existing controller. Summary remains unchanged under the requested fallback rule: its global stamp event has no equivalent authoritative global API. The communication, startup/lifecycle and legacy-water audits are complete. Runtime compilation passes with zero errors and the same 11 warnings. Temporary regressions pass 117 assertions; all 21 baseline notification/summary snapshots match. Only Advisor source and its two scene assignments changed, plus this report. Stamp and active barrier configuration remain intact. Unity Play Mode was not executed; prior Stage 5I1–I2 visual verification is user-reported.

## 1. Preflight dependency map

Read the Stage 5A, Stage 5H23, Stage 5H45 and Stage 5I12 records. Inspected the current Advisor, Summary, stamp, BarrierController, BarrierPlacementState, BarrierPhysicsAdapter, CommunicationTowerController, WaterSimulator and IBarrierProvider implementations. Project C# searches cover Assets/Packages; serialized searches cover Assets scenes, prefabs, assets and other text assets, including script GUIDs, local object references and persistent UnityEvent calls. A dependency map and complete starting asset hashes were saved before editing.

| Component / exact source | Original dependency | Decision and evidence |
| --- | --- | --- |
| `Assets/Game/Features/UI/Scripts/UI_Advisor_Scripts/AdvisorPanelController.cs` | Private serialized `FloodDefenseBoxStamp floodDefense`; first-object discovery; guarded `ZoneBarrierPlaced +=/-= OnZoneBarrierPlaced` | Migrate all four responsibilities to `BarrierController barrierController`. Both instance events are `Action<int,int>` and the stamp forwards arguments synchronously without filtering. |
| `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs` | Static `FloodDefenseBoxStamp.OnAllZoneBarriersPlaced +=/-= OnAllZoneBarriersPlaced` | Keep unchanged. Global `Action` has no sender/arguments; any subscribed stamp can broadcast. Controller exposes only instance `Action AllZoneBarriersPlaced`. |
| `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs` | Serialized stamp, `FindFirstObjectByType<FloodDefenseBoxStamp>()`, `stamp.MapData`, `stamp.GetComponent<ZoneController>()` | Read-only audit. Retains missing-map recovery and an initialization anchor. Current explicit map/zone assignments make these fallbacks unnecessary for correctly wired saved scenes, but not for null references generally. |
| `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs` | Awake/OnEnable configuration, controller creation/discovery, mode/placement/static-event relays, disable-triggered exit and six interface getters | Read-only lifecycle audit below. Only the global relay still has an external stamp-event consumer after Advisor migration. |
| `Assets/Game/Features/LegacyWaterSystem/WaterSimulator.cs` | Serialized `MonoBehaviour barrierProviderBehaviour`, cached `as IBarrierProvider` in Awake | Read-only. Saved provider is the stamp in both scenes; root GameObject is inactive. Uses four of six interface methods. |
| `Assets/Dev/Scripts/Flood/Interface/IBarrierProvider.cs` | Six compatibility getters | Separate existing declaration; only the stamp implements it. BarrierController has matching methods but does not declare the interface. |

Barrier sources are under `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/`. The active water controller is `Assets/Dev/WaterSystemRefactor/Scripts/WaterController.cs`; zone ownership is `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneController.cs`.

No other external C# stamp controls, counts, zone queries or conversions were found after the previous migrations. No persistent UnityEvent targets the stamp. Serialized bindings for the stamp, Advisor, Summary, Communication, BarrierController and legacy WaterSimulator occur only in the two authorized scenes, with no prefab instances found. Historical markdown references are documentation, not active dependencies.

## 2. Stage 5I3 — Advisor migration

The successful placement chain is:

1. BarrierController validates the zone/boundary and compatibility grid, then registers water edges through BarrierPhysicsAdapter.
2. BarrierPlacementState records the committed zone and padded compatibility edges.
3. Controller updates HUD progress, paints the committed outline and shows its placement message.
4. Controller invokes `ZoneBarrierPlaced(BarriersPlaced, RequiredBarriersBeforeFlood)`.
5. Previously, stamp `OnBarrierPlaced` synchronously forwarded those exact arguments to Advisor. Advisor now subscribes to step 4 directly.
6. Controller clears the preview. At its maximum it invokes all-placed, then exits mode.

The Advisor callback still clamps placed to at least zero and required to at least one, chooses the existing text and starts the same sanitized popup/typewriter routine. No message text, random-variant selection, progress formatting, visibility, close behavior, timing calculation or other gameplay subscription changed.

The serialized provider, resolver, private subscription-guard name and event receiver are the complete source delta. Public APIs remain unchanged. OnEnable still resolves references, binds UI and subscribes; OnDisable still unsubscribes, removes the close callback, stops active routines and clears binding state. The original guard still prevents duplicate subscriptions. A missing provider is tolerated; explicit restoration followed by the existing lifecycle recovers the subscription.

Both source events are instance scoped and carry the same two integers. Failed validation, rejected/partial water registration and duplicate-zone placement do not reach either event. Final placement still notifies Advisor while barrier mode is active and after committed state/outline updates, before global completion and mode exit. Disabling/re-enabling BarrierController does not erase committed zones or replay placement notifications.

Direct subscription occupies Advisor's ordinary position in the controller's C# invocation list instead of nesting inside the stamp's relay. Project searches found no other external controller placement-event listener whose ordering would be displaced; the remaining stamp placement relay has no external subscribers after this migration. Notification timing relative to committed state, completion and mode exit is preserved. This does not promise priority over hypothetical future listeners registered in a different order.

Intentional authorized difference: Advisor continues receiving placements with the stamp disabled/absent. Stamp OnDisable still cancels the current barrier mode once; subsequent requests and placements use the authoritative system independently.

## 3. Stage 5I4 — Exact blocker; Summary unchanged

| Property | Stamp event | Existing authoritative event |
| --- | --- | --- |
| Declaration | `public static event Action OnAllZoneBarriersPlaced` | `public event Action AllZoneBarriersPlaced` |
| Arguments | None | None |
| Scope | All subscribed stamp instances, including different controllers/scenes | One BarrierController instance |
| Trigger | Each stamp's synchronous `OnAllBarriersPlaced` relay | Each successful commit that reaches the controller's actual maximum |
| Deduplication | One relay subscription per stamp/controller binding; no global once-only latch | No global latch; normal committed-state/limit guards prevent repeated placement |
| Cleanup | Summary unsubscribes globally in OnDisable; stamp detaches its source relay in OnDisable | Listener must unsubscribe from its selected instance |

Current scenes each contain one controller and one stamp. No code enforces one participating controller/stamp across all loaded scenes or lifetime. Summary has no barrier reference or source-selection rule: any global completion refreshes its own resolved source data with action `AllZoneBarriersPlaced`. A serialized reference to 420333452 would narrow that behavior. Scanning only current controllers would additionally need a creation/destruction/additive-scene policy and is outside this stage.

The static event fires at **actual maximum**, not merely required count. Saved maximum is **4**, required count **3**: Advisor displays completion-style wording at 3/3, but Summary's barrier refresh occurs on the fourth successful placement. Both behaviors are unchanged. Re-entry at the maximum and controller disable/re-enable emit nothing further. There is no public controller reset API; compatibility-grid dimension changes clear placement state, allowing completion to fire again. Two distinct stamp relays attached to one controller can each broadcast its one completion; the global event does not suppress that multiplicity.

The temporary harness demonstrates that a different controller's completion refreshes Summary while a listener on the first controller receives nothing. It also demonstrates two broadcasts from two distinct relays. Consequently, **GameStateSummaryBuilder.cs and its scene records remain byte-identical**. No aggregator, event, reference or alternate callback was introduced. This is the request's explicit permitted fallback, not a completed Summary migration.

**Smallest future scope extension:** approve an authoritative global, argument-free completion contract in BarrierSystem and migrate Summary's two subscription sites to it. A static event on BarrierController published in the existing completion phase is the smallest source option; an event service would require more lifecycle machinery. Before implementation, specify global participation and whether emission is once per controller completion or must preserve per-stamp relay multiplicity. The former matches the current one-stamp-per-controller scenes but changes the demonstrated multiple-relay case. Do not label that change exact without an approved runtime invariant/compatibility policy. No such extension is implemented here.

## 4. Source and scene changes

Modified exactly:

- `Assets/Game/Features/UI/Scripts/UI_Advisor_Scripts/AdvisorPanelController.cs`.
- `Assets/Game/Content/Scenes/PlayableTilemap.unity`.
- `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity`.

Both scene edits affect only Advisor component **515287497**, GameObject **515287495**, script GUID **aa1334e4ffac427594386370898b6a7e**:

`floodDefense: {fileID: 1046329326}` → `barrierController: {fileID: 420333452}`.

Verified BarrierController **420333452**, script GUID **9d52216790784401b854340a1b65e9eb**, already exists and is active. Summary **2019949261**, stamp **1046329326**, Communication **810699005**, legacy WaterSimulator **1818902683**, ZoneController **2070746514**, hover input **704415077** and visibility **704415078** are unchanged. No new GameObjects/components or activation changes.

All unrelated Advisor references and Inspector settings remain identical, including popup delay **1**, character interval **0.07**, auto-hide **0**, maximum characters **220**, varied messages, unscaled time and gameplay-event subscription settings. The full BarrierController serialized record is byte-identical: maximum **4**, required **3**, height **40**, seepage **0.001**, mode setting, all map/child/water references and preview/outline settings. Archived stamp values were not copied. Game source/scene edits use this task's explicit authorization. Dev_UINavigation was not run or deleted.

## 5. Compilation and automated regression

- Installed Unity **6000.0.63f1** Roslyn with the current project response file/references: before/after **203 runtime sources, zero errors, 11 warnings**, identical diagnostic paths, locations and messages.
- Temporary harness: **111 baseline / 117 migrated assertions pass; all 21 captured notification/summary states match**. Uses actual before/after Advisor, actual unchanged Summary and its data declarations, formatter, stamp, BarrierController, placement state, physics adapter, ZoneSystem and shelter/evacuation sources. Engine scheduling, drawing/input, water writes, risk/flood/communication data sources and UI services use stand-ins.
- Tests cover no early callback; rejected/partial/unknown/duplicate placement; one notification per success; placed/required arguments; message/progress text; callback after state/outline updates and before completion/exit; required-three versus maximum-four timing; rejected re-entry; controller disable/re-enable; consumer unsubscribe/re-enable; close-button cleanup; missing/restored provider; and unload cleanup callbacks.
- Summary tests cover one ordinary final-placement refresh, no failed/required-only refresh, correct callback action and populated summary sections, unsubscribe/re-enable, another controller/scene's global completion, multiple stamp relays and repeated completion after existing grid reinitialization. They verify retained behavior, not a migrated Summary event.
- Migrated-only tests verify Advisor text/events with the stamp disabled and removed. Summary intentionally remains dependent on the unchanged stamp relay.
- Summary comparisons use deterministic field serialization excluding the generated timestamp; Unity's native JsonUtility, coroutine waits, UI rendering and input delivery were not executed. Advisor's message/timing bodies and Summary's entire source are unchanged. Manual checks remain necessary for native popup timing and scene behavior.
- Ruby Psych parses **346 / 360** scene records. Only Advisor's record changes, with exactly the reference replacement above. All **834 / 865 nonzero local PPtrs resolve**; component/object IDs and component lists remain valid with no additions or new duplicates. The new target resolves to the existing BarrierController and its script GUID.
- GUID inventory still finds the same **8 / 9 pre-existing unresolved external script bindings**, excluding Unity built-ins, in untouched records. Thus no new unresolved binding is introduced, but this audit does not claim that every historical external script binding resolves. Unity import must assess those existing issues separately.
- SHA-256 comparison against **6,197 starting Assets files** verifies the three authorized modifications and this report only, with no deletions or changes to existing metadata/unrelated assets. Other working-tree changes were preserved. Ordinary metadata may be generated by Unity for this report.

Evidence is outside Assets at `/private/tmp/zone-stage-5i34-wjij04vj`: pre-edit map, hashes/source snapshots, serialized inventory, compiler logs, before/after test executables/logs and parsed scene checks. **Unity Editor/Play Mode was not run for this stage.**

## 6. Read-only communication placement-map audit

`ResolveReferences` selects map data in this order: existing serialized `tileMapData` → assigned/discovered stamp's `MapData` → first loaded TileMapData with positive N or sizeX from `Resources.FindObjectsOfTypeAll`. The Resources fallback is not constrained to a particular scene/map and does not guarantee the stamp's asset. Removing only the stamp branch can select a different map when the field is null.

Awake calls reference resolution, then zone resolution: explicit zone → stamp's local ZoneController → this component's local ZoneController → unique active same-scene controller. Ambiguity leaves the reference null and warns. The global stamp discovery used before that anchor is broader than the final same-scene search. Start retries tower generation for up to **120 frames**; generation retries ResolveReferences and relies on existing validation/lazy zone readiness. Zone discovery itself is the existing initialization lookup, not a new per-frame search.

Both scenes explicitly assign Communication **810699005** to map GUID **7e81a30567a6c1b44ae537c0db92678b**, ZoneController **2070746514** and communication Tilemap **36957482**. The map matches the stamp/controller map. Those assignments are sufficient to remove this consumer's stamp field/discovery/anchor for the current saved scenes in a future authorized stage. No new provider API is needed for that explicitly wired contract.

If preserving automatic null-map recovery is required, separately define an authoritative map-provider/setup contract before removing the fallback. JsonMapLoader and BarrierController keep their map fields private; ZoneController has no public MapData getter. Do not add one merely to replace an optional fallback. A future explicit-reference-only decision must document its null-field behavior rather than claim the Resources fallback is equivalent. Communication source/configuration was not changed.

## 7. Read-only stamp startup/lifecycle audit

Categories: **1** BarrierController already owns it; **2** ZoneController already owns it; **3** still required by a consumer; **4** redundant with current explicit wiring; **5** legacy compatibility; **6** separate migration/decision required. Categories overlap where appropriate.

| Stamp responsibility | Classification / retirement implication |
| --- | --- |
| Awake camera/toast discovery | **1,4**: BarrierController independently resolves Camera.main, toast singleton/first object and water. Current explicit references already agree. |
| EnsureZoneControllerConfigured: assigned → local → global first → runtime AddComponent, then Configure | **2,4,6**: zone index/geometry/configuration belong to ZoneController; its serialized sources and BarrierController's entry configuration cover current wiring. Do not silently retain unrestricted cross-scene fallback or runtime creation as an assumed requirement. |
| Resolve/create BarrierController and call ConfigureReferencesFromLegacyAdapter | **1,4,6**: current controller is explicit. Missing-controller runtime creation is a legacy bootstrap fallback, not independent controller ownership. Validate configured scenes before retiring it. |
| Reference transfer, child input/visual/physics configuration | **1,4**: controller Awake independently resolves/configures its children. Adapter transfers references, not gameplay settings. |
| Compatibility-grid initialization | **1,5,6**: BarrierPlacementState owns grids and committed GEOIDs. Same grid size preserves state; changed dimensions clear it. Controller Awake/entry can initialize; stamp configuration also refreshes dimensions. Preserve late-loader/map-size behavior during future removal. |
| HUD progress refresh | **1,4**: controller updates at Awake, adapter configuration, entry and successful commit. Current repeated stamp writes are redundant; removal must leave the authoritative updates intact. |
| Awake forces mode false and clears through controller | **1,4**: controller Awake already forces false and clears preview. Default-order component startup can differ; Loader metadata has order **100**, so test cold readiness after retirement. |
| Hidden archived fields and PreserveLegacySerializedConfiguration | **5**: preservation reads, not active settings. Archived auto-start-flood setting is not acted on. No current stamp call starts legacy flood simulation. |
| OnEnable configuration and relay binding | **3,4,5,6**: repeat setup is redundant under current wiring; instance mode/placement relays now have no external stamp subscribers, but static completion remains required by Summary. |
| Relay deduplication/cleanup | **3,5**: stored actual-source token avoids duplicate binding and detaches the three source events on disable. This is per stamp, not global completion deduplication. |
| OnDisable exits mode, stops pending entry, clears preview, then detaches relays and clears archived mode | **1,6**: controller independently exits when it is disabled. Disabling the separate stamp GameObject also exits a still-enabled controller today; removing the stamp changes that coupling. A live removal can produce one last exit. |
| Start / OnDestroy | Absent. Cleanup relies on OnDisable. The static event has no explicit reset/clear API; each subscriber owns unsubscription. |
| Six IBarrierProvider forwarding getters | **3,5,6**: retained dormant WaterSimulator binding still needs a compatible provider even though its saved GameObject is inactive. |

ZoneController owns serialized map sources, index lifecycle and renderer lifetime independently. Same-reference Configure does not invalidate mutations to existing loader data; explicit rebuild/invalidation remains its existing contract. Retirement must not introduce a second cache or treat removal of the stamp as an implicit map-reload notification.

## 8. Read-only WaterSimulator / IBarrierProvider audit

Both scenes retain legacy WaterSimulator **1818902683**, GameObject **1818902682** (`WaterSimulator`), inactive at the root, with enabled component, `startOnPlay: 0`, `showDebugFloodButton: 0` and `barrierProviderBehaviour: 1046329326`. No prefab provider assignment was found.

The saved active path instead uses WaterController **1876854016** on `WaterSystem/WaterSystemRoot`, with initialization and start-on-play enabled. BarrierPhysicsAdapter writes its edge barriers to that controller, and FloodImpactController subscribes to its reset/step events. This is evidence from source/saved configuration, not a new observed Play Mode trace.

There is also an existing persistent UnityEvent: inactive `StartFloodBtn` GameObject **841802068**, Button **841802070**, targets WaterSimulator **1818902683** → `BeginSimulationFromUI`. The method returns immediately while its saved debug-button flag is false. Advisor and ResourceBar retain legacy-water references, and the stamp retains its hidden reference. None of these references was edited. No external call or persistent binding to `BeginSimulationFromBarrierTrigger` was found. Do not equate dormant water with safe script/component deletion.

WaterSimulator casts its provider once in Awake. StepSimulation calls **IsBlockedX, IsBlockedY, GetSeepageX, GetSeepageY**; **GetBarrierHeightX/Y** are in the interface but unused by this simulator. Coordinates are the legacy padded grid coordinates, not normalized zone tiles or absolute WaterController cells. Changing the Inspector reference after Awake does not refresh the cached interface.

BarrierController already exposes all six matching public getters, but implements no IBarrierProvider interface. BarrierPlacementState is a plain state class; BarrierPhysicsAdapter is a writer and is not a compatible MonoBehaviour provider. The stamp itself is the only existing interface adapter; no separate replacement provider exists in the searched code.

**Smallest future provider migration:** authorize BarrierController to declare `IBarrierProvider` using its existing six methods, then change the two dormant WaterSimulator provider assignments to **420333452** before Awake. This requires a source change and two scene edits; scene reassignment alone would fail the interface cast. It avoids new gameplay logic/components. If the architecture must keep the legacy interface out of BarrierController, the alternative is a small Dev-owned forwarding MonoBehaviour implementing the interface, with its own metadata/component and explicit controller reference. Choose that scope separately. Keep WaterSimulator inactive and its other settings unchanged; test before/after provider values, cached binding and padded-coordinate behavior before retiring the stamp.

## 9. Remaining dependencies and proposed Stage 5J sequence

After this stage, direct external stamp C# dependencies are Summary's **active static completion subscription** and Communication's **map fallback/discovery/zone-resolution anchor**. Its serialized map/zone assignments are correct today, but fallback semantics still exist. The dormant WaterSimulator interface provider and stamp's own bootstrap/global relay/disable coupling also remain. Each scene has only the Communication field, Water provider and GameObject component-list entry pointing to stamp fileID 1046329326. Advisor has no remaining stamp dependency.

Proposed sequence; **none of the following is implemented or authorized by this stage**:

1. Approve the global completion contract/runtime invariant, add its authoritative API and migrate Summary while preserving its callback/action and balanced subscriptions. Test multiple controllers/scenes, duplicate-relay policy, resets, rejected/final placement and unload. Keep the old relay until consumers are migrated.
2. Migrate Communication under an explicit map/zone contract, or approve a separate map-provider contract if null recovery must remain automatic. Preserve Resources/ambiguity/readiness behavior where required, and document approved differences. Remove its stamp assignments only after its source no longer requires them.
3. Implement the selected compatible legacy provider and rebind both dormant WaterSimulator records before Awake. Verify all six getters and unchanged activation. Do not replace the provider with a MonoBehaviour that cannot satisfy the interface.
4. Verify controller/zone setup with the stamp absent in an unsaved PlayableTilemap runtime fixture: cold startup, late loader sizing, all serialized references/children, grids/HUD, four-barrier limit, water registration, previews/outlines, zone consumers, event ordering, disable/re-enable and scene reload. Decide the obsolete stamp-disable coupling explicitly. Preserve active controller settings; never seed them from archived stamp values.
5. In a later explicitly authorized retirement change, remove only the stamp component records/list entries after project-wide C#/GUID/PPtr/UnityEvent checks show no remaining consumers. Preserve GameObjects, other components and Zone/Barrier identities. Delete the script and its GUID-bearing metadata only after serialized references are gone, then compile/import and perform PlayableTilemap regression. No shared declaration must be extracted from the stamp file; IBarrierProvider already has its own file.

Summary's global contract is the current migration blocker. Communication null-map policy, legacy-provider choice and stamp-only setup/disable effects are the remaining retirement decisions. Stamp is **not ready for removal**.

## 10. Manual Unity checks — PlayableTilemap only

1. Import/compile; inspect Advisor **515287497** → BarrierController **420333452**. Confirm all popup settings, controller values/references, Summary/stamp and ZoneSystem identities match the preserved values above. Assess the existing external missing-script bindings separately; do not save unrelated cleanup here.
2. Place valid barriers and check exactly one Advisor notification per success with placed/required counts. Check no notification for invalid, duplicate, rejected or partial placement. Verify existing varied text, one-second delay, typewriter interval, close button and panel transitions. At 3/3 verify Advisor wording; on the fourth barrier verify one Summary `AllZoneBarriersPlaced` refresh before mode exits.
3. Disable/re-enable Advisor and Summary between placements; ensure no callbacks while disabled or duplicate notifications after re-enable. Disable/re-enable BarrierController and check no placement replay/reset. Test scene exit/reload and Console subscription behavior.
4. In an unsaved runtime test, disable the stamp, expect its existing one-time mode cancellation, then re-enter and place. Advisor should still notify; Summary still requires the retained relay and is not expected to become stamp-independent in this stage. Restore the stamp for normal completion tests.
5. Use an unsaved controlled fixture for multiple controllers/scenes to confirm retained global Summary participation and lifecycle cleanup. Do not substitute an instance event or alter the duplicate-relay contract during this verification.
6. Inspect Communication's explicit map/zone references and dormant WaterSimulator's provider/debug flags/button binding without changing them. Exercise cold map readiness and ordinary gameplay through the active WaterController. Do not run or delete Dev_UINavigation. Do not save test mutations. Review and commit this stage after manual verification.
