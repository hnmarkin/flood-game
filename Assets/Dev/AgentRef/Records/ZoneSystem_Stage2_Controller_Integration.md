# ZoneSystem — Stage 2 Controller Integration

Added the controller outline gateway while retaining the Stage 1 renderer, legacy APIs, scene component identity and authored settings. Modified `ZoneController.cs`, `ZoneOutlineRenderer.cs` in `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/`, and `Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs`. Created only this report; no new C# helper or Inspector assignment is required. Stage 1 manual validation in both scenes was supplied by the user; Stage 2 Play Mode verification remains pending.

### A. Architecture After Stage 2

Normal requests: `existing consumers → ZoneThinOutlineByHover → ZoneController → ZoneIndex / ZoneGeometry → registered ZoneOutlineRenderer`.
New callers enter directly through `ZoneController`. It retrieves existing `ZoneBoundaryEdge` geometry and converts normalized grid-line endpoints through `NormalizedTileToCell` and `TerrainTilemap.CellToWorld`; X edges extend vertically, Y horizontally, with no simulation padding. Each zone remains separate, including holes, exterior edges and shared seams. A conversion-only ordering step retains legacy tile/down/up/left/right order so existing pool slots keep their creation-time appearance when Inspector values change.

Hover and raw normalized tile-set compatibility still follow `ZoneThinOutlineByHover → existing prepared world segments → same renderer`. There is no controller callback into the adapter and no recursive forwarding.

### B. New Public API

`ZoneOutlineChannel` is public in the existing renderer file: `Hover`, `BaselineRisk`, `LiveFloodRisk`, `ShelterCandidateZones`, `ShelterCandidateTiles`, `PlacedShelterZones`, `CommunicationZones`.

| Exact public member on ZoneController | Contract |
| --- | --- |
| `bool IsOutlineRenderingAvailable { get; }` | Side-effect-free registration/lifetime query; does not initialize the map. |
| `bool SetZoneOutline(ZoneOutlineChannel channel, string geoid, Color color)` | Replace one channel with one zone. Unknown/blank GEOID returns false without clearing it. |
| `bool SetZoneOutlines(ZoneOutlineChannel channel, IEnumerable<ZoneRiskOutlineRequest> requests)` | Replace one channel with individually colored zones. Legacy-compatible skipping of blank/unknown GEOIDs; null/empty batches clear once ready. |
| `bool SetTileOutlines(ZoneOutlineChannel channel, IEnumerable<TileOutlineRequest> requests)` | Replace one channel with four world-space edges per absolute Tilemap cell; null/empty clears once ready. |
| `void ClearOutlines(ZoneOutlineChannel channel)` | Clear only that channel, without needing map readiness. |
| `bool SetOutlineVisibility(ZoneOutlineChannel channel, bool visible)` | Hide/restore cached lines without clearing. Explicit visibility survives replacements, clears and barrier hide/restore; interaction suppression still applies. |

Setters require a registered renderer and ready loader/index; failure leaves channels intact and does not queue requests. Clear/visibility need only registration. Invalid channel values fail with diagnostics. Missing registration logs once until a successful registration; public requests never allocate a renderer or search for scene objects. Existing readiness APIs perform lazy index construction without unnecessary rebuilds.

### C. Compatibility

All 19 legacy public method signatures/overloads, both request structs, all 12 serialized fields/types/defaults, original script GUIDs and authored template/material settings remain. GEOID and candidate-cell methods forward to the gateway and return after success. Clears use the gateway, including both shelter candidate channels together; placed shelter coverage remains independent. Baseline callers still share one pool, live risk remains separate, and consumer-level baseline/live mutual exclusion is untouched.

Every path uses the same helper; no consumer, scene, prefab, UXML or USS was changed. Existing hover and all barrier flag combinations, including the nondefault `(false,true)` quirk, were compared with Stage 1. An explicit per-channel visibility mask defaults to visible and adds no overlay arbitration.

### D. Renderer Ownership

The existing adapter initiates internal registration in Awake; Start provides one bounded retry. Discovery selects a controller in the same scene with the matching terrain source and rejects ambiguity. Update does not perform registration searches. Registration adopts the adapter's existing helper when present, or creates the sole helper under the adapter's transform once; both components retain the same reference. A second live owner or replacement helper is rejected before allocation. Stale destroyed-owner resources are disposed before accepting a replacement.

The adapter owns appearance settings and resource lifetime, pushes settings/suppression instructions at initialization, Update and legacy show calls, unregisters on destruction, then disposes owned lines/fallback material. Assigned assets survive. Disable does not unregister, clear or dispose; GameObject hierarchy visibility remains unchanged. Controller destruction only drops its references, allowing the surviving legacy owner to keep its helper. No static renderer state, new MonoBehaviour or additional rendering root exists.

### E. Remaining Legacy Dependencies

- The adapter still resolves `FloodDefenseBoxStamp`, uses it for mouse-to-zone lookup, raw-set/fallback boundary conversion, initial terrain matching, and `IsZoneBoundaryModeActive`; legacy show guards still require the stamp. These retain hover, startup and failure semantics for Stage 3.
- If a forwarded legacy show cannot use the ready gateway, its original geometry path uses the same helper, preserving legacy early-map behavior, including empty replacement. New public controller calls never use this fallback or query the stamp.
- Existing consumer data/readiness dependencies on the stamp remain unchanged. Map reload invalidation and reissuing persistent requests still require existing lifecycle coordination; no automatic redraw/rebinding policy was added. Hover and suppression updates remain owned by the old component and should move together during Stage 3.

### F. Validation

- Unity 6000.0.63f1 runtime assembly compiled using project references: zero errors, 12 warnings outside the changed files. Legacy consumers compile unchanged.
- Temporary tests passed 1,831 Stage 1/2 rendered-output comparisons and 2,025 assertions with Unity stand-ins and the actual ZoneController/ZoneIndex/ZoneGeometry/ZoneBoundaryEdge sources: seven-channel isolation/reuse, candidate paired clear, all legacy entry points, flag combinations, negative origins, holes/seams/exterior edges, template/material fallbacks, live appearance changes, and disposal.
- Gateway checks covered shared helper identity, duplicate/wrong-owner registration, stale-owner cleanup, scene/map selection, no-renderer failures without allocation, invalid requests, unready loader/index preserving cached lines, single/batch/cell requests, and explicit hide/update/barrier/restore behavior. All 3,807 protected files matched starting hashes, including consumers, scenes, prefabs, original metadata and untouched zone/barrier/water code. No test/build artifacts were added to Assets.
- Unity was not launched for Stage 2 validation; the tests do not establish actual Play Mode visual parity.

Manual Play Mode checks in BOTH Dev_UINavigation and PlayableTilemap:

1. Repeat Stage 1 hover, Inspect Zones/live flood, candidate tiles/zones, placed shelter, tower hover and barrier enter/Escape checks; verify width `0.08`, assigned template/material, Z/sorting, colors and transparency. Candidate clearing must preserve placed coverage; communication clearing must preserve normal hover.
2. Through an attached debugger, call the new gateway with known GEOIDs and absolute cells on separate channels. Confirm existing UI/facade clears affect those same lines. Exercise simultaneous baseline/live channels through the API while preserving existing UI mutual exclusion.
3. Hide one channel with `SetOutlineVisibility(false)`, replace its data, enter/exit barriers, then restore it: only its latest cached lines should return. Clear hidden candidates and verify they remain cleared. Repeat all three legacy flag combinations plus the `(false,true)` quirk; change appearance values during Play and grow/reuse pools to compare creation-time settings.
4. Call before renderer/map readiness: expect false/diagnostics and no new rendering root or lost cached lines. Test duplicate adapter registration without duplicating already-rendered children, component/GameObject disable/re-enable, and owner destruction; verify retained pools on disable, unavailable gateway after destruction, and assigned assets surviving cleanup. Do not save runtime Inspector changes.

### G. Stage 3 Readiness

Stage 2 implementation and automated checks are complete. The gateway is ready to support hover migration and further stamp removal after the manual checks pass. Stage 3 should move hover lookup and visibility-policy ownership deliberately, then retire the documented compatibility geometry/fallbacks; no consumer migration was performed here. Review and commit the validated Stage 2 changes before proceeding.
