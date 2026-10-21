# ZoneSystem — Stage 5E1: Hover input extraction

ZoneHoverInputController now owns mouse polling, camera, hover settings, GEOID/HashSet caching, Hover requests and clearing. The existing legacy policy Update explicitly calls it at the original hover-processing point, preserving frame order and barrier visibility timing. Both scenes add the new component on the unchanged rendering root with explicit dependencies. Legacy policy and compatibility methods remain active; ZoneController and ZoneOutlineRenderer are unchanged. Compilation passed with zero errors and 11 existing warnings. All 187 before/after line-state comparisons and additional lifecycle checks passed using Unity stand-ins. Unity Play Mode was not executed for this stage.

## Responsibility split

Previously, ZoneThinOutlineByHover.Update refreshed renderer policy, applied persistent visibility decisions, polled the mouse, identified a zone and cached/drew/cleared Hover.

| Original statements/responsibility | Stage 5E1 owner |
| --- | --- |
| Camera.main fallback; Input.mousePosition; ScreenToWorldPoint; world.z = 0 | New input component; camera fallback remains Awake-only |
| Hover-enabled/color settings; controller/map readiness guards; world zone query | New input component, using ZoneController's public APIs |
| _currentGeoid/_currentZoneTiles identity cache; success-only commit; miss/failure clear | New input component; no in-place mutation invalidation was added |
| RefreshOutlineSettings and transient suppression flags | Legacy policy; active camera/enabled availability now comes from the new input component |
| Missing stamp and hide-hover-on-barrier branches | Legacy policy decides suppression, then explicitly calls ProcessHover with that decision |
| Persistent hiding and conditional cached restoration, including both barrier flags | Legacy policy; original ordering and branches retained |
| Legacy destruction: clear stale Hover/release suppression/restore persistent visibility | Existing controller reset retained, with coordinated input-cache invalidation |

The new MonoBehaviour intentionally has **no independent Update loop**. Legacy Update calls ProcessHover exactly once on each branch, at the former hover-processing/clearing point. This keeps one mouse-driven requester and preserves the original interleaving of policy, persistent restoration and hover. The input method checks the explicitly assigned policy identity and both components' active state. It reads no stamp/barrier state and implements no competing barrier policy.

## Exact files and serialized wiring

Modified:

- Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs
- Assets/Game/Content/Scenes/PlayableTilemap.unity
- Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity — corresponding minimal compatibility wiring only.

Created:

- Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneHoverInputController.cs
- Its new .meta file.
- This report.

**New script GUID: 64f9a390e9f248e7b4d7ad09bb6a968d.** Global namespace/Assembly-CSharp, DisallowMultipleComponent, default execution order. No assembly definitions or existing metadata changed.

Each scene adds one enabled component, fileID **704415077**, to existing Canvas/ZoneBoundaryLine (GameObject 704415074). Its assignments are:

| New input field | Assignment |
| --- | --- |
| zoneController | Existing component 2070746514 |
| mainCamera | Existing camera 917516086 |
| visibilityPolicy | Existing legacy component 704415076 |
| lineColor | RGBA (1, .85, .15, 1) |
| outliningEnabled | true |

Legacy component 704415076 adds only hoverInput = 704415077. Its saved camera/color/enabled values are retained for serialization/compatibility and hidden in the Inspector; active hover settings now belong to the new component. Existing compatibility color defaults remain unchanged. The existing scoped unused-field suppression also covers the archived enabled flag.

Scene parsing verified record counts 345→346 and 359→360. Exactly two existing records change per scene: the root's component list gains the new entry, and the legacy component gains its input reference. Every other existing record is byte-identical, including the root transform, hierarchy/activation, template/material, and ZoneController's eight rendering assignments. Local file references are valid; no duplicate component entries or script GUID collisions exist.

## Hover and policy behavior

The query/conversion/cache/draw section moved verbatim apart from controller/enum qualification. It still queries through TryGetZoneTilesAtWorld, submits SetZoneOutline(Hover), and clears through ClearOutlines(Hover). Camera failure, disabled hover, unready map, misses and rejected requests invalidate the cache. Successful draws alone commit it. Reference-identical HashSet mutation still does not redraw; replacement sets do.

All four barrier flag combinations remain:

- true/true: clear Hover and hide persistent outlines on barrier entry; restore cached persistent contents on exit.
- true/false: clear Hover without persistent hiding.
- false/true: retain the existing nondefault timing: Update may restore persistent lines, while a show during barrier mode suppresses them until the subsequent restore.
- false/false: barrier mode does not suppress Hover or persistent lines through these flags.

Transient suppression still uses the existing policy API, independently of each channel's requested visibility mask. No geometry, renderer, channel allocation, shared BaselineRisk behavior or persistent cleanup logic changed. All 19 public legacy compatibility signatures and their implementation/geometry/resolution region remain byte-identical. ZoneController.cs and ZoneOutlineRenderer.cs are entirely byte-identical.

## Lifecycle coordination

| Case | Result |
| --- | --- |
| Awake/Start order | Input resolves its camera in Awake. Explicit scene references exist before callbacks. Policy retains its existing bounded controller resolution; input can adopt that resolved reference during ProcessHover when its own reference is missing, without discovery or initialization |
| Late/unavailable controller or map | Existing policy Start retry remains; normal readiness failures clear Hover/cache and retry on a later coordinated tick. No per-frame searches or new renderer creation |
| Legacy policy disabled/re-enabled | No policy Update means no input tick. Existing frozen hover/persistent policy behavior is preserved; enabling resumes the original ordered decisions |
| New input disabled/re-enabled | OnDisable clears only Hover/cache. Legacy persistent policy continues running. Enabling redraws the same hovered zone on the next coordinated tick |
| Shared GameObject disabled/re-enabled | New input cleanup clears Hover/cache; persistent pools remain cached under the same root. Re-enabling redraws Hover rather than retaining a stale cache |
| Input destroyed | OnDestroy clears only Hover/cache; no generated objects are destroyed and persistent masks/counts/policy remain intact |
| Policy destroyed | InvalidateHoverCache plus the existing ResetOutlineVisibilityPolicy clears stale Hover and restores eligible persistent cached lines. The remaining input does not poll independently |
| Controller destruction/scene unload | Existing controller disposal remains the sole rendering cleanup. Input/policy callbacks tolerate unavailable resources; no renderer resurrection or pool duplication |

The only new coordination methods are ProcessHover(source, resolvedController, suppressed), IsHoverInputAvailable, and InvalidateHoverCache on the input component. Persistent cleanup still runs through the unchanged controller reset. No global events, searches, manager or scheduling attributes were added. Replacing a policy component requires explicitly updating the reciprocal references; no automatic replacement discovery was introduced.

## Validation

- Standalone compilation with installed Unity 6000.0.63f1 Roslyn and project references included **203 runtime sources**: the previous 202 plus the new input source. **0 errors, 11 warnings identical to Stage 5D2.** This does not claim Unity import or Play Mode validation.
- Extended the temporary Stage 5D2 harness using actual before/after controller, renderer, legacy, input, index, geometry and contract sources against Unity stand-ins. **187 line-state snapshots matched exactly** across seven channels, four flag combinations, masks, colors/coordinates, misses, camera/stamp loss, readiness fallbacks, all compatibility methods and policy disable/re-enable behavior.
- Additional checks passed for coordinate normalization, identity-cache behavior, failed-show retry, independent renderer lifetime, same-root pooling, input disable/destruction, continuing persistent policy without input, shared-GameObject reactivation, policy-destruction cache invalidation, correct policy identity, new settings ownership, camera fallback and late controller adoption without searches. Existing disposal/missing-root tests also passed.
- Source checks confirm no mouse polling/cache remains in legacy code. New input has no ZoneOutlineRenderer/LineRenderer/geometry/stamp/barrier reference or component search, and uses public zone/outline APIs. One input component is serialized in each scene.
- Scope checks compared 6,139 starting Assets files. Authored changes are limited to the listed source, new source/meta, two scenes and report. Existing metadata/GUIDs and unrelated assets remain unchanged. Test/build artifacts are outside Assets. Unity may generate ordinary metadata for the new report during import.

## Remaining work and manual checks

Legacy still owns barrier/missing-stamp suppression, persistent hide/restore timing, the coordinated input call, controller resolution, destruction reset and unused compatibility APIs/fallback geometry. It contains no mouse polling or hovered-zone cache. Policy extraction and legacy retirement remain later stages.

Use **PlayableTilemap only** for manual Play Mode verification:

1. Inspect the new component and reciprocal references above. Adjust active hover color/enabling/camera on ZoneHoverInputController; keep legacy policy enabled. Confirm the existing root and eight controller rendering assignments are unchanged.
2. Check hover entry, movement, same-zone caching, mouse miss, color/width/Z/sorting and map startup. Verify baseline/live risk, candidate zones/tiles, placed coverage and communication hover remain independent, with no duplicate line children.
3. Exercise barrier entry/exit, both flags in all four combinations, requested masks and replacement/clear while hidden. Retain the nondefault timing behavior.
4. Disable/re-enable input, then policy, then the shared GameObject separately. Confirm input disable clears only Hover; policy disable freezes coordinated updates; persistent visibility still works when only input is disabled.
5. In disposable Play sessions, destroy input alone, then policy alone, and controller in both destruction orders. Check stale Hover removal, persistent counts/masks/restoration, assigned asset survival and end-of-frame generated-resource disposal. Do not save runtime changes.

The request records user verification of Stage 5D2 gameplay/visuals. No Stage 5E1 Unity Play Mode run or Dev_UINavigation Play Mode test was performed. No implementation blocker remains; Unity-native visual/lifecycle parity and deferred destruction still require the checklist. Review and commit after manual verification.
