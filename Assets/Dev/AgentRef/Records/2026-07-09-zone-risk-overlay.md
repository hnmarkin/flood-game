# 2026-07-09 Zone Risk Overlay

Expanded Inspect Zones so `action_Button3` now reuses cached baseline-risk results to show all zone outlines by risk tier and spawn lightweight world-space percentage labels without recalculating risk. Extended `ZoneBaselineRiskController` with cached zone centers and per-level colors, upgraded `ZoneThinOutlineByHover` to support mixed-color persistent outlines, and updated `HighRiskManager` to refresh or toggle a single overlay lifecycle without duplicating click handlers.
