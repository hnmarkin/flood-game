# EvacuationSystem — Stage E1: Dependency and Behavioral Audit

Audit date: 2026-10-26 (America/New_York). Scope: current working-tree source and serialized assets, including pre-existing uncommitted refactors. E1 creates only this report and its normal `.meta`. No production source, scene, prefab, Inspector setting, UI asset, or project setting was changed. No Unity Editor import, player build, or native Play Mode test was performed. The deleted Dev_UINavigation scene was neither restored nor run.

## 1. Executive summary

EvacuationController is the existing public MonoBehaviour facade and currently owns generation, A*, mutable route/selection/confirmation state, greedy capacity allocation, input, reference discovery, and presentation coordination. Seven external runtime-source consumers depend on it. Its public identity, 5 properties, 22 methods (including overloads/aliases), 6 C# events, serialized fields, and global SimpleEvacuationRoute contract must survive extraction.

The route tooltip with an active gameplay call path is MapHoverTooltipController. EvacuationRouteTooltipController is **serialized and enabled in PlayableTilemap, with no external source caller**; its lifecycle creates/finds and hides an independent legacy label. Thus it is serialized-only as a route-service dependency, and its intended tooltip responsibility is duplicated by MapHoverTooltipController. It is not absent or an uninstantiated dead file. Keep it intact.

The narrowest safe E2 is extraction of the existing A* and traversal logic into a plain EvacuationPathfinder, retaining discovery, serialized settings, generation, collections, events, input, and visual/tooltip components in the current facade. Do not optimize search, sort collections, repair failure semantics, change tooltips, or introduce another MonoBehaviour. Future source edits under Assets/Game need explicit stage authorization; E1 grants none.

Fresh managed verification: **207 runtime sources compile, 0 errors, 11 existing warnings; 79 managed assertions pass, with 14 captured data/path/event/rendering traces**. The harness executes the actual controller and visualizer against controlled engine/provider/UI stand-ins outside Assets. These results do not claim scene-native gameplay or actual UI layout. See §16 for reproduction and limits.

Evidence notation: `C:L` = EvacuationController.cs line L; `V:L` = EvacuationRouteVisualizer.cs; `T:L` = EvacuationRouteTooltipController.cs; `M:L` = MapHoverTooltipController.cs. Line numbers describe the audited working-tree versions, not historical records.

## 2. Script paths, GUIDs, and assembly ownership

| Key | Exact repository path | `.meta` GUID |
| --- | --- | --- |
| C | `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationController.cs` | `ef31d31705ac48f898e5746ce90d32fa` |
| V | `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationRouteVisualizer.cs` | `37785d2c97374ef3abdc0d8dc1ba82b6` |
| T | `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationRouteTooltipController.cs` | `908e8eaeef6084fcf840ee5b5e6f5cad` |
| M | `Assets/Dev/Scripts/Zones/MapHoverTooltipController.cs` | `42eb184f08424502a75b2bf59696fd7b` |
| Shelter | `Assets/Dev/Dev_Shelter/Scripts/ShelterController.cs` | `889273345757b49ba982c2c5ca64aa4b` |
| Zone | `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneController.cs` | `32a2bd6974cc48f781edac70b6557993` |
| Baseline | `Assets/Dev/Scripts/Zones/ZoneBaselineRiskController.cs` | `c66ee8e505ab40bb8b2899bcd87b2b3e` |
| Map | `Assets/Game/Features/LegacyWaterSystem/ScriptableObjects/TileMapData.cs` | `194c942412658f943a92a424432fd5c3` |
| HUD | `Assets/Dev/Scripts/Zones/GlobalHUDController.cs` | `6ece4d5d3dc514adc827a920d9c395f1` |

All three target scripts are global-namespace types in **Assembly-CSharp**, confirmed by their `<Compile Include>` entries in Assembly-CSharp.csproj and the absence of an enclosing `.asmdef`/`.asmref`. All nine types above belong to that default runtime assembly. SimpleEvacuationRoute is global, `[Serializable]`, and defined at C:29–43 in the controller file. No separate script asset or GUID exists for that data class. C and V metadata specify MonoImporter executionOrder 0; T metadata contains only file format/GUID and sets no custom order. No target has DefaultExecutionOrder, RequireComponent, or DisallowMultipleComponent attributes.

The only explicit assembly boundaries under Assets are the separate WaterSystemRefactor runtime and test assemblies. They do not own these scripts. The repository has 55 existing scenes and 159 prefabs; all were searched read-only for target identities and references. Packages and ProjectSettings contained no target-type name dependency. Historical markdown is evidence of prior intent only, not active wiring.

## 3. Serialized Inspector inventory

These tables contain **all 44 serialized fields** in the three target scripts: 23 controller, 16 visualizer, 5 legacy tooltip. Declarations omit the common `private` and `[SerializeField]` syntax but retain types, names, and source initializers. An absent initializer means the C# default (null/false/zero). Scene values take precedence over source initializers; Awake and configuration calls can then overwrite runtime state.

### EvacuationController

| Serialized declaration (source default) | PlayableTilemap value |
| --- | --- |
| `ZoneBaselineRiskController baselineRiskController;` | `{fileID: 1590100951}` |
| `ShelterController shelterController;` | `{fileID: 1420635165}` |
| `ZoneController zoneController;` | `{fileID: 2070746514}` |
| `TileMapData tileMapData;` | `{fileID: 11400000, guid: 7e81a30567a6c1b44ae537c0db92678b, type: 2}` |
| `Tilemap routeTilemap;` | `{fileID: 36957482}` |
| `EvacuationRouteVisualizer routeVisualizer;` | `{fileID: 1117061671}` |
| `MapHoverTooltipController mapHoverTooltip;` | `{fileID: 0}` |
| `UIDocument routeTooltipUIDocument;` | `{fileID: 2044171025}` |
| `Camera mainCamera;` | `{fileID: 917516086}` |
| `Color routeColor = new(0f, 0.75f, 1f, 1f);` | `{r: 1, g: 0, b: 0.3026948, a: 1}` |
| `bool debugLogs = true;` | `1` |
| `bool previewModeActive;` | `1` |
| `int maxPathSearchIterations = 6000;` | `6000` |
| `int minimumVisibleRouteLengthTiles = 8;` | `8` |
| `bool skipShortRoutes = true;` | `1` |
| `float roadTileCost = 1f;` | `1` |
| `float landTileCost = 2f;` | `3` |
| `float waterTileCost = 999f;` | `5` |
| `float deepFloodWaterThreshold = 0.5f;` | `0.5` |
| `bool allowRouteSelection = true;` | `1` |
| `bool limitSelectedRoutes = true;` | `0` |
| `int maxSelectedRoutes = 1;` | `3` |
| `bool lockRouteAfterSelection = false;` | `1` |

### EvacuationRouteVisualizer

| Serialized declaration (source default) | PlayableTilemap value |
| --- | --- |
| `Tilemap routeTilemap;` | `{fileID: 36957482}` |
| `LineRenderer linePrefab;` | `{fileID: 1963393216}` |
| `Material lineMaterial;` | `{fileID: 2100000, guid: 5c951169c35ea49eab614112f3848aa0, type: 2}` |
| `Color routeColor = new(0f, 0.75f, 1f, 1f);` | `{r: 0.9056604, g: 0, b: 0.27954072, a: 1}` |
| `float routeLineWidth = 0.08f;` | `0.05` |
| `string sortingLayerName = "Default";` | `Default` |
| `int routeSortingOrder = 2300;` | `2300` |
| `float routeZOffset = -0.05f;` | `-0.05` |
| `bool debugLogs = true;` | `1` |
| `bool hideUnselectedRoutesWhenSelected = true;` | `1` |
| `Color previewRouteColor = new(0f, 0.85f, 1f, 0.25f);` | `{r: 0.90588236, g: 0, b: 0.28235295, a: 0.6117647}` |
| `Color hoveredRouteColor = new(0f, 0.85f, 1f, 0.75f);` | `{r: 0.90588236, g: 0, b: 0.28235295, a: 0.8627451}` |
| `Color selectedRouteColor = new(0f, 0.85f, 1f, 1f);` | `{r: 1, g: 0, b: 0.39857292, a: 1}` |
| `float previewLineWidth = 0.025f;` | `0.05` |
| `float hoveredLineWidth = 0.045f;` | `0.06` |
| `float selectedLineWidth = 0.055f;` | `0.1` |

### EvacuationRouteTooltipController

| Serialized declaration (source default) | PlayableTilemap value |
| --- | --- |
| `UIDocument tooltipUIDocument;` | `{fileID: 2044171025}` |
| `Vector2 cursorOffset = new(16f, 18f);` | `{x: 5, y: -50}` |
| `float maxTooltipWidth = 240f;` | `240` |
| `float screenEdgePadding = 8f;` | `8` |
| `bool debugLogs;` | `0` |


C:252–260 OnValidate clamps iterations and maxSelectedRoutes to ≥1; three movement costs to ≥0.01; flood threshold and minimum route length to ≥0. V:64–69 clamps all four widths to ≥0.005. T:46–49 clamps width to ≥80 and edge padding to ≥0. These are Editor validation behaviors, not new runtime guards to add during extraction.

The controller's `routeColor` is presently unused beyond declaration. `waterTileCost` is serialized/validated but never used to traverse water. Preserve both fields. Awake forces `previewModeActive=false` despite the scene's saved 1. Controller configuration overwrites the visualizer's Tilemap and debug flag and the shared tooltip's document/debug flag. Scene `limitSelectedRoutes=0` disables the count cap, but `lockRouteAfterSelection=1` still locks the first successful selection.

MapHoverTooltipController has no scene/prefab component. Its runtime-created defaults are: tooltipUIDocument null; evacRouteTooltipUxml null; communicationTowerTooltipUxml null; CommunicationController reference null; tooltipOffset (18,18); screenEdgePadding 8; fallbackTooltipWidth 260; fallbackTooltipHeight 120; debugLogs true (M:43–54). Evacuation supplies its assigned document and debug flag. Communication can configure the same discovered instance; this shared-host behavior is an existing coupling.

## 4. Public API inventory

### EvacuationController exact declared surface

```csharp
public bool IsPreviewModeActive => previewModeActive;
public SimpleEvacuationRoute CurrentRoute => _availableRoutes.Count > 0 ? _availableRoutes[0] : null;
public IReadOnlyList<SimpleEvacuationRoute> CurrentRoutes => _availableRoutes;
public bool HasConfirmedEvacuationRoute => hasConfirmedEvacuationRoute;
public string ConfirmedRouteId => confirmedRouteId;

public event Action OnEvacuationRoutesChanged;
public event Action OnEvacuationRoutesBuilt;
public event Action OnEvacuationRoutesHidden;
public event Action<SimpleEvacuationRoute> OnEvacuationRouteSelected;
public event Action<SimpleEvacuationRoute> OnEvacuationRouteDeselected;
public event Action<SimpleEvacuationRoute> OnEvacuationRouteConfirmed;

public bool IsPreviewActive()
public IReadOnlyList<SimpleEvacuationRoute> GetAvailableRoutes()
public IReadOnlyList<SimpleEvacuationRoute> GetSelectedRoutes()
public bool IsRouteSelected(string routeId)
public void ShowOnlySelectedRoute(string routeId)
public void ShowOnlySelectedRoute(int routeIndex)
public void ClearRouteSelectionAndShowAll()
public void ToggleEvacuationPreview()
public void ToggleEvacuationPreviewMode()
public void ShowEvacuationRoutes()
public void HideEvacuationRoutes()
public void RefreshEvacuationRoutes()
public void ShowSingleEvacuationRoute()
public void HideEvacuationRoute()
public void RefreshSingleRoute()
public bool HasEvacuationRouteForZone(string geoid)
public bool HasSelectedEvacuationRouteForZone(string geoid)
public bool TrySelectRoute(string routeId)
public float GetEvacuationMitigationForZone(string geoid)
public int GetEvacuatedPopulationForZone(string geoid)
public int GetRemainingPopulationAfterEvacuation(string geoid)
public int GetTotalEvacuatedPopulation()
```

| Members | Required behavior |
| --- | --- |
| IsPreviewModeActive / IsPreviewActive | Same current flag; no initialization. |
| CurrentRoute / CurrentRoutes / GetAvailableRoutes | First entry or null / the **same live backing list**, not copies or automatically generated data. Read-only interface does not make mutable route objects immutable. |
| GetSelectedRoutes | A new List each call; valid, non-null routes with nonempty selected IDs, in available-route order. |
| IsRouteSelected | Trim/empty normalization; ordinal selected-ID membership, even if an ID has become stale after failed rebuild. |
| HasEvacuationRouteForZone | Trimmed ordinal zone equality among valid cached routes. |
| HasSelectedEvacuationRouteForZone | Same, also nonempty ID and selected membership. |
| ToggleEvacuationPreview / ToggleEvacuationPreviewMode | Hide if currently active, otherwise Show; second is an alias. |
| ShowEvacuationRoutes / ShowSingleEvacuationRoute | Rebuild every time; placement-active branch keeps preview false. Alias does not restrict to one route. |
| HideEvacuationRoutes / HideEvacuationRoute | Clear hover/visible lookup/lines/tooltip; retain available data, selections, confirmation and mitigation. Event gating in §11. |
| RefreshEvacuationRoutes / RefreshSingleRoute | Resolve/configure, destructive rebuild attempt, conditional cleanup/recalculation, presentation, events; alias. |
| TrySelectRoute | Add/idempotent selection only; no public toggle/deselect/confirm method. Failure returns false without selection events. |
| ShowOnlySelectedRoute(string) | TrySelectRoute then visualizer call; **original string argument** is forwarded, although TrySelectRoute normalizes internally. |
| ShowOnlySelectedRoute(int) | Indexes available list; bad index or null/empty route warns/returns, otherwise delegates by ID. |
| ClearRouteSelectionAndShowAll | Explicitly clears confirmation even when locked. An already-clear fast path only asks the visualizer to redraw cached previews and emits nothing. |
| GetEvacuationMitigationForZone | Trim; unknown/empty →0; cached value Clamp01. |
| GetEvacuatedPopulationForZone / GetTotalEvacuatedPopulation | Unknown/empty zone →0; nonnegative cached values. |
| GetRemainingPopulationAfterEvacuation | Empty→0; max(0, knownPopulation−evacuated). Known population scans all route records for the zone, takes maximum, otherwise consults baseline data. That fallback **can calculate baseline risk and publish its events**; do not silently enforce side-effect-free getter rules during this refactor. |
| HasConfirmedEvacuationRoute / ConfirmedRouteId | Stored state, including null initially/after clear; committed before Selected is published. |

### Global route data contract

```csharp
[Serializable]
public class SimpleEvacuationRoute
{
    public string routeId;
    public string sourceZoneGeoid;
    public int sourceZonePopulation;
    public string destinationShelterId;
    public string destinationShelterType;
    public int destinationShelterCapacity;
    public Vector3Int startCell;
    public Vector3Int shelterCell;
    public List<Vector3Int> pathCells = new();
    public float distanceTiles;
    public bool isValid;
}
```

These are Unity-serializable public fields without NonSerialized attributes. They are not a ScriptableObject or MonoBehaviour. Controller route collections are private/nonserialized, so current scene YAML contains none of this runtime route data. Keep class name, global namespace, field names/types/initializer, mutability, and shared route/path object identity; no record/struct/immutable replacement or type move across assemblies in initial stages.

### Visualizer and legacy tooltip compatibility

V has no public events. Its one property and 18 methods are:

```csharp
public bool HideUnselectedRoutesWhenSelected => hideUnselectedRoutesWhenSelected;
public void Configure(Tilemap tilemap, float lineWidth, int sortingOrder, float zOffset, bool enableDebugLogs)
public void SetTilemap(Tilemap tilemap)
public void SetDebugLogs(bool enableDebugLogs)
public void ShowRoute(List<Vector3Int> pathCells, Color color)
public void ShowRoutes(IReadOnlyList<SimpleEvacuationRoute> routes, Color color)
public void DrawRoutes(IReadOnlyList<SimpleEvacuationRoute> routes, string hoveredRouteId, HashSet<string> selectedRouteIds)
public void ShowOnlySelectedRoute(string routeId)
public void ShowOnlySelectedRoute(int routeIndex)
public void ClearRouteSelectionAndShowAll()
public void HideAllRoutes()
public void SetRouteVisible(LineRenderer routeVisual, bool visible)
public void SetRouteVisible(GameObject routeVisual, bool visible)
public void ClearRoute()
public void ClearRouteVisuals()
public void ClearRoutes()
public void ClearAllEvacuationVisuals()
public void DrawRoute(List<Vector3Int> pathCells, Color color)
public void ShowSimpleRoute(List<Vector3Int> pathCells, Color color)
```

The controller directly calls SetTilemap, SetDebugLogs, DrawRoutes, ClearRoute, ShowOnlySelectedRoute(string), ClearRouteSelectionAndShowAll, and reads HideUnselectedRoutesWhenSelected. No other runtime-source caller of V was found. Preserve unused aliases/overloads too. ShowRoutes ignores its `color` argument and uses state styling; DrawRoute appends without clearing existing lines, whereas ShowRoute clears. ClearRoute and its four clear/hide aliases retain caches. These differences are compatibility behavior.

T has no public events. Preserve `public bool IsTooltipVisible => _isVisible;`, `public void SetTooltipUIDocument(UIDocument uiDocument)`, `public void SetDebugLogs(bool enableDebugLogs)`, `public void ShowTooltip(string tooltipText, Vector2 screenPosition)`, `public void UpdateTooltipPosition(Vector2 screenPosition)`, `public void HideTooltip()`. No external source callers exist. Do not rename its class, fields, GUID or serialized component even though route hover uses M.

## 5. Direct consumer and event-subscriber matrix

All paths are exact current paths. The seven external consumer files compile in Assembly-CSharp; default runtime compilation includes Dev and Old_UI_Ref unless explicitly excluded by an assembly boundary. Compilation alone does not prove an instance is active in a scene.

| Direct consumer / exact path | Controller members used | Event subscriptions / observable consequence | PlayableTilemap binding |
| --- | --- | --- | --- |
| `Assets/Dev/Scripts/PanelController/ActionsPanelController.cs` | ToggleEvacuationPreview; IsPreviewModeActive | Built + Hidden → OnEvacuationPreviewStateChanged → refresh action button state. OnEnable/bind/toggle paths subscribe with guard; OnDisable removes. Cited at 155–167, 447–451, 502–505, 550/570. | Component `1389987227`, field evacuationController=`315251714` |
| `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs` | GetAvailableRoutes, GetSelectedRoutes, IsPreviewActive, GetTotalEvacuatedPopulation, IsRouteSelected, GetEvacuatedPopulationForZone, GetEvacuationMitigationForZone, GetRemainingPopulationAfterEvacuation | Built/Hidden/Selected/Deselected → synchronous RefreshSummary with corresponding reason; guarded OnEnable/OnDisable subscription (178–181, 232–235, 415–460, 617–634). | Component `2019949261`, same field target |
| `Assets/Game/Features/UI/Scripts/UI_Advisor_Scripts/AdvisorPanelController.cs` | HasConfirmedEvacuationRoute | Selected + Confirmed (279–280, 320–321, 386–414). Selected handler returns if already confirmed, so locked first selection produces the confirmation wording once. OnEnable/OnDisable own subscriptions. | Component `515287497`, same field target |
| `Assets/Dev/Scripts/Main_UI/UI_Overlay/Dev_ResourceBarRuntime/Scripts/ResourceBarRuntimeController.cs` | No controller method call; typed field/discovery and Selected payload | Selected → RegisterMitigationAction("EvacuationRouteSelected") if flag true; unscaled-time cooldown can suppress duplicate turn actions (195–222, 612–618, 632–633, 692–695). OnEnable/OnDisable own subscription. | No serialized component anywhere; RuntimeInitializeOnLoadMethod AfterSceneLoad discovers ResourceBar UIDocument and AddComponent at 81–97. Resolves evacuation via FindFirstObjectByType at 292. |
| `Assets/Dev/Scripts/Zones/ZoneBaselineRiskController.cs` | GetEvacuationMitigationForZone, GetEvacuatedPopulationForZone | **No evacuation event subscription**. Pulls on adjusted-risk refresh (452,459), listens to shelter changes independently. Reciprocal dependency: C requests baseline population. | Component `1590100951`, same field target |
| `Assets/Dev/Scripts/Flood/FloodImpactController.cs` | GetEvacuationMitigationForZone, GetEvacuatedPopulationForZone | **No evacuation event subscription**. Pulls during live-impact refresh (314,348), triggered by water reset/step and shelter callbacks. | Component `35733723`, same field target |
| `Assets/Dev/Dev_CommunicationSystem/CommunicationPanelController.cs` | HasSelectedEvacuationRouteForZone, HasEvacuationRouteForZone | No evacuation event subscription. Pulls when populating/updating zone tabs (516–518). | Component `2100000004`, same field target |

All seven resolve missing evacuation references with FindFirstObjectByType; some query paths retry. Six are explicitly scene-wired. No direct controller dependency was found in ShelterController, ZoneController or GlobalHUDController: they are providers/hosts. V's mention of EvacuationController is documentation, not a back-reference.

| Public event | External subscribers found | Publication scope |
| --- | --- | --- |
| OnEvacuationRoutesChanged | **None** in current source | Hover changes; refresh regardless of success; successful selection/deselection/clear/replacement; nontrivial hide |
| OnEvacuationRoutesBuilt | ActionsPanelController; GameStateSummaryBuilder | Successful reference-valid rebuild, including zero routes and preview hidden |
| OnEvacuationRoutesHidden | ActionsPanelController; GameStateSummaryBuilder | Nontrivial Hide; placement suppression branch in Refresh |
| OnEvacuationRouteSelected | AdvisorPanelController; ResourceBarRuntimeController; GameStateSummaryBuilder | New successful add, after mitigation/redraw |
| OnEvacuationRouteDeselected | GameStateSummaryBuilder | Explicit clear/toggle/replacement, not stale-ID cleanup |
| OnEvacuationRouteConfirmed | AdvisorPanelController | First new selection with lock enabled |

No persistent UnityEvent callback targets C, V, or T component fileIDs in current scenes/prefabs/assets; no matching m_MethodName/m_TargetAssemblyTypeName binding or reflection/SendMessage dispatch to these types/APIs was found. C# event calls are synchronous, unguarded by try/catch. Event listeners can re-enter controller queries/actions; exceptions interrupt subsequent publications. Inter-component delegate order follows actual subscription order, not the order of this table. Default script order does not establish global startup/subscriber order.

### Outgoing dependencies and dynamic acquisition

| Source | Dependency / operations | Discovery / creation behavior |
| --- | --- | --- |
| C | ShelterController.GetActiveShelters, IsShelterPlacementModeActive; OnSheltersChanged; ShelterPlacementModeChanged | Missing shelter searched in ResolveReferences and placement checks; subscriptions attempted only in OnEnable, not Refresh. A shelter discovered late during Refresh is not automatically subscribed. |
| C | ZoneController.TryGetZoneTiles, TerrainTilemap, NormalizedTileToCell, CellToNormalizedTile | One fallback search attempt per C instance if unassigned, guarded by `_hasAttemptedZoneControllerResolution`. No zone component creation; explicit later assignment can recover. |
| C | ZoneBaselineRiskController.HasCalculatedBaselineRisk, EnsureBaselineRiskCalculated, TryGetRiskData.rawPopulation | Missing baseline searched each ResolveReferences. Optional for generation; population zero if absent/unknown. Baseline calculation can synchronously emit its own events during generation/getter fallback. |
| C | TileMapData.N/sizeX/sizeY/Get/water; TileInstance.category/waterHeight/tileType.isWater | Missing map scans loaded Resources.FindObjectsOfTypeAll and accepts first N>0 or sizeX>0; no sorting or map-loader readiness wait. |
| C | V configuration/draw/clear/selection | GetComponent on self, then GetComponentInChildren(includeInactive=true), then new child GameObject named EvacuationRouteVisualizer + AddComponent. Does not search the whole scene for V. |
| C | M.IsEvacRouteTooltipVisible, SetTooltipUIDocument, SetDebugLogs, ShowEvacRouteTooltip, HideEvacRouteTooltip | FindFirstObjectByType; if absent, create child GameObject named MapHoverTooltipController + AddComponent. **Never creates or calls T.** |
| C/T/M | UIDocument.rootVisualElement, UI Toolkit queries; GlobalHUDController.Instance.TryGetComponent(out UIDocument) | Explicit assigned document wins. Otherwise singleton HUD, then unsorted search including inactive UIDocuments. See §10. |
| C | Camera.main, ScreenToWorldPoint; Input.mousePosition/GetMouseButtonDown; Tilemap.WorldToCell/HasTile | Camera retried during Update if absent. No EventSystem/UI pointer exclusion and no camera ray/plane intersection. |
| V | Tilemap.GetCellCenterWorld; LineRenderer; optional prefab/Material/Shader | Per-cell border GameObjects/LineRenderers created or prefab instantiated on pool growth; runtime material cloned/created lazily. No gameplay provider or controller lookup. |
| T | UIDocument; Label/style/Screen | Creates a Label only, no GameObject, route calculation, input polling, event subscription or controller discovery. |
| M | UIDocument/UXML; DemoDisplayIdFormatter; CommunicationController | Clones UXML roots if missing; optional Editor-only AssetDatabase fallback; communication also configures/uses this shared tooltip. |

### Indirect and historical dependencies

Baseline and live-impact systems combine shelter/evacuation ratios as `1 - ((1 - Clamp01(shelterMitigation)) * (1 - Clamp01(evacuationMitigation)))`. This belongs to those consumers, not C's allocation formula. Their callbacks and summary/turn/advisor events can create further UI changes without direct evacuation calls. DemoDisplayIdFormatter (`Assets/Dev/Scripts/Main_UI/UI_Flow_Controllers/Dev_UIIntroOutro/Utilities/DemoDisplayIdFormatter.cs`) maps route IDs to E-01 etc. on first use via a static ordinal dictionary; presentation order can therefore affect short display IDs even when gameplay IDs are unchanged. Preserve access order.

ZoneController delegates to ZoneIndex and shares mutable HashSet membership, rather than returning a sorted/copy snapshot. ShelterState.GetActiveShelters returns a new list in `_placedShelters` insertion order, skipping null/inactive entries; ShelterPlacement and ShelterMetrics construct normalized associated GEOID lists in supplied order, first occurrence retained. ShelterMetrics generates random shelter IDs via Guid.NewGuid().ToString("N"); C never generates shelter IDs. Stable cross-session route IDs cannot be assumed.

Historical records from July–September and ZoneSystem_Stage5H45 mention earlier prototypes, stamp queries and the now-deleted Dev_UINavigation scene. Current C has no FloodDefenseBoxStamp field/call/subscription. Current shelter uses BarrierController for barrier-mode integration. Old records do not authorize restoring the deleted scene or migrating retired interfaces. A complete current textual dependency index appears after §17.

## 6. Route generation behavior

C:1072–1267, 1382–1510, 1769–1805 define the sequence:

1. Clear the existing available backing list **before** validating all five required references (shelter, zone, map, route Tilemap, visualizer). Every missing reference warns; no short-circuit after the first. Missing baseline, tooltip, document or camera does not invalidate generation.
2. If validation fails return false. Refresh then skips selected-ID cleanup and mitigation recomputation: available routes become empty while selected IDs/confirmation/mitigation can remain stale. Do not change this into transactional retention or a full reset in E2.
3. Obtain GetActiveShelters. Null/empty is a **successful** rebuild with zero routes. Iterate the supplied list by index, checking null/isActive again; no sorting, prioritization or global duplicate shelter filtering.
4. Resolve shelter normalized coordinates: first convert stored tileCell and check logical map bounds; only if invalid use routeTilemap.WorldToCell(worldPosition), then the same provider-based inverse/bounds check. A valid-but-wrong stored cell wins over world position. Neither conversion tests passability; A* later tests it. Invalid destination warns/skips that shelter.
5. For each shelter, require nonempty associatedZoneGeoids or warn/skip. Create a per-shelter HashSet<string>; iterate the original list by index, trim GEOIDs (whitespace/null→empty), skip empties and duplicates, with case-sensitive equality. No case folding, leading-zero conversion, numeric parsing, or cross-shelter deduplication.
6. Resolve safe shelter ID each iteration: trimmed ID or literal `Unknown Shelter`. If ZoneController.TryGetZoneTiles succeeds with a nonnull set containing the shelter normalized tile, skip the whole zone→shelter route. Unknown membership is not treated as inside; anchor lookup then normally fails.
7. Anchor: query membership again; false/null/empty fails. Arithmetic mean of **all zone tile coordinates**, including tiles later rejected as impassable/out of map, accumulates in HashSet iteration order. Examine that same set without sorting; accept snapshot/passable tiles only. Minimize squared distance to mean **before** category preference. Candidate rejection is exactly `hasCandidate && (distance > bestDistance || (Mathf.Approximately(distance, bestDistance) && priority >= bestPriority))`. Keep this expression: a slightly greater approximately-equal distance still loses in its first clause; a slightly smaller approximately-equal distance can lose on priority. Do not replace with an idealized comparator.
8. Category preference on distance ties: road/street/highway substring→0; building/residential/commercial/industrial/city/shelter→1; land/grass/field/park/forest/soil/terrain→2; other→3. All are lowercased trimmed category substrings, in that branch order. Equal distance/category retains first encountered HashSet tile. Float accumulation and enumeration order matter.
9. Convert chosen anchor to Unity cell using ZoneController. A* runs from its normalized tile to normalized shelter tile. Failed search warns/skips, retaining no invalid placeholder.
10. If skipShortRoutes and `distanceTiles < minimumVisibleRouteLengthTiles`, omit route from available data entirely. Exact minimum passes. This is generation filtering, not merely visibility filtering. Even with short filtering disabled, shelter-inside-zone skipping remains.
11. Construct one new SimpleEvacuationRoute: `routeId = trimmedGeoid + "_to_" + trimmedSafeShelterId`; normalized source GEOID; baseline source population; safe shelter ID/type/capacity; chosen startCell; converted shelterCell; actual returned path list; hop distance; isValid=`pathCells.Count>0`. Append immediately in shelter-major/associated-zone-minor order. Colliding/fallback shelter IDs can generate duplicate route IDs; there is no global collision repair.

Source population trims GEOID, returns 0 for absent baseline/empty/unknown; if baseline has not calculated, invokes EnsureBaselineRiskCalculated and then TryGetRiskData, clamping rawPopulation ≥0. It does not use shelter population caches, current remaining population, path tiles' population sums, selected state, or a different risk formula. Baseline callbacks can observe partially rebuilt available data before Built. Invalid map loader/index readiness is not separately validated: failed membership queries yield a successful zero/subset-route rebuild, then stale selection cleanup occurs.

## 7. A* pathfinding behavior

C:61–67, 1512–1752. Preserve algorithm and its collection operations literally.

| Concern | Existing rule |
| --- | --- |
| Coordinate space | Search uses normalized Vector2Int; outputs absolute Unity Vector3Int cells via provider conversion. Cell Z is discarded by inverse conversion; forward Z=0. |
| Bounds | Requires TileMapData. Width and height both N when N>0; otherwise sizeX and sizeY respectively. Valid XY is [0,width)×[0,height). Does not use GridWidth=N+2/padding, route Tilemap.cellBounds, or serialized range extents directly. |
| Endpoints | Both within bounds and TryGetMovementCost passable before initializing search; start=destination produces one cell, distance 0 if allowed through those guards. |
| Open/closed | List<Vector2Int> openSet seeded with start; HashSet closedSet; cameFrom, gScore, fScore dictionaries. No heap, sorting, diagonal, reopening, smoothing or simplification. |
| Lowest score | Linear scan; missing score→float.MaxValue. Replace best only when score strictly less. Equal f keeps earliest current list entry. RemoveAt retains remaining order. |
| Neighbor order | +X, −X, +Y, −Y, appended in that order if not already open. Closed/out-of-bounds/passability reject first. |
| Relaxation tie | `tentativeGScore >= existingScore` skips: first equal-cost predecessor survives. Updating an already-open node does not move its position. Closed nodes never reopen. |
| Heuristic | ManhattanDistance × min(roadTileCost,landTileCost). Initial g(start)=0; start's tile cost is not charged. Each edge charges destination neighbor's category cost. |
| Costs | Road/street/highway→roadTileCost; all other passable categories→landTileCost. Water cost field is unused. No congestion, capacity, safety, danger or shelter-accessibility penalty. |
| Water blocking | Reject category containing water/river/stream/lake/ocean/flood_source OR tileType.isWater. Reject water depth **strictly greater than** deepFloodWaterThreshold. Exactly threshold remains traversable. |
| Depth source | Nonnegative TileInstance.waterHeight, then max with nonnegative `water[x,y]` if independently in grid bounds. No +1 simulation index offset. Missing/out-of-range water grid supplies no depth; no projection/WaterController query. |
| Snapshot fallback | Get TileInstance (Get catches IndexOutOfRangeException only). Compute cell and HasTile regardless of whether TileInstance exists. A nonnull TileInstance allows traversal even if route Tilemap has no tile. Missing instance + present Tilemap tile uses empty category/0 instance depth (land cost; grid depth can still block). Missing both rejects. If routeTilemap is null, `hasRouteTile` is true, so low-level path calls can accept any missing instance as generic land; normal public generation validation rejects null Tilemap first. |
| Iterations | Increment once per pop while open nonempty and iterations<max. Destination succeeds on its pop, including the last permitted pop. On exhausting max returns false/empty path/distance 0; warning only when debugLogs and iterations>=max, even if open also just became empty. Empty-open failure under cap has no cap warning. |
| Reconstruction | Follow cameFrom backwards from destination, append through start, reverse; no extra cycle guard. Convert every tile in order. distanceTiles=max(0,pathCells.Count−1), a float storing hop count, **not weighted gScore or world distance**. |

Negative terrain origins are supported through ZoneController.NormalizedTileToCell adding TileOrigin once and CellToNormalizedTile subtracting it once. ZoneController.TileOrigin returns the index's captured origin when initialized; otherwise it CompressBounds on terrain and reads bounds min (or zero when absent). A read can thus have provider-side effects; conversion timing is part of compatibility. Explicit route Tilemap is used for HasTile/world fallback/render/input, but its own origin is not the normalization origin. Differing route/terrain origins must not introduce a second offset or be silently unified.

Missing data edge: TileMapData.Get uses a flattened `x*sizeY+y` backing array. C catches IndexOutOfRangeException, not NullReferenceException or arbitrary provider failures. N versus backing-array dimensions can differ. A null tiles array can throw; do not broaden exception handling. If every cross-map corridor is flooded/water, no route is constructed. Flood changes do **not** automatically invalidate cached routes: C subscribes to shelters, not water events. Rebuild sees current map data at query time; no snapshot-wide consistency, traversal caching or permanent obstacle memoization exists.

## 8. Route selection and mitigation behavior

C:100–112, 136–222, 428–477, 617–860, 1269–1402.

- `_availableRoutes`: live generation data, survives Hide; cleared/repopulated in place on Refresh. CurrentRoute is simply its first item.
- `_visibleRoutes`: independently rebuilt valid routes eligible for interaction, available order. Clear empties it. Selected-only filtering depends on V's HideUnselectedRoutesWhenSelected and selected-ID count; it can differ from what V actually draws (§9).
- `routeIdsByCell`: rebuilt from visible routes, in visible-route and path-cell order. Each cell maps to a List<string>; exact duplicate IDs within a cell are suppressed. Null path/empty ID ignored; no extra isValid check here beyond visible filtering.
- `selectedRouteIds`: ordinal HashSet<string>, normalized on user input; selected objects are resolved from available routes when queried. Selection insertion order does not drive allocation or GetSelectedRoutes ordering.
- Confirmation: first new successful TrySelectRoute stores confirmed ID/true immediately if lockRouteAfterSelection and no prior confirmation. Other IDs then fail; clicking confirmed route cannot deselect. A repeated public TrySelectRoute on the confirmed ID returns true silently. No explicit confirm API or separate confirmation click exists. Confirmation persists through Hide/disable/re-enable and is cleared by explicit clear or removal of its stale selected ID.
- Max rule: allowRouteSelection=false rejects all add/toggle attempts. If limit false or selected count<max, accept slot. If full and max=1, deselect every other ID, recalculate/redraw/publish intermediate events, then add new route. If full and max>1, warn/reject without replacement. Lock check runs before cap/replacement. Serialized Min is not a runtime setter clamp.
- Private click toggle removes selected ID, recomputes/redraws, publishes Deselected if route is still available, then Changed; otherwise delegates to TrySelectRoute. Failed IDs or disabled/locked selections emit no public event. A selected stale ID can make TrySelectRoute return true before availability validation.
- Explicit clear snapshots GetSelectedRoutes in available order; clears selected/confirmation; recalculates; redraws active preview or asks V to restore all cached previews when hidden; publishes each snapshot Deselected, then Changed. Already-clear fast path still asks V to redraw but emits nothing. Hidden cached redraw can visibly restore lines while preview remains false.
- Successful rebuild removes missing/invalid selected IDs by HashSet enumeration, clears matching confirmation, emits **no Deselected**; then recomputes mitigation. Failure leaves stale selected/confirmed IDs and cached mitigation. The cleanup early returns when selected set is empty, so it does not independently repair an inconsistent confirmed flag.

Allocation is greedy in **available-route order**, independently of hover, preview, confirmation and selection click order:

1. Clear evacuatedPopulationByZone and evacuationMitigationByZone and reset total to 0. Create ordinal local dictionaries for remaining capacity per shelter and maximum encountered zone population.
2. Iterate valid, non-null available routes with nonempty selected IDs. Trim zone/shelter IDs; skip empty. `zonePopulation=max(0,route.sourceZonePopulation)`; `already=zoneEvacuated or 0`; `remainingZone=max(0,zonePopulation−already)`. Track maximum zone population seen across all these selected route records, even when allocation is zero.
3. First selected route for a shelter ID initializes remaining capacity from that route's nonnegative capacity; later routes with the same ID reuse it, ignoring their own possibly different capacity. `evacuated=min(remainingZone,remainingCapacity)`; always store remaining capacity, even when evacuated=0. If positive, accumulate per-zone count and total.
4. For each zone with positive evacuation, mitigation=`population>0 ? Clamp01((float)evacuated/population) : 0`, using the maximum selected-route population tracked in step 2, or GetKnownZonePopulation fallback. No dictionary entry is required for zero allocation; public getters return 0.

This is **not** ShelterMetrics' capacity-coverage×safety×accessibility formula. It does not reserve capacity already consumed by shelter protection, reduce allocation by communication status, or depend on route distance. Duplicate route IDs cause all matching route records to be treated as selected; first GetRouteById match wins for single-ID lookup. Preserve this existing identity behavior.

Hand-check baseline: A(pop100) then B(pop80) to S1(cap120), both selected → A100, B20, total120, mitigation A1/B0.25, even if B was clicked first. Add S2(cap50) with A then B routes → A remains100, B becomes70, total170, B mitigation0.875. Reordering routes changes capacity distribution. Risk consumers' later combined mitigation and UI refresh cadence are separate contracts (§5).

## 9. Input and visual behavior

C Update polls only while previewModeActive. No visible routes/lookup → SetHoveredRoute(null,zero), then return. Missing route Tilemap or camera → return without clearing existing hover/tooltip; camera is retried with Camera.main. It samples Input.mousePosition for ScreenToWorldPoint and again for XY screen coordinates, forces world z=0, converts via route Tilemap.WorldToCell, and uses exact cell hit testing. No line-distance tolerance, UI pointer guard, input abstraction, phase check, touch handling, or shelter-mode check in this Update.

Overlapping routes: scan per-cell list for the **first selected ID**, otherwise first listed ID. Multiple selected overlaps retain list order, not selection recency/distance. SetHoveredRoute trims ID, converts empty to null, no-ops for identical hover. Changed target stores ID, shows/hides tooltip, redraws, emits Changed. Then Update re-shows current tooltip to update its position if already visible, and finally a left-button-down toggles selection. A newly hovered route can therefore receive ShowEvacRouteTooltip twice in the same frame. Hover notification precedes selection notification. No dedicated hover event exists.

V caching/pooling (V:196–580):

- Public DrawRoutes first ClearRoute, then, only for nonempty routes, shallow-copies nonnull route references, normalized hover and selected IDs into caches. Empty/null draws disable pooled lines **without clearing old caches/visible-only ID**. Cache retains route/path object references, not geometry copies.
- With hideUnselectedRoutesWhenSelected=true and any cached selection, CacheRouteData chooses the first HashSet selected ID for `_visibleOnlyRouteId`; ShouldDrawRoute gives that single ID priority over the general selected-membership rule. **Controller `_visibleRoutes` may contain all selected routes while V draws only one.** HashSet enumeration determines which one; no ordering guarantee should be invented. With hide flag false and no explicit visible-only ID, all valid nonempty-ID routes draw. Explicit ShowOnlySelectedRoute also overrides the flag.
- Style priority selected > hovered > preview, using Inspector colors/widths. DrawRouteTileBorders substitutes routeColor only if passed alpha≤0, enforces alpha≥0.01 and width≥0.005. Controller routeColor is not passed in its DrawRoutes path. No danger, flood-segment, dashed-road, filled selection, or movement animation logic exists.
- Each path cell draws one looped four-point LineRenderer rectangle, based on center and half right/up adjacent-center world vectors, plus routeZOffset on each corner. Adjacent/shared cell borders are not deduplicated. Pool index increments across all drawn route cells. Every redraw clears/disables all pooled lines then reuses them in list order; pool grows to peak demand and never shrinks here.
- Pool reuse reactivates object/enables line. Creation instantiates the assigned **scene LineRenderer's GameObject** under V, retrieves or adds LineRenderer, disables all other Renderer components in its hierarchy; fallback creates `EvacuationRoute_Line_{index+1}` and adds LineRenderer. Other prefab components are not removed. Creation appends to both pool lists; no destroy/repair redesign is authorized.
- Appearance: world-space loop; View alignment; Stretch texture; cap/corner vertices4; start/end/widthMultiplier all supplied width; supplied colors; configured sortingOrder and nonblank layer. Clear sets positionCount0, enabled=false, GameObject inactive, active counter0, leaving caches/pool/material intact. OnDisable calls ClearRoute; no OnDestroy cleanup method exists for runtime material.
- Runtime material: reuse existing runtime copy; else clone assigned material only if nonnull shader and shader name lacks InternalErrorShader; name `EvacuationRouteLine_Runtime`, set `_BaseColor`/`_Color` white if present. Otherwise Shader.Find in order `Sprites/Default`, `Universal Render Pipeline/Unlit`, `Unlit/Color`; all null→no assignment. Each fallback tests null only, not error shader. Lazily creates one shared runtime material, keeps line tint in vertex colors. No new shader blend/queue setup or disposal logic should be added during extraction.

Legacy direct V methods can redraw independently of C's preview/lookup. ShowOnlySelectedRoute on C can call V while preview is false. These are existing inconsistencies to preserve, not grounds to make V an authoritative gameplay-state owner.

## 10. Tooltip ownership and usage

**Classification:** T is referenced only by serialized assets as a component; it is enabled and receives Unity lifecycle callbacks. No external source reference, serialized callback, or controller field invokes its public API. Its route-tooltip role is duplicated/replaced in the active C call path by M. Keep both components intact during initial extraction.

T OnEnable Ensures a label named `evacuation_route_tooltip` (find or programmatic new Label, pickingMode Ignore) then HideTooltip. OnDisable hides. No Update/Awake/Start/events. ShowTooltip(empty/whitespace) hides; otherwise Ensure, set text/display Flex, BringToFront, visible=true, UpdateTooltipPosition. Position updates only if visible and Ensure succeeds. SetTooltipUIDocument no-ops on same document; otherwise clears cached root/label/visible and Ensures new root without removing old label. Ensure applies default hidden styles once: absolute, max width240, black alpha0.82, white, font11, wrapping, padding7 horizontal/5 vertical, corner radius4. It scales screen XY to root dimensions with inverted Y, offset (scene5,−50), clamps to root edges; fallback root dimensions Screen≥1, label width maxTooltipWidth, height96.

C instead uses M (C:566–615): Show resolves route; missing route hides; configure shared host/debug before showing; missing M returns. Position updates call ShowEvacRouteTooltip again only while M says visible; missing hovered route hides. Hide does nothing if M absent/already hidden; it hides only evacuation tooltip, not communication tooltip. Unchanged hover ID does not retry a failed tooltip show, so later UI availability alone does not guarantee recovery until target changes.

M uses existing `evac_tooltip_root` or clones evacRouteTooltipUxml. Missing both warns/returns; **no programmatic fallback route label**. It sets pickingMode Ignore recursively, absolute/hidden/auto size, max width260. It caches/query-warns once per missing label, and still treats the root as usable when individual labels are absent. Show formats route/zone/shelter display IDs using DemoDisplayIdFormatter, fills `zone_id_label` (`E-xx | Zone: Z-xx`), `population_label`, `shelter_id_label`, `shelter_type_label`, `shelter_capacity_label`; populations/capacity clamp nonnegative, fallback text is used for missing shelter type. It sets visible ID/flag, displays/BringToFront/positions; target-change-only debug logging. Hide clears display/ID/flag. OnEnable EnsureTooltipRoots then HideAllTooltips; OnDisable HideAllTooltips. SetTooltipUIDocument resets both tooltip caches/visible flags and Ensures roots when the document changes; does not remove old cloned roots.

M positioning uses RuntimePanelUtils.ScreenToPanel when panel exists; otherwise (screen.x, Screen.height−screen.y). Root/tooltip dimensions use positive, non-NaN resolved size or Screen/fallback260×120; offset18,18 and edge8 clamp. This differs from T's manual screen-to-root scaling; do not merge them.

Document discovery for C and T: assigned document first; GlobalHUDController.Instance with UIDocument next; otherwise FindObjectsByType including inactive, sort None (legacy branch FindObjectsOfType(true)), ignore null/unbuilt roots, remember first usable document, prefer one containing money_label/pop_label/zone_label. M also recognizes evac/communication root names. If none match use first usable fallback, else null. GlobalHUDController has RequireComponent(UIDocument), singleton assignment in Awake, optional persistence/duplicate destruction; startup order can affect whether singleton or fallback wins.

Current scene supplies C/T document `2044171025`, a separate EvacRouteTooltip GameObject. Its source is `Assets/Game/Features/UI/UI_Documents/UI_Panels_Tooltips/EvacRouteTooltip.uxml`, GUID `5dfdfc7f2edc14c218f183d3f63da4ff`; it already contains `evac_tooltip_root` and all five labels and starts display:none. It contains no `evacuation_route_tooltip` label, so T creates its separate hidden label there. Shared M can discover/use the existing route root without cloning.

Existing fallback issue: M's Editor-only AssetDatabase paths point to `Assets/Dev/Dev_Scripts/UI_UXML_USS/UI Editor/UI_Panels_Tooltips/EvacRouteTooltip.uxml` and the corresponding CommunicationTowerTooltip.uxml; those Dev paths do not exist in this checkout. The assigned scene root avoids that route-clone dependency when this document is configured. Player builds have no AssetDatabase fallback at all. Communication also creates/discovers M and can retarget its host; behavior depends on startup/reconfiguration order and available roots. E1 records this; it does not fix paths, add UI assets, or claim native tooltip verification.

## 11. Unity lifecycle and event-order traces

Notation: B=Built, H=Hidden, S(route)=Selected, D(route)=Deselected, F(route)=Confirmed, X=Changed. Arrows are synchronous call order inside C, excluding optional diagnostic logs and arbitrary subscriber re-entry. No target script has Start. C lifecycle: Awake, OnEnable, OnDisable, OnValidate, Update; V: OnValidate, OnDisable; T: OnEnable, OnDisable, OnValidate. V/T have no public events. Component-to-component startup order is not guaranteed by this local trace.

| Trigger | Exact state/presentation/publication sequence and early paths |
| --- | --- |
| C Awake | ResolveReferences (may AddComponent V/M, whose native lifecycle runs synchronously) → configure V → configure tooltip → preview=false → clear hover/visible/lookup → hide tooltip if visible → V.ClearRoute. No C public event. No generation. |
| C OnEnable | Resolve → configure V → configure tooltip → subscribe shelter changes → subscribe placement-mode changes, each guarded. No rebuild, auto-show or public event. |
| C OnDisable | Unsubscribe shelter → unsubscribe mode → Hide. If prior preview/visible/hover state existed, H→X; otherwise no event. Available/selected/confirmed/mitigation persist. |
| Show (placement inactive) | preview=true → Refresh. Successful rebuild: clear/build → stale cleanup → mitigation → redraw → B→X. Even if preview already active, rebuild again. |
| Show (placement active) | preview=false **before** Refresh → rebuild/cleanup/mitigation if valid → clear visuals → B if successful → X → return. Does not emit H merely because Show was blocked. |
| Refresh | Resolve/configure V → clear/build attempt → only on success cleanup+mitigation. If preview && placement: preview=false→clear→H. Else if preview redraw; else clear. Then B iff rebuild succeeded → X always. Failure can therefore emit H→X or X, with no B. Resolve does not configure tooltip here. |
| Hide | Compute hadState=(preview OR visible count>0 OR hover nonempty) → preview=false → clear hover/visible/lookup → hide tooltip → V.ClearRoute → if !hadState return; otherwise H→X. Repeated inactive Hide still clears presentation but emits nothing. |
| Shelter OnSheltersChanged | Direct Refresh, including while preview hidden. Zero active shelters is success and cleans stale selections. Shelter RaiseShelterChangedEvents invokes OnShelterPlaced **before** OnSheltersChanged; other systems may act before C rebuilds. |
| PlacementModeChanged(true) | If !preview && visibleCount=0 return even if hover is inconsistent; otherwise Hide→H→X as gated above. false returns immediately; no automatic preview resume. |
| New hover | Normalize/store changed ID → show/hide route tooltip → redraw visible routes → X. Same ID returns with no redraw/event. Clearing via ClearVisibleRouteVisuals does not use this setter and emits no independent X. |
| Update click on new hover | Hover path→X → if tooltip visible, Show again to follow cursor → toggle selection path. With no overlap or no left click, only hover/position actions occur. |
| New successful select | Reject disabled/empty/locked, or return true for existing selected ID before route validation; lookup first route and reject missing/invalid; prepare slot; add ID; possibly set confirmed state; recalculate mitigation; redraw; S(route) → F(route) iff newly confirmed → X → true. All selection listeners see committed confirmation/mitigation before events. |
| Replacement with max=1 | Snapshot other IDs in selected HashSet order → remove all → recalculate/redraw → D(each available old route)→X; then add new ID/possibly confirm → recalculate/redraw → S→optional F→X. Observers see an intermediate empty-selection state. |
| Click deselect | After allow/empty/lock checks: if confirmed locked ID return false; else remove→recalculate→redraw→D iff route lookup nonnull→X. No confirmation reset in this normal toggle branch. |
| Clear selection | If already clear and unconfirmed, V.ClearRouteSelectionAndShowAll only → return. Otherwise snapshot selected routes in available order → clear IDs/confirmation → mitigation→active redraw or hidden V cached redraw→D(each snapshot)→X. No H or B. |
| Successful stale cleanup | Build valid-ID set → iterate selected HashSet → remove stale → clear confirmation only if matching removed ID. No D/F events; mitigation/presentation then B→X. |
| Visualizer direct calls | DrawRoutes clears pool→possibly updates caches→styles/draws cells. ShowOnlySelectedRoute/clear-selection redraw caches. OnDisable clears pool. No C public events or synchronization of C's hover lookup from V. |
| Tooltip changes | C show/configure/hide calls precede its hover X; clear/hide presentation precedes H/B/X. T and M lifecycle hide routines emit no C event. |

Synchronous consumers matter: ResourceBar can AdvanceTurn (and emit its own UnityEvents) during S before F/X; summary refreshes can query population/risk; advisor inspects already-committed confirmation. C does not subscribe to its own events to refresh baseline/live impact. ZoneBaselineRiskController and FloodImpactController each independently listen to shelter changes, so which observes old/new evacuation mitigation in that dispatch depends on actual subscription order. Do not reorder subscriptions or combine event publications into one final notification.

## 12. Scene serialization findings

Only `Assets/Game/Content/Scenes/PlayableTilemap.unity` is authorized for native gameplay tests. It contains **342 YAML documents**, parsed successfully read-only. All three target components have m_Enabled=1, active GameObjects, and an active ancestor chain through EvacSystem GameObject `240475033` and Systems `1434462329`. None is a prefab instance or stripped component. Script references use fileID11500000/type3 and the GUIDs in §2. C component begins near scene line859, T near4614, V near5801.

| Script | GUID | Scene component / GameObject |
| --- | --- | --- |
| EvacuationController | `ef31d31705ac48f898e5746ce90d32fa` | `315251714` / `315251712` EvacuationController (activeSelf=1) |
| EvacuationRouteVisualizer | `37785d2c97374ef3abdc0d8dc1ba82b6` | `1117061671` / `1117061669` EvacRouteVisualizer (activeSelf=1) |
| EvacuationRouteTooltipController | `908e8eaeef6084fcf840ee5b5e6f5cad` | `817002036` / `817002034` EvacRouteTooltip (activeSelf=1) |
| MapHoverTooltipController | `42eb184f08424502a75b2bf59696fd7b` | No component |
| ShelterController | `889273345757b49ba982c2c5ca64aa4b` | `1420635165` / `1420635162` ShelterController (activeSelf=1) |
| ZoneController | `32a2bd6974cc48f781edac70b6557993` | `2070746514` / `2070746513` ZoneController (activeSelf=1) |
| ZoneBaselineRiskController | `c66ee8e505ab40bb8b2899bcd87b2b3e` | `1590100951` / `1590100949` ZoneBaselineRisk (activeSelf=1) |
| TileMapData | `194c942412658f943a92a424432fd5c3` | No component |
| GlobalHUDController | `6ece4d5d3dc514adc827a920d9c395f1` | `555384874` / `555384872` UIDocument (activeSelf=1) |

| Referenced object | Scene/external identity and relevant finding |
| --- | --- |
| Evacuation controller | Component315251714 on GO315251712, Transform315251713. Inspector inventory §3; not on visualizer GO. |
| Visualizer | Component1117061671 on GO1117061669 named EvacRouteVisualizer. Explicit C reference avoids runtime V creation. |
| Legacy tooltip | Component817002036 on GO817002034 named EvacRouteTooltip; different GO from UIDocument host despite same name. No incoming serialized call/reference to this component was found. |
| Route Tilemap | Component36957482 on GO36957479 named ZoneOutlineTilemap. Serialized origin(0,0,0), size(13,39,1), m_Tiles empty. Logical TileMapData can still supply passable cells; do not require filled route Tilemap. Used for both C and V. |
| Terrain / shelter target | Zone/baseline/shelter terrain1822120955; separate shelter target628953066. Zone TileOrigin derives from terrain/index, not route Tilemap. Loader1678446140 shared by Zone/shelter/baseline. |
| TileMapData asset | fileID11400000, GUID7e81a30567a6c1b44ae537c0db92678b → `Assets/Game/Features/LegacyWaterSystem/TileMapDataTest.asset`. TileMapData script GUID is separately194c942412658f943a92a424432fd5c3. Stored Inspector values: sizeX256, sizeY256, sizeZ10; rangeX(0,5), rangeY(0,5), rangeZ(0,1); N5; dx1, dy1, dt0.1; simInitialized1. These are asset serialization, not an observed runtime grid. TileMapData.OnEnable assigns N from rangeX.y; loader initialization can replace dimensions/data. Water and TileInstance contents remain runtime-dependent. |
| Line prefab | LineRenderer1963393216 on GO1963393215, ZoneLinePrefab; a local scene object, not an external prefab asset. V clones this GO. |
| Line material | fileID2100000/GUID5c951169c35ea49eab614112f3848aa0 → `Assets/Dev/Ash_dev/ash_prefabs/ZoneLineMaterial.mat`. |
| Route tooltip document | UIDocument2044171025 on GO2044171023 named EvacRouteTooltip. PanelSettings11400000/GUID3a9d075b934104e3da06f152a0c525a7; source9197481963319205126/GUID5dfdfc7f2edc14c218f183d3f63da4ff. Shared C/T explicit host. |
| Global HUD | GlobalHUDController555384874 on GO555384872 named UIDocument, distinct host from assigned route tooltip document; persistAcrossScenes1. Explicit document assignment wins over HUD discovery. |
| Main camera | Camera917516086 on GO917516084 named Main Camera, tag MainCamera, orthographic1. C and shelter share it. |
| Shared tooltip | No MapHoverTooltipController script binding anywhere in current scene/prefabs. C.mapHoverTooltip=0 and CommunicationController.mapHoverTooltip=0; runtime discovery/creation decides shared instance/host. |
| Other providers | Zone2070746514, Baseline1590100951, Shelter1420635165; all explicitly assigned. Relevant dependency edges listed below. |

Related provider Inspector references: Zone: map asset above, terrain1822120955, loader1678446140, outlineRoot704415075, linePrefab1963393216, material above. Baseline: same map/terrain/loader; Zone2070746514; Shelter1420635165; evacuation315251714; autoCalculateOnStart1. Shelter: BarrierController420333452; Zone2070746514; same map/terrain/loader; target628953066; camera917516086; Baseline1590100951; presentation434996198; placementInput748270053; keepPlacementModeActiveAfterPlacement0. These make zone membership/population/placement callbacks observable without any new field needed for E2. Full current records are captured in temporary scene-components.json.

The inbound consumer script identities are: FloodImpactController `f976661f3d2f4dbeaa227f63d307a845`; AdvisorPanelController `aa1334e4ffac427594386370898b6a7e`; ActionsPanelController `d2f5606ce6104486bd942076aa45a50a`; GameStateSummaryBuilder `b4f071b1bf5c4fbcb484a15e63fc3c40`; CommunicationPanelController `0f1e2d3c4b5a69788776655443322110`. Baseline's GUID is in §2. ResourceBarRuntimeController's GUID `b0659c20e5c348d5b8a55377e7f38133` has no serialized component binding and is added by its runtime bootstrap.

All six serialized `evacuationController` fields target315251714: FloodImpact35733723, Advisor515287497, Actions1389987227, Baseline1590100951, Summary2019949261, CommunicationPanel2100000004. No other scene/prefab contains C/V/T/Shelter/Zone/Baseline/HUD script GUID bindings. TileMapData script instances also exist in the standalone `Assets/Dev/WaterSystemRefactor/RefactorScene_TileMapData.asset`; its mere presence does not make it the evacuation map. All nonzero local references listed in this report resolve in the authorized scene. No target persistent UnityEvent binding was found. No route data, selected IDs or confirmation state is serialized into it.

The former `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity` and its meta are already deleted in the starting worktree. They were not read from history, restored, recreated or run. Existing unrelated modified/deleted/untracked assets were left alone. Scene/prefab searches and YAML parsing are read-only evidence, not Editor deserialization/import verification.

## 13. Proposed helper responsibilities

| Proposed type | Safe responsibility and constraints |
| --- | --- |
| EvacuationController | Retain current global public MonoBehaviour, GUID, scene component, serialized fields, lifecycle, discovery, public API/events and orchestration. Own event publication/re-entry timing; no replacement facade component. |
| EvacuationPathfinder | Plain class owning exact A*, neighbor order, tile snapshot/passability/cost rules, reconstruction and hop distance. Dependencies supplied explicitly per call/configuration; no scene discovery or retained traversal cache. Preserve live provider query timing. First recommended extraction. |
| EvacuationRouteGenerator | Later plain class enumerating supplied active shelters/associated zones, normalization, same-zone/short filtering, centroid/preferences and route record construction. Append to the existing owned available list incrementally; preserve failure vs successful-zero distinction. Baseline provider calls remain lazy/in-sequence. |
| EvacuationState | Later plain owner of available/visible lists, selected IDs, confirmed ID/flag, hover ID and derived hover lookup with one authority for each. Must preserve list identity and enumeration order. Facade still orchestrates phased transitions/events; do not collapse replacement intermediate state. |
| EvacuationMitigationCalculator | Plain calculation over ordered routes + selected IDs, producing/repopulating same ordinal caches and total. No UI, discovery, events or independent second selection store. Preserve first-capacity-per-shelter and maximum selected-zone population logic. |
| EvacuationInputController | Prefer a plain input sampler/hit-test coordinator called from existing C.Update, consistent with other extracted input helpers. Keep one Update driver, legacy Input access sequence and hover-before-click order. Avoid an additional MonoBehaviour/serialized scene reference. |
| EvacuationPresentation | Plain coordination of intact V/M and optional document bindings. Does not calculate routes/mitigation or become a second owner of selection/visibility. Preserve configure/clear/draw/tooltip ordering, cached redraws and host resolution. |

Adjustment to preliminary decomposition: make **all new helpers plain runtime classes**, retain Unity lifecycle/events/discovery in C, and explicitly include visible-list/hover-cell-index ownership in State rather than duplicating it between Input and Presentation. Keep route/path data mutable and shared. The generator may use a separate narrow query adapter if coordinate/snapshot logic is shared later; do not create an abstraction framework during initial extraction or have helpers call back into C. Provider controllers remain the interfaces to their own systems. Do not move/rename V/T/M or rewrite shared communication tooltip coordination as part of this initial work.

A possible remaining stage sequence is E2 pathfinding, E3 generation, E4 state/mitigation, E5 input/presentation; each needs its own explicit approved scope and matching baseline. This is a recommendation, not implementation or authorization of later-stage source edits.

## 14. Refactor risks and compatibility constraints

1. Preserve MonoBehaviour/script identity, global namespace, public signatures/aliases/events and all 44 serialized field names/types/defaults/attributes. No scene YAML migration is needed for plain helpers. Keep unused routeColor/waterTileCost and existing T component.
2. Preserve live list/route/path identity, in-place available clear/add and partial rebuild observability. Returning new route snapshots changes public semantics and callback visibility.
3. Preserve shelter/list/HashSet/open-list enumeration, floating-point expression order and first-match ID lookup. Sorting, a priority queue or “equivalent” tie handling changes paths, capacity allocation, hover precedence and possibly display IDs.
4. Preserve destructive failed rebuild with retained selected/confirmed/mitigation state versus successful zero-route cleanup. Missing provider/index/loader conditions do not all share one failure result.
5. Preserve first-selection confirmation before Selected; replacement D→X before new S→F?→X; no D for stale cleanup; hover X noise; no guarantee of subscriber execution order across components. Turn/advisor/summary callbacks make these gameplay-visible.
6. Preserve negative-origin conversions and separate route/terrain maps, no water grid padding, strict flood threshold and sparse-data fallback. Do not conflate hop distance with weighted path cost.
7. Preserve hidden cached redraw, V's one-ID selected-only behavior, and possible mismatch with C's multiple selected hit-test routes. A bug fix requires a separate approved behavior change.
8. Preserve M host discovery/configuration and separate legacy T label, including lifecycle cache-reset behavior. Missing Editor asset fallback and communication competition are recorded limitations, not E1 fixes.
9. Preserve shelter-only route invalidation, one-attempt Zone discovery, and late shelter subscription limitation. Adding water subscriptions/retries/startup generation changes cadence and failure recovery.
10. Baseline generation calls can synchronously publish baseline-risk events, and GetRemainingPopulationAfterEvacuation can initialize baseline despite general getter guidelines. Apply behavior-preservation priority rather than opportunistic cleanup.
11. Managed compilation uses existing external assembly references; it is not a fresh package build, IL postprocessing pass, native Editor import or platform build. Controlled harness stubs cannot prove scene loading, real camera projection, input scheduling, prefab scripts, shader availability or UI layout.

## 15. Recommended E2 implementation boundary

E2 should move only C's existing traversal/search implementation into EvacuationPathfinder: TryBuildPath, movement/snapshot/passability, TileInstance/water-grid reads, open-set scoring, heuristic, path reconstruction/output conversion, neighbor order, and necessary bounds/category helpers. Supply the existing map/zone/Tilemap and scalar settings explicitly; preserve current per-call reads and outputs. Keep any conversion/category function still needed by generation at a deliberate shared seam without changing call timing. Do not eagerly snapshot whole grids or replace provider lookups with normalized data caches.

Keep validation/discovery, zone-center/shelter enumeration, population lookup, ID construction, available-list mutation, selection/confirmation, mitigation, input, visualizer/tooltip configuration and all event Invoke sites in the facade. Leave V/T/M byte-identical. Retain every serialized Inspector setting on C and exact private behavior wrappers where useful to compare existing harness calls.

Before edits, obtain the later stage's explicit source scope because the facade resides under Assets/Game and the current project policy forbids writes there. Stage new plain helper source under an approved dedicated `Assets/Dev/Dev_EvacuationSystem` folder, not shared Dev/Scripts; E1 creates no such folder/helper. After extraction, compile the same runtime source set plus approved helper and compare frozen/current ordered traces, values and event sequence, then verify only PlayableTilemap in native Unity when available. Do not require scene rewiring or a new MonoBehaviour.

## 16. Baseline compilation and regression results

Evidence directory: `/private/tmp/evacuation-e1-21doq055` (temporary, outside Assets; may be removed by the OS). Contains initial-status.txt, initial-hashes.json, script-identities.json, source-dependencies.json, serialized-dependencies.json, scene-records.json, scene-components.json, YAML parse log, compiler response files/logs, frozen controller/visualizer sources, controlled stubs/tests, regression output and replay.py. It is not a permanent project test suite or an extracted gameplay helper implementation.

| Check | Fresh result / limit |
| --- | --- |
| Runtime assembly compile | Unity6000.0.63f1 bundled Roslyn/NetCoreRuntime, current207 Assembly-CSharp sources, current Unity/package references and recorded Unity options: **0 errors, 11 warnings**. Compiles all seven complete consumer files and all three target scripts. No Unity executable launched. |
| Diagnostics | Same warning multiset as recorded completed Communication C5: CS0105×1 duplicate using, CS0618×8 obsolete API uses, CS0414×2 assigned-unused fields. A first diagnostic pass omitted slash-prefixed Unity response-file options and reported160 warnings; rerunning with the complete original options restored11. No source was changed or extra suppression invented. |
| Managed behavioral harness | **79 assertions passed** against actual current C/V, with engine/map/zone/shelter/baseline/tooltip stand-ins. Harness compile:0 errors,2 expected CS0649 warnings for visualizer Inspector references not assigned in source. |
| Captured traces | **14** labeled traces for negative-origin generation; equal-score path; replacement events; ordered allocation; hidden clear; confirmation; stale cleanup; failed rebuild; anchors; pool count; input; multiple shelters; weighted detour; shader fallback. Frozen/current replay matches byte-for-byte, with no output normalization. |
| Route generation | First-occurrence GEOID filtering, unknown zone omission, safe metadata/population lookup, distance/minimum equality, same-zone skip, shelter-major/zone-minor order, negative origins. |
| A* | Equal-score +X tie path, same-cell success, bounds and iteration cap, exact flood threshold vs grid-depth block, category/type water block, absent-instance/Tilemap fallback, absent-both rejection, data-only traversal, IndexOutOfRange fallback, weighted road detour, impassable flood wall. |
| Selection/mitigation/events | New/repeated selection, max1 replacement and intermediate events, max2 rejection, unlimited selection, greedy shared shelter capacity, multiple shelters without double counting zone demand, confirmation lock, explicit clear, successful stale cleanup without Deselected, failed rebuild retaining old mitigation/selected IDs. |
| Input/presentation | Actual C.Update on controlled Input/Camera/Tilemap: hover-before-click events, overlap first/selected precedence, repeated-hover no event, cursor tooltip re-show, miss/hide; selected-only lookup; actual V pool reuse, four-point borders, widths, clear/cache redraw, multi-selected mismatch, ordered shader fallback. |
| Tooltip integration | C→M show/hide/re-show calls and flags tested through a controlled M seam. Actual M and T source/API/scene/UXML inspected and full-assembly compiled; their native UI construction/layout and T lifecycle were **not executed**. |
| Serialization |342 YAML documents parsed read-only; reported local refs resolve; target GUID/class/component bindings inspected across55 scenes/159 prefabs; no scene/prefab writes or persistent target callbacks. |
| Native Unity | **Not performed:** import, Play Mode, real input/camera/UI rendering/shader/prefab behavior, build. No result from past records is represented as current native testing. |
| Scope | Before/after SHA-256 comparison of6298 initial Assets/Packages/ProjectSettings/UserSettings files: all unchanged. Only this requested report and normal metadata added; all pre-existing dirty/deleted files preserved. |

Audited source fingerprints (working tree, including pre-existing changes):

| Audited target source | SHA-256 |
| --- | --- |
| EvacuationController.cs | `59912e17cbd631fe1087a5f83811eb519cee84ca5b622dfb97bcd0a8464f129a` |
| EvacuationRouteVisualizer.cs | `047fefca99b2a6d458ea1c6f1019bf2ceae4417473ae36fb3020a15b0f067406` |
| EvacuationRouteTooltipController.cs | `3f12885fa29fc0279a0f348acb32a507c783cd995c9e25b52191e7899eccafde` |

Reproduce frozen/current managed baseline from repository root:

```sh
python3 /private/tmp/evacuation-e1-21doq055/replay.py
```

replay.py rebuilds the current runtime assembly, compiles the frozen and current C/V harnesses, runs both with Unity's bundled Mono, and checks79 assertions and14 traces/output identity. It writes only evidence-directory outputs. For later extraction, explicitly add the new helper to the current-side runtime and harness compiler inputs before comparing and update the expected runtime source count; the frozen side stays unchanged. Harness providers return controlled data; no actual JsonMapLoader startup or full shelter/zone/baseline systems are simulated. Scene values are documented independently rather than claimed as native harness execution.

Manual native follow-up, **PlayableTilemap only**, without saving test mutations: verify import/compile and the three component/document identities; show preview with zero/one/two placed shelters and compare ordered IDs/paths; hover route overlap and move off-route; select first route and confirm lock/advisor/turn behavior; hide/show preview preserving selection; enter/exit shelter placement and verify no automatic resume; rebuild after shelter changes; compare summary population/capacity/ratios and route/tooltip styling. Use disposable runtime fixtures for alternate caps, negative origins, flood barriers and missing dependencies; do not edit scene YAML or restore a development scene to test them.

## 17. Unresolved questions or blockers

No missing source or API dependency blocks planning the narrow E2 extraction. E1 is complete as a read-only audit with the verification limits above. The following remain explicit follow-ups, not implied approvals to change behavior:

- Native Unity tooltip-host competition, lifecycle order, actual input/camera projection, LineRenderer prefab/material output and full loader timing remain unverified in this session. Only PlayableTilemap is authorized for that follow-up.
- Existing count/visibility, hidden cached redraw, stale failure state and missing Dev tooltip fallback are behavior decisions for separate tasks if fixes are desired. E2 should preserve them.
- Future facade changes need explicit Assets/Game authorization. E1 authorizes only this report/meta; no helper or new assembly was implemented.
- Temporary evidence is reproducible while retained, but not durable CI. A permanent test suite would require its own approved files/scope.

Review this report and commit the E1 report/meta as a documentation checkpoint while preserving unrelated working-tree changes.

### Current dependency search index

This is an exhaustive **textual source index for the ten requested names** in existing Assets C# files, excluding each type's own defining file below. It includes documentation mentions as well as actual typed dependencies; §5 identifies the actual direct controller callers and event subscribers. Editor paths are tooling only. Remaining entries reference shared providers/tooltip data, not necessarily EvacuationController. No evidence here alone asserts that a compiled component is instantiated.

| Exact source path | Requested names and line references |
| --- | --- |
| `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierController.cs` | ZoneController (21,121,474,541,544,548…); TileMapData (17,117); GlobalHUDController (676,679) |
| `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierPhysicsAdapter.cs` | ZoneController (67,133,164) |
| `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierPlacementInput.cs` | ZoneController (24) |
| `Assets/Dev/Dev_BarrierSystem/Scripts/BarrierSystem/BarrierVisuals.cs` | ZoneController (41,67) |
| `Assets/Dev/Dev_CommunicationSystem/CommunicationController.cs` | MapHoverTooltipController (58,91,327,337,339,530); ShelterController (56,317); ZoneController (4,5,54,55,358,364…); TileMapData (3,52,400,544,546,550) |
| `Assets/Dev/Dev_CommunicationSystem/CommunicationPanelController.cs` | EvacuationController (54,835); ShelterController (53,832); ZoneBaselineRiskController (55,838) |
| `Assets/Dev/Dev_CommunicationSystem/CommunicationPresentation.cs` | ZoneController (36,43,180); GlobalHUDController (125,126) |
| `Assets/Dev/Dev_CommunicationSystem/CommunicationTowerGenerator.cs` | ShelterController (12,21); ZoneController (11,20); TileMapData (9,18) |
| `Assets/Dev/Dev_Scripts/Editor/FloodImpactValidationWindow.cs` | ZoneController (17,87,90,290,479,594); TileMapData (18,92,95,462,479,598) |
| `Assets/Dev/Dev_Shelter/Scripts/ShelterCandidateResolver.cs` | ZoneController (191,541,752,760,776,779); ZoneBaselineRiskController (746,747); TileMapData (58,135,142,144,146,603…) |
| `Assets/Dev/Dev_Shelter/Scripts/ShelterController.cs` | ZoneController (14,670,697,738); ZoneBaselineRiskController (20,675,704); TileMapData (16,672,750) |
| `Assets/Dev/Dev_Shelter/Scripts/ShelterPlacementInput.cs` | ShelterController (11,51) |
| `Assets/Dev/Dev_Shelter/Scripts/ShelterPresentation.cs` | ZoneController (14,15,93,98,106,114…) |
| `Assets/Dev/Dev_Shelter/Scripts/ShelterTypes.cs` | ZoneController (95,113,119,134); ZoneBaselineRiskController (100); TileMapData (97,119,131) |
| `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneController.cs` | TileMapData (9,76) |
| `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneHoverInputController.cs` | ZoneController (8,32) |
| `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneIndex.cs` | TileMapData (49,63) |
| `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneOutlineVisibilityController.cs` | ZoneController (7,15,100) |
| `Assets/Dev/Scripts/Flood/FloodDamageCalculator.cs` | TileMapData (45,100) |
| `Assets/Dev/Scripts/Flood/FloodImpactController.cs` | EvacuationController (43,90,222); ShelterController (42,87,112); ZoneController (40,81,420); ZoneBaselineRiskController (41,84); TileMapData (44,94,426) |
| `Assets/Dev/Scripts/Flood/FloodImpactOverlayManager.cs` | ZoneController (13,14,69,72,78,86…) |
| `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs` | EvacuationController (12,143,572); ShelterController (9,134,569); ZoneBaselineRiskController (10,137,570); SimpleEvacuationRoute (422,423,435,627,632,681…) |
| `Assets/Dev/Scripts/Main_UI/UI_Controller_Scripts/InventoryToolsController.cs` | ShelterController (15,203,236,238,243,247) |
| `Assets/Dev/Scripts/Main_UI/UI_Flow_Controllers/ScenarioIntroController.cs` | ZoneBaselineRiskController (23,154) |
| `Assets/Dev/Scripts/Main_UI/UI_Overlay/Dev_ResourceBarRuntime/Scripts/ResourceBarRuntimeController.cs` | EvacuationController (22,292,618); ShelterController (20,76,305,315,318,641…); TileMapData (18,761,763); SimpleEvacuationRoute (692) |
| `Assets/Dev/Scripts/MapLoader/JsonMapLoader.cs` | TileMapData (25,185) |
| `Assets/Dev/Scripts/PanelController/ActionsPanelController.cs` | EvacuationController (20,157,163,249); ShelterController (19,144,246) |
| `Assets/Dev/Scripts/PanelController/AlertsController.cs` | ZoneBaselineRiskController (14,35) |
| `Assets/Dev/Scripts/Zones/HighRiskManager.cs` | ZoneController (16,17,74,77,83,91…); ZoneBaselineRiskController (15,54,490) |
| `Assets/Dev/Scripts/Zones/HoverTileTooltip.cs` | TileMapData (10); GlobalHUDController (109,194,199) |
| `Assets/Dev/Scripts/Zones/MapHoverTooltipController.cs` | GlobalHUDController (357,358); SimpleEvacuationRoute (125) |
| `Assets/Dev/Scripts/Zones/ZoneBaselineRiskController.cs` | EvacuationController (70,134,325,433); ShelterController (69,131,171); ZoneController (67,68,143,149,157,165…); TileMapData (65) |
| `Assets/Dev/WaterSystemRefactor/Editor/WaterLegacyMapConverter.cs` | TileMapData (11,15,18,23,30,46…) |
| `Assets/Game/Features/LegacyWaterSystem/MapLoader.cs` | TileMapData (20) |
| `Assets/Game/Features/LegacyWaterSystem/TileManager.cs` | TileMapData (10,19) |
| `Assets/Game/Features/LegacyWaterSystem/WaterSimulator.cs` | TileMapData (20,87,115) |
| `Assets/Game/Features/LegacyWaterSystem/WaterWaveStripeAnimator.cs` | TileMapData (8) |
| `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationController.cs` | EvacuationRouteVisualizer (13,23,75,921,924,928…); MapHoverTooltipController (24,76,934,941,943); ShelterController (71,909,1067,1110); ZoneController (72,914,1116); ZoneBaselineRiskController (70,906); TileMapData (73,1122,1754,1756,1760); GlobalHUDController (976,977) |
| `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationRouteTooltipController.cs` | GlobalHUDController (7,150,151) |
| `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationRouteVisualizer.cs` | EvacuationController (23); SimpleEvacuationRoute (54,106,112,146,197,217…) |
| `Assets/Game/Features/UI/Scripts/UI_Advisor_Scripts/AdvisorMainController.cs` | ZoneBaselineRiskController (18,62,247) |
| `Assets/Game/Features/UI/Scripts/UI_Advisor_Scripts/AdvisorPanelController.cs` | EvacuationController (23,246); ShelterController (22,243); ZoneBaselineRiskController (17,228,605); SimpleEvacuationRoute (386,400) |
| `Assets/Tools/Editor/TileCellInspector/TileCellInspector.cs` | TileMapData (11,56) |
| `Assets/Tools/Editor/TileCellInspector/TileMapDataEditor.cs` | TileMapData (4,11) |
