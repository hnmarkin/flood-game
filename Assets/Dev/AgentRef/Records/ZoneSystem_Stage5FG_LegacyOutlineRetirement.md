# ZoneSystem — Stage 5F + 5G: Legacy outline retirement

Removed ZoneThinOutlineByHover from both authorized scenes and deleted its source/metadata pair. The rendering root, modular components, serialized assignments and all modular source files are unchanged. Fresh dependency searches found no external active legacy dependency. Compilation passed with 203 runtime sources, zero errors and 11 existing warnings. A modular-only regression harness passed; 164 captured states match Stage 5E2. YAML and local references validate, with no newly unresolved script bindings. Unrelated unresolved script GUIDs already existed in both scenes and remain untouched. Unity import and Play Mode were not executed.

## Preflight and independence

Read the Stage 5A, 5E1 and 5E2 reports and the current legacy, controller, input and visibility implementations before editing. The request supplies successful Stage 5E2 Play Mode verification in PlayableTilemap. Both scenes matched every requested assignment; there were no discrepancies requiring a scope change.

Fresh project-wide searches covered the legacy type/GUID, all 19 public compatibility signatures (18 distinct method names), component fileID 704415076, serialized event/method bindings, scenes, prefabs, ScriptableObjects and animation assets. Results were classified as follows:

| Result | Classification and disposition |
| --- | --- |
| Legacy class, internal calls and log strings | Retired source; no external active consumer |
| One legacy GUID binding and one component-list entry per scene | The two authorized scene components being retired |
| CommunicationTowerController.cs line 7 | Documentation-only legacy type mention; preserved |
| HighRiskManager.ShowRiskOutlines declaration/internal call | Unrelated private manager method, not a call to the retired component |
| Prior migration/audit reports | Historical references; preserved |
| Assembly-CSharp.csproj and Library/Bee/artifacts/200b0aE.dag/Assembly-CSharp.rsp | Generated source lists; preserved for Unity to regenerate |

No external UnityEvent, animation event, persistent method binding, prefab or ScriptableObject targeted the retired component. Its file declared only the MonoBehaviour. ZoneRiskOutlineRequest and TileOutlineRequest each have exactly one independent declaration in their dedicated ZoneSystem files. No shared type needed extraction.

ZoneController initializes its renderer in its own startup paths, retains seven channels and appearance configuration, and disposes the renderer itself. ZoneHoverInputController has no legacy reference and owns mouse conversion/cache/Hover requests. ZoneOutlineVisibilityController uses BarrierController directly and schedules input after visibility policy. Neither new component requires a legacy Awake, Start, Update or teardown callback. No modular source changes were necessary.

## Exact changes

Deleted this matched pair, without reusing its GUID:

- Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs
- Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs.meta — GUID 71bfc9ffc1b314ef7a54476c563601a5

Changed only these scenes:

- Assets/Game/Content/Scenes/PlayableTilemap.unity — the explicit scene exception authorized by this request.
- Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity.

In each scene, removed the complete `--- !u!114 &704415076` record and its single component-list entry from GameObject 704415074, Canvas/ZoneBoundaryLine. This removes 27 lines / 882 bytes per scene. Its component list changes from `[704415075, 704415076, 704415077, 704415078]` to `[704415075, 704415077, 704415078]`.

The root GameObject, Transform 704415075, hierarchy and active state remain. Every other existing YAML record is byte-identical to the preflight snapshot. The containing Zones directory remains. This report is the only new project file; Unity may generate its normal metadata during import.

## Preserved modular configuration

These assignments remain unchanged in both scenes:

| Component | Preserved references/settings |
| --- | --- |
| ZoneController 2070746514 | outlineRoot 704415075; linePrefab 1963393216; lineMaterial fileID 2100000, GUID 5c951169c35ea49eab614112f3848aa0; lineColor (1, 0.85, 0.15, 1); lineWidth 0.08; zOffset -0.1; sortingLayerName Default; sortingOrder 1000 |
| ZoneHoverInputController 704415077 | Script GUID 64f9a390e9f248e7b4d7ad09bb6a968d; zoneController 2070746514; mainCamera 917516086; visibilityPolicy 704415078; yellow lineColor as above; outliningEnabled true |
| ZoneOutlineVisibilityController 704415078 | Script GUID 572166c511a9404c9cbaf2c27f858ce2; zoneController 2070746514; hoverInput 704415077; barrierController 420333452; both barrier-hide flags true |

All three modular script GUIDs resolve to their current metadata. No existing metadata/GUID was changed. Root/template/material records and every gameplay source remain unchanged, including geometry/index behavior, absolute cell coordinates, BaselineRisk semantics and persistent channel masks.

## Scene and dependency validation

Ruby Psych successfully parsed both scenes: PlayableTilemap has 347 → 346 records; Dev_UINavigation has 361 → 360. No duplicate object IDs or component-list entries exist. All nonzero local PPtrs resolve. The only removed record is 704415076; the only changed retained record is its GameObject's component list. Both outputs exactly match the prepared minimal removal.

Neither scene retains the old GUID, component record, component-list entry or a dangling local reference to 704415076. The source/meta pair is absent. Post-deletion searches found zero active C# or serialized legacy dependencies. Historical reports, the existing communication comment and generated source lists remain as classified above. Generated project/build lists still contain the deleted source path until Unity refreshes them; validation used a temporary response file with that path removed, without editing generated files.

**Existing scene issue:** matching external MonoBehaviour script GUIDs against Assets, Packages and the installed Library/PackageCache found the following unresolved bindings. Every listed record and GUID is identical in the preflight snapshot and final scene; retirement introduces zero unresolved bindings. Built-in engine references were excluded from this check.

| Component fileID | GameObject | Unresolved script GUID |
| --- | --- | --- |
| 35733722 | FloodImpactController | 6c1278d19cccb4a3da4563a6458f29fa |
| 434996196 | ShelterPresentation | c32d7337dc7a94591bd7e955b4968ba4 |
| 555384876 | UIDocument | 5145aba9c2a8f4b2ea7c85a977475f1e |
| 748270052 | ShelterPlacementInput | 94259d23cf9f043cd96b10d0e72dbb21 |
| 1046329327 | FloodDefenseManager | a4da8be3a33cf40d9a2e41e79ba0ff92 |
| 1420635163 | ShelterController | b516d3fb7dd444b769132f5e17485a7e |
| 1434780123 | UIDocument | 3a49d09d23d6483fb909c984ac5131dd |
| 1678446138 | JsonMapLoaderObject | 9dacb7ec027494fa3a48748735e186e5 |
| 2036055009 | UIDocument, Dev_UINavigation only | 4b97d28e41ca947aa960cd0886b99041 |

The first eight occur in both scenes. These unrelated records were preserved under the strict scope. The old outline component cannot become a Missing (Mono Script) component because both its list entry and record are removed before import. A blanket claim that either entire scene has no missing scripts is unsupported; Unity must confirm the unrelated bindings above. They may affect scene-wide manual testing and need separate investigation if unresolved after import.

## Compilation and regression results

- Installed Unity 6000.0.63f1 Roslyn with the project's runtime references compiled **203 runtime sources, 0 errors, 11 warnings**, down from Stage 5E2's 204 sources. The warning messages match Stage 5E2 exactly. No source/API fix was required.
- Reused Stage 5E2's temporary Unity stand-ins, fixture coordinates, snapshot format, parity cases and hover-cache regressions. Adapted setup instantiates only the modular components; neither the legacy source nor a legacy component participates.
- **13,070 assertions passed; all 164 captured states match the saved Stage 5E2 results exactly.** Coverage includes all seven channels; all four barrier flag combinations crossed with requested masks and hover enabled/disabled; barrier entry/exit; cached reuse; replacement/clearing while hidden; map readiness/misses; absolute tile geometry/appearance; HashSet identity caching and retry after failed submission.
- Lifecycle assertions passed for renderer initialization/reuse, no per-frame discovery, one eligible hover query per frame, input/policy disable/re-enable/destruction, missing/restored provider or controller, shared-root activation, late root initialization, persistent count/mask preservation, both policy/controller teardown orders, and exactly-once disposal of generated lines/fallback material while preserving assigned assets.
- SHA-256 comparison against 6,181 starting Assets files confirms exactly two modified scenes, the two authorized deletions and this new report. All other starting files are byte-identical, preserving unrelated development. Stage-only whitespace checks pass; the broader worktree check reports pre-existing scene/layout whitespace, left unchanged.
- Harness/build evidence is outside Assets at /private/tmp/zone-stage-5fg-r63rpywd. Stand-ins do not verify Unity-native rendering, import, callback ordering or deferred destruction. **Unity Editor/Play Mode was not executed for Stage 5FG.**

## Remaining work and manual checklist

No additional source edit is needed for legacy outline retirement. FloodDefenseBoxStamp retirement and any later removal of unused compatibility helpers require separate dependency audits/stages. Historical documentation and unrelated comments were intentionally preserved. Investigate the pre-existing unresolved script bindings separately; do not change their GUIDs speculatively.

Use **PlayableTilemap only** for required Play Mode verification. Dev_UINavigation received matching serialized cleanup and does not require Play Mode testing.

1. Refresh/import in Unity and confirm compilation. Inspect Canvas/ZoneBoundaryLine: retained Transform, enabled input and visibility components, no legacy or missing outline component. Verify the assignments above, existing template/material and ZoneController settings. Check the listed unrelated script bindings/Console before assessing full-scene behavior.
2. Enter Play Mode and verify hover, baseline risk, live flood risk after its normal activation/data calculation, shelter candidate zones/tiles, placed shelter coverage and communication highlights. Check line color, width, Z, sorting and absence of duplicate generated children.
3. Enter/exit Place Barriers with persistent outlines already displayed. Check all four hide-flag combinations, including the existing false/true next-frame restoration timing; requested masks must survive. Replace and clear contents while hidden and verify the resulting restored contents.
4. In a disposable Play session, disable/re-enable input and visibility separately, then deactivate/reactivate the root. Confirm Hover/cache recovery, persistent contents/masks and pool reuse. Clear/restore provider, camera and controller references to check guards/recovery; do not save runtime changes.
5. Exercise controller/policy teardown in both orders. Inspect end-of-frame generated-line cleanup and preservation of the assigned root/template/material. Review and commit after manual verification.
