using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Independent cached outline channels shared by legacy and controller callers.</summary>
public enum ZoneOutlineChannel
{
    Hover,
    BaselineRisk,
    LiveFloodRisk,
    ShelterCandidateZones,
    ShelterCandidateTiles,
    PlacedShelterZones,
    CommunicationZones
}

/// <summary>
/// Renders prepared world-space segments. The owner supplies appearance and
/// visibility decisions; this helper does not query gameplay or map data.
/// </summary>
internal sealed class ZoneOutlineRenderer : IDisposable
{
    internal struct Segment
    {
        public Vector3 a;
        public Vector3 b;

        public Segment(Vector3 a, Vector3 b)
        {
            this.a = a;
            this.b = b;
        }
    }

    internal struct ColoredSegment
    {
        public Segment segment;
        public Color color;

        public ColoredSegment(Segment segment, Color color)
        {
            this.segment = segment;
            this.color = color;
        }
    }

    private sealed class LinePool
    {
        public readonly List<LineRenderer> Lines = new();
        public int ActiveLineCount;
        public bool RequestedVisible = true;
    }

    private readonly Transform _parent;
    private readonly Dictionary<ZoneOutlineChannel, LinePool> _pools = new()
    {
        { ZoneOutlineChannel.Hover, new LinePool() },
        { ZoneOutlineChannel.BaselineRisk, new LinePool() },
        { ZoneOutlineChannel.LiveFloodRisk, new LinePool() },
        { ZoneOutlineChannel.ShelterCandidateZones, new LinePool() },
        { ZoneOutlineChannel.ShelterCandidateTiles, new LinePool() },
        { ZoneOutlineChannel.PlacedShelterZones, new LinePool() },
        { ZoneOutlineChannel.CommunicationZones, new LinePool() }
    };

    private LineRenderer _linePrefab;
    private Material _lineMaterial;
    private Color _lineColor;
    private float _lineWidth;
    private float _zOffset;
    private string _sortingLayerName;
    private int _sortingOrder;
    private Material _runtimeMaterial;
    private bool _disposed;
    private bool _suppressHoverOnShow;
    private bool _suppressPersistentOnShow;

    internal bool IsDisposed => _disposed;
    internal bool IsAvailable => !_disposed && _parent != null;
    internal bool PersistentOutlinesVisible { get; private set; } = true;

    internal ZoneOutlineRenderer(Transform parent)
    {
        _parent = parent;
    }

    internal void Configure(
        LineRenderer linePrefab,
        Material lineMaterial,
        Color lineColor,
        float lineWidth,
        float zOffset,
        string sortingLayerName,
        int sortingOrder)
    {
        _linePrefab = linePrefab;
        _lineMaterial = lineMaterial;
        _lineColor = lineColor;
        _lineWidth = lineWidth;
        _zOffset = zOffset;
        _sortingLayerName = sortingLayerName;
        _sortingOrder = sortingOrder;
    }

    // The scene owner supplies explicit suppression instructions; the renderer
    // does not discover input, barrier state, cameras, or gameplay components.
    internal void ConfigureVisibility(bool suppressHoverOnShow, bool suppressPersistentOnShow)
    {
        _suppressHoverOnShow = suppressHoverOnShow;
        _suppressPersistentOnShow = suppressPersistentOnShow;
    }

    internal void Show(ZoneOutlineChannel channel, List<Segment> segments, Color color)
    {
        if (!IsAvailable)
            return;

        if (segments == null)
        {
            _pools[channel].ActiveLineCount = 0;
            return;
        }

        List<ColoredSegment> coloredSegments = new(segments.Count);

        for (int i = 0; i < segments.Count; i++)
            coloredSegments.Add(new ColoredSegment(segments[i], color));

        Show(channel, coloredSegments);
    }

    internal void Show(ZoneOutlineChannel channel, List<ColoredSegment> segments)
    {
        if (!IsAvailable)
            return;

        LinePool pool = _pools[channel];
        pool.ActiveLineCount = segments != null ? segments.Count : 0;

        if (segments == null)
            return;

        EnsureLinePoolSize(pool.Lines, segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            LineRenderer lr = pool.Lines[i];
            ColoredSegment coloredSegment = segments[i];

            lr.gameObject.SetActive(pool.RequestedVisible);
            lr.positionCount = 2;
            lr.loop = false;
            ApplyRendererColor(lr, coloredSegment.color);

            Vector3 a = coloredSegment.segment.a;
            Vector3 b = coloredSegment.segment.b;

            a.z += _zOffset;
            b.z += _zOffset;

            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
        }

        for (int i = segments.Count; i < pool.Lines.Count; i++)
            pool.Lines[i].gameObject.SetActive(false);

        if (channel == ZoneOutlineChannel.Hover)
        {
            if (_suppressHoverOnShow)
                ApplyChannelVisibility(channel, false);
        }
        else
        {
            // Preserve the legacy show/restore timing, including its flag quirks.
            PersistentOutlinesVisible = true;
            if (_suppressPersistentOnShow)
                SetPersistentVisibility(false);
        }
    }

    internal void Clear(ZoneOutlineChannel channel)
    {
        _pools[channel].ActiveLineCount = 0;
        ApplyChannelVisibility(channel, false);
    }

    internal void SetChannelVisibility(ZoneOutlineChannel channel, bool visible)
    {
        _pools[channel].RequestedVisible = visible;
        bool allowed = channel == ZoneOutlineChannel.Hover
            ? !_suppressHoverOnShow
            : PersistentOutlinesVisible && !_suppressPersistentOnShow;
        ApplyChannelVisibility(channel, visible && allowed);
    }

    private void ApplyChannelVisibility(ZoneOutlineChannel channel, bool visible)
    {
        LinePool pool = _pools[channel];

        for (int i = 0; i < pool.Lines.Count; i++)
        {
            LineRenderer lr = pool.Lines[i];
            if (lr != null)
                lr.gameObject.SetActive(visible && pool.RequestedVisible && i < pool.ActiveLineCount);
        }
    }

    internal void SetPersistentVisibility(bool visible)
    {
        PersistentOutlinesVisible = visible;
        foreach (ZoneOutlineChannel channel in _pools.Keys)
        {
            if (channel != ZoneOutlineChannel.Hover)
                ApplyChannelVisibility(channel, visible);
        }
    }

    private void EnsureLinePoolSize(List<LineRenderer> linePool, int count)
    {
        while (linePool.Count < count)
            linePool.Add(CreateLineRenderer());
    }

    private LineRenderer CreateLineRenderer()
    {
        LineRenderer lr;

        if (_linePrefab != null)
        {
            lr = UnityEngine.Object.Instantiate(_linePrefab, _parent);
        }
        else
        {
            GameObject go = new GameObject("ZoneThinOutlineSegment");
            go.transform.SetParent(_parent, false);
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
        lr.widthMultiplier = _lineWidth;
        lr.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.sortingLayerName = _sortingLayerName;
        lr.sortingOrder = _sortingOrder;

        Material sharedMaterial = CreateMaterialInstance();
        if (sharedMaterial != null)
            lr.sharedMaterial = sharedMaterial;

        ApplyRendererColor(lr, _lineColor);
    }

    private Material CreateMaterialInstance()
    {
        if (_lineMaterial != null)
            return _lineMaterial;

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

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (LinePool pool in _pools.Values)
        {
            foreach (LineRenderer lr in pool.Lines)
            {
                if (lr != null)
                    DestroyOwnedObject(lr.gameObject);
            }

            pool.Lines.Clear();
            pool.ActiveLineCount = 0;
        }

        DestroyOwnedObject(_runtimeMaterial);
        _runtimeMaterial = null;
    }

    private static void DestroyOwnedObject(UnityEngine.Object ownedObject)
    {
        if (ownedObject == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(ownedObject);
        else
            UnityEngine.Object.DestroyImmediate(ownedObject);
    }
}
