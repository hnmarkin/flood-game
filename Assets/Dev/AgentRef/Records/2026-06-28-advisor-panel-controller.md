# 2026-06-28 Advisor Panel Controller

Added `AdvisorPanelController` to subscribe to baseline-risk inspection, read cached risk results from `ZoneBaselineRiskController`, and show a restartable typewriter popup in the existing advisor panel. Extended `HighRiskManager` with a simple inspection event, added a close-button name in the advisor UXML, and introduced additive popup USS classes so the panel starts hidden and animates in without changing its existing visual design.
