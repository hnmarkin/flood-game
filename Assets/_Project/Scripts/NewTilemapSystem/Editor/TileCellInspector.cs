// Assets/Editor/TileCellInspector.cs
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCellInspector : EditorWindow
{
    // Hook up your data source here.
    // Replace this with however you store/lookup TileInstance (array, dict, ScriptableObject, etc.).
    public TileMapData data; // your ScriptableObject that contains TileInstances and dimensions

    [MenuItem("Window/Tile Cell Inspector")]
    public static void ShowWindow() => GetWindow<TileCellInspector>("Tile Cell Inspector");

    void OnEnable()
    {
        EditorApplication.update += Repaint; // keep UI fresh as selection changes
    }
    void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    void OnGUI()
    {
        // Pick your data asset
        data = (TileMapData)EditorGUILayout.ObjectField("Data Asset", data, typeof(TileMapData), false);

        // Find the active Tilemap the palette is targeting
        var targetGO = GridPaintingState.scenePaintTarget;
        var tilemap = targetGO ? targetGO.GetComponent<Tilemap>() : null;
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Active Tilemap", tilemap, typeof(Tilemap), true);
        EditorGUI.EndDisabledGroup();

        if (!tilemap)
        {
            EditorGUILayout.HelpBox("Open the Tile Palette and select a Tilemap with the Select tool.", MessageType.Info);
            return;
        }

        // Read current grid selection (from palette Select tool)
        var bounds = GridSelection.position; // BoundsInt in grid coords
        bool singleCellSelected = GridSelection.active && bounds.size == Vector3Int.one;

        if (!singleCellSelected)
        {
            EditorGUILayout.HelpBox("Select a single cell (1×1) with the Tile Palette’s Select tool.", MessageType.Info);
            return;
        }

        Vector3Int cell = bounds.position; // grid coords
        EditorGUILayout.LabelField("Selected Cell", $"({cell.x}, {cell.y})");

        // Lookup your TileInstance by (x,y)
        TileInstance ti = data ? data.Get(cell.x, cell.y, cell.z) : null;

        if (ti == null)
        {
            EditorGUILayout.HelpBox("No TileInstance found for this cell.", MessageType.Warning);
            return;
        }

        // Display fields (read-only)
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.LabelField("Tile Type", ti.tileType.tileName);
            EditorGUILayout.IntField("x", ti.x);
            EditorGUILayout.IntField("y", ti.y);
            EditorGUILayout.IntField("Elevation", ti.elevation);
            EditorGUILayout.FloatField("Water Height", ti.waterHeight);
            EditorGUILayout.IntField("Population", ti.population);
            EditorGUILayout.IntField("Economic Value", ti.econVal);
            EditorGUILayout.IntField("Damage", ti.damage);
            EditorGUILayout.IntField("Casualties", ti.casualties);
        }
    }
}
