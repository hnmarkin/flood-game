# 2026-07-24 Evacuation Route Visuals

Added `EvacuationRouteVisualizer` to draw evacuation previews as thin tile-border LineRenderer outlines instead of filled sprite overlays. `EvacuationController` now delegates route preview, dangerous segment, missing-road, and selected-route film visuals to the visualizer, clears legacy filled overlay sprites, and keeps scoring/path calculation unchanged.
