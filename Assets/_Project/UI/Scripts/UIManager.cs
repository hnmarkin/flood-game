using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Resource Overlay")]
    public GameObject resourceOverlayPrefab;
    
    [Header("Canvas Assignment (Optional)")]
    [Tooltip("If left empty, will automatically find the first Canvas in the scene")]
    public Canvas targetCanvas;
    
    private GameObject resourceOverlayInstance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InstantiateResourceOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InstantiateResourceOverlay()
    {
        if (resourceOverlayPrefab == null)
        {
            Debug.LogWarning("Resource overlay prefab not assigned!");
            return;
        }

        // Find target canvas
        Canvas canvas = GetTargetCanvas();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found for resource overlay!");
            return;
        }

        // Instantiate and parent to canvas
        resourceOverlayInstance = Instantiate(resourceOverlayPrefab, canvas.transform);
        DontDestroyOnLoad(resourceOverlayInstance);
        
        Debug.Log($"Resource overlay instantiated in canvas: {canvas.name}");
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
