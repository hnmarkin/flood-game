# 2026-05-13 Scoring Scene Mockup

Created `SS-scoring-scene.html`, a self-contained scoring/debrief mockup with three evenly spaced judge AI avatars at the top and report-style panels below for damage, reputation, infrastructure survival, civilian impact, faction reactions, and final city status. Visual direction uses layered pastel-purple surfaces to match a consequence-focused scoring scene. If approved, likely production work would center on a dedicated scoring-screen UXML, a router-managed controller, and event-fed data binding from scoring/results systems.

Refined the scoring mockup into a more game-screen-oriented layout by shrinking the header, flattening the judge cards horizontally, removing descriptive judge body text, and replacing the lower stacked panels with one large tabbed results panel for outcome, metrics, and reactions/status.

Updated the mockup styling again to use `Scoring_Background.png` as the scene backdrop, shift the palette from purple to blue, and round the main panels, cards, tabs, chips, and status elements for a softer UI frame.

Added inline star ratings to the right side of each judge avatar card so each judge now shows a compact score like `2 / 4` inside the same top panel.

Simplified the judge ratings again by removing the numeric text labels and enlarging the star glyphs for clearer emphasis.
