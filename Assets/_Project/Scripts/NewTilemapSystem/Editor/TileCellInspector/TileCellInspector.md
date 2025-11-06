# 🧩 Tile Cell Inspector — Usage Guide

The **Tile Cell Inspector** lets you view per-tile data directly from your **TileMapData** ScriptableObject while using the **Tile Palette Select Tool** in the Unity Editor.

---

## ⚙️ Setup

1. Make sure your project has a **TileMapData ScriptableObject** that contains your tile instances and the `Get(x, y, z)` function.  
2. Place the **TileCellInspector.cs** script inside an `Editor/` folder (e.g., `Assets/Editor/TileCellInspector.cs`).  
3. Ensure your **TileMapData** asset is populated with tile data before inspection.

---

## 🔍 Opening the Window

In Unity, open the menu:

**Window ▸ Tile Cell Inspector**

You can dock the window beside the Inspector or leave it floating.

---

## 🗺️ Viewing Tile Info

1. In the **Tile Palette**, switch to the **Select Tool** (rectangle icon).  
2. Click a single tile (1×1 area) on your active Tilemap in the Scene view.  
3. In the **Tile Cell Inspector** window:
   - If not already assigned, drag your **TileMapData** asset into the **Data Asset** field at the top.  
   - The window will now display all stored properties for that tile:
     - Tile Type  
     - X, Y, Z position  
     - Elevation  
     - Water Height  
     - Population  
     - Economic Value  
     - Damage  
     - Casualties  

---

## 💡 Notes

- The inspector updates automatically when your tile selection changes.  
- If you switch tilemaps, simply select a cell on the new map—the window will refresh.  
- To inspect a different data asset, reassign it in the **Data Asset** field.
