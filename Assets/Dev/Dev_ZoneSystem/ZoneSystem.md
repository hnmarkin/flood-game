# **ZoneSystem**

## **ZoneController Refactor**

Stage 1 extracts census-zone lookup and boundary geometry from
`FloodDefenseBoxStamp`.

`ZoneController` is the public gateway for zone data.
It owns initialization, readiness, GEOID lookups, coordinate conversion,
boundary tile queries, and boundary edge queries.
Legacy HashSet-returning methods keep temporary mutable references.

`ZoneIndex` builds the GEOID-to-normalized-tile lookup from
`JsonMapLoader`, `TileMapData`, and the terrain Tilemap origin.
It also supports normalized-tile membership and GEOID lookup.

`ZoneGeometry` calculates four-direction boundary tiles and edges.
It does not draw tiles, place barriers, or touch water simulation.

`ZoneBoundaryEdge` represents one normalized grid-line boundary.
X edges map to future `TrySetBarrierX` calls.
Y edges map to future `TrySetBarrierY` calls.
Legacy padded simulator coordinates are derived, not stored.

`FloodDefenseBoxStamp` remains the compatibility adapter.
It forwards zone queries to `ZoneController` while keeping barrier
placement, previews, outlines, events, and flood-start compatibility.

Add `ZoneController` to the same GameObject as `FloodDefenseBoxStamp`.
Assign the same `TileMapData`, terrain Tilemap, and `JsonMapLoader`.
