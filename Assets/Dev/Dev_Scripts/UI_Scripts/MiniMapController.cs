using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MiniMapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private SimpleCameraPanZoom cameraPanZoom;
    [SerializeField] private Camera targetCamera;

    [Header("Minimap Image")]
    [SerializeField] private Sprite minimapSprite;
    [SerializeField] private Texture2D minimapTexture;

    [Header("Map World Bounds")]
    [SerializeField] private Vector2 mapWorldMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 mapWorldMax = new Vector2(20f, 20f);

    [Header("Overlay Sizes (Normalized 0-1)")]
    [SerializeField] private Vector2 hoverRectSizeNormalized = new Vector2(0.18f, 0.18f);
    [SerializeField] private Vector2 minimumCurrentRectSizeNormalized = new Vector2(0.08f, 0.08f);

    [Header("Optional Button Names")]
    [SerializeField] private string zoomInButtonName = "";
    [SerializeField] private string zoomOutButtonName = "";
    [SerializeField] private string resetButtonName = "";

    private static readonly Vector2 ClickMarkerSizePixels = new Vector2(10f, 10f);

    private VisualElement _miniMapViewport;
    private VisualElement _miniMapImage;
    private VisualElement _currentCameraRect;
    private VisualElement _hoverSelectionRect;
    private VisualElement _clickMarker;

    private Button _zoomInButton;
    private Button _zoomOutButton;
    private Button _resetButton;

    private bool _isBound;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (cameraPanZoom == null)
            cameraPanZoom = FindObjectOfType<SimpleCameraPanZoom>();

        if (targetCamera == null && cameraPanZoom != null)
            targetCamera = cameraPanZoom.GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        BindUI();
        ApplyMinimapImage();
        RegisterCallbacks();
        HideOptionalOverlays();
        UpdateCurrentCameraRect();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void LateUpdate()
    {
        UpdateCurrentCameraRect();
    }

    private void BindUI()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[MiniMapController] UIDocument is not assigned.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("[MiniMapController] UIDocument rootVisualElement is null.");
            return;
        }

        _miniMapViewport = root.Q<VisualElement>("MiniMapViewport");
        _miniMapImage = root.Q<VisualElement>("MiniMapImage");
        _currentCameraRect = root.Q<VisualElement>("CurrentCameraRect");
        _hoverSelectionRect = root.Q<VisualElement>("HoverSelectionRect");
        _clickMarker = root.Q<VisualElement>("ClickMarker");

        _zoomInButton = FindOptionalButton(root, zoomInButtonName);
        _zoomOutButton = FindOptionalButton(root, zoomOutButtonName);
        _resetButton = FindOptionalButton(root, resetButtonName);

        LogIfMissing(_miniMapViewport, "MiniMapViewport");
        LogIfMissing(_miniMapImage, "MiniMapImage");
        LogIfMissing(_currentCameraRect, "CurrentCameraRect");
        LogIfMissing(_hoverSelectionRect, "HoverSelectionRect");
        LogIfMissing(_clickMarker, "ClickMarker");

        SetOverlayPickingIgnore(_miniMapImage);
        SetOverlayPickingIgnore(_currentCameraRect);
        SetOverlayPickingIgnore(_hoverSelectionRect);
        SetOverlayPickingIgnore(_clickMarker);
    }

    private void RegisterCallbacks()
    {
        if (_isBound || _miniMapViewport == null)
            return;

        _miniMapViewport.RegisterCallback<PointerEnterEvent>(OnPointerEnterViewport);
        _miniMapViewport.RegisterCallback<PointerMoveEvent>(OnPointerMoveViewport);
        _miniMapViewport.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveViewport);
        _miniMapViewport.RegisterCallback<PointerDownEvent>(OnPointerDownViewport);
        _miniMapViewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);

        if (_zoomInButton != null)
            _zoomInButton.clicked += OnZoomInClicked;

        if (_zoomOutButton != null)
            _zoomOutButton.clicked += OnZoomOutClicked;

        if (_resetButton != null)
            _resetButton.clicked += OnResetClicked;

        _isBound = true;
    }

    private void UnregisterCallbacks()
    {
        if (!_isBound || _miniMapViewport == null)
            return;

        _miniMapViewport.UnregisterCallback<PointerEnterEvent>(OnPointerEnterViewport);
        _miniMapViewport.UnregisterCallback<PointerMoveEvent>(OnPointerMoveViewport);
        _miniMapViewport.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveViewport);
        _miniMapViewport.UnregisterCallback<PointerDownEvent>(OnPointerDownViewport);
        _miniMapViewport.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);

        if (_zoomInButton != null)
            _zoomInButton.clicked -= OnZoomInClicked;

        if (_zoomOutButton != null)
            _zoomOutButton.clicked -= OnZoomOutClicked;

        if (_resetButton != null)
            _resetButton.clicked -= OnResetClicked;

        _isBound = false;
    }

    private void ApplyMinimapImage()
    {
        if (_miniMapImage == null)
            return;

        Texture2D sourceTexture = GetAssignedMinimapTexture();

        if (sourceTexture == null)
        {
            Debug.LogWarning("[MiniMapController] No minimap Sprite or Texture2D is assigned.");
            return;
        }

        _miniMapImage.style.backgroundImage = new StyleBackground(sourceTexture);
    }

    private void HideOptionalOverlays()
    {
        if (_hoverSelectionRect != null)
            _hoverSelectionRect.style.display = DisplayStyle.None;

        if (_clickMarker != null)
            _clickMarker.style.display = DisplayStyle.None;

        if (_currentCameraRect != null)
            _currentCameraRect.style.display = DisplayStyle.Flex;
    }

    private void OnPointerEnterViewport(PointerEnterEvent evt)
    {
        if (_hoverSelectionRect == null)
            return;

        _hoverSelectionRect.style.display = DisplayStyle.Flex;
        UpdateHoverSelectionRect(evt.localPosition);
    }

    private void OnPointerMoveViewport(PointerMoveEvent evt)
    {
        if (_hoverSelectionRect == null)
            return;

        _hoverSelectionRect.style.display = DisplayStyle.Flex;
        UpdateHoverSelectionRect(evt.localPosition);
    }

    private void OnPointerLeaveViewport(PointerLeaveEvent evt)
    {
        if (_hoverSelectionRect != null)
            _hoverSelectionRect.style.display = DisplayStyle.None;
    }

    private void OnPointerDownViewport(PointerDownEvent evt)
    {
        if (evt.button != 0)
            return;

        if (!TryGetViewportSize(out Vector2 viewportSize))
            return;

        Vector2 clampedLocalPosition = ClampPointToViewport(evt.localPosition, viewportSize);
        Vector2 normalizedPosition = LocalToNormalized(clampedLocalPosition, viewportSize);
        Vector3 worldPosition = NormalizedToWorld(normalizedPosition);

        UpdateHoverSelectionRect(clampedLocalPosition);
        UpdateClickMarker(clampedLocalPosition);

        if (cameraPanZoom != null)
            cameraPanZoom.FocusOnWorldPosition(worldPosition);
        else
            Debug.LogWarning("[MiniMapController] SimpleCameraPanZoom is not assigned.");

        UpdateCurrentCameraRect();
    }

    private void OnViewportGeometryChanged(GeometryChangedEvent evt)
    {
        UpdateCurrentCameraRect();
    }

    private void OnZoomInClicked()
    {
        cameraPanZoom?.ZoomIn();
        UpdateCurrentCameraRect();
    }

    private void OnZoomOutClicked()
    {
        cameraPanZoom?.ZoomOut();
        UpdateCurrentCameraRect();
    }

    private void OnResetClicked()
    {
        cameraPanZoom?.ResetCameraToStart();
        UpdateCurrentCameraRect();
    }

    private void UpdateHoverSelectionRect(Vector2 localPosition)
    {
        if (_hoverSelectionRect == null || !TryGetViewportSize(out Vector2 viewportSize))
            return;

        Vector2 sizePixels = new Vector2(
            Mathf.Max(8f, viewportSize.x * Mathf.Clamp01(hoverRectSizeNormalized.x)),
            Mathf.Max(8f, viewportSize.y * Mathf.Clamp01(hoverRectSizeNormalized.y))
        );

        Vector2 topLeft = ClampTopLeft(localPosition - (sizePixels * 0.5f), sizePixels, viewportSize);

        ApplyOverlayRect(_hoverSelectionRect, topLeft, sizePixels);
    }

    private void UpdateClickMarker(Vector2 localPosition)
    {
        if (_clickMarker == null || !TryGetViewportSize(out Vector2 viewportSize))
            return;

        Vector2 topLeft = ClampTopLeft(localPosition - (ClickMarkerSizePixels * 0.5f), ClickMarkerSizePixels, viewportSize);

        _clickMarker.style.display = DisplayStyle.Flex;
        ApplyOverlayRect(_clickMarker, topLeft, ClickMarkerSizePixels);
    }

    private void UpdateCurrentCameraRect()
    {
        if (_currentCameraRect == null || targetCamera == null || !TryGetViewportSize(out Vector2 viewportSize))
            return;

        GetWorldBounds(out Vector2 minBounds, out Vector2 maxBounds);

        Vector2 mapSize = maxBounds - minBounds;
        if (mapSize.x <= 0.001f || mapSize.y <= 0.001f)
            return;

        Vector2 sizeNormalized = minimumCurrentRectSizeNormalized;

        if (targetCamera.orthographic)
        {
            float visibleWorldHeight = targetCamera.orthographicSize * 2f;
            float visibleWorldWidth = visibleWorldHeight * targetCamera.aspect;

            sizeNormalized = new Vector2(
                Mathf.Clamp01(Mathf.Max(minimumCurrentRectSizeNormalized.x, visibleWorldWidth / mapSize.x)),
                Mathf.Clamp01(Mathf.Max(minimumCurrentRectSizeNormalized.y, visibleWorldHeight / mapSize.y))
            );
        }

        Vector2 cameraCenterNormalized = new Vector2(
            Mathf.InverseLerp(minBounds.x, maxBounds.x, targetCamera.transform.position.x),
            Mathf.InverseLerp(minBounds.y, maxBounds.y, targetCamera.transform.position.y)
        );

        Vector2 sizePixels = new Vector2(
            Mathf.Clamp(sizeNormalized.x * viewportSize.x, 8f, viewportSize.x),
            Mathf.Clamp(sizeNormalized.y * viewportSize.y, 8f, viewportSize.y)
        );

        Vector2 centerPixels = new Vector2(
            cameraCenterNormalized.x * viewportSize.x,
            (1f - cameraCenterNormalized.y) * viewportSize.y
        );

        Vector2 topLeft = ClampTopLeft(centerPixels - (sizePixels * 0.5f), sizePixels, viewportSize);

        _currentCameraRect.style.display = DisplayStyle.Flex;
        ApplyOverlayRect(_currentCameraRect, topLeft, sizePixels);
    }

    private Texture2D GetAssignedMinimapTexture()
    {
        if (minimapTexture != null)
            return minimapTexture;

        return minimapSprite != null ? minimapSprite.texture : null;
    }

    private Button FindOptionalButton(VisualElement root, string buttonName)
    {
        if (root == null || string.IsNullOrWhiteSpace(buttonName))
            return null;

        Button button = root.Q<Button>(buttonName);

        if (button == null)
            Debug.LogWarning($"[MiniMapController] Could not find optional button named '{buttonName}'.");

        return button;
    }

    private void SetOverlayPickingIgnore(VisualElement element)
    {
        if (element != null)
            element.pickingMode = PickingMode.Ignore;
    }

    private void LogIfMissing(VisualElement element, string elementName)
    {
        if (element == null)
            Debug.LogWarning($"[MiniMapController] Could not find '{elementName}' in the minimap UXML.");
    }

    private bool TryGetViewportSize(out Vector2 viewportSize)
    {
        viewportSize = Vector2.zero;

        if (_miniMapViewport == null)
            return false;

        Rect contentRect = _miniMapViewport.contentRect;
        if (contentRect.width <= 0f || contentRect.height <= 0f)
            return false;

        viewportSize = contentRect.size;
        return true;
    }

    private void GetWorldBounds(out Vector2 minBounds, out Vector2 maxBounds)
    {
        minBounds = Vector2.Min(mapWorldMin, mapWorldMax);
        maxBounds = Vector2.Max(mapWorldMin, mapWorldMax);
    }

    private Vector2 ClampPointToViewport(Vector2 point, Vector2 viewportSize)
    {
        return new Vector2(
            Mathf.Clamp(point.x, 0f, viewportSize.x),
            Mathf.Clamp(point.y, 0f, viewportSize.y)
        );
    }

    private Vector2 ClampTopLeft(Vector2 topLeft, Vector2 sizePixels, Vector2 viewportSize)
    {
        return new Vector2(
            Mathf.Clamp(topLeft.x, 0f, Mathf.Max(0f, viewportSize.x - sizePixels.x)),
            Mathf.Clamp(topLeft.y, 0f, Mathf.Max(0f, viewportSize.y - sizePixels.y))
        );
    }

    private Vector2 LocalToNormalized(Vector2 localPosition, Vector2 viewportSize)
    {
        return new Vector2(
            Mathf.Clamp01(localPosition.x / viewportSize.x),
            Mathf.Clamp01(1f - (localPosition.y / viewportSize.y))
        );
    }

    private Vector3 NormalizedToWorld(Vector2 normalizedPosition)
    {
        GetWorldBounds(out Vector2 minBounds, out Vector2 maxBounds);

        float worldX = Mathf.Lerp(minBounds.x, maxBounds.x, normalizedPosition.x);
        float worldY = Mathf.Lerp(minBounds.y, maxBounds.y, normalizedPosition.y);
        float worldZ = targetCamera != null ? targetCamera.transform.position.z : 0f;

        return new Vector3(worldX, worldY, worldZ);
    }

    private void ApplyOverlayRect(VisualElement element, Vector2 topLeft, Vector2 sizePixels)
    {
        element.style.left = topLeft.x;
        element.style.top = topLeft.y;
        element.style.width = sizePixels.x;
        element.style.height = sizePixels.y;
    }
}
