# ZoneSystem — Stage 5H2 + 5H3: Baseline and communication zone data

Both consumers now use their existing ZoneController for zone queries and coordinate conversion. Baseline's obsolete stamp field was removed. Communication retains its stamp for the existing placement-data asset fallback and initialization lookup. Required scene references are explicitly wired; algorithms, outline channels and barrier gameplay remain unchanged. Compilation passes with 203 runtime sources, zero errors and the same 11 warnings. Independent regressions pass 424 assertions, with all 69 baseline and 53 communication states matching their originals. Scene YAML validates with only essential reference changes. Unity import and Play Mode were not executed.

## Preflight

Read the Stage 5A audit and Stage 5H1 report. Inspected both complete consumer implementations, FloodDefenseBoxStamp, ZoneController, ZoneIndex, ZoneGeometry and the current JsonMapLoader grid/world-lookup paths. The request supplies successful Stage 5H1 gameplay verification in PlayableTilemap.

Both consumers already had a ZoneController field for outlines, but still obtained zone data through the stamp. Neither calls stamp barrier controls/state or subscribes to stamp events. In both saved scenes, stamp 1046329326 points to controller 2070746514. Controller, stamp and baseline share map GUID 7e81a30567a6c1b44ae537c0db92678b, terrain 1822120955 and loader 1678446140. Communication's placement-data asset matches that map, while its separately assigned communication Tilemap 36957482 remains unchanged.

Stamp queries already forward to the existing controller APIs after configuration. The verified assignments allow direct access to the same index, dictionary and mutable membership sets. No new index, collection copy, rendering root or controller was introduced.

## Stage 5H2 — ZoneBaselineRiskController

Source: Assets/Dev/Scripts/Zones/ZoneBaselineRiskController.cs.

| Original dependency | Purpose / return type | Replacement and failure behavior |
| --- | --- | --- |
| floodDefense.TryGetZoneTiles | Uncached center lookup; bool plus HashSet<Vector2Int> | zoneController.TryGetZoneTiles; same GEOID/set and false/null/empty handling |
| floodDefense.GetAllZoneTiles | Calculation membership; IReadOnlyDictionary<string, HashSet<Vector2Int>> | zoneController.GetAllZoneTiles; same lookup/iteration order and no-data return |
| floodDefense.EnsureZoneIndexReadyForExternalUse | Readiness; bool | Same ZoneController method, after all existing stronger map guards |
| floodDefense.NormalizedTileToCell | Row/column lookup and world centers; Vector3Int | Same ZoneController conversion; origin added once |
| floodDefense.TerrainTilemap | Missing terrain fallback; Tilemap | zoneController.TerrainTilemap; missing terrain still fails validation |
| Field/discovery/GetComponent/validation | Initialization dependency | Removed stamp field, stamp discovery and stamp-based controller lookup; retained the existing local/unique-active-same-scene ZoneController resolver |

Existing zoneController is reused for data and rendering. Missing-reference validation and resolver messages now identify the required controller. No other readiness gate changed: payload, nonempty cell mapping, GEOID/category/population grids and N > 0 remain required. Failed recalculation preserves existing risk results; EnsureBaselineRiskCalculated and cached centers retain their existing early-return behavior.

All calculation, aggregation, threshold, color, mitigation, sorting and event code remains unchanged apart from query receivers/guards. BaselineRisk show/clear bodies are byte-identical to the starting snapshot. Shared last-writer/any-clear behavior with HighRiskManager remains intact; no arbitration or separate channel was added.

Scene component **1590100951**, script GUID **c66ee8e505ab40bb8b2899bcd87b2b3e**:

- Both scenes: remove only obsolete floodDefense = 1046329326.
- PlayableTilemap: set existing zoneController from 0 to 2070746514.
- Dev_UINavigation: add the absent zoneController assignment to 2070746514.

**Independent validation:** 287 assertions before and 288 after; **69 matching states**. Compared every ZoneBaselineRiskData field, order, event counts/order, all four risk colors and rendered outline state across all three population modes, mitigation on/off and origins (0,0), (9,-4), (-20,-10). Hand-calculated population, elevation and water-distance inputs pass. Also covered invalid GEOIDs, uncached/cached centers, stronger cold-map failures, retained cached results, missing tile-instance fallback, no-water behavior, shared-channel replacement/clearing, and explicit index invalidation. The additional migrated assertion confirms baseline calculation without a stamp component.

**Remaining direct stamp dependency:** none. Shelter/evacuation dependencies remain in their own unchanged consumers.

## Stage 5H3 — CommunicationTowerController

Source: Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs, explicitly authorized by this request.

| Original dependency | Purpose / return type | Replacement or retained responsibility |
| --- | --- | --- |
| stamp.GetAllZoneTiles | Tower grouping membership; IReadOnlyDictionary<string, HashSet<Vector2Int>> | Existing zoneController.GetAllZoneTiles; same lazy readiness/null/empty behavior |
| stamp.NormalizedTileToCell, two sites | Placement/occupancy cells; Vector3Int | Existing zoneController.NormalizedTileToCell |
| stamp.TerrainTilemap | Missing communication Tilemap fallback; Tilemap | zoneController.TerrainTilemap; explicit communication Tilemap remains preferred |
| Required stamp validation | Zone-data provider availability | Validate zoneController instead; missing controller still prevents generation |
| stamp.MapData | Recover an unassigned placement/category asset; TileMapData | Retained unchanged, before the existing Resources asset fallback |
| Stamp field/discovery/GetComponent | Asset-provider lookup and existing controller-resolution anchor | Retained unchanged; no new search or search loop |

ZoneController has no public MapData getter. This is not needed for the migrated zone queries: Communication retains its own TileMapData field and its existing stamp-backed asset fallback, as permitted for compatibility responsibilities. Removing that fallback would change map selection when the field is unassigned. Accordingly, the stamp field and both serialized assignments remain. This stage does not claim complete stamp independence for Communication.

Only query/conversion/terrain receivers, the provider-validation target/message, relevant resolver messages and three Inspector-documentation lines changed. Controller discovery still occurs at the existing Awake location. No new per-frame component discovery was introduced. Group clustering, centroid/distance math, search radius, category priorities, water/shelter avoidance, occupancy checks, costs, sprites/visuals, selection, hover, activation/status transitions and refresh scheduling are unchanged. CommunicationZones show/clear bodies are byte-identical to the starting snapshot.

Scene component **810699005**, script GUID **98e604977a2f4ef5ac4190852d01e7fa**:

- PlayableTilemap: existing zoneController = 2070746514 already correct; no Communication record edit.
- Dev_UINavigation: add only absent zoneController = 2070746514.
- Preserve floodDefenseBoxStamp = 1046329326, placement map and communicationTilemap = 36957482 in both scenes.

**Independent validation:** 135 assertions before and 136 after; **53 matching states**. Compared tower IDs, absolute cells, world centers, associated-zone order, statuses, visual colors/visibility, event order, tooltip/panel interactions and outline state. Cases span group sizes 1/2/3 and the same three origins, water/shelter avoidance, unique occupied cells, cached generation, explicit regeneration/index invalidation, null/unready providers, map/terrain fallback, hover/outside-map clearing, repeated clicks, activation and preservation of warned status. An eligible hover frame performs no component discovery. The additional migrated assertion confirms generation without a stamp when placement data and controller are explicit.

**Remaining stamp dependency:** placement-asset recovery and the existing initialization anchor. Removing these requires a separately authorized setup/asset-selection decision; no ZoneController API was added here.

## Combined validation and files changed

- Modified exactly the two source paths above and the two authorized scene paths: Assets/Game/Content/Scenes/PlayableTilemap.unity and Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity. Added this report only. No existing metadata was edited; Unity may generate ordinary metadata for the report.
- SHA-256 comparison against 6,183 starting Assets files confirms those four modifications and this report are the complete stage delta, with no deletions. Every other starting asset and all existing metadata are byte-identical, preserving unrelated development. Stage-only whitespace checks pass.
- **Compilation:** installed Unity 6000.0.63f1 Roslyn and current project references; before/after both **203 sources, 0 errors, 11 warnings**. Diagnostic paths, locations and messages are identical.
- **Regression:** **424 migrated assertions pass; all 122 captured states match**. Used actual before/after consumer sources, actual unchanged stamp/ZoneSystem sources and temporary Unity/mitigation/UI stand-ins reused from earlier stages. Stand-ins do not establish Unity-native rendering, callbacks, physics or deferred destruction. Missing-provider diagnostics intentionally name the new dependency; comparisons focus on result/event/rendering state.
- Source comparisons verify 31 complete calculation, event, input, clustering and outline method bodies unchanged. Query data stays in normalized coordinates; row/column lookup and world centers use absolute cells after one origin conversion. GEOID ordinal indexing and existing consumer trimming remain unchanged, including whitespace-key behavior. Cache reuse/invalidation remains explicit; same-loader mutation does not invalidate ZoneIndex automatically.
- **YAML:** Ruby Psych parses 346 PlayableTilemap records and 360 Dev_UINavigation records before/after. Only baseline record 1590100951 changes in PlayableTilemap; only 1590100951 and 810699005 change in Dev_UINavigation. All other records are byte-identical. All nonzero local PPtrs resolve; no duplicate records/component entries, new objects or rendering roots exist. Controller renderer settings, hover/visibility components, stamp and BarrierController records remain unchanged.
- Existing unresolved external script bindings reported in Stage 5FG are untouched; no script identity or new script binding changed. Unity import is still required to assess those unrelated bindings. No blanket claim that the entire scenes are free of pre-existing missing scripts is made.
- Evidence is outside Assets at /private/tmp/zone-stage-5h23-5nr22gyi. **Unity Editor/Play Mode was not executed.** Dev_UINavigation received serialized compatibility cleanup only and was not run.

## Dependencies remaining for Stage 5H4 + 5H5

| Consumer | Remaining stamp responsibility |
| --- | --- |
| **5H4: EvacuationController** | Zone tile queries, terrain fallback, normalized-to-cell conversion and TileOrigin-based inverse conversion; field/discovery/validation still present |
| **5H5: ShelterController** | Zone tiles/terrain and context construction; separately, barrier-mode state and ExitBuildModeFromUI remain and must not be confused with zone access |
| **5H5: ShelterCandidateContext in ShelterTypes.cs** | Stamp-typed FloodDefense field; terrain and readiness forwarding |
| **5H5: ShelterCandidateResolver** | Context-backed GetAllZoneTiles and conversions for candidates, centroids and loader row/column lookup |
| CommunicationTowerController | Retained placement-asset fallback and initialization lookup described above |
| ActionsPanelController / PreparationCardsController | Barrier state, controls and/or mode-event compatibility; later barrier-consumer stages |
| AdvisorPanelController / GameStateSummaryBuilder | Barrier-placement instance event / static all-barriers event; later event migration |
| Dormant legacy WaterSimulator / stamp startup | Serialized IBarrierProvider binding and controller configuration; later adapter retirement |

FloodImpactController and ZoneBaselineRiskController now have no direct stamp reference. No missing equivalent zone-query API blocks these completed migrations. The remaining asset fallback, other consumers and adapter startup prevent treating the stamp itself as ready for deletion.

## Manual verification — PlayableTilemap only

1. Import/compile. Confirm both consumers point to ZoneController 2070746514; baseline has no stamp field; Communication retains its stamp/map/communication Tilemap. Confirm all numeric settings and renderer assignments remain intact. Check the pre-existing external script-binding issues separately if Unity reports them.
2. Calculate baseline risk and compare counts, population, elevation, water distance, original/adjusted scores, categories, colors and centers with Stage 5H1. Exercise shelter/evacuation refresh and cached readiness failures/recovery. Alternate baseline-controller and HighRiskManager show/clear operations to verify existing shared-channel behavior.
3. Toggle Communication, compare tower positions/groups/coverage and category/shelter avoidance, then hover, activate, click again, warn zones and leave the mode. Confirm sprites, tooltips, panel opening, status events and CommunicationZones highlighting/clearing.
4. In disposable runtime changes, test missing controller, unready loader, missing placement asset and terrain fallback; verify the retained map-asset fallback. Check a nonzero/negative-origin fixture if available and explicitly invalidate the index after map edits. Do not save test mutations.
5. Exercise barrier entry/exit and placement alongside both overlays. Review and commit after manual verification. Do not run or delete Dev_UINavigation for this stage.
