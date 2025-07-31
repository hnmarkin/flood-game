using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(FloodSimData))]
public class FloodSimDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FloodSimData data = (FloodSimData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Data Information", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("Grid Dimensions", $"{data.GridWidth} x {data.GridHeight} (includes boundary)");
        EditorGUILayout.LabelField("Simulation Area", $"{data.N} x {data.N}");
        EditorGUILayout.LabelField("Status", data.IsInitialized ? "Initialized" : "Not Initialized");

        if (data.TerrainDataSource != null)
        {
            EditorGUILayout.LabelField("Terrain Source", data.TerrainDataSource.name);
            EditorGUILayout.LabelField("Terrain Data Loaded", data.TerrainDataSource.DataLoaded ? "Yes" : "No");
        }
        else
        {
            EditorGUILayout.LabelField("Terrain Source", "None");
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This ScriptableObject only contains data. Use a FloodSimulationManager component to run the simulation.", MessageType.Info);
    }
}
#endif
