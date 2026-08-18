using System.Collections.Generic;
using UnityEngine;

public struct ZoneRiskOutlineRequest
{
    public readonly string Geoid;
    public readonly Color Color;

    public ZoneRiskOutlineRequest(string geoid, Color color)
    {
        Geoid = geoid;
        Color = color;
    }
}

public struct TileOutlineRequest
{
    public readonly Vector3Int Cell;
    public readonly Color Color;

    public TileOutlineRequest(Vector3Int cell, Color color)
    {
        Cell = cell;
        Color = color;
    }
}

public class ZoneThinOutlineByHover : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private FloodDefenseBoxStamp floodDefense;
    [SerializeField] private Camera mainCamera;

    [Header("Line Appearance")]
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private float lineWidth = 0.045f;
    [SerializeField] private float zOffset = -0.1f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 1000;

    [Header("Behavior")]
    [SerializeField] private bool outliningEnabled = true;

    [Tooltip("Recommended ON. This prevents the hover outline from fighting with Place Barriers mode.")]
    [SerializeField] private bool hideWhileBarrierModeIsActive = true;

    [Tooltip("Recommended ON. This hides persistent outlines while Place Barriers mode is active, without clearing cached risk highlights.")]
    [SerializeField] private bool hidePersistentWhileBarrierModeIsActive = true;

    private readonly List<LineRenderer> _linePool = new();
    private readonly List<LineRenderer> _baselineLinePool = new();
    private readonly List<LineRenderer> _liveFloodLinePool = new();
    private readonly List<LineRenderer> _shelterZoneLinePool = new();
    private readonly List<LineRenderer> _shelterTileLinePool = new();
    private readonly List<LineRenderer> _communicationZoneLinePool = new();

    private string _currentGeoid = null;
    private HashSet<Vector2Int> _currentZoneTiles = null;

    private Material _runtimeMaterial;
    private int _baselineActiveLineCount;
    private int _liveFloodActiveLineCount;
    private int _shelterZoneActiveLineCount;
    private int _shelterTileActiveLineCount;
    private int _communicationZoneActiveLineCount;
    private bool _persistentOutlinesVisible = true;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (floodDefense == null)
            floodDefense = FindFirstObjectByType<FloodDefenseBoxStamp>();
    }

    private void Update()
    {
        if (floodDefense == null)
        {
            ClearOutline();
            SetPersistentOutlineVisibility(false);
            return;
        }

        if (hideWhileBarrierModeIsActive && floodDefense.IsZoneBoundaryModeActive)
        {
            ClearOutline();

            if (hidePersistentWhileBarrierModeIsActive)
                SetPersistentOutlineVisibility(false);

            return;
        }

        if (hidePersistentWhileBarrierModeIsActive && !_persistentOutlinesVisible)
            SetPersistentOutlineVisibility(true);

        if (!outliningEnabled || mainCamera == null)
        {
            ClearOutline();
            return;
        }

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        string geoid;
        HashSet<Vector2Int> zoneTiles;

        bool hit = floodDefense.TryGetZoneTilesAtWorld(
            world,
            out geoid,
            out zoneTiles
        );

        if (!hit || string.IsNullOrEmpty(geoid) || zoneTiles == null)
        {
            ClearOutline();
            return;
        }

        if (_currentGeoid == geoid && _currentZoneTiles == zoneTiles)
            return;

        _currentGeoid = geoid;
        _currentZoneTiles = zoneTiles;

        DrawThinZoneOutline(zoneTiles);
    }

    private void DrawThinZoneOutline(HashSet<Vector2Int> zoneTiles)
    {
        List<Segment> segments = BuildBoundarySegments(zoneTiles);
        DrawSegments(segments, _linePool, lineColor, out _);
    }

    public void ShowPersistentZoneOutlines(IEnumerable<HashSet<Vector2Int>> zones, Color color)
    {
        if (!TryResolveFloodDefense())
        {
            Debug.LogWarning("[ZoneThinOutlineByHover] Cannot show baseline risk outlines because FloodDefenseBoxStamp is missing.");
            return;
        }

        List<Segment> segments = new List<Segment>();

        if (zones != null)
        {
            foreach (HashSet<Vector2Int> zoneTiles in zones)
            {
                if (zoneTiles == null || zoneTiles.Count == 0)
                    continue;

                segments.AddRange(BuildBoundarySegments(zoneTiles));
            }
        }

        DrawSegments(segments, _baselineLinePool, color, out _baselineActiveLineCount);
        _persistentOutlinesVisible = true;

        if (hidePersistentWhileBarrierModeIsActive && floodDefense.IsZoneBoundaryModeActive)
            SetPersistentOutlineVisibility(false);
    }

    public void ShowPersistentZoneOutlinesByGeoid(IEnumerable<string> geoids, Color color)
    {
        ShowBaselineRiskOutlines(BuildOutlineRequestsFromGeoids(geoids, color));
    }

    public void ShowOutlineForZone(string geoid)
    {
        ShowBaselineRiskOutlines(new[] { new ZoneRiskOutlineRequest(geoid, lineColor) });
    }

    public void ShowRiskOutlines(IEnumerable<string> geoids)
    {
        ShowBaselineRiskOutlines(BuildOutlineRequestsFromGeoids(geoids, lineColor));
    }

    public void ShowRiskOutlineForZone(string geoid, Color color)
    {
        ShowBaselineRiskOutlines(new[] { new ZoneRiskOutlineRequest(geoid, color) });
    }

    public void ShowRiskOutlines(IEnumerable<ZoneRiskOutlineRequest> requests)
    {
        ShowBaselineRiskOutlines(requests);
    }

    public void ShowBaselineRiskOutlines(IEnumerable<ZoneRiskOutlineRequest> requests)
    {
        if (!TryResolveFloodDefense())
        {
            Debug.LogWarning("[ZoneThinOutlineByHover] Cannot show baseline risk outlines because FloodDefenseBoxStamp is missing.");
            return;
        }

        ShowOverlayRequests(requests, _baselineLinePool, out _baselineActiveLineCount);
    }

    public void ShowLiveFloodRiskOutlines(IEnumerable<ZoneRiskOutlineRequest> requests)
    {
        if (!TryResolveFloodDefense())
        {
            Debug.LogWarning("[ZoneThinOutlineByHover] Cannot show live flood risk outlines because FloodDefenseBoxStamp is missing.");
            return;
        }

        ShowOverlayRequests(requests, _liveFloodLinePool, out _liveFloodActiveLineCount);
    }

    public void ClearPersistentZoneOutlines()
    {
        ClearBaselineRiskOutlines();
    }

    public void ClearRiskOutlines()
    {
        ClearBaselineRiskOutlines();
    }

    public void ClearBaselineRiskOutlines()
    {
        _baselineActiveLineCount = 0;
        HidePool(_baselineLinePool);
    }

    public void ClearLiveFloodRiskOutlines()
    {
        _liveFloodActiveLineCount = 0;
        HidePool(_liveFloodLinePool);
    }

    public void ShowShelterAssociatedZoneOutlines(IEnumerable<ZoneRiskOutlineRequest> requests)
    {
        if (!TryResolveFloodDefense())
        {
            Debug.LogWarning("[ZoneThinOutlineByHover] Cannot show shelter zone outlines because FloodDefenseBoxStamp is missing.");
            return;
        }

        ShowOverlayRequests(requests, _shelterZoneLinePool, out _shelterZoneActiveLineCount);
    }

    public void ShowShelterCandidateTiles(IEnumerable<TileOutlineRequest> requests)
    {
        if (!TryResolveFloodDefense())
        {
            Debug.LogWarning("[ZoneThinOutlineByHover] Cannot show shelter tile highlights because FloodDefenseBoxStamp is missing.");
            return;
        }

        List<ColoredSegment> segments = BuildTileSegments(requests);
        DrawSegments(segments, _shelterTileLinePool, out _shelterTileActiveLineCount);
        _persistentOutlinesVisible = true;

        if (hidePersistentWhileBarrierModeIsActive && floodDefense.IsZoneBoundaryModeActive)
            SetPersistentOutlineVisibility(false);
    }

    public void ClearShelterCandidateHighlights()
    {
        _shelterZoneActiveLineCount = 0;
        _shelterTileActiveLineCount = 0;
        HidePool(_shelterZoneLinePool);
        HidePool(_shelterTileLinePool);
    }

    public void ShowCommunicationZoneOutlines(IEnumerable<ZoneRiskOutlineRequest> requests)
    {
        if (!TryResolveFloodDefense())
        {
            Debug.LogWarning("[ZoneThinOutlineByHover] Cannot show communication zone outlines because FloodDefenseBoxStamp is missing.");
            return;
        }

        ShowOverlayRequests(requests, _communicationZoneLinePool, out _communicationZoneActiveLineCount);
    }

    public void ClearCommunicationZoneOutlines()
    {
        _communicationZoneActiveLineCount = 0;
        HidePool(_communicationZoneLinePool);
    }

    private bool TryResolveFloodDefense()
    {
        if (floodDefense == null)
            floodDefense = FindFirstObjectByType<FloodDefenseBoxStamp>();

        return floodDefense != null;
    }

    private void DrawSegments(
        List<Segment> segments,
        List<LineRenderer> linePool,
        Color color,
        out int activeLineCount)
    {
        if (segments == null)
        {
            activeLineCount = 0;
            return;
        }

        List<ColoredSegment> coloredSegments = new(segments.Count);

        for (int i = 0; i < segments.Count; i++)
            coloredSegments.Add(new ColoredSegment(segments[i], color));

        DrawSegments(coloredSegments, linePool, out activeLineCount);
    }

    private void DrawSegments(
        List<ColoredSegment> segments,
        List<LineRenderer> linePool,
        out int activeLineCount)
    {
        activeLineCount = segments != null ? segments.Count : 0;

        if (segments == null)
            return;

        EnsureLinePoolSize(linePool, segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            LineRenderer lr = linePool[i];
            ColoredSegment coloredSegment = segments[i];

            lr.gameObject.SetActive(true);
            lr.positionCount = 2;
            lr.loop = false;
            ApplyRendererColor(lr, coloredSegment.color);

            Vector3 a = coloredSegment.segment.a;
            Vector3 b = coloredSegment.segment.b;

            a.z += zOffset;
            b.z += zOffset;

            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
        }

        for (int i = segments.Count; i < linePool.Count; i++)
        {
            linePool[i].gameObject.SetActive(false);
        }
    }

    private List<ColoredSegment> BuildBoundarySegments(HashSet<Vector2Int> zoneTiles, Color color)
    {
        List<Segment> segments = BuildBoundarySegments(zoneTiles);
        List<ColoredSegment> coloredSegments = new(segments.Count);

        for (int i = 0; i < segments.Count; i++)
            coloredSegments.Add(new ColoredSegment(segments[i], color));

        return coloredSegments;
    }

    private List<Segment> BuildBoundarySegments(HashSet<Vector2Int> zoneTiles)
    {
        List<Segment> segments = new();

        foreach (Vector2Int t in zoneTiles)
        {
            bool hasLeft = zoneTiles.Contains(new Vector2Int(t.x - 1, t.y));
            bool hasRight = zoneTiles.Contains(new Vector2Int(t.x + 1, t.y));
            bool hasDown = zoneTiles.Contains(new Vector2Int(t.x, t.y - 1));
            bool hasUp = zoneTiles.Contains(new Vector2Int(t.x, t.y + 1));

            Vector3Int blCell = floodDefense.NormalizedTileToCell(t);
            Vector3Int brCell = floodDefense.NormalizedTileToCell(new Vector2Int(t.x + 1, t.y));
            Vector3Int tlCell = floodDefense.NormalizedTileToCell(new Vector2Int(t.x, t.y + 1));
            Vector3Int trCell = floodDefense.NormalizedTileToCell(new Vector2Int(t.x + 1, t.y + 1));

            Vector3 bl = floodDefense.TerrainTilemap.CellToWorld(blCell);
            Vector3 br = floodDefense.TerrainTilemap.CellToWorld(brCell);
            Vector3 tl = floodDefense.TerrainTilemap.CellToWorld(tlCell);
            Vector3 tr = floodDefense.TerrainTilemap.CellToWorld(trCell);

            if (!hasDown)
                segments.Add(new Segment(bl, br));

            if (!hasUp)
                segments.Add(new Segment(tl, tr));

            if (!hasLeft)
                segments.Add(new Segment(bl, tl));

            if (!hasRight)
                segments.Add(new Segment(br, tr));
        }

        return segments;
    }

    private void EnsureLinePoolSize(List<LineRenderer> linePool, int count)
    {
        while (linePool.Count < count)
        {
            LineRenderer lr = CreateLineRenderer();
            linePool.Add(lr);
        }
    }

    private LineRenderer CreateLineRenderer()
    {
        LineRenderer lr;

        if (linePrefab != null)
        {
            lr = Instantiate(linePrefab, transform);
        }
        else
        {
            GameObject go = new GameObject("ZoneThinOutlineSegment");
            go.transform.SetParent(transform, false);
            lr = go.AddComponent<LineRenderer>();
        }

        ConfigureLineRenderer(lr);
        lr.gameObject.SetActive(false);

        return lr;
    }

    private void ConfigureLineRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = true;
        lr.loop = false;

        lr.widthMultiplier = lineWidth;
        lr.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);

        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;

        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        Material sharedMaterial = CreateMaterialInstance();

        if (sharedMaterial != null)
            lr.sharedMaterial = sharedMaterial;

        ApplyRendererColor(lr, lineColor);
    }

    private Material CreateMaterialInstance()
    {
        if (lineMaterial != null)
            return lineMaterial;

        if (_runtimeMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader != null)
                _runtimeMaterial = new Material(shader);
        }
        return _runtimeMaterial;
    }

    private void ApplyRendererColor(LineRenderer lr, Color color)
    {
        lr.startColor = color;
        lr.endColor = color;

        if (lr.sharedMaterial != null)
        {
            MaterialPropertyBlock propertyBlock = new();

            if (lr.sharedMaterial.HasProperty("_BaseColor"))
                propertyBlock.SetColor("_BaseColor", color);

            if (lr.sharedMaterial.HasProperty("_Color"))
                propertyBlock.SetColor("_Color", color);

            lr.SetPropertyBlock(propertyBlock);
        }
    }

    private void ClearOutline()
    {
        _currentGeoid = null;
        _currentZoneTiles = null;

        for (int i = 0; i < _linePool.Count; i++)
        {
            if (_linePool[i] != null)
                _linePool[i].gameObject.SetActive(false);
        }
    }

    private void SetPersistentOutlineVisibility(bool visible)
    {
        _persistentOutlinesVisible = visible;

        ApplyPoolVisibility(_baselineLinePool, _baselineActiveLineCount, visible);
        ApplyPoolVisibility(_liveFloodLinePool, _liveFloodActiveLineCount, visible);
        ApplyPoolVisibility(_shelterZoneLinePool, _shelterZoneActiveLineCount, visible);
        ApplyPoolVisibility(_shelterTileLinePool, _shelterTileActiveLineCount, visible);
        ApplyPoolVisibility(_communicationZoneLinePool, _communicationZoneActiveLineCount, visible);
    }

    private void ShowOverlayRequests(
        IEnumerable<ZoneRiskOutlineRequest> requests,
        List<LineRenderer> targetPool,
        out int activeLineCount)
    {
        List<ColoredSegment> segments = BuildColoredSegments(requests);
        DrawSegments(segments, targetPool, out activeLineCount);
        _persistentOutlinesVisible = true;

        if (hidePersistentWhileBarrierModeIsActive && floodDefense.IsZoneBoundaryModeActive)
            SetPersistentOutlineVisibility(false);
    }

    private List<ColoredSegment> BuildTileSegments(IEnumerable<TileOutlineRequest> requests)
    {
        List<ColoredSegment> segments = new();

        if (requests == null || floodDefense == null || floodDefense.TerrainTilemap == null)
            return segments;

        foreach (TileOutlineRequest request in requests)
        {
            Vector3Int blCell = request.Cell;
            Vector3Int brCell = new Vector3Int(request.Cell.x + 1, request.Cell.y, request.Cell.z);
            Vector3Int tlCell = new Vector3Int(request.Cell.x, request.Cell.y + 1, request.Cell.z);
            Vector3Int trCell = new Vector3Int(request.Cell.x + 1, request.Cell.y + 1, request.Cell.z);

            Vector3 bl = floodDefense.TerrainTilemap.CellToWorld(blCell);
            Vector3 br = floodDefense.TerrainTilemap.CellToWorld(brCell);
            Vector3 tl = floodDefense.TerrainTilemap.CellToWorld(tlCell);
            Vector3 tr = floodDefense.TerrainTilemap.CellToWorld(trCell);

            segments.Add(new ColoredSegment(new Segment(bl, br), request.Color));
            segments.Add(new ColoredSegment(new Segment(br, tr), request.Color));
            segments.Add(new ColoredSegment(new Segment(tr, tl), request.Color));
            segments.Add(new ColoredSegment(new Segment(tl, bl), request.Color));
        }

        return segments;
    }

    private List<ColoredSegment> BuildColoredSegments(IEnumerable<ZoneRiskOutlineRequest> requests)
    {
        List<ColoredSegment> segments = new List<ColoredSegment>();

        if (requests == null)
            return segments;

        foreach (ZoneRiskOutlineRequest request in requests)
        {
            if (string.IsNullOrWhiteSpace(request.Geoid))
                continue;

            if (!floodDefense.TryGetZoneTiles(request.Geoid, out HashSet<Vector2Int> zoneTiles) ||
                zoneTiles == null ||
                zoneTiles.Count == 0)
            {
                continue;
            }

            segments.AddRange(BuildBoundarySegments(zoneTiles, request.Color));
        }

        return segments;
    }

    private IEnumerable<ZoneRiskOutlineRequest> BuildOutlineRequestsFromGeoids(IEnumerable<string> geoids, Color color)
    {
        List<ZoneRiskOutlineRequest> requests = new List<ZoneRiskOutlineRequest>();

        if (geoids == null)
            return requests;

        foreach (string geoid in geoids)
        {
            if (string.IsNullOrWhiteSpace(geoid))
                continue;

            requests.Add(new ZoneRiskOutlineRequest(geoid, color));
        }

        return requests;
    }

    private void HidePool(List<LineRenderer> linePool)
    {
        for (int i = 0; i < linePool.Count; i++)
        {
            if (linePool[i] != null)
                linePool[i].gameObject.SetActive(false);
        }
    }

    private void ApplyPoolVisibility(List<LineRenderer> linePool, int activeLineCount, bool visible)
    {
        for (int i = 0; i < linePool.Count; i++)
        {
            if (linePool[i] == null)
                continue;

            bool shouldBeActive = visible && i < activeLineCount;
            linePool[i].gameObject.SetActive(shouldBeActive);
        }
    }

    private struct Segment
    {
        public Vector3 a;
        public Vector3 b;

        public Segment(Vector3 a, Vector3 b)
        {
            this.a = a;
            this.b = b;
        }
    }

    private struct ColoredSegment
    {
        public Segment segment;
        public Color color;

        public ColoredSegment(Segment segment, Color color)
        {
            this.segment = segment;
            this.color = color;
        }
    }
}
