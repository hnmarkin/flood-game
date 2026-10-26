# EvacuationSystem — Stage E4: Input and Presentation Extraction

Date: 2026-10-26 (America/New_York). Contracts: [E1 audit](EvacuationSystem_StageE1_DependencyAudit.md), [E2 extraction](EvacuationSystem_StageE2_RouteGenerationPathfindingExtraction.md), and [E3 extraction](EvacuationSystem_StageE3_StateMitigationExtraction.md). Scope follows the supplied E4 request, including its explicit authorization to modify the existing controller under Assets/Game.

## 1. Executive summary

**E4 is complete.** EvacuationInputController interprets legacy mouse input through synchronous private facade actions. EvacuationPresentation prepares and forwards existing rendering/tooltip requests without owning state or replacing components. The facade retains lifecycle, discovery, selection/confirmation policy, generation and public events. Pre-E4/current execution passes 240 assertions per implementation with 80 matching traces, including every E3 expectation. Runtime compilation succeeds with 213 sources, zero errors and 11 unchanged warnings. Existing helpers, consumers, visualizer/tooltips and scene serialization are unchanged. Only the authorized controller, two helpers, report and normal metadata changed. Native Unity testing was not performed.

## 2. Exact changed and created files

| Action | Exact repository path |
| --- | --- |
| Modified | `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationController.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationInputController.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationInputController.cs.meta` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationPresentation.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationPresentation.cs.meta` |
| Created | `Assets/Dev/AgentRef/Records/EvacuationSystem_StageE4_InputPresentationExtraction.md` |
| Created | `Assets/Dev/AgentRef/Records/EvacuationSystem_StageE4_InputPresentationExtraction.md.meta` |

New helper GUIDs: InputController `2487cf50f1e44502b86ccf421e427213`; Presentation `092cac043dfc4ed3bb7adf3abdb78a24`. Existing folder and controller metadata are untouched. Helper filenames match the request; no convention-driven rename or scope exception was required. Temporary verification code and outputs are outside Assets.

## 3. Final six-helper architecture

| Helper | Responsibility / ownership | E4 status |
| --- | --- | --- |
| EvacuationRouteGenerator | Ordered shelter/zone enumeration, anchors, metadata, route construction, incremental appends into borrowed available list | E2 source and metadata byte-identical |
| EvacuationPathfinder | Existing A*, live traversal reads and coordinate conversion | E2 source and metadata byte-identical |
| EvacuationState | Sole owner of seven route/selection/confirmation/hover/lookup fields | E3 source and metadata byte-identical |
| EvacuationMitigationCalculator | Sole owner of population/ratio dictionaries and total, original greedy allocation | E3 source and metadata byte-identical |
| EvacuationInputController | Eligibility interpretation, mouse/world/cell sampling, lookup candidate, hover-change interpretation and late click dispatch | New internal sealed class, no fields |
| EvacuationPresentation | Borrowed-state route preparation/visibility, visualizer forwarding, route-tooltip dispatch and clear/cache-redraw requests | New internal sealed class, no fields |

EvacuationController remains the global public MonoBehaviour and sole public gameplay event publisher. It retains serialized settings/providers, lifecycle, optional discovery, document/camera fallback, public API forwarding, generation, selection/confirmation rules and mitigation timing. It constructs `_input` and `_presentation` inline as readonly fields alongside the existing State/Calculator. The new helpers have no explicit constructor or construction side effect and are ready before lifecycle/public paths.

Dependencies remain narrow: Input queries State and invokes three private facade actions synchronously; Presentation borrows State and existing visualizer/shared tooltip references. Neither helper stores a controller, component, route, selection, hover value, frame result or rendering cache. Neither depends on the other. No public facade API is called by a helper, and no service locator, singleton, event bus, coroutine, queue, extra MonoBehaviour, ScriptableObject or assembly boundary was introduced.

## 4. Method-level input extraction mapping

| Pre-E4 controller logic | Current owner / retained boundary |
| --- | --- |
| Update preview guard | Facade Update is byte-identical: hidden preview returns before input work. |
| Visible-route/lookup-empty check | Input.ShouldClearHover reads the original State counts/short-circuit condition. Facade still calls SetHoveredRoute(null,zero) immediately and returns when required. |
| Missing route Tilemap/camera checks and Camera.main fallback | Remain in facade UpdateRouteHoverAndSelection at their original point and order. Camera fallback assignment updates the same serialized facade field. |
| Mouse sampling, ScreenToWorldPoint, z=0 and WorldToCell | Input.ProcessInput contains the original sequence, including three mousePosition reads and the separate screen XY samples. |
| Route hit candidate / overlap precedence | Input calls unchanged State.GetHoverRouteIdForCell with the absolute Unity cell; no duplicate lookup or selected set. |
| SetHoveredRoute normalization and same-target test | Input.TryGetHoverChange applies the original State normalization, converts empty to null and compares with the existing hovered ID. Facade retains mutation, logging, tooltip, redraw and Changed publication. |
| Tooltip-follow phase | Input synchronously invokes the private facade UpdateRouteTooltipPosition after hover callbacks, if the current State hovered ID is nonempty. |
| Left-click interpretation | Input reads GetMouseButtonDown(0) only after hover/tooltip callbacks and only if the original local hit ID is nonempty. It invokes private TryToggleRouteSelection synchronously. |
| Selection, deselection, lock, limits, confirmation and events | Remain in facade methods, with no new policy or debounce in Input. |
| Private GetHoverRouteIdForCell facade wrapper | Removed because its production caller moved; unchanged State method remains the lookup owner. Temporary tests target State directly when this private seam is absent. |

The input helper does not batch a frame result or pre-sample the click. Subscribers can change input, hide/refresh preview or change selection before the original local hit candidate is processed. The helper does not recheck preview/cap/confirmation after callbacks or suppress exceptions. It adds no pointer capture, UI-blocking rule, drag detection or new Input System dependency.

## 5. Method-level presentation extraction mapping

| Pre-E4 method/operation | Current implementation |
| --- | --- |
| EnsureVisualizerConfigured | Presentation owns the original null guard, SetTilemap and SetDebugLogs calls; facade forwards at unchanged lifecycle/refresh call sites. |
| RedrawVisibleRoutes | Presentation retains original preview/visualizer guard, visible-list clear, available-order render list construction, State visibility additions, cell lookup rebuild and DrawRoutes call. |
| ShouldDisplayRouteVisual | Private Presentation policy with unchanged validity, hide-unselected flag, selected-count and exact-ID membership rules. |
| BuildRouteCellLookup facade wrapper | Removed; Presentation calls unchanged State.BuildRouteCellLookup at the original point. |
| ClearVisibleRouteVisuals | Presentation records had-hover, clears State hover/visible/lookup, hides route tooltip, clears visualizer, then logs in the original order. |
| ShowRouteTooltip | Facade retains route lookup, missing-route hide branch, EnsureTooltipConfigured and null-tooltip branch. Presentation owns the existing ShowEvacRouteTooltip call and subsequent diagnostic text. This preserves lookup-before-configuration timing. |
| UpdateRouteTooltipPosition | Presentation retains visibility guard, lookup of the current hovered route, missing-route hide, and re-show of the same route object at the new screen position. |
| HideRouteTooltip | Presentation retains null/visibility guard, evacuation-only hide and diagnostic. |
| ShowOnlySelectedRoute forwarding | After the facade's existing TrySelectRoute success, Presentation forwards the original string to the unchanged visualizer. |
| Explicit clear / already-clear cached redraw | Presentation forwards ClearRouteSelectionAndShowAll at the same existing facade branches; state changes and notifications remain in the facade. |
| ResolveReferences / ResolveTooltipUIDocument / EnsureTooltipConfigured | Remain byte-identical in the facade, preserving searches, runtime creation, document priority/fallback and configuration side effects. |

Presentation borrows visualizer references by ref where operations cross callbacks or perform successive component calls. In particular, clearing presentation reads the current facade visualizer after tooltip hide, retaining the original behavior if a synchronous callback retargets that reference. Ref parameters are call-scoped aliases, not retained ownership.

Six moved presentation bodies compare equivalent to pre-E4 source after explicit borrowed-state/parameter forwarding. The sampled input/callback tail also compares equivalent. Fifty facade method bodies remain byte-identical; 55 retained facade policy bodies match after expanding the small input/presentation dispatch calls. Three obsolete private facade wrappers/implementations were removed; no production compatibility wrapper was added for tests.

## 6. Public API preservation

The global `public class EvacuationController : MonoBehaviour`, all five properties, 22 public method signatures including aliases/overloads, and six public event declarations are unchanged. All seven external consumer files remain byte-identical and compile in the full runtime assembly.

The complete global `[Serializable] public class SimpleEvacuationRoute` definition remains byte-identical: 11 fields, pathCells initializer, mutability and reference semantics. CurrentRoutes/GetAvailableRoutes retain the live State list; selected-list copy semantics, incremental generation and consumer route/path identity remain covered by the unchanged E3 expectations.

All 15 existing event Invoke expressions remain in their original facade methods. Input receives private synchronous method groups, not a replacement public interface or event publisher. Presentation invokes existing visualizer/tooltip APIs and never emits gameplay events.

## 7. Serialized GUID and Inspector preservation

| Component | Script GUID | PlayableTilemap component / GameObject fileIDs |
| --- | --- | --- |
| EvacuationController | `ef31d31705ac48f898e5746ce90d32fa` | `315251714` / `315251712` |
| EvacuationRouteVisualizer | `37785d2c97374ef3abdc0d8dc1ba82b6` | `1117061671` / `1117061669` |
| EvacuationRouteTooltipController | `908e8eaeef6084fcf840ee5b5e6f5cad` | `817002036` / `817002034` |

The original three components remain enabled and bound. Their metadata and the complete PlayableTilemap scene are byte-identical. All 23 serialized controller declarations retain exact names/types, SerializeField/Min attributes, initializers and all four Header attributes. The unchanged scene preserves every Inspector assignment, camera/Tilemap/provider/document reference and external consumer binding recorded in E1.

No serialized helper field or component was added. The new helper fields are private readonly references to plain internal classes. No scene, prefab, material, UXML/USS, GlobalHUD, settings or assembly-definition change occurred. The intentionally deleted Dev_UINavigation scene and metadata remain deleted.

## 8. Input, hover and click behavior verification

Focused frozen/current cases verify exact ordered input/visual/tooltip/event calls:

- Three mousePosition reads: a queued first sample20,30,9 goes to ScreenToWorldPoint; independent later samples supply screen88,66; projected world z8 is forced to0 before WorldToCell. No ray/plane conversion or normalization offset is added.
- Negative terrain origin, controlled nontrivial camera projection, absolute-cell hit and original screen-position forwarding. Raw floating-point trace values are compared without normalization.
- First matching route on overlap, selected-first precedence, route-to-route transitions, selected-only lookup and the existing undrawn-selected-route hit case.
- Hover changes update tooltip/rendering before Changed. Same target follows the cursor without redraw or event; no-hit clears once and skips mouse-button sampling; a repeated null target remains silent.
- Click selection, unlocked click deselection, confirmation-before-Selected and locked repeated click behavior match the baseline.
- Hidden preview and shelter-placement suppression perform no input sampling; placement exit does not resume preview.
- Missing camera returns after the original Camera.main attempt while retaining prior hover/tooltip. Recovery stores fallback camera in the facade. Missing route Tilemap returns before camera lookup. Missing optional tooltip permits input/selection without discovering or creating a replacement during input.
- Empty lookup clears a stale hover/tooltip without sampling mouse or button input, including the original delayed cleanup after successful zero-route refresh.

No change to frame processing, eligibility, overlap ordering, input API or selection rules was introduced.

## 9. Visualizer call order and rendering verification

The actual unchanged EvacuationRouteVisualizer source executes in both managed variants. Instrumented requests/logs, pooled LineRenderer state and reference assertions verify:

- New hover shows tooltip, clears/redraws routes, publishes Changed, then follows the tooltip and samples the click. Selection redraws committed selected styling before Selected/Confirmed/Changed listeners run.
- Show and successful Refresh draw before Built→Changed. Hide clears before Hidden→Changed. Repeated Hide still clears without events.
- Available-route drawing order, shallow route/path identity, selected-over-hover-over-preview styling, selected-only drawing and the multiple-selected hit-test/draw mismatch remain intact.
- Existing line objects and runtime material are reused across hover changes. The pool remains13 lines for the two fixture routes; selected A activates6, selected B7. Geometry remains the four cell corners0,0 /1,0 /1,1 /0,1 with z−0.05, world-space loop, Default layer and order2300.
- Hidden selection clear can redraw all cached lines before Deselected/Changed while preview remains false. Already-clear calls can redraw with no event.

Presentation owns only the original ephemeral routes-to-draw list preparation. It introduces no geometry, renderer, material, styling, shader lookup, pool or render cache. All existing visualizer APIs, fields, caches, prefab/material ownership, clear behavior and shader fallbacks are byte-preserved; the original E1–E3 pooling/material/shader traces still match.

## 10. Tooltip ownership and behavior verification

MapHoverTooltipController remains the shared route-tooltip implementation. The legacy EvacuationRouteTooltipController remains an enabled serialized component with its original GUID/component identity and unchanged source; E4 does not retire, move or disable it. Presentation creates no tooltip labels, styles, document, UXML root or alternate tooltip system.

The controller's original document discovery/configuration remains intact: explicit assigned document, then GlobalHUD singleton document, then unsorted inactive-inclusive document search with money/pop/zone label preference and first-usable fallback. Controlled UIDocument roots exercise these exact facade branches, including unbuilt-root exclusion and inactive preferred document selection. Existing Editor asset fallback paths and player behavior are not changed.

Focused tooltip verification additionally compiles the actual DemoDisplayIdFormatter source and executes copies of the actual MapHoverTooltipController ShowEvacRouteTooltip, SafeText and SetLabelText bodies with instrumented root/label/position seams. Literal expected text includes `E-01 | Zone: Z-01`, population100, S-01, School and capacity120; it verifies blank-zone/type fallbacks and nonnegative labels. The existing literal `Unknown Shelter` is an ordinary input to DemoDisplayIdFormatter and becomes S-02 in the fixture; only the existing empty/placeholder rules produce its Unknown Shelter display fallback. That behavior is preserved.

Route and path identity match the renderer's cached record. New-hover tooltip show plus position re-show, same-hover position-only re-show, hide-before-redraw, missing optional tooltip and absent-root failures match. When a root becomes available after a failed show, unchanged hover does not retry until the target changes, as before.

Communication coexistence is tested through a controlled shared-tooltip document/visibility model: same-host route show leaves communication visible, evacuation hide clears only the route tooltip, and document retarget resets both visibility flags as the unchanged ResetCachedElements source specifies. The complete CommunicationController/MapHoverTooltipController UI lifecycle and native cloned-root/layout behavior are not executed by that model. No claim of native communication-tooltip rendering is made.

## 11. Event order and synchronous re-entry verification

All six events and 15 publication sites remain in the facade, with original success/failure/early-return conditions and ordering. The moved hover comparison is equivalent to the old normalize/null/same-ID test. No helper event, deferred queue, coroutine, exception suppression or re-entry guard was introduced.

Focused callbacks demonstrate the sensitive phases:

- A hover Changed listener sets Input.Click=true. The later legacy button read sees it and selects the original hit route.
- The second tooltip show/position callback cancels Input.Click. The subsequent read sees false and does not select.
- A hover Changed listener hides preview. The old local hit candidate still receives its original click processing after Hide: Changed→Hidden→Changed→Selected(A)→Changed, with preview hidden and visuals clear. No new post-callback eligibility guard is added.
- A hover listener selects B before the original click for A. Nested B selection occurs first, then the original A click performs the same replacement and exact Deselected/Changed/Selected/Changed sequence.
- A throwing hover listener propagates its original exception type/message after the first tooltip/redraw, preventing the position-follow and button phases. No callback exception is swallowed.
- A controlled tooltip-hide callback retargets the facade's visualizer. Clear still dispatches to the updated reference after that callback, matching the pre-E4 implementation.

Every E3 subscriber-re-entry expectation remains unchanged too, including confirmation cleared before the outer Confirmed publication and max-one replacement re-entry leaving two selected IDs. Presentation updates remain on the same side of each notification; listeners observe committed visualizer state during Selected.

## 12. State and mitigation ownership preservation

All four E2/E3 helper scripts and their metadata are byte-identical to pre-E4. State still owns available/visible routes, selected IDs, confirmation, hover and the cell lookup; Calculator still owns the two mitigation caches and total. Generator still appends incrementally to that exact State list. The controller still performs selection, confirmation and mitigation recalculation at the same points.

Both new E4 helpers have **zero persistent fields**. Input borrows State and retains no candidate or hover state across calls. Presentation borrows State/component references, uses the original ephemeral render list and retains no cache or authoritative collection. It requests existing State mutations at the former clear/redraw points without taking over ownership. There is no duplicate selected set, route-cell dictionary, mitigation cache, material or pool.

Lifecycle methods Awake, OnEnable, OnDisable, OnValidate and Update remain byte-identical, as do ResolveReferences, ResolveTooltipUIDocument and EnsureTooltipConfigured. No enable/disable reset or new gameplay initialization phase was added.

## 13. Frozen/current regression results

E3 replay ran before editing and passed its original 211-source build, 176 assertions and 39 traces. The actual pre-E4 controller and four existing helper sources were then independently preserved. Both pre-E4 and current variants were compiled and executed.

| Coverage | Assertions per implementation | Matching traces | Result |
| --- | --- | --- | --- |
| Existing E1–E3 fixtures | 176 | 39 | Entire outputs equal their saved E3 expectations |
| Focused E4 fixtures | 64 | 41 | Complete frozen/current instrumented outputs byte-identical |
| Unique combined coverage | **240** | **80** | Passed on both implementations |

The original E3 output remains an exact prefix of the focused run. No route-ID mapping, sorting, event rewriting, object-identity substitution or trace normalization is applied. Object identity is checked with reference assertions; raw mouse/cell/tooltip/render/event traces are compared exactly.

Temporary current-side reflection adapts the removed private facade GetHoverRouteIdForCell seam to the unchanged State method. Original baseline fixtures/expectations are not rewritten. The existing E2 pathfinder and E3 field-owner bridges remain temporary harness utilities. No production wrapper was retained or introduced for tests. Focused common fixtures use that same bridge with the baseline taking its original method branch.

New probes instrument engine/input/UI seams around actual controller/helpers/visualizer execution. Real shared-tooltip formatting method bodies and the real display-ID formatter run against controlled labels/root availability/position delivery; full UI Toolkit layout and component initialization remain outside this harness. The original fixtures also run separately with their original controlled seams. Newly written E4 assertions were calibrated against the untouched pre-E4 implementation before comparison; existing E1–E3 expected outputs were never altered.

Evidence directory:

`/private/tmp/evacuation-e4-joytoa_b`

Includes starting status/hashes, independently preserved pre-E4 sources, original E3 fixture/output/compiler-input copies, frozen fixture sources, instrumented formatting excerpts, compiler response files/logs, both executables/full outputs, extraction mapping, results.json, focused-results.json, verification.json and final status. From repository root while these temporary artifacts remain:

```sh
python3 /private/tmp/evacuation-e4-joytoa_b/replay.py
```

The replay rebuilds both runtime assemblies and reruns the original and focused comparisons. It writes evidence outputs only. Temporary evidence can be removed by the OS and is not a permanent project test suite.

## 14. Runtime compilation and warning comparison

| Full runtime build | Sources | Errors | Warnings |
| --- | --- | --- | --- |
| Pre-edit E3 replay / pre-E4 baseline | 211 | 0 | 11 |
| Independently preserved pre-E4 comparison | 211 | 0 | 11 |
| Current E4 | 213 | 0 | 11 |

Current runtime sources are freshly enumerated with the existing Editor/asmdef/asmref exclusions. All six helpers and all seven complete consumers compile in the default runtime assembly using the same Unity6000.0.63f1 Roslyn/NetCoreRuntime compiler, references and complete options. No new suppression is added. The warning-message multiset is identical: CS0105×1 duplicate using, CS0618×8 obsolete APIs and CS0414×2 existing assigned-unused fields.

Separate fixture compilations retain the same two expected CS0649 warnings for visualizer Inspector references; these are not new runtime warnings. Generated project/Library files are not manually edited. This is managed compilation with existing dependency outputs, not a native Unity import, clean package build, IL postprocessing result or platform player build.

## 15. Unity-native verification limitations

Native Unity import, Play Mode and player build were **not performed**. No scene was launched; only PlayableTilemap is authorized for native follow-up. Dev_UINavigation was not restored, recreated or run.

Controlled tests establish logic, reference identity, exact synchronous ordering, sampled values and requested geometry/rendering/tooltip operations. They do not establish actual Unity camera projection, tilemap transforms, frame/input scheduling, shader availability/GPU output, prefab lifecycle, native UI Toolkit layout/cloning or complete provider/document startup. Tooltip text methods execute through controlled seams; actual full MapHoverTooltipController, legacy tooltip and CommunicationController UI behavior are source-preserved/compiled rather than natively executed.

The next native PlayableTilemap smoke test can check initial hidden preview, route generation, overlap hover, unlocked/locked click behavior, advisor/turn feedback, tooltip coexistence, shelter placement suppression and hide/show/cached-clear quirks without saving scene mutations. No earlier record is treated as a new native result.

## 16. Working-tree scope verification

Starting git status and SHA-256 hashes cover **6313 existing files** under Assets, Packages, ProjectSettings and UserSettings. Final comparison identifies only the authorized EvacuationController.cs as changed; every other 6312 starting file is byte-identical. Exactly six new files were added, listed in §2, with no initial deletion or folder metadata change.

The manifest protects the four old helpers, their metadata, all seven consumers, visualizer/shared/legacy tooltip scripts and metadata, all scenes/prefabs/UI/materials/settings and previous E1–E3 reports. New metadata GUIDs are unique. Source checks confirm the 23 serialized fields, 5 properties, 22 public methods, 6 events, 15 Invoke sites, stateless new helpers, equivalent moved/input/policy bodies and absence of new discovery or renderer/UI ownership in helpers. Original authorized-scene GUID/fileID/enabled bindings resolve unchanged. Whitespace checks pass for the changed/new source and report.

Every pre-existing modified, deleted and untracked status entry is preserved, including the already-dirty controller, untracked helper directory, unrelated CommunicationSystem work and deleted Dev_UINavigation scene/meta. The manifest distinguishes E4 edits inside existing status entries. No staging, commit, revert or unrelated cleanup was performed.

## 17. Known quirks deliberately preserved

- Legacy three-read mouse sampling, forced world z0, exact cell hits, no UI-blocking/debounce, same-target behavior and late click sampling remain unchanged.
- Missing camera/Tilemap can retain old hover/tooltip; a newly available tooltip root does not automatically retry the same target. Empty-route refresh can leave tooltip cleanup for the following input update.
- Failed rebuild clears available routes while retaining stale selection/confirmation/mitigation. Successful zero cleanup emits no Deselected. Nested generation can expose partial lists and duplicate records.
- HashSet order, duplicate route IDs/first-match lookup, ordered greedy capacity allocation and selected hit-test/render mismatch remain intact.
- Hidden or already-clear selection paths can redraw cached lines without preview/events. No visual cache disagreement was repaired.
- Confirmation-before-Selected and subscriber-re-entry effects remain, including post-Hide local click processing. Callback exceptions still abort later phases.
- Optional discovery, shared-tooltip document competition, existing missing Dev asset fallback paths and unused routeColor/waterTileCost fields are unchanged.
- Literal Unknown Shelter display-ID mapping retains existing formatter behavior. The enabled legacy tooltip remains intact; no retirement decision was made.

## 18. Readiness for Stage E5

**E4 is complete with no scope blocker.** Both required internal helpers are implemented in this single stage, with all E3 expectations and focused input/presentation comparisons matching the pre-E4 implementation and no new runtime diagnostics. The facade remains the public coordinator/event publisher, and the four existing helpers and presentation components are untouched.

The six-helper structure is ready for the separately scoped E5 review. E5 was not started; legacy tooltip retirement is not automatic. Native Unity verification remains the explicit limitation in §15.

Review and commit the E4 checkpoint while preserving unrelated working-tree work.
