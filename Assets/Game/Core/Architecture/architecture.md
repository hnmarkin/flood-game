# Flood Game Technical Architecture

## Overview

This document explains the architecture of FloodGame in a way that aims to maximize modularity and minimize duplication. We aim for maximum readability and separation of concerns, so that debugging and future updating are easy.

Practically, this means that our architecture is divided into a couple domains, with cross-interaction happening through specified centralized locations. Also, we want to be data-driven and avoid hard coding.

## Domains

### Core Systems/Game State

Game State includes core game logic that the other systems interact with, including the state machines that trigger events that other systems should listen to, the modifiers for the map and level, and all resources that the player accumulates.

#### Menu Handler

#### Scenario Initialization

Scenario initialization loads relevant scenario-specific values and objects, including modifiers, resources, LLM personas, and Preparation Actions. All of this is done by `ScenarioBootstrapper.cs`.

`ScenarioBootstrapper.cs` will reference the objects in Scenario & Content Data, as well as all necessary initialization scripts (e.g. `ModifierInitializer.cs`). The list is:

1. `ModifierInitializer.cs`
2. (MISSING)

#### Parts of Game State

1. **Finite State Machines (FSMs) / Event Buses**

   The State Machines should be simple enums with C# events tied to state changes. Game Phase covers the conceptual phases of the game (`Preparation`, `Crisis`, `Scoring`), while Game Flow covers the logical states of the game. The Tool State Machine is for tracking what mode the player is in and interacts primarily with the Input System.

   **Game Phase FSM**  
   States: `Preparation`, `Crisis`, `Scoring`

   Example: a `Preparation -> Crisis` state change should change how time passes (from turn-based to real-time), change the visual UI effects to be dark and rainy, change audio to ambient rain, and disable long-term Preparation Actions.

   Systems that should subscribe:
   1. Time Tracker
   2. UI (particles / overlay effects)
   3. Audio (ambient sounds)
   4. Preparation Actions (long-term availability toggle)
   5. LLM (for scoring)

   **Game Flow FSM**  
   States: `Main Menu`, `Campaign Select`, `Loading`, `Gameplay`, `Pause`

   Systems that should subscribe:
   1. World Simulation (check for Gameplay/Pause/Loading)
   2. Scenario and Content Data (for Campaign Select/Loading)
   3. UI (Pause/Loading)
   4. Input System?
   5. Save/Load/Meta-Progression

   **Tool FSM**  
   States: `Normal`, `Placement`, `Inspection`

2. **Modifiers**

    Modifiers is a one-stop shop for effects that modify other actions in the game. These are divided into two scripts: Scenario Modifiers, which broadly affect the map; and Crisis Modifiers, which (naturally) affect certain actions the player can take in the Crisis Phase. 

    Because many modifier changes are multiplicative, we need to track contributions. Thus, we will use three scripts to track and interact with modifiers: `ModifierTracker.cs`, `ModifierResolver.cs`, and `ModifierController.cs`. Additionally, `ModifierInitializer.cs` sets modifier values at the beginning of a scenario.

    `ModifierInitializer.cs` sets initial values and is called by `ScenarioBootstrapper.cs`. It must clear all modifer values and history, and then add the values to `ModifierTracker.cs` using a special contribution type that can only be overwritten/erased through `ModifierInitializer.cs`.
    
    `ModifierTracker.cs` stores the contributions to any modifiers *and their source* (e.g. starting Scenario Modifiers, Preparation Action X, etc.), which is vital if we use multiplicative effects and we want an undo feature. 
    
    `ModifierResolver.cs` is relatively simple and computes the final value from what is in ModifierTracker. We recompute a value whenever a new contribution is made to that value.

    `ModifierController.cs` is the public interface that all other game systems use to read/write Modifier values. Because this is a global gateway, we should have very detailed debugging checks to avoid complicated troubleshooting later.

    The default values at the start of the level should be in ScriptableObjects for each Scenario (e.g. `HurricaneSally.SO`), which should be in Scenario and Content Data. These will be loaded in by a `ScenarioLoader.cs` script.

    *Scenario Modifiers:* `Drainage Efficiency`, `Base Infrastructure Resilience`, `Rainfall Rate`, `Antecedent Wetness`, `External Water Load`, `Wind Stress`, and `Event Pacing`

    *Crisis Modifiers:* `Defense Placement Speed`, `Evacuation Speed`, `Warning Window`

3. **Resources**

    Resources tracks the "currencies" of the game. Because these are additive and not multiplicative, we will use a simple two-script system to track and read/write.

    `ResourceTracker.cs` stores the values of each resource.

    `ResourceController.cs` is the public interface that other game systems use to interact. As it will be accessed by many game systems, it needs detailed debugging checks to avoid complicated bugs later on.

    *Resources:*
   1. Money
   2. Action Points
   3. Residential Reputation
   4. Corporate Reputation
   5. Political Reputation
   6. Placeable Defenses
        a. Sandbags
        b. Barriers
        c. Pumps
        d. Generators
   7. Emergency Response Personnel
   8. Communication Level

4. **Time Tracker**
    
    Time involves two simple systems--Preparation Phase Time (PPT) and Crisis Phase Time (CPT), which are turn-based and real-time, respectively. The time tracker involves two scripts: `PhaseTime.cs` and `TimeController.cs`.
    
    `PhaseTime.cs` defines PPT and CPT and tracks them, as well as defining their modification rules and time passage rates. `TimeController.cs` calls a PhaseAdvance() function when the game phase advances (e.g. Preparation -> Crisis), and also calls a modifier function whenever Event Pacing is changed. Time passage rate modifiers are multiplicative and thus all recorded, much like the Modifier system.

    `TimeController.cs` interfaces with other game systems, including the important subscription to the Game Phase FSM event bus. Also, Event Pacing must be retrieved from Modifiers when the phase is changed to Crisis Phase. All interaction with the time system passes through here. The other role of `TimeController.cs` is error checks--ensuring that every phase has a timer and preventing repeated phase transitions.

> Note: Use C# events, not Unity events - C# event subscriptions are easier to track.

### World Simulation: Tile Map & Water System
#### Water System
The refactored Water System is large and includes the terrain system. Some parts still await other system implementations--Game State, Modifiers, etc. The existing work (ignoring testing, which is a self-contained system and unimportant to high-level architecture) can be divided into Data definitions and Runtime scripts/controllers.
##### Data
The new data types are listed below. For a general overview, `MapCellDef` defines the atomic objects of the map, `TerrainTypeDef` defines general types that MapCells can be, and `MapDef` defines the grid that is the actual map.

`WaterState` handles the live runtime state of the water system, and `RendererDef` defines the visual variants of terrain tiles. `RendererDef` is a ScriptableObject, and `WaterState `is (primarily supposed to be) instantiated at the beginning of Play.

**Breakdown**
1. `MapDef` is a ScriptableObject definition with settings for the grid dimensions. It has error checks for cell interactions. It is immutable during runtime and can only be accessed through `MapAccessor`.
2. `MapCellDef` is the cell definition, including elevation, terrain type, initial water depth, initial-water-body-status, and everything except coordinates.
3. `TerrainTypeDef` is the Terrain Type definition, allowing for forests, plains, beaches, infrastructure, etc. The main attributes are a drainage multiplier (affects the physics), a RendererDef (affects the tile visual), and the `ParticipatesInSimulation` flag.
4. `RendererDef` is a ScriptableObject definition that configures base terrain visuals and waterlogged tile variants based on current water depth.
5. `MapAccessor` is a read-only layer on MapDef.
6. `WaterState` is the definition of the volatile object that holds logical water information during Play time. The main data used by the water-pipes simulation system in `WaterPhysics` is stored here. 
7. `ScenarioDef` is a ScriptableObject definition for scenario settings, with placements of water sources, map edge boundaries, and the optional preliminary flood settings. There are three profiles total, including the Preliminary Prep Phase, Baseline Profile, and Crisis Profile. Ideally, all scenarios will be individual ScriptableObjects.
8. `WaterTypes` is a collection of data types that are used by the runtime scripts, including:
	1. `enums` for readability: `WaterGameFlow`, `WaterGamePhase`, `WaterProfileStage`, `WaterConfigurationMode`, `WaterBoundaryMode`, `WaterBoundarySide`, `WaterSourceKind`, `WaterStepMode`
	2. `class WaterSimulationSettings` and `class WaterBoundarySettings`: `WaterSimulationSettings` subsumes `WaterBoundarySettings `and is passed as an argument by `WaterController` for`WaterPhysics.Initialize`.
	3. `class WaterSourceSpec`: defines water source tiles and how modifiers can scale them.
	4. `class WaterStormProfile`: contains simulations settings and handles water source tiles.
	5. `class WaterPreliminaryFloodingConfig`: the data type for the Prep Phase "Preliminary Flooding" settings. Optionally instantiated by `ScenarioDef`.
	6. `struct WaterModifierSnapshot`: temporary placeholder until real modifier system is implemented.
	7. `struct WaterStepSummary`: used to group the results of water steps for logging/debugging/stats.
	8. `class WaterProjection`: read-only data type used by ProjectionController (and potentially other scripts) for flood forecasting.

##### Runtime
The runtime behavior of the Water System follows the convention, with `WaterController` handling all interactions with outside systems (unit testing not included). `WaterPhysics` handles all logical water math (water height, flow, etc.), and `WaterPhysicsBarrier` supplements it with handling of player barriers.

1. `WaterRenderer` is a renderer for the RendererDef data type, translating map tiles to the correct variant based on water depth info from `WaterState` (shallow or deep water for water tiles, waterlogged variants for land tiles).
2. `WaterController` is the interface that handles all connections to outside systems. It handles initialization, interactions and their error checks, and provides two C# events to facilitate interactions:
	1. `OnWaterSimulationReset` is triggered every time there is an all-encompassing reloading. The only current instance of this is scenario initialization.
	2. `OnWaterSimulationStepped` is simply triggered every step of the water simulation. As such, it is triggered mainly in the Crisis Phase, with the additional steps of Prep Phase time-skip preliminary flooding.
3. `WaterPhysics` runs the "water pipes" simulation. For the most part, a direct adaptation of [Simulating water over terrain | lisyarus blog](https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html). It reads/writes to the `WaterState` data type.
4. `WaterPhysicsBarrier` manages player-created barriers.
5. `ProjectionController` manages the forward logical simulation for the flood forecasting overlay, though it does not render. It outputs a `WaterProjection` data type.
6. `WaterLifecycleCoordinator` is a wrapper around `WaterController` to connect it to the future Game State system.

**Legacy Conversion/Dev Tools**
`WaterLegacyMapConverter` converts legacy maps and their data types into the new data types.
`WaterRefactorSceneBootstrapper` is for deterministaclly regenerating the `RefactorScene` map asset and Tilemap preview for developer testing.

### Preparation Actions
The system for Preparation Actions includes six components, the base class, individual card definitions, a runtime card state, a loader script, a communication failure resolver, and the interface with other game systems.

1. **Base ScriptableObject Definition - `PrepActionDef.cs`**

    The base class definition defines the common variables for a Preparation Action.

    *Variables:*
    1. Card Name
    2. Card Type
    3. Residential Reputation Cost
    4. Corporate Reputation Cost
    5. Political Reputation Cost
    6. Money Cost
    7. Action Point Cost
    8. Turns
    9. Prerequisites
    10. Stackability
    11. Effects (on modifiers)
    12. Comms Failure Type
    13. Text Description/Tooltip

2. **Individual Preparation Action Definitions**

    Most individual Preparation Actions are simple ScriptableObjects with the proper variables. However, some cards may be more complex. To accomodate these, we will simply create a subclass of the `PrepActionDef.cs` base class with the functionality we need, and then create subclass ScriptableObjects.

3. **Runtime Preparation Action State - `PrepActionInstance.cs`**

    This script handles the status of cards during play and is modified by `PrepActionService.cs`. The variables include:

    1. Status - `Locked`, `Available`, `In-Progress`, `Completed`
    1. Turns Remaining (null if not being enacted)


4. **Preparation Action Loader Script - `PrepActionLoader.cs`**

    The Preparaction Action loader takes a Preparation Action Configuration ScriptableObject from the Scenario and Content Data section and maintains a list of enabled/disabled Preparation Actions for the current level. This should be exported to a file in Save/Load/Meta-Progression.

5. **Communication Failure Resolver - `CommsFailResolver.cs`**

    Because communication failure varies widely, it is handled by a separate script. This script both defines the Comms Failure types that `PrepActionDef.cs` references and is used by `PrepActionService.cs` to modify card values (depending on failure type) before sending changes to other systems.

6. **Interface With Other Systems - `PrepActionService.cs`**

    The outside interface, as usual, primarily consists of error checking--if prerequisites/resources are met, if stackability has been exceeded, and if it clashes with a mutually exclusive card already in play (many of these reference `PrepActionInstance.cs`). `PrepActionService.cs` additionally ensures that `CommsFailResolver.cs` checks for communication failure based on current Communication Level.

### Scenario and Content Data

This section is where the loadable data for scenarios is stored. Tilemaps, starting resource configurations, LLM personas, Preparation Action libraries, etc. We will have a `ScenarioConfig.cs` that defines a Scenario ScriptableObject, although it will be complemented by a few other files. It is likely best to have folders for each scenario. Each Scenario Config will have these:

1. Phase Structure (mainly if the level is multi-phased)
2. Starting Modifiers & Resources
3. Available Preparation Actions

Each scenario folder should contain these files:

1. Scenario Config
2. Tilemap
3. LLM Personas

>Remember: this section does not write to other systems!

### UI

This section is left to Ashley. Some rules to follow:
1. The UI should be modular, with separate UXML docs and controller scripts for each major screen or overlay.
2. **UI should not contain game logic**. It should subscribe to C# events and display data from the underlying systems, while separate interface/controller scripts handle communication with gameplay systems when needed.
3. A high-level UI manager/router should control screen and overlay visibility, while individual UI controllers should only manage their own panel.
4. UI transitions should be driven by the Finite State Machines in `Core Systems`, rather than hard-coded per-screen logic.

### Audio

### Input System

The input system includes all player input, including mouse/keyboard input itself, map clicks, drag placement, hovers, selection, mode switching, and tool activation.

We are using the New Input System, not the legacy InputManager

### LLM

### Save/Load/Meta-Progression
