# Dev Water Refactor Boundaries

Tightened the Dev Water System around authoritative runtime state: removed public runtime-state exposure, visual persistence/interpolation, tile-name water fallbacks, and reflection-based provider adapters. Added a concrete `Dev_WaterBarrierGrid`, wired it into RefactorScene, and made the test-map bootstrapper a manual repeatable generator that clears its prior layout. Added concise Water System role comments and updated the Dev README. No changes were made under `Assets/Game`; Unity Editor play-mode verification remains required.
