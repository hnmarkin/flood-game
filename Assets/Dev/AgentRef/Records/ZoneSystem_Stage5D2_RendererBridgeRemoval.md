# ZoneSystem — Stage 5D2: Renderer bridge removal

Removed legacy renderer registration and borrowing. ZoneController independently initializes, configures and disposes its single helper; ZoneThinOutlineByHover resolves only the controller and sends hover, transient-policy and compatibility requests through it. All public signatures remain. Readiness fallbacks retain their prepared-segment behavior through an internal controller adapter. Scenes, Inspector values and renderer implementation are unchanged. Compilation passed with zero errors and 11 existing warnings; 183 before/after line-state comparisons and lifecycle checks passed with Unity stand-ins. No Unity Play Mode execution was performed for this stage.

## Previous and new paths

Stage 5D1 owned the helper in ZoneController but still required legacy Awake/Start registration to borrow it. The controller stored _outlineBridge, enforced registration identity and lent the helper back through an out parameter. Legacy methods called that helper directly for visibility and fallback draws. Unregistration released transient hiding and stale Hover.

Now ZoneController.Awake initializes from its explicit, same-scene outlineRoot; its existing bounded Start initialization retry remains. Initialization is idempotent, appearance refresh remains in controller Update, and controller OnDestroy still prevents reinitialization and disposes once. No legacy call initializes or obtains the helper.

Legacy Awake retains camera/stamp resolution, resolves the unique same-scene controller with matching terrain, and submits the current visibility policy. Start repeats **reference resolution only if the controller is missing**, preserving support for a controller created in a later Awake. Missing renderer availability never triggers that lookup. Compatibility calls resolve once if invoked before Awake; Update never discovers components. Renderer registration, renderer-driven retries and the borrowed helper field are gone.

Visibility flags can be supplied before controller Awake without allocating resources. Two cached suppression booleans are applied when the controller initializes its helper, preserving initial policy independently of Awake order. Existing ConfigureVisibility and SetPersistentVisibility behavior remains separate from per-channel RequestedVisible masks.

## Exact changes and API review

Modified:

- Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneController.cs
- Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs

Created this report. ZoneOutlineRenderer.cs is byte-identical. No other implementation or serialized asset changes were required.

| API/state | Result |
| --- | --- |
| TryRegisterOutlineRenderer, UnregisterOutlineRenderer, _outlineBridge | Removed; dependency search found only the legacy caller and controller declarations |
| Legacy RegisterOutlineRenderer, EnsureOutlineRenderer, _outlineRegistrationAttempted, _outlineRenderer | Removed/replaced by controller-reference resolution and request preparation; no helper instance crosses the controller boundary |
| Existing public ZoneController APIs | Signatures and implementations unchanged, including normal map-readiness guards |
| All 19 public legacy signatures | Retained; current project search found no external callers or serialized method bindings. HighRiskManager's same-named private ShowRiskOutlines is unrelated |
| SetOutlineVisibilityPolicy(bool, bool) | New internal method: cache/apply transient suppression only |
| PersistentOutlinesVisible | New internal, side-effect-free property for the existing conditional restore check |
| SetPersistentOutlineVisibility(bool) | New internal method: hide/restore cached persistent lines without changing requested masks |
| ResetOutlineVisibilityPolicy() | New internal cleanup: release suppression, clear stale Hover, restore persistent visibility with cached counts/masks intact |
| SetPreparedOutlineSegments(channel, List<ZoneOutlineRenderer.ColoredSegment>) | New internal adapter for retained raw-set/map-unready compatibility fallbacks; validates channel/resource availability, then submits prepared world segments without imposing the normal zone-map guard |

Normal GEOID/cell compatibility calls still try the public controller setters first. If their existing readiness check fails, the same legacy geometry preparation feeds the internal segment adapter. Raw normalized sets still have no GEOID and use that adapter directly. The legacy component references the renderer's segment **value types**, not a renderer instance. No renderer is exposed publicly and no fallback behavior was discarded.

Hover Update changed only the persistent-state check to read the controller property and removed redundant borrowed-helper availability checks. Mouse input, camera conversion, world Z normalization, zone lookup, GEOID/HashSet cache identity, success-only cache updates, misses, map readiness, color and enabled flags remain unchanged. No UI-pointer filtering or input extraction was added.

## Lifecycle and visibility comparison

| Case | Preserved result |
| --- | --- |
| Barrier entry/exit | Hover clearing, persistent hide/restore and all four flag combinations remain; the nondefault show/update timing quirk is unchanged |
| Explicit channel mask and replacements/clears while hidden | Requested mask survives; replacements update cached contents; cleared channels stay empty |
| Legacy component disable | No cleanup or resource destruction; last input policy remains until updates or existing explicit compatibility calls change it |
| Rendering-root deactivation | Existing hierarchy hides children; cached pools remain |
| Legacy destruction | Reset transient policy, clear stale Hover, restore eligible cached persistent lines; destroy no generated objects and dispose no helper |
| Controller destruction, either order | Dispose generated lines and owned fallback material once; retain assigned root/template/material; later legacy callbacks cannot recreate pools |
| Missing/cross-scene/lost root | Retain explicit-root validation and unavailable-request behavior; no discovery fallback or replacement root |

All seven channels remain: Hover, BaselineRisk, LiveFloodRisk, ShelterCandidateZones, ShelterCandidateTiles, PlacedShelterZones and CommunicationZones. Shared BaselineRisk replacement/clear behavior is unchanged.

## Validation and asset preservation

- Compiled all 202 Assembly-CSharp.csproj runtime sources using Unity 6000.0.63f1's installed Roslyn compiler and existing project response-file references: **0 errors, 11 warnings identical to Stage 5D1**, no new diagnostics. This was standalone compilation, not a Unity import/Play Mode run.
- Reused and extended the Stage 5D1 temporary harness with actual before/after controller, renderer, legacy, index, geometry and contract sources against Unity stand-ins. **183 line-state snapshots matched exactly**, covering seven channels, hover/cache/misses, negative origins, four barrier-flag combinations, masks, missing camera/stamp, disabling/re-enabling and all 19 compatibility signatures, including cached-GEOID/cell/raw-set readiness fallbacks and empty requests.
- Lifecycle and decoupling assertions passed: independent initialization, one helper/pool set, startup reuse, input-before-controller Awake, cached initial policy, no frame discovery, no renderer-driven Start lookup, necessary late-controller resolution, cross-scene rejection, legacy destruction while hidden, mask/count preservation, both destruction orders, missing/lost root and single cleanup of generated objects/fallback material. Invalid/unavailable prepared requests cannot initialize or replace a helper.
- Source comparisons confirmed identical public signatures/serialized field declarations and defaults, unchanged public controller outline/map/geometry implementations, and unchanged helper implementation. Source search leaves no registration API/ownership-bridge references or borrowed renderer field in legacy code. One renderer constructor and one disposal call remain in ZoneController.
- Compared against a starting snapshot of **6,137 Assets files**. Both scenes, every existing .meta/GUID, prefabs, materials, sprites and unrelated sources remain unchanged. Test/build artifacts are outside Assets. Unity may generate ordinary metadata for this new report during import; this task does not author metadata.

Both scenes preserve Canvas/ZoneBoundaryLine (GameObject 704415074, Transform 704415075), legacy component 704415076 and controller 2070746514. The controller's eight rendering assignments remain: root 704415075, template 1963393216, material GUID 5c951169c35ea49eab614112f3848aa0, RGBA (1, .85, .15, 1), width .08, Z -.1, Default layer and sorting order 1000. Legacy hover color/flags, component states and hierarchy are unchanged.

## Remaining responsibilities and manual verification

ZoneThinOutlineByHover remains active for camera/mouse input, zone/cache selection, hover color/enabling, barrier/missing-stamp visibility policy and destruction cleanup. It retains the unused compatibility signatures, stamp-based fallback geometry and hidden serialized appearance preservation fields. It owns no renderer, material or generated line object. Hover extraction/component retirement remain later stages.

Use **PlayableTilemap only** for manual Play Mode checks:

1. Check Console, normal hover/miss, baseline/live risk, candidate zones/tiles, placed shelter coverage and tower hover; confirm original appearance and parent, with no duplicate children after repeated toggles.
2. Enter/exit Place Barriers. Check cached restoration, explicit masks, hidden replacement/clear and all four hide-flag combinations, retaining the nondefault timing behavior.
3. Disable/re-enable the legacy component, then its GameObject separately. Confirm retained pools and existing frozen-policy/hierarchy behavior.
4. In a disposable Play session, destroy only the legacy component while persistent lines are hidden and one channel is explicitly masked. Hover clears; eligible cached persistent lines return; controller submissions still work; no generated objects are destroyed.
5. In another disposable session, destroy ZoneController before/after the legacy component. After Unity's deferred destruction, generated children are gone once, with assigned root/template/material retained. Test a missing-root assignment for a diagnostic and no new root. Do not save runtime Inspector/destruction changes.

No implementation blocker remains. The request records user verification of Stage 5D1 gameplay/visuals; Stage 5D2 still needs the manual checks above. Stand-ins cannot establish Unity-native visual parity or deferred-destruction timing. Keep the single authored hover/policy driver until Stage 5E; the internal policy calls do not arbitrate multiple input drivers. Review and commit after manual verification.
