# ZoneSystem — Stage 5C

Removed one unused declaration per source; every other source line, including methods, subscriptions, resolution, channel behavior and shared BaselineRisk semantics, is unchanged.

| Source modified | Field removed (ZoneThinOutlineByHover) | Component in each scene |
| --- | --- | --- |
| `Assets/Dev/Scripts/Zones/HighRiskManager.cs` | zoneOutlineController | 1663569816 |
| `Assets/Dev/Scripts/Flood/FloodImpactOverlayManager.cs` | zoneOutlineController | 1089964579 |
| `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs` | zoneVisualController | 810699005 |
| `Assets/Dev/Dev_Shelter/Scripts/ShelterPresentation.cs` | zoneOutlineHighlighter | 434996198 |
| `Assets/Dev/Scripts/Zones/ZoneBaselineRiskController.cs` | zoneOutlineHighlighter | 1590100951 |

Each field appeared only in its declaration before removal. No executable uses remain.

Removed exactly five corresponding YAML entries pointing to `704415076` in each scene:

- `Assets/Game/Content/Scenes/PlayableTilemap.unity`
- `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity`

Each component's current MonoScript GUID was verified first. Existing ZoneController fields/assignments remain identical, including absent/zero entries; none were created or migrated. Legacy component `704415076`, its GameObject, settings and renderer configuration are byte-identical in both scenes.

Validation: YAML parsing passed; record counts remain 345/359; no dangling local references or duplicate component entries. Only the five specified records changed per scene, each by one field. Compilation: 202 sources, 0 errors/11 existing warnings, no new warnings. Existing metadata and all other source/scene content are unchanged.

Concurrent workspace activity was detected after the initial scope check: `BuildingTileType.asset`, `GrassTileType.asset`, `RoadTileType.asset` and `WaterTileType.asset` changed under `Assets/Dev/Tilemaps/Terrain/`, and terrain sprite/metadata files appeared there. A report `.meta` also appeared during import. This task did not write or revert those assets/metadata. The seven authorized source/scene files still match exactly the intended five declaration and ten YAML-entry removals.

Remaining dependency: the legacy component still owns renderer registration/lifetime, hover, appearance and barrier visibility. Documentation comments mentioning the former fields/type were preserved within the declaration-only scope.

No blockers identified. Unity Play Mode was not executed. Required manual scene: **PlayableTilemap only**.

- Check hover, baseline/live risk, communication hover, shelter candidate zones/tiles and placed coverage.
- Check channel clearing/shared-baseline behavior, buttons, barrier hide/restore and disable/re-enable cleanup.
- Check Console for missing scripts/references and renderer registration failures.
