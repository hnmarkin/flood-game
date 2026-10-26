# EvacuationSystem — Stage E2: Route Generation and Pathfinding Extraction

Date: 2026-10-26 (America/New_York). Behavioral contract: [Stage E1 audit](EvacuationSystem_StageE1_DependencyAudit.md). Implementation scope: the supplied E2 request, including explicit authorization to change only the existing EvacuationController under Assets/Game.

## 1. Executive summary

**E2 is complete.** Route construction and A* now reside in two internal plain C# helpers. EvacuationController retains its public identity, serialized fields, live route-list ownership, state, events, lifecycle, input and presentation coordination. The frozen E1 implementation and current implementation were both compiled and executed: all 79 E1 assertions and 14 traces match, plus 36 additional assertions and 9 traces. Current runtime compilation succeeds with 209 sources, zero errors and the same 11 warnings. Only the authorized controller, helpers, normal metadata and this report changed. Native Unity import and Play Mode were not performed.

## 2. Exact files changed and created

| Action | Exact repository path |
| --- | --- |
| Modified | `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationController.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationRouteGenerator.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationRouteGenerator.cs.meta` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationPathfinder.cs` |
| Created | `Assets/Dev/Dev_EvacuationSystem/EvacuationPathfinder.cs.meta` |
| Created | `Assets/Dev/Dev_EvacuationSystem.meta` |
| Created | `Assets/Dev/AgentRef/Records/EvacuationSystem_StageE2_RouteGenerationPathfindingExtraction.md` |
| Created | `Assets/Dev/AgentRef/Records/EvacuationSystem_StageE2_RouteGenerationPathfindingExtraction.md.meta` |

The folder metadata and report metadata are normal Unity asset metadata. The existing controller `.meta` was not edited. Helper filenames match the request exactly; no filename-convention exception or rename was needed. Both helpers belong to the same default Assembly-CSharp assembly as the controller; no assembly definition, generated IDE project or scene reference was added. Temporary verification files live outside Assets and are not project scripts or new gameplay helpers.

| New metadata | GUID |
| --- | --- |
| `Assets/Dev/Dev_EvacuationSystem.meta` | `887623914df147868e9f6325fbb673d3` |
| `Assets/Dev/Dev_EvacuationSystem/EvacuationPathfinder.cs.meta` | `0e99c2a7e80f46e382fce810ae11bc6c` |
| `Assets/Dev/Dev_EvacuationSystem/EvacuationRouteGenerator.cs.meta` | `59d548a758c447c48030a5714584f907` |

## 3. Responsibilities after extraction

**EvacuationController** remains the sole public evacuation gameplay coordinator. RebuildAvailableRouteData still clears `_availableRoutes` in place before running the unchanged ValidateRouteReferences. After validation it creates a pathfinder and generator from the current providers and settings, passes the existing list to the generator, and returns true when generation completes. It never replaces that list. GetSourceZonePopulation remains a private forwarding method because GetKnownZonePopulation, used by public population queries, still needs the same lazy lookup behavior outside generation.

All available/visible collections, selected/confirmed/hover state, per-cell lookup, evacuated-population/mitigation dictionaries and total stay in the facade. Its lifecycle, discovery, subscription guards, Show/Hide/Refresh orchestration, public notifications, mouse handling, visual redraw and tooltip coordination stay there too.

**EvacuationRouteGenerator**, an `internal sealed class`, owns active-shelter enumeration, per-shelter destination resolution, associated GEOID order/normalization/deduplication, same-zone skipping, centroid/anchor preferences, path requests, short-route filtering, population/metadata lookup, route construction and incremental appends. The destination list is a method argument propagated through generation calls; the helper has no route-list field, selection state, or second route store.

**EvacuationPathfinder**, also an `internal sealed class`, owns A*, bounds, live tile/flood queries, passability/costs, normalized/cell conversion and reconstruction. Each search creates its own open/closed sets, scores and path. The helper retains no search results, gameplay state or cross-rebuild cache. Its internal RouteTileSnapshot and road/passability/coordinate methods provide the small shared seam needed by anchor and shelter resolution.

The helpers receive explicit provider references and scalar settings in constructors and are short-lived per rebuild. Constructors assign fields only; they do not query providers, validate again, discover objects or generate routes. Map contents, water arrays, zone sets, origins and Tilemap queries remain live and occur within the original algorithm. No helper holds or calls back into EvacuationController. No MonoBehaviour, ScriptableObject, event bus, singleton, adapter hierarchy or serialized helper field was introduced. The existing internal CommunicationTowerGenerator supplied the nearby plain-helper pattern.

## 4. Method-level extraction mapping

Names on the left refer to the pre-E2 controller. Full extracted method bodies were compared mechanically, with only destination-parameter and helper-reference qualification normalized for the comparison. Runtime output comparisons do not normalize anything.

| Original method/declaration | Current owner / delegation |
| --- | --- |
| RebuildAvailableRouteData | Facade retains in-place clear and unchanged reference validation; remaining enumeration/logging moves to Generator.GenerateRoutes. Facade returns true after generation, including zero routes. |
| BuildRoutesForAllActiveShelters | Generator, now receives the existing destination list as a parameter. |
| BuildRoutesForShelter | Generator, same incremental append point; destination parameter instead of facade field access. |
| IsShelterInsideZone | Generator, same ZoneController membership query. |
| TryFindZoneCenterAnchor | Generator, same HashSet traversal/comparator and pathfinder conversion seam. |
| CalculateZoneCenter | Generator, unchanged body and float expression order. |
| TryGetAnchorTilePreference | Generator, delegates live snapshot/passability/road checks to Pathfinder; building/land preference stays here. |
| TryGetShelterNormalizedTile | Generator, same stored-cell then world-position fallback; shared inverse/bounds check in Pathfinder. |
| GetSourceZonePopulation | Generator internal static method with explicit baseline provider; unchanged lookup body. Facade retains a private forwarding method for GetKnownZonePopulation. |
| GetSafeShelterId / GetSafeShelterType / GetSafeShelterCapacity | Generator, unchanged bodies and fallbacks. |
| BuildRouteId | Generator, unchanged interpolated trimmed-ID construction. |
| IsBuildingCategory / IsLandCategory | Generator, unchanged substring tests. |
| NormalizeGeoid | Private copy in Generator; the facade's existing private method remains byte-identical for state/query consumers. No new shared utility file. |
| ZoneAnchor | Private Generator struct, unchanged fields. |
| TryBuildPath | Pathfinder internal entry point; facade's original private implementation is removed. Generator calls Pathfinder directly. |
| TryGetMovementCost | Pathfinder, unchanged body. |
| TryGetRouteTileSnapshot / IsPassable | Pathfinder internal shared seam; same bodies; IsPassable is static because it uses only its argument. |
| ReconstructPath / ConvertNormalizedPathToCells | Pathfinder, unchanged bodies and ordering. |
| NormalizedTileToCell / TryCellToNormalizedTile | Pathfinder internal shared seam; unchanged conversion/bounds bodies. |
| TryGetTileInstance / TryGetWaterDepthFromGrid | Pathfinder, unchanged live reads and exception handling. |
| RemoveLowestScoreTile | Pathfinder, unchanged List scan/removal and tie rules. |
| GetHeuristicCost / GetScore / ManhattanDistance | Pathfinder, unchanged bodies. |
| IsWithinMap | Pathfinder, unchanged N/sizeX/sizeY rule. |
| IsRoadCategory / IsWaterCategory / NormalizeCategory | Pathfinder; road test internal for generation reuse, bodies unchanged. |
| RouteTileSnapshot / NeighborOffsets | Pathfinder, unchanged fields and +X, −X, +Y, −Y array order. |
| CreatePathfinder | New private facade factory, supplies existing providers/settings without performing provider queries. |

Verification confirms **18 pathfinder method bodies are identical** and **15 generator method bodies match** after the explicit parameter/reference forwarding substitutions. The latter includes NormalizeGeoid's copied body and the static population method's unchanged body. **62 retained controller methods are byte-identical**, including both ShowOnlySelectedRoute overloads and every public action/query, lifecycle method, selection/mitigation/input/presentation method, discovery routine and subscription handler. Only RebuildAvailableRouteData and the private population forwarding method have changed retained bodies.

## 5. Public API preservation

The global public `EvacuationController : MonoBehaviour` name and namespace are unchanged. Source declaration comparison confirms all **5 properties, 22 public methods including overloads/aliases, and 6 public events** are unchanged. All 15 event Invoke sites remain in the same facade methods with identical source and order. No notification moved into a helper.

The entire `[Serializable] public class SimpleEvacuationRoute` source is byte-identical: all 11 public fields, List initializer, global type, mutability and reference semantics remain. CurrentRoutes and GetAvailableRoutes expose the same existing list, while GetSelectedRoutes still returns a new list in available-route order.

Managed identity assertions verify that provider callbacks see the same exposed list, the visualizer caches the actual appended route object, and the shared tooltip receives that same route reference. A successful rebuild still creates new route records in the retained list; it does not recycle old records or substitute copies.

The seven external consumer files identified in E1 are untouched and compile in the full current runtime build. Visualizer/tooltip/public controller bindings did not need changes. Helper types/methods are internal and introduce no public gameplay API.

## 6. Serialized GUID and Inspector preservation

| Component | Script GUID | PlayableTilemap component / GameObject fileIDs | Result |
| --- | --- | --- | --- |
| EvacuationController | `ef31d31705ac48f898e5746ce90d32fa` | `315251714` / `315251712` | Original enabled component and bindings retained |
| EvacuationRouteVisualizer | `37785d2c97374ef3abdc0d8dc1ba82b6` | `1117061671` / `1117061669` | Source, metadata and bindings unchanged |
| EvacuationRouteTooltipController | `908e8eaeef6084fcf840ee5b5e6f5cad` | `817002036` / `817002034` | Still enabled and serialized; source/metadata unchanged |

All **23 controller serialized declarations** match the original exactly, including names, types, SerializeField/Min attributes and initializers. Header attributes and all runtime/state field declarations also match. No serialized helper references or new scene components were added. The controller's metadata, the entire PlayableTilemap scene, every other scene/prefab, and all Inspector serialization are byte-identical to the starting working tree.

The scene still assigns its original baseline, shelter, zone, map, route Tilemap, visualizer, UIDocument and camera references. The shared map tooltip reference remains null in scene YAML for runtime discovery/creation, and the separate legacy tooltip retains its existing document. E1's saved scene values, including preview1, landCost3, waterCost5, selected limit disabled/max3, and lock enabled, remain untouched. Awake still forces preview false.

No native Unity scene was run. The intentionally deleted Dev_UINavigation scene and `.meta` remain deleted.

## 7. Route-generation ordering verification

Frozen/current execution compares actual route IDs, source populations, capacities, anchors, shelter cells, complete ordered path-cell lists and hop distances. The fixtures retain shelter-major, associated-zone-minor order: A_to_S1, B_to_S1, A_to_S2, B_to_S2. Trimmed repeated/empty/unknown GEOIDs yield the same omitted routes. No list, GEOID set, membership HashSet, candidate or route sort was added.

Anchor tests cover distance before category, road-vs-building ties and equal-category ties in both HashSet insertion orders. The exact squared-distance/Mathf.Approximately rejection expression and floating-point centroid accumulation were moved unchanged. Short filtering still rejects strictly less than the minimum, accepts equality and omits records entirely. Same-zone shelters still skip before anchor/path generation. Safe IDs/types/capacities/populations, including duplicate `Unknown Shelter` route IDs, retain their original behavior.

Two instrumented provider traces compare every ordered GetActiveShelters, zone lookup, cell conversion, TileMapData.Get, Tilemap.HasTile, baseline Ensure/TryGetRiskData and world-to-cell call in partial generation and negative-origin world fallback. Both full query sequences match byte-for-byte. No query is performed in the helper constructors and no whole-map snapshot replaces per-cell reads.

Population callbacks observe list counts **0 then 1** before each route append. A callback-triggered nested Refresh still yields the original final route order **A_to_S1, B_to_S1, A_to_S1, B_to_S1**, with inner Built→Changed before outer Built→Changed. No duplicate repair, transaction or re-entry guard was introduced. A population callback that floods a cell after the first path is computed leaves that first path unchanged; later generation sees live map state.

## 8. A* behavior verification

All 18 moved pathfinder bodies compare identical to the original controller bodies. Managed execution retains:

- List open set, +X/−X/+Y/−Y neighbors, strictly smaller f-score scan replacement, first equal-score/equal-cost predecessor and no closed-node reopening.
- Manhattan heuristic times min(road,land), neighbor destination cost, weighted road detours and hop-count distance rather than gScore.
- Strict `depth > threshold` rejection; equality traversable; max of nonnegative TileInstance/grid depths; category and tileType water rejection. Serialized waterTileCost remains unused for traversal.
- Nonnull TileInstance passability even when the route Tilemap is empty; sparse Tilemap fallback when instance is absent; rejection when both are absent; the existing low-level null-Tilemap generic-land fallback; IndexOutOfRangeException-only recovery. A controlled NullReferenceException still propagates.
- Negative provider origins and separate route Tilemap behavior, original normalized/cell timing, stored-cell then world-position fallback, no second offset and no flood-grid padding.
- N overriding both dimensions; rectangular sizeX/sizeY bounds when N≤0; same-cell one-node success; iteration-cap failure and success when the goal is popped on the final permitted iteration.
- Exact cap-warning text and existing `[EvacuationController]` diagnostic prefix even from helper code.

No diagonal, priority queue, sorting, smoothing, safety weighting, traversal cache, water subscription, new fallback or broader exception handling was added.

## 9. Failed and successful rebuild behavior

The unchanged facade validation still runs **after clearing the original live list** and warns for all five missing required references. An instrumented warning callback confirms list count0 at all five warnings. Rebuild failure still returns false and preserves selected IDs, confirmation and previous mitigation; Refresh skips cleanup/recalculation and publishes Changed without Built, just as before.

A null/empty active-shelter list is still successful generation. The facade returns true, performs its original stale-ID cleanup and mitigation recalculation, then publishes Built→Changed. Successful zero-route cleanup removes confirmation and selected IDs without Deselected. The generator does not own/reset gameplay state or choose event behavior.

Successful rebuilds preserve still-valid selected/confirmed IDs, replace route objects in place, and recompute the same greedy mitigation. Hide/disable/preview placement suppression retain their original state and notification behavior. The ZoneController one-attempt discovery policy and shelter subscription timing remain in unchanged facade methods.

## 10. Baseline versus current compilation

The E1 replay was executed **before production edits**, with the original controller verified byte-identical to EvacuationController.baseline.cs. It passed its original compilation, 79 assertions and 14 traces. The frozen baseline implementation/fixture sources were not edited. A second comparison compiled the pre-E2 controller and current controller against the same current external references.

| Build | Runtime sources | Errors | Warnings |
| --- | --- | --- | --- |
| Original E1 replay before edits | 207 | 0 | 11 |
| Preserved pre-E2 controller / full baseline runtime | 207 | 0 | 11 |
| Extracted current runtime | 209 | 0 | 11 |

The complete current runtime source set was re-enumerated using the existing Editor and asmdef/asmref exclusions. Both new helper files are included; all seven complete consumers and the unchanged presentation scripts compile. Existing Unity6000.0.63f1 Roslyn/NetCoreRuntime and project Unity/package references are reused with the same complete compiler options. Generated .csproj/Library artifacts were not manually edited.

Warning message multisets are identical: CS0105×1 duplicate using, CS0618×8 obsolete APIs, CS0414×2 existing assigned-unused fields. No new error/warning or new suppression was introduced. Controlled harness compilations separately have the same two expected CS0649 warnings for visualizer Inspector reference fields; these are not the runtime-assembly diagnostic count.

This is a managed C# compilation result, not a native Unity import, IL postprocessing result, clean package rebuild or platform player build.

## 11. Regression assertion counts and ordered traces

Both frozen and current versions were actually executed. No route/event/data output normalization, ID remapping, sorting or warning-message rewriting was applied.

| Coverage | Assertions per implementation | Matching traces | Result |
| --- | --- | --- | --- |
| Original E1 fixtures | 79 | 14 | Entire output equals the original recorded E1 output byte-for-byte |
| Additional E2 fixtures | 36 | 9 | Entire expanded frozen/current output byte-identical |
| Unique combined coverage | **115** | **23** | Passed on both implementations |

The original 14 traces cover negative-origin generation, equal-score paths, selection replacement, ordered allocation, hidden clear, confirmation, stale cleanup, failed rebuild, anchor choice, line pooling, input, multiple shelters, weighted detour and shader fallback. All match the saved E1 output exactly.

The additional 9 traces cover partial generation with complete ordered provider calls; nested refresh; live map mutation during population callbacks; negative-origin world fallback with complete calls; missing-data/exception behavior; exact iteration warning; all-dependency validation warnings; successful null-shelter cleanup; and duplicate-safe-ID/metadata behavior. Additional assertions also check public list/route/tooltip identity, equal anchor ties, rectangular bounds, selected/confirmed persistence and the lazy population getter.

Allocation remains A(pop100), B(pop80), S1(cap120) → A100/B20/total120/mitigation1 and0.25, irrespective of click order; adding S2(cap50) yields A100/B70/total170/B mitigation0.875. Original input, visualizer pooling/styling/selected-only behavior and controller-to-shared-tooltip calls run in the harness and match. No presentation source was changed to make tests pass.

**Private test seam adaptation:** E1 directly reflects the private controller TryBuildPath method. Current-side test code redirects only that removed private seam through CreatePathfinder to the extracted TryBuildPath. Baseline-side E1 tests remain original. The expanded common fixtures use the same reflection bridge for either implementation, falling back only when the original private method is absent. The route/path assertions and all14 original trace formats/expected values are unchanged. This is temporary harness plumbing, not a retained production wrapper or a public API change.

Evidence directory:

`/private/tmp/evacuation-e2-4jvo3cri`

It contains initial hashes/status, the preserved pre-E2 controller, extraction mapping, frozen/current compiler response files and logs, original and expanded fixture sources, exact outputs, results.json, extra-results.json, verification.json and final status. Run from repository root while the temporary E1/E2 evidence remains available:

```sh
python3 /private/tmp/evacuation-e2-4jvo3cri/replay.py
```

The replay rebuilds both runtime assemblies, reruns the original baseline/current comparison and expanded comparison, and verifies the recorded equality/counts. It writes only temporary evidence files. The original E1 frozen source and fixture files are read-only inputs. These fixtures are not a durable CI suite and may be removed by the OS.

## 12. Unavailable or untested evidence

Native Unity Editor import, Play Mode and player build were **not performed**. Only PlayableTilemap is authorized for native gameplay follow-up; no scene was launched in this stage. No real UI Toolkit layout, mouse scheduling/camera projection, prefab component lifecycle or shader rendering result is claimed from managed tests.

The harness executes the actual controller and visualizer with controlled Unity/map/zone/shelter/baseline/shared-tooltip stand-ins. It compares synchronous public callback re-entry and provider query order, but does not run the entire JsonMapLoader/Zone/Shelter/Baseline systems or instantiate all seven UI consumers. Complete source compilation and byte preservation complement these controlled behavioral checks. Actual MapHoverTooltipController and legacy EvacuationRouteTooltipController UI creation/layout were not executed; their source, metadata, UXML and scene bindings remain unchanged.

No new native verification result is inferred from earlier records. No broad claim about every possible external mutation, real-time scene lifecycle, package or platform is made. A future PlayableTilemap smoke test should check generated routes, first-route lock/advisor/turn response, hover tooltips, hide/show persistence and shelter-placement suppression without saving scene mutations.

## 13. Scope and working-tree preservation

Starting git status was captured before edits. A SHA-256 manifest covered **6300 existing files** under Assets, Packages, ProjectSettings and UserSettings, including existing untracked refactor work and the E1 report. Final comparison confirms that the **only existing file changed by E2 is EvacuationController.cs**; all other 6299 existing files are byte-identical. The seven new files are exactly those listed in §2. No starting file was deleted. Each new metadata GUID is unique among current asset metadata.

Source checks confirm all 23 serialized controller fields, all public declarations, the complete SimpleEvacuationRoute definition and runtime state fields are unchanged. All15 event Invoke sites remain in C; 62 retained methods are byte-identical; 18 pathfinder bodies and 15 generator bodies have the extraction equivalence described in §4. The authorized scene's three original script GUID/component identities still resolve, and the scene is byte-identical.

The final git-status delta contains only the authorized controller modification, new Dev_EvacuationSystem folder/metadata and E2 report/metadata. Every pre-existing modified, deleted or untracked entry remains as it was, including unrelated CommunicationSystem work and the intentionally deleted Dev_UINavigation scene/meta. The E1 report was not rewritten. Whitespace checks pass for the controller/helper sources and report.

## 14. Known behavior quirks deliberately preserved

- Failed validation clears available routes but can retain selected/confirmed IDs and old mitigation; successful zero-route generation clears stale state without Deselected.
- Re-entrant baseline callbacks can see/alter a partially rebuilt list and nested rebuilds can leave duplicate route records. No transaction or recursion guard was added.
- HashSet encounter order affects anchor ties, overlap precedence and the visualizer's single visible selected ID. Multiple selected routes can remain in the controller's hit-test list while only one is drawn.
- Clearing selection while preview is hidden can redraw cached visualizer lines. Cached visual routes survive clear calls.
- The first new selection confirms before Selected listeners run when locking is enabled. Single-slot replacement publishes intermediate deselection/change before new selection/change. Repeated selection remains silent.
- Controller routeColor and waterTileCost remain serialized even though current generation does not use them. No water-triggered route invalidation was added.
- The population getter can lazily calculate baseline risk; event/subscription order and optional discovery remain as audited.
- The enabled legacy tooltip and shared MapHoverTooltipController remain separate. Missing Dev asset fallback paths and communication/evacuation host competition were not fixed or redesigned.

## 15. Readiness for Stage E3

**Stage E2 is complete.** Both required helper extractions are implemented in this single stage, with matching frozen/current managed behavior, unchanged public/serialized contracts and no new runtime diagnostics. No additional E2 subdivision or approval checkpoint was introduced.

The facade still owns every state, selection/confirmation, mitigation, input and presentation responsibility reserved for subsequent stages. E3 can start from this documented boundary with its own explicit source scope and the preserved baseline evidence. E3, E4 and E5 were not implemented.

Review and commit the E2 checkpoint while keeping the unrelated working-tree changes separate.
