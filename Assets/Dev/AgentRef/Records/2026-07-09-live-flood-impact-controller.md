# 2026-07-09 Live Flood Impact Controller

Added `FloodImpactController` as the live-flood source of truth for zone impact, combining flooded tile coverage, water depth, population, and damage estimates into cached per-zone results that refresh on flood simulation start and step events. Added `FloodImpactOverlayManager` to drive `action_Button4`, render white world-space risk and damage labels, and coordinate with `HighRiskManager` so baseline and live overlays do not display at the same time. Updated scene wiring and split persistent zone outlines into baseline and live-flood channels.
