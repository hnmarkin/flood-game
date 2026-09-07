# Water automated testing

Split water tests into dedicated EditMode and PlayMode assemblies with deterministic fixtures and layered coverage for maps, state, configuration, physics, scenarios, lifecycle, projections, and Tilemaps. Fixed renderer tint loss by refreshing tiles before setting their final cell colors. Added categories, tolerances, teardown, a runbook, and a detailed report. Final results: EditMode 57/57 and PlayMode 20/20 passing.
