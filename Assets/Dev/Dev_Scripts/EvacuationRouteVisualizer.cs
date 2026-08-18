using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/*
Inspector references required by EvacuationRouteVisualizer:
- Route Tilemap: required. Converts path cells to world-space cell centers with GetCellCenterWorld.
- Line Prefab: optional. If assigned, only its LineRenderer is used; other renderers are disabled to avoid filled prefabs.
- Line Material: optional. A runtime copy is used, or a simple transparent/sprite material is created.

Inspector settings:
- Route Color: default color used when callers pass an invalid color.
- Route Line Width: legacy single-route line width.
- Preview/Hovered/Selected Route Color: colors used by route interaction states.
- Preview/Hovered/Selected Line Width: thin border widths used by route interaction states.
- Sorting Layer Name: layer used by route LineRenderers.
- Route Sorting Order: should be above the terrain/elevation tilemap.
- Route Z Offset: offsets cell centers toward the camera so lines sit over the map.
- Debug Logs: logs draw and clear operations.

Current behavior:
EvacuationRouteVisualizer draws preview visuals only. It receives path cells from
EvacuationController, draws thin LineRenderer borders around each tile in each route path,
and clears old route border lines before drawing new ones. It does not calculate routes, draw filled tiles,
selected overlays, danger segments, flooded segments, missing-road/dashed segments, or mitigation.
*/

public class EvacuationRouteVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap routeTilemap;
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private Material lineMaterial;

    [Header("Inspector Settings")]
    [SerializeField] private Color routeColor = new(0f, 0.75f, 1f, 1f);
    [SerializeField, Min(0.005f)] private float routeLineWidth = 0.08f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int routeSortingOrder = 2300;
    [SerializeField] private float routeZOffset = -0.05f;
    [SerializeField] private bool debugLogs = true;

    [Header("Route States")]
    [SerializeField] private Color previewRouteColor = new(0f, 0.85f, 1f, 0.25f);
    [SerializeField] private Color hoveredRouteColor = new(0f, 0.85f, 1f, 0.75f);
    [SerializeField] private Color selectedRouteColor = new(0f, 0.85f, 1f, 1f);
    [SerializeField, Min(0.005f)] private float previewLineWidth = 0.025f;
    [SerializeField, Min(0.005f)] private float hoveredLineWidth = 0.045f;
    [SerializeField, Min(0.005f)] private float selectedLineWidth = 0.055f;

    private readonly List<LineRenderer> _routeLines = new();
    private readonly List<GameObject> _routeObjects = new();

    private Material _runtimeLineMaterial;
    private int _activeRouteLineCount;

    private void OnValidate()
    {
        routeLineWidth = Mathf.Max(0.005f, routeLineWidth);
        previewLineWidth = Mathf.Max(0.005f, previewLineWidth);
        hoveredLineWidth = Mathf.Max(0.005f, hoveredLineWidth);
        selectedLineWidth = Mathf.Max(0.005f, selectedLineWidth);
    }

    private void OnDisable()
    {
        ClearRoute();
    }

    public void Configure(Tilemap tilemap, float lineWidth, int sortingOrder, float zOffset, bool enableDebugLogs)
    {
        routeTilemap = tilemap;
        routeLineWidth = Mathf.Max(0.005f, lineWidth);
        routeSortingOrder = sortingOrder;
        routeZOffset = zOffset;
        debugLogs = enableDebugLogs;
    }

    public void SetTilemap(Tilemap tilemap)
    {
        routeTilemap = tilemap;
    }

    public void SetDebugLogs(bool enableDebugLogs)
    {
        debugLogs = enableDebugLogs;
    }

    public void ShowRoute(List<Vector3Int> pathCells, Color color)
    {
        ClearRoute();
        DrawRouteTileBorders(pathCells, color, routeLineWidth);
    }

    public void ShowRoutes(IReadOnlyList<SimpleEvacuationRoute> routes, Color color)
    {
        DrawRoutes(routes, null, null);
    }

    public void DrawRoutes(
        IReadOnlyList<SimpleEvacuationRoute> routes,
        string hoveredRouteId,
        HashSet<string> selectedRouteIds)
    {
        ClearRoute();

        int routeCount = routes != null ? routes.Count : 0;

        if (debugLogs)
            Debug.Log($"[EvacuationRouteVisualizer] Drawing tile-border evacuation routes. Route count={routeCount}.");

        if (routes == null || routes.Count == 0)
            return;

        for (int i = 0; i < routes.Count; i++)
        {
            SimpleEvacuationRoute route = routes[i];

            if (route == null || !route.isValid)
                continue;

            string state = GetRouteState(route.routeId, hoveredRouteId, selectedRouteIds, out Color stateColor, out float stateLineWidth);
            DrawRouteTileBorders(route.pathCells, stateColor, stateLineWidth);

            if (debugLogs)
            {
                int cellCount = route.pathCells != null ? route.pathCells.Count : 0;
                Debug.Log($"[EvacuationRouteVisualizer] Route drawn. Route={route.routeId}, State={state}, Cells={cellCount}.");
            }
        }
    }

    public void ClearRoute()
    {
        for (int i = 0; i < _routeLines.Count; i++)
        {
            LineRenderer line = _routeLines[i];

            if (line != null)
            {
                line.positionCount = 0;
                line.enabled = false;
            }

            if (i < _routeObjects.Count && _routeObjects[i] != null)
                _routeObjects[i].SetActive(false);
        }

        _activeRouteLineCount = 0;

        if (debugLogs)
            Debug.Log("[EvacuationRouteVisualizer] Evacuation route visuals cleared.");
    }

    public void ClearRouteVisuals()
    {
        ClearRoute();
    }

    public void ClearRoutes()
    {
        ClearRoute();
    }

    public void ClearAllEvacuationVisuals()
    {
        ClearRoute();
    }

    public void DrawRoute(List<Vector3Int> pathCells, Color color)
    {
        DrawRouteTileBorders(pathCells, color, routeLineWidth);
    }

    public void ShowSimpleRoute(List<Vector3Int> pathCells, Color color)
    {
        ShowRoute(pathCells, color);
    }

    private bool DrawRouteTileBorders(List<Vector3Int> pathCells, Color color, float lineWidth)
    {
        if (routeTilemap == null)
        {
            Debug.LogWarning("[EvacuationRouteVisualizer] Cannot draw evacuation route because Route Tilemap is not assigned.");
            return false;
        }

        if (pathCells == null || pathCells.Count == 0)
        {
            Debug.LogWarning("[EvacuationRouteVisualizer] Cannot draw evacuation route because no path cells were provided.");
            return false;
        }

        Color lineColor = color.a > 0f ? color : routeColor;
        lineColor.a = Mathf.Max(0.01f, lineColor.a);
        float safeLineWidth = Mathf.Max(0.005f, lineWidth);
        int drawnCellCount = 0;

        for (int i = 0; i < pathCells.Count; i++)
        {
            DrawTileBorder(pathCells[i], lineColor, safeLineWidth);
            drawnCellCount++;
        }

        if (debugLogs)
            Debug.Log($"[EvacuationRouteVisualizer] Drawing tile-border route. Cells={drawnCellCount}.");

        return drawnCellCount > 0;
    }

    private void DrawTileBorder(Vector3Int cell, Color lineColor, float lineWidth)
    {
        LineRenderer routeLine = GetRouteLineRenderer(_activeRouteLineCount);
        _activeRouteLineCount++;

        Vector3 center = routeTilemap.GetCellCenterWorld(cell);
        Vector3 rightStep = routeTilemap.GetCellCenterWorld(cell + Vector3Int.right) - center;
        Vector3 upStep = routeTilemap.GetCellCenterWorld(cell + Vector3Int.up) - center;
        Vector3 halfRight = rightStep * 0.5f;
        Vector3 halfUp = upStep * 0.5f;

        Vector3 corner0 = center - halfRight - halfUp;
        Vector3 corner1 = center + halfRight - halfUp;
        Vector3 corner2 = center + halfRight + halfUp;
        Vector3 corner3 = center - halfRight + halfUp;

        corner0.z += routeZOffset;
        corner1.z += routeZOffset;
        corner2.z += routeZOffset;
        corner3.z += routeZOffset;

        routeLine.positionCount = 4;
        routeLine.SetPosition(0, corner0);
        routeLine.SetPosition(1, corner1);
        routeLine.SetPosition(2, corner2);
        routeLine.SetPosition(3, corner3);

        ApplyLineAppearance(routeLine, lineColor, lineWidth, true);
    }

    private string GetRouteState(
        string routeId,
        string hoveredRouteId,
        HashSet<string> selectedRouteIds,
        out Color stateColor,
        out float stateLineWidth)
    {
        if (!string.IsNullOrEmpty(routeId) && selectedRouteIds != null && selectedRouteIds.Contains(routeId))
        {
            stateColor = selectedRouteColor;
            stateLineWidth = selectedLineWidth;
            return "selected";
        }

        if (!string.IsNullOrEmpty(routeId) && routeId == hoveredRouteId)
        {
            stateColor = hoveredRouteColor;
            stateLineWidth = hoveredLineWidth;
            return "hovered";
        }

        stateColor = previewRouteColor;
        stateLineWidth = previewLineWidth;
        return "preview";
    }

    private LineRenderer GetRouteLineRenderer(int lineIndex)
    {
        if (lineIndex < _routeLines.Count && _routeLines[lineIndex] != null)
        {
            if (lineIndex < _routeObjects.Count && _routeObjects[lineIndex] != null)
                _routeObjects[lineIndex].SetActive(true);

            _routeLines[lineIndex].enabled = true;
            return _routeLines[lineIndex];
        }

        GameObject routeObject;
        LineRenderer routeLine;

        if (linePrefab != null)
        {
            routeObject = Instantiate(linePrefab.gameObject, transform);
            routeObject.name = $"EvacuationRoute_Line_{lineIndex + 1}";

            routeLine = routeObject.GetComponent<LineRenderer>();

            if (routeLine == null)
                routeLine = routeObject.AddComponent<LineRenderer>();

            DisableNonLineRenderers(routeObject, routeLine);
        }
        else
        {
            routeObject = new GameObject($"EvacuationRoute_Line_{lineIndex + 1}");
            routeObject.transform.SetParent(transform, false);
            routeLine = routeObject.AddComponent<LineRenderer>();
        }

        routeObject.SetActive(true);
        routeLine.enabled = true;
        _routeObjects.Add(routeObject);
        _routeLines.Add(routeLine);
        return routeLine;
    }

    private void ApplyLineAppearance(LineRenderer lineRenderer, Color lineColor, float lineWidth, bool loop)
    {
        float safeLineWidth = Mathf.Max(0.005f, lineWidth);

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = loop;
        lineRenderer.widthMultiplier = safeLineWidth;
        lineRenderer.startWidth = safeLineWidth;
        lineRenderer.endWidth = safeLineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.sortingOrder = routeSortingOrder;

        if (!string.IsNullOrWhiteSpace(sortingLayerName))
            lineRenderer.sortingLayerName = sortingLayerName;

        Material material = GetRuntimeLineMaterial();

        if (material != null)
            lineRenderer.sharedMaterial = material;
    }

    private Material GetRuntimeLineMaterial()
    {
        if (_runtimeLineMaterial != null)
            return _runtimeLineMaterial;

        if (lineMaterial != null && lineMaterial.shader != null && !IsErrorShader(lineMaterial.shader))
        {
            _runtimeLineMaterial = new Material(lineMaterial)
            {
                name = "EvacuationRouteLine_Runtime",
            };
            ApplyMaterialColor(_runtimeLineMaterial, Color.white);
            return _runtimeLineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            return null;

        _runtimeLineMaterial = new Material(shader)
        {
            name = "EvacuationRouteLine_Runtime",
        };
        ApplyMaterialColor(_runtimeLineMaterial, Color.white);
        return _runtimeLineMaterial;
    }

    private static void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static void DisableNonLineRenderers(GameObject root, LineRenderer routeLineRenderer)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == routeLineRenderer)
                continue;

            renderer.enabled = false;
        }
    }

    private static bool IsErrorShader(Shader shader)
    {
        return shader != null && shader.name.Contains("InternalErrorShader");
    }
}
