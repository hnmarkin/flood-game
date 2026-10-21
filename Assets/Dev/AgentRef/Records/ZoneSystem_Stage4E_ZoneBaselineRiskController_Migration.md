# ZoneSystem — Stage 4E

Files: `Assets/Dev/Scripts/Zones/ZoneBaselineRiskController.cs`; this report.

Path: `ZoneBaselineRiskController→ZoneThinOutlineByHover→ZoneController→renderer` becomes `ZoneBaselineRiskController→ZoneController→renderer`.

APIs: `bool SetZoneOutlines(ZoneOutlineChannel channel, IEnumerable<ZoneRiskOutlineRequest> requests)`; `void ClearOutlines(ZoneOutlineChannel channel)`.

Ownership: both controllers share `BaselineRisk`; either clear removes all baseline outlines. Last show wins. Manager-level baseline/live arbitration unchanged; direct highlighting bypasses it.

Awake resolution: Inspector → stamp/local component → unique active scene controller, once. Missing/ambiguous: warn once.

Legacy `zoneOutlineHighlighter` retained, unused; `FloodDefenseBoxStamp`, map/mitigation dependencies remain.

Validation: 0 errors/12 existing warnings; 41 methods, public/serialized APIs, 3,747 protected files unchanged.

Play Mode pending: Inspect/live, shared clears, colors/labels, startup, barriers, diagnostics.

Risks: unavailable/late controllers; unready renderer/map; existing shared clearing.
