# CommunicationSystem — Helper Filename Cleanup

Date: 2026-10-23. Completed the five helper source renames and matching metadata renames within Dev_CommunicationSystem. All ten files preserve their original bytes and all five GUIDs. Classes, visibility, architecture, public controller, consumers and scene are unchanged. No maintained dependency uses an obsolete filename. Both managed builds compile 207 runtime sources with zero errors and the same 11 warnings; all 348 assertions pass and 108 traces match C5 exactly. Only the ten renames and this report/metadata were added. Unity-native import and Play Mode were not performed for this cleanup.

## 1. Old and new filenames

All paths remain inside `Assets/Dev/Dev_CommunicationSystem/`. The folder and its metadata were not renamed or modified.

| Original source filename | Final source filename |
| --- | --- |
| `Dev_CommunicationInputController.cs` | `CommunicationInputController.cs` |
| `Dev_CommunicationPresentation.cs` | `CommunicationPresentation.cs` |
| `Dev_CommunicationState.cs` | `CommunicationState.cs` |
| `Dev_CommunicationTowerGenerator.cs` | `CommunicationTowerGenerator.cs` |
| `Dev_CommunicationTowerVisuals.cs` | `CommunicationTowerVisuals.cs` |

Each existing source's `.meta` followed the same rename: append `.meta` to both filenames in each row. For example, `Dev_CommunicationState.cs.meta` became `CommunicationState.cs.meta`. This is five source renames plus five matching metadata renames, with no source rewrite or regenerated metadata. All five destinations were absent before the moves.

## 2. Original and final GUIDs

| Helper | Original GUID | Final GUID |
| --- | --- | --- |
| `CommunicationInputController` | `6e587df9dddf410d8cd840641f727805` | `6e587df9dddf410d8cd840641f727805` |
| `CommunicationPresentation` | `85d331ce537b48beacb8733564757dd7` | `85d331ce537b48beacb8733564757dd7` |
| `CommunicationState` | `dff76e34aeb941b1a54bafcfc8f7d50a` | `dff76e34aeb941b1a54bafcfc8f7d50a` |
| `CommunicationTowerGenerator` | `0868b4c9a38b43f7996dc89a6408c407` | `0868b4c9a38b43f7996dc89a6408c407` |
| `CommunicationTowerVisuals` | `9679c72192ee41ff8d205110273504cf` | `9679c72192ee41ff8d205110273504cf` |

Each helper GUID has exactly one metadata owner at its new path. The public `CommunicationController.cs` and its metadata remain unchanged, retaining GUID `98e604977a2f4ef5ac4190852d01e7fa`. No script, MonoBehaviour, component, compatibility alias or authority was added.

## 3. Source and metadata byte comparisons

Compared every renamed file directly against a pre-rename byte snapshot and recorded SHA-256 hashes in preflight.json:

| Verification | Result |
| --- | --- |
| Source bytes | All five match their originals exactly. |
| Metadata bytes | All five match their originals exactly. |
| Current helper source/metadata paths | Exactly five new `.cs` names and five matching `.cs.meta` names. |
| Old helper source/metadata paths | All ten absent. |
| Class names and visibility | All five still declare their original `internal sealed class Communication...` names. |
| Duplicate helper definitions | None; one definition per helper across current Assets/Packages C#. |
| Public controller, six consumers and helper dependencies | Source bytes unchanged, including all 17 public facade signatures and existing orchestration. |
| Gameplay scene | Whole-file bytes unchanged. |

PlayableTilemap retains component `810699005`, GameObject `810699003`, one controller script binding, all **24 Inspector assignments** and all **four inbound communicationTowerController references** from components `515287497`, `1389987227`, `2019949261`, `2100000004`. Its SHA-256 remains `482a2ccedd674f2b1af5cd095982d0b0a7de1bfb2b4faa7031bc5ee426dc0796`. The intentionally deleted development scene and metadata remain absent.

## 4. Maintained path-reference search

Before moving files, searched current text sources, tests, tooling, assembly configuration, package/project configuration, serialized assets and documentation for all five old filenames. Repeated the dependency check after the moves.

- **Maintained source/test/tooling/configuration dependencies: zero.** No production path fix or scope expansion was required.
- Ten existing historical-report lines reference the old paths; C1–C5 reports were preserved unchanged as requested. This cleanup report also records the required old/new pairs.
- The ignored generated `Assembly-CSharp.csproj` still contains five old helper paths at verification time. It was not edited. Unity may regenerate it; project-file regeneration is the remaining IDE refresh step.
- Old absolute paths in the temporary harness were mapped to the renamed source files only in the new temporary verification inputs outside the Unity project.

Generated Library/Temp/Logs/obj/Build and Git internals were excluded from maintained-dependency inspection. No source/class name was changed to accommodate a tool or test runner.

## 5. Compilation results

Fresh pre-rename snapshots and the actual renamed sources compile using the installed Unity 6000.0.63f1 Roslyn compiler and the same existing Unity/package/assembly references and complete compiler options as C5.

| Managed build | Runtime sources | Errors | Warnings |
| --- | --- | --- | --- |
| Pre-rename baseline | 207 | 0 | 11 |
| Renamed current sources | 207 | 0 | Same 11 |
| Baseline/current regression harnesses | Existing C2/C3/C4 coverage | 0 each | 0 each |

All six complete consumer sources compile unchanged, and CommunicationController resolves all five renamed helper files. The runtime diagnostic message multiset matches C5: one CS0105, eight CS0618 and two CS0414 warnings.

The first temporary response-file filter omitted 12 slash-prefixed Unity options, including existing warning suppressions, so both comparison builds initially printed 160 warnings and zero errors. Restoring the exact C5 options in temporary inputs reproduced the expected 11 warnings. No project file or source change was made for that correction; its initial logs and cause are retained in compiler-input-correction.json and the initial-options logs.

## 6. Managed regression results

Both fresh managed runs pass **348 assertions** and match all **108 states/traces**, also matching the recorded completed C5 output byte-for-byte. No trace normalization or expected-result update was needed. No additional managed tests were added.

Coverage remains C2 generation/state (215 assertions, 73 traces), C3 rendering/input/presentation (88 assertions, 26 traces), and C4 API/consumer integration (45 assertions, nine traces). This includes activation/event ordering, panel/warning/tooltip behavior, subscription cleanup, Actions discovery/fallback reuse, advisor/resources/summary responses and no duplicate authority.

The harness executes actual controller/helper and zone-related sources with engine/provider/UI stand-ins. Consumer checks include the existing 35 extracted methods/subscription branches; complete consumer files compile in the runtime build. These managed results do not establish native Unity asset import or UI Toolkit rendering.

Temporary evidence and repeatable runner:

`/var/folders/k0/k_vxwym14hs2n16m3n8bt2t40000gn/T/communication-helper-filenames-yek_a_87`

The directory contains preflight.json (original GUIDs and byte hashes), baseline hashes/status, source/meta snapshots, frozen runtime sources, updated temporary response files, compiler/harness logs, verification.json, final-scope.json and replay.py. The runner verifies the pre-rename/current compilation and exact C5 trace parity. No verification binary or temporary test file was written under Assets.

## 7. Changes outside the authorized scope

**None.** Captured hashes for all 6,300 entry files under Assets, Packages, ProjectSettings and UserSettings. The final scope check permits only the ten moves plus these two new files:

- `Assets/Dev/AgentRef/Records/CommunicationSystem_HelperFilenameCleanup.md`
- Its new `.md.meta`, using the repository's report metadata convention and a fresh report-only GUID.

No remaining entry file changed bytes or was deleted. Historical reports, public controller, consumers, helper folder metadata, scenes/prefabs/UI/assets, packages/settings and unrelated working-tree edits retain their entry contents. Generated IDE project files were not manually changed. Review and commit this filename-only checkpoint while preserving the existing mixed working tree.

## 8. Unity manual verification checklist

No native Unity import or Play Mode run was performed for this cleanup. The earlier user-reported successful Play Mode verification followed C4 and preceded these filename changes.

1. Let Unity import the five source/meta moves. Confirm the new filenames appear once each, their class names/GUIDs match section 2, and there are no new compilation or missing-script errors.
2. Inspect the original communication GameObject in `Assets/Game/Content/Scenes/PlayableTilemap.unity`: one CommunicationController, unchanged controller GUID, 24 assignments and four inbound references; no helper component should be attached.
3. In that scene only, smoke-test communication toggling, hover/outlines/tooltips, activation/repeated selection, panel warnings and advisor/resource/summary updates. Keep test changes unsaved; do not restore or run the deleted development scene.
4. Allow Unity to regenerate the ignored IDE project, or use its normal project-file regeneration command, and confirm the five new paths replace the old entries. Do not hand-edit generated projects or force changes through ProjectSettings/Editor preferences.
