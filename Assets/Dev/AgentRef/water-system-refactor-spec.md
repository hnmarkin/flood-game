# Water System Refactor: Production Lifecycle, Profiles, and Projection Specification

## Problem Statement

The Dev water-system refactor has a working prototype runtime but is not ready to replace the Legacy water path in playable scenarios. It lacks a production lifecycle contract with Game Flow and Game Phase, scenario-owned storm conditions, preparation-phase preliminary flooding, modifier integration, validated loading, and a safe forecast boundary for Preparation Action inspection. The project also needs a clear cutover standard that does not treat Legacy behavior as the product authority.

## Solution

Integrate the refactored Water System through its controller boundary. Scenario and Content Data provides persistent baseline, preliminary-time-skip, and Crisis water profiles. Game State controls Loading, Preparation turns, the one optional preliminary time-skip, Crisis, Pause, and modifier resolution. Water remains the authoritative owner of live water physics, terrain effects, flow, and explicit edge barriers; Projection receives cloned, immutable forecast results and never changes live state.

The playable scene hard-cuts to the refactor only after it meets the documented production-scenario acceptance checks. Legacy water runtime types remain editor/conversion-only.

## User Stories

1. As a player, I want a scenario map to display its configured initial water state when Loading finishes, so that the world is understandable before I act.
2. As a player, I want water to remain physically static during ordinary Preparation turns, so that preparation choices are deliberate rather than hidden real-time pressure.
3. As a player, I want a scenario to optionally advance flooding between preparation turns, so that long-form storm scenarios can create escalating but actionable consequences.
4. As a player, I want preliminary flooding to happen only after the authored global preparation-turn threshold, so that the event is predictable within the scenario's intended pacing.
5. As a player, I want a preliminary flood to occur between turns, so that I see its outcome before choosing the next Preparation Action.
6. As a player, I want only completed Preparation Actions to affect flooding, so that a multi-turn action such as a dam upgrade does not provide benefits early.
7. As a player, I want completed defenses and completed modifier-changing actions to affect the next flood calculation, so that my finished work has visible consequences.
8. As a player, I want the preliminary event to occur only once in a run, so that duplicate event delivery cannot flood the map twice.
9. As a player, I want normal automatic flooding to begin only in Crisis gameplay, so that the phase transition has its intended gameplay meaning.
10. As a player, I want water to pause when the game pauses, so that the simulation respects Game Flow.
11. As a player, I want the storm's conditions to change across scenario stages, so that baseline, preliminary, and Crisis flooding can feel meaningfully different.
12. As a player, I want profile changes to preserve the water already on the map, its motion, and completed defenses, so that a storm transition does not erase physical history.
13. As a player, I want storm escalation to arise from accelerated normal water physics rather than hand-authored surge injections, so that scenario outcomes remain coherent with the simulation.
14. As a player, I want flood-risk inspection to forecast from the actual current water state, so that Preparation Actions can be evaluated against meaningful risk.
15. As a player, I want forecast overlays to remain visually separate from current water, so that I can distinguish present flooding from projected danger.
16. As a player, I want forecasts to use the current storm profile rather than reveal a future gameplay transition, so that the forecast does not grant artificial foreknowledge.
17. As a designer, I want water profiles to live in scenario data, so that storm behavior is authored with the scenario rather than hidden in runtime code.
18. As a designer, I want a single optional preliminary time-skip configuration per scenario, so that the feature is explicit without prematurely building a generic event timeline.
19. As a designer, I want the time-skip configured by simulated duration, so that its meaning remains stable if water timestep values change.
20. As a designer, I want invalid water configuration to fail loudly during production Loading, so that a broken scenario cannot silently run with altered flood behavior.
21. As a developer, I want Modifier contribution history reapplied to a clean profile baseline at transitions, so that undo remains correct and modifiers never compound accidentally.
22. As a developer, I want one controller boundary for each system's external interactions, so that Game State, Water, Projection, UI, and defenses do not gain hidden dependencies.
23. As a developer, I want Dev modifier defaults to be explicit and noisy, so that prototype convenience cannot masquerade as production behavior.
24. As a developer, I want a repeatable cutover checklist, so that the Legacy runtime is removed only after approved scenario behavior is demonstrated.

## Implementation Decisions

- `Dev_WaterRuntimeState` remains authoritative for one live water run and stays private behind `Dev_WaterController`. Legacy water runtime types remain editor conversion inputs only.
- Scenario data provides three persistent water profiles: baseline, preliminary time-skip, and Crisis. A profile supplies complete water settings and source behavior for the period in which it is active.
- Loading validates the required scenario, Game State, and modifier contracts before entering Gameplay. It initializes modifier state, creates runtime water state, applies and renders initial water, and publishes an initialized/inspectable state. It does not advance a physical timestep merely to make inspection available.
- Water automatically steps only while both Game Flow is `Gameplay` and Game Phase is `Crisis`. It pauses for `Pause`, remains non-automatic during Preparation, and freezes for Scoring. Lifecycle transitions must be idempotent.
- A scenario may define one optional preliminary flooding configuration: a global completed-preparation-turn threshold and a simulated duration. Game State owns one `hasAppliedPreliminaryFlooding` runtime boolean, initialized false for a new run and set true before or during the between-turn execution to prevent duplicates.
- At the triggering turn boundary, Game State first commits all completed Preparation Action effects, including completed terrain changes, completed modifier contributions, and any explicit completed edge barriers. It then selects the preliminary profile and runs normal water microsteps for the configured simulated duration. The batch does not advance the preparation-turn counter or action construction progress. The next preparation turn opens only after the resulting water state is available.
- Water time is simulated time. Flow, continuous sources, drainage, spread gating, and every other time-dependent water rule advance from simulated time in both preliminary batches and automatic Crisis stepping, never from Unity wall-clock frames.
- Storm profile transitions persist. Baseline is active after Loading; the preliminary profile becomes active after its time-skip; the Crisis profile replaces it when Crisis begins. Transitions preserve water depths, flow velocities, completed terrain effects, and completed barriers.
- Do not model storm surges as manually injected on-entry water. A surge-like outcome must result from running ordinary physics for the scenario's configured duration and profile.
- On a profile transition, replace the scenario baseline values, preserve active non-baseline modifier contributions with their sources, and resolve a fresh water modifier snapshot. Do not apply modifiers to an already modified profile. Scenario/storm baseline contributions are replaced; expired or undone contributions do not carry forward.
- `ModifierController` is the public source of resolved water modifiers. The water controller receives a resolved snapshot; it does not own modifier history, action undo, or scenario selection. Production simulation refuses to run when this dependency is unavailable. An explicit Dev-only default mode may run with loud startup and per-step diagnostic warnings.
- Water has no knowledge of defense objects or construction stages. A completed tile modifier changes runtime terrain through a validated controller operation. A completed scenario modifier reaches water through the resolved modifier snapshot. `Dev_WaterBarrierGrid` is used only for a completed defense deliberately modeled as an edge-flow barrier with physical properties such as height and seepage.
- `ProjectionController` is the controller boundary for forecasting and outside subscriptions. It reacts to game-time advancement, completed-defense updates, water-affecting modifier changes, and live-water step completion; it coalesces each completed change transaction into a forecast replacement.
- Projection clones current water state and produces immutable forecast output. It must not mutate live water state, persistent map data, scoring history, or the live water renderer. Projection extrapolates only the currently active profile, even when its horizon crosses a scheduled preliminary or Crisis transition.
- Game State controls the variable forecast horizon in game turns and supplies its explicit simulated-water equivalent. Water does not infer what a game turn means.
- The normal water renderer shows current runtime water only. A future dedicated projection overlay renderer consumes forecast results. Hazard classification is deferred behind a mechanically empty, warning-emitting `CalculateHazards` seam; future hazard logic will use projection data and hazard configuration to choose dangerous cells, warning categories, colors, and icons.
- Production validation rejects missing profiles, missing required contracts, non-finite values, invalid timestep/duration values, and other invalid water inputs with actionable Loading errors. Development diagnostics remain prominent rather than silently normalizing broken configurations.
- The production hard cutover removes the Legacy runtime path from normal playable scenes. The refactor must not run alongside Legacy as a second authority in normal play.

## Testing Decisions

- The primary behavioral seam is the public water-controller boundary driven by scenario lifecycle events. Tests and manual checks must not inspect or mutate private runtime arrays directly.
- The secondary integration seam is `ProjectionController`, which verifies forecast invalidation and immutable replacement without coupling the hazard overlay to live water.
- A good test observes player-visible or system-contract behavior: initialized rendering, lifecycle state, exactly-once preliminary flooding, completed-action effects, resulting water state, pause behavior, and projection replacement. It does not assert internal engine loop structure or private storage layout.
- The cutover gate is a documented manual acceptance pass on an approved production scenario. It verifies: Loading validation and initial rendering; no automatic Preparation stepping; exactly-once preliminary flooding; completed-versus-incomplete action behavior; profile persistence; Crisis automatic stepping; Pause behavior; terrain/modifier/barrier effects; renderer fidelity to current state; and separation of projected from live visuals.
- Water unit testing is a separate initiative and is not required by this spec. When added, it should prefer controller-level scenario fixtures and deterministic simulated-time inputs over Legacy-output snapshots.

## Out of Scope

- Multiple preliminary time-skips, stage-local time-skip IDs, or a generic scenario event timeline.
- A fully implemented projection engine, forecast overlay renderer, hazard thresholds, warning icons, or red danger-tile visuals.
- Peak-water scoring history, scoring display behavior, and Save/Load persistence beyond the stated future ownership boundary.
- New Input System integration beyond retaining Dev-only test input until its own contract exists.
- Legacy dual-runtime comparison harnesses or retaining Legacy runtime compatibility in playable scenes.
- In-progress construction physics or water awareness of construction stages.
- Automated water unit-test implementation and stress/performance testing.

## Further Notes

- The refactor intentionally removes Legacy visual persistence/interpolation. If scoring later needs historical peak water, it must use separately recorded data rather than rendering a stale visual maximum.
- The architecture documentation's World Simulation section should be completed after these runtime contracts are implemented.
- The existing Dev `CalculateHazards` method is a deliberate warning-emitting seam only. It records the future boundary; it is not a partial hazard implementation.
