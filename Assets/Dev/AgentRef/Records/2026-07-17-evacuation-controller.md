# 2026-07-17 Evacuation Controller

Added `EvacuationController` as the cached route source of truth for shelter and city-exit previews, route selection, scoring, and per-zone evacuation mitigation. Wired `action_Button5` through `ActionsPanelController`, attached the controller in `PlayableTilemap`, and extended baseline/live flood risk refreshes so evacuation lowers exposed and affected population without changing physical flood metrics.
