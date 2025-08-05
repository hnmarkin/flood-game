using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TerrainLoader))]
public class TerrainLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainLoader loader = (TerrainLoader)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Loader Actions", EditorStyles.boldLabel);

        // Check if we have the required references
        bool hasTerrainData = loader.GetType().GetField("terrainData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(loader) != null;
        bool hasTilemap = loader.GetType().GetField("sourceTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(loader) != null;

        if (!hasTerrainData)
        {
            EditorGUILayout.HelpBox("No TerrainData assigned. Please assign a TerrainData ScriptableObject.", MessageType.Warning);
        }

        if (!hasTilemap)
        {
            EditorGUILayout.HelpBox("No source tilemap assigned. Please assign a tilemap to load terrain data from.", MessageType.Warning);
        }

        EditorGUI.BeginDisabledGroup(!hasTerrainData || !hasTilemap);
        
        if (GUILayout.Button("Load Terrain from Tilemap"))
        {
            if (loader.LoadTerrainFromTilemap())
            {
                EditorUtility.DisplayDialog("Success", "Terrain data loaded successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Failed", "Failed to load terrain data. Check the console for details.", "OK");
            }
        }

        EditorGUILayout.Space();
        
        // Z-Level specific loading
        EditorGUILayout.LabelField("Z-Level Specific Loading", EditorStyles.boldLabel);
        
        SerializedObject so = new SerializedObject(loader);
        SerializedProperty zLevelProp = so.FindProperty("zLevel") ?? so.FindProperty("targetZLevel");
        
        // Create temporary variables for the Z-level loading UI
        int zLevel = EditorGUILayout.IntField("Z-Level", 0);
        bool useZAsElevation = EditorGUILayout.Toggle("Use Z as Elevation", true);
        
        EditorGUI.BeginDisabledGroup(useZAsElevation);
        int fixedElevation = EditorGUILayout.IntField("Fixed Elevation", 0);
        EditorGUI.EndDisabledGroup();
        
        if (GUILayout.Button($"Load from Z-Level {zLevel}"))
        {
            Tilemap tilemap = loader.GetType().GetField("sourceTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(loader) as Tilemap;
            if (tilemap != null)
            {
                if (loader.LoadTerrainFromTilemapAtZ(tilemap, zLevel, useZAsElevation, fixedElevation))
                {
                    EditorUtility.DisplayDialog("Success", $"Terrain data loaded successfully from Z-level {zLevel}!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Failed", $"Failed to load terrain data from Z-level {zLevel}. Check the console for details.", "OK");
                }
            }
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        
        // Info section
        if (GUILayout.Button("Log Terrain Info"))
        {
            loader.LogTerrainInfo();
        }

        EditorGUILayout.Space();
        
        // Show tilemap bounds if available
        if (hasTilemap)
        {
            Tilemap tilemap = loader.GetType().GetField("sourceTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(loader) as Tilemap;
            if (tilemap != null)
            {
                EditorGUILayout.LabelField("Tilemap Information", EditorStyles.boldLabel);
                var bounds = tilemap.cellBounds;
                EditorGUILayout.LabelField("Tilemap Bounds", bounds.ToString());
                EditorGUILayout.LabelField("Total Potential Cells", (bounds.size.x * bounds.size.y * bounds.size.z).ToString());
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This component loads terrain elevation data from tilemap z-values. Make sure your tilemap has tiles placed at different z-levels to represent elevation.", MessageType.Info);
    }
}
