# Dev Game State

This folder contains the disposable Game State foundation for issue 31. It owns the
authoritative Game Flow, Game Phase, and Tool State values, but not UI, Water physics,
input routing, placement validation, time calculation, LLM behavior, or scene lookup.

## Ownership and lifetime

`Dev_GameStateController` is a scene-independent C# module. The application owner must
retain one instance across menu and scenario scene changes; the controller does not use a
singleton or discover its owner. Its public queries and C# events are the seam for UI and
future gameplay systems.

## Lifecycle contract

Game Flow is `MainMenu -> CampaignSelect -> Loading -> Gameplay`, with `Loading` returning
to `CampaignSelect` when scenario initialization fails and `Gameplay <-> Pause`. Game Phase
is one-way per scenario: `Preparation -> Crisis -> Scoring`. Tool State is `Normal`,
`Placement`, or `Inspection` and may be changed only during active Gameplay.

Pausing, ending a scenario, and every Phase transition clear active-tool identity before
the owning Flow or Phase event is published. Redundant and illegal requests return a failed
result without changing state or publishing a transition event.

## Scenario initialization

The selected scenario is passed directly as `IScenarioConfiguration`; Game State does not
load assets. Configured initializer adapters run in this order:

1. Modifiers
2. Resources
3. Preparation Actions
4. LLM/persona setup
5. Water
6. Risk Overlay

`ScenarioInitialized` is published only after all configured adapters succeed. A failure
tears down attempted adapters in reverse order, returns to Campaign Select, and publishes
`ScenarioInitializationFailed`. `ScenarioEnding` is published before scenario teardown so
future save/meta-progression subscribers can observe live runtime state.

## Water and Time handoff

`IDevGameStateWaterAdapter` is the Game State-side seam for Water. The real adapter will be
added in the final integration phase; Game State never reaches into Water internals. It
receives lifecycle values from the authoritative `GameFlow` and `GamePhase` enums and
provides explicit methods for crisis start, crisis-time advancement, and crisis stop.

Time Tracker issue 33 will call `TryAcknowledgeCrisisStart` after the crisis presentation
and will report duration expiry through `TryReportCrisisDurationElapsed`. Crisis time is
not modeled as a fourth global FSM.
