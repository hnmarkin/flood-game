The script JsonMapLoader loads a grid tilemap from a JSON file (rows/cols + per-cell category/population/elevation).
Paints all tiles to GroundTilemap 
Elevation can be visulaized using stacked elevation layers but stacking them looks weird. So its optional.


When the scene starts, JsonMapLoader.Awake() calls PaintsFRomJson() and paints
tiles + populates TileMapData.
JsonMapLoaderObject -> Inspector
Ground Tilemap: GroundTilemap (Its an isometric z as y tilemap)
Elevation Layers: 0 (optional to add them, but looks like stacked blocks for now)
The JSON file is located in the StreamingAssests Folder
Location : Assets/StreamingAssets/tilemap_with_population_elev.json

Right now the Water Simulator is wired to a FloodButton that starts the flooding

In WaterSimulator an added enum is WaterBodies so water starts seeding from the 
WaterTileTypes

Water Simulator Object -> Inspector 
Script: WaterSimulator
Tile map Data: TileMapDataTest
Water Height: 10 (Lower water height shows lesser flooding)
Tile Types
Water Tile Type: WaterTileType
Start on Play: Unchecked

TileManagerRoot -> Inspector
Tile Manager Script
Tilemaps: 3
Element 0: GroundTilemap
Element 1: RoadTilemap
Element 2: BuildingTilemap

Grid: TileMapDataTest (Tile Map Data)
------------------
Sprites used for this map are located within Assets/_Project/Sprites/Tiles
The Buildings are painted manually on OverlayTilemap