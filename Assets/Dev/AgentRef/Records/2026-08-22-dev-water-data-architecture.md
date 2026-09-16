# Dev Water Data Architecture

- Superseded the previous runtime-compatibility approach.
- Legacy `TileMapData`, `TileInstance`, `TileType`, `DynamicTile`, and `TileManager` data are now conversion inputs only.
- Added new persistent map, cell, terrain-definition, and visual-definition types plus a read-only map accessor.
- Reworked runtime state, simulation, controller, renderer, barriers, and the Dev scene around the new data model.
- Added an editor-only legacy map converter; retained runtime code must not reference legacy water types.
