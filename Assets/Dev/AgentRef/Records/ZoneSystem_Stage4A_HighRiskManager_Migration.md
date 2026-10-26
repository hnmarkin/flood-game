# ZoneSystem — Stage 4A

Files: `Assets/Dev/Scripts/Zones/HighRiskManager.cs`; this report.

Path: `HighRiskManager→ZoneThinOutlineByHover→ZoneController→renderer` becomes `HighRiskManager→ZoneController→renderer`.

APIs: `SetZoneOutlines(ZoneOutlineChannel.BaselineRisk, outlineRequests)`; `ClearOutlines(ZoneOutlineChannel.BaselineRisk)`.

Awake resolution: Inspector → baseline dependency's component → local component → unique active controller in this scene. Missing/ambiguous controllers warn; only outlines skip. Legacy `zoneOutlineController` stays serialized, unused. Existing scenes need no assignment when discovery succeeds.

Dependencies: baseline calculations; existing renderer registration.

Validation: compilation passed (0 errors/12 existing warnings); serialized/public APIs, labels/markers/colors/order/mutual exclusion unchanged; 3,739 protected files unchanged.

Play Mode unexecuted. Required in both scenes: toggles, labels/markers, shared-baseline/channel isolation, barrier visibility, missing/unready dependencies.

Risks: inactive/late-created controllers; unverified runtime parity.
