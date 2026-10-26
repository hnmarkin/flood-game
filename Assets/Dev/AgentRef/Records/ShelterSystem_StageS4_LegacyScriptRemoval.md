# ShelterSystem — Stage S4

Removed both obsolete script/metadata pairs after dependency checks passed. Their parent directory remains. All active ShelterSystem scripts, ResourceBarRuntimeController, shared data, scenes, prefabs and other metadata are unchanged. No dependency blocker was found.

Deleted:

- `Assets/Game/Features/MitigationFeatures/Shelter/ShelterManager.cs`
- `Assets/Game/Features/MitigationFeatures/Shelter/ShelterManager.cs.meta`
- `Assets/Game/Features/MitigationFeatures/Shelter/ShelterCandidateController.cs`
- `Assets/Game/Features/MitigationFeatures/Shelter/ShelterCandidateController.cs.meta`

Searches covered 6,924 C# files, including package sources: no external dependency on either legacy type. The candidate's only other declarations were private ZoneContext, ShelterGroup and TileSnapshot helpers. After deletion, 6,922 sources contain only two legacy-name mentions, both inside the header comment at `CommunicationTowerController.cs:8–9`.

Both legacy GUIDs were searched across all 6,182 Assets/Packages/ProjectSettings files, including 869 scene/prefab/asset files. No serialized bindings remained, including in PlayableTilemap and Dev_UINavigation. Post-deletion GUID matches are historical Markdown only.

Exactly one global, serializable declaration each remains: `Assets/Dev/Dev_Shelter/Scripts/PlacedShelterData.cs` and `ShelterTypeDefinition.cs`. Neither depends on the deleted components. ResourceBarRuntimeController still uses ShelterController.OnShelterPlaced. All 6,109 other existing Assets files were preserved byte-for-byte.

Compilation passed using Unity's Roslyn compiler/settings: 200 sources, 0 errors/11 existing warnings. Only the two deleted source entries were omitted from a temporary compiler response; generated project files were untouched.

Retained documentation mentions occur in ten historical AgentRef records: ShelterSystem audit/S1–S3, ZoneSystem outline audit, and dated shelter/evacuation reports. These and the communication header comments were preserved within scope.

No unresolved dependency issue was identified. Unity import and Play Mode were not executed during S4. Manual checklist:

- Refresh Unity; check Console for missing types/scripts and allow generated project lists to refresh.
- Open both primary scenes; verify active ShelterSystem references and absence of missing components.
- Test candidates, ghost/sprites, placement/coverage/mitigation, one turn per successful placement, no turn for failed/cancelled placement, and cooldown.
- Verify resource overlay, Advisor, communication and evacuation behavior.
