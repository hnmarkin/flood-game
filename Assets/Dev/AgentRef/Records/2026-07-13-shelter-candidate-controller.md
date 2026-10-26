# 2026-07-13 Shelter Candidate Controller

Added `ShelterCandidateController` to cache zone-grouped shelter candidates from existing GEOID/tile data, support Inspector-based zone enable/disable overrides, and score candidate tiles by dryness, elevation, nearby support features, and center proximity. Extended `ZoneThinOutlineByHover` with dedicated shelter tile and zone highlight pools, and updated `ActionsPanelController` so `action_Button2` toggles the shelter candidate overlay without duplicate bindings or stacked labels.
