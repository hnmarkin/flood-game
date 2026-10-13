using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Assign your cursor texture here")]
    [SerializeField] private Texture2D cursorTexture;

    [Header("Hotspot: where the click happens")]
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    [Header("Keep cursor across scenes")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        ApplyCursor();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ApplyCursor();
    }

    private void ApplyCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (cursorTexture == null)
        {
            Debug.LogWarning("CursorManager: No cursor texture assigned.");
            return;
        }

        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}