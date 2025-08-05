using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NewTerrainData))]
public class NewTerrainDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NewTerrainData terrainData = (NewTerrainData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Terrain Information", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("Status", terrainData.DataLoaded ? "Data Loaded" : "No Data");
        EditorGUILayout.LabelField("Total Tiles", terrainData.TotalTilesWritten.ToString());
        
        if (terrainData.DataLoaded)
        {
            EditorGUILayout.LabelField("Elevation Range", $"[{terrainData.MinElevation}, {terrainData.MaxElevation}]");
            
            var bounds = terrainData.GetTileBounds();
            EditorGUILayout.LabelField("Tile Bounds", $"({bounds.xMin},{bounds.yMin},{bounds.zMin}) to ({bounds.xMax},{bounds.yMax},{bounds.zMax})");
            EditorGUILayout.LabelField("Grid Size", $"{bounds.size.x} x {bounds.size.y} x {bounds.size.z}");
        }
        
        EditorGUILayout.LabelField("Last Operation", terrainData.LastOperationResult);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(!terrainData.DataLoaded);
        if (GUILayout.Button("Validate Data"))
        {
            terrainData.ValidateData();
            EditorUtility.SetDirty(terrainData);
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Clear All Data"))
        {
            if (EditorUtility.DisplayDialog("Clear Terrain Data", 
                "Are you sure you want to clear all terrain data? This action cannot be undone.", 
                "Clear", "Cancel"))
            {
                terrainData.ClearData();
                EditorUtility.SetDirty(terrainData);
            }
        }

        EditorGUILayout.Space();
        
        // Find and show info about associated loaders
        NewTerrainLoader[] loaders = FindObjectsOfType<NewTerrainLoader>();
        if (loaders.Length > 0)
        {
            EditorGUILayout.LabelField("Associated Loaders", EditorStyles.boldLabel);
            foreach (var loader in loaders)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {loader.name}", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = loader;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No NewTerrainLoader components found in the scene. Create a NewTerrainLoader to load terrain data into this ScriptableObject.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This ScriptableObject stores terrain elevation data based on tile z-values. Use a NewTerrainLoader component to populate this data from a tilemap.", MessageType.Info);
    }
}
