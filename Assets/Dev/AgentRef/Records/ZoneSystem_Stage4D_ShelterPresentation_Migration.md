# ZoneSystem — Stage 4D

Files: `Assets/Dev/Dev_Shelter/Scripts/ShelterPresentation.cs`; this report.

Path: `ShelterPresentation→ZoneThinOutlineByHover→ZoneController→renderer` becomes `ShelterPresentation→ZoneController→renderer`.

APIs: `bool SetZoneOutlines(ZoneOutlineChannel channel, IEnumerable<ZoneRiskOutlineRequest> requests)`; `bool SetTileOutlines(ZoneOutlineChannel channel, IEnumerable<TileOutlineRequest> requests)`; `void ClearOutlines(ZoneOutlineChannel channel)`.

Channels: `ShelterCandidateZones`/`ShelterCandidateTiles` clear together; `PlacedShelterZones` independently. Absolute `tileCell` preserved.

Resolution: Inspector → component → unique active scene controller; once. Assignment optional in both saved scenes; runtime-unverified.

Legacy `zoneOutlineHighlighter` retained, unused. Stamp access remains in ShelterController.

Validation: 0 errors/12 existing warnings; public/serialized APIs, gameplay, 3,745 protected files unchanged.

Play Mode pending, both scenes:

- Candidates/labels/ghosts/placement.
- Placed coverage survives candidate clearing.
- Barriers, disable/re-enable, channel isolation, diagnostics.

Risks: late/inactive controllers; unready renderer/map.
