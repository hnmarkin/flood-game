#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor entry points for rebuilding the deterministic RefactorScene map asset.</summary>
public static class WaterRefactorSceneAuthoring
{
    private const string RefactorScenePath = "Assets/Dev/WaterSystemRefactor/RefactorScene.unity";

    [MenuItem("Dev/Water System/Rebuild Refactor Scene Map")]
    public static void RebuildRefactorSceneMap()
    {
        EditorSceneManager.OpenScene(RefactorScenePath, OpenSceneMode.Single);
        WaterRefactorSceneBootstrapper bootstrapper = Object.FindFirstObjectByType<WaterRefactorSceneBootstrapper>();
        if (bootstrapper == null)
        {
            Debug.LogError("[WaterRefactorSceneAuthoring] RefactorScene has no WaterRefactorSceneBootstrapper.");
            return;
        }

        bootstrapper.RebuildScene();
        AssetDatabase.SaveAssets();
    }

    public static void RebuildRefactorSceneMapFromCommandLine()
    {
        RebuildRefactorSceneMap();
        EditorApplication.Exit(0);
    }
}
#endif
