using UnityEngine;

public sealed class Dev_WaterSimulationEngine
{
    private readonly Dev_IWaterBarrierProvider _barrierProvider;

    private Dev_WaterRuntimeState _state;
    private Dev_WaterSimulationSettings _settings;
    private float _spreadTimer;
    private int _stepIndex;

    public Dev_WaterSimulationEngine(Dev_IWaterBarrierProvider barrierProvider)
    {
        _barrierProvider = barrierProvider;
    }

    public Dev_WaterStepSummary LastSummary { get; private set; }

    public void Initialize(Dev_WaterRuntimeState state, Dev_WaterSimulationSettings settings)
    {
        _state = state;
        _settings = settings != null ? settings.Clone() : new Dev_WaterSimulationSettings();
        _settings.Sanitize();
        _stepIndex = 0;
        _spreadTimer = 0f;

        ClearFlow();
        if (_settings.useBoundaryWalls)
            SetupBoundaryWalls();
    }

    public void ApplyInitialSources(Dev_WaterSourceSpec[] sources, TileType fallbackWaterTileType, Dev_WaterModifierSnapshot modifiers)
    {
        if (_state == null)
            return;

        modifiers.Sanitize();

        if (sources == null || sources.Length == 0)
            return;

        foreach (Dev_WaterSourceSpec source in sources)
            ApplySource(source, fallbackWaterTileType, modifiers, 0f, false);
    }

    public void InitializeActiveRegion()
    {
        if (_state == null)
            return;

        _state.Active = new bool[_state.GridWidth, _state.GridHeight];

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                _state.Active[x, y] = _state.HasTile[x, y]
                    && _state.Water[x, y] > _settings.expandFromWaterThreshold;
            }
        }

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

    public Dev_WaterStepSummary Step(Dev_WaterSourceSpec[] continuousSources, TileType fallbackWaterTileType, Dev_WaterModifierSnapshot modifiers)
    {
        if (_state == null)
            return default;

        modifiers.Sanitize();

        float dt = Mathf.Max(0.001f, _settings.dt * modifiers.EventPacing);
        ApplyContinuousSources(continuousSources, fallbackWaterTileType, modifiers, dt);

        AccelerateFlows(dt, modifiers);
        ScaleOutflows(dt);
        UpdateWaterDepths(dt, modifiers);
        KeepBoundaryDry();

        _stepIndex++;
        LastSummary = BuildSummary(dt);
        return LastSummary;
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

    private void SetupBoundaryWalls()
    {
        float maxTerrain = 0f;
        bool foundTerrain = false;

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasTile[x, y])
                    continue;

                maxTerrain = foundTerrain ? Mathf.Max(maxTerrain, _state.Terrain[x, y]) : _state.Terrain[x, y];
                foundTerrain = true;
            }
        }

        float boundaryHeight = maxTerrain + _settings.boundaryHeightPadding;

        for (int x = 0; x < _state.GridWidth; x++)
        {
            _state.Terrain[x, 0] = boundaryHeight;
            _state.Terrain[x, _state.GridHeight - 1] = boundaryHeight;
            _state.Water[x, 0] = 0f;
            _state.Water[x, _state.GridHeight - 1] = 0f;
        }

        for (int y = 0; y < _state.GridHeight; y++)
        {
            _state.Terrain[0, y] = boundaryHeight;
            _state.Terrain[_state.GridWidth - 1, y] = boundaryHeight;
            _state.Water[0, y] = 0f;
            _state.Water[_state.GridWidth - 1, y] = 0f;
        }
    }

    private void ApplyContinuousSources(Dev_WaterSourceSpec[] sources, TileType fallbackWaterTileType, Dev_WaterModifierSnapshot modifiers, float dt)
    {
        if (sources == null || sources.Length == 0)
            return;

        foreach (Dev_WaterSourceSpec source in sources)
            ApplySource(source, fallbackWaterTileType, modifiers, dt, true);
    }

    private void ApplySource(
        Dev_WaterSourceSpec source,
        TileType fallbackWaterTileType,
        Dev_WaterModifierSnapshot modifiers,
        float dt,
        bool continuous)
    {
        if (source == null || source.depth <= 0f)
            return;

        float depth = ResolveSourceDepth(source, modifiers);
        if (continuous)
            depth *= Mathf.Max(0f, dt);

        if (depth <= 0f)
            return;

        switch (source.kind)
        {
            case Dev_WaterSourceKind.FullMap:
            case Dev_WaterSourceKind.Rainfall:
                ApplyToAllCells(depth, continuous);
                break;

            case Dev_WaterSourceKind.Edges:
            case Dev_WaterSourceKind.Boundary:
                ApplyToEdgeCells(depth, continuous);
                break;

            case Dev_WaterSourceKind.Corners:
                ApplyToCornerCells(depth, continuous);
                break;

            case Dev_WaterSourceKind.ExistingWaterBodies:
                ApplyToWaterBodyCells(depth, continuous);
                break;
        }
    }

    private float ResolveSourceDepth(Dev_WaterSourceSpec source, Dev_WaterModifierSnapshot modifiers)
    {
        float depth = Mathf.Max(0f, source.depth);

        if (source.kind == Dev_WaterSourceKind.Rainfall || source.scaleByRainfallRate)
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
        for (int x = 1; x <= _state.Width; x++)
        {
            AddOrSetWaterAtSim(x, 1, depth, additive);
            AddOrSetWaterAtSim(x, _state.Height, depth, additive);
        }

        for (int y = 2; y <= _state.Height - 1; y++)
        {
            AddOrSetWaterAtSim(1, y, depth, additive);
            AddOrSetWaterAtSim(_state.Width, y, depth, additive);
        }
    }

    private void ApplyToCornerCells(float depth, bool additive)
    {
        AddOrSetWaterAtSim(1, 1, depth, additive);
        AddOrSetWaterAtSim(1, _state.Height, depth, additive);
        AddOrSetWaterAtSim(_state.Width, 1, depth, additive);
        AddOrSetWaterAtSim(_state.Width, _state.Height, depth, additive);
    }

    private void ApplyToWaterBodyCells(float depth, bool additive)
    {
        for (int y = 1; y <= _state.Height; y++)
            for (int x = 1; x <= _state.Width; x++)
                if (_state.IsWaterBody[x, y])
                    AddOrSetWaterAtSim(x, y, depth, additive);
    }

    private void AddOrSetWaterAtSim(int simX, int simY, float depth, bool additive)
    {
        if (!_state.HasTileAtSim(simX, simY))
            return;

        float next = additive
            ? _state.Water[simX, simY] + depth
            : Mathf.Max(_state.Water[simX, simY], depth);

        _state.Water[simX, simY] = ClampDepth(next);
        _state.Active[simX, simY] = _state.Water[simX, simY] > _settings.expandFromWaterThreshold;
        _state.MarkDirtyBySim(simX, simY);
    }

    private void AccelerateFlows(float dt, Dev_WaterModifierSnapshot modifiers)
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
        return _state.HasTileAtSim(x - 1, y) && _state.HasTileAtSim(x, y);
    }

    private bool CanFlowAcrossY(int x, int y)
    {
        return _state.HasTileAtSim(x, y - 1) && _state.HasTileAtSim(x, y);
    }

    private float GetBarrierTransmissionX(int x, int y)
    {
        if (_barrierProvider == null || !_barrierProvider.IsBlockedX(x, y))
            return 1f;

        float barrierHeight = _barrierProvider.GetBarrierHeightX(x, y);
        if (barrierHeight <= 0f)
            return 0f;

        float overtopDepth = Mathf.Max(Surface(x - 1, y), Surface(x, y)) - barrierHeight;
        if (overtopDepth <= 0f)
            return 0f;

        return Mathf.Clamp01(overtopDepth / _settings.overtopDepthForFullFlow);
    }

    private float GetBarrierTransmissionY(int x, int y)
    {
        if (_barrierProvider == null || !_barrierProvider.IsBlockedY(x, y))
            return 1f;

        float barrierHeight = _barrierProvider.GetBarrierHeightY(x, y);
        if (barrierHeight <= 0f)
            return 0f;

        float overtopDepth = Mathf.Max(Surface(x, y - 1), Surface(x, y)) - barrierHeight;
        if (overtopDepth <= 0f)
            return 0f;

        return Mathf.Clamp01(overtopDepth / _settings.overtopDepthForFullFlow);
    }

    private void ApplyXSeepage(int x, int y, float dt)
    {
        float seepage = _barrierProvider != null ? _barrierProvider.GetSeepageX(x, y) : 0f;
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
        float seepage = _barrierProvider != null ? _barrierProvider.GetSeepageY(x, y) : 0f;
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
        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasTile[x, y])
                    continue;

                if (_settings.useSpreadGating && !_state.Active[x, y])
                {
                    _state.Water[x, y] = 0f;
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

    private bool IsFlowTargetActive(int simX, int simY)
    {
        if (!_state.IsSimCellInBounds(simX, simY))
            return false;

        if (!_settings.useSpreadGating)
            return _state.HasTileAtSim(simX, simY);

        return _state.HasTileAtSim(simX, simY) && _state.Active[simX, simY];
    }

    private void UpdateWaterDepths(float dt, Dev_WaterModifierSnapshot modifiers)
    {
        float drainage = _settings.baseDrainageDepthPerSecond * modifiers.DrainageEfficiency * dt;

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasTile[x, y])
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
        if (_state.Active == null)
            return;

        for (int layer = 0; layer < _settings.spreadLayersPerTick; layer++)
        {
            bool[,] next = (bool[,])_state.Active.Clone();

            for (int y = 1; y <= _state.Height; y++)
            {
                for (int x = 1; x <= _state.Width; x++)
                {
                    if (!_state.HasTile[x, y])
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

            _state.Active = next;
        }
    }

    private void ActivateIfTile(bool[,] active, int simX, int simY)
    {
        if (_state.HasTileAtSim(simX, simY))
            active[simX, simY] = true;
    }

    private float Surface(int x, int y)
    {
        return _state.Terrain[x, y] + _state.Water[x, y];
    }

    private float ClampDepth(float depth)
    {
        if (_settings.maxWaterDepth <= 0f)
            return Mathf.Max(0f, depth);

        return Mathf.Clamp(depth, 0f, _settings.maxWaterDepth);
    }

    private Dev_WaterStepSummary BuildSummary(float dt)
    {
        int wetTileCount = 0;
        float totalWater = 0f;
        float maxDepth = 0f;

        for (int y = 1; y <= _state.Height; y++)
        {
            for (int x = 1; x <= _state.Width; x++)
            {
                if (!_state.HasTile[x, y])
                    continue;

                float depth = _state.Water[x, y];
                if (depth <= 0f)
                    continue;

                wetTileCount++;
                totalWater += depth;
                maxDepth = Mathf.Max(maxDepth, depth);
            }
        }

        return new Dev_WaterStepSummary
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
