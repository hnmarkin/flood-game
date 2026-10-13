# 2026-08-09 Simple Evacuation Route Preview

Applied the DOCX instructions: `inventory_tool_2` now toggles shelter placement on/off, ShelterManager exposes active-shelter helpers, and evacuation preview defaults to one test route from the highest live-risk or baseline-risk zone. The test route picks nearest active shelter first, nearest city exit fallback, logs route availability counts, and draws one tile-border route layer with danger, missing-road, and selected white overlays disabled.
