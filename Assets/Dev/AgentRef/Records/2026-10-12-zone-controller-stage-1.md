# Zone Controller Stage 1

Extracted a Stage 1 zone gateway into `Assets/Dev/Dev_ZoneSystem`. Added
`ZoneController`, `ZoneIndex`, `ZoneGeometry`, and `ZoneBoundaryEdge` for
legacy GEOID indexing, tile-origin conversion, and deterministic four-neighbor
boundary queries. `FloodDefenseBoxStamp` now resolves a `ZoneController`,
forwards public zone APIs, and uses boundary-edge geometry for legacy barrier
array commits while retaining previews, placement events, and flood-start
compatibility. Added Editor tests for boundary geometry.
