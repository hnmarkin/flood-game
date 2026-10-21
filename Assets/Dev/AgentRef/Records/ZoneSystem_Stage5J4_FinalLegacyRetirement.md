# ZoneSystem — Stage 5J4: Final legacy retirement

**Final status: completed.** Retired FloodDefenseBoxStamp after fresh dependency and independent-startup gates passed. Removed only component 1046329326 and its component-list entry from each authorized scene, then deleted the verified script/metadata. All retained production source, startup safety, Inspector settings and unrelated assets are unchanged. Runtime compilation passes with 202 sources, zero errors and the same 11 warnings. Temporary regression suites pass 14,781 assertions; 425 captured states match their baselines. Unity Play Mode was not run by Codex. The user reports prior gameplay visual verification; native post-retirement checks are listed below.

## Preflight and gate evidence

Completed 2026-10-21. Read the Stage 5J12, Stage 5J34 and Stage 5J3A reports. Revalidated the first two reports' hashes against the previously read versions; read the current Stage 5J3A report. Inspected the current stamp, BarrierController, BarrierPlacementState, ZoneController, JsonMapLoader, GameStateSummaryBuilder, CommunicationTowerController, WaterSimulator and IBarrierProvider.

Recorded fresh hashes of **6,245 starting Assets files**, Git status, source/scene snapshots, source lists and parsed scene inventories before mutation. The workspace already contains earlier migrations and unrelated user changes; comparisons use this fresh baseline rather than Git HEAD.

The stamp declared only `FloodDefenseBoxStamp : MonoBehaviour, IBarrierProvider`; it owned no separately shared data type. Its .meta GUID was verified as **a99f55ae89eac4deca4c7d4c65418c4e**. All map, terrain, camera, loader, zone and water assignments it forwarded match the BarrierController's existing explicit references in both scenes. Required child/preview/outline references are already assigned. ZoneController has the same explicit map/terrain/loader references. No unique startup resource needs transferring.

Before editing, the independent regression executable compiled production controller/state code **without compiling or constructing any stamp**. It passed **733 assertions**. The cold test starts actual BarrierController.Awake with N=5 and a 7×7 provisional grid, waits with loader data unavailable, supplies the 64×64 map, then verifies 66×66 dimensions before active placement. ZoneController builds the index; a distant valid zone at normalized tiles 60–61 commits successfully at three origins: (0,0), (11,5), (-20,-10). Actual physics-adapter registration, water-barrier data, HUD, committed tiles and one placement/Advisor notification are checked. Entry and final-commit mismatch rejection preserve committed state.

The gate also covers data ready before initialization, late readiness, repeated same-size entry, controller disable/re-enable, pending cancellation, missing/unready references, timeout/recovery and unexpected dimensions after one or three commits. These tests directly execute the Stage 5J3A implementation; no replacement initializer or runtime map-reset feature was added. Both mandatory gates passed before any scene or script removal.

## Fresh dependency audit

Searches covered Assets, Packages and ProjectSettings, including C# discovery/creation/type use, serialized fields/assets, scene/prefab script bindings, persistent UnityEvents and local scene PPtrs. Historical documentation was distinguished from runtime/serialized consumers.

| Dependency | Before | After |
| --- | --- | --- |
| External C# stamp type references, fields, discovery/creation, calls or event subscriptions | 0 | 0 |
| Stamp source declarations | 1 MonoBehaviour only | 0 |
| Stamp script GUID bindings | Two scene records and its .meta | 0 outside historical documentation |
| Local PPtrs to stamp 1046329326 | Its own component-list entry in each scene only | 0 |
| Prefab/asset/persistent UnityEvent consumers of stamp | 0 | 0 |
| IBarrierProvider | Independent interface; BarrierController, stamp and WaterSimulator references | Independent interface; BarrierController implementation and WaterSimulator consumer retained |

After scene cleanup, a fresh search found only the script and its verified .meta; those two files were then deleted. A final byte/string search over all files in Assets/Packages/ProjectSettings finds **zero non-documentation stamp name/GUID occurrences and zero serialized local stamp references**. The 21 historical documentation files mentioning the stamp are preserved, along with this retirement report. No unexpected external dependency was found.

## Exact scene removals and retained configuration

| Scene | YAML records before → after | Removed | Retained owner |
| --- | --- | --- | --- |
| `Assets/Game/Content/Scenes/PlayableTilemap.unity` | 346 → 345 | MonoBehaviour 1046329326 and its one component-list entry | GameObject 1046329324, named FloodDefenseManager |
| `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity` | 360 → 359 | Same verified component identity/list entry | Same verified GameObject identity/name |

Each owner retains Transform **1046329325** and unrelated component **1046329327**. No GameObject, hierarchy entry or other component was removed, added, moved or recreated. Every retained YAML record is byte-identical except the owner's component list, whose sole delta is removal of 1046329326. Each whole scene matches the expected exact text transformation.

Preserved unchanged: BarrierController **420333452**, ZoneController **2070746514**, ZoneHoverInputController **704415077**, ZoneOutlineVisibilityController **704415078**, all map/loader/water/camera/child/UI/Tilemap/tile/material references, colors, renderer settings, logging values, activation states, fileIDs and retained script GUIDs. BarrierController retains **maximum 4, required 3, height 40, seepage 0.001**. No archived stamp value was copied.

Dormant WaterSimulator **1818902683** still references provider **420333452**. Its root remains inactive, its component enabled, and startOnPlay/showDebugFloodButton false. Its UI event binding is unchanged. The active WaterController **1876854016**, its complete record and source are unchanged. Dev_UINavigation received serialized cleanup only; it was never run or deleted.

Deleted exactly:

- `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs`.
- Its `.cs.meta`, verified to own GUID **a99f55ae89eac4deca4c7d4c65418c4e**.

The containing directory, IBarrierProvider and every retained production source/metadata file remain. BarrierController's now-unused legacy configuration API remains intact because no production source cleanup was authorized.

## Compilation and automated regressions

Unity 6000.0.63f1 Roslyn with the project's generated runtime references compiled **203 sources before / 202 after**, with **zero errors and 11 warnings in both**. Sorted diagnostic paths, locations and messages match exactly. The post-retirement temporary response file removes only the deleted stamp source from the fresh project response file; no project/build configuration was edited. IBarrierProvider implementation and all communication, shelter, evacuation, Advisor, Summary, flood and water consumers compile.

| Temporary suite | Passing assertions | Comparison/evidence |
| --- | ---: | --- |
| Independent startup, mismatch, normal placement/lifecycle, Advisor/Summary | 733 | Complete output identical before/after retirement; 13 normal-gameplay snapshots also exactly match Stage 5J3A. |
| Provider, preview and fresh controller lifetime | 78 | Actual dormant WaterSimulator.Awake caches BarrierController as IBarrierProvider; six provider getters, preview/commit clearing, old-consumer unsubscription and fresh independent startup/completion pass at three origins. |
| Communication generation/activation/coverage | 141 | 55 states match Stage 5J12, including group sizes, world coordinates, coverage/statuses, shelter avoidance, hover/outlines, retry and fallback behavior. |
| Shelter placement/capacity and evacuation | 443 | 142 states match Stage 5H45; complete output identical, including candidate contexts, capacity/mitigation, placement events and route selection/confirmation behavior. |
| Zone hover, persistent channels, visibility and renderer lifecycle | 13,070 | 164 traces match Stage 5FG across 16 visibility/barrier configurations; resource fallback, reuse, channel isolation and disposal pass. |
| Flood-impact calculations/events/coordinates | 280 | 51 states match Stage 5H1, including risk/damage/population, mitigation, readiness, water sample coordinates and subscription lifecycle. |
| Live flood overlay | 36 | Actual overlay/impact/ZoneController activation through the button callback, paired labels, water-step-driven refresh, label limit, LiveFloodRisk channel isolation, toggle/disable cleanup, re-enable and renderer disposal pass at three origins. |
| **Total** | **14,781** | **425 comparable states match** across the five comparison suites. |

Stage 5J3A's stamp-specific assertions are omitted from the independent suite because there is no stamp to configure or re-enable. Its supported startup/placement/state-preservation cases remain. The 733-assertion suite was run before deletion with the stamp source already excluded, then recompiled/repeated after deletion. Other retained source hashes equal the fresh Stage 5J3A-era starting state; the unchanged consumer suites also match their earlier captured baselines. Newly added provider/preview/overlay checks make explicit assertions rather than claim historical parity.

Placement order remains **placement/Advisor → instance completion → global completion/Summary → mode exit**. HUD and committed visuals precede placement callbacks; required-three does not trigger completion; placement four triggers exactly one Summary refresh. Re-entry at the maximum, rejected/partial/duplicate placement, cancellation and mismatches do not emit false success/completion events. Same-size calls preserve six array identities/provider values, installed water-barrier values/write counts, committed tiles, HUD and notifications. Mismatch diagnostics are expected only in deliberately invalid fixtures.

These are managed C# harnesses with temporary Unity/input/Tilemap/UI/TMP/loader/data-service seams. Core tests use actual BarrierController/state/physics adapter/visuals/ZoneSystem/Advisor/Summary and WaterPhysicsBarrier; a WaterController facade forwards writes into the actual water-barrier implementation. Consumer suites use their actual consumer code and actual ZoneSystem, with supporting stand-ins; the renderer suite drives a barrier-mode stand-in. The overlay suite runs the actual overlay and impact implementations with a baseline-inspection stand-in. No saved test fixtures or test source files were added to Assets.

**Unity Play Mode was not executed by Codex.** Native JsonMapLoader execution order/JSON import, Unity serialization/rendering/input delivery, full active WaterController simulation and actual scene reload are not established by these seams. The fresh-lifetime check exercises managed initialization and disabled-consumer cleanup, not native scene loading. The user reports prior gameplay visual verification; this is separate from the automated retirement evidence.

## YAML, scope and hash inventory

Ruby Psych parses both scenes before/after. All nonzero local PPtrs resolve: **833 → 822** in PlayableTilemap and **864 → 853** in Dev_UINavigation. The reduction is the removed component record's references plus its list entry. There are no dangling references to 1046329326, no duplicate IDs/component-list entries, no new records/components and no new unresolved script binding. All retained component/GUID assignments are identical.

The same **8 / 9 pre-existing unrelated unresolved external script bindings** remain, after resolving project/package metadata and excluding Unity built-ins. Their exact component/GUID identities are stored in scene-validation.json. This includes unrelated retained component 1046329327 on the preserved owner; it was not the stamp and was not removed or repaired.

Fresh SHA-256 checks confirm only the two authorized existing scene files changed and only the two authorized stamp files were deleted. All other starting Assets files, every retained C# source, every retained .meta, prefabs, map/JSON/UI assets and historical reports are unchanged. This report is the only new deliverable; Unity may generate its ordinary metadata. Project settings were not changed.

| Key file | Before SHA-256 | After SHA-256 |
| --- | --- | --- |
| `Assets/Game/Content/Scenes/PlayableTilemap.unity` | `5e945138c9e693f390f46e2e481a88af6ce52abf2f53b1cbbac5b6a1fedaa49a` | `f51e9176b9c3ef5ad12664769851b856109dd54ecc8233ec560a050eed84a3b7` |
| `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity` | `bc48280c070ca863c07b8f2269d487a0c045976ab51b97354cce617df978bcc0` | `25f1d90a301c1c36caabeb4b3687dc888328478bc96334316db2eb7bbf27f42b` |
| `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs` | `6d51e0328e1e658966192223a57c62989938cd401efa286f17532106d4d307fb` | Deleted |
| `Assets/Dev/Scripts/Flood/FloodDefenseBoxStamp.cs.meta` | `0a21d20b47228820762f136f0761cd3e684cc9ed4730806cc54fec10d57e43e4` | Deleted |
| `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierController.cs` | `176a7f338ed089935feadf8cc1b15bd6bf42f30492c427c6c6fa174b24f9ec8e` | `176a7f338ed089935feadf8cc1b15bd6bf42f30492c427c6c6fa174b24f9ec8e` |
| `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierPlacementState.cs` | `6159fc045b4004adb6b01bef51cdcb77cc3eddeb08492b6e61a22e8d2cc3e194` | `6159fc045b4004adb6b01bef51cdcb77cc3eddeb08492b6e61a22e8d2cc3e194` |

Evidence is outside Assets at `/private/tmp/zone-stage-5j4-0dlguu05`: baseline.json/status.before; source/scene snapshots; dependency-before.json/pre-deletion-search.json/post-retirement-audit.json; startup-gate.json/startup-regression.json; before/after compiler response files/logs and compilation.json; temporary test sources/executables/logs and regression-validation.json; parsed before/after scenes, exact diffs/expected files, scene-validation.json, key-hashes.json and final-scope.json. Compiler response files include the complete before/after runtime source inventories.

## Final ownership and remaining checks

ZoneController independently retains map/terrain references, index, geometry, queries, hover/persistent channels, renderer initialization/retry and disposal. BarrierController independently retains mode/input/state, compatibility initialization, limits, physics registration, preview/committed visuals, HUD, instance/static events and IBarrierProvider. Summary, Communication and dormant WaterSimulator use their migrated owners directly. No gameplay consumer requires the removed stamp.

BarrierController.OnDisable remains byte-identical and owns normal cancellation. There is no artificial substitute for stamp-disable cancellation; after retirement no stamp-disable transition exists. Fixed-map-per-session behavior and Stage 5J3A's startup-only resize/committed-mismatch guard remain byte-identical. Runtime map replacement, including same-size content replacement, is unsupported.

No mandatory dependency, independent-startup, state-consistency, compile or serialized-retirement gate failed. Retirement is complete. Remaining practical checks are Unity-native import and the manual PlayableTilemap acceptance below, plus the unchanged historical missing-script bindings outside scope.

## Manual Unity Play Mode checklist

1. Import/compile, inspect both scenes without saving unrelated changes, and confirm the stamp component is absent while FloodDefenseManager and its other components remain. Confirm no new missing-script component; distinguish the preserved historical bindings. Inspect the unchanged BarrierController values/references, ZoneController renderer settings and dormant WaterSimulator provider/flags.
2. Run **PlayableTilemap only** from a fresh saved N=5 state. Watch provisional 7×7 become 66×66 after the current 64×64 JSON loads and before placement. Verify a distant valid boundary, active-water blocking, preview/committed outlines, HUD and one Advisor notification, without stamp configuration.
3. Check data-ready-before/after initialization, unready/missing loader, timeout/recovery, pending cancellation, Escape and controller disable/re-enable. Repeat same-size entry and verify state/water/outlines/HUD persist.
4. Place four distinct barriers: required-three may show 3/3, while Summary completes once at four before mode exit. Check invalid/duplicate/failed placement and maximum rejection. In an unsaved runtime fixture, change dimensions after a commit and test both re-entry and an already-active click; require the mismatch diagnostic and preservation of state/water/outlines/HUD with no false notifications. Restore dimensions without saving.
5. Check Communication tower generation/activation/coverage, Shelter placement/capacity, Evacuation routes/selection/confirmation, live flood overlay activation/refresh/labels, zone hover/persistent channel visibility and full active WaterController stepping. Keep legacy WaterSimulator dormant; its provider remains BarrierController.
6. Reload PlayableTilemap and repeat cold initialization/completion to check native lifecycle and event cleanup. Do not run or delete Dev_UINavigation. Do not save temporary fixtures or unrelated Inspector/map changes. Review and commit this retirement after the native checks.
