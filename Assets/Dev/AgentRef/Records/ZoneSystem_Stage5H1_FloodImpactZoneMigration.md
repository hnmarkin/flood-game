# ZoneSystem — Stage 5H1: Flood impact zone queries

FloodImpactController now queries the existing ZoneController directly. Its stamp dependency was exclusively zone access, so the private stamp field and its two scene assignments were replaced with explicit controller wiring. Flood calculations, event handlers, iteration and coordinate conventions are unchanged. Compilation passes with 203 runtime sources, zero errors and the same 11 warnings. All 51 before/after state comparisons match; the migrated harness passes 280 assertions. Both scenes parse with only the intended reference field changed. Barrier code, existing metadata and unrelated assets remain untouched. Unity import and Play Mode were not executed.

## Preflight and original dependencies

Read the Stage 5A audit and Stage 5FG retirement report. Inspected the current FloodImpactController, FloodDefenseBoxStamp, ZoneController, ZoneIndex, ZoneGeometry and JsonMapLoader at their verified repository paths. The request supplies successful Stage 5FG gameplay verification in PlayableTilemap.

| Original stamp use in FloodImpactController | Classification | Equivalent direct query / return type |
| --- | --- | --- |
| Serialized field; Awake null check/discovery; ValidateReferences | Initialization/reference dependency | Private serialized ZoneController; same one-shot Awake fallback and validation position |
| EnsureZoneIndexReadyForExternalUse in calculation and coordinate diagnostic | Initialization/readiness and zone query | ZoneController.EnsureZoneIndexReadyForExternalUse(), bool |
| GetAllZoneTiles in calculation and coordinate diagnostic | Zone-data query | ZoneController.GetAllZoneTiles(), IReadOnlyDictionary<string, HashSet<Vector2Int>> |
| NormalizedTileToCell in water sampling, fallback center and diagnostic | Zone coordinate conversion | ZoneController.NormalizedTileToCell(Vector2Int), Vector3Int |
| TerrainTilemap null check and GetCellCenterWorld for fallback center | Zone terrain/geometry access | ZoneController.TerrainTilemap, UnityEngine.Tilemaps.Tilemap |

There were no stamp barrier-state queries, placement/control calls, event subscriptions or other compatibility uses to retain. FloodImpactController does not call the stamp's individual-GEOID/world lookup APIs, but their equivalence was also checked as part of the requested query validation.

The stamp's query methods already forward to these exact controller APIs after ensuring configuration. In both scenes, stamp 1046329326 references controller 2070746514. Their TileMapData, terrain and loader assignments match exactly: map GUID 7e81a30567a6c1b44ae537c0db92678b, terrain 1822120955 and loader 1678446140. FloodImpactController uses that same map asset. Direct access therefore uses the same ZoneIndex and mutable dictionary/HashSet objects, with no copied cache or reordering.

## Exact source and scene changes

Modified only Assets/Dev/Scripts/Flood/FloodImpactController.cs:

- Replaced `[SerializeField] private FloodDefenseBoxStamp floodDefense` with `[SerializeField] private ZoneController zoneController`.
- Retargeted the existing null-guarded FindFirstObjectByType fallback in Awake to ZoneController. Both authorized scenes assign it explicitly, so that fallback performs no zone discovery there. No lookup was added to Update, water sampling or zone loops; no component is created.
- Replaced every zone-query/conversion/terrain receiver, including DebugWaterCoordinateAlignment, with zoneController.
- Updated the corresponding missing-reference validation message to name ZoneController.

An exact source comparison confirms these receiver/type/name substitutions are the entire diff. All flood formulas, thresholds, weights, damage/population/mitigation behavior, baseline-center precedence, events/subscriptions, sorting, GEOID normalization, public APIs and ZoneFloodImpactResult fields are unchanged. Existing evacuation-controller discovery during recalculation remains unchanged.

In each authorized scene, changed only MonoBehaviour **35733723**, script GUID **f976661f3d2f4dbeaa227f63d307a845**, on the existing FloodImpactController GameObject:

| Removed serialized assignment | New serialized assignment |
| --- | --- |
| floodDefense: {fileID: 1046329326} | zoneController: {fileID: 2070746514} |

Scenes: Assets/Game/Content/Scenes/PlayableTilemap.unity and Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity. The PlayableTilemap edit uses this request's explicit Assets/Game scene authorization. All other Inspector fields, objects, script GUIDs and renderer settings remain unchanged. No metadata was edited; Unity may generate normal metadata for this new report.

## Coordinates, readiness and failure behavior

ZoneIndex stores normalized tiles by subtracting the compressed terrain cell-bounds origin from absolute cells. NormalizedTileToCell adds that origin once and returns Z=0. WaterController still receives absolute `(cell.x, cell.y)` coordinates; TileMapData population access still receives normalized tiles. Fallback centers still average terrain.GetCellCenterWorld for those absolute cells in the existing iteration order. Baseline centers retain priority.

GEOID dictionaries retain exact ordinal keys, including leading zeros and whitespace. FloodImpactController's own Trim behavior is unchanged; this stage does not normalize the index. The same lookup/set objects preserve membership and iteration order.

ValidateReferences still precedes index readiness. A missing data source or impact map clears cached results and returns without FloodImpactRefreshed; only the source type named in the error changes. Cold-map failures for payload, mapping, GEOID grid, terrain, map or empty zones behave identically. Success populates/sorts results, sets HasCalculatedLiveImpact and then raises the event. Missing/uninitialized water still supplies zero depth. The existing behavior where an initialized index remains usable after payload loss is preserved; invalidating that index restores readiness failure until the loader recovers.

The direct path relies on the verified controller map assignments. The stamp remains attached and retains its startup configuration responsibilities; Stage 5H1 does not copy its query-time resolve/create/reconfigure fallback into FloodImpactController. Runtime reference replacement, adapter retirement and map-reload coordination remain separate work. No missing ZoneController API or blocker was found for the current scene setup.

## Validation

- **Compilation:** installed Unity 6000.0.63f1 Roslyn with current project runtime references; before and after both compile **203 sources, 0 errors, 11 warnings**. Diagnostic sets are identical, including paths/locations/messages; only compiler output order varies.
- **Automated comparisons:** actual before/after FloodImpactController, actual stamp, controller, index, geometry and renderer sources, with temporary Unity/water/mitigation stand-ins. The loader stand-in uses the unchanged actual TryGetTileInfoAtWorld method body. **279 baseline assertions and 280 migrated assertions pass; all 51 captured states match.** The additional migrated assertion verifies direct impact calculation without a stamp component.
- Compared every ZoneFloodImpactResult field, sorted result order, water sample coordinates/order, refresh-event counts/result state, readiness warnings and failure clearing. The intentional missing-source error type was normalized for comparison. Cases include baseline/fallback population and centers, shelter/evacuation mitigation, zero weights, flood-threshold boundaries, missing/uninitialized water, source/map failures, warm cache/invalidation/recovery and shelter/water subscribe-disable-re-enable behavior. Hand-calculated depth/count/damage assertions pass.
- Query checks cover valid/unknown/null/empty/whitespace GEOIDs, valid/outside world positions, multi-cell zones, dictionary/set identity and origins **(0,0), (7,11), (-20,-10), (-3,2)**. Normalized/absolute conversions and cell round trips match.
- **Scene YAML:** Ruby Psych parses 346 PlayableTilemap records and 360 Dev_UINavigation records before and after. No record was added/removed; only 35733723 changed. All other records are byte-identical, including ZoneController, stamp and BarrierController. All nonzero local references resolve, with no duplicate records/component entries. Script identities and existing metadata remain unchanged. Pre-existing unrelated unresolved external script bindings documented in Stage 5FG were not altered; no new script binding was introduced.
- **Scope:** SHA-256 comparison against 6,181 starting Assets files confirms only FloodImpactController and the two authorized scenes changed, with this report added and no deletions. All other starting assets and every existing metadata file are byte-identical. Stage-only whitespace checks pass.
- Temporary test/build evidence is outside Assets at /var/folders/k0/k_vxwym14hs2n16m3n8bt2t40000gn/T/zone-stage-5h1-zq3coiso. Stand-ins establish query/calculation parity, not Unity-native scene execution, rendering or physics. **No Unity Editor/Play Mode execution is claimed.**

## Remaining dependencies and manual verification

FloodImpactController has **zero direct FloodDefenseBoxStamp references**. Other systems still use the stamp, including the unchanged baseline-risk, shelter and evacuation consumers used by flood impact. This stage does not make the entire gameplay graph independent of the stamp. Its component, configuration, barrier forwarding, events and provider APIs remain unchanged for later stages.

PlayableTilemap is the sole required Play Mode test scene; Dev_UINavigation received matching serialized wiring only.

1. Import/compile in Unity. On the actual FloodImpactController component, verify Zone Controller points to Systems/ZoneSystem/ZoneController; baseline, shelter, evacuation, map, water and all numeric settings must retain their prior values. Confirm the existing stamp/barrier components remain attached.
2. Start PlayableTilemap, allow normal map/water initialization, and refresh flood impact. Compare per-zone tile counts, depths, population, risk, damage, mitigation and centers with Stage 5FG. Activate the live overlay normally and verify its event-driven refresh/labels/outlines.
3. Step/reset water, place shelters and apply evacuation changes. Confirm the same calculations and one refresh per subscribed notification, with no callbacks after disabling the impact component and normal recovery after re-enabling.
4. In a disposable Play session, exercise missing/unready references and recovery, including a negative-origin fixture if available; check that water samples align with absolute cells. Do not save temporary runtime reference changes.
5. Exercise Place Barriers entry/exit, placement limits and water interaction; confirm existing barrier behavior is unchanged. Review and commit after manual verification.
