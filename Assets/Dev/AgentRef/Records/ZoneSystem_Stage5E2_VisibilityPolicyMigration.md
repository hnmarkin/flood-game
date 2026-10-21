# ZoneSystem — Stage 5E2: Visibility policy migration

ZoneOutlineVisibilityController now owns barrier visibility and the coordinated policy-before-hover tick. ZoneHoverInputController references this new owner; normal hover/rendering works without legacy execution. Legacy Update and destruction cleanup are removed, while compatibility APIs remain. Both scenes explicitly wire the new component to the existing root, input and BarrierController. Renderer ownership and configuration are unchanged. Compilation passed with zero errors and 11 existing warnings. Enabled-policy comparisons match Stage 5E1; legacy-disable differences are intentional. New policy disable/destruction releases suppression without changing persistent counts/masks. Unity Play Mode was not executed for this stage.

## Ownership and frame order

Previously, ZoneThinOutlineByHover refreshed suppression, hid/restored persistent outlines and called ZoneHoverInputController.ProcessHover. Its destruction reset suppression and invalidated the hover cache.

Now:

| Responsibility | Owner |
| --- | --- |
| Renderer initialization, appearance, seven pools, disposal | ZoneController, unchanged |
| Barrier-state evaluation, transient suppression, persistent hide/restore, scheduling | New ZoneOutlineVisibilityController |
| Mouse/camera conversion, GEOID/HashSet cache, Hover requests/clearing | ZoneHoverInputController |
| Retained compatibility APIs, stamp-based fallback geometry and reference resolution | ZoneThinOutlineByHover; no Update or policy cleanup |

The new Update body is the previous policy Update verbatim apart from provider/controller qualification:

1. Evaluate barrier/input availability and refresh transient suppression flags.
2. If the provider is missing: process suppressed Hover (clearing it), hide persistent outlines, return.
3. If the hide-hover flag requires barrier suppression: process suppressed Hover first, optionally hide persistent outlines, return.
4. Otherwise conditionally restore cached persistent visibility, then process ordinary Hover.

Thus barrier-entry Hover clearing still precedes persistent hiding; normal cached restoration precedes hover processing. Input has no independent Update. The existing policy-identity and active-state guards now use the new owner. Eligible ticks perform one mouse/world query; cache identity and success-only commit behavior remain unchanged.

The new provider is the existing **BarrierController**, whose IsZoneBoundaryModeActive is the value formerly forwarded by FloodDefenseBoxStamp. No placement, limits, physics or commit behavior moved. A null new provider retains the missing-provider contract: clear Hover, suppress shows and hide persistent cached lines. Removing only the stamp no longer removes the policy's state source; a missing BarrierController is now the relevant failure condition.

## Exact source changes

- Created Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneOutlineVisibilityController.cs and its new .meta. It owns policy/scheduling, lifecycle refresh and cleanup; no renderer access, allocation, disposal or component searches.
- Updated Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneHoverInputController.cs: changed the serialized policy type and ProcessHover source parameter to ZoneOutlineVisibilityController; updated the controller-adoption comment. Every other line is unchanged.
- Updated Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs: removed Update, direct policy refresh/hide/restore methods, hover scheduling and OnDestroy reset. Awake/Start retain compatibility reference resolution only. PrepareOutlineRequest may ask the explicitly assigned new policy to refresh before a compatibility show; only the active new owner computes/writes that policy. Archived input/hide-flag fields remain serialized and hidden, with their original names/defaults.
- ZoneController.cs, ZoneOutlineRenderer.cs, index/geometry, FloodDefenseBoxStamp.cs and BarrierController.cs are byte-identical to the starting snapshot.

All 19 public legacy signatures and their public compatibility/geometry region remain identical. Normal public controller APIs and shared BaselineRisk last-writer/any-clear behavior are unchanged. No new policy arbitration, manager or events were introduced.

## Scene and Inspector assignments

**New script GUID: 572166c511a9404c9cbaf2c27f858ce2.** New component fileID **704415078** is enabled on existing Canvas/ZoneBoundaryLine (GameObject 704415074) in:

- Assets/Game/Content/Scenes/PlayableTilemap.unity
- Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity — matching minimal serialization only.

| New visibility field | Assignment |
| --- | --- |
| zoneController | 2070746514 |
| hoverInput | 704415077 |
| barrierController | 420333452, the same controller referenced by the stamp |
| hideWhileBarrierModeIsActive | true |
| hidePersistentWhileBarrierModeIsActive | true |

Existing input component 704415077 changes only visibilityPolicy from legacy 704415076 to new 704415078. Camera, color and enabled state remain identical. Legacy 704415076 stays attached/enabled and adds visibilityPolicy = 704415078 solely for compatibility-show refresh; nothing points back to the legacy component.

Per scene, only three existing records change: the root component list, the legacy compatibility reference and the input's policy reference. One new component record is added. Record counts are 346→347 and 360→361; all local references are valid with no duplicate component entries. All other existing records are byte-identical, including hierarchy, transform, activation, eight renderer assignments, template/material and barrier gameplay settings. Existing script GUIDs/metadata remain unchanged.

## Visibility equivalence and lifecycle

All four enabled-policy flag combinations retain their previous behavior:

| Hide Hover / hide persistent | Barrier behavior |
| --- | --- |
| true / true | Clear Hover, hide persistent contents without clearing counts, restore cached eligible lines on exit |
| true / false | Clear Hover without hiding persistent lines |
| false / true | Preserve the old timing quirk: a show suppresses persistent lines during barrier mode; a subsequent Update may restore them |
| false / false | Neither barrier suppression applies |

Requested channel masks remain independent. Replacement while hidden updates cached contents; clearing while hidden remains cleared. Missing camera, hover disabled, map unready and zone misses still clear Hover through the input component.

**Intentional lifecycle differences:** disabling/destroying the legacy component no longer freezes, clears or resets active policy/input. Disabling the new visibility controller releases its suppression, clears stale Hover and invalidates input cache, restoring only eligible persistent lines. This replaces the old policy's frozen-disable behavior, as required by Stage 5E2. Compatibility shows cannot reactivate a disabled policy.

Cleanup retains the last controller that received policy, so clearing the serialized controller reference still releases that previous target. The reference is cleared before reset, making OnDisable followed by OnDestroy idempotent. Persistent counts and requested masks are untouched. On re-enable, current suppression flags refresh and the next coordinated tick applies normal hide/restore rules.

Input disable/destruction still clears only Hover; persistent policy continues. Shared-root deactivation releases policy and retains pools beneath the unchanged inactive parent; reactivation resumes policy. Controller destruction remains the sole renderer disposal path. Cleanup tolerates missing/disposed resources and creates no replacement renderer/root.

## Compilation and automated verification

- Standalone compilation using installed Unity 6000.0.63f1 Roslyn and project references: **204 runtime sources, 0 errors, 11 warnings identical to Stage 5E1**. No Unity import/Play Mode claim is made.
- Reused the temporary Stage 5E1 harness with actual before/after source and Unity stand-ins. Of 187 line-state snapshots, **184 match exactly**. The three differences are legacy-disable, explicit show while legacy-disabled and subsequent re-enable: the modular policy now continues running rather than retaining the old frozen state. Normal enabled-policy, all four flag combinations, seven channels, masks, readiness/compatibility fallbacks, coordinates and cache regressions match.
- New assertions passed for one query per eligible frame; startup without any legacy Awake/Start; legacy disable/destruction with active Hover and with hidden persistent channels; continuing hover/cache state; new policy disable/re-enable/destruction; idempotent cleanup; input loss; missing/restored direct provider; lost/restored controller assignment; shared-root lifecycle; cached masks/counts; and both controller/policy destruction orders with generated objects destroyed once. Stand-ins do not establish Unity-native rendering or deferred-destruction timing.
- Source checks verify only the new visibility owner applies active policy; only input polls the mouse; legacy has no Update, ProcessHover call or reset path. Controller/renderer remain unchanged.
- Compared 6,177 starting Assets files: only the two existing sources, new source/meta, two scenes and this report were authored. Existing metadata/GUIDs and unrelated assets remain unchanged. Test/build artifacts are outside Assets. Unity may generate ordinary metadata for this report during import.

## Remaining dependencies and Stage 5FG readiness

Fresh searches found no external C# consumers of the legacy type or its compatibility methods, and no serialized method bindings. The remaining external type-name occurrence is a CommunicationTowerController documentation comment. Its script GUID occurs only in the two legacy scene components. Component 704415076 is referenced only by its own GameObject component-list entry and record header in each scene; input/new policy no longer reference it.

The legacy script still contains its 19 compatibility methods, outbound stamp/controller/input/policy references and archived data. Shared request structs are already independent. A temporary full-runtime compilation **excluding only the legacy source** passed: **203 sources, 0 errors, the same 11 warnings**. No source, component or metadata was removed for that check.

Code/serialization evidence supports combined Stage 5FG after PlayableTilemap verification and a fresh dependency search. That later stage can remove legacy component 704415076/list entry from both scenes, then the legacy .cs/.meta pair, preserving the rendering GameObject and both modular components. Stamp retirement remains separate. This stage does not authorize or perform those removals.

## Manual Play Mode checklist — PlayableTilemap only

1. Verify the new policy references/flags above and input's new policy reference. Keep the legacy attached for now. Confirm normal hover and all persistent overlays, appearance and root children without duplicates.
2. Exercise Place Barriers entry/exit, all four flag combinations, requested masks, and replacement/clear while hidden. Compare the false/true timing quirk with Stage 5E1.
3. Disable the legacy component before starting a disposable Play session; verify hover, baseline/live risk, candidate zones/tiles, placed coverage, communication and barrier hide/restore. Destroy the legacy component during Play with Hover active and persistent outlines hidden; neither active policy nor cache should reset.
4. Disable/re-enable and destroy the new policy separately. Check Hover/cache clearing and restoration of eligible persistent lines without count/mask changes. Repeat with input disabled/destroyed and with the shared root deactivated/reactivated.
5. In disposable runtime Inspector changes, clear/restore provider, camera and controller references. Check missing-provider hiding, readiness guards and recovery. Destroy controller/policy in both orders and inspect end-of-frame cleanup, with assigned root/template/material preserved. Do not save runtime destruction/reference changes.

Stage 5E1 Play Mode success is supplied by the request. No Stage 5E2 Unity Play Mode run or Dev_UINavigation Play Mode test was performed. No implementation blocker remains. Remaining checks concern Unity-native visuals, default-order interactions with other components and deferred destruction. Review and commit after manual verification.
