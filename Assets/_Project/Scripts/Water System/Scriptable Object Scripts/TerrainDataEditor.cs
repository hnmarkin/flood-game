using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TerrainData))]
public class TerrainDataEditor : Editor
{
    private bool showTileDebugInfo = false;
    private Vector2 debugScrollPosition;
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
        EditorGUILayout.TextField("Last Operation", terrainData.LastOperationResult);
        GUI.enabled = true;
        
        // Validation
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Validate Data"))
        {
            bool isValid = false;
            string message = "No TerrainLoader found to validate data";
            
            if (terrainLoader != null)
            {
                isValid = terrainLoader.ValidateData();
                message = isValid ? "All terrain data is valid and ready to use!" : terrainData.LastOperationResult;
            }
            
            string title = isValid ? "Validation Passed" : "Validation Failed";
            EditorUtility.DisplayDialog(title, message, "OK");
        }
        
        // Clear data button
        EditorGUILayout.Space(5);
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear All Data"))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Data", 
                "Are you sure you want to clear all loaded terrain data?", 
                "Yes", "No"))
            {
                if (terrainLoader != null)
                {
                    terrainLoader.ClearData();
                }
                else
                {
                    // Fallback: clear data directly
                    ClearTerrainData(terrainData);
                }
            }
        }
        GUI.backgroundColor = Color.white;
        
        // Debug tile information
        EditorGUILayout.Space(10);
        showTileDebugInfo = EditorGUILayout.Foldout(showTileDebugInfo, "Debug: Tile Information");
        
        if (showTileDebugInfo && terrainData.TilePositions.Count > 0)
        {
            EditorGUILayout.LabelField($"Showing {terrainData.TilePositions.Count} tiles:");
            
            debugScrollPosition = EditorGUILayout.BeginScrollView(debugScrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < Mathf.Min(terrainData.TilePositions.Count, 100); i++) // Limit to first 100 for performance
            {
                Vector2Int pos = terrainData.TilePositions[i];
                int value = i < terrainData.TileValues.Count ? terrainData.TileValues[i] : -1;
                float height = value >= 0 && value < terrainData.TerrainTypesList.Count ? 
                              terrainData.TerrainTypesList[value].height : 0f;
                
                EditorGUILayout.LabelField($"Tile {i}: Pos({pos.x}, {pos.y}) Type({value}) Height({height:F2})");
            }
            
            if (terrainData.TilePositions.Count > 100)
            {
                EditorGUILayout.LabelField($"... and {terrainData.TilePositions.Count - 100} more tiles");
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        // Full status output
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Print Full Status to Console"))
        {
            if (terrainLoader != null)
            {
                string status = terrainLoader.GetDataStatus();
                Debug.Log($"[TerrainData] Full Status:\n{status}");
            }
            else
            {
                Debug.Log($"[TerrainData] No TerrainLoader found to get full status");
            }
        }
        
        // Auto-save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(terrainData);
        }
    }
    
    // Simple fallback methods for when no TerrainLoader is available
    private void ClearTerrainData(TerrainData terrainData)
    {
        terrainData.TilePositions.Clear();
        terrainData.TileValues.Clear();
        terrainData.DataLoaded = false;
        terrainData.TotalTilesWritten = 0;
        terrainData.LastOperationResult = "FAILED: Data cleared manually (Tiles: 0)";
        EditorUtility.SetDirty(terrainData);
    }
}
