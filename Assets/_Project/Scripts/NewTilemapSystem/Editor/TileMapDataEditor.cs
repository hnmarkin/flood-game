using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileMapData))]
public class TileMapDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TileMapData mapData = (TileMapData)target;
        EditorGUILayout.Space(10);

        int numEntries = mapData.CountNonNullTiles();
        EditorGUILayout.LabelField($"Non-null TileInstances: {numEntries}");
    }
}