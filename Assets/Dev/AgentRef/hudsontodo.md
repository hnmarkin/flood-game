# Hudson Todo

## [ ] Water System Refactor Prototype

Status: In progress

The water-system refactor prototype is the first implementation priority and remains under `Assets/Dev` while its boundaries are validated. Detailed completion notes will be added here when we return to the prototype.

## [ ] Architecture Sections Still to Be Written

These sections currently have headings in `architecture.md` but no substantive design notes:

### [ ] World Simulation: Tile Map & Water System

### [ ] Audio

### [ ] Input System

### [ ] LLM

### [ ] Save/Load/Meta-Progression

## [ ] Foundational Systems

### [ ] Core Systems / Game State

Game State includes the state machines that trigger events for other systems, modifiers for the map and level, and player resources.

#### [ ] Menu Handler

#### [ ] Scenario Initialization

Scenario initialization loads scenario-specific values and objects, including modifiers, resources, LLM personas, and Preparation Actions. `ScenarioBootstrapper.cs` is the orchestration point and references Scenario & Content Data plus the required initialization scripts.

##### [ ] ScenarioBootstrapper.cs

##### [ ] ModifierInitializer.cs

##### [ ] Remaining initialization scripts

The architecture currently marks the remaining initializer dependency as missing.

### [ ] Parts of Game State

#### [ ] Finite State Machines / Event Buses

State machines should be simple enums with C# events tied to state changes. Use C# events rather than Unity events.

##### [ ] Game Phase FSM

States: `Preparation`, `Crisis`, `Scoring`.

The architecture describes a `Preparation -> Crisis` transition as affecting time passage, UI effects, ambient audio, long-term Preparation Actions, and LLM scoring.

##### [ ] Game Flow FSM

States: `Main Menu`, `Campaign Select`, `Loading`, `Gameplay`, `Pause`.

The architecture identifies World Simulation, Scenario and Content Data, UI, Input System, and Save/Load/Meta-Progression as subscribers or related systems.

##### [ ] Tool FSM

States: `Normal`, `Placement`, `Inspection`.

The Tool FSM primarily interacts with the Input System.

#### [ ] Modifiers

Modifiers provide effects that modify other game actions. Scenario Modifiers broadly affect the map, while Crisis Modifiers affect actions available during the Crisis Phase.

##### [ ] Modifier Access

`ModifierTracker.cs` stores modifier contributions and their sources. `ModifierResolver.cs` computes the final value, including multiplicative effects.

##### [ ] Modifier Interface

`ModifierController.cs` is the public interface used by other systems to read and write modifier values.

##### [ ] Modifier Initialization

`ModifierInitializer.cs` clears modifier values and history, then applies scenario-starting values through the protected initialization contribution type. Scenario defaults are stored in Scenario and Content Data and loaded by `ScenarioLoader.cs`.

Scenario Modifiers: `Drainage Efficiency`, `Base Infrastructure Resilience`, `Rainfall Rate`, `Antecedent Wetness`, `External Water Load`, `Wind Stress`, and `Event Pacing`.

Crisis Modifiers: `Defense Placement Speed`, `Evacuation Speed`, and `Warning Window`.

#### [ ] Resources

Resources are additive values tracked through a tracker/controller pair.

##### [ ] Resource Tracker

`ResourceTracker.cs` stores resource values.

##### [ ] Resource Controller

`ResourceController.cs` is the public interface used by other systems to read and write resources.

Resources: Money, Action Points, Residential Reputation, Corporate Reputation, Political Reputation, Placeable Defenses (Sandbags, Barriers, Pumps, Generators), Emergency Response Personnel, and Communication Level.

#### [ ] Time Tracker

Preparation Phase Time is turn-based and Crisis Phase Time is real-time.

##### [ ] PhaseTime.cs

Defines and tracks PPT and CPT, including their modification rules and time-passage rates.

##### [ ] TimeController.cs

Advances phases, responds to Event Pacing changes, subscribes to the Game Phase FSM, retrieves Event Pacing from Modifiers when entering Crisis Phase, and prevents invalid or repeated phase transitions.

### [ ] World Simulation / Water System

#### [ ] Water System Rework

##### [ ] Water profile and modifier integration

Scenario data must provide persistent baseline, preliminary-time-skip, and Crisis water profiles. On a profile transition, replace the scenario baseline and resolve a fresh water modifier snapshot by reapplying the active, sourced contribution history from `ModifierTracker`; never compound values from an already modified profile. The one optional preliminary time-skip uses the active profile to advance normal water physics. Water must fail production loading when its Scenario/Game State/Modifier contracts are unavailable; Dev defaults require loud diagnostics.

#### [ ] New Water System Testing
