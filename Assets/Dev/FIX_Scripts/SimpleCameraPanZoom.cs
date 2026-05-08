using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class SimpleCameraPanZoom : MonoBehaviour
{
    [Header("Camera Zoom Settings")]
    public float startingOrthoSize = 8f;
    public float zoomStep = 2f;
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 50f;

    [Header("Drag Pan Settings")]
    public bool enableMouseDragPan = true;
    public float dragPanSpeed = 1f;
    public bool ignoreDragWhenPointerOverUI = true;

    [Header("Optional Keyboard Pan")]
    public bool enableKeyboardPan = false;
    public float keyboardPanSpeed = 10f;

    [Header("Map Bounds")]
    public Tilemap targetTilemap;
    public bool useTilemapBounds = true;

    [Header("Manual Bounds If No Tilemap Is Assigned")]
    public Vector2 manualMinBounds = new Vector2(-20f, -20f);
    public Vector2 manualMaxBounds = new Vector2(20f, 20f);

    private Camera cam;
    private Vector3 lastMouseWorldPosition;
    private bool isDragging;

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (!cam.orthographic)
        {
            Debug.LogWarning("SimpleCameraPanZoom works best with an Orthographic camera.");
        }
    }

    void Start()
    {
        ResetCameraToStart();
    }

    void Update()
    {
        HandleMouseDragPan();

        if (enableKeyboardPan)
        {
            HandleKeyboardPan();
        }
    }

    private void HandleMouseDragPan()
    {
        if (!enableMouseDragPan || Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (ignoreDragWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                isDragging = false;
                return;
            }

            isDragging = true;
            lastMouseWorldPosition = GetMouseWorldPosition();
        }

        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            Vector3 currentMouseWorldPosition = GetMouseWorldPosition();

            Vector3 difference = lastMouseWorldPosition - currentMouseWorldPosition;
            difference.z = 0f;

            transform.position += difference * dragPanSpeed;

            ClampCameraToBounds();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }

    private void HandleKeyboardPan()
    {
        if (Keyboard.current == null)
            return;

        float dx = (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed ? 1f : 0f)
                 - (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed ? 1f : 0f);

        float dy = (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed ? 1f : 0f)
                 - (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed ? 1f : 0f);

        Vector3 move = new Vector3(dx, dy, 0f) * keyboardPanSpeed * Time.deltaTime;

        transform.position += move;
        ClampCameraToBounds();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Vector3 screenPosition = new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            Mathf.Abs(cam.transform.position.z)
        );

        return cam.ScreenToWorldPoint(screenPosition);
    }

    public void ZoomIn()
    {
        SetZoom(cam.orthographicSize - zoomStep);
    }

    public void ZoomOut()
    {
        SetZoom(cam.orthographicSize + zoomStep);
    }

    public void SetZoom(float newOrthoSize)
    {
        cam.orthographicSize = Mathf.Clamp(newOrthoSize, minOrthoSize, maxOrthoSize);
        ClampCameraToBounds();
    }

    public void ResetCameraToStart()
    {
        cam.orthographicSize = Mathf.Clamp(startingOrthoSize, minOrthoSize, maxOrthoSize);
        CenterCameraOnMap();
        ClampCameraToBounds();
    }

    public void CenterCameraOnMap()
    {
        Bounds bounds = GetCameraBounds();

        Vector3 position = transform.position;
        position.x = bounds.center.x;
        position.y = bounds.center.y;

        transform.position = position;
    }

    private void ClampCameraToBounds()
    {
        Bounds bounds = GetCameraBounds();

        float cameraHeight = cam.orthographicSize;
        float cameraWidth = cam.orthographicSize * cam.aspect;

        float minX = bounds.min.x + cameraWidth;
        float maxX = bounds.max.x - cameraWidth;
        float minY = bounds.min.y + cameraHeight;
        float maxY = bounds.max.y - cameraHeight;

        Vector3 position = transform.position;

        position.x = ClampOrCenter(position.x, minX, maxX, bounds.center.x);
        position.y = ClampOrCenter(position.y, minY, maxY, bounds.center.y);

        transform.position = position;
    }

    private Bounds GetCameraBounds()
    {
        if (useTilemapBounds && targetTilemap != null)
        {
            Renderer tilemapRenderer = targetTilemap.GetComponent<Renderer>();

            if (tilemapRenderer != null)
            {
                return tilemapRenderer.bounds;
            }
        }

        Vector3 center = (manualMinBounds + manualMaxBounds) / 2f;
        Vector3 size = manualMaxBounds - manualMinBounds;

        return new Bounds(center, size);
    }

    private float ClampOrCenter(float value, float min, float max, float center)
    {
        if (min > max)
        {
            return center;
        }

        return Mathf.Clamp(value, min, max);
    }
}