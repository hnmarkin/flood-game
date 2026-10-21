using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class ZoneController : MonoBehaviour
{
    [Header("Legacy Map Sources")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private JsonMapLoader jsonMapLoader;

    [Header("Outline Rendering")]
    [SerializeField] private Transform outlineRoot;
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float zOffset = -0.1f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 1000;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly ZoneIndex _zoneIndex = new ZoneIndex();
    private ZoneOutlineRenderer _outlineRenderer;
    private bool _suppressHoverOnShow;
    private bool _suppressPersistentOnShow;
    private bool _loggedMissingOutlineRenderer;
    private bool _isDestroying;

    public bool IsInitialized => _zoneIndex.IsInitialized;
    public int ZoneCount => _zoneIndex.ZoneCount;
    public Tilemap TerrainTilemap => terrainTilemap;

    /// <summary>Side-effect-free resource check; map readiness is checked by setters.</summary>
    public bool IsOutlineRenderingAvailable =>
        !_isDestroying && _outlineRenderer != null && _outlineRenderer.IsAvailable;

    private void Awake()
    {
        EnsureOutlineRenderer();
    }

    private void Start()
    {
        // One bounded retry allows another Awake to supply a missing reference.
        if (_outlineRenderer == null)
            EnsureOutlineRenderer();
    }

    private void Update()
    {
        if (IsOutlineRenderingAvailable)
            ConfigureOutlineRenderer();
    }

    public Vector2Int TileOrigin
    {
        get
        {
            if (_zoneIndex.IsInitialized)
                return _zoneIndex.TileOrigin;

            if (terrainTilemap == null)
                return Vector2Int.zero;

            terrainTilemap.CompressBounds();
            BoundsInt bounds = terrainTilemap.cellBounds;
            return new Vector2Int(bounds.xMin, bounds.yMin);
        }
    }

    public void Configure(
        TileMapData mapData,
        Tilemap terrain,
        JsonMapLoader loader,
        bool enableDebugLogs)
    {
        bool referencesChanged =
            tileMapData != mapData ||
            terrainTilemap != terrain ||
            jsonMapLoader != loader;

        tileMapData = mapData;
        terrainTilemap = terrain;
        jsonMapLoader = loader;
        debugLogs = enableDebugLogs;

        if (referencesChanged)
            _zoneIndex.Clear();
    }

    public bool IsZoneLoaderReady()
    {
        if (tileMapData == null || terrainTilemap == null)
            return false;

        return ZoneIndex.IsLoaderReady(jsonMapLoader);
    }

    public bool EnsureZoneIndexReadyForExternalUse()
    {
        if (_zoneIndex.IsInitialized)
            return _zoneIndex.ZoneCount > 0;

        return RebuildZoneIndex();
    }

    public bool RebuildZoneIndex()
    {
        if (!IsZoneLoaderReady())
        {
            if (debugLogs)
                Debug.LogWarning("[ZoneController] Zone index is not ready to build.");

            _zoneIndex.Clear();
            return false;
        }

        bool built = _zoneIndex.TryBuild(jsonMapLoader, tileMapData, terrainTilemap, debugLogs);
        return built && _zoneIndex.ZoneCount > 0;
    }

    public void InvalidateZoneIndex()
    {
        _zoneIndex.Clear();
    }

    /// <summary>
    /// Compatibility API. Returns the mutable legacy tile set until callers migrate
    /// to read-only zone queries.
    /// </summary>
    public bool TryGetZoneTiles(string geoid, out HashSet<Vector2Int> zoneTiles)
    {
        zoneTiles = null;

        if (string.IsNullOrEmpty(geoid))
            return false;

        if (!EnsureZoneIndexReadyForExternalUse())
            return false;

        return _zoneIndex.TryGetZoneTiles(geoid, out zoneTiles);
    }

    /// <summary>
    /// Compatibility API. Returns the mutable legacy lookup until callers migrate
    /// to read-only zone queries.
    /// </summary>
    public IReadOnlyDictionary<string, HashSet<Vector2Int>> GetAllZoneTiles()
    {
        if (!EnsureZoneIndexReadyForExternalUse())
            return null;

        return _zoneIndex.AllZoneTiles;
    }

    public bool TryGetZoneTilesAtWorld(
        Vector3 world,
        out string geoid,
        out HashSet<Vector2Int> zoneTiles)
    {
        geoid = null;
        zoneTiles = null;

        if (!EnsureZoneIndexReadyForExternalUse())
            return false;

        if (jsonMapLoader == null)
            return false;

        world.z = 0f;

        bool hit = jsonMapLoader.TryGetTileInfoAtWorld(
            world,
            out _,
            out _,
            out _,
            out geoid,
            out _);

        if (!hit || string.IsNullOrEmpty(geoid))
            return false;

        return _zoneIndex.TryGetZoneTiles(geoid, out zoneTiles);
    }

    public Vector3Int NormalizedTileToCell(Vector2Int normalizedTile)
    {
        Vector2Int origin = TileOrigin;
        return new Vector3Int(
            normalizedTile.x + origin.x,
            normalizedTile.y + origin.y,
            0);
    }

    public Vector2Int CellToNormalizedTile(Vector3Int cell)
    {
        Vector2Int origin = TileOrigin;
        return new Vector2Int(cell.x - origin.x, cell.y - origin.y);
    }

    public bool TryGetGeoidAtNormalizedTile(Vector2Int normalizedTile, out string geoid)
    {
        geoid = null;

        if (!EnsureZoneIndexReadyForExternalUse())
            return false;

        return _zoneIndex.TryGetGeoid(normalizedTile, out geoid);
    }

    public bool ContainsTile(string geoid, Vector2Int normalizedTile)
    {
        if (!EnsureZoneIndexReadyForExternalUse())
            return false;

        return _zoneIndex.ContainsTile(geoid, normalizedTile);
    }

    public bool TryGetBoundaryTiles(string geoid, out IReadOnlyList<Vector2Int> boundaryTiles)
    {
        boundaryTiles = null;

        if (!TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles))
            return false;

        boundaryTiles = ZoneGeometry.CalculateBoundaryTiles(zoneTiles);
        return true;
    }

    public bool TryGetBoundaryEdges(string geoid, out IReadOnlyList<ZoneBoundaryEdge> boundaryEdges)
    {
        boundaryEdges = null;

        if (!TryGetZoneTiles(geoid, out HashSet<Vector2Int> zoneTiles))
            return false;

        boundaryEdges = ZoneGeometry.CalculateBoundaryEdges(zoneTiles, _zoneIndex.MapSize);
        return true;
    }

    /// <summary>Replace one channel with one GEOID outline; an unknown GEOID fails without clearing it.</summary>
    public bool SetZoneOutline(ZoneOutlineChannel channel, string geoid, Color color)
    {
        if (!TryPrepareOutlineRequest(channel) ||
            string.IsNullOrWhiteSpace(geoid) ||
            !TryGetBoundaryEdges(geoid, out IReadOnlyList<ZoneBoundaryEdge> edges))
        {
            return false;
        }

        List<ZoneOutlineRenderer.ColoredSegment> segments = new();
        AppendOutlineEdges(segments, edges, color, geoid);
        _outlineRenderer.Show(channel, segments);
        return true;
    }

    /// <summary>
    /// Replace one channel with separately colored zone boundaries. As in the
    /// legacy batch API, blank/unknown GEOIDs are skipped and null/empty batches
    /// clear this channel once ready. Unavailable renderer/map leaves it intact.
    /// </summary>
    public bool SetZoneOutlines(ZoneOutlineChannel channel, IEnumerable<ZoneRiskOutlineRequest> requests)
    {
        if (!TryPrepareOutlineRequest(channel))
            return false;

        List<ZoneOutlineRenderer.ColoredSegment> segments = new();
        if (requests != null)
        {
            foreach (ZoneRiskOutlineRequest request in requests)
            {
                if (string.IsNullOrWhiteSpace(request.Geoid) ||
                    !TryGetBoundaryEdges(request.Geoid, out IReadOnlyList<ZoneBoundaryEdge> edges))
                {
                    continue;
                }

                AppendOutlineEdges(segments, edges, request.Color, request.Geoid);
            }
        }

        _outlineRenderer.Show(channel, segments);
        return true;
    }

    /// <summary>Replace one channel with cell outlines. Cell is absolute, not normalized.</summary>
    public bool SetTileOutlines(ZoneOutlineChannel channel, IEnumerable<TileOutlineRequest> requests)
    {
        if (!TryPrepareOutlineRequest(channel))
            return false;

        List<ZoneOutlineRenderer.ColoredSegment> segments = new();
        if (requests != null)
        {
            foreach (TileOutlineRequest request in requests)
            {
                Vector3Int cell = request.Cell;
                Vector3 bl = terrainTilemap.CellToWorld(cell);
                Vector3 br = terrainTilemap.CellToWorld(new Vector3Int(cell.x + 1, cell.y, cell.z));
                Vector3 tl = terrainTilemap.CellToWorld(new Vector3Int(cell.x, cell.y + 1, cell.z));
                Vector3 tr = terrainTilemap.CellToWorld(new Vector3Int(cell.x + 1, cell.y + 1, cell.z));

                segments.Add(new ZoneOutlineRenderer.ColoredSegment(new ZoneOutlineRenderer.Segment(bl, br), request.Color));
                segments.Add(new ZoneOutlineRenderer.ColoredSegment(new ZoneOutlineRenderer.Segment(br, tr), request.Color));
                segments.Add(new ZoneOutlineRenderer.ColoredSegment(new ZoneOutlineRenderer.Segment(tr, tl), request.Color));
                segments.Add(new ZoneOutlineRenderer.ColoredSegment(new ZoneOutlineRenderer.Segment(tl, bl), request.Color));
            }
        }

        _outlineRenderer.Show(channel, segments);
        return true;
    }

    /// <summary>Clear only this channel, including cached active counts. Does not require map readiness.</summary>
    public void ClearOutlines(ZoneOutlineChannel channel)
    {
        if (TryUseOutlineRenderer(channel))
            _outlineRenderer.Clear(channel);
    }

    /// <summary>
    /// Hide/restore one cached channel without clearing it. The explicit setting
    /// survives replacements and barrier hiding; existing interaction suppression still applies.
    /// </summary>
    public bool SetOutlineVisibility(ZoneOutlineChannel channel, bool visible)
    {
        if (!TryUseOutlineRenderer(channel))
            return false;

        _outlineRenderer.SetChannelVisibility(channel, visible);
        return true;
    }

    private bool TryPrepareOutlineRequest(ZoneOutlineChannel channel)
    {
        if (!TryUseOutlineRenderer(channel))
            return false;

        if (IsZoneLoaderReady() && EnsureZoneIndexReadyForExternalUse())
            return true;

        if (debugLogs)
            Debug.LogWarning("[ZoneController] Outline request rejected: zone map is not ready; cached outlines were preserved. Retry after map initialization.");
        return false;
    }

    private bool TryUseOutlineRenderer(ZoneOutlineChannel channel)
    {
        if (channel < ZoneOutlineChannel.Hover || channel > ZoneOutlineChannel.CommunicationZones)
        {
            Debug.LogWarning($"[ZoneController] Invalid outline channel: {channel}.");
            return false;
        }

        if (IsOutlineRenderingAvailable)
            return true;

        if (!_loggedMissingOutlineRenderer)
        {
            Debug.LogWarning("[ZoneController] Outline rendering is unavailable. Check the controller's assigned outline root and initialization; no renderer was created by this request.");
            _loggedMissingOutlineRenderer = true;
        }
        return false;
    }

    private void AppendOutlineEdges(
        List<ZoneOutlineRenderer.ColoredSegment> segments,
        IReadOnlyList<ZoneBoundaryEdge> edges,
        Color color,
        string geoid)
    {
        // Geometry already determines membership/exposed edges. Only restore the
        // legacy tile/down/up/left/right ordering so pool slots retain the same
        // creation-time appearance when Inspector settings change during play.
        _zoneIndex.TryGetZoneTiles(geoid, out HashSet<Vector2Int> tiles);
        Dictionary<Vector2Int, int> tileOrder = new();
        foreach (Vector2Int tile in tiles)
            tileOrder.Add(tile, tileOrder.Count);

        List<ZoneBoundaryEdge> orderedEdges = new(edges);
        orderedEdges.Sort((a, b) =>
        {
            int tileComparison = tileOrder[a.InteriorTile].CompareTo(tileOrder[b.InteriorTile]);
            return tileComparison != 0 ? tileComparison : OutlineEdgeOrder(a).CompareTo(OutlineEdgeOrder(b));
        });

        foreach (ZoneBoundaryEdge edge in orderedEdges)
        {
            Vector2Int a = edge.GridLine;
            Vector2Int b = edge.Orientation == ZoneBoundaryEdgeOrientation.X
                ? new Vector2Int(a.x, a.y + 1)
                : new Vector2Int(a.x + 1, a.y);
            Vector3 worldA = terrainTilemap.CellToWorld(NormalizedTileToCell(a));
            Vector3 worldB = terrainTilemap.CellToWorld(NormalizedTileToCell(b));
            segments.Add(new ZoneOutlineRenderer.ColoredSegment(new ZoneOutlineRenderer.Segment(worldA, worldB), color));
        }
    }

    private static int OutlineEdgeOrder(ZoneBoundaryEdge edge)
    {
        if (edge.Orientation == ZoneBoundaryEdgeOrientation.Y)
            return edge.GridLine.y == edge.InteriorTile.y ? 0 : 1;
        return edge.GridLine.x == edge.InteriorTile.x ? 2 : 3;
    }

    private bool EnsureOutlineRenderer()
    {
        if (_isDestroying)
            return false;

        if (_outlineRenderer != null)
            return IsOutlineRenderingAvailable;

        if (outlineRoot == null || outlineRoot.gameObject.scene != gameObject.scene)
        {
            if (!_loggedMissingOutlineRenderer)
            {
                Debug.LogWarning("[ZoneController] Assign an outline root in this scene. No rendering root or renderer was created.");
                _loggedMissingOutlineRenderer = true;
            }
            return false;
        }

        _outlineRenderer = new ZoneOutlineRenderer(outlineRoot);
        ConfigureOutlineRenderer();
        _outlineRenderer.ConfigureVisibility(_suppressHoverOnShow, _suppressPersistentOnShow);
        _loggedMissingOutlineRenderer = false;
        return true;
    }

    private void ConfigureOutlineRenderer()
    {
        // Existing pool slots retain creation-time settings, as before.
        _outlineRenderer.Configure(
            linePrefab, lineMaterial, lineColor, lineWidth, zOffset,
            sortingLayerName, sortingOrder);
    }

    // Transient interaction policy is independent of channel requested masks.
    // Cache it before Awake too, so input/controller Awake order does not matter.
    internal void SetOutlineVisibilityPolicy(bool suppressHoverOnShow, bool suppressPersistentOnShow)
    {
        if (_isDestroying)
            return;

        _suppressHoverOnShow = suppressHoverOnShow;
        _suppressPersistentOnShow = suppressPersistentOnShow;
        _outlineRenderer?.ConfigureVisibility(suppressHoverOnShow, suppressPersistentOnShow);
    }

    internal bool PersistentOutlinesVisible =>
        _outlineRenderer == null || _outlineRenderer.PersistentOutlinesVisible;

    internal void SetPersistentOutlineVisibility(bool visible)
    {
        _outlineRenderer?.SetPersistentVisibility(visible);
    }

    internal void ResetOutlineVisibilityPolicy()
    {
        SetOutlineVisibilityPolicy(false, false);
        if (_outlineRenderer == null || _outlineRenderer.IsDisposed)
            return;

        // Clear stale hover and release transient hiding without clearing cached
        // persistent counts or changing any channel's requested visibility.
        _outlineRenderer.Clear(ZoneOutlineChannel.Hover);
        _outlineRenderer.SetPersistentVisibility(true);
    }

    // Retained legacy raw-set/readiness fallbacks submit prepared world segments
    // without borrowing the renderer or imposing the normal zone-map guard.
    internal bool SetPreparedOutlineSegments(
        ZoneOutlineChannel channel,
        List<ZoneOutlineRenderer.ColoredSegment> segments)
    {
        if (!TryUseOutlineRenderer(channel))
            return false;

        _outlineRenderer.Show(channel, segments);
        return true;
    }

    private void OnDestroy()
    {
        _isDestroying = true;
        ZoneOutlineRenderer renderer = _outlineRenderer;
        _outlineRenderer = null;
        renderer?.Dispose();
    }
}
