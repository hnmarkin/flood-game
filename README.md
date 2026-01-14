JSON-Based Tilemap Loading & Flood Simulation

The JsonMapLoader script loads a grid-based tilemap from a JSON file and paints tiles into Unity Tilemaps at runtime.

The JSON contains:
* Grid dimensions (rows / columns)
* Per-cell data:
    Category (ground / road / building / water)
    Population
    Elevation

JSON File Location -> Assets/StreamingAssets/tilemap_with_population_elev.json

Unity loads this file at runtime using the StreamingAssets path.
The script paints all tiles to GroundTilemap 
Elevation can be visulaized using stacked elevation layers but stacking them looks weird. So its optional.

--------------------------------------

Scene Startup Flow

When the scene starts:
1. JsonMapLoader.Awake() is called
2. PaintsFromJson() is executed
3. Tiles are painted onto the Ground Tilemap
4. Internal grid data is stored in TileMapData
This allows other systems (e.g., flood simulation) to query elevation, tile type, and population data.

--------------------------------------

Tilemap Data Structure

The TileManager manages references to all tilemaps used in the scene.
TileManagerRoot
├─ Tile Manager Script
├─ Tilemaps (Size: 3)
│   ├─ Element 0: GroundTilemap
│   ├─ Element 1: RoadTilemap
│   └─ Element 2: BuildingTilemap
├─ Grid Data: TileMapDataTest

TileMapData
TileMapDataTest stores:
    Tile category
    Elevation
    Population
    Flood state
This data is populated during JSON loading and used by the flood simulator.

--------------------------------------


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

WaterSimulator – Inspector Configuration

WaterSimulatorObject
├─ Script: WaterSimulator
├─ Tile Map Data: TileMapDataTest
├─ Water Height: 10
│   (Lower values result in less flooding)
├─ Tile Types:
│   └─ Water Tile Type: WaterTileType
├─ Start On Play: Unchecked

------------------

Sprites used for this map are located within Assets/_Project/Sprites/Tiles
The Buildings are painted manually on OverlayTilemap

Tiles and Sprites used are as follows:
Water Tile Type -> 
    Element 0 
        TileBase: WaterDynamicTile
        Sprite: waterTopFace
        Min: 0
        Max: 0
    Element 1 
        TileBase: WaterDynamicTile
        Sprite: waterTopFace2
        Min: 1
        Max: 2
    Element 2 
        TileBase: WaterDynamicTile
        Sprite: waterTopFace5
        Min: 3
        Max: 1000

Grass Tile Type -> 
    Element 0 
        TileBase: GrassDynamicTile
        Sprite: LightGrassTopFace
        Min: 0
        Max: 1
    Element 1 
        TileBase: GrassDynamicTile
        Sprite: waterTopFace
        Min: 2
        Max: 1000

Road Tile Type -> 
    Element 0 
        TileBase: RoadDynamicTile
        Sprite: HighwayTopFace
        Min: 0
        Max: 1
    Element 1 
        TileBase: RoadDynamicTile
        Sprite: waterTopFace
        Min: 2
        Max: 1000

Building Tile Type -> 
    Element 0 
        TileBase: BuildingDynamicTile
        Sprite: GrassTopFace
        Min: 0
        Max: 1
    Element 1 
        TileBase: BuildingDynamicTile
        Sprite: waterTopFace
        Min: 2
        Max: 1000

------------------