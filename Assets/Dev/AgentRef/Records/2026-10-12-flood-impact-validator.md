# Flood Impact Validator

Added an Editor-only `FloodImpactValidationWindow` at
`Assets/Dev/Dev_Scripts/Editor`. The window samples ZoneController tiles
against WaterController depths, reads existing FloodImpactController results,
and reports PASS/FAIL/UNAVAILABLE checks without starting, resetting, or
mutating simulation/gameplay state.
