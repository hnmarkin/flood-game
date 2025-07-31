using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(FloodSimulationManager))]
public class FloodSimulationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FloodSimulationManager manager = (FloodSimulationManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Simulation Controls", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Simulation controls are only available in Play Mode.", MessageType.Info);
            return;
        }

        // Status display
        EditorGUILayout.LabelField("Status", manager.IsInitialized ? "Initialized" : "Not Initialized");

        EditorGUILayout.Space();

        // Control buttons
        EditorGUI.BeginDisabledGroup(!manager.IsInitialized);
        
        if (GUILayout.Button("Step Simulation"))
        {
            manager.StepSimulation();
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Auto Step"))
        {
            manager.StartAutoStepping();
        }
        if (GUILayout.Button("Stop Auto Step"))
        {
            manager.StopAutoStepping();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset Simulation"))
        {
            manager.ResetSimulation();
        }

        if (GUILayout.Button("Print Water Matrix"))
        {
            manager.PrintWaterMatrix();
        }

        // Auto-repaint the inspector during play mode to update the status
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
#endif
