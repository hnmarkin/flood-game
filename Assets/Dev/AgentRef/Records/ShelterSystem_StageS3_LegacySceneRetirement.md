# ShelterSystem — Stage S3

Removed only the two legacy MonoBehaviour records and their owning GameObjects' component-list entries in each scene. The candidate-to-manager reference disappeared with its record. All GameObjects, transforms, hierarchy/activation settings, scripts and metadata remain. The active `Systems/ShelterSystem` records and references are byte-identical. No active consumer referenced either retired component; the active system has its own configuration. Legacy differences below were recorded without copying values.

Scenes modified:

- `Assets/Game/Content/Scenes/PlayableTilemap.unity`
- `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity`

Instances removed in **each** scene:

| Component | Component fileID | Owner GameObject fileID | Original location |
| --- | --- | --- | --- |
| ShelterManager | 12772933 | 12772931 | `Systems/Shelter/ShelterManager` |
| ShelterCandidateController | 1482495044 | 1482495042 | `Systems/Shelter/ShelterController` |

Verified MonoScript GUIDs: manager `99c9588008534c42bc36f39928818186`; candidate `22b63ec2700e46229b5b3ba3af12f4b0`. Both components were enabled beneath the inactive `Systems/Shelter` parent.

Authored configuration comparisons are identical in both scenes:

| Setting | Legacy | Active |
| --- | --- | --- |
| Community Shelter capacity / cost | 1000 / 100 | 250 / 100 |
| Storm Shelter capacity / cost | 3000 / 1000 | 150 / 150 |
| School Shelter capacity / cost | 800 / 500 | 400 / 200 |
| keepPlacementModeActiveAfterPlacement | true | false |
| candidateTileColor, RGBA | (1, .796214, 0, 1) | (.15, .9, 1, 1) |
| candidateAssociatedZoneColor, RGBA | (0, .013331577, .8773585, 1) | (0, .8, 1, .45) |
| placedShelterAssociatedZoneColor, RGBA | (.2805456, 1, 0, .75) | (0, 1, .65, .75) |
| labelParent | `Systems/Shelter` | `Systems/ShelterSystem/ShelterPresentation` |

All 18 candidate settings match, including grouping 8–9 and empty zone overrides. Placement limit 2, duplicate-placement restriction, safety defaults, catalog names/descriptions/multipliers match. Catalog sprites are null in both catalogs; fallback sprite, ghost/prefab references, font, label settings, placement colors and coverage visibility match. No necessary configuration exists exclusively in the retired components.

Validation: YAML syntax parsing, unique record/component IDs, owner links and all local references passed. Record counts: 347→345 and 361→359; exactly 108 lines removed per scene. Each active hierarchy retains 14 unchanged records and all 11 controller references. No missing-script binding was introduced. All 6,109 other existing Assets files are unchanged. Compilation: 202 sources, 0 errors/12 existing warnings. Unity was not executed.

Manual Unity checklist:

- Open both scenes; inspect preserved legacy GameObjects, active ShelterSystem and Console for missing scripts/references.
- Test candidates, labels, ghost/sprites, coverage/mitigation, successful placement advancing at most one turn, failed/cancelled placement advancing none, and cooldown.
- Verify Advisor, communication, evacuation and resource-bar behavior.

Before script deletion: recheck project references; retire both legacy `.cs`/`.meta` pairs together because the candidate still depends on ShelterManager. Keep the extracted Dev data classes. Neither legacy script was deleted here.
