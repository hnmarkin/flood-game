using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FloodSimData))]
public class FloodSimDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FloodSimData sim = (FloodSimData)target;

        if (GUILayout.Button("Initialize"))
        {
            sim.Initialize();
            EditorUtility.SetDirty(sim);
        }

        if (GUILayout.Button("Step Simulation"))
        {
            sim.StepSimulation();
            EditorUtility.SetDirty(sim);
        }

        if (GUILayout.Button("Print Water Matrix"))
        {
            if (sim.water == null)
            {
                Debug.Log("Water matrix not initialized. Please click 'Initialize' first.");
                return;
            }
            
            for (int y = 0; y < sim.N; y++) {
                string row = "";
                for (int x = 0; x < sim.N; x++)
                    row += sim.water[x, y].ToString("0.00") + " ";
                Debug.Log(row);
            }
        }
    }
}
