#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class FloodImpactValidationWindow : EditorWindow
{
    private const string MenuPath = "Dev/Validation/Flood Impact Validator";
    private const double AutoRefreshSeconds = 1.0d;
    private const int MaxDisplayedSamples = 25;
    private const float DepthTolerance = 0.0001f;

    private FloodImpactController _floodImpactController;
    private WaterController _waterController;
    private ZoneController _zoneController;
    private TileMapData _tileMapData;

    private string[] _zoneOptions = Array.Empty<string>();
    private int _selectedZoneIndex = -1;
    private string _selectedGeoid = string.Empty;
    private float _floodDepthThreshold = 0.01f;
    private bool _autoRefresh;
    private double _nextAutoRefreshTime;
    private Vector2 _scroll;
    private Snapshot _snapshot;

    [MenuItem(MenuPath)]
    public static void Open()
    {
        GetWindow<FloodImpactValidationWindow>("Flood Impact Validator");
    }

    private void OnEnable()
    {
        AutoBindReferences();
        RefreshZoneOptions();
    }

    private void Update()
    {
        if (!_autoRefresh)
            return;

        if (EditorApplication.timeSinceStartup < _nextAutoRefreshTime)
            return;

        RefreshSnapshot();
        _nextAutoRefreshTime = EditorApplication.timeSinceStartup + AutoRefreshSeconds;
        Repaint();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Flood Impact Validator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Editor-only readout for comparing FloodImpactController results with independently sampled WaterController depths. It does not start, reset, step, or modify simulation state.",
            MessageType.Info);

        DrawReferences();
        DrawControls();
        DrawZoneSelection();
        DrawSnapshot();

        EditorGUILayout.EndScrollView();
    }

    private void DrawReferences()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _floodImpactController = (FloodImpactController)EditorGUILayout.ObjectField(
            "Flood Impact Controller",
            _floodImpactController,
            typeof(FloodImpactController),
            true);
        _waterController = (WaterController)EditorGUILayout.ObjectField(
            "Water Controller",
            _waterController,
            typeof(WaterController),
            true);
        _zoneController = (ZoneController)EditorGUILayout.ObjectField(
            "Zone Controller",
            _zoneController,
            typeof(ZoneController),
            true);
        _tileMapData = (TileMapData)EditorGUILayout.ObjectField(
            "Tile Map Data",
            _tileMapData,
            typeof(TileMapData),
            false);

        if (EditorGUI.EndChangeCheck())
        {
            RefreshZoneOptions();
            _snapshot = null;
        }

        if (GUILayout.Button("Find Scene References"))
        {
            AutoBindReferences(force: true);
            RefreshZoneOptions();
            _snapshot = null;
        }
    }

    private void DrawControls()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _floodDepthThreshold = Mathf.Max(0f, EditorGUILayout.FloatField("Flood Depth Threshold", _floodDepthThreshold));
        if (EditorGUI.EndChangeCheck() && _snapshot != null)
            _snapshot.ThresholdChangedAfterCapture = true;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Snapshot"))
            RefreshSnapshot();

        _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", "Button");
        if (_autoRefresh && _nextAutoRefreshTime <= 0d)
            _nextAutoRefreshTime = EditorApplication.timeSinceStartup + AutoRefreshSeconds;

        GUI.enabled = _snapshot != null;
        if (GUILayout.Button("Copy Summary to Clipboard"))
            EditorGUIUtility.systemCopyBuffer = BuildSummary(_snapshot);

        if (GUILayout.Button("Clear Snapshot"))
            _snapshot = null;
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawZoneSelection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Zone Selection", EditorStyles.boldLabel);

        if (_zoneOptions.Length == 0)
        {
            EditorGUILayout.LabelField("Available GEOIDs", "Unavailable");
            return;
        }

        int safeIndex = Mathf.Clamp(_selectedZoneIndex, 0, _zoneOptions.Length - 1);
        EditorGUI.BeginChangeCheck();
        safeIndex = EditorGUILayout.Popup("Selected GEOID", safeIndex, _zoneOptions);
        if (EditorGUI.EndChangeCheck())
        {
            _selectedZoneIndex = safeIndex;
            _selectedGeoid = _zoneOptions[safeIndex];
            RefreshSnapshot();
        }

        EditorGUILayout.LabelField("Available GEOID Count", _zoneOptions.Length.ToString());
    }

    private void DrawSnapshot()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Snapshot", EditorStyles.boldLabel);

        if (_snapshot == null)
        {
            EditorGUILayout.LabelField("Status", "No snapshot captured.");
            return;
        }

        if (_snapshot.ThresholdChangedAfterCapture)
            EditorGUILayout.HelpBox("The threshold field changed after this snapshot. Refresh to resample water metrics.", MessageType.Warning);

        EditorGUILayout.LabelField("Captured", _snapshot.CapturedAt.ToString("HH:mm:ss"));
        EditorGUILayout.LabelField("Selected GEOID", EmptyAsUnavailable(_snapshot.Geoid));
        EditorGUILayout.LabelField("Normalized Tile Count", _snapshot.ZoneTileCount >= 0 ? _snapshot.ZoneTileCount.ToString() : "Unavailable");
        EditorGUILayout.LabelField("Tilemap Origin", _snapshot.HasTileOrigin ? _snapshot.TileOrigin.ToString() : "Unavailable");
        EditorGUILayout.LabelField("WaterController Initialized", _snapshot.WaterInitialized ? "Yes" : "No");
        EditorGUILayout.LabelField("Snapshot Threshold", _snapshot.SampleThreshold.ToString("0.####"));
        EditorGUILayout.LabelField("Controller Flood Threshold", _snapshot.HasControllerThreshold ? _snapshot.ControllerThreshold.ToString("0.####") : "Unavailable");

        DrawWaterMetrics();
        DrawImpactMetrics();
        DrawValidationChecks();
        DrawSampleTiles();
    }

    private void DrawWaterMetrics()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Independently Sampled Water Metrics", EditorStyles.boldLabel);

        if (!_snapshot.WaterMetrics.Available)
        {
            EditorGUILayout.LabelField("Status", "Unavailable");
            return;
        }

        WaterMetrics metrics = _snapshot.WaterMetrics;
        EditorGUILayout.LabelField("Minimum Depth", FormatFloat(metrics.MinDepth));
        EditorGUILayout.LabelField("Maximum Depth", FormatFloat(metrics.MaxDepth));
        EditorGUILayout.LabelField("Average Depth, All Zone Tiles", FormatFloat(metrics.AverageAllTilesDepth));
        EditorGUILayout.LabelField("Positive Water Tiles", metrics.PositiveDepthCount.ToString());
        EditorGUILayout.LabelField("Tiles Meeting Threshold", metrics.ThresholdDepthCount.ToString());
        EditorGUILayout.LabelField("Average Depth, Threshold Tiles", FormatFloat(metrics.AverageThresholdDepth));
        EditorGUILayout.LabelField("Invalid Depth Samples", metrics.InvalidDepthCount.ToString());
    }

    private void DrawImpactMetrics()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("FloodImpactController Reported Metrics", EditorStyles.boldLabel);

        if (!_snapshot.HasImpactResult)
        {
            EditorGUILayout.LabelField("Result", "Unavailable");
            return;
        }

        ZoneFloodImpactResult result = _snapshot.ImpactResult;
        EditorGUILayout.LabelField("Total Zone Population", result.rawPopulation.ToString());
        EditorGUILayout.LabelField("Exposed Population", FormatFloat(result.exposedPopulationAfterMitigation));
        EditorGUILayout.LabelField("Affected Population", FormatFloat(result.affectedPopulation));
        EditorGUILayout.LabelField("Flooded Tile Count", result.floodedTileCount.ToString());
        EditorGUILayout.LabelField("Average Flood Depth", FormatFloat(result.averageWaterDepth));
        EditorGUILayout.LabelField("Estimated Damage", FormatFloat(result.estimatedDamage));
        EditorGUILayout.LabelField("Risk Score", FormatFloat(result.liveFloodRisk));
        EditorGUILayout.LabelField("Risk Level", result.riskLevel.ToString());
    }

    private void DrawValidationChecks()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation Checks", EditorStyles.boldLabel);

        foreach (ValidationCheck check in _snapshot.Checks)
        {
            Color previous = GUI.color;
            GUI.color = GetStatusColor(check.Status);
            EditorGUILayout.LabelField(check.Label, $"{FormatStatus(check.Status)}: {check.Detail}");
            GUI.color = previous;
        }
    }

    private void DrawSampleTiles()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sample Tiles", EditorStyles.boldLabel);

        if (_snapshot.Samples.Count == 0)
        {
            EditorGUILayout.LabelField("Samples", "Unavailable");
            return;
        }

        int count = Mathf.Min(MaxDisplayedSamples, _snapshot.Samples.Count);
        EditorGUILayout.LabelField("Displayed", $"{count} of {_snapshot.Samples.Count} sorted tiles");

        for (int i = 0; i < count; i++)
        {
            TileSample sample = _snapshot.Samples[i];
            EditorGUILayout.LabelField(
                $"#{i + 1}",
                $"Normalized={sample.NormalizedTile} Cell={sample.TilemapCell} Depth={FormatFloat(sample.Depth)}");
        }
    }

    private void RefreshSnapshot()
    {
        AutoBindReferences();
        RefreshZoneOptions();

        Snapshot snapshot = new Snapshot
        {
            CapturedAt = DateTime.Now,
            Geoid = _selectedGeoid,
            SampleThreshold = _floodDepthThreshold,
            HasTileOrigin = _zoneController != null,
            TileOrigin = _zoneController != null ? _zoneController.TileOrigin : Vector2Int.zero,
            WaterInitialized = _waterController != null && _waterController.IsInitialized,
        };

        if (_zoneController == null || string.IsNullOrEmpty(_selectedGeoid))
        {
            snapshot.ZoneTileCount = -1;
            snapshot.Checks.Add(new ValidationCheck("Zone exists and has tiles", CheckStatus.Unavailable, "ZoneController or selected GEOID is unavailable."));
            _snapshot = snapshot;
            return;
        }

        bool hasZone = _zoneController.TryGetZoneTiles(_selectedGeoid, out HashSet<Vector2Int> zoneTiles);
        snapshot.ZoneTileCount = hasZone && zoneTiles != null ? zoneTiles.Count : 0;

        if (TryGetControllerFloodThreshold(out float controllerThreshold))
        {
            snapshot.HasControllerThreshold = true;
            snapshot.ControllerThreshold = controllerThreshold;
        }

        if (hasZone && zoneTiles != null && zoneTiles.Count > 0)
        {
            List<Vector2Int> orderedTiles = GetOrderedTiles(zoneTiles);
            snapshot.WaterMetrics = SampleWaterMetrics(orderedTiles, snapshot);
            snapshot.Samples = BuildSamples(orderedTiles);
        }
        else
        {
            snapshot.WaterMetrics = WaterMetrics.Unavailable();
        }

        if (_floodImpactController != null &&
            _floodImpactController.TryGetFloodImpactResult(_selectedGeoid, out ZoneFloodImpactResult result))
        {
            snapshot.HasImpactResult = true;
            snapshot.ImpactResult = result;
        }

        snapshot.Checks = BuildChecks(snapshot, zoneTiles);
        _snapshot = snapshot;
    }

    private void RefreshZoneOptions()
    {
        if (_zoneController == null)
        {
            _zoneOptions = Array.Empty<string>();
            _selectedZoneIndex = -1;
            _selectedGeoid = string.Empty;
            return;
        }

        IReadOnlyDictionary<string, HashSet<Vector2Int>> allZones = _zoneController.GetAllZoneTiles();
        if (allZones == null || allZones.Count == 0)
        {
            _zoneOptions = Array.Empty<string>();
            _selectedZoneIndex = -1;
            _selectedGeoid = string.Empty;
            return;
        }

        List<string> geoids = new List<string>();
        foreach (string geoid in allZones.Keys)
        {
            if (!string.IsNullOrEmpty(geoid))
                geoids.Add(geoid);
        }

        geoids.Sort(StringComparer.Ordinal);
        _zoneOptions = geoids.ToArray();

        int existingIndex = Array.IndexOf(_zoneOptions, _selectedGeoid);
        if (existingIndex >= 0)
        {
            _selectedZoneIndex = existingIndex;
            return;
        }

        _selectedZoneIndex = _zoneOptions.Length > 0 ? 0 : -1;
        _selectedGeoid = _selectedZoneIndex >= 0 ? _zoneOptions[_selectedZoneIndex] : string.Empty;
    }

    private WaterMetrics SampleWaterMetrics(List<Vector2Int> orderedTiles, Snapshot snapshot)
    {
        if (_waterController == null || !_waterController.IsInitialized)
            return WaterMetrics.Unavailable();

        if (orderedTiles == null || orderedTiles.Count == 0)
            return WaterMetrics.Unavailable();

        WaterMetrics metrics = new WaterMetrics
        {
            Available = true,
            MinDepth = float.PositiveInfinity,
        };

        foreach (Vector2Int normalizedTile in orderedTiles)
        {
            float depth = SampleDepth(normalizedTile);
            bool valid = IsFiniteNonNegative(depth);

            if (!valid)
            {
                metrics.InvalidDepthCount++;
                continue;
            }

            metrics.MinDepth = Mathf.Min(metrics.MinDepth, depth);
            metrics.MaxDepth = Mathf.Max(metrics.MaxDepth, depth);
            metrics.AllDepthSum += depth;

            if (depth > 0f)
                metrics.PositiveDepthCount++;

            if (depth >= snapshot.SampleThreshold)
            {
                metrics.ThresholdDepthCount++;
                metrics.ThresholdDepthSum += depth;
            }
        }

        int validCount = orderedTiles.Count - metrics.InvalidDepthCount;
        metrics.MinDepth = validCount > 0 ? metrics.MinDepth : 0f;
        metrics.AverageAllTilesDepth = validCount > 0 ? metrics.AllDepthSum / validCount : 0f;
        metrics.AverageThresholdDepth = metrics.ThresholdDepthCount > 0
            ? metrics.ThresholdDepthSum / metrics.ThresholdDepthCount
            : 0f;

        return metrics;
    }

    private List<TileSample> BuildSamples(List<Vector2Int> orderedTiles)
    {
        List<TileSample> samples = new List<TileSample>();

        if (_zoneController == null || _waterController == null || !_waterController.IsInitialized)
            return samples;

        foreach (Vector2Int normalizedTile in orderedTiles)
        {
            Vector3Int cell = _zoneController.NormalizedTileToCell(normalizedTile);
            samples.Add(new TileSample
            {
                NormalizedTile = normalizedTile,
                TilemapCell = new Vector2Int(cell.x, cell.y),
                Depth = _waterController.GetWaterDepth(new Vector2Int(cell.x, cell.y)),
            });
        }

        return samples;
    }

    private List<ValidationCheck> BuildChecks(Snapshot snapshot, HashSet<Vector2Int> zoneTiles)
    {
        List<ValidationCheck> checks = new List<ValidationCheck>();

        bool hasZoneTiles = zoneTiles != null && zoneTiles.Count > 0;
        checks.Add(new ValidationCheck(
            "Zone exists and has tiles",
            hasZoneTiles ? CheckStatus.Pass : CheckStatus.Fail,
            hasZoneTiles ? $"{zoneTiles.Count} normalized tiles." : "Selected GEOID has no zone tiles."));

        checks.Add(BuildCoordinateCheck(zoneTiles));
        checks.Add(BuildWaterDepthCheck(snapshot));
        checks.Add(BuildFloodedTileLimitCheck(snapshot));
        checks.Add(BuildExposedPopulationCheck(snapshot));
        checks.Add(BuildFloodedTileComparisonCheck(snapshot));
        checks.Add(BuildAverageDepthComparisonCheck(snapshot));

        return checks;
    }

    private ValidationCheck BuildCoordinateCheck(HashSet<Vector2Int> zoneTiles)
    {
        if (_zoneController == null || zoneTiles == null || zoneTiles.Count == 0)
            return new ValidationCheck("Normalized tile coordinates convert to valid map coordinates", CheckStatus.Unavailable, "Zone data is unavailable.");

        if (_tileMapData == null || _tileMapData.N <= 0)
            return new ValidationCheck("Normalized tile coordinates convert to valid map coordinates", CheckStatus.Unavailable, "TileMapData or N is unavailable.");

        int invalidCount = 0;

        foreach (Vector2Int tile in zoneTiles)
        {
            Vector3Int cell = _zoneController.NormalizedTileToCell(tile);
            Vector2Int roundTrip = _zoneController.CellToNormalizedTile(cell);
            bool insideLegacyMap = tile.x >= 0 && tile.y >= 0 && tile.x < _tileMapData.N && tile.y < _tileMapData.N;

            if (!insideLegacyMap || roundTrip != tile)
                invalidCount++;
        }

        return new ValidationCheck(
            "Normalized tile coordinates convert to valid map coordinates",
            invalidCount == 0 ? CheckStatus.Pass : CheckStatus.Fail,
            invalidCount == 0 ? $"All tiles are within TileMapData.N={_tileMapData.N} and round-trip through ZoneController." : $"{invalidCount} invalid tile conversions.");
    }

    private ValidationCheck BuildWaterDepthCheck(Snapshot snapshot)
    {
        if (!snapshot.WaterMetrics.Available)
            return new ValidationCheck("Water depths are finite and nonnegative", CheckStatus.Unavailable, "WaterController is missing or not initialized.");

        return new ValidationCheck(
            "Water depths are finite and nonnegative",
            snapshot.WaterMetrics.InvalidDepthCount == 0 ? CheckStatus.Pass : CheckStatus.Fail,
            snapshot.WaterMetrics.InvalidDepthCount == 0 ? "All sampled depths are finite and nonnegative." : $"{snapshot.WaterMetrics.InvalidDepthCount} invalid depths.");
    }

    private ValidationCheck BuildFloodedTileLimitCheck(Snapshot snapshot)
    {
        if (!snapshot.HasImpactResult)
            return new ValidationCheck("Flooded tile count does not exceed zone tile count", CheckStatus.Unavailable, "FloodImpactController result is unavailable.");

        bool valid = snapshot.ImpactResult.floodedTileCount >= 0 &&
                     snapshot.ImpactResult.floodedTileCount <= snapshot.ZoneTileCount;

        return new ValidationCheck(
            "Flooded tile count does not exceed zone tile count",
            valid ? CheckStatus.Pass : CheckStatus.Fail,
            $"Reported={snapshot.ImpactResult.floodedTileCount}, ZoneTiles={snapshot.ZoneTileCount}.");
    }

    private ValidationCheck BuildExposedPopulationCheck(Snapshot snapshot)
    {
        if (!snapshot.HasImpactResult)
            return new ValidationCheck("Exposed population does not exceed total population", CheckStatus.Unavailable, "FloodImpactController result is unavailable.");

        bool valid = snapshot.ImpactResult.exposedPopulationAfterMitigation <= snapshot.ImpactResult.rawPopulation + 0.01f;

        return new ValidationCheck(
            "Exposed population does not exceed total population",
            valid ? CheckStatus.Pass : CheckStatus.Fail,
            $"Exposed={FormatFloat(snapshot.ImpactResult.exposedPopulationAfterMitigation)}, Total={snapshot.ImpactResult.rawPopulation}.");
    }

    private ValidationCheck BuildFloodedTileComparisonCheck(Snapshot snapshot)
    {
        if (!CanCompareImpactToSample(snapshot, out string unavailableReason))
            return new ValidationCheck("Reported flooded tile count matches independently sampled water data", CheckStatus.Unavailable, unavailableReason);

        bool matches = snapshot.ImpactResult.floodedTileCount == snapshot.WaterMetrics.ThresholdDepthCount;

        return new ValidationCheck(
            "Reported flooded tile count matches independently sampled water data",
            matches ? CheckStatus.Pass : CheckStatus.Fail,
            $"Reported={snapshot.ImpactResult.floodedTileCount}, Sampled={snapshot.WaterMetrics.ThresholdDepthCount}.");
    }

    private ValidationCheck BuildAverageDepthComparisonCheck(Snapshot snapshot)
    {
        if (!CanCompareImpactToSample(snapshot, out string unavailableReason))
            return new ValidationCheck("Reported average depth matches independently sampled depth", CheckStatus.Unavailable, unavailableReason);

        bool matches = Approximately(snapshot.ImpactResult.averageWaterDepth, snapshot.WaterMetrics.AverageThresholdDepth);

        return new ValidationCheck(
            "Reported average depth matches independently sampled depth",
            matches ? CheckStatus.Pass : CheckStatus.Fail,
            $"Reported={FormatFloat(snapshot.ImpactResult.averageWaterDepth)}, SampledFloodedOnly={FormatFloat(snapshot.WaterMetrics.AverageThresholdDepth)}.");
    }

    private bool CanCompareImpactToSample(Snapshot snapshot, out string unavailableReason)
    {
        if (!snapshot.HasImpactResult)
        {
            unavailableReason = "FloodImpactController result is unavailable.";
            return false;
        }

        if (!snapshot.WaterMetrics.Available)
        {
            unavailableReason = "Independent water sampling is unavailable.";
            return false;
        }

        if (!snapshot.HasControllerThreshold)
        {
            unavailableReason = "FloodImpactController flood threshold is not readable through serialized data.";
            return false;
        }

        if (!Approximately(snapshot.SampleThreshold, snapshot.ControllerThreshold))
        {
            unavailableReason = $"Thresholds differ. Snapshot={FormatFloat(snapshot.SampleThreshold)}, Controller={FormatFloat(snapshot.ControllerThreshold)}.";
            return false;
        }

        unavailableReason = string.Empty;
        return true;
    }

    private float SampleDepth(Vector2Int normalizedTile)
    {
        if (_zoneController == null || _waterController == null)
            return 0f;

        Vector3Int cell = _zoneController.NormalizedTileToCell(normalizedTile);
        return _waterController.GetWaterDepth(new Vector2Int(cell.x, cell.y));
    }

    private void AutoBindReferences(bool force = false)
    {
        if (force || _floodImpactController == null)
            _floodImpactController = FindFirstObjectByType<FloodImpactController>();

        if (force || _waterController == null)
            _waterController = FindFirstObjectByType<WaterController>();

        if (force || _zoneController == null)
            _zoneController = FindFirstObjectByType<ZoneController>();

        if (force || _tileMapData == null)
        {
            TileMapData[] allMapData = Resources.FindObjectsOfTypeAll<TileMapData>();
            _tileMapData = allMapData != null && allMapData.Length > 0 ? allMapData[0] : null;
        }
    }

    private bool TryGetControllerFloodThreshold(out float threshold)
    {
        threshold = 0f;

        if (_floodImpactController == null)
            return false;

        SerializedObject serializedObject = new SerializedObject(_floodImpactController);
        SerializedProperty thresholdProperty = serializedObject.FindProperty("floodedDepthThreshold");

        if (thresholdProperty == null || thresholdProperty.propertyType != SerializedPropertyType.Float)
            return false;

        threshold = thresholdProperty.floatValue;
        return true;
    }

    private string BuildSummary(Snapshot snapshot)
    {
        if (snapshot == null)
            return "Flood Impact Validator: No snapshot captured.";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Flood Impact Validator Snapshot");
        builder.AppendLine($"Captured: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Play Mode: {EditorApplication.isPlaying}");
        builder.AppendLine($"GEOID: {EmptyAsUnavailable(snapshot.Geoid)}");
        builder.AppendLine($"Zone Tiles: {(snapshot.ZoneTileCount >= 0 ? snapshot.ZoneTileCount.ToString() : "Unavailable")}");
        builder.AppendLine($"Tile Origin: {(snapshot.HasTileOrigin ? snapshot.TileOrigin.ToString() : "Unavailable")}");
        builder.AppendLine($"Water Initialized: {snapshot.WaterInitialized}");
        builder.AppendLine($"Snapshot Threshold: {FormatFloat(snapshot.SampleThreshold)}");
        builder.AppendLine($"Controller Threshold: {(snapshot.HasControllerThreshold ? FormatFloat(snapshot.ControllerThreshold) : "Unavailable")}");

        if (snapshot.WaterMetrics.Available)
        {
            builder.AppendLine($"Sampled Depth Min/Max/AvgAll: {FormatFloat(snapshot.WaterMetrics.MinDepth)} / {FormatFloat(snapshot.WaterMetrics.MaxDepth)} / {FormatFloat(snapshot.WaterMetrics.AverageAllTilesDepth)}");
            builder.AppendLine($"Sampled Positive/Threshold Tiles: {snapshot.WaterMetrics.PositiveDepthCount} / {snapshot.WaterMetrics.ThresholdDepthCount}");
        }
        else
        {
            builder.AppendLine("Sampled Water Metrics: Unavailable");
        }

        if (snapshot.HasImpactResult)
        {
            builder.AppendLine($"Impact Population Total/Exposed: {snapshot.ImpactResult.rawPopulation} / {FormatFloat(snapshot.ImpactResult.exposedPopulationAfterMitigation)}");
            builder.AppendLine($"Impact Flooded/AverageDepth/Damage/Risk: {snapshot.ImpactResult.floodedTileCount} / {FormatFloat(snapshot.ImpactResult.averageWaterDepth)} / {FormatFloat(snapshot.ImpactResult.estimatedDamage)} / {FormatFloat(snapshot.ImpactResult.liveFloodRisk)}");
        }
        else
        {
            builder.AppendLine("Impact Result: Unavailable");
        }

        builder.AppendLine("Checks:");
        foreach (ValidationCheck check in snapshot.Checks)
            builder.AppendLine($"- {FormatStatus(check.Status)}: {check.Label} ({check.Detail})");

        return builder.ToString();
    }

    private static List<Vector2Int> GetOrderedTiles(HashSet<Vector2Int> zoneTiles)
    {
        List<Vector2Int> orderedTiles = new List<Vector2Int>(zoneTiles);
        orderedTiles.Sort((a, b) =>
        {
            int xCompare = a.x.CompareTo(b.x);
            return xCompare != 0 ? xCompare : a.y.CompareTo(b.y);
        });

        return orderedTiles;
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }

    private static bool Approximately(float a, float b)
    {
        float tolerance = Mathf.Max(DepthTolerance, Mathf.Abs(b) * 0.001f);
        return Mathf.Abs(a - b) <= tolerance;
    }

    private static string FormatFloat(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? "Invalid" : value.ToString("0.####");
    }

    private static string EmptyAsUnavailable(string value)
    {
        return string.IsNullOrEmpty(value) ? "Unavailable" : value;
    }

    private static Color GetStatusColor(CheckStatus status)
    {
        switch (status)
        {
            case CheckStatus.Pass:
                return new Color(0.3f, 0.75f, 0.35f);
            case CheckStatus.Fail:
                return new Color(1f, 0.35f, 0.25f);
            default:
                return new Color(0.9f, 0.75f, 0.25f);
        }
    }

    private static string FormatStatus(CheckStatus status)
    {
        switch (status)
        {
            case CheckStatus.Pass:
                return "PASS";
            case CheckStatus.Fail:
                return "FAIL";
            default:
                return "UNAVAILABLE";
        }
    }

    private enum CheckStatus
    {
        Pass,
        Fail,
        Unavailable,
    }

    private readonly struct ValidationCheck
    {
        public ValidationCheck(string label, CheckStatus status, string detail)
        {
            Label = label;
            Status = status;
            Detail = detail;
        }

        public string Label { get; }
        public CheckStatus Status { get; }
        public string Detail { get; }
    }

    private sealed class Snapshot
    {
        public DateTime CapturedAt;
        public string Geoid;
        public int ZoneTileCount = -1;
        public bool HasTileOrigin;
        public Vector2Int TileOrigin;
        public bool WaterInitialized;
        public float SampleThreshold;
        public bool HasControllerThreshold;
        public float ControllerThreshold;
        public bool HasImpactResult;
        public ZoneFloodImpactResult ImpactResult;
        public WaterMetrics WaterMetrics = WaterMetrics.Unavailable();
        public List<TileSample> Samples = new List<TileSample>();
        public List<ValidationCheck> Checks = new List<ValidationCheck>();
        public bool ThresholdChangedAfterCapture;
    }

    private struct WaterMetrics
    {
        public bool Available;
        public int PositiveDepthCount;
        public int ThresholdDepthCount;
        public int InvalidDepthCount;
        public float MinDepth;
        public float MaxDepth;
        public float AllDepthSum;
        public float ThresholdDepthSum;
        public float AverageAllTilesDepth;
        public float AverageThresholdDepth;

        public static WaterMetrics Unavailable()
        {
            return new WaterMetrics { Available = false };
        }
    }

    private struct TileSample
    {
        public Vector2Int NormalizedTile;
        public Vector2Int TilemapCell;
        public float Depth;
    }
}
#endif
