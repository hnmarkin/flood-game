# 2026-08-08 Evacuation Route Visual Debug

Added evacuation route diagnostics and tightened visual separation. Controller logs route generation, path-cell counts, route types, reference/sorting status, and sends dangerous cells separately. Visualizer forces preview routes to thin tile-border LineRenderer outlines, disables extra renderers cloned from line prefabs, falls back from broken materials, logs spawned/cleared visuals, and keeps selected white overlay subtle and selection-only.
