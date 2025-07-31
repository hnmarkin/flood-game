using UnityEngine;
using System;

[CreateAssetMenu(fileName = "FloodSimData", menuName = "Flood/Flood Simulation Data")]
public class FloodSimDataNew : ScriptableObject
{
    [Header("Simulation Parameters")]
    public int N = 10;
    public float dx = 1f, dy = 1f, dt = 1f;
    public float g = 9.81f;
    public float friction = 0.02f;
    
    [Header("Water Settings")]
    [Range(0f, 1f)]
    public float startingWaterDepth = 0.1f;

    [Header("Terrain Data Source")]
    [SerializeField] private TerrainData terrainDataSource;

    [Header("Runtime Data (Read-Only)")]
    [SerializeField, ReadOnly] private bool isInitialized = false;
    
    // Runtime simulation arrays - these will be managed by the FloodSimulationManager
    [NonSerialized] public float[,] water;
    [NonSerialized] public float[,] terrain;
    [NonSerialized] public float[,] flowX;
    [NonSerialized] public float[,] flowY;

    // Property to access terrain data source
    public TerrainData TerrainDataSource 
    { 
        get => terrainDataSource; 
        set => terrainDataSource = value; 
    }

    public bool IsInitialized 
    { 
        get => isInitialized; 
        set => isInitialized = value; 
    }

    // Grid dimensions calculation
    public int GridWidth => N + 2;
    public int GridHeight => N + 2;

    // Data validation
    private void OnValidate()
    {
        // Ensure positive values
        N = Mathf.Max(1, N);
        dx = Mathf.Max(0.1f, dx);
        dy = Mathf.Max(0.1f, dy);
        dt = Mathf.Max(0.01f, dt);
        g = Mathf.Max(0f, g);
        friction = Mathf.Clamp01(friction);
        startingWaterDepth = Mathf.Clamp01(startingWaterDepth);
    }
}

// Custom attribute for read-only fields in inspector
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif
