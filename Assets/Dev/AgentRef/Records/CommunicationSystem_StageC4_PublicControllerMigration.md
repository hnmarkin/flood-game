# CommunicationSystem — Stage C4: Public Controller Migration

Date: 2026-10-23. The existing script and metadata were renamed in place to CommunicationController, retaining the original GUID. Six consumers now use that type; all 24 Inspector fields, 17 public signatures and five helpers are preserved. PlayableTilemap remains byte-identical to the C3 baseline, including four inbound references. Runtime compilation passes for 207 sources with zero errors and the same 11 warnings. Baseline and migrated managed runs each pass 348 assertions with 108 matching traces. Unity Editor import and Play Mode remain unverified; an ignored generated IDE project needs regeneration.

## 1. Files renamed, modified and created

Renamed together:

- `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs` → `Assets/Game/Features/MitigationFeatures/Communication/CommunicationController.cs`.
- Its existing `.cs.meta` → `CommunicationController.cs.meta`, without changing any metadata bytes.

The controller has exactly one source-token edit: its owning class identifier. The six consumers in section 5 contain the other 19 type-token edits. All seven source files compare byte-for-byte with their captured C3 versions after applying only these 20 identifier substitutions. Comments, string literals, method/field names and method bodies otherwise remain intact.

Created this report and its `.md.meta`. All five helper sources, their metadata and the helper folder metadata are unchanged. No scene, prefab, UXML/USS, gameplay algorithm, asset, map data or project setting was edited for C4. Verification programs, snapshots, compiler outputs and traces were kept outside the project in the temporary evidence directory in section 10.

The entry baseline captured 6,296 files under Assets, Packages, ProjectSettings and UserSettings plus Git status. Existing font, PlayableTilemap, layout and C1–C3 changes were preserved. The user's existing deletion of the former development scene and its metadata remains intact; neither was restored or run. The C4-specific rename/consumer changes are explicitly authorized by the stage request despite the general folder restrictions.

## 2. Original and final script GUID

| Check | Result |
| --- | --- |
| Original GUID | `98e604977a2f4ef5ac4190852d01e7fa` |
| Final GUID | `98e604977a2f4ef5ac4190852d01e7fa` |
| Original/final metadata SHA-256 | `1fd03dd987ebf5b944b9f54fc761d9c382c60919307878bc1ca1bfc44f88ebe0` |
| GUID owner | Exactly one metadata asset: `CommunicationController.cs.meta` |
| Old source/metadata paths | Both absent |

The original metadata file was moved. No replacement MonoBehaviour, compatibility alias/component, new script GUID, singleton or second communication authority was introduced. `CommunicationController : MonoBehaviour` is the sole communication gameplay facade; the global `CommunicationTowerData` and `ZoneCommunicationStatus` declarations retain their original identities and definitions.

## 3. Scene component identity verification

Scene: `Assets/Game/Content/Scenes/PlayableTilemap.unity`.

| Identity | C3 and C4 |
| --- | --- |
| MonoBehaviour fileID | `810699005` |
| Owning GameObject fileID | `810699003` |
| Script reference | `{fileID: 11500000, guid: 98e604977a2f4ef5ac4190852d01e7fa, type: 3}` |
| Communication script occurrences in scene | Exactly one |
| Complete scene SHA-256 | `482a2ccedd674f2b1af5cd095982d0b0a7de1bfb2b4faa7031bc5ee426dc0796` |

The four existing inbound components retain `communicationTowerController: {fileID: 810699005}`:

| Component fileID | Consumer |
| --- | --- |
| `515287497` | AdvisorPanelController |
| `1389987227` | ActionsPanelController |
| `2019949261` | GameStateSummaryBuilder |
| `2100000004` | CommunicationPanelController |

No new component was attached, and the original component was not removed or replaced. No scene YAML edit was needed. Runtime tooltip/resource-bar discovery remains as before; this scene has no additional saved inbound fields for those two consumers.

## 4. All 24 serialized fields and assignments

Names, declared types, source defaults and saved values below match the C3 capture. The consumers' existing lower-case `communicationTowerController` field names also remain unchanged.

| Field | Type | Source default | Saved Inspector assignment |
| --- | --- | --- | --- |
| `tileMapData` | `TileMapData` | `null` | `{fileID: 11400000, guid: 7e81a30567a6c1b44ae537c0db92678b, type: 2}` |
| `communicationTilemap` | `Tilemap` | `null` | `{fileID: 36957482}` |
| `zoneController` | `ZoneController` | `null` | `{fileID: 2070746514}` |
| `shelterController` | `ShelterController` | `null` | `{fileID: 1420635165}` |
| `mainCamera` | `Camera` | `null` | `{fileID: 917516086}` |
| `mapHoverTooltip` | `MapHoverTooltipController` | `null` | `{fileID: 0}` |
| `tooltipUIDocument` | `UIDocument` | `null` | `{fileID: 2082171433}` |
| `communicationPanelController` | `CommunicationPanelController` | `null` | `{fileID: 2100000004}` |
| `mainTowerSprite` | `Sprite` | `null` | `{fileID: 21300000, guid: 7e25c158da0c7974cbf783bc599cbba3, type: 3}` |
| `zoneTowerSprite` | `Sprite` | `null` | `{fileID: 21300000, guid: 984ce4b18bfef434881def49d974e4c6, type: 3}` |
| `towerVisualRoot` | `Transform` | `null` | `{fileID: 1688803249}` |
| `inactiveTowerColor` | `Color` | `new(0.62f, 0.86f, 1f, 0.85f)` | `{r: 0.62, g: 0.86, b: 1, a: 0.85}` |
| `activeTowerColor` | `Color` | `new(0.25f, 1f, 0.55f, 1f)` | `{r: 0.25, g: 1, b: 0.55, a: 1}` |
| `hoveredTowerColor` | `Color` | `new(1f, 0.95f, 0.35f, 1f)` | `{r: 1, g: 0.95, b: 0.35, a: 1}` |
| `associatedZoneHoverColor` | `Color` | `new(0.2f, 0.85f, 1f, 1f)` | `{r: 0.2, g: 0.85, b: 1, a: 1}` |
| `towerVisualScale` | `float` | `0.45f` | `0.45` |
| `towerHeightOffset` | `float` | `0.35f` | `0.35` |
| `towerSortingLayerName` | `string` | `"Default"` | `Default` |
| `towerSortingOrder` | `int` | `2300` | `2300` |
| `zonesPerTower` | `int` | `3` | `3` |
| `towerSearchRadius` | `int` | `4` | `4` |
| `showInactiveTowersAtStart` | `bool` | `true` | `1` |
| `avoidShelterTiles` | `bool` | `true` | `1` |
| `debugLogs` | `bool` | `true` | `1` |

No FormerlySerializedAs migration is required because no serialized field was renamed. Whole-scene byte equality also covers unrelated serialized values and references.

## 5. Six-consumer migration inventory

| Actual repository path | Necessary changes | Type-token edits |
| --- | --- | --- |
| `Assets/Game/Features/MitigationFeatures/Communication/CommunicationPanelController.cs` | Serialized field type and discovery generic argument. | 2 |
| `Assets/Dev/Scripts/Zones/MapHoverTooltipController.cs` | Serialized field type, existing setter parameter type, two discovery generic arguments. | 4 |
| `Assets/Dev/Scripts/PanelController/ActionsPanelController.cs` | Serialized field type, two discovery generic arguments, fallback return/local types and AddComponent argument. | 6 |
| `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs` | Serialized field type, discovery generic argument and missing-reference `nameof`. | 3 |
| `Assets/Dev/Scripts/Main_UI/UI_Overlay/Dev_ResourceBarRuntime/Scripts/ResourceBarRuntimeController.cs` | Serialized field type and discovery generic argument. | 2 |
| `Assets/Game/Features/UI/Scripts/UI_Advisor_Scripts/AdvisorPanelController.cs` | Serialized field type and discovery generic argument. | 2 |

No additional active source consumer was found. All calls, subscription/unsubscription bodies and lifecycle scheduling remain unchanged. The tooltip setter remains named `SetCommunicationTowerController`, now accepting `CommunicationController`. UI structure, tabs, population calculations, warning selection/submission, buttons, sprites, evacuation compatibility, advisor wording, resource cooldowns and summary calculations were not refactored.

## 6. Dynamic discovery and creation migration

Actions retains its two existing call sites into one fallback factory: `ResolveReferences` and the late-resolution branch in `ToggleCommunicationMode`. Each first uses `FindFirstObjectByType<CommunicationController>()`; only a null result reaches the existing factory, which now uses `AddComponent<CommunicationController>()`. Cached references are reused. There was no GetComponent-based communication path to update in this consumer.

The private factory's existing method name and the fallback GameObject's display name remain unchanged. A GameObject named `CommunicationTowerController` now carries a `CommunicationController` component; the label does not resolve or instantiate a type. Managed probes verify existing-scene discovery, repeated resolution, late Toggle discovery, both fallback entry paths, reuse by another Actions consumer, one created controller and single mode-event subscription. Provider-unavailable generation retains the original mode/failure behavior.

Other consumers retain their existing discovery timing. The tooltip still resolves its status provider on demand, and the controller still configures it through the existing setter. The five helpers retain the same dependencies, borrowed presentation callbacks and state ownership. Generator construction remains per attempt; no helper implementation changed.

## 7. Public API compatibility inventory

All 17 facade signatures remain, with only their owning class identity changed:

```csharp
bool IsCommunicationModeActive { get; }
IReadOnlyList<CommunicationTowerData> CommunicationTowers { get; }
event Action<bool> CommunicationModeChanged;
event Action<CommunicationTowerData> OnTowerActivated;
event Action<string> OnZoneWarned;
void ToggleCommunicationMode();
void SetCommunicationMode(bool active);
void ShowCommunicationTowers();
void HideCommunicationTowers();
bool TryGetCommunicationStatus(string geoid, out ZoneCommunicationStatus status);
IReadOnlyList<CommunicationTowerData> GetAllTowers();
IReadOnlyList<CommunicationTowerData> GetActiveTowers();
ZoneCommunicationStatus GetZoneCommunicationStatus(string geoid);
Dictionary<string, ZoneCommunicationStatus> GetAllZoneCommunicationStatuses();
void MarkZoneWarned(string geoid);
void NotifyActionButtonConnected();
void RegenerateCommunicationTowers();
```

The existing Regenerate context-menu attribute is unchanged. Reflection checks confirm 12 public non-accessor methods, two properties and three events. The same five Unity lifecycle methods, private helper fields, orchestration and synchronous event semantics remain. All helper types remain internal plain classes. The facade owns no additional tower/status/visual/hover collections.

Activation still marks active → updates visuals → applies Pending transitions → publishes OnTowerActivated synchronously → opens the panel. Repeated selection reopens the panel without another activation event. No controller method body changed.

## 8. Remaining legacy-name search results

A fresh before/after sweep covered runtime/editor/test C#, serialized scenes/prefabs/assets, reflection/type strings, UnityEvent bindings, assembly definitions/references, packages, project settings and text tooling configuration. The post-sweep also examined ignored generated project files separately. C# identifier scans exclude comments/string literals and distinguish preserved member names from obsolete type tokens.

| Category | Result |
| --- | --- |
| Old type declarations, aliases and compile-time references | None in active runtime/editor/test source. |
| Old typeof/AddComponent/GetComponent/FindFirst/FindAny type usages | None. |
| Serialized old type/assembly-qualified strings and old UnityEvent type bindings | None in project assets. |
| Old source path in maintained tooling/asset configuration | None. |
| Script GUID references | Renamed metadata and original scene script binding; no duplicate metadata owner. |
| Historical reports | C1–C3 retain their stage-specific names and paths; untouched. |
| Ignored generated IDE project | `Assembly-CSharp.csproj:215` still lists the old source path. This generated cache is excluded by `.gitignore:11`; regenerate Unity's project files after import. It was left untouched. Managed compilation enumerated actual sources, including the new path. |

Intentional residual names in active C# are non-type identifiers/labels:

- Existing diagnostic text: controller (17 lines), generator (1), visuals (1), presentation (2), CommunicationPanel (1), Actions (2).
- Actions' existing private `CreateRuntimeCommunicationTowerController` name (declaration and two calls) and fallback GameObject label (1).
- Tooltip's existing public `SetCommunicationTowerController` name and its controller-side call (one each).
- Summary and Advisor `_subscribedToCommunicationTowerController` identifiers (five lines each).
- The six serialized `communicationTowerController` field names and usages intentionally retain their original spelling.

These do not require the removed class to exist. No reflection lookup uses a retained diagnostic/GameObject string as a type name. C5 can review diagnostic labels and regenerate IDE caches while preserving serialized/public method contracts.

## 9. Compilation results

| Managed build | C3 baseline | C4 |
| --- | --- | --- |
| Relevant runtime sources | 207 | 207 |
| Errors | 0 | 0 |
| Warnings | 11 | 11 |

Used the installed Unity 6000.0.63f1 Roslyn compiler and existing Unity/package/assembly references. Sources outside Editor folders and separately defined assemblies were enumerated using the same boundary as C3. Their existing assembly outputs supplied references. All six full consumer files were included in this runtime compilation.

The warning message multiset is identical: one CS0105 duplicate using, eight CS0618 obsolete API uses, and two CS0414 unused fields (`CardLoader.disabledAlpha`, `GameStateSummaryBuilder.logDisplayIdMappings`). No new warning or compiler error was introduced. Both extended managed harness compilations also pass with zero errors/warnings.

## 10. Regression comparisons

Fresh C3 snapshots were taken before mutation; the existing C2/C3 harness was rerun against that baseline and the migrated source. Each extended run passes **348 assertions**: all **303 existing C2/C3 assertions** plus **45 C4 assertions**. All **108 captured states/traces** match: 99 C2/C3 and nine C4.

The sole normalization is the intentional owner identity in warning metadata: `:context=CommunicationTowerController` → `:context=CommunicationController`. No message, state, UI count, event ordering, tower data or other difference is normalized. The runtime source path change is accounted for in compiler inputs, not hidden in gameplay comparisons.

Coverage:

- C2: deterministic grouping/IDs/cells/world positions, map coordinate offsets and category/water/shelter avoidance, retry bounds/readiness/failure, state and warning normalization, mode/visibility, activation ordering, regeneration and disable/re-enable behavior.
- C3: sprite combinations/fallbacks, colors/scale/height/sorting, destruction requests, pointer/click order, hover outlines/cleanup, shared tooltip resolution/configuration, panel requests, missing dependencies and lifecycle behavior.
- C4 API: reflected signature/field counts, internal helper shape, tower query contracts and copied status snapshots.
- Actions: existing/cached discovery, late discovery, both fallback entry points, no duplicate authority, mode button event handling, unsubscribe/resubscribe.
- Panel: exact ShowForTower handoff/body, repeat selection, missing/null/unbound/late-bind behavior, selected-warning iteration, empty-ID skip, clearing/refresh/success output requests.
- Tooltip: exact communication-status method and setter, inactive/null/unknown/partially warned/all-warned states and lazy provider discovery.
- Advisor/resources/summary: exact communication event branches and handlers, duplicate-subscription guards, unsubscribe/resubscribe/opt-out, advisor message construction using the real display-ID formatter, resource turn/cooldown behavior, and actual communication-summary calculation at synchronous event time.
- Asset checks: GUID ownership, filename/class pairing, original scene component/GameObject, 24 assignments and four inbound fields.

The C4 consumer harness extracts **35 exact source methods or communication subscription branches** for each version into isolated probes. Unrelated UI/provider endpoints are stand-ins. Subscription call sites/lifecycle methods in the complete consumer files are unchanged by byte comparison and compile in the full runtime build; native lifecycle execution and UI Toolkit rendering are not simulated by these probes. The panel handoff is connected from the real facade/presentation through an instrumented output endpoint into the extracted ShowForTower body.

Temporary evidence directory:

`/var/folders/k0/k_vxwym14hs2n16m3n8bt2t40000gn/T/communication-stage-c4-p7aeq0xv`

Key evidence: `baseline-hashes.json`, `baseline-status.txt`, `baseline/`, `config.json`, `preservation.json`, `compilation.json`, `regression.json`, `consumer-regression.json`, `consumer-extraction.json`, before/after response files, compiler logs and managed trace logs. `replay.py` recompiles/runs both runtime and extended managed builds and compares normalized output. These temporary artifacts are not committed project assets and may be removed by the OS.

## 11. Unity serialization verification

Source/YAML validation passes: matching `CommunicationController.cs`/class identity, original metadata bytes/GUID, exactly one scene script binding, unchanged GameObject/component IDs, all 24 fields/defaults/assignments and four unchanged incoming serialized fields. The serialized asset refers to the script GUID rather than its old source filename; preserving that GUID and field names requires no YAML migration here. No obsolete assembly-qualified class name is saved in the scene binding.

These are the required source/serialization conditions for Unity to resolve the renamed class to the existing component. Actual Unity Editor import, Inspector rendering and confirmation of no native missing-script warning remain pending. No native import, scene save or Play Mode session was performed for C4.

## 12. Known limitations

- Managed engine/UI stand-ins validate logic and output calls, not native object lifecycle, domain reload, deferred destruction, rendering, hit testing, UI Toolkit event dispatch or visual layout.
- Consumer probes execute extracted communication methods/branches, not the six complete components in a live Unity scene. The complete sources are covered by compilation and exact type-only source comparison.
- Existing project assembly references were reused; this was not a clean Unity rebuild of all separate assemblies or an Editor assembly import.
- The ignored generated IDE project still requires regeneration; cached Library/editor type information was not rewritten. Historical docs and intentional member/diagnostic names remain as inventoried.
- The only retained gameplay scene was inspected as YAML. Its native binding and interactive gameplay need the C5 checks below.

## 13. Manual Unity Play Mode checklist

Use only `Assets/Game/Content/Scenes/PlayableTilemap.unity` for gameplay verification.

1. Allow Unity to import the rename, regenerate IDE project files, and confirm the new script compiles without new diagnostics. Check that the ignored IDE project now lists CommunicationController.cs.
2. Inspect the original communication GameObject: one CommunicationController, no missing script, component/asset GUID preserved. Compare all 24 saved assignments and four inbound consumer references above without resaving unrelated objects.
3. Enter Play Mode and confirm bounded startup generation, expected deterministic towers/served zones, terrain/water/shelter avoidance and initial visibility. Toggle Communication through Actions and confirm it reuses the original controller.
4. Hover inactive/active towers; verify sprites/colors/sorting, CommunicationZones outlines, shared tooltip statuses and evacuation tooltip compatibility. Confirm hover processing precedes click activation and mode exit clears only communication presentation.
5. Activate a tower: verify active visual and Pending status before synchronous subscribers update, panel opens with the same tower and activation fires once. Select it again and confirm the panel reopens without another activation/resource/advisor update.
6. Select zones and send warnings; confirm counts/population UI, Warned transitions, advisor messages, resource turn/cooldown and summary counts. Check the partial/all-warned tooltip states.
7. Disable/re-enable the relevant controllers/UI; verify original subscription cleanup/reconnection, no duplicate events and preserved mode/state behavior. Exercise regeneration and delayed/missing dependencies.
8. In a disposable runtime setup only, exercise each existing Actions fallback entry path with no discoverable communication component. Confirm it creates one CommunicationController and subsequent discovery/toggles reuse it; do not save an extra gameplay component to the scene.
9. Exit Play Mode and verify no unexpected serialized changes. Keep unrelated user changes and the existing development-scene deletion intact.

## 14. Stage C5 readiness assessment

C4 implementation and managed/source/YAML verification are complete. The rename is identity-only, all six consumers compile against the sole CommunicationController facade, helper ownership/implementations are unchanged, and deterministic managed behavior matches C3. No gameplay redesign, new authority or serialization migration was required.

Ready for C5 final verification and deliberate legacy-label cleanup. Remaining work is the native import/Play Mode checklist and regeneration of ignored IDE caches; historical reports and compatibility-sensitive field/public method names should remain intact. Review and commit the completed C4 changes as a checkpoint after checking the existing mixed working tree.
