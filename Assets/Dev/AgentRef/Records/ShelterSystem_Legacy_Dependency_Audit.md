# ShelterSystem — Legacy Dependency Audit

Audit date: 2026-10-19. Only this report was created; no implementation, asset edits, deletion, or Unity execution was performed.

Disabling the legacy hierarchy does not remove its C# declarations or serialized component bindings. `ShelterManager.cs` still declares `PlacedShelterData` and `ShelterTypeDefinition`, which the refactored system uses. `ResourceBarRuntimeController` also references the legacy MonoBehaviour itself. `ShelterCandidateController` supplies no shared declaration needed by the refactor, but remains attached in two scenes. Safe removal requires extracting both data classes, migrating the resource bar, and retiring the serialized legacy components. Moving data alone does not make `ShelterManager` removable.

Paths below are relative to the project root; line numbers describe the audited source.

## 1. Legacy declarations

Both scripts are in `Assets/Game/Features/MitigationFeatures/Shelter/`. Neither declares a namespace, enum, interface, or delegate. Their `Action` events use framework delegates rather than declaring new delegate types.

| File and line | Declaration | Role / extraction requirement |
| --- | --- | --- |
| `ShelterManager.cs:7` | `[Serializable] public class ShelterTypeDefinition` | Shared configuration data; extract. |
| `ShelterManager.cs:18` | `[Serializable] public class PlacedShelterData` | Shared placed-shelter data; extract. |
| `ShelterManager.cs:36` | `public class ShelterManager : MonoBehaviour` | Legacy gameplay component; resource bar still depends on this type. |
| `ShelterCandidateController.cs:10` | `public class ShelterCandidateController : MonoBehaviour` | Dormant legacy component; no external C# type consumers found. |
| `ShelterCandidateController.cs:12` | Nested `private sealed class ZoneContext` | Private legacy implementation detail; do not extract. |
| `ShelterCandidateController.cs:22` | Nested `private sealed class ShelterGroup` | Private legacy implementation detail; do not extract. |
| `ShelterCandidateController.cs:1851` | Nested `private struct TileSnapshot` | Private legacy implementation detail; do not extract. |

The new `ShelterCandidateResolver` declares its own private `ZoneContext` and `ShelterGroup`. These are distinct types, not dependencies on the legacy nested classes. New candidate data already lives in `ShelterTypes.cs`; the legacy candidate controller actually consumes `ShelterCandidateResult` and `ShelterZoneOverride` from that new file.

The exact shared data declarations currently embedded in `ShelterManager.cs` are:

```csharp
[Serializable]
public class ShelterTypeDefinition
{
    public string shelterTypeName = "Community Shelter";
    public Sprite shelterSprite;
    public int capacity = 250;
    public int cost = 100;
    [Range(0f, 2f)] public float safetyMultiplier = 1f;
    [TextArea] public string description;
}

[Serializable]
public class PlacedShelterData
{
    public string shelterId;
    public string shelterTypeName;
    public Vector3Int tileCell;
    public Vector3 worldPosition;
    public List<string> associatedZoneGeoids = new();
    public int capacity;
    public float shelterSafety;
    public float capacityCoverage;
    public float routeAccessibility;
    public float shelterMitigationScore;
    public int estimatedPeopleProtected;
    public int exposedPopulationAfterShelter;
    public float exposureReductionPercent;
    public bool isActive;
}
```

These are ordinary serializable reference types, not MonoBehaviours, ScriptableObjects, utilities, or interfaces. Preserve every field, default, attribute, and class modifier above. Do not move mitigation calculations or ID generation into them; those operations already live in `ShelterMetrics`.

## 2. Dependencies from the eight refactored scripts

All eight files under `Assets/Dev/Dev_Shelter/Scripts/` were inspected. The requested “ShelterType” corresponds to the actual `ShelterTypes.cs` file; no `ShelterType` declaration exists there.

| Refactored script | `PlacedShelterData` references | `ShelterTypeDefinition` references |
| --- | --- | --- |
| `ShelterController.cs` | Placement event, registration, retrieval, inspection, and placement results; e.g. lines 59, 354, 396, 413, 658. | Serialized `shelterTypes` list, selected type, public type APIs, defaults; e.g. lines 24, 56, 66, 346, 407, 778. |
| `ShelterCandidateResolver.cs` | None. | None. |
| `ShelterMetrics.cs` | Builds data and computes caches/info; lines 12, 34, 92, 105, 136. | Configuration input to data creation; line 13. |
| `ShelterPlacement.cs` | Registration output; line 44. | Registration input; line 34. |
| `ShelterPlacementInput.cs` | None directly. | None directly. |
| `ShelterPresentation.cs` | Coverage refresh and placed visuals; lines 213, 234, 308. | Ghost/sprite/placed-visual inputs; lines 274, 294, 308, 332, 343, 396. |
| `ShelterTypes.cs` | None. | None. |
| `ShelterState.cs` | Runtime lists, tile lookup, active shelter queries and insertion; lines 10–112. | None. |

Both dependencies are data models declared in the legacy `ShelterManager.cs`, in the global namespace. Removing either declaration causes compilation errors in its consumers. Five refactored scripts reference `PlacedShelterData`; four reference `ShelterTypeDefinition`.

None of the eight directly references the `ShelterManager` or `ShelterCandidateController` MonoBehaviour type. No shared legacy utility, interface, enum, delegate, or private helper needs extraction. Existing map/stamp dependencies are separate from this audit's two legacy shelter scripts.

## 3. Other project consumers

| Consumer | Dependency and current role |
| --- | --- |
| `Assets/Game/Features/MitigationFeatures/Communication/CommunicationPanelController.cs:488,790,934` | Uses `PlacedShelterData` for shelter lookup/details through the new `ShelterController`. Enabled in both audited scenes. |
| `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs:860` | Uses `PlacedShelterData` when excluding occupied shelter tiles through `ShelterController`. Enabled in both scenes. Header comments mentioning the old components are stale comments, not executable dependencies. |
| `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationController.cs:1075,1137,1168,1491,1779` | Uses placed-shelter collections, route targets, coordinates, IDs, type names and capacities through `ShelterController`. Enabled in both scenes. |
| `Assets/Game/Features/UI/Scripts/UI_Advisor_Scripts/AdvisorPanelController.cs:273,371` | Subscribes to new `ShelterController.OnShelterPlaced`; handler accepts `PlacedShelterData`. Enabled in both scenes. |
| `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs:386,395` | Reads new controller's placed shelters for summaries. Enabled in both scenes. |
| `Assets/Dev/Scripts/Main_UI/UI_Overlay/Dev_ResourceBarRuntime/Scripts/ResourceBarRuntimeController.cs:20,279,558,603` | Still declares `[SerializeField] private ShelterManager shelterManager`, searches for it, and subscribes/unsubscribes to its `OnShelterPlaced` event. Handler accepts `PlacedShelterData` and advances turns when configured. This is a remaining MonoBehaviour-type compilation dependency. |
| Legacy `ShelterCandidateController.cs:38,114,484,923,980` | Dormant consumer of `ShelterManager`, `ShelterTypeDefinition`, and `PlacedShelterData`; uses the manager for registration, active-shelter queries and event-driven coverage refresh. |
| Legacy `ShelterManager.cs` itself | Uses both data classes for its catalog, collections, registration and events. Keep these declarations accessible while the component remains. |

The resource bar cannot be classified as harmless dead code: its `AfterSceneLoad` bootstrap can add the component when a matching HUD document exists (`ResourceBarRuntimeController.cs:80–96`). No saved component instance was found, but the existing Editor log records runtime bootstrap activity. Its default lookup excludes the inactive legacy shelter hierarchy, so its old placement-event source is not automatically connected to the new shelter system. Migrating that subscription is a separate integration task; it must avoid subscribing to both old and new sources and advancing turns twice.

No other executable source references to either legacy shelter MonoBehaviour were found outside the two legacy scripts and the resource bar. Historical records and generated `.csproj` includes are not additional gameplay consumers; Unity regenerates project files after script removal.

## 4. Scene, prefab and serialization dependencies

845 serialized `.unity`, `.prefab`, and `.asset` files were scanned, with additional GUID searches in Assets, ProjectSettings and Packages. Both legacy script GUIDs occur as scene component bindings, not prefab bindings.

| Script | Existing `.meta` GUID |
| --- | --- |
| `ShelterManager.cs` | `99c9588008534c42bc36f39928818186` |
| `ShelterCandidateController.cs` | `22b63ec2700e46229b5b3ba3af12f4b0` |

| Scene | Legacy components and references |
| --- | --- |
| `Assets/Game/Content/Scenes/PlayableTilemap.unity` | Manager component `12772933` at line 154 on `Systems/Shelter/ShelterManager`; candidate component `1482495044` at line 26914 on `Systems/Shelter/ShelterController`. Candidate's serialized `shelterManager` points to `12772933`. |
| `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity` | Same component IDs and paths; manager at line 154, candidate at line 26915. Same candidate-to-manager reference. |

In both scenes, the parent `Systems/Shelter` has `m_IsActive: 0`; the legacy components themselves have `m_Enabled: 1`. Their inactivity comes from the hierarchy. The new components are under the separate active `Systems/ShelterSystem` hierarchy.

Deleting both scripts without removing their saved components would leave **four missing-script components across these two scenes**, even though their parent is inactive. Deleting only the manager also leaves the candidate's manager reference unresolved. No other serialized component references to those legacy instances, and no prefab bindings to either script, were found.

`ShelterController.shelterTypes` serializes `ShelterTypeDefinition` inline; the old manager also retains an authored catalog. `ShelterState` stores placed-shelter collections as runtime state. No `[SerializeReference]` usage or managed-reference type-name entries for these classes were found in the scanned source/assets. Preserve the data classes' global full names and current assembly; moving source files is different from changing the data contract or replacing a MonoScript binding.

## 5. Exact cause of the compilation error

`PlacedShelterData` has one declaration: `ShelterManager.cs:18`. Removing/excluding that file removes the data declaration along with its legacy MonoBehaviour. Disabling a component leaves all its file's C# declarations compiled; deleting a source file does not.

The existing Unity Editor log confirms `CS0246` for `PlacedShelterData`, including `ShelterState.cs(10,27)`, and for `ShelterTypeDefinition`, including `ShelterMetrics.cs(13,9)`. It separately confirms `CS0246` for `ShelterManager` at `ResourceBarRuntimeController.cs(20,30)`. These are recorded errors, not a new deletion experiment performed during this audit.

Deleting `ShelterCandidateController.cs` alone does not remove `PlacedShelterData`; that file contains no such declaration. Restoring only `PlacedShelterData` would still leave `ShelterTypeDefinition` errors. Extracting both models would still leave the resource bar's `ShelterManager` dependency, and the legacy candidate controller's manager dependency until that controller is retired.

This is missing source/type ownership, not a missing `using` directive. The current generated `Assembly-CSharp.csproj` includes both legacy scripts and all eight refactored scripts; no separate assembly definition exists in the Dev shelter folder.

## 6. Recommended extraction files

- `Assets/Dev/Dev_Shelter/Scripts/PlacedShelterData.cs` — move only the existing `[Serializable] public class PlacedShelterData` declaration, unchanged.
- `Assets/Dev/Dev_Shelter/Scripts/ShelterTypeDefinition.cs` — move only the existing `[Serializable] public class ShelterTypeDefinition` declaration, unchanged.

Keep the global namespace and `Assembly-CSharp` membership. Preserve all fields, accessibility, attributes, defaults and the list initializer shown above. Add the required existing framework/Unity imports in the new files. Do not create a replacement `ShelterType` class, duplicate definitions, add an assembly definition, or move gameplay calculations.

Each new source file needs its own new, stable Unity-generated `.meta`, committed alongside it. Do not copy/reassign either legacy MonoBehaviour's GUID to a data file. Retain the original legacy `.cs`/`.meta` pairs unchanged in identity until their component bindings are retired.

## 7. Safe migration sequence — future work only

1. Review/checkpoint the current scenes and metadata. Compare the inactive legacy catalogs, overrides and visual assignments with the active refactor before discarding any authored settings.
2. Extract both data declarations in one changeset, removing their original declarations from `ShelterManager.cs` at the same time. Retain both legacy MonoBehaviours and their GUIDs during this phase. Compile with all current consumers; confirm serialized shelter catalogs and sprite references remain intact.
3. Separately migrate the resource bar's lookup and subscription/unsubscription to `ShelterController.OnShelterPlaced`, preserving its handler, turn/cooldown behavior and public APIs. A staged migration can add a new controller reference while retaining the legacy field temporarily. However, a field still typed as `ShelterManager` prevents deleting that type: retiring that private serialized legacy reference requires its own approved consumer change. Do not silently rename/type-change an existing serialized field or repoint its component reference.
4. With that consumer migrated, explicitly remove the legacy candidate and manager components from both scenes and retire their mutual reference. Preserve the new `ShelterSystem` hierarchy. Do not delete an entire parent GameObject without reviewing its other contents and assignments.
5. Verify no executable references to either legacy MonoBehaviour type remain, no serialized bindings reference their GUIDs/component IDs, and there is exactly one definition of each extracted model. Then remove each obsolete `.cs` and its matching `.meta` together under an approved deletion scope. Regenerate Unity project files normally.
6. Compile and reopen both scenes. Check for missing scripts, lost catalog/sprite data and missing-type errors. In Play Mode test candidate display, ghost/placement validation, shelter limits/capacity/mitigation, activation, labels/coverage, Advisor messages, communication/evacuation consumers, summaries, and exactly one resource-bar turn advance per placement. Validate enable/disable event cleanup. Commit only after review.

## 8. Removal risks and limits

- Data extraction alone does not remove the resource bar's concrete legacy type dependency.
- Removing scripts while inactive components remain produces missing scripts; disabling them is insufficient.
- Duplicated declarations, renamed fields/types, changed namespaces/assemblies, or changed defaults/attributes can break compilation or serialized data compatibility.
- Reusing old MonoScript GUIDs for the data files would misdirect existing component bindings. Fresh data-file metadata does not repair those bindings.
- The inactive components retain authored settings; their removal should follow comparison/backup rather than assuming they contain no useful data.
- Static scene activation and existing log evidence do not prove current live runtime state. No compilation, deletion experiment, or Play Mode test was performed in this audit.

Only the requested report was written. All pre-existing source, scene, prefab, asset and metadata contents were preserved.
