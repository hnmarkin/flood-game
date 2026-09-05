# Dev Water System Production Cutover Checklist

Run this checklist on an approved scenario before removing the Legacy runtime from a playable scene.

- [ ] Production Loading rejects a missing map, scenario profile, modifier provider, non-finite value, or invalid duration with an actionable error.
- [ ] Loading renders the configured initial state without advancing a physical water step.
- [ ] Ordinary Preparation turns do not automatically step water.
- [ ] The authored preliminary threshold runs one normal-physics batch exactly once, between turns, after completed action effects are committed.
- [ ] Incomplete actions do not alter water; completed terrain, modifier, and barrier effects do.
- [ ] Baseline, preliminary, and Crisis profiles persist their live water, flow, and completed barriers across transitions.
- [ ] Every production profile explicitly configures each map edge as a Wall, limitless Source, or limitless Sink; upstream and ocean edges are verified against the scenario design.
- [ ] Automatic water stepping occurs only during Gameplay plus Crisis, and stops during Pause and Scoring.
- [ ] Current renderer visuals match live water depths; projection output remains separate and cannot mutate them.
- [ ] A projection uses the current profile and current water state without disclosing a later stage.
- [ ] The playable scene contains no Legacy `WaterSimulator`, `TileMapData`, `TileManager`, `DynamicTile`, or `TileInstance` runtime dependency.
