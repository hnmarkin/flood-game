# ZoneSystem — Stage 4B

Files: `Assets/Dev/Scripts/Flood/FloodImpactOverlayManager.cs`; this report.

Path: `FloodImpactOverlayManager→ZoneThinOutlineByHover→ZoneController→renderer` becomes `FloodImpactOverlayManager→ZoneController→renderer`.

APIs: `bool SetZoneOutlines(ZoneOutlineChannel channel, IEnumerable<ZoneRiskOutlineRequest> requests)`; `void ClearOutlines(ZoneOutlineChannel channel)`; exclusively `LiveFloodRisk`.

Awake resolution: Inspector → impact dependency's component → local component → unique active controller in this scene. Missing/ambiguous: warn; skip outlines only. Legacy `zoneOutlineController` remains serialized, unused. Existing scenes need no assignments.

Validation: compilation passed (0 errors/12 existing warnings); refresh/subscriptions/labels/percentages/colors/order/mutual exclusion unchanged; 3,741 protected files unchanged; no renderer/pool creation.

Play Mode unexecuted. Required in both scenes: toggles/refresh/cleanup, labels, mutual exclusion, channel isolation, barrier restoration, unavailable-controller diagnostics.

Risks: inactive/late-created controllers; renderer/map readiness; unverified runtime parity.
