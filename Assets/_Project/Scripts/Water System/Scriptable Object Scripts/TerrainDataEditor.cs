using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TerrainData))]
public class TerrainDataEditor : Editor
{
    private Tilemap sourceTilemap;
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
            // Tilemap source field
            sourceTilemap = (Tilemap)EditorGUILayout.ObjectField(
                "Source Tilemap", 
                sourceTilemap, 
                typeof(Tilemap), 
                true
            );
            
            // Load button
            EditorGUI.BeginDisabledGroup(sourceTilemap == null);
            if (GUILayout.Button("Load Terrain from Tilemap", GUILayout.Height(30)))
            {
                bool success = LoadTerrainDataFromTilemap(terrainData, sourceTilemap);
                if (success)
                {
                    EditorUtility.DisplayDialog(
                        "Success", 
                        $"Successfully loaded {terrainData.TotalTilesWritten} tiles from tilemap!", 
                        "OK"
                    );
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Failed", 
                        terrainData.LastOperationResult, 
                        "OK"
                    );
                }
            }
            EditorGUI.EndDisabledGroup();
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
            bool isValid = ValidateTerrainData(terrainData);
            string title = isValid ? "Validation Passed" : "Validation Failed";
            string message = isValid ? "All terrain data is valid and ready to use!" : terrainData.LastOperationResult;
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
                ClearTerrainData(terrainData);
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
                float height = value >= 0 && value < terrainData.TerrainHeights.Count ? 
                              terrainData.TerrainHeights[value] : 0f;
                
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
            string status = GetTerrainDataStatus(terrainData);
            Debug.Log($"[TerrainData] Full Status:\n{status}");
        }
        
        // Auto-save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(terrainData);
        }
    }
    
    // Helper methods that use TerrainLoader functionality
    private bool LoadTerrainDataFromTilemap(TerrainData terrainData, Tilemap tilemap)
    {
        if (terrainLoader != null)
        {
            // Use the TerrainLoader component
            return terrainLoader.LoadTerrainFromTilemap(tilemap);
        }
        else
        {
            // Fallback: create temporary loader
            GameObject tempGO = new GameObject("TempTerrainLoader");
            TerrainLoader tempLoader = tempGO.AddComponent<TerrainLoader>();
            bool result = tempLoader.LoadTerrainFromTilemap(tilemap);
            DestroyImmediate(tempGO);
            return result;
        }
    }
    
    private bool ValidateTerrainData(TerrainData terrainData)
    {
        bool isValid = terrainData.DataLoaded && 
                      terrainData.TilePositions.Count == terrainData.TileValues.Count && 
                      terrainData.TerrainHeights.Count == terrainData.TerrainTypes &&
                      terrainData.TotalTilesWritten > 0;
        
        if (!isValid)
        {
            string reason = "Unknown validation error";
            if (!terrainData.DataLoaded) reason = "No data loaded";
            else if (terrainData.TilePositions.Count != terrainData.TileValues.Count) reason = "Tile position/value count mismatch";
            else if (terrainData.TerrainHeights.Count != terrainData.TerrainTypes) reason = "Terrain heights count doesn't match terrain types";
            else if (terrainData.TotalTilesWritten <= 0) reason = "No tiles were written";
            
            terrainData.DataLoaded = false;
            terrainData.LastOperationResult = $"FAILED: Data validation failed: {reason}";
            EditorUtility.SetDirty(terrainData);
        }
        
        return isValid;
    }
    
    private void ClearTerrainData(TerrainData terrainData)
    {
        terrainData.TilePositions.Clear();
        terrainData.TileValues.Clear();
        terrainData.DataLoaded = false;
        terrainData.TotalTilesWritten = 0;
        terrainData.LastOperationResult = "FAILED: Data cleared manually (Tiles: 0)";
        EditorUtility.SetDirty(terrainData);
    }
    
    private string GetTerrainDataStatus(TerrainData terrainData)
    {
        return $"Data Loaded: {terrainData.DataLoaded}\n" +
               $"Terrain Types: {terrainData.TerrainTypes}\n" +
               $"Total Tiles: {terrainData.TotalTilesWritten}\n" +
               $"Last Operation: {terrainData.LastOperationResult}\n" +
               $"Heights: [{string.Join(", ", terrainData.TerrainHeights)}]";
    }
}
