# CommunicationSystem — Stage C1: Architecture and Dependency Audit

Date: 2026-10-23. Scope: documentation only. No production code, scenes, prefabs, metadata, UI assets, map data or settings were changed. No MonoBehaviours were added or removed. Unity Editor import and Unity Play Mode were **not** executed; Dev_UINavigation was inspected only as text. Only PlayableTilemap is authorized for future Play Mode checks.

Recommendation: extract plain C# helpers behind the existing authoritative component first. Once behavior is verified and production integration is separately authorized, rename that same script asset to `CommunicationController`, preserving its `.meta` GUID, scene component identities, serialized field names and public method/event contracts. Do not install a second controller alongside it.

## 1. Current architecture and evidence

### Repository snapshot and search coverage

The audit reads the current working tree at HEAD `b905838841efca9ca4b22b82c0cf6d4d1bb38583`, including existing local edits. At entry, these files were already modified and were left untouched:

- `Assets/Dev/Ash_dev/Pixel_Font/PublicPixel SDF.asset`
- `Assets/Game/Content/Scenes/PlayableTilemap.unity`
- `UserSettings/Layouts/CurrentMaximizeLayout.dwlt`
- `UserSettings/Layouts/default-6000.dwlt`

Read `AGENTS.md`, the AgentRef guidelines/UI reference, Assets README, core architecture, root CONTEXT and ADR routing. No applicable ADR exists. Current project version is Unity `6000.0.63f1`. Findings below come from current sources and serialized assets; older AgentRef reports are historical context, not proof of present behavior or test execution.

Searches covered all 235 C# files physically present under Assets/Packages, plus a repository-wide hidden-file-aware identifier search excluding generated Library/Temp/Logs/obj/Build and Git internals. Serialized inspection covered 904 text assets: scenes, prefabs, assets, UXML, USS, animation and animator assets; this includes all 56 scenes and 159 prefabs under Assets. GUID searches and persistent-call inspection included 254 scene blocks, 665 prefab blocks and one asset block containing `m_PersistentCalls`. These are containing-block counts, not listener counts. No native object resolution or binary-asset inspection is claimed.

For source citations, the following aliases resolve to exact paths; `C:311` means that file's line 311 in this snapshot.

| Alias | Exact source path |
| --- | --- |
| C | `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs` |
| P | `Assets/Game/Features/MitigationFeatures/Communication/CommunicationPanelController.cs` |
| T | `Assets/Dev/Scripts/Zones/MapHoverTooltipController.cs` |
| A | `Assets/Dev/Scripts/PanelController/ActionsPanelController.cs` |
| G | `Assets/Dev/Scripts/GameStateSummary/GameStateSummaryBuilder.cs` |
| R | `Assets/Dev/Scripts/Main_UI/UI_Overlay/Dev_ResourceBarRuntime/Scripts/ResourceBarRuntimeController.cs` |
| D | `Assets/Game/Features/UI/Scripts/UI_Advisor_Scripts/AdvisorPanelController.cs` |
| Z | `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneController.cs` |
| S | `Assets/Dev/Dev_Shelter/Scripts/ShelterController.cs` |
| I | `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneIndex.cs` |
| J | `Assets/Dev/Scripts/MapLoader/JsonMapLoader.cs` |
| M | `Assets/Game/Features/LegacyWaterSystem/ScriptableObjects/TileMapData.cs` |
| E | `Assets/Game/Features/MitigationFeatures/Evacuation/EvacuationController.cs` |

### What exists today

`CommunicationTowerController` is a 1,221-line MonoBehaviour owning generation, communication state, rendering, pointer input and presentation integration. `ZoneCommunicationStatus` and the serializable `CommunicationTowerData` are declared in that same source file (`C:32–47`). None of the proposed seven-component architecture is implemented under the proposed names. The runtime child named `CommunicationTowerVisuals` is a GameObject/root, not an existing helper component.

Authoritative runtime data is `_communicationTowers`, `_towerByCell`, `_zoneStatusByGeoid`, plus each tower's mutable `isActive` field. Visual dictionaries store GameObjects/renderers; they are not an independent gameplay state. Mode/generated flags, hovered tower, retry coroutine and diagnostic flags also reside on C (`C:102–121`). There is no communication singleton and no communication subscription to map, shelter, zone or warning events.

Current flow:

1. `J.Awake → PaintFromJson` synchronously paints the map and populates loader mappings and TileMapData. C's `Awake` resolves general references, then resolves its ZoneController once. C's `Start` begins bounded readiness polling.
2. `C.GenerateCommunicationTowers → Z.GetAllZoneTiles` lazily builds/reuses **ZoneIndex owned by Z**. C groups zones, finds cells, records inactive towers and spawns visuals. This is not a new communication-owned map cache.
3. `action_Button6.clicked → A.OnCommunicationClicked → A.ToggleCommunicationMode → C.ToggleCommunicationMode → C.SetCommunicationMode`. Mode changes update visuals and notify Actions/summary observers.
4. In communication mode, C's `Update` reads pointer hover, updates one communication outline channel and tooltip, then handles the left click.
5. First activation sets tower active, changes associated Unreached statuses to Pending, emits `OnTowerActivated` synchronously, then opens P. Repeated selection only opens P again.
6. P selects Pending zones and calls `C.MarkZoneWarned` once per selected GEOID. C stores Warned before synchronously invoking `OnZoneWarned`. Summary, advisor and resource-bar observers react; no network dispatch, warning queue or warning-card resolver exists here.

The header in C still mentions removed `ZoneThinOutlineByHover`, ShelterManager and ShelterCandidateController setup. Those are stale comments. The actual executable dependencies are Z and S; there is no serialized legacy outline/stamp/shelter field in C.

## 2. Complete dependency inventory

### Direct runtime consumers

There are **six direct external C# consumers**, including two scripts in Game and four in Dev. Dev location does not make a runtime consumer test-only.

| Consumer | Current type/discovery and operations | Events and consequence |
| --- | --- | --- |
| P | Serialized C field `P:52`; `FindFirstObjectByType<C>` at 829. Holds `CommunicationTowerData` at 92; `ShowForTower` at 134; reads associated GEOIDs/ID at 441, 922, 927. `TryGetCommunicationStatus` at 537, 641; `MarkZoneWarned` at 716. | No communication event subscriptions. Refresh is explicit through panel opening/own actions. |
| T | Serialized C field `T:47`; public `SetCommunicationTowerController(C)` at 115; discovery at 336, 521. `ShowCommunicationTowerTooltip(CommunicationTowerData, Vector2)` at 169; status query at 528; tower fields at 180–196, 499–567. | No communication event subscriptions; tooltip data refreshes when shown each hover frame. Shared with evacuation UI. |
| A | Serialized C field `A:21`; discovery at 183, 252; runtime creation at 261–269 through `new GameObject` and `AddComponent<C>()` at 264. Toggle at 197, diagnostic notification at 365, mode property at 575. | `CommunicationModeChanged +=` 443 / `-=` 498; updates button active styling. Subscribes in OnEnable and unbinds/unsubscribes in OnDisable. Can retry discovery/creation from its public toggle. |
| G | Serialized C field `G:13`; discovery at 146. `GetAllTowers` 471, `GetAllZoneCommunicationStatuses` 498, `GetZoneCommunicationStatus` 514. Reads tower ID/activity/association; creates separate summary DTOs at 861–887. | Mode/activation/warning subscribe at 187–189, unsubscribe at 241–243. Handlers 637–650 refresh summary JSON. Enabled-lifetime subscription, not a second authority. |
| R | Serialized C field `R:21`; discovery at 289. Tower event payload parameter at 680. | Activation/warning subscribe at 604–605, unsubscribe at 628–629. Handlers 680–689 optionally register mitigation actions and advance turns. R bootstraps itself onto a suitable UIDocument with `AddComponent<R>` at 81–98, so absence from scene YAML does not remove this dependency. |
| D | Serialized C field `D:24`; discovery at 249. Uses activation payload's ID and associated-zone count at 413–425. | Activation/warning subscribe at 286–287, unsubscribe at 327–328. Handlers 413, 427 produce advisor messages. Enabled-lifetime subscription. |

All six serialized field names are currently `communicationTowerController`. Keep the field names during type migration. There are no additional reflection/string-based creation sites for C in the searched working-tree text, and no `GetComponent<C>` call outside the audited discovery/creation paths.

G's communication summary types contain tower/status snapshots and do not reference C's type. They remain a downstream output format. `GlobalHUDController`'s population-to-resource-bar calls do not read communication state directly. No other gameplay controller directly queries communication state in the current source set.

### Requested identifier cross-check

This table excludes declarations/internal calls in C and repeats externally relevant call sites to make negative findings explicit.

| Identifier | External executable references |
| --- | --- |
| `CommunicationTowerController` | P, T, A, G, R, D: serialized types, generic discovery, A runtime creation; T setter signature. Scene script binding in the two scenes listed below. |
| `CommunicationTowerData` | P:92,134,922,927; T:169,499,554; G:471,478,642; R:680; D:413. |
| `ZoneCommunicationStatus` | P status display/selection code 506–643; T summary 528–539; G summary 498–522. |
| `ToggleCommunicationMode` | A:179 public wrapper, 197 call, 430 button handler. |
| `SetCommunicationMode` | None. |
| `ShowCommunicationTowers` | None. |
| `HideCommunicationTowers` | None. |
| `RegenerateCommunicationTowers` | None; C exposes it through a ContextMenu. |
| `TryGetCommunicationStatus` | P:537,641; T:528. |
| `GetZoneCommunicationStatus` | G:514. |
| `GetAllZoneCommunicationStatuses` | G:498. |
| `MarkZoneWarned` | P:716. |
| `OnTowerActivated` | G:188,242,642; R:604,628,680; D:286,327,413. |
| `OnZoneWarned` | G:189,243,647; R:605,629,686; D:287,328,427. |
| `CommunicationModeChanged` | A:443,498; G:187,241 (handler at 637). |
| `NotifyActionButtonConnected` | A:365. |

Other public surfaces: `GetAllTowers` is consumed only by G; `IsCommunicationModeActive` by A. No external usage of `GetActiveTowers` or `CommunicationTowers` was found. Unused public APIs must still be retained.

### Providers, shared UI and serialized bindings

| Dependency from C | Existing contract and audit implication |
| --- | --- |
| TileMapData / TileInstance | Placement/category read at `C:784–801`, map dimensions at 1110–1127; missing asset recovery at 1130. No WaterController/WaterState dependency and no live-water-depth test. |
| Z | Zone dictionary 330; normalized-to-absolute conversion 365,770; terrain fallback 404; dedicated outlines 1063,1081. Discovery is local/unique-active-same-scene at Awake, not repeated during generation. |
| S / PlacedShelterData | `TryGetShelterAtTile` and `IsValidShelterCandidateTile` at `C:846–857`. No communication subscriptions or automatic tower regeneration on shelter changes. |
| Camera / communication Tilemap | Screen→world→cell hit detection; explicit communication Tilemap is separate from Z's terrain assignment. |
| T | C searches or creates T (`C:409–423`); configures document/controller/logging at 466–476; calls show/hide at 1099,1107. E independently searches/creates the **same tooltip type** at `E:933–943` and configures its document at 961–970. E is an indirect integration dependency, not a direct C/state consumer. |
| P | C searches at 415 and again when opening at 1036; calls `ShowForTower` at 1045. No automatic creation of P. |
| GlobalHUDController / UIDocument | Tooltip-host fallback only (`C:479–515`); no new singleton is needed for extraction. |
| SpriteRenderer / Transform / Unity Object | Visual allocation, parenting, delayed destruction, sprite/color/sort selection. These can be called from a plain helper on the Unity main thread. |

**Serialized findings:** C's script GUID occurs in exactly two scene script bindings and no prefab/other asset binding. Each scene has four external `communicationTowerController` references plus C's own GameObject component-list entry. No communication target, communication method name or communication target-type name occurs in persistent UnityEvents in the inspected text assets. No communication API type/method binding occurs in UXML/USS. Action wiring is a C# UI Toolkit `clicked` subscription, not a persistent UnityEvent. Preserve the negative baseline by re-running the same searches immediately before migration.

### Historical and test-only references

Seventeen existing AgentRef Markdown reports mention C or its data/status types: `2026-08-12-communication-tower-controller`; `2026-08-12-map-hover-tooltips`; `ZoneSystem_Stage4C_CommunicationTowerController_Migration`; `ZoneSystem_Audit_A_Outline_Dependencies`; `ZoneSystem_Stage5A_Legacy_Dependency_Audit`; `ZoneSystem_Stage5C_LegacyConsumerFieldCleanup`; `ZoneSystem_Stage5E2_VisibilityPolicyMigration`; `ZoneSystem_Stage5FG_LegacyOutlineRetirement`; `ZoneSystem_Stage5H23_BaselineCommunicationMigration`; `ZoneSystem_Stage5H45_EvacuationShelterMigration`; `ZoneSystem_Stage5I12_BarrierControlsMigration`; `ZoneSystem_Stage5I34_BarrierEventsAndRetirementAudit`; `ZoneSystem_Stage5J12_FinalDependencies`; `ZoneSystem_Stage5J34_FinalStampRetirement`; `ZoneSystem_Stage5J4_FinalLegacyRetirement`; `ShelterSystem_Legacy_Dependency_Audit`; `ShelterSystem_StageS4_LegacyScriptRemoval`. All reside in `Assets/Dev/AgentRef/Records/` with `.md` extensions. They are documentation, not active consumers.

No current test C# file references the requested communication identifiers. The older 5H23 report describes 53 communication comparison states and a temporary harness outside Assets; that is an earlier result, not a C1 execution or a checked-in reusable test suite. Its stamp-retention description is superseded by current C, which no longer has a stamp field. No claim is made that old temporary harness files remain available.

## 3. Serialized component identities and Inspector contract

Use these exact scene paths:

- **Playable:** `Assets/Game/Content/Scenes/PlayableTilemap.unity`
- **Navigation:** `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity`

FileIDs are local to a scene even when their numbers match across scenes. All listed custom-script `m_Script` references use script fileID `11500000`, type `3`. Scene C bindings use GUID `98e604977a2f4ef5ac4190852d01e7fa`. Both are direct scene components (`m_PrefabInstance`/`m_PrefabAsset` zero), enabled, on GameObject `810699003`, named `CommunicationTower`. The component is `810699005`; component headers are Playable line 4513 / Navigation line 4514.

### Script GUID and consumer binding manifest

| Script (path alias from §1) | Script GUID | Component fileID in Playable / Navigation | GameObject fileID; name |
| --- | --- | --- | --- |
| C | `98e604977a2f4ef5ac4190852d01e7fa` | `810699005` / `810699005` | `810699003`; CommunicationTower |
| P | `0f1e2d3c4b5a69788776655443322110` | `2100000004` / `2100000004` | `2100000001`; CommunicationPanel |
| T | `42eb184f08424502a75b2bf59696fd7b` | No saved component / no saved component | Resolved/created at runtime; no fixed runtime fileID available. |
| A | `d2f5606ce6104486bd942076aa45a50a` | `1389987227` / `1389987227` | `1389987225`; ActionsController |
| G | `b4f071b1bf5c4fbcb484a15e63fc3c40` | `2019949261` / `2019949261` | `2019949259`; GameStateSummary |
| R | `b0659c20e5c348d5b8a55377e7f38133` | No saved component / no saved component | Runtime bootstrap; no fixed runtime fileID available. |
| D | `aa1334e4ffac427594386370898b6a7e` | `515287497` / `515287497` | `515287495`; AdvisorPanel |
| Z | `32a2bd6974cc48f781edac70b6557993` | `2070746514` / `2070746514` | `2070746513`; ZoneController |
| S | `889273345757b49ba982c2c5ca64aa4b` | `1420635165` / `1420635165` | `1420635162`; ShelterController |

For both scenes, P/A/G/D's field `communicationTowerController` points to **810699005**. Reference lines are Playable 80681/26312/80293/1417 and Navigation 80889/26358/80437/1418, respectively. There are no saved T/R script bindings elsewhere in the inspected scenes/prefabs/assets either; runtime behavior must still be tested.

Supporting provider identities (both scenes have the same local IDs):

| Script | Script GUID | Component fileID in each scene |
| --- | --- | --- |
| E (exact path in §1) | `ef31d31705ac48f898e5746ce90d32fa` | `315251714` |
| J (exact path in §1) | `ccb87baceffa24218947b8368bac3e99` | `1678446140` |
| `Assets/Dev/Scripts/Zones/GlobalHUDController.cs` | `6ece4d5d3dc514adc827a920d9c395f1` | `555384874` |
| M (exact path in §1) | `194c942412658f943a92a424432fd5c3` | ScriptableObject type; assigned map asset identity below, no scene MonoBehaviour. |
| `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneOutlineVisibilityController.cs` | `572166c511a9404c9cbaf2c27f858ce2` | `704415078` |
| `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneHoverInputController.cs` | `64f9a390e9f248e7b4d7ad09bb6a968d` | `704415077` |

`Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/ZoneOutlineRenderer.cs` has metadata GUID `72e1e060271494970ae2fc4f63bbe816`, but is a plain class owned by Z, with no saved MonoBehaviour component. These supporting systems do not directly query communication state.

### All 24 current C Inspector fields

Assignments are identical in both saved scenes. Values below are the serialized baseline, not assumed defaults. Field declarations are `C:69–100`.

| Field | Saved assignment in each scene |
| --- | --- |
| `tileMapData` | `{fileID: 11400000, guid: 7e81a30567a6c1b44ae537c0db92678b, type: 2}` → `Assets/Game/Features/LegacyWaterSystem/TileMapDataTest.asset` |
| `communicationTilemap` | `{fileID: 36957482}` → Tilemap on GameObject `36957479`, ZoneOutlineTilemap |
| `zoneController` | `{fileID: 2070746514}` |
| `shelterController` | `{fileID: 1420635165}` |
| `mainCamera` | `{fileID: 917516086}` → Camera on GameObject `917516084`, Main Camera |
| `mapHoverTooltip` | `{fileID: 0}`; runtime discovery/creation required |
| `tooltipUIDocument` | `{fileID: 2082171433}` → UIDocument on GameObject `2082171431`, UIDocument |
| `communicationPanelController` | `{fileID: 2100000004}` |
| `mainTowerSprite` | `{fileID: 21300000, guid: 7e25c158da0c7974cbf783bc599cbba3, type: 3}` → `Assets/Dev/FIX_Models/tempIsoFiles/icp_u423/Buildings/water_tower_a.png` |
| `zoneTowerSprite` | `{fileID: 21300000, guid: 984ce4b18bfef434881def49d974e4c6, type: 3}` → `Assets/Dev/FIX_Models/tempIsoFiles/icp_u423/Buildings/network_tower.png` |
| `towerVisualRoot` | `{fileID: 1688803249}` → Transform on GameObject `1688803248`, CommunicationTowerVisualRoot |
| `inactiveTowerColor` | `{r: 0.62, g: 0.86, b: 1, a: 0.85}` |
| `activeTowerColor` | `{r: 0.25, g: 1, b: 0.55, a: 1}` |
| `hoveredTowerColor` | `{r: 1, g: 0.95, b: 0.35, a: 1}` |
| `associatedZoneHoverColor` | `{r: 0.2, g: 0.85, b: 1, a: 1}` |
| `towerVisualScale` | `0.45` |
| `towerHeightOffset` | `0.35` |
| `towerSortingLayerName` | `Default` |
| `towerSortingOrder` | `2300` |
| `zonesPerTower` | `3` |
| `towerSearchRadius` | `4` |
| `showInactiveTowersAtStart` | `1` (true) |
| `avoidShelterTiles` | `1` (true) |
| `debugLogs` | `1` (true) |

Keep these fields on the MonoBehaviour during initial extraction, even when logic moves to helpers. Inject their values/references into helpers; moving them into nested helper fields would change serialization paths. No `FormerlySerializedAs` is required if names stay unchanged. That attribute would address a field-name change, not a class/script identity change.

Additional saved contracts:

- Z uses terrain Tilemap **1822120955**, loader **1678446140**, outline Transform **704415075**; its terrain is **not** the saved communication Tilemap 36957482. Preserve both assignments independently.
- Loader **1678446140**, script GUID `ccb87baceffa24218947b8368bac3e99`, uses the same TileMapData asset, `fileName: tilemap_with_population_elev.json`, and null `jsonOverride`. Default runtime input is `Assets/StreamingAssets/tilemap_with_population_elev.json`.
- Tooltip UIDocument **2082171433** already uses `CommunicationTowerTooltip.uxml`, source fileID `9197481963319205126`, GUID `a47e3a5a0eb4e4062adf4cc40c2c40ea`. Its root is reused; the stale Editor path fallback is not necessary for this saved host.
- P's document field points to **2100000003**, on CommunicationPanel GameObject **2100000001**. Both that document's sourceAsset and P's template use fileID `9197481963319205126`, GUID `7a7554768217246c7a4b79f427772e7e`, `Assets/Game/Features/UI/UI_Documents/UI_Panels_Tooltips/CommunicationPanel.uxml`.
- Both UI documents use PanelSettings GUID `3a9d075b934104e3da06f152a0c525a7`, fileID `11400000`, type `2`.
- Panel/tooltip USS assets are in the same `UI_Panels_Tooltips` directory. GUIDs are `9b59be7f8e30e4826b12cdce48ebb004` / `bc9d5364c3aaf43d8848a5d5e5c22bdf`. Their UXML style links retain older Dev path text alongside these GUIDs. Preserve; native style resolution was not tested.

## 4. Public API inventory and UI compatibility

All existing C public signatures must remain available on the final facade, including those without current callers. Do not rename them to enforce newer naming conventions during extraction.

| API | Current behavior / compatibility requirement |
| --- | --- |
| `bool IsCommunicationModeActive { get; }` (`C:116`) | Reads mode without generating anything. |
| `IReadOnlyList<CommunicationTowerData> CommunicationTowers { get; }` (117) | Returns live backing list through read-only interface; elements are mutable. |
| `event Action<bool> CommunicationModeChanged` (119) | Raised on every Set call, including repeated identical values and failed generation. |
| `event Action<CommunicationTowerData> OnTowerActivated` (120) | Once on first activation; state/visual update precedes event, panel open follows it. |
| `event Action<string> OnZoneWarned` (121) | Once per transition into Warned, after state write; independent of tower activation. |
| `void ToggleCommunicationMode()` (161) | Inverts current mode through setter. |
| `void SetCommunicationMode(bool active)` (166) | Generation/show before mode=true; mode=false clears hover, applies startup/active visibility; emits mode event last. |
| `void ShowCommunicationTowers()` (190) | Attempts generation then shows all; does not change mode/emit mode event. |
| `void HideCommunicationTowers()` (196) | Clears hover and hides only inactive towers; leaves active towers visible and mode unchanged. |
| `bool TryGetCommunicationStatus(string geoid, out ZoneCommunicationStatus status)` (202) | Trims GEOID. Blank/unknown false, default Unreached; no lazy generation. |
| `IReadOnlyList<CommunicationTowerData> GetAllTowers()` (215) | Same live backing list as property; no generation. |
| `IReadOnlyList<CommunicationTowerData> GetActiveTowers()` (220) | Allocates a new filtered list containing the same tower objects. |
| `ZoneCommunicationStatus GetZoneCommunicationStatus(string geoid)` (235) | Unknown/blank returns Unreached; no mutation. |
| `Dictionary<string, ZoneCommunicationStatus> GetAllZoneCommunicationStatuses()` (242) | New dictionary copy with `StringComparer.Ordinal`; callers can modify copy independently. |
| `void MarkZoneWarned(string geoid)` (247) | Blank ignored; trims, inserts unknown keys, permits Unreached→Warned; repeated Warned no event. |
| `void NotifyActionButtonConnected()` (271) | One-time unconditional log, guarded by `_loggedActionButtonConnected`, even when debugLogs=false. |
| `[ContextMenu("Regenerate Communication Towers")] void RegenerateCommunicationTowers()` (280) | Clears generated guard, attempts destructive regeneration and reapplies visibility; no reset event. |

Shared types must retain global type names, enum numeric values (`Unreached=0`, `Pending=1`, `Warned=2`) and tower fields: `string towerId`, `Vector3Int tileCell`, `Vector3 worldPosition`, initialized `List<string> associatedZoneGeoids`, `bool isActive`. Preserve `[Serializable]` and reference identity. Do not convert towers into immutable copies, structs, ScriptableObjects or renamed/namespaced types in this pass. Runtime tower/status collections are not serialized scene data.

### CommunicationPanelController contract

Minimum future dependency change: change **only the field's declared type and generic discovery** from C to `CommunicationController` (`P:52,829`), retaining serialized name `communicationTowerController`. Update the diagnostic type name at 700 if desired; no functional reason to rewrite the rest of P. Its existing `ShowForTower(CommunicationTowerData)` and status/warning method calls can remain unchanged when types/API are preserved. A same-GUID facade rename preserves the referenced scene component; replacing the component would instead require explicit PPtr migration.

Preserve all of the following (`P:108–132,134–163,250–355,374–813`):

- Bind on enable with up to 30 unsuccessful frame iterations plus final attempt; unbind stored callbacks and stop bind/success coroutines on disable. Minimize/restore/hide are P's responsibility.
- Opening a tower binds UI if needed, stores that **tower object**, clears selection, expands the full panel and refreshes. No automatic mode exit or tooltip clear occurs. Reopening an active tower clears prior selection again.
- Render zone tabs in `associatedZoneGeoids` order. Three tabs exist in current UXML; additional tabs clone `zone_tab_1` from the template. Blank/extra zones hide tabs and remove their handlers. Preserve required names, callbacks and clone behavior.
- Only Pending zones can be selected; click handling rechecks the latest status. Warned uses completed icon; Pending and Unreached share the pending icon while status labels display their actual enum text. No automatic event-driven refresh of external status changes exists.
- Send snapshots the selected HashSet into a List, calls `MarkZoneWarned` for each nonempty key, then clears selection, refreshes and shows the success sprite. No batch event or asynchronous dispatch exists; HashSet iteration does not promise a zone-event order. No Pending recheck occurs inside the send loop itself. Empty selection still runs the refresh/success path.
- Population is `Max(0, riskData.rawPopulation)` from ZoneBaselineRiskController, lazily ensuring baseline calculation; missing controller/data yields zero (`P:777–787`). Selected population sums stored tab populations with `Max(0, ...)`; it is not recalculated from tiles or shelter capacities.
- Active-shelter display uses S; route status uses EvacuationController; risk icons use baseline risk. Preserve all those calculations and sources.
- Missing sprites log once and retain existing UI backgrounds. Send normal/hover/selected sprites, hover scale and scaled-time success duration remain unchanged.

Do not redesign or modify P, its UXML or its USS in C1. Future dependency-only compatibility work must not absorb warning selection, callbacks, sprites or population behavior into communication helpers.

### MapHoverTooltipController contract

Minimum future change: change its serialized C field, the **parameter type** of `SetCommunicationTowerController` and two generic discoveries (`T:47,115,336,521`) to `CommunicationController`. Keep the setter name and all tooltip signatures; update the composition call after the facade rename. No event subscription or tooltip template redesign is needed.

Preserve (`T:87–118,169–224,244–327,395–409,412–567`):

- Reuse an existing tooltip root or clone its assigned UXML; ignore pointer picking recursively. Position in panel coordinates, bring to front and clamp with existing offset/padding/fallback dimensions. Hide all on T enable/disable; communication clear calls only its communication hide method.
- Inactive tower summary always says Unreached and counts **all** associated zones as unreached, regardless of actual dictionary entries. An active tower counts queried statuses; missing provider/status counts Pending. Overall active summary says Warned only when zone count>0 and all are Warned, otherwise Pending.
- Counts refresh on repeated show calls. Up to four served zones display IDs; more than four display just the count. Preserve DemoDisplayIdFormatter usage and null/unknown text.
- Unassigned UXML Editor fallbacks still point at `Assets/Dev/Dev_Scripts/UI_UXML_USS/UI Editor/UI_Panels_Tooltips/…`, while actual assets now live in Game. No corresponding old-path UXML was found. Builds lack this Editor-only recovery. The saved tooltip UIDocument already contains the root; general missing-host behavior remains a separate risk.
- Changing UIDocument resets cached element references and visibility flags. E and C can both configure a shared T; changing the host through one consumer can affect the other. Do not replace shared T or use `HideAllTooltips` for communication-only hover cleanup.

## 5. Proposed modular architecture

Only `CommunicationController` needs to be a MonoBehaviour. Plain helpers can use Unity value/object types and run on the main thread without independent Update/Awake callbacks. Keep helpers internal (or their operations internal when assembly/test constraints require type visibility). External gameplay/UI sees the facade and shared data definitions only.

### Responsibility and data movement map

| Component | Methods/data moving from C | Injected dependencies | Lifecycle and visibility |
| --- | --- | --- | --- |
| **CommunicationController** | Retains all §4 public APIs/events; Awake/Start/Update/OnDisable/OnValidate; `GenerateWhenReadyRoutine` and coroutine handle; `ResolveReferences`, `ResolveZoneController`, `ValidateGenerationReferences`, `FindTileMapDataAsset`; diagnostic connection flag. Coordinates private Generate/Activate/mode/regenerate ordering. | Existing 24 serialized fields, one instance of each helper; existing scene providers. | **MonoBehaviour**, sole lifecycle/input scheduler and event publisher. Public compatibility surface remains here; generation/activation orchestration and composition stay private. No singleton. |
| **CommunicationTowerGenerator** | `BuildZoneClusters`, `TryGetGroupCenter`, `TryGetZoneCenter`, `GetClusterCenter`, `FindNearestZoneIndex`, `TryFindTowerTile`, `TryScoreTowerTile`, `TryGetTileSnapshot`, `GetTowerTilePriority`, `IsRoadAdjacentUsableTile`, `IsShelterTile`, `IsWithinMap`, `GetMapLimit`, category predicates; private TileSnapshot/ZoneCandidate. Tower ID/cell/world/association construction from Generate body. | Existing Z zone membership and conversion, TileMapData read, communication Tilemap world-center conversion, S shelter predicates, settings/logging. A per-attempt input context/typed functions can isolate native calls for harnesses. | **Plain class**, no lifecycle. Internal generation operation returns ordered tower proposals/results; local clusters/occupied-cell set only. No persisted map/zone cache, no state ownership, no visuals/events/UI. |
| **CommunicationState** | `_communicationTowers`, `_towerByCell`, `_zoneStatusByGeoid`, `_hasGeneratedTowers`, `_isCommunicationModeActive`; `InitializeZoneStatuses`; data portions of status getters/Mark/Activate; tower clear/add/cell lookup/active filtering. | Shared tower/status types, ordered generated towers/GEOIDs; no scene lookup, camera, UI or rendering. | **Plain class**, no lifecycle. One authoritative instance. Internal mutations report transition outcomes to facade; do not invoke the facade. Getter semantics and mutable tower identity remain unchanged. |
| **CommunicationTowerVisuals** | `_towerVisualById`, `_towerRendererById`, `_loggedMissingSpriteWarning`; `SpawnTowerVisual`, `UpdateTowerVisual`, `GetSpriteForTower`, `ClearTowerVisuals`, `SetTowerVisualsVisible`. | Resolved visual root, sprites/colors/scale/offset/sorting settings; tower and hovered target passed by facade. | **Plain class**, no lifecycle. Uses Unity Object/GameObject APIs on main thread; explicit clear called by facade. No own communication mode/activation/status state and no independent scene search. |
| **CommunicationInputController** | Pointer-reading portions of `UpdateHoveredTower`, `HandleTowerClick`, `TryGetTowerUnderMouse`. | Camera, communication Tilemap, pointer position/button sample; read-only cell→tower lookup view/typed function from state. | **Plain class**, no Unity callbacks. One internal poll/evaluate call returns hit tower, screen point and left-down intent. Does not activate, publish events or call controller. No second hover target cache is necessary. |
| **CommunicationPresentation** | `_hoveredTower`, `_loggedOutlineRequestFailure`; presentation portions of Update/ClearHoveredTower; `ShowAssociatedZoneHighlight`, `ShowTowerTooltip`, `HideTowerTooltip`, `EnsureTooltipConfigured`, `ResolveTooltipUIDocument`; panel-open integration logic. | Z outline API/channel/color, resolved T/UIDocument and panel-show sink; tower/screen point passed from facade. Visual hover changes requested through returned old/new targets or facade ordering. | **Plain class**, no lifecycle. Explicit show/clear/open calls from facade. Owns only presentation target/diagnostics, no authoritative gameplay collection. No controller back-reference or state writes. |
| **CommunicationTypes** | Existing global `ZoneCommunicationStatus` and serializable `CommunicationTowerData` from `C:32–47`; only minimal internal result/context records if needed. C's `NormalizeGeoid` can become a shared internal utility with identical trim/null semantics; do not change P/T's independent formatting helpers. | Unity value types and standard collections. | **Plain definitions**, no MonoBehaviour/lifecycle or global service. Preserve public shared type shape; keep generation-only TileSnapshot/ZoneCandidate private to generator. |

### Composition and dependency direction

The facade constructs helpers, passes existing providers and calls them in this order:

```text
CommunicationController
  -> CommunicationTowerGenerator -> existing ZoneController/TileMapData/ShelterController
  -> CommunicationState         -> CommunicationTypes
  -> CommunicationTowerVisuals  -> Unity rendering objects + CommunicationTypes
  -> CommunicationInputController -> pointer/camera/Tilemap + read-only cell lookup
  -> CommunicationPresentation  -> outline/tooltip/panel output ports + CommunicationTypes
```

No helper stores or discovers `CommunicationController`, calls a sibling through an owner reference, or subscribes to its public events to perform its own work. The facade applies state results, renders and publishes events explicitly. Share the one state's tower references rather than duplicating them in another model. A lookup index over those same objects is not a second state; copied authoritative tower lists/status maps would be.

Use narrow presentation sinks (for example a supplied `Action<CommunicationTowerData>` bound to `P.ShowForTower`) rather than making Presentation depend on a UI controller that itself depends on the facade. Bind tooltip provider/document at the composition root. The existing P/T→facade gameplay queries remain UI inputs; helpers never depend on those provider back-references. Panel opening and status reads must not recursively trigger opening, generation or helper construction. No new global event bus or singleton is needed.

Separate the currently mixed `ResolveReferences` duties: the MonoBehaviour remains the composition root for scene searches, runtime T/root creation and missing-reference recovery; Presentation owns choosing/configuring the tooltip host and calling UI sinks, using resolved dependencies. Do not scatter `FindFirstObjectByType` into every helper.

For compatibility, keep serialized settings on the facade and preserve when changes are observed: sprites/colors are read during existing visual update operations, scale/height/sort are applied at spawn, grouping settings on generation. Do not silently freeze all Inspector values at Awake or refresh all existing renderers each frame.

## 6. Behavior-preservation baseline

These are **source-confirmed expectations**, not observed C1 Play Mode results. Preserve surprising behavior during extraction; schedule fixes separately.

### Startup, readiness and retries

- C Awake calls general resolution **before** its one-time ZoneController resolver (`C:123–127`). The resolver prefers explicit field, then same GameObject, then one active same-scene controller. Multiple active candidates produce null plus warning; a local/explicit assignment bypasses uniqueness checks. No zone search is retried by Generate.
- Start runs up to **120 failed generation attempts**, each followed by a yield, then one final normal attempt if still unsuccessful (`C:288–309`). After timeout there is no continuous retry; Show, Set(true) or Regenerate can attempt again. The `logWarnings:false` flag suppresses only some C warnings; Z/provider logs, missing-sprite and no-valid-cell warnings may still occur.
- Each unguarded generation calls ResolveReferences, destroys/clears existing visuals, clears tower list and cell lookup, then validates map/Z/communication Tilemap (`C:311–338`). Camera, sprites, panel, tooltip document and shelter are not required for generation. A null/empty Z dictionary returns without marking generated.
- Z.GetAllZoneTiles lazily ensures its existing index. Loader readiness requires non-null loader/payload/geoidGrid and nonempty cellToRC; Z additionally requires its map/terrain references (`Z:95–158`, `I:23–39`). Current J loads synchronously in Awake, but its failures/late configuration can exercise the retry path. There is no C subscription to loader completion.
- A nonempty zone dictionary reaches `_hasGeneratedTowers=true` even if every group fails to produce a tower. A fully initialized empty ZoneIndex returns no usable zones; subsequent Ensure does not rebuild it automatically. Same-loader mutations require explicit Z index invalidation/rebuild; C.Regenerate does not invalidate it.

### Grouping, placement and deterministic ordering

- Take Z's zone keys, sort with `StringComparer.Ordinal`, and initialize normalized nonempty unique status keys to Unreached (`C:343–350,549–610`). GEOID normalization only trims/turns whitespace into null. ZoneIndex stores original case-sensitive GEOID keys. Whitespace-wrapped keys can receive normalized statuses but fail the subsequent normalized zone lookup; do not normalize upstream data as a refactor side effect.
- Eligible zones need a nonempty tile set. Seed each group with the first remaining sorted zone. Grow to zonesPerTower by nearest **squared** distance from the mean of zone centroids, recomputing that center after each addition. Ties retain the first remaining candidate (`>=` skip), so sorted seed/list order matters.
- Placement group center is the mean of **all member tiles**, weighting larger zones more, unlike the equal-zone centroid used during clustering. Do not combine these calculations or sort the associated list again.
- Round group center with Unity Mathf.RoundToInt. Search square perimeters radius 0 through `Max(towerSearchRadius, map width, map height)`, inclusive. Within a perimeter iterate y ascending, then x ascending. Return after the **first radius with a valid winner** (`C:711–757`). towerSearchRadius is not an outer search cap.
- Score = `priority*1000f + EuclideanDistanceToUnroundedGroupCenter`. Reject null/out-of-map tiles, water, occupied tower cells and (when enabled) shelter tiles. Equal scores keep the first scanned cell. Priority applies within the chosen radius; a distant building cannot displace a valid center tile merely because its category is better.
- Priorities: 0 building-category tiles; 1 usable land/building tiles adjacent orthogonally to road-category tiles (buildings already win at 0); 2 other land-category tiles; 3 other non-water categories, including roads themselves. Exact sets are listed below. There is no requirement that the candidate belong to a served zone.

Exact category sets from current source override ordinary-language descriptions:

| Predicate | Exact matching rule |
| --- | --- |
| Water | Lowercase category **contains** water, river, stream, lake, ocean or flood; OR non-null TileType.isWater. |
| Building | Lowercase category equals building, industrial, residential, commercial or city. |
| Road | Lowercase category equals road, highway or rail. |
| Land | Lowercase category equals land, grass, forest, field, plain, beach, mountain or soil. |

The source does not trim category text and does not inspect TileInstance.waterHeight, terrain elevation, warning status, population, route access or resource costs for tower placement. Category priority/order must be transplanted exactly.

### Water and shelter avoidance

- Water exclusion uses the predicates above, not current flood depth. Flooded land can still be eligible; a category merely containing `flood` is rejected. Do not connect a new water-system provider during extraction.
- S.TryGetShelterAtTile returns an existing placement; C rejects it if returned data is null **or active**. An inactive placement alone is not excluded. S.IsValidShelterCandidateTile separately excludes a currently valid/placeable **cached** candidate (`S:342–346,398–405`). That query does not refresh candidate generation.
- Missing S allows all otherwise valid cells. Candidate exclusions depend on whether shelter candidates were already refreshed when C first generates. There is no readiness barrier for S and no update subscription; shelter changes after generation do not move towers automatically.

### Coordinates, origins and tower identity

- Tile snapshots use normalized `Vector2Int` with dimensions `N×N` when N>0, else sizeX×sizeY. `TileMapData.Get` uses x*sizeY+y and does not itself bounds-check. Preserve the existing normalized read/provider assumptions.
- Z's origin is cached by ZoneIndex from compressed terrain cellBounds; before index initialization its property can compress the terrain and read its origin. `NormalizedTileToCell` adds origin **once**, z=0; `CellToNormalizedTile` subtracts it (`Z:59–72,190–203`). Never use communication Tilemap bounds to invent a second origin/cache.
- `tower.tileCell = Z.NormalizedTileToCell(candidate)`; `tower.worldPosition = communicationTilemap.GetCellCenterWorld(tileCell)` (`C:365–375`). The visual adds the y offset; the data's worldPosition remains the cell center. Hover uses the original cell, not the raised sprite position or a collider.
- IDs start `tower_01`, `tower_02`, … using `tower_{index:00}`; index increases only for successful groups. Associated GEOIDs preserve seed/nearest-addition order. Two towers cannot occupy the same absolute cell in one generation. Every generated tower starts inactive.
- No runtime towers/positions/GEOID lists are saved in the scenes. This audit records their exact construction contract, not an invented live manifest. Future baseline capture must record the ordered `(towerId, normalized tile, absolute tileCell, worldPosition, associatedZoneGeoids, isActive)` tuples from the actual current controller before extraction.
- Determinism assumes identical provider contents, candidate readiness, Unity math and iteration order. Zone tiles are HashSets and centroids accumulate float sums; do not claim cross-runtime/platform byte determinism for arbitrary very large coordinates. Keep ordering and math unchanged and compare the real tuples in supported fixtures.
- J currently paints at `(column, rows-1-row)` and stores TileMapData at those absolute zero-based cells (`J:272–315`). A shifted-origin fixture must arrange provider data consistently; moving a transform alone is not equivalent to shifting cell coordinates. Negative/nonzero-origin tests must keep normalized map reads distinct from absolute cells.

### Activation, warning timing and state resets

- First click on a hovered tower sets isActive and updates sprite/color; each normalized associated Unreached/unknown zone becomes Pending; preexisting Pending/Warned stays unchanged; then `OnTowerActivated(tower)` fires; then P.ShowForTower (`C:987–1045`). List entries are not de-duplicated during activation, but only the first effective status change writes Pending. There is no public ActivateTower API today.
- Repeated click on an active tower opens P again, with no activation event and no status reset. No action-point/money check exists in this controller.
- MarkZoneWarned normalizes, ignores blank, accepts **unknown** GEOIDs and writes Warned even directly from Unreached. Only a changed status invokes OnZoneWarned, synchronously after the write (`C:247–269`). Tower activation never emits OnZoneWarned. These events are ordinary instance C# events, not persistent UnityEvents.
- P's send loop emits one event per newly warned selected zone; observers can see partially completed multi-zone batches. R uses an unscaled-time mitigation cooldown (default 0.1s), so several warning events in one frame can lead to fewer turn advances. Preserve event counts/timing independently of downstream turn counts (`R:195–223,680–689`).
- Generation with usable zones clears **all statuses** back to Unreached before building towers. Explicit regeneration creates new inactive tower objects, restarts IDs, and discards prior activation/warnings without activation/warning/mode/reset events.
- Failed regeneration clears tower list/cell lookup/visuals **before** validating, but does **not** clear the old status dictionary unless it reaches InitializeZoneStatuses. Do not make this an atomic success-only replacement in a behavior-preserving extraction.
- Regenerate does not clear `_hoveredTower`, P's current tower or selection, tooltip state, diagnostic flags or Z's cache. Old tower references can survive until hover changes/panel reopening; immediately after regeneration the old hover outlines/tooltip may remain. Preserve and test this edge case separately from any later fix.

### Visibility, input and presentation

- Spawn GameObject `CommunicationTower_{towerId}`, parent under saved/generated root, local scale `Vector3.one*towerVisualScale`, world position y+=towerHeightOffset; add SpriteRenderer; apply sorting settings (`C:860–889`). Missing both sprites produces one warning per controller lifetime and still spawns empty placeholders and logical towers.
- Inactive sprite prefers main, falls back to zone; active prefers zone, falls back to main. Hovered color overrides active/inactive color. Sprite change is not tied to whether zones have been Warned.
- Startup visibility is mode OR showInactiveTowersAtStart, keeping active towers visible. Set(true) attempts generation twice through the explicit call and Show (guarded after success), shows all, then sets mode and emits. Set(false) clears hover and shows inactive according to startup setting, keeping active. Repeating Set emits again. Show/Hide do not alter mode or emit mode events; Hide keeps active visible (`C:161–200,927–941`).
- Input runs only when mode flag is true, hover before click. It uses legacy `UnityEngine.Input.mousePosition` / `GetMouseButtonDown(0)`, despite the general architecture's New Input System direction. There is no UI-pointer-over check, mode-exclusion gate, collider hit test, same-frame entry guard or renderer-visibility test. Do not migrate input systems or add gates in this extraction. Mode can remain true after generation fails; hidden logical towers remain hittable.
- Hover converts camera screen position to world, forces world.z=0, then communication Tilemap.WorldToCell and state cell lookup. Missing camera/Tilemap produces no hit; no hover-time camera rediscovery.
- A new target clears prior hover, recolors, replaces outlines in **CommunicationZones** with associated GEOIDs/colors, then shows tooltip. Remaining over the same tower only updates tooltip; it does not retry a failed outline request (`C:943–964`). Leaving/no hit/Hide/mode-off/disable clears only CommunicationZones and the communication tooltip. Ordinary Hover/BaselineRisk/Shelter/Evacuation outline channels are not cleared by these methods.
- Outline request failure warns on transition into failure; successful future requests reset that diagnostic flag. Z controls renderer readiness and visibility policy; gameplay/tooltip can continue without outlines. Do not replace this with per-zone legacy rendering or a new outline cache.
- Panel open retries only its missing P lookup, warns if missing, and otherwise calls ShowForTower. It does not disable communication mode. A missing/unbound panel cannot roll back activation/events.

### Disable/re-enable and fallback behavior

- OnDisable clears hover and stops/nuls the startup coroutine. It does not change mode, hide all tower visuals, reset state, emit mode events or close P (`C:143–152`). With the component disabled, Unity stops Update but ordinary public calls are not explicitly guarded against disabled state.
- There is **no C.OnEnable**. Re-enabling after its initial Start does not restart an interrupted/expired generation coroutine. Existing state/mode persists and input resumes; missing generation needs an explicit public retry. Hierarchy deactivation may independently affect child visuals; disabling just the component does not request that.
- There is no explicit C.OnDestroy subscriber cleanup or visual-root disposal. Child GameObjects follow Unity hierarchy destruction; an externally assigned root is not explicitly emptied on controller destruction. Consumers own their instance-event subscriptions and unsubscribe on disable. Do not add permanent/static forwarding subscriptions or rely on a new reset event that current consumers do not receive.
- Missing map: choose the first loaded Resources.FindObjectsOfTypeAll<TileMapData> with N>0 or sizeX>0; selection is not uniquely scoped and can choose the wrong map in multi-map contexts. Missing S/T/P use global first-object discovery. Missing T creates a child component; missing visual root creates a child root. Missing camera uses Camera.main on reference resolution. Missing communication Tilemap falls back to Z.TerrainTilemap when Z is already resolved; the later generation pass can fill it after Awake's Z resolver.
- Missing Z/TileMapData/communication Tilemap prevents generation; optional UI/camera/renderer/shelter/sprites degrade as described. Missing tooltip document chooses GlobalHUD's document, else a discovered document with HUD/tooltip marker names, else first root-bearing document (including inactive documents). A provider appearing later may be recovered only at the existing resolution/call points.

## 7. Migration strategy

All steps below are **recommendations**, not C1 edits or authorization to write Game. The standing AGENTS rule forbids writes to Assets/Game, and C1 additionally forbids production/serialization changes. Future work needs an explicit authorized integration scope before touching/moving the current Game controller, panel, advisor or Game scene. Within the current policy, preparation can occur only in a dedicated Dev feature folder without installing a competing MonoBehaviour.

### Recommended incremental stages

1. **C2 preparation and baseline:** record current source/scene hashes and runtime tuple/event traces; add only plain helper/types work and meaningful fixtures in a dedicated `Assets/Dev/Dev_CommunicationSystem/` area. Follow the Dev filename-prefix policy for new helper/test files. Do not duplicate globally named shared types while their old declarations still compile. New internal generation/state helpers can consume existing types initially. No second component or scene binding.
2. **Authorized extraction behind C:** retain the original class, script asset GUID and all Inspector fields. Move shared definitions exactly once; extract State and Generator, then Visuals, then pointer/presentation logic in individually compilable checkpoints. C remains sole authority. Each completed slice delegates to a single helper instance; delete/replace the moved executable path in C in the same slice so no double generation/input/events can occur. Helper data remains runtime-only. Compare before/after fixtures after each behavior-bearing slice.
3. **Verify the still-named legacy facade:** all public calls/events/panel/status contracts pass with helpers. Only C owns Start/Update; helpers never start coroutines or register parallel input. Preserve destructive reset ordering, not an improved regeneration transaction. Verify actual PlayableTilemap in native Play Mode; Navigation stays text-only.
4. **Atomic facade rename and consumer retarget:** rename the same `.cs` asset **and move/rename its existing `.meta` with it**, maintaining GUID `98e604977a2f4ef5ac4190852d01e7fa`; change the MonoBehaviour class to `CommunicationController` and file basename to match. Retarget all six consumers' declared field types and generic discoveries, T setter parameter, A factory return type/AddComponent type, and diagnostic/nameof references as needed in one compile-safe change. Keep existing field names/method/event names. If relocation into Dev is required, it must be explicitly authorized because the source currently lives in Game. Keep the current assembly/global namespace to avoid unnecessary type migration.
5. **Serialized validation:** import and inspect the two existing component identities and all 24 values and four inbound PPtrs per scene. A GUID-preserving rename should require no scene/prefab rebinding; do not remove/re-add the component or mass-reserialize the scenes. Verify all old method names/persistent target strings with another full scan. No current prefab binding needs migration. Preserve current UXML/USS and button names/callback registration exactly.
6. **Retirement gate:** only after compilation, serialization inspection, comparison fixtures and PlayableTilemap checks pass, accept the new class/file name as authoritative and confirm no executable old type/discovery/AddComponent remains. Historical reports can retain old names. Avoid an obsolete second MonoBehaviour or an empty stub with its own scene component. Review and commit each verified checkpoint; keep asset plus meta changes together for rollback.

### Why the same-asset rename is safer

| Option | Consequence in this repository | Recommendation |
| --- | --- | --- |
| Rename the existing MonoBehaviour asset/class and keep its GUID | Preserves two component fileIDs, 48 total saved field assignments and eight external scene PPtrs; no current communication persistent-event strings to rewrite. Requires atomic C# consumer updates and Unity import verification. | **Preferred**, after helper extraction has been verified. Keep backup/manifest; matching GUID alone does not establish successful native deserialization. |
| Create a new component with explicit serialized migration | New GUID/component fileIDs; must copy all fields, rewrite every inbound PPtr and runtime creation path, and remove/disable old authority without an intermediate playable duplicate. Additional native migration code/scene edits and rollback complexity. | Reserve for a demonstrated Unity import/type-migration limitation or a separately approved structural/assembly change. No evidence currently requires it. |

If a fresh-component migration becomes unavoidable, implement a dry-run manifest/mapping first, validate each destination value and inbound reference, then replace the authority in one editor transaction while not playing. Preserve scene and prefab overrides individually, not by property-name guesswork. Scan persistent events' `m_Target`, `m_TargetAssemblyTypeName` and `m_MethodName` and rewrite only actual matches. Verify exactly one enabled authority and that A discovers that same authority before allowing Play Mode. Never retain both active for comparison.

Renaming removes the old C# type name, so all in-repository references must change atomically. It does not remove existing facade method/event APIs. No external compiled assembly dependency on C was found in source; if one is later discovered, decide its migration explicitly rather than adding a second authoritative MonoBehaviour. A type-movement attribute is not a substitute for preserving the MonoScript asset or validating serialized references.

## 8. Regression baseline and test plan

### C1 verification actually performed

- Read and traced the current controller, panel, tooltip, zone/shelter providers and all direct consumers; inspected loader/index/map behavior and shared evacuation tooltip integration.
- Reconciled every requested identifier with current C# callers; resolved script/asset GUIDs through existing metadata; inspected the full C YAML blocks and inbound references in both scenes; searched scenes/prefabs/UI/persistent calls as described in §1–3.
- Confirmed C has exactly 24 serialized Inspector fields, both C scene blocks have identical assignments, and no current communication test source or proposed CommunicationController exists.
- **No gameplay harness, C# compilation, Unity import, native rendering/UI callback test or Unity Play Mode was run in C1.** Historical test counts are not included as C1 passes. No actual scene tower manifest has been captured in this stage.

### Future capture and comparison protocol

Before changing C, run the original and extracted implementations against the same provider fixtures in separate runs. Capture ordered tower tuples (§6), status dictionary snapshots, mode, query copy/reference semantics and ordered trace markers (`state write`, `visual update`, event, panel open, outline/tooltip requests). Independent expected cases below prevent a harness from merely repeating implementation. Run actual C/source where practical; Unity stand-ins establish managed logic/order only.

Keep runtime manifests temporary or under authorized Dev test artifacts, never map/scene/settings changes. For native tests use only `Assets/Game/Content/Scenes/PlayableTilemap.unity`; make reference/origin/failure mutations in memory during Play Mode, do not save them. Navigation receives serialized read-only inspection only. Stop native testing if it would require unauthorized scene writes or a different scene.

| Check | Required expected result / observation | Managed C# harness | Native Unity Play Mode |
| --- | --- | --- | --- |
| Cold startup after JSON | Original loader populates mappings/map; one generated tower set; IDs/centers/associations/statuses match captured original; no double spawned set after Start and mode-on. | Replay ready-provider fixture and facade sequencing; cannot prove loader Awake/import order. | **Required** for actual JSON, TileMapData initialization, Awake ordering and visuals. |
| Late loader readiness | Ready within polling bound succeeds; beyond timeout no automatic endless retry; Show/Set(true)/Regenerate can retry; missing Z at Awake is not rediscovered by generation. | Step IEnumerator through 120 failed yields plus final attempt; ready-at-boundary and no-zone fixtures. | **Required** for real coroutine frames, dynamic enabled objects and loader configuration. |
| Deterministic grouping | Ordinal seed, first nearest tie, recomputed equal-zone centers, tile-weighted placement center; stable tuple sequence for repeated unchanged input. | **Yes** with asymmetric zones and shuffled insertion fixtures. Example A one tile (0,0), B three tiles (6,0),(6,1),(6,2): group placement center=(4.5,0.75), not equal-zone mean=(3,0.5). | Confirm saved-map manifest across reloads; platform claims limited to tested runtime. |
| Candidate priority/ties | Valid center land beats radius-1 building; in same ring building priority wins; equal distance/priority retains y-then-x scan order; search can exceed configured radius; unknown categories eligible at priority 3. | **Yes**, including blocked center (5,5), tied cells (5,4)/(4,5) → (5,4); exact predicates and water-flag override. | Spot-check sprite cell positions and actual map category data. |
| Activation/repeated selection | First selection active, associated Unreached→Pending before one activation event, then one panel-open request; repeat produces panel-open only. Warned remains Warned on activation. | **Yes**, assert event-time state and reference identity; internal test access, no new public Activate API needed. | **Required** for pointer hit and panel reopening/selection reset. |
| Status transitions and queries | Unreached→Pending→Warned; direct Unreached/unknown→Warned accepted; blanks ignored; whitespace trimmed; repeated Mark no event. Getter default/copy/live list behaviors unchanged. | **Yes**, assert enum values, map copy isolation and same tower object references. | Confirm displayed statuses/icons agree with original. |
| Warning dispatch/event counts | Select k Pending zones → k state transitions/events, zero activation events; same zones again produce zero new warning events; state visible inside callback. HashSet zone order not required. | **Yes** for C/P command trace and observers; distinguish emitted events from R cooldown effects. | **Required** for actual buttons, callback multiplicity, advisor/summary/turn timing; check zero-selection success behavior. |
| Mode toggles/show/hide | Repeated Set emits each time; Toggle flips; Show/Hide leave mode unchanged; active towers stay visible on Hide; mode can be true with no towers. | **Yes** for state/event/visibility commands. | **Required** for action_Button6 styling/one callback and SpriteRenderer activity. |
| Hover/channel isolation | New target replaces CommunicationZones only; leaving, mode-off, Hide and disable clear it only; same-target failed outlines are not retried until target changes. | **Yes** using captured channel calls and failing renderer sink. | **Required** with existing Hover/BaselineRisk/Shelter/Evacuation outlines and renderer visibility policies. |
| Tooltip visibility/content | Existing root reused; follows/clamps; inactive all-Unreached; active unknown status→Pending; warned summary only all-Warned nonempty; communication clear preserves evacuation tooltip. | Content/count and call-order checks possible with narrow UI stand-ins. | **Required** for UIDocument layout, pointer ignoring, cloning/host changes and shared E/T behavior. |
| Panel/tab population | Associated order, three initial tabs, cloning for >3, extra/blank tabs hidden; Pending selection only, reopen clears selection, minimize/restore retained, population uses raw risk values. | Use mocked dependencies to check value sources/command counts; not evidence of native callbacks. | **Required** for actual UXML/USS, callbacks, icon fallbacks, send hover/success sprites and population labels. |
| Shelter avoidance | Exclude active placed/null-returned shelter and valid cached candidate; inactive-only/missing S allowed; avoid flag bypass; changing S after generation does not move towers. | **Yes**, including empty candidate cache vs refreshed cache at generation. | **Required** with real S readiness, placement/candidate data and cell conversions. |
| Nonzero/negative origins | Fixtures (0,0), (9,-4), (-20,-10): normalized candidate unchanged; cell=normalized+origin once; world uses communication Tilemap center; shelter/hit tests use same absolute cell. | **Yes** for injected conversion/math, roundtrip and distinct terrain/communication map providers. | **Required** for real Grid/Tilemap transforms/cell bounds/camera; arrange normalized map data consistently with shifted cells. |
| Disable/re-enable | Clear hover/tooltip; stop retry; retain mode/state; do not auto-resume generation after Start; component-only disable does not explicitly hide sprites or panel. | Step lifecycle methods/IEnumerator with stand-ins; assert preserved flags/no events. | **Required** for Unity component vs GameObject behavior and actual coroutine cancellation. |
| Scene reload/event cleanup | No cross-run towers/state/listeners; one A callback/subscription, one per G/D/R event handler on re-enable; no unexpected external-root leftovers. | Verify subscription pairings and managed fake-publisher counts. | **Required** for domain/scene reload configurations, Unity destroyed-object semantics and deferred destruction. |
| Regeneration after activation | New tower instances inactive, IDs restarted, statuses reset without events; failed prerequisites remove towers but preserve old status dictionary; stale P/hover references match baseline until refreshed. | **Yes**, successful/failed/zero-valid-cell variants and event counts. | **Required** for delayed Destroy (old/new visuals may coexist until frame end), actual panel/tooltip/outline residue. |
| Missing references/fallback | Required references fail as described; optional sprites/camera/S/P/T/UIDocument degrade without changing state semantics; first Resources map/first UI-provider lookup remains current behavior. | **Yes** for guards, logs and fallback selections with controlled stand-ins; don't claim real asset loading. | **Required** for Camera.main, Resources/FindObjects discovery, AddComponent Awake/OnEnable timing and Editor vs build tooltip recovery. |

Meaningful harnesses can compile the plain state/generator with small provider seams and Unity value types/stand-ins, or run in Unity Edit Mode for Unity-backed values. Native tests remain necessary even if managed comparisons pass. Existing water test asmdefs do not define a communication test assembly; avoid moving this subsystem into a new assembly simply for testing, because dependencies currently live in default project compilation. Use an explicitly reviewed Editor/harness arrangement under Dev without project-settings changes.

### Snapshot fingerprints

These SHA-256 values identify the audited bytes; they are not test results:

| File | SHA-256 |
| --- | --- |
| C | `b79f83c2df8ac962468ea4133aa6e32847a8eccc05e38254bbe1641d80b5d7f8` |
| P | `0ae952b633a5ef1d2673f04b08a0e0679692b5fe276f1c5e1f82499f21aae43d` |
| T | `878290cb4ff3ef794c1bb30536b164ab07534b2069052bdb4a99e7b18ef16077` |
| Playable | `482a2ccedd674f2b1af5cd095982d0b0a7de1bfb2b4faa7031bc5ee426dc0796` |
| Navigation | `25f1d90a301c1c36caabeb4b3687dc888328478bc96334316db2eb7bbf27f42b` |

The pre-existing tracked working-tree delta (`git diff --binary HEAD`) had SHA-256 `e95416e201710b7ac112eec92dfafa5f438713f311fc44b03a3510762c1de080`. Final verification should retain that fingerprint; the new audit is the sole added artifact and has no manually created metadata.

## 9. Risks and blockers

| Risk / blocker | Implication and treatment |
| --- | --- |
| Production integration is outside current write authorization | Stage C1 can complete. A future integrated extraction/rename touches Game sources and possibly import-generated metadata; obtain explicit task scope overriding applicable restrictions before doing it. Dev-only plain helpers are preparatory until wired into the existing authority. |
| Unity behavior is unverified in C1 | GUID/fileID text matching does not prove successful import, live pointer/UI behavior or scene reload cleanup. Require PlayableTilemap Play Mode before retiring the old name. Do not cite old reports as current native validation. |
| A can auto-create C when discovery misses | Updating only scene bindings or only some generic discovery sites can create a second authority. Migrate the factory atomically and check scene-disabled/additive/fallback setups without silently deleting current fallback behavior. |
| Shared T host ownership | C/E can reconfigure the same tooltip document; fallback UI paths are stale. Preserve host and communication-only cleanup; verify with native UI. Defer recovery-path cleanup to separately scoped work. |
| Generation and readiness are not atomic | Timeout, one-time Z resolution, zero-tower success and failed-regeneration retained statuses are current edge cases. Extract faithfully; changing retries/transactions affects gameplay and test expectations. |
| Cached shelter availability affects first generation | Tower manifest can differ with candidate readiness even on the same map. Capture readiness and cached-candidate state with baseline tuples; no new shelter cache/subscription in extraction. |
| Mutable tower objects escape through read-only lists | External code could mutate them; present consumers only read. Preserve identity/shape now; encapsulation redesign would change a public contract. P/hover may keep stale references after regeneration. |
| Event order has gameplay effects | G/D refresh and R's turn cooldown run synchronously inside events before panel open or before a warning batch finishes. Avoid duplicate/queued/deferred/coalesced communication events. |
| Coordinate/map-provider assumptions | Explicit communication Tilemap differs from terrain; map dimensions use N and the old TileMapData API; shifted cell data must match normalization. Do not create a replacement map cache or expand rectangular-map behavior during extraction. |
| Legacy input vs architecture direction | Current C uses old Input APIs. Migrating to New Input System or UI click blocking is behavior work and outside the extraction baseline. |
| User has unrelated uncommitted changes | Work from current bytes, preserve them and avoid blanket resaves/resets. Re-capture manifest if those assets change before C2. |

No unresolved question blocks the documentation deliverable. The principal implementation gate is explicit integration scope plus native behavior/serialization verification; neither is permission to modify production assets in C1.

## 10. Recommended Stage C2 implementation scope

Keep C2 small: **types/state/generator extraction preparation with an unchanged authoritative legacy facade until production integration is authorized**.

Deliver a dedicated Dev feature area with internal plain state/generator helpers using the existing shared types, plus independent fixtures for grouping, scoring, origin conversion, state transitions and regeneration outcomes. Do not introduce a public CommunicationController MonoBehaviour yet, duplicate shared global type declarations, add a map/zone cache, modify panels or change scene bindings under the current restrictions. Moving the two shared declarations and delegating C to those helpers belongs to the first separately authorized integration slice, since their current declaration/source is in Game.

For that authorized slice, retain all 24 serialized fields, original C class/GUID/component identities and all public APIs; replace only the moved state/generation bodies with delegation, preserve runtime ordering and compare captured original/extracted results. Keep rendering/input/presentation in C until that slice passes. Next slices extract those concerns, then perform the atomic same-asset rename and consumer type changes described in §7.

C2 exit evidence should identify changed files, compile results, ordered tower/status/event comparisons and untouched serialized manifests. Native PlayableTilemap verification remains required before final facade rename/retirement. Do not couple communication extraction to panel redesign, new warning mechanics, population logic, input-system migration, fallback fixes or map-system replacement.
