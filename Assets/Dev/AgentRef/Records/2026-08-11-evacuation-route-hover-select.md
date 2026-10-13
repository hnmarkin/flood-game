# 2026-08-11 Evacuation Route Hover Select

Updated the simplified evacuation preview to draw route paths as tile-border outlines instead of center lines. `EvacuationRouteVisualizer` now redraws all routes in preview, hovered, and selected states with separate colors/widths. `EvacuationController` now builds a route-id lookup by tile cell, tracks hovered and selected route ids, toggles selection on click, respects selection limits, and redraws routes when hover/selection changes. No mitigation, city-exit routing, danger coloring, or scoring was added.
