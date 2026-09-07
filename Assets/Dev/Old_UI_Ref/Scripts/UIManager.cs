using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Resources Overlay (Always Used)")]
    [Tooltip("The resources overlay that shows money, action points, etc.")]
    public GameObject resourcesOverlayPrefab;

    [Header("Title Overlay")]
    [Tooltip("The title overlay that shows game title, level name, etc.")]
    public GameObject titleOverlayPrefab;

    [Header("Additional UI Prefabs")]
    [Tooltip("List of additional UI prefabs to instantiate in the canvas")]
    public List<GameObject> uiPrefabs = new List<GameObject>();
    
    [Header("Canvas Assignment (Optional)")]
    [Tooltip("If left empty, will automatically find the first Canvas in the scene")]
    public Canvas targetCanvas;
    
    private GameObject resourcesOverlayInstance;
    private GameObject titleOverlayInstance;
    private List<GameObject> instantiatedUIs = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InstantiateUIPrefabs();
            InstantiateResourcesOverlay();
            InstantiateTitleOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InstantiateResourcesOverlay()
    {
        if (resourcesOverlayPrefab == null)
        {
            Debug.LogWarning("Resources overlay prefab not assigned!");
            return;
        }

        // Find target canvas
        Canvas canvas = GetTargetCanvas();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found for resources overlay!");
            return;
        }

        // Instantiate and parent to canvas
        resourcesOverlayInstance = Instantiate(resourcesOverlayPrefab, canvas.transform);
        DontDestroyOnLoad(resourcesOverlayInstance);
        
        Debug.Log($"Resources overlay instantiated in canvas: {canvas.name}");
    }

    private void InstantiateUIPrefabs()
    {
        if (uiPrefabs == null || uiPrefabs.Count == 0)
        {
            Debug.Log("No additional UI prefabs assigned to UIManager.");
            return;
        }

        // Find target canvas
        Canvas canvas = GetTargetCanvas();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found for additional UI prefabs!");
            return;
        }

        // Instantiate all additional UI prefabs
        foreach (GameObject prefab in uiPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Null prefab found in UI prefabs list, skipping...");
                continue;
            }

            GameObject instance = Instantiate(prefab, canvas.transform);
            DontDestroyOnLoad(instance);
            instantiatedUIs.Add(instance);
            
            Debug.Log($"Additional UI prefab '{prefab.name}' instantiated in canvas: {canvas.name}");
        }
        
        Debug.Log($"UIManager instantiated {instantiatedUIs.Count} additional UI prefabs");
    }

    public void InstantiateTitleOverlay()
    {
        if (titleOverlayPrefab != null)
        {
            Canvas targetCanvas = GetTargetCanvas();
            if (targetCanvas != null)
            {
                titleOverlayInstance = Instantiate(titleOverlayPrefab, targetCanvas.transform);
                DontDestroyOnLoad(titleOverlayInstance);
                Debug.Log("Title overlay instantiated successfully.");
            }
            else
            {
                Debug.LogError("UIManager: No canvas found for title overlay instantiation.");
            }
        }
        else
        {
            Debug.LogWarning("UIManager: Title overlay prefab is not assigned.");
        }
    }

    private Canvas GetTargetCanvas()
    {
        // Use manually assigned canvas if available
        if (targetCanvas != null)
        {
            return targetCanvas;
        }

        // Auto-find canvas in scene
        Canvas foundCanvas = FindObjectOfType<Canvas>();
        if (foundCanvas != null)
        {
            Debug.Log($"Auto-found canvas: {foundCanvas.name}");
            return foundCanvas;
        }

        return null;
    }
}
