# 2026-06-29 HUD Panel Controls And Alerts

Added reusable HUD panel collapse controls for the Actions, Advisor, Alerts, and Mission Checklist panels, plus a dedicated Actions panel controller that routes `action_Button1` into the existing barrier-placement flow. Added an event-driven Alerts controller that reads cached baseline-risk results from `ZoneBaselineRiskController`, refreshes alert text without per-frame work, and lightly polished the Actions buttons with a subtle hover pop-out effect.

Follow-up: made the Alerts collapse binding tolerant of either a collapsed container name or a collapsed button name so the current scene wiring still minimizes correctly, and switched the first alert line from critical-zone count to high-risk-zone count.
