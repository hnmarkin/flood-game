# 2026-06-25 Zone Baseline Risk Controller

Added `ZoneBaselineRiskController` to calculate and cache per-zone baseline flood risk from water distance, elevation, and aggregated population after map load. Extended `ZoneThinOutlineByHover` with persistent outlines for high-risk zone highlighting, and exposed minimal extra zone-index helpers from `FloodDefenseBoxStamp` so the risk controller can reuse the existing GEOID tile mapping instead of rebuilding it.
