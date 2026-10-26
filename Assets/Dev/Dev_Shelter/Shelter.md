# Shelter

## Purpose
The Shelter system lets players place emergency shelters.
Shelters reduce population exposure during a flood.

## Shelter Candidates
Shelter candidates are created from population zones.
Nearby enabled zones are grouped together.
Each group receives one candidate shelter tile.
Dry, high, and central tiles score better.
Roads and buildings can improve tile suitability.
Water and deeply flooded tiles are avoided.

## Placement
Shelters can only use valid candidate tiles.
Placement respects the maximum shelter limit.
A used tile cannot be reused unless explicitly allowed.
Each placed shelter serves its associated zones.

## Shelter Properties
Capacity comes from the selected shelter type.
Safety depends on elevation and water conditions.
Route accessibility currently uses a default value.

## Mitigation
`Mitigation = Capacity Coverage × Shelter Safety × Route Accessibility`

Protected population uses the mitigation score.
Remaining exposure is population minus protected population.
Exposure reduction equals the mitigation score.

## System Access
Other gameplay systems should use `ShelterController`.
They should not access Shelter state or resolvers directly.
Shelter changes are exposed through controller events.

## Presentation
World visuals are handled separately from gameplay logic.
Toolkit UI is also kept outside Shelter gameplay logic.

## Legacy Dependencies
The staged system still uses legacy map data.
Map access can migrate after the new map API is integrated.