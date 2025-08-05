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

        if (data.NewTerrainDataSource != null)
        {
            EditorGUILayout.LabelField("New Terrain Source", data.NewTerrainDataSource.name);
            EditorGUILayout.LabelField("New Terrain Data Loaded", data.NewTerrainDataSource.DataLoaded ? "Yes" : "No");
            if (data.NewTerrainDataSource.DataLoaded)
            {
                EditorGUILayout.LabelField("Elevation Range", $"[{data.NewTerrainDataSource.MinElevation}, {data.NewTerrainDataSource.MaxElevation}]");
                EditorGUILayout.LabelField("Tiles Loaded", data.NewTerrainDataSource.TotalTilesWritten.ToString());
            }
        }
        else if (data.TerrainDataSource != null)
        {
            EditorGUILayout.LabelField("Old Terrain Source", data.TerrainDataSource.name);
            EditorGUILayout.LabelField("Old Terrain Data Loaded", data.TerrainDataSource.DataLoaded ? "Yes" : "No");
        }
        else
        {
            EditorGUILayout.LabelField("Terrain Source", "None");
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This ScriptableObject only contains data. Use a FloodSimulationManager component to run the simulation.", MessageType.Info);
        
        if (data.NewTerrainDataSource != null && data.TerrainDataSource != null)
        {
            EditorGUILayout.HelpBox("Both terrain data sources are assigned. NewTerrainData (z-value based) will take priority over the old TerrainData (tile type based).", MessageType.Info);
        }
    }
}
#endif
