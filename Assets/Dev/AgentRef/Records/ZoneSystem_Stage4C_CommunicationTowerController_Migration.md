# ZoneSystem — Stage 4C

Files: `Assets/Game/Features/MitigationFeatures/Communication/CommunicationTowerController.cs`; this report.

Path: `CommunicationTowerController→ZoneThinOutlineByHover→ZoneController→renderer` becomes `CommunicationTowerController→ZoneController→renderer`.

APIs: `bool SetZoneOutlines(ZoneOutlineChannel channel, IEnumerable<ZoneRiskOutlineRequest> requests)`; `void ClearOutlines(ZoneOutlineChannel channel)`; exclusively `CommunicationZones`.

Awake resolution: Inspector → stamp/local component → unique active controller in this scene. Missing/ambiguous: warn once; skip outlines. Rejection warnings reset after success. Legacy `zoneVisualController` stays serialized, unused.

Stamp dependencies: `MapData`/`TerrainTilemap`, `GetAllZoneTiles`, `NormalizedTileToCell` unchanged.

Validation: compilation passed (0 errors/12 existing warnings); 52 methods, serialized/public APIs, 3,743 protected files unchanged. No new assignments/renderers/pools.

Play Mode unexecuted. Required: both scenes—Button6, tower transitions/activation/statuses/tooltips, channel isolation, mode exit/disable, failure diagnostics.

Risks: inactive/late-created controllers; readiness failures require rehover; runtime parity unverified.
