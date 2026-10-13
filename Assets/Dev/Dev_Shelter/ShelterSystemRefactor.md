# Shelter System Refactor

## Purpose
The Shelter system is split into small components.
`ShelterController` is the public interface.
Other systems should communicate through this controller.

## ShelterController
Coordinates the Shelter system.
Exposes public `Get`, `Can`, `Try`, and related operations.
Raises events after successful state changes.
It should not contain UI or rendering logic.

## ShelterState
Stores placed shelters and runtime caches.
It represents the current Shelter state.
It does not handle input or presentation.

## ShelterCandidateResolver
Builds shelter candidates from zones and map data.
Owns grouping, scoring, distance, and tile selection.
It does not place shelters or draw visuals.

## ShelterPlacement
Validates and performs shelter placement.
It changes Shelter state after successful validation.
It does not read player input directly.

## ShelterMetrics
Calculates mitigation and population effects.
It owns the Shelter mitigation formula.

## ShelterPlacementInput
Translates player input into Shelter requests.
It sends requests to `ShelterController`.
It does not decide gameplay validity.

## ShelterPresentation
Controls world-space Shelter visuals.
This includes ghosts, highlights, outlines, labels, and sprites.
It displays controller decisions but does not make them.

## Toolkit UI
Toolkit UI owns UXML buttons, panels, labels, and tooltips.
Toolkit UI sends gameplay requests to `ShelterController`.
Shelter gameplay scripts must not query UXML elements directly.

## Data Flow
`Input → Controller → Logic → State`
`State/Events → Presentation`
`Toolkit UI ↔ Controller`

## Dependencies
Other systems depend on `ShelterController`, not Shelter internals.
Legacy map dependencies remain until the map API migration is complete.