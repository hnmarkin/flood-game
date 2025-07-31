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
            EditorGUILayout.HelpBox("TerrainLoader found in scene. Use the TerrainLoader component to load terrain data from tilemaps.", MessageType.Info);
            
            if (GUILayout.Button("Select TerrainLoader", GUILayout.Height(30)))
            {
                Selection.activeGameObject = terrainLoader.gameObject;
            }
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
}
