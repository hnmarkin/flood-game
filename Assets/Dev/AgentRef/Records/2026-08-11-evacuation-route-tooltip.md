# 2026-08-11 Evacuation Route Tooltip

Added route hover metadata and a small UI Toolkit tooltip for simplified evacuation routes. `SimpleEvacuationRoute` now stores source population plus destination shelter id/type/capacity. `EvacuationController` fills those fields while building existing shelter routes, shows/hides a tooltip on hover changes, and updates its cursor position while hovering. Added `EvacuationRouteTooltipController`, which creates or reuses `evacuation_route_tooltip` on a HUD UIDocument and keeps it hidden by default.
