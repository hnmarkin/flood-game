# Water automated testing

Split water tests into dedicated EditMode and PlayMode assemblies. Added deterministic fixtures and layered coverage for maps, state, configuration, physics, scenarios, lifecycle, projections, and temporary Tilemaps. EditMode passed 57/57; PlayMode passed 18/20. Two retained failures show `RefreshTile` resetting resolved cell tint to white. Added categories, tolerances, teardown, a runbook, and a detailed report. Runtime water APIs and behavior were not changed.
