# ZoneSystem — Stage 5D1: Renderer ownership

ZoneController now initializes, configures and disposes one ZoneOutlineRenderer, independently of the legacy hover component. Both scenes explicitly retain Canvas/ZoneBoundaryLine as the rendering parent and preserve the original template, material and appearance values. The legacy component remains enabled for hover and barrier visibility policy. Destroying it releases transient suppression without destroying persistent resources. Public outline signatures, channel contents/masks, geometry and shared BaselineRisk behavior are preserved. Compilation passed with no new diagnostics; temporary source-based checks passed. Unity Play Mode was not executed.

## Ownership and lifecycle

Previously, legacy Awake/Start registered a helper through ZoneController, using the legacy component's transform as parent. Legacy RefreshOutlineSettings configured appearance and visibility. Legacy OnDestroy unregistered and disposed it; ZoneController.OnDestroy only discarded references. Availability depended on that legacy owner.

Now:

- ZoneController.Awake calls EnsureOutlineRenderer. Its Start retries once if no helper exists. Legacy registration may also invoke the same initialization path, covering either Awake order. Repeated initialization returns the existing helper and pools.
- Initialization requires an explicitly assigned, same-scene outlineRoot. Missing/cross-scene roots produce a bounded diagnostic and allocate neither a helper nor a replacement root. There is no root discovery fallback. Public requests and availability getters do not initialize or search.
- ZoneController owns appearance configuration at initialization and in Update. Existing pool slots retain creation-time width/material/sorting settings; new slots use current controller settings, matching the previous configuration model. Each show still supplies its own color/endpoints/Z.
- TryRegisterOutlineRenderer lends that helper to one same-scene compatibility bridge. A duplicate bridge or foreign helper is rejected without replacing resources. Bridge identity is independent of renderer availability and lifetime.
- Legacy OnDestroy only unregisters and drops its references. Unregistration clears stale Hover, releases transient suppression, and restores cached persistent visibility while retaining each requested mask and persistent active count. It does not dispose or destroy generated objects.
- Component disable has no new cleanup: cached pools and the last transient visibility policy remain until updates resume. Deactivating the rendering root hides its children through the existing hierarchy. Neither action disposes resources.
- ZoneController.OnDestroy prevents reinitialization, detaches references and disposes its helper once. Existing idempotent helper disposal removes generated lines and any owned fallback material; assigned template, material and root survive. Either destruction order is safe.
- Renderer availability now also requires its original parent to exist. Both Show overloads reject a disposed helper or missing parent, preventing later draws from creating orphan root-level lines. Destroyed roots are not automatically replaced.

## Exact source changes

| File | Change |
| --- | --- |
| Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneController.cs | Eight serialized rendering fields; owned initialization/configuration/disposal; bridge-only registration and transient-policy release; updated availability diagnostic |
| Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneOutlineRenderer.cs | Parent-aware IsAvailable and guards in both Show overloads; pooling, visibility and disposal algorithms otherwise unchanged |
| Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs | Removed appearance configuration and disposal calls; compatibility show paths require controller availability. Six old appearance fields remain serialized but hidden and unused; hover color remains editable. A narrowly scoped CS0414 suppression covers four intentionally unread initialized compatibility fields |

Legacy Awake/Start/controller resolution, Update, public compatibility methods, geometry fallbacks, hover color/enabled state and both barrier flags remain. The entire Update and compatibility-method region is byte-identical. Controller map/index/geometry methods and public outline method implementations/signatures are unchanged. No new public API was needed.

All seven channels remain: Hover, BaselineRisk, LiveFloodRisk, ShelterCandidateZones, ShelterCandidateTiles, PlacedShelterZones and CommunicationZones. Transient hiding still uses ConfigureVisibility/SetPersistentVisibility, independently of RequestedVisible. The existing four barrier-flag combinations, including the nondefault timing quirk, remain unchanged while the bridge is active. Shared BaselineRisk remains last-writer/any-clear.

## Scene assignments and appearance

Only these scene files changed:

- Assets/Game/Content/Scenes/PlayableTilemap.unity
- Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity — matching minimal compatibility assignments only.

Each scene adds exactly eight entries to existing ZoneController component 2070746514:

| Controller field | Preserved assignment/value |
| --- | --- |
| outlineRoot | RectTransform 704415075, Canvas/ZoneBoundaryLine (GameObject 704415074) |
| linePrefab | Existing scene LineRenderer 1963393216, Prefabs/ZoneLinePrefab |
| lineMaterial | Existing ZoneLineMaterial.mat, GUID 5c951169c35ea49eab614112f3848aa0, fileID 2100000 |
| lineColor | RGBA (1, 0.85, 0.15, 1); creation default. Actual hover color remains on the legacy component |
| lineWidth | 0.08 |
| zOffset | -0.1 |
| sortingLayerName | Default |
| sortingOrder | 1000 |

YAML parsing confirmed 345/359 records, valid local file references, and no duplicate component entries. Removing these eight additions reproduces each starting scene byte-for-byte. Root name, hierarchy, transform, activation, components, legacy component 704415076 and all prior serialized references are unchanged. Both components remain enabled. No other saved scenes/prefabs/assets reference the ZoneController script GUID.

## Validation

- Compiled all 202 runtime sources with the installed Unity 6000.0.63f1 Roslyn compiler and existing project response-file references: **0 errors, 11 existing warnings**, identical to Stage 5C. Source coverage matches Assembly-CSharp.csproj. This is a standalone compilation, not a Unity import or Play Mode run.
- Temporary harness compiled the actual before/after controller, renderer and legacy sources, plus actual index/geometry/contracts, against Unity stand-ins. All **163 line-state snapshots matched exactly** across seven channels, four barrier-flag combinations, masks, hover enabled/disabled/cache/miss, negative origins, hidden replacement/clear, map readiness and legacy compatibility entry points.
- Additional lifecycle assertions passed: initialization without the bridge, repeated Awake/Start/registration reuse, duplicate/foreign/cross-scene rejection, disabling/deactivating, bridge destruction while hidden, cached masks, replacement bridge, controller-first/bridge-first teardown, single destruction of generated objects/fallback material, retained assigned assets, missing/lost root and bounded retry. Controller appearance remains authoritative; pool growth preserves creation-time settings.
- Compared against a starting snapshot of 6,135 Assets files. Authored changes are limited to the three allowed sources, two scenes and this report. All existing metadata/GUIDs and unrelated assets remain unchanged. Tests/build outputs are outside Assets.
- A new report .meta appeared during Unity import after this report was created; it was left intact. This task did not write or modify metadata.

## Required manual checks — PlayableTilemap only

1. Inspect ZoneController's eight assignments above. Keep ZoneThinOutlineByHover enabled on the unchanged Canvas/ZoneBoundaryLine. Change renderer appearance on ZoneController; hover color/flags remain on the legacy component.
2. Enter Play Mode. Check Console and normal hover/miss; exercise baseline risk, live flood, shelter candidate zones/tiles, placed shelter coverage and tower hover. Compare color/alpha, width 0.08, Z -0.1, sorting and root children against the prior verified stage.
3. Enter/exit Place Barriers: Hover clears, persistent lines hide without losing cached data, and eligible lines return. Test explicit channel masks and show/clear while hidden. In temporary runtime Inspector changes, check all four hide-flag combinations; preserve the previous nondefault behavior.
4. Grow and reuse pools; repeated toggles must not duplicate lines. Disable/re-enable the legacy component, then its GameObject separately, checking retained pools and normal hierarchy visibility.
5. In a disposable Play session with persistent lines visible/hidden and one channel explicitly masked, destroy only the legacy component. Persistent rendering and controller submissions must remain usable; stale Hover clears, masks survive and the line objects remain. Do not save runtime changes.
6. In another disposable session, destroy ZoneController. After Unity's deferred destruction completes, generated lines must be gone while the assigned root/template/material remain. Check both destruction orders and missing-root diagnostics without creating a new root.

No Dev_UINavigation Play Mode testing was performed or is required for this stage. Remaining risk is Unity-native lifecycle/rendering parity pending the checklist; stand-ins do not establish visual parity or deferred-destruction timing. Existing startup map readiness, shared-baseline ownership and bridge disable visibility behavior remain unchanged. Other scenes adding ZoneController in future must explicitly supply a same-scene root. No broader changes or blockers were identified. Review the scoped diff and commit after manual verification.
