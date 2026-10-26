# EvacuationSystem — Stage E3: Route State and Mitigation Extraction

Date: 2026-10-26 (America/New_York). Contracts: [E1 dependency audit](EvacuationSystem_StageE1_DependencyAudit.md) and [E2 extraction report](EvacuationSystem_StageE2_RouteGenerationPathfindingExtraction.md). The supplied E3 request explicitly authorizes the existing controller edit under Assets/Game and the two helpers/report below.

## 1. Executive summary

**E3 is complete.** EvacuationState now owns route, selection, confirmation, hover and cell-lookup state. EvacuationMitigationCalculator owns evacuated-population/ratio caches and total. The controller retains public orchestration, lifecycle, input, presentation and all event publication. Frozen pre-E3 and current implementations pass 176 managed assertions each, with 39 byte-identical traces, including all 115 E2 assertions/23 traces. Current compilation succeeds with 211 runtime sources, zero errors and the same 11 warnings. Public/serialized contracts and scene bindings are preserved. Only the authorized controller, two helpers, report and normal metadata changed. Native Unity testing was not performed.

## 2. Exact changed and created files

| Action | Exact repository path |
| --- | --- |
| Modified | `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationController.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationState.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationState.cs.meta` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationMitigationCalculator.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationMitigationCalculator.cs.meta` |
| Created | `Assets/Dev/AgentRef/Records/EvacuationSystem_StageE3_StateMitigationExtraction.md` |
| Created | `Assets/Dev/AgentRef/Records/EvacuationSystem_StageE3_StateMitigationExtraction.md.meta` |

The existing Dev_EvacuationSystem directory and its metadata are retained. Helper filenames match the request. Controller metadata, generator/pathfinder source and metadata, previous reports, every consumer and all presentation/scene/UI assets are unchanged. No new directory, assembly definition, scene component, serialized reference or generated IDE-project edit was needed.

| New helper | Script metadata GUID |
| --- | --- |
| EvacuationState | `9bcce4a81a12447b92aa7624a6433016` |
| EvacuationMitigationCalculator | `2bed4a7cbd2a49e0ba899329a1ba3c29` |

## 3. Final state ownership table

| Data | Sole authoritative owner | Facade interaction |
| --- | --- | --- |
| `_availableRoutes` | EvacuationState: readonly List field | Reads the same List; asks state to clear before validation; passes that exact object to unchanged E2 generator. |
| `_visibleRoutes` | EvacuationState: readonly List field | Existing visibility policy remains in controller; clear/add operations delegate to state. |
| `selectedRouteIds` | EvacuationState: ordinal HashSet field | Controller decides validation/cap/lock/transition timing, delegates mutations, passes the actual set to calculator/visualizer. |
| `hasConfirmedEvacuationRoute` | EvacuationState: bool | Controller decides when to store/clear; public property delegates. |
| `confirmedRouteId` | EvacuationState: string | Stored before Selected; cleared under existing explicit/stale conditions. |
| `hoveredRouteId` | EvacuationState: string | Controller keeps normalize/change detection, tooltip/redraw/event timing; state stores value. |
| `routeIdsByCell` | EvacuationState: Dictionary<Vector3Int,List<string>> | State rebuilds/queries it in existing visible-route/path order. |
| `evacuatedPopulationByZone` | EvacuationMitigationCalculator: ordinal Dictionary field | Calculator clears/repopulates synchronously at original facade invocation points. |
| `evacuationMitigationByZone` | EvacuationMitigationCalculator: ordinal Dictionary field | Same cache lifetime, normalized getter and Clamp01 behavior. |
| `totalEvacuatedPopulation` | EvacuationMitigationCalculator: int | Reset/accumulated by existing greedy algorithm; public getter delegates. |

The controller replaces its ten former storage fields with two readonly helper fields initialized inline: `_state = new()` and `_mitigation = new()`. Both exist before Awake, OnEnable, OnDisable or a public method can use them. They are not recreated during refresh or enable/disable. Serialized preview/settings/provider fields remain in the controller.

The state helper has exactly seven authoritative fields; the calculator has exactly three. The controller has no List, Dictionary or HashSet field storing route or mitigation state. The calculator stores no route-list, selected-set, state-helper or provider field: it receives the existing collections and a narrow population callback per synchronous operation. State stores no calculator reference. Temporary facade render lists and ordered event-payload snapshots retain their existing presentation/notification role; they are not another authoritative route store. Existing visualizer caches remain unchanged presentation caches.

Both helpers are internal sealed plain classes with no MonoBehaviour/ScriptableObject base, component discovery, subscription or gameplay event. They remain in Assembly-CSharp. The nearby CommunicationState and ShelterState supplied existing ownership patterns.

## 4. Method-level extraction mapping

| Pre-E3 operation | Current implementation |
| --- | --- |
| CurrentRoute / CurrentRoutes / GetAvailableRoutes | Delegate to state CurrentRoute / exact AvailableRoutes List; no copy or replacement. |
| GetSelectedRoutes | State owns unchanged available-order filtering and fresh List construction; facade forwards. |
| IsRouteSelected | State normalizes/trims the query and checks the original ordinal set; facade forwards. |
| HasEvacuationRouteForZone / HasSelectedEvacuationRouteForZone | State owns unchanged valid-route/zone/membership queries. |
| GetRouteById | State owns first exact matching cached route lookup; facade private method forwards for existing coordination callers. |
| GetHoverRouteIdForCell | State owns selected-first, then first-per-cell precedence; facade input calls the same private forwarding method. |
| BuildRouteCellLookup | State owns the original visible/path iteration and per-cell duplicate-ID suppression. |
| RemoveInvalidSelectedRouteIds | State owns unchanged successful-rebuild cleanup, early return, ordinal valid-ID set, HashSet encounter order and diagnostics; facade forwards at the original call site. |
| ClearConfirmedRouteSelection | Private State method used by stale cleanup; same flag guard, log-before-clear and ID-then-flag assignment. |
| NormalizeGeoid | State contains the original trim expression; controller's private method forwards for selection/hover normalization. Calculator keeps its own identical private normalization expression for population/zone queries. |
| Selection mutations | State AddSelectedRoute, RemoveSelectedRoute and ClearSelectedRoutes contain the original raw set operations. Policy and notification sequencing remain in controller. |
| Replacement ID enumeration | State GetSelectedRouteIdsExcept returns the original HashSet-order ID list. Controller removes/logs, recalculates, redraws and publishes in the original phases. |
| Confirmation storage / explicit clear | State StoreConfirmation and ClearConfirmation preserve original ID/flag mutation order; controller keeps lock/selection policy. |
| Visible collection clear/add and hover assignment | Small State methods perform original assignments/mutations; controller keeps input, filtering, redraw, tooltip and notification timing. |
| ClearVisibleRouteVisuals state portion | State ClearVisibleRouteState clears hover, visible list and cell lookup in the original order; controller still hides tooltip, clears visualizer and logs afterward. |
| RecalculateEvacuationMitigation | Calculator owns original cache clears, greedy allocation, ratio loop and diagnostic messages; facade private method forwards the actual route list/set and callback synchronously. |
| GetEvacuationMitigationForZone / GetEvacuatedPopulationForZone / GetTotalEvacuatedPopulation | Calculator owns original normalized/cache/clamp getters; public facade signatures unchanged. |
| GetRemainingPopulationAfterEvacuation / GetKnownZonePopulation | Calculator owns original route maximum/fallback sequence and post-fallback evacuated-count read. Public facade method supplies the live route list and population callback. |
| GetSourceZonePopulation | Existing private controller forwarder remains byte-identical to E2 and still calls EvacuationRouteGenerator.GetSourceZonePopulation with the current baseline provider. |
| Generation/pathfinding | E2 implementations unchanged. Controller clear and generator destination access now delegate to State, preserving exact timing/list identity. |

Source verification checks ten moved/copied State bodies and seven Calculator bodies against pre-E3 source, allowing only explicit forwarding/parameter substitutions and whitespace. Another **49 facade orchestration methods**, including both overloads where applicable, retain equivalent source after expanding the small state access/mutation calls back to their original statements. All 15 event Invoke sites retain their exact source, surrounding ordering and conditions. No selection/mitigation balancing change was introduced.

## 5. Controller public API preservation

The global `public class EvacuationController : MonoBehaviour` remains the public gameplay interface. Automated signature checks confirm the same **5 public properties, 22 public methods including aliases/overloads and 6 event declarations**. No consumer call or event subscription needed migration.

The entire global `[Serializable] public class SimpleEvacuationRoute` definition is byte-identical to pre-E3, preserving all 11 fields, the pathCells initializer and mutable route/path reference semantics. CurrentRoute still returns the first available record or null. CurrentRoutes/GetAvailableRoutes still return the exact live backing List through IReadOnlyList. GetSelectedRoutes still allocates a new List each time and preserves available-route order, including multiple records sharing a selected ID.

All seven external consumer files identified in E1 are byte-identical and compile in the full runtime build. No public helper API or replacement controller was introduced.

## 6. Serialization and GUID preservation

| Script/component | Preserved GUID | PlayableTilemap component / GameObject fileIDs |
| --- | --- | --- |
| EvacuationController | `ef31d31705ac48f898e5746ce90d32fa` | `315251714` / `315251712` |
| EvacuationRouteVisualizer | `37785d2c97374ef3abdc0d8dc1ba82b6` | `1117061671` / `1117061669` |
| EvacuationRouteTooltipController | `908e8eaeef6084fcf840ee5b5e6f5cad` | `817002036` / `817002034` |

All three components remain enabled with their original bindings. The controller metadata is byte-identical. All **23 serialized controller declarations** match pre-E3 exactly, including names/types, SerializeField/Min attributes and source initializers; all four Header attributes match too. The two helper fields are private readonly, unannotated fields, and neither helper is a serialized Unity component.

The entire PlayableTilemap YAML file is byte-identical to the captured starting file, so all 23 Inspector assignments are preserved: nine provider/presentation/document/camera references and fourteen settings. E1's complete Inspector inventory remains the source of exact values. In particular, saved preview1, road1/land3/water5, flood threshold0.5, minimum8, selected cap disabled/max3 and confirmation locking1 remain unchanged; Awake still disables preview. Route visualizer and legacy tooltip source/metadata/assignments are untouched.

No scene, prefab, UIDocument, UXML/USS, material, assembly boundary or generated project file changed. Dev_UINavigation and its metadata remain intentionally deleted.

## 7. Available-list identity verification

State constructs its available List once. The facade's rebuild calls `_state.ClearAvailableRoutes()` before unchanged validation and passes `_state.AvailableRoutes` directly to unchanged Generator.GenerateRoutes. Neither state nor facade replaces it, stages output in another list, or changes route construction.

Identity assertions compare the same object before generation, after refresh and through disable/re-enable; selected-set, hover-cell dictionary and mitigation dictionary identities also remain stable. Provider callbacks still see partial available counts 0 then 1, and nested Refresh retains the original incremental append order A_to_S1,B_to_S1,A_to_S1,B_to_S1 and nested event trace. The complete original E2 provider-query traces match exactly.

GetSelectedRoutes returns different List instances each call, in available-route order even when B was selected before A. Clearing that returned List does not alter selected IDs. The returned entries and pathCells are the same objects held in available data and passed to visualizer/shared tooltip within that rebuild. Successful regeneration still creates new route objects in the retained list, as before; moving ownership alone does not recreate route records.

## 8. Selection and confirmation regression results

Focused assertions verify:

- State is usable before Awake; disable/re-enable does not recreate/reset available, selected or mitigation storage.
- Disabled, empty, unavailable and wrong-case IDs reject with original event behavior; whitespace-trimmed queries use ordinal membership.
- Repeated selected IDs return true silently, including a stale selected ID after a failed rebuild, before available-route validation.
- Maximum-one replacement exposes the original intermediate empty-selection/zero-mitigation/all-visible state to Deselected and Changed listeners before the new selected route is committed.
- Replacement removal retains HashSet encounter order B then A when selected in that order; explicit clear instead notifies in available order A then B.
- Full max-greater-than-one rejects without events; disabling the cap permits additional routes and updates the same selected set used by allocation.
- With locking enabled, confirmation is already stored before Selected; an inside-Selected repeated call is silent and another route is rejected; clicking the confirmed route cannot deselect.
- With locking disabled, click deselection recalculates/redraws before Deselected→Changed and does not invent a confirmation reset.
- Explicit clear removes confirmation, emits ordered Deselected then Changed, and retains hidden cached redraw. Already-clear fast path redraws cached previews with no event.
- Stale cleanup preserves the original empty-set early return, matching-confirmation rule and absence of Deselected.

A subscriber observing max-one replacement sees exactly `D::0:2`, `X::0`, `S:B_to_S1:80:1`, `X:B_to_S1:80` (selected IDs, evacuated total and applicable visible count). Hover lookup, tooltip calls, line pool requests and selected-only controller/visualizer behavior remain unchanged in original and focused fixtures.

## 9. Mitigation formula and allocation verification

The calculator's allocation body is unchanged after substituting the existing route-list parameter for the former facade field and the narrow population callback for the old private population call. It clears the same ordinal dictionaries/reset total, iterates valid selected records in **available order**, trims zone/shelter IDs, tracks maximum selected-route population per zone and initializes shelter remaining capacity only from its first encountered selected route. Later records with that shelter ID reuse capacity regardless of their own capacity field. Allocation remains min(nonnegative remaining zone demand, remaining shelter capacity), accumulating per-zone counts and total. Ratios retain the existing maximum-selected-population denominator/fallback and Clamp01.

Hand-calculated assertions retain A100/B80 to S1(cap120) → A100/B20/total120/ratios1 and0.25 even when B was clicked first. With S2(cap50), A remains100 and B becomes70, total170/B ratio0.875. The original E2 trace for multiple shelters matches.

An additional duplicate-ID fixture selects three records with the same ID: A(pop100,S cap60), B(pop80,S cap999), A(pop200,T cap80). The Selected payload is the first matching record, all three records appear selected, shelter S keeps its first capacity60, shelter T adds80, A evacuation=140/B=0/total140, A ratio0.7 and remaining60. No ID collision repair, population double counting or capacity sorting was added.

GetRemainingPopulationAfterEvacuation still scans all matching route records, including invalid ones, for maximum population before considering baseline fallback. A positive invalid record avoids baseline calculation exactly as before. With no known route population, the callback can synchronously calculate baseline risk, rebuild/select a route and change mitigation before the getter reads evacuated count. The tested re-entrant fallback returns0 after selection evacuates100 from population100. Empty/unknown GEOID and nonnegative/ratio clamp logic remain unchanged.

The calculator stores no route/selected copies and does not reserve shelter protection capacity or incorporate communication, distance, danger, safety or accessibility. It has no dependency on State. The only facade callback is the explicitly scoped synchronous population lookup; the callback itself still forwards to the unchanged E2 generator population function.

## 10. Event ordering and re-entry verification

All six event declarations and **all 15 existing Invoke sites remain in EvacuationController**. Both new helpers have no gameplay events/subscriptions and do not publish these notifications. Diagnostics moved with state/calculation retain the existing `[EvacuationController]` text and their relative mutation timing.

The unchanged sequences include Built→Changed on successful refresh, Changed without Built on failure, Hidden→Changed, hover Changed before selection, Selected→optional Confirmed→Changed, intermediate replacement Deselected→Changed and explicit-clear ordered Deselected→Changed. Stale cleanup emits no Deselected; already-clear fast path emits nothing. No subscriber exception handling, batching, deferred dispatch or subscription reorder was added.

Focused synchronous subscriber tests preserve two particularly sensitive cases:

1. A Selected listener calls explicit clear. It sees confirmation already true, then emits nested Deselected/Changed. The outer method still emits Confirmed from its original local `routeConfirmed` result even though the callback has cleared confirmation. Exact trace: Selected(A,total100,confirmedTrue) → Deselected(A,total0) → Changed → Confirmed(A) → Changed.
2. During max-one replacement, the Deselected listener reselects A. The outer transition then adds B without a new cap check, leaving A and B selected with total120. Exact trace: Deselected(A,total0) → nested Selected(A,total100) → Changed → outer Changed → Selected(B,total120) → Changed.

Neither helper enforces new invariants that would suppress these existing outcomes. Remaining-population baseline callbacks and nested generation callbacks retain their original synchronous query/mutation visibility too.

## 11. Failed and successful rebuild behavior

A failed reference-validating rebuild clears available data but leaves selected IDs, confirmation and previous mitigation unchanged. Focused assertions verify the actual dictionary objects and contents survive, Changed occurs without Built and selecting the stale selected ID still returns true silently.

After the missing provider is restored and shelters are cleared, successful zero-route generation removes stale selected/confirmed IDs, clears mitigation in place and emits Built→Changed without Deselected. The existing E2 null-shelter and five-missing-reference warning/list-clear traces remain exact.

Successful nonzero generation reuses the list, preserves valid selected/confirmed IDs and recalculates allocation. Clear-before-validation, cleanup-after-success, presentation-after-state/calculation and event-after-presentation ordering remain in the facade. Generator/pathfinder provider references/settings and code are unchanged. No transaction, copied route snapshot, re-entry guard, lock or new initialization/retry phase was introduced.

## 12. Compilation and warning comparison

The existing E2 replay was executed before editing and passed its original E1/E2 checks. The pre-E3 controller and both E2 helper sources were then preserved independently under the E3 evidence directory. Frozen/current full runtime assemblies and actual controller/visualizer harness executables were compiled and executed.

| Build | Runtime sources | Errors | Warnings |
| --- | --- | --- | --- |
| Pre-edit E2 replay / pre-E3 runtime | 209 | 0 | 11 |
| Preserved pre-E3 runtime comparison | 209 | 0 | 11 |
| Current E3 runtime | 211 | 0 | 11 |

Both new source files are included in a fresh enumeration of the complete default runtime assembly using the existing Editor/asmdef/asmref exclusions. The Unity6000.0.63f1 bundled Roslyn/NetCoreRuntime compiler and identical Unity/package references/options are used. Generated project files and Library outputs are not manually edited.

Warning-message multisets match: CS0105×1 duplicate using, CS0618×8 obsolete APIs, CS0414×2 existing assigned-unused fields. No new suppression, error or warning was introduced. Separate managed fixture compilations retain the same two expected CS0649 warnings for unassigned-in-source visualizer Inspector fields; these do not change the runtime diagnostic count.

This is managed compilation with existing dependency assemblies, not a clean native package rebuild, Unity import, IL postprocessing pass or player build.

## 13. Assertion and trace comparison against E2

| Coverage | Assertions per implementation | Matching traces | Comparison |
| --- | --- | --- | --- |
| Original E1 expectations | 79 | 14 | Exact original recorded output retained |
| Additional E2 expectations | 36 | 9 | Exact E2 expanded output retained |
| Additional focused E3 checks | 61 | 16 | Frozen pre-E3/current outputs byte-identical |
| Total unique coverage | **176** | **39** | Passed on both implementations |

Original fixtures retain complete ordered route IDs, path cells, distances, population/capacity values, selection/confirmation, mitigation, public event traces, provider queries, actual visualizer pool/style requests and shared-tooltip calls. Focused E3 traces add ownership/lifetime, normalization/toggling, selected-copy semantics, replacement observer state/removal order, selection limits, confirmation, explicit clear, failure recovery, duplicate allocation, baseline-getter re-entry, public subscriber re-entry and hover clearing.

No expected-output sorting, ID mapping, trace normalization or rewriting was applied. The full original E2 output remains an exact prefix of the focused run. The two frozen/current implementations were actually executed, not inferred equivalent from source comparison alone.

Temporary current-side fixture adaptation redirects the removed private fields (such as `_visibleRoutes`) to the same-named private field in `_state` or `_mitigation`. It exposes no mirrored production field and does not change any gameplay expectation. The E2 pathfinder reflection bridge remains unchanged. Original baseline fixture/source files are read-only inputs. New focused fixtures use the same ownership-aware reflection utility for both variants; the baseline takes its existing-field branch. Public API assertions and recorded trace formats/values remain unchanged.

Evidence directory:

`/private/tmp/evacuation-e3-na9dapw2`

It contains the initial git status/hash manifest, pre-E3 sources, original E2 fixture/output copies, compiler response files/logs, frozen/current executables and logs, focused fixtures, extraction mapping, results.json, focused-results.json, verification.json and final git status. From the repository root, while temporary E2/E3 evidence remains available:

```sh
python3 /private/tmp/evacuation-e3-na9dapw2/replay.py
```

This recompiles/runs the frozen/current runtime and original/expanded/focused comparisons and checks the same counts and byte equality. It writes temporary evidence outputs only. The frozen pre-E3 source is retained independently of the current controller. These temporary files are not a durable CI suite and can be removed by the OS.

## 14. Unity-native verification limitations

Native Unity import, Play Mode and player build were **not performed**. No scene was launched. Only `Assets/Game/Content/Scenes/PlayableTilemap.unity` is authorized for native follow-up. No Dev_UINavigation scene was restored, recreated or run.

Managed tests execute actual controller, E2 helpers and visualizer code with controlled Unity/map/zone/shelter/baseline/tooltip stand-ins. They verify logic, object identity, ordered calls, callback re-entry and requested rendering/tooltip operations. They do not establish native camera/input scheduling, shader output, prefab lifecycle, UI Toolkit layout/callback delivery or complete loader/provider scene timing. Actual MapHoverTooltipController and legacy tooltip UI layout are not executed; all related production files/assets remain unchanged. All seven complete consumers compile, but are not instantiated as native Unity UI components by these fixtures.

A future PlayableTilemap smoke test should verify import, generated routes, selected/confirmed state, summary/mitigation, advisor/turn responses, hover tooltips, hidden preview persistence and shelter-placement suppression without saving scene mutations. No native result is attributed from previous-stage records.

## 15. Working-tree scope verification

Initial git status and SHA-256 hashes were captured before edits for **6307 existing files** under Assets, Packages, ProjectSettings and UserSettings. Final comparison confirms that the only existing file changed is the authorized EvacuationController.cs; all other 6306 initial files are byte-identical. Exactly the six new files in §2 were added. No initial file was deleted; no folder metadata, old metadata or unrelated file changed.

The manifest includes both E2 helper scripts/metadata, E1/E2 reports, all scenes/prefabs/UI/settings and all consumers. Their byte preservation is checked, including pre-existing modified/untracked work. Original controller/visualizer/legacy-tooltip GUIDs, enabled components and scene fileIDs resolve unchanged. New metadata GUIDs are unique. Source checks confirm 23 serialized fields, 5 properties, 22 methods, 6 events, 15 Invoke sites, single ownership of 7 State fields/3 Calculator fields, the moved-body comparisons and 49 equivalent facade orchestration methods described above. Whitespace checks pass for the changed/new source and report.

Git status preserves every pre-existing modified/deleted/untracked entry, including the already-dirty controller and untracked E2 helper directory. The hash manifest identifies E3's changes inside those existing status entries. Dev_UINavigation and its metadata remain deleted. No staging, commit or unrelated cleanup was performed.

## 16. Known behavior quirks deliberately preserved

- Failed generation retains stale selected/confirmed state and mitigation after clearing available routes; successful zero generation removes stale state without Deselected.
- Available/route/path objects remain mutable and shared; nested generation callbacks can expose partial data and produce duplicate records.
- Selection order does not drive allocation. Duplicate route IDs select all matching valid records, while single-ID lookup/event payload uses the first match. First selected-record capacity for a shelter ID wins.
- HashSet order controls replacement removal and some hover/visualizer choices; selected queries/explicit-clear notifications instead use available-route order.
- Controller selected-visible routes can outnumber the one selected route actually drawn by the unchanged visualizer. Hidden explicit clear and already-clear calls can redraw cached lines without enabling preview.
- Temporarily disabling lock and deselecting does not automatically clear confirmation. Successful cleanup with an empty selected set returns early, retaining an inconsistent confirmation until explicit clear, just as before.
- Subscriber re-entry can clear confirmation before the outer Confirmed publication or leave multiple selected IDs after a max-one replacement. No new state-machine invariant repair was added.
- Remaining-population lookup can include invalid route records or lazily calculate baseline risk; re-entrant mitigation changes are read after that fallback.
- Optional discovery, one-attempt Zone resolution, shelter subscription timing, shelter-only invalidation, unused routeColor/waterTileCost and missing tooltip fallback/shared-host behavior remain unchanged.

## 17. Readiness for E4

**E3 is complete, with no scope blocker.** Both required helpers are extracted together. The controller remains the sole public API/event publisher and retains lifecycle, discovery, generation orchestration, selection policy, input, visual/tooltip coordination and serialized settings. State and mitigation ownership are isolated without duplicate authoritative collections.

E4 can begin from this documented boundary with its own explicit scope and preserved evidence. E4 and E5 were not started. Native Unity verification remains the explicit limitation in §14.

Review and commit the E3 checkpoint while keeping unrelated working-tree work separate.
