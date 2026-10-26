# CommunicationSystem — Stage C2: Tower Generation and State Extraction

Date: 2026-10-23. Implemented as one stage under the task's explicit authorization to edit the existing Game controller. `CommunicationTowerController` remains the sole public gameplay MonoBehaviour. Internal plain classes now own generation and runtime state; UI, rendering, input and lifecycle orchestration remain in the original controller. All 215 managed assertions pass before/after; all 73 captured states match. Runtime compilation passes with zero errors and the same 11 warnings. Both scenes and all existing metadata are unchanged. No Unity Editor or Play Mode execution was performed by this stage.

## 1. Files added and modified

Modified:

- `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs` — delegates generation and state operations; 1,221 → 721 lines. This is the only previously existing file changed by C2.

Added:

- `Assets/Dev/Dev_CommunicationSystem/Dev_CommunicationTowerGenerator.cs` — internal `CommunicationTowerGenerator`.
- `Assets/Dev/Dev_CommunicationSystem/Dev_CommunicationState.cs` — internal `CommunicationState`.
- New folder/script metadata for that Dev feature area, using fresh GUIDs and the repository's existing metadata formats. No existing metadata was edited.
- This report and its new metadata.

Existing unrelated changes to the font asset, PlayableTilemap scene and two layout files were preserved. The untracked Stage C1 audit was also preserved. No other production source, UI asset, scene, prefab, map asset, package or project setting was modified.

## 2. Responsibility split

| Owner | Responsibility |
| --- | --- |
| `CommunicationTowerController` | Existing public API/events, all 24 Inspector fields, Awake/Start/Update/OnDisable/OnValidate, readiness coroutine, reference resolution, mode/activation/regeneration coordination, visuals, mouse input, hover/outlines, tooltips and panel opening. |
| `CommunicationTowerGenerator` | Sorted zone input, grouping, centroid math, perimeter search/scoring, water/shelter rejection, occupied cells, ID/cell/world-position construction. Receives existing providers/settings; returns ordered tower records. |
| `CommunicationState` | Tower list, cell lookup, zone status dictionary, generated/mode flags, activation/warning transitions and queries. Returns transition outcomes without public events, UI, rendering, discovery or a controller reference. |

The generator is constructed per generation attempt after reference validation and status initialization. It borrows the existing zone dictionary; it does not retain a map/zone cache or create a ZoneIndex. Its local occupied-cell set lasts only for that generation enumeration. Neither helper is a MonoBehaviour, singleton or independent input/coroutine source. No `CommunicationController` MonoBehaviour was introduced.

## 3. Methods moved

Generator methods moved with identical bodies: `BuildZoneClusters`, `TryGetGroupCenter`, `TryGetZoneCenter`, `GetClusterCenter`, `FindNearestZoneIndex`, `TryFindTowerTile`, `TryScoreTowerTile`, `TryGetTileSnapshot`, `GetTowerTilePriority`, `IsRoadAdjacentUsableTile`, `IsShelterTile`, `IsWithinMap`, `GetMapLimit`, `IsWaterCategory`, `IsBuildingCategory`, `IsRoadCategory`, `IsLandCategory`, and its private `NormalizeGeoid` utility. The private TileSnapshot/ZoneCandidate definitions moved with them.

Ordinal sorting now lives in `GetOrderedGeoids`; the tower-construction loop is `GenerateTowers`. Its enumerable yields each constructed record to the coordinator, which immediately adds it to state, spawns its visual and logs its association before requesting the next record. This preserves interleaving and avoids an atomic replacement of generation.

State now owns `InitializeZoneStatuses`, `TryGetCommunicationStatus`, `GetActiveTowers`, `GetZoneCommunicationStatus`, `GetAllZoneCommunicationStatuses`, and the collections those operations use. New internal operations `ClearTowers`, `AddTower`, `TryGetTowerAtCell`, `TryActivateTower`, `SetTowerZonesPending` and `TryMarkZoneWarned` contain the corresponding extracted mutation/lookup logic. Pending transitions yield diagnostic outcomes after each state write, preserving per-zone log timing in the controller. The controller's separate GEOID-normalization utility remains for its unchanged presentation logic.

No generation/scoring or dictionary-based transition loop remains in the controller. It retains visual lookup dictionaries and the hovered-tower presentation reference.

## 4. Public API compatibility

All **17 controller public method/property/event signatures** are unchanged. Query methods delegate; the live list property returns the state's original backing list. Shared global `CommunicationTowerData` and `ZoneCommunicationStatus` declarations remain byte-identical in the original source, including serializability, field names, list initialization, numeric enum values and reference semantics.

No panel, tooltip, Actions, summary, resource-bar, advisor, zone, shelter or evacuation source was changed. The complete runtime compilation includes those existing consumers and their actual dependencies; no consumer migration or compatibility fallback was needed.

Twenty-five remaining controller method bodies are byte-identical to the original, including reference resolution, spawn/update/clear visuals, hover handlers, outlines, tooltip configuration, panel opening and disable cleanup. Other necessary changes route mode flags, tower-list reads and cell lookups through the state helper.

## 5. State ownership and lifecycle

One readonly state instance is created per controller. Only it owns the tower list, cell dictionary, status dictionary and generated/mode flags; the controller retains no parallel authoritative copies. Tower objects remain the same references returned to public queries, activation events and the panel.

`ClearTowers` deliberately clears only towers/cell lookup. Statuses are reset only after valid providers and a nonempty zone dictionary are available. Mode is independent of regeneration. Successful generation creates inactive tower objects; a nonempty zone dictionary still marks generation complete even when placement produces zero towers.

Lifecycle remains unchanged: Start drives the bounded retry coroutine; OnDisable clears hover and stops the routine without resetting mode/state or closing the panel. No OnEnable retry restart, map/shelter subscription or new reset event was added. The generator does not publish events or retain authoritative state.

## 6. Generation behavior comparison

The baseline used the current controller captured before editing, SHA-256 `b79f83c2df8ac962468ea4133aa6e32847a8eccc05e38254bbe1641d80b5d7f8`, rather than a historical implementation. The original source was run against the same fixtures as the extracted version.

Preserved and compared:

- Ordered IDs, absolute cells, world centers, associated GEOID order, active flags and unique occupancy; group sizes 1/2/3 and origins `(0,0)`, `(9,-4)`, `(-20,-10)`.
- Ordinal seed/nearest-zone ties, unequal zone sizes, tile-weighted group centers, first-valid-radius behavior, y-then-x perimeter ties, category priority, rail adjacency, water-name/TileType rejection, and search beyond the configured minimum radius.
- Active/inactive/null placed shelters, cached candidates, avoidance disabled, and absent shelter provider; absolute-cell comparisons at negative origins.
- Successful and failed regeneration, zero-valid-placement completion, cached index reuse/explicit invalidation and late readiness/120-yield timeout.

Independent expectations include:

| Fixture | Expected result, verified on both versions |
| --- | --- |
| A has `(0,0)`; B has `(4,0),(4,1),(4,2)` | Tile-weighted center `(3,0.75)` selects normalized `(3,1)`; association order A,B. This differs from equal-zone averaging. |
| A centroid `(1,1)`; B `(2,1)`; C `(0,1)`; size 2 | Equal-distance nearest-zone tie selects B before C. |
| Blocked center `(2,2)`; equal land candidates `(2,1)` and `(1,2)` | Perimeter traversal selects `(2,1)`. A farther building does not displace that first valid radius. |
| All water except `(4,4)`, center `(2,2)`, configured radius 1 | Placement reaches `(4,4)` beyond the configured minimum. |
| First tower's spawn changes subsequent map candidates to water | First tower is already in the public live view at spawn; the next group observes the changed candidates and is skipped. This verifies generation/spawn interleaving. |

Generation still clears visuals, clears towers/cell lookup, validates providers, obtains zones, initializes statuses, then calculates/stores/spawns results and sets generated=true. Failed regeneration can leave no towers/visuals but retain old statuses. Existing stale panel/hover references and diagnostic behavior were not repaired.

## 7. State and event comparison

The user clarified the activation ordering discrepancy in the written C2 specification: **preserve current source order**. Accordingly, activation marks active, updates visuals, applies Pending transitions, synchronously publishes OnTowerActivated, then opens the panel. A renderer-hook fixture observes `visual:True:Unreached`, followed by `event:True:Pending:0` before the first panel opening. Repeated activation only reopens the panel.

Assertions verify:

- Unreached→Pending→Warned, pre-warned zones surviving activation, unknown nonblank GEOIDs accepted directly as Warned, trimming/case sensitivity, blank/unknown query defaults and repeated-warning suppression.
- Event observers see committed state; no deferred/batched warning publication and no extra activation, warning or mode events during regeneration.
- Repeated SetCommunicationMode calls each emit one event; Show/Hide do not change mode or emit mode events. Existing visibility rules remain.
- GetAllTowers/property share one live list; regeneration retains that list identity and replaces its tower objects. GetActiveTowers allocates a new list holding the original objects; status dictionaries are independent copies.
- Query, event payload and UI handoff share the same tower object. Failed regeneration preserves the old active object referenced by the panel, while clearing the public tower collection and preserving statuses.

## 8. Compile and test results

| Verification | Before | After |
| --- | --- | --- |
| Relevant default runtime assembly sources | 202 | 204, including both new helpers |
| Runtime C# errors | 0 | 0 |
| Runtime C# warnings | 11 | Same 11 diagnostics; no added warning |
| Managed harness compile errors/warnings | 0 / 0 | 0 / 0 |
| Managed assertions | 215 passed | 215 passed |
| Captured communication states | 73 | All 73 exactly match |
| Entire harness output | Captured | Exactly matches baseline |

Compilation used installed Unity `6000.0.63f1` Roslyn with the project's existing Unity/package references and current runtime source enumeration. This is managed compilation, not a Unity Editor import/build. The unchanged warnings comprise one duplicate using (CS0105), eight obsolete API uses (CS0618), and two unused assigned fields (CS0414) in unrelated existing sources. Warning output order differs; the complete diagnostic sets, including source locations and messages, are identical.

Harnesses execute the captured original/current controller sources and real zone/index/geometry/renderer sources, with controlled Unity, map, shelter and UI stand-ins. They establish managed placement, state, identity, ordering, event counts and requested presentation calls. They do not establish native rendering, Unity coroutine scheduling, deferred destruction, actual JSON loading or UI Toolkit callbacks.

Reproducible evidence is retained at:

`/var/folders/k0/k_vxwym14hs2n16m3n8bt2t40000gn/T/communication-stage-c2-6vfe0neu`

It contains `OriginalController.cs`, `UnityStubs.cs`, `CommunicationTests.cs`, before/after runtime and test response files/logs, `baseline.json`, `compilation.json`, `regression.json` and `preservation.json`. From the repository root, rerun with:

```sh
python3 /var/folders/k0/k_vxwym14hs2n16m3n8bt2t40000gn/T/communication-stage-c2-6vfe0neu/replay.py
```

The script uses the captured original controller for baseline runs and current repository files for after runs. These are temporary local verification artifacts, not a new Unity test assembly or a guaranteed permanent CI installation. No compiled verification outputs were placed under Assets.

## 9. Serialization and scene verification

SHA-256 comparison of all **6,282 starting files** under Assets/Packages/ProjectSettings/UserSettings found only the authorized controller changed. No starting file was deleted. All existing metadata, consumers, C1 audit and both scenes retain their starting bytes.

| Scene | Communication component / GameObject | Inspector fields | Inbound references |
| --- | --- | --- | --- |
| `Assets/Game/Content/Scenes/PlayableTilemap.unity` | `810699005` / `810699003` | All 24 unchanged | `515287497`, `1389987227`, `2019949261`, `2100000004` |
| `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity` | `810699005` / `810699003` | All 24 unchanged | Same four local component IDs |

Each inbound component's existing `communicationTowerController` field still points to `810699005`. The script GUID remains **`98e604977a2f4ef5ac4190852d01e7fa`**. All 24 C# serialized field declarations, names, types and initializers are identical to the original; helpers have no serialized component fields.

Scene SHA-256 values:

- PlayableTilemap: `482a2ccedd674f2b1af5cd095982d0b0a7de1bfb2b4faa7031bc5ee426dc0796`.
- Dev_UINavigation: `25f1d90a301c1c36caabeb4b3687dc888328478bc96334316db2eb7bbf27f42b`.

There is no new public controller, second authority, scene component, prefab binding, assembly, namespace or duplicate shared type. No scene/UI migration was performed.

## 10. Known limitations

Native Unity import and Play Mode remain unverified by this stage; Dev_UINavigation was not run. The existing controller's fallback selection, bounded retry/no re-enable restart, legacy Input use, status reset edge cases, mutable tower objects and stale presentation references remain as audited in C1. Tests use fixtures rather than claiming an observed production-map tower manifest.

## 11. Manual Unity Play Mode checklist

Use **only `Assets/Game/Content/Scenes/PlayableTilemap.unity`**. Keep scene/reference/origin test mutations unsaved; never run Dev_UINavigation.

1. Let Unity import the helpers, confirm no missing script and no additional communication component. Inspect the original component's GUID, references and Inspector values.
2. Cold-start JSON loading; verify one tower set, IDs/served zones, sprites/positions and initial visibility. Compare a captured native tower manifest with the pre-extraction behavior if available.
3. Toggle action_Button6 repeatedly; hover/leave towers; check communication outlines/tooltips and isolation from ordinary zone, shelter, evacuation and risk channels.
4. Activate a tower, reopen it, select Pending zones and send warnings. Check panel tabs/order/populations, repeat-click behavior, summary/advisor updates, event-driven turns and existing sprite states.
5. Exercise cached shelter candidates/placed shelters, missing optional references and late map readiness with unsaved runtime fixtures. Verify failure fallbacks and the bounded retry behavior.
6. Regenerate after activation/warnings, including a failed-provider case; inspect reset/no-event behavior, retained failure statuses, deferred visual destruction and stale panel/hover references against the known baseline.
7. With correctly normalized runtime fixtures, verify `(9,-4)` and `(-20,-10)` origins, absolute shelter/hit cells and world centers. Do not merely move a transform and assume that shifts TileMapData cells.
8. Disable/re-enable the component, then reload the authorized scene. Confirm input/tooltip cleanup, retained state where appropriate, no duplicate subscriptions/callbacks/generation and no unintended external-root residue.

## 12. Stage C3 readiness assessment

C2's managed extraction and serialization checks pass. The single facade, unchanged shared types and state/generator boundaries are ready for C3 review. Visuals, input and presentation remain deliberately concentrated in the existing controller, so C3 can extract them without another gameplay-state migration.

Run the native checklist before treating gameplay verification as complete. C3 should preserve this stage's public/serialized contract and the clarified activation order, without mixing in a facade rename, panel redesign, input-system migration or fixes to existing reset/fallback behavior. Review and commit this complete C2 checkpoint with its new metadata and report.
