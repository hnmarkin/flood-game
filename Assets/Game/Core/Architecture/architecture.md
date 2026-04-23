# Flood Game Technical Architecture

## Overview

This document explains the architecture of FloodGame in a way that aims to maximize modularity and minimize duplication. We aim for maximum readability and separation of concerns, so that debugging and future updating are easy.

Practically, this means that our architecture is divided into a couple domains, with cross-interaction happening through specified centralized locations. Also, we aim to be data-driven and avoid hard coding.

## Domains

### Core Systems/Game State

Game State includes core game logic that the other systems interact with, including the state machines that trigger events that other systems should listen to, the modifiers for the map and level, and all resources that the player accumulates.

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

   **Game Flow FSM**  
   States: `Main Menu`, `Campaign Select`, `Loading`, `Gameplay`, `Pause`, `Results`

   **Tool FSM**  
   States: `Normal`, `Placement`, `Inspection`

2. **Modifiers**

    Modifiers is a one-stop shop for effects that modify other actions in the game. These are divided into two scripts: Scenario Modifiers, which broadly affect the map; and Crisis Modifiers, which (naturally) affect certain actions the player can take in the Crisis Phase. 

    Because many modifier changes are multiplicative, we need to track contributions. Thus, we will use three scripts to track and interact with modifiers: `ModifierTracker.cs`, `ModifierResolver.cs`, and `ModifierController.cs`. 
    
    `ModifierTracker.cs` stores the contributions to any modifiers *and their source* (e.g. starting Scenario Modifiers, Preparation Action X, etc.), which is vital if we use multiplicative effects and we want an undo feature. 
    
    `ModifierResolver.cs` is relatively simple and computes the final value from what is in ModifierTracker. We recompute a value whenever a new contribution is made to that value.

    `ModifierController.cs` is the public interface that all other game systems use to read/write Modifier values. Because this is a global gateway, we should have very detailed debugging checks to avoid complicated troubleshooting later.

    The default values at the start of the level should be in ScriptableObjects for each Scenario (e.g. `HurricaneSally.SO`), which should be in Scenario and Content Data. These will be loaded in by a `ScenarioLoader.cs` script.

    *Scenario Modifiers:* `Drainage Eficiency`, `Base Infrastructure Resilience`, `Rainfall Rate`, `Antecedent Wetness`, `External Water Load`, `Wind Stress`, and `Event Pacing`

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
   6. Placeable Defenses (sandbags, barriers, generators)
   7. Emergency Response Personnel

4. **Time Tracker**

> Note: Use C# events, not Unity events - C# event subscriptions are easier to track.

### World Simulation: Tile Map & Water System

### Preparation Actions

    This system includes the Preparation Action base class, the Preparation Actions, and the interface with other systems, `PreparationActionService.cs`.

### Scenario and Content Data

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
