# ZoneSystem — Stage 5B

Modified: `Assets/Dev/Scripts/Zones/ZoneThinOutlineByHover.cs` (declaration removal only).

Created under `Assets/Dev/Dev_ZoneSystem/Scripts/ZoneSystem/`: `ZoneRiskOutlineRequest.cs:3`, `TileOutlineRequest.cs:3`, matching metadata; this report.

From: legacy lines 7/19. Both remain byte-identical: global public structs, readonly fields, constructors, no serialization attributes; Cell remains absolute. Assembly-CSharp unchanged; one declaration each.

MonoBehaviour body/APIs/lifecycle byte-identical. GUID preserved; new GUIDs unique. Both scenes and 6,112 existing files unchanged.

Compilation: 202 sources, 0 errors/11 existing warnings; controller/legacy/five consumers included.

Remaining: legacy hover/renderer/visibility ownership, five unused typed fields, stamp dependencies.

Play Mode unexecuted; PlayableTilemap checklist:

- Hover, baseline/live, candidate zones/tiles, placed coverage, communication, barrier hide/restore, disable/re-enable.

No extraction blockers; runtime verification pending.
