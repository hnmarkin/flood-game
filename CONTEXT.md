# Flood Game Domain Glossary

## Game State

The persistent foundation that owns and coordinates the game lifecycle. It publishes authoritative state changes but does not own domain algorithms such as water simulation, placement validation, UI rendering, or input dispatch.

## Game Flow

The lifecycle context of the application: Main Menu, Campaign Select, Loading, Gameplay, or Pause. Pause is valid during any Game Phase.

## Game Phase

The one-way lifecycle of a scenario: Preparation, Crisis, then Scoring. A new scenario begins in Preparation.

## Tool State

The player’s current map-interaction mode: Normal, Placement, or Inspection. Game State owns this state; the Tool subsystem uses it to determine which tool, if any, has exclusive map interaction.

## Active Tool

A tool identified by a stable ID and granted exclusive map interaction by the Tool subsystem. The tool retains responsibility for its domain behavior, including placement validation.

## Scenario Initialization

The Loading operation that establishes scenario runtime state. It succeeds only after every required scenario initializer succeeds, then publishes `ScenarioInitialized`; failure returns Game Flow to Campaign Select.

## Preparation Action Transaction

One player action and its associated reversible resource and modifier changes. Undo reverses the matching changes as one action-level operation.

## Preparation Decision Window

The interval within one Preparation turn when player actions are reversible. Advancing the turn closes the window; actions from earlier turns cannot be undone. Preliminary flooding and the transition from Preparation to Crisis are irreversible.

## Crisis Time

The real-time scenario period represented by a rapidly advancing 24-hour clock. No player action can be undone during Crisis. Exhausting its configured duration begins the transition to Scoring.

## Scenario Time Structure

Scenario-authored values defining the Preparation start date, total Preparation turns, Crisis start time, Crisis duration, and real-seconds-per-game-hour.
