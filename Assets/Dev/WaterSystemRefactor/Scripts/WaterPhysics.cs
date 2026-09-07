using UnityEngine;

/// <summary>
/// Performs the Water System's step-by-step flow calculations against runtime state.
/// It has no scene or UI responsibilities and reads barrier behavior from the concrete Dev barrier grid.
/// </summary>
public sealed class WaterPhysics
{
    private readonly MapAccessor _map;
    private readonly WaterPhysicsBarrier _barrierGrid;

    private WaterState _state;
    private WaterSimulationSettings _settings;
    private float _spreadTimer;
    private int _stepIndex;

    public WaterPhysics(MapAccessor map, WaterPhysicsBarrier barrierGrid)
    {
        _map = map;
        _barrierGrid = barrierGrid;
    }

    public void Initialize(WaterState state, WaterSimulationSettings settings)
    {
        _state = state;
        _settings = settings != null ? settings.Clone() : new WaterSimulationSettings();
        _settings.Sanitize();
        _stepIndex = 0;
        _spreadTimer = 0f;

        ClearFlow();
        ConfigureBoundaryGhosts();
    }

    public void Reconfigure(WaterSimulationSettings settings)
    {
        _settings = settings != null ? settings.Clone() : new WaterSimulationSettings();
        _settings.Sanitize();
        ConfigureBoundaryGhosts();
    }

    /// <summary>Configures a cloned projection state without clearing its live flow history.</summary>
    public void InitializeProjection(WaterState state, WaterSimulationSettings settings)
    {
        InitializeProjection(state, settings, 0f);
    }

    /// <summary>Configures a cloned projection state while preserving its spread-gate timer.</summary>
    public void InitializeProjection(
        WaterState state,
        WaterSimulationSettings settings,
        float spreadTimer)
    {
        _state = state;
        _settings = settings != null ? settings.Clone() : new WaterSimulationSettings();
        _settings.Sanitize();
        _stepIndex = 0;
        _spreadTimer = !float.IsNaN(spreadTimer) && !float.IsInfinity(spreadTimer)
            ? Mathf.Max(0f, spreadTimer)
            : 0f;
        ConfigureBoundaryGhosts();
    }

    internal WaterPhysics CloneForState(WaterState state, WaterPhysicsBarrier barrierGrid)
    {
        WaterPhysics clone = new WaterPhysics(_map, barrierGrid);
        clone._state = state;
        clone._settings = _settings != null ? _settings.Clone() : new WaterSimulationSettings();
        clone._spreadTimer = _spreadTimer;
        clone._stepIndex = _stepIndex;
        return clone;
    }

    public float GetSimulationStepDuration(WaterModifierSnapshot modifiers)
    {
        modifiers.Sanitize();
        return Mathf.Max(0.001f, _settings.dt * modifiers.EventPacing);
    }

    public void ApplyInitialSources(WaterSourceSpec[] sources, WaterModifierSnapshot modifiers)
    {
        if (_state == null)
            return;

        modifiers.Sanitize();

        if (sources == null || sources.Length == 0)
            return;

        foreach (WaterSourceSpec source in sources)
            ApplySource(source, modifiers, 0f, false);
    }

    public void InitializeActiveRegion()
    {
        if (_state == null)
            return;

        bool[,] active = new bool[_state.GridWidth, _state.GridHeight];

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                active[x, y] = _state.HasMapCellAtSim(x, y)
                    && _state.Water[x, y] > _settings.expandFromWaterThreshold;
            }
        }

        _state.ReplaceActiveGrid(active);
        _spreadTimer = 0f;

        if (_settings.useSpreadGating && _settings.expandOnceImmediatelyOnStart)
            ExpandActiveRegion();
    }

    public void TickSpreadGate(float deltaTime)
    {
        if (_state == null || !_settings.useSpreadGating)
            return;

        _spreadTimer += Mathf.Max(0f, deltaTime);
        if (_spreadTimer < _settings.spreadInterval)
            return;

        _spreadTimer = 0f;
        ExpandActiveRegion();
    }

    public WaterStepSummary Step(WaterSourceSpec[] continuousSources, WaterModifierSnapshot modifiers)
    {
        if (_state == null)
            return default;

        modifiers.Sanitize();

        return Step(continuousSources, modifiers, GetSimulationStepDuration(modifiers));
    }

    public WaterStepSummary Step(
        WaterSourceSpec[] continuousSources,
        WaterModifierSnapshot modifiers,
        float simulatedDeltaTime)
    {
        if (_state == null)
            return default;

        modifiers.Sanitize();

        float dt = Mathf.Max(0.0001f, simulatedDeltaTime);
        ApplyBoundarySources(dt);
        ApplyContinuousSources(continuousSources, modifiers, dt);

        AccelerateFlows(dt, modifiers);
        ScaleOutflows(dt);
        UpdateWaterDepths(dt, modifiers);
        ApplyBoundaryWallSeepage(dt);
        ApplyBoundarySinks();
        KeepBoundaryDry();
        TickSpreadGate(dt);

        _stepIndex++;
        return BuildSummary(dt);
    }

    private void ClearFlow()
    {
        for (int y = 0; y < _state.GridHeight; y++)
        {
            for (int x = 0; x < _state.GridWidth; x++)
            {
                _state.FlowX[x, y] = 0f;
                _state.FlowY[x, y] = 0f;
            }
        }
    }

    private void ConfigureBoundaryGhosts()
    {
        float maxTerrain = 0f;
        bool foundTerrain = false;

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasMapCellAtSim(x, y))
                    continue;

                maxTerrain = foundTerrain ? Mathf.Max(maxTerrain, _state.Terrain[x, y]) : _state.Terrain[x, y];
                foundTerrain = true;
            }
        }

        float boundaryHeight = maxTerrain + _settings.boundaryHeightPadding;

        ClearBoundaryGhosts();

        if (_settings.GetBoundary(WaterBoundarySide.South).mode == WaterBoundaryMode.Wall)
        {
            for (int x = 0; x < _state.GridWidth; x++)
                _state.Terrain[x, 0] = boundaryHeight;
        }

        if (_settings.GetBoundary(WaterBoundarySide.North).mode == WaterBoundaryMode.Wall)
        {
            for (int x = 0; x < _state.GridWidth; x++)
                _state.Terrain[x, _state.GridHeight - 1] = boundaryHeight;
        }

        if (_settings.GetBoundary(WaterBoundarySide.West).mode == WaterBoundaryMode.Wall)
        {
            for (int y = 0; y < _state.GridHeight; y++)
                _state.Terrain[0, y] = boundaryHeight;
        }

        if (_settings.GetBoundary(WaterBoundarySide.East).mode == WaterBoundaryMode.Wall)
        {
            for (int y = 0; y < _state.GridHeight; y++)
                _state.Terrain[_state.GridWidth - 1, y] = boundaryHeight;
        }
    }

    private void ClearBoundaryGhosts()
    {
        for (int x = 0; x < _state.GridWidth; x++)
        {
            _state.Terrain[x, 0] = 0f;
            _state.Terrain[x, _state.GridHeight - 1] = 0f;
            _state.Water[x, 0] = 0f;
            _state.Water[x, _state.GridHeight - 1] = 0f;
        }

        for (int y = 0; y < _state.GridHeight; y++)
        {
            _state.Terrain[0, y] = 0f;
            _state.Terrain[_state.GridWidth - 1, y] = 0f;
            _state.Water[0, y] = 0f;
            _state.Water[_state.GridWidth - 1, y] = 0f;
        }
    }

    private void ApplyBoundarySources(float dt)
    {
        ApplyBoundarySource(WaterBoundarySide.North, dt);
        ApplyBoundarySource(WaterBoundarySide.East, dt);
        ApplyBoundarySource(WaterBoundarySide.South, dt);
        ApplyBoundarySource(WaterBoundarySide.West, dt);
    }

    private void ApplyBoundarySource(WaterBoundarySide side, float dt)
    {
        WaterBoundarySettings boundary = _settings.GetBoundary(side);
        if (boundary.mode != WaterBoundaryMode.Source || boundary.sourceDepthPerSecond <= 0f)
            return;

        float depth = boundary.sourceDepthPerSecond * dt;
        if (float.IsNaN(depth) || depth <= 0f)
            return;
        if (float.IsInfinity(depth))
            depth = _settings.maxWaterDepth > 0f ? _settings.maxWaterDepth : float.MaxValue;
        switch (side)
        {
            case WaterBoundarySide.North:
                for (int x = 1; x <= _state.Width; x++)
                    AddOrSetWaterAtSim(x, _state.Height, depth, true);
                break;
            case WaterBoundarySide.East:
                for (int y = 1; y <= _state.Height; y++)
                    AddOrSetWaterAtSim(_state.Width, y, depth, true);
                break;
            case WaterBoundarySide.South:
                for (int x = 1; x <= _state.Width; x++)
                    AddOrSetWaterAtSim(x, 1, depth, true);
                break;
            case WaterBoundarySide.West:
                for (int y = 1; y <= _state.Height; y++)
                    AddOrSetWaterAtSim(1, y, depth, true);
                break;
        }
    }

    private void ApplyBoundarySinks()
    {
        ClearBoundarySink(WaterBoundarySide.North);
        ClearBoundarySink(WaterBoundarySide.East);
        ClearBoundarySink(WaterBoundarySide.South);
        ClearBoundarySink(WaterBoundarySide.West);
    }

    private void ApplyBoundaryWallSeepage(float dt)
    {
        DrainBoundaryWall(WaterBoundarySide.North, dt);
        DrainBoundaryWall(WaterBoundarySide.East, dt);
        DrainBoundaryWall(WaterBoundarySide.South, dt);
        DrainBoundaryWall(WaterBoundarySide.West, dt);
    }

    private void DrainBoundaryWall(WaterBoundarySide side, float dt)
    {
        WaterBoundarySettings boundary = _settings.GetBoundary(side);
        if (boundary.mode != WaterBoundaryMode.Wall || boundary.seepageDepthPerSecond <= 0f)
            return;

        float depth = boundary.seepageDepthPerSecond * dt;
        if (float.IsNaN(depth) || depth <= 0f)
            return;
        if (float.IsInfinity(depth))
            depth = _settings.maxWaterDepth > 0f ? _settings.maxWaterDepth : float.MaxValue;

        switch (side)
        {
            case WaterBoundarySide.North:
                for (int x = 1; x <= _state.Width; x++)
                    DrainWaterAtSim(x, _state.Height, depth);
                break;
            case WaterBoundarySide.East:
                for (int y = 1; y <= _state.Height; y++)
                    DrainWaterAtSim(_state.Width, y, depth);
                break;
            case WaterBoundarySide.South:
                for (int x = 1; x <= _state.Width; x++)
                    DrainWaterAtSim(x, 1, depth);
                break;
            case WaterBoundarySide.West:
                for (int y = 1; y <= _state.Height; y++)
                    DrainWaterAtSim(1, y, depth);
                break;
        }
    }

    private void DrainWaterAtSim(int simX, int simY, float depth)
    {
        if (!_state.HasMapCellAtSim(simX, simY) || _state.Water[simX, simY] <= 0f)
            return;

        float previous = _state.Water[simX, simY];
        _state.Water[simX, simY] = Mathf.Max(0f, previous - depth);
        if (!Mathf.Approximately(previous, _state.Water[simX, simY]))
            _state.MarkDirtyBySim(simX, simY);
    }

    private void ClearBoundarySink(WaterBoundarySide side)
    {
        if (_settings.GetBoundary(side).mode != WaterBoundaryMode.Sink)
            return;

        switch (side)
        {
            case WaterBoundarySide.North:
                for (int x = 1; x <= _state.Width; x++)
                    ClearWaterAtSim(x, _state.Height);
                break;
            case WaterBoundarySide.East:
                for (int y = 1; y <= _state.Height; y++)
                    ClearWaterAtSim(_state.Width, y);
                break;
            case WaterBoundarySide.South:
                for (int x = 1; x <= _state.Width; x++)
                    ClearWaterAtSim(x, 1);
                break;
            case WaterBoundarySide.West:
                for (int y = 1; y <= _state.Height; y++)
                    ClearWaterAtSim(1, y);
                break;
        }
    }

    private void ClearWaterAtSim(int simX, int simY)
    {
        if (!_state.HasMapCellAtSim(simX, simY) || _state.Water[simX, simY] <= 0f)
            return;

        _state.Water[simX, simY] = 0f;
        _state.MarkDirtyBySim(simX, simY);
    }

    private void ApplyContinuousSources(WaterSourceSpec[] sources, WaterModifierSnapshot modifiers, float dt)
    {
        if (sources == null || sources.Length == 0)
            return;

        foreach (WaterSourceSpec source in sources)
            ApplySource(source, modifiers, dt, true);
    }

    private void ApplySource(
        WaterSourceSpec source,
        WaterModifierSnapshot modifiers,
        float dt,
        bool continuous)
    {
        if (source == null)
            return;

        float sourceDepth = continuous ? source.continuousDepthPerSecond : source.initialDepth;
        if (float.IsNaN(sourceDepth) || float.IsInfinity(sourceDepth) || sourceDepth <= 0f)
            return;

        float depth = ResolveSourceDepth(source, sourceDepth, modifiers);
        if (continuous)
            depth *= Mathf.Max(0f, dt);

        if (depth <= 0f)
            return;

        switch (source.kind)
        {
            case WaterSourceKind.FullMap:
            case WaterSourceKind.Rainfall:
                ApplyToAllCells(depth, continuous);
                break;

            case WaterSourceKind.Edges:
            case WaterSourceKind.Boundary:
                ApplyToEdgeCells(depth, continuous);
                break;

            case WaterSourceKind.Corners:
                ApplyToCornerCells(depth, continuous);
                break;

            case WaterSourceKind.ExistingWaterBodies:
                ApplyToWaterBodyCells(depth, continuous);
                break;
        }
    }

    private float ResolveSourceDepth(
        WaterSourceSpec source,
        float sourceDepth,
        WaterModifierSnapshot modifiers)
    {
        float depth = Mathf.Max(0f, sourceDepth);

        if (source.kind == WaterSourceKind.Rainfall || source.scaleByRainfallRate)
            depth *= modifiers.RainfallRate;

        if (source.scaleByExternalWaterLoad)
            depth *= modifiers.ExternalWaterLoad;

        if (source.scaleByAntecedentWetness)
            depth *= modifiers.AntecedentWetness;

        return depth;
    }

    private void ApplyToAllCells(float depth, bool additive)
    {
        for (int y = 1; y <= _state.Height; y++)
            for (int x = 1; x <= _state.Width; x++)
                AddOrSetWaterAtSim(x, y, depth, additive);
    }

    private void ApplyToEdgeCells(float depth, bool additive)
    {
        for (int y = 1; y <= _state.Height; y++)
            for (int x = 1; x <= _state.Width; x++)
                if (x == 1 || x == _state.Width || y == 1 || y == _state.Height)
                    AddOrSetWaterAtSim(x, y, depth, additive);
    }

    private void ApplyToCornerCells(float depth, bool additive)
    {
        AddOrSetWaterAtSim(1, 1, depth, additive);
        if (_state.Height > 1)
            AddOrSetWaterAtSim(1, _state.Height, depth, additive);
        if (_state.Width > 1)
            AddOrSetWaterAtSim(_state.Width, 1, depth, additive);
        if (_state.Width > 1 && _state.Height > 1)
            AddOrSetWaterAtSim(_state.Width, _state.Height, depth, additive);
    }

    private void ApplyToWaterBodyCells(float depth, bool additive)
    {
        for (int y = 1; y <= _state.Height; y++)
            for (int x = 1; x <= _state.Width; x++)
                    if (_map != null && _map.IsInitialWaterBody(x, y))
                        AddOrSetWaterAtSim(x, y, depth, additive);
    }

    private void AddOrSetWaterAtSim(int simX, int simY, float depth, bool additive)
    {
        if (!_state.HasMapCellAtSim(simX, simY) || float.IsNaN(depth) || depth <= 0f)
            return;

        float next = additive
            ? _state.Water[simX, simY] + depth
            : Mathf.Max(_state.Water[simX, simY], depth);

        if (float.IsNaN(next))
            return;
        if (float.IsInfinity(next))
            next = _settings.maxWaterDepth > 0f ? _settings.maxWaterDepth : float.MaxValue;

        _state.Water[simX, simY] = ClampDepth(next);
        // Any positive source input is live water. The spread threshold controls
        // expansion, not whether a source cell is immediately discarded.
        _state.Active[simX, simY] = _state.Water[simX, simY] > 0f;
        _state.MarkDirtyBySim(simX, simY);
    }

    private void AccelerateFlows(float dt, WaterModifierSnapshot modifiers)
    {
        float frictionFactor = Mathf.Pow(1f - _settings.friction, dt);
        float windScale = _settings.windForceScale * modifiers.WindStress * dt;
        Vector2 windDirection = modifiers.WindDirection;

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 2; x <= _state.Width; x++)
            {
                if (!CanFlowAcrossX(x, y))
                {
                    _state.FlowX[x, y] = 0f;
                    continue;
                }

                float transmission = GetBarrierTransmissionX(x, y);
                if (transmission <= 0f)
                {
                    ApplyXSeepage(x, y, dt);
                    continue;
                }

                float acceleration = (Surface(x - 1, y) - Surface(x, y)) * _settings.gravity * dt / _settings.dx;
                _state.FlowX[x, y] = _state.FlowX[x, y] * frictionFactor + acceleration * transmission;
                _state.FlowX[x, y] += windDirection.x * windScale * transmission;
            }
        }

        for (int y = 2; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!CanFlowAcrossY(x, y))
                {
                    _state.FlowY[x, y] = 0f;
                    continue;
                }

                float transmission = GetBarrierTransmissionY(x, y);
                if (transmission <= 0f)
                {
                    ApplyYSeepage(x, y, dt);
                    continue;
                }

                float acceleration = (Surface(x, y - 1) - Surface(x, y)) * _settings.gravity * dt / _settings.dy;
                _state.FlowY[x, y] = _state.FlowY[x, y] * frictionFactor + acceleration * transmission;
                _state.FlowY[x, y] += windDirection.y * windScale * transmission;
            }
        }
    }

    private bool CanFlowAcrossX(int x, int y)
    {
        if (!_state.HasMapCellAtSim(x - 1, y) || !_state.HasMapCellAtSim(x, y))
            return false;

        return !_settings.useSpreadGating || (_state.Active[x - 1, y] && _state.Active[x, y]);
    }

    private bool CanFlowAcrossY(int x, int y)
    {
        if (!_state.HasMapCellAtSim(x, y - 1) || !_state.HasMapCellAtSim(x, y))
            return false;

        return !_settings.useSpreadGating || (_state.Active[x, y - 1] && _state.Active[x, y]);
    }

    private float GetBarrierTransmissionX(int x, int y)
    {
        if (!_barrierGrid.IsBlockedX(x, y))
            return 1f;

        float barrierHeight = _barrierGrid.GetBarrierHeightX(x, y);
        if (barrierHeight <= 0f)
            return 0f;

        float overtopDepth = Mathf.Max(Surface(x - 1, y), Surface(x, y)) - barrierHeight;
        if (overtopDepth <= 0f)
            return 0f;

        return Mathf.Clamp01(overtopDepth / _settings.overtopDepthForFullFlow);
    }

    private float GetBarrierTransmissionY(int x, int y)
    {
        if (!_barrierGrid.IsBlockedY(x, y))
            return 1f;

        float barrierHeight = _barrierGrid.GetBarrierHeightY(x, y);
        if (barrierHeight <= 0f)
            return 0f;

        float overtopDepth = Mathf.Max(Surface(x, y - 1), Surface(x, y)) - barrierHeight;
        if (overtopDepth <= 0f)
            return 0f;

        return Mathf.Clamp01(overtopDepth / _settings.overtopDepthForFullFlow);
    }

    private void ApplyXSeepage(int x, int y, float dt)
    {
        float seepage = _barrierGrid.GetSeepageX(x, y);
        if (seepage <= 0f)
        {
            _state.FlowX[x, y] = 0f;
            return;
        }

        float deltaHeight = Surface(x - 1, y) - Surface(x, y);
        float cap = 0.1f * Mathf.Max(_state.Water[x - 1, y], _state.Water[x, y]) * _settings.dx * _settings.dy / dt;
        _state.FlowX[x, y] = Mathf.Clamp(seepage * deltaHeight, -cap, cap);
    }

    private void ApplyYSeepage(int x, int y, float dt)
    {
        float seepage = _barrierGrid.GetSeepageY(x, y);
        if (seepage <= 0f)
        {
            _state.FlowY[x, y] = 0f;
            return;
        }

        float deltaHeight = Surface(x, y - 1) - Surface(x, y);
        float cap = 0.1f * Mathf.Max(_state.Water[x, y - 1], _state.Water[x, y]) * _settings.dx * _settings.dy / dt;
        _state.FlowY[x, y] = Mathf.Clamp(seepage * deltaHeight, -cap, cap);
    }

    private void ScaleOutflows(float dt)
    {
        ClearFlowsAcrossInactiveCells();

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasMapCellAtSim(x, y))
                    continue;

                if (_settings.useSpreadGating && !_state.Active[x, y])
                {
                    if (_state.Water[x, y] > 0f)
                    {
                        _state.Water[x, y] = 0f;
                        _state.MarkDirtyBySim(x, y);
                    }

                    _state.FlowX[x, y] = 0f;
                    _state.FlowY[x, y] = 0f;
                    continue;
                }

                bool leftActive = IsFlowTargetActive(x - 1, y);
                bool rightActive = IsFlowTargetActive(x + 1, y);
                bool downActive = IsFlowTargetActive(x, y - 1);
                bool upActive = IsFlowTargetActive(x, y + 1);

                float outLeft = leftActive ? Mathf.Max(0f, -_state.FlowX[x, y]) : 0f;
                float outDown = downActive ? Mathf.Max(0f, -_state.FlowY[x, y]) : 0f;
                float outRight = rightActive ? Mathf.Max(0f, _state.FlowX[x + 1, y]) : 0f;
                float outUp = upActive ? Mathf.Max(0f, _state.FlowY[x, y + 1]) : 0f;

                float totalOutflow = outLeft + outDown + outRight + outUp;
                if (totalOutflow <= 0f)
                    continue;

                float maxOutflow = _state.Water[x, y] * _settings.dx * _settings.dy / dt;
                float scale = Mathf.Min(1f, maxOutflow / totalOutflow);

                if (leftActive && _state.FlowX[x, y] < 0f) _state.FlowX[x, y] *= scale;
                if (downActive && _state.FlowY[x, y] < 0f) _state.FlowY[x, y] *= scale;
                if (rightActive && _state.FlowX[x + 1, y] > 0f) _state.FlowX[x + 1, y] *= scale;
                if (upActive && _state.FlowY[x, y + 1] > 0f) _state.FlowY[x, y + 1] *= scale;
            }
        }
    }

    private void ClearFlowsAcrossInactiveCells()
    {
        if (!_settings.useSpreadGating)
            return;

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 2; x <= _state.Width; x++)
            {
                if (!_state.HasMapCellAtSim(x - 1, y) || !_state.HasMapCellAtSim(x, y) ||
                    !_state.Active[x - 1, y] || !_state.Active[x, y])
                    _state.FlowX[x, y] = 0f;
            }
        }

        for (int y = 2; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasMapCellAtSim(x, y - 1) || !_state.HasMapCellAtSim(x, y) ||
                    !_state.Active[x, y - 1] || !_state.Active[x, y])
                    _state.FlowY[x, y] = 0f;
            }
        }
    }

    private bool IsFlowTargetActive(int simX, int simY)
    {
        if (!_state.IsSimCellInBounds(simX, simY))
            return false;

        if (!_settings.useSpreadGating)
            return _state.HasMapCellAtSim(simX, simY);

        return _state.HasMapCellAtSim(simX, simY) && _state.Active[simX, simY];
    }

    private void UpdateWaterDepths(float dt, WaterModifierSnapshot modifiers)
    {
        float drainage = _settings.baseDrainageDepthPerSecond * modifiers.DrainageEfficiency * dt;

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasMapCellAtSim(x, y))
                    continue;

                if (_settings.useSpreadGating && !_state.Active[x, y])
                {
                    if (_state.Water[x, y] > 0f)
                    {
                        _state.Water[x, y] = 0f;
                        _state.MarkDirtyBySim(x, y);
                    }
                    continue;
                }

                float previous = _state.Water[x, y];
                float next = previous + (
                    _state.FlowX[x, y] + _state.FlowY[x, y]
                    - _state.FlowX[x + 1, y] - _state.FlowY[x, y + 1]
                ) * dt / _settings.dx / _settings.dy;

                next = Mathf.Max(0f, next - drainage);
                next = ClampDepth(next);
                _state.Water[x, y] = next;

                if (!Mathf.Approximately(previous, next))
                    _state.MarkDirtyBySim(x, y);
            }
        }
    }

    private void KeepBoundaryDry()
    {
        for (int x = 0; x < _state.GridWidth; x++)
        {
            _state.Water[x, 0] = 0f;
            _state.Water[x, _state.GridHeight - 1] = 0f;
        }

        for (int y = 0; y < _state.GridHeight; y++)
        {
            _state.Water[0, y] = 0f;
            _state.Water[_state.GridWidth - 1, y] = 0f;
        }
    }

    private void ExpandActiveRegion()
    {
        for (int layer = 0; layer < _settings.spreadLayersPerTick; layer++)
        {
            bool[,] next = (bool[,])_state.Active.Clone();

            for (int y = 1; y <= _state.Height; y++)
            {
                for (int x = 1; x <= _state.Width; x++)
                {
                    if (!_state.HasMapCellAtSim(x, y))
                        continue;

                    if (!_state.Active[x, y])
                        continue;

                    if (_state.Water[x, y] <= _settings.expandFromWaterThreshold)
                        continue;

                    ActivateIfTile(next, x - 1, y);
                    ActivateIfTile(next, x + 1, y);
                    ActivateIfTile(next, x, y - 1);
                    ActivateIfTile(next, x, y + 1);
                }
            }

            _state.ReplaceActiveGrid(next);
        }
    }

    private void ActivateIfTile(bool[,] active, int simX, int simY)
    {
        if (_state.HasMapCellAtSim(simX, simY))
            active[simX, simY] = true;
    }

    private float Surface(int x, int y)
    {
        return _state.Terrain[x, y] + _state.Water[x, y];
    }

    private float ClampDepth(float depth)
    {
        if (float.IsNaN(depth) || depth <= 0f)
            return 0f;
        if (float.IsInfinity(depth))
            return _settings.maxWaterDepth > 0f ? _settings.maxWaterDepth : float.MaxValue;

        if (_settings.maxWaterDepth <= 0f)
            return depth;

        return Mathf.Clamp(depth, 0f, _settings.maxWaterDepth);
    }

    private WaterStepSummary BuildSummary(float dt)
    {
        int wetTileCount = 0;
        float totalWater = 0f;
        float maxDepth = 0f;

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasMapCellAtSim(x, y))
                    continue;

                float depth = _state.Water[x, y];
                if (depth <= 0f)
                    continue;

                wetTileCount++;
                totalWater += depth;
                maxDepth = Mathf.Max(maxDepth, depth);
            }
        }

        return new WaterStepSummary
        {
            StepIndex = _stepIndex,
            DeltaTime = dt,
            WetTileCount = wetTileCount,
            DirtyTileCount = _state.DirtyCells.Count,
            TotalWater = totalWater,
            MaxDepth = maxDepth
        };
    }
}
