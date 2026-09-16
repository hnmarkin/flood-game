#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only input for constructing a MapDef. Runtime code has no map
/// mutation API; authoring tools use this serialized-data boundary instead.
/// </summary>
public readonly struct WaterMapCellAuthoringData
{
    public WaterMapCellAuthoringData(
        bool exists,
        int elevation,
        TerrainTypeDef terrain,
        float initialWaterDepth,
        bool isInitialWaterBody)
    {
        Exists = exists;
        Elevation = elevation;
        Terrain = terrain;
        InitialWaterDepth = initialWaterDepth;
        IsInitialWaterBody = isInitialWaterBody;
    }

    public bool Exists { get; }
    public int Elevation { get; }
    public TerrainTypeDef Terrain { get; }
    public float InitialWaterDepth { get; }
    public bool IsInitialWaterBody { get; }
}

/// <summary>
/// The sole map-writing seam for editor authoring tools. It is excluded from
/// player builds and rejects Play Mode writes.
/// </summary>
public static class WaterMapDefAuthoring
{
    public static bool TryOverwrite(
        MapDef map,
        Vector2Int origin,
        int width,
        int height,
        WaterMapCellAuthoringData[] authoredCells,
        out string error)
    {
        if (Application.isPlaying)
        {
            error = "MapDef authoring is unavailable while Play Mode is active.";
            return false;
        }

        if (map == null)
        {
            error = "MapDef is missing.";
            return false;
        }

        if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
        {
            error = "Map dimensions must be positive and within the supported cell count.";
            return false;
        }

        int expectedCount = width * height;
        if (authoredCells == null || authoredCells.Length != expectedCount)
        {
            error = $"Map authoring requires exactly {expectedCount} cells.";
            return false;
        }

        Undo.RecordObject(map, "Author Map Definition");
        var serializedMap = new SerializedObject(map);
        serializedMap.FindProperty("origin").vector2IntValue = origin;
        serializedMap.FindProperty("width").intValue = width;
        serializedMap.FindProperty("height").intValue = height;

        SerializedProperty cells = serializedMap.FindProperty("cells");
        cells.arraySize = expectedCount;
        for (int i = 0; i < expectedCount; i++)
        {
            WaterMapCellAuthoringData source = authoredCells[i];
            SerializedProperty cell = cells.GetArrayElementAtIndex(i);
            cell.FindPropertyRelative("exists").boolValue = source.Exists;
            cell.FindPropertyRelative("elevation").intValue = source.Elevation;
            cell.FindPropertyRelative("terrain").objectReferenceValue = source.Terrain;
            cell.FindPropertyRelative("initialWaterDepth").floatValue = Mathf.Max(0f, source.InitialWaterDepth);
            cell.FindPropertyRelative("isInitialWaterBody").boolValue = source.IsInitialWaterBody;
        }

        serializedMap.ApplyModifiedProperties();
        EditorUtility.SetDirty(map);
        error = null;
        return true;
    }
}
#endif
