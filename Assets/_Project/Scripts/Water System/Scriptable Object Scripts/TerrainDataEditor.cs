using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TerrainData))]
public class TerrainDataEditor : Editor
{
    private TerrainLoader terrainLoader;
    
    public override void OnInspectorGUI()
    {
        TerrainData terrainData = (TerrainData)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tilemap Loading", EditorStyles.boldLabel);
        
        // Find or create terrain loader
        if (terrainLoader == null)
        {
            terrainLoader = FindObjectOfType<TerrainLoader>();
        }
        
        if (terrainLoader == null)
        {
            EditorGUILayout.HelpBox("No TerrainLoader found in scene. Create a GameObject with TerrainLoader component to load terrain data.", MessageType.Info);
            
            if (GUILayout.Button("Create TerrainLoader GameObject"))
            {
                GameObject loaderGO = new GameObject("TerrainLoader");
                terrainLoader = loaderGO.AddComponent<TerrainLoader>();
                // We'll need to set the reference via reflection or a public method
                Selection.activeGameObject = loaderGO;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("TerrainLoader found in scene. You can load terrain data directly from here.", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Terrain Data", GUILayout.Height(30)))
            {
                LoadTerrainData(terrainData);
            }
            
            if (GUILayout.Button("Select TerrainLoader", GUILayout.Height(30)))
            {
                Selection.activeGameObject = terrainLoader.gameObject;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Data Status", EditorStyles.boldLabel);
        
        // Status display
        GUI.enabled = false;
        EditorGUILayout.TextField("Status", terrainData.DataLoaded ? "Loaded" : "Not Loaded");
        EditorGUILayout.IntField("Total Tiles", terrainData.TotalTilesWritten);
        GUI.enabled = true;
        
        // Auto-save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(terrainData);
        }
    }
    
    private void LoadTerrainData(TerrainData terrainData)
    {
        if (terrainLoader == null)
        {
            EditorUtility.DisplayDialog("Error", "No TerrainLoader found in scene. Please create a TerrainLoader GameObject first.", "OK");
            return;
        }
        
        // Set the terrain data reference on the loader if it's not set
        terrainLoader.SetTerrainData(terrainData);
        
        // Attempt to load terrain data
        bool success = terrainLoader.LoadTerrainFromTilemap();
        
        if (success)
        {
            EditorUtility.DisplayDialog("Success", 
                $"Terrain data loaded successfully!\n" +
                $"Tiles processed: {terrainData.TotalTilesWritten}\n" +
                $"Operation result: {terrainData.LastOperationResult}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Failed", 
                "Failed to load terrain data. Check the console for error details.\n" +
                "Make sure the TerrainLoader has a valid source tilemap assigned.", "OK");
        }
        
        // Mark the asset as dirty to save changes
        EditorUtility.SetDirty(terrainData);
    }
}
