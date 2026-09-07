# Water naming cleanup

Renamed the Dev water refactor types to distinguish authored map data, reusable terrain behavior, live water state, physics, rendering, barriers, and scenario definitions. Initial-water-body status now belongs to each `Dev_MapCellDef`, rather than `Dev_TerrainTypeDef`; serialization bridges preserve existing controller, bootstrapper, terrain-renderer, and map-cell assignments. Updated the legacy converter and README to use the new terminology.
