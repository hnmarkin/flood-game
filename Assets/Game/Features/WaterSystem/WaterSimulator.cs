using System;
using UnityEngine;

public enum BlanketTypes
{
    Full,
    Edges,
    Corners,
    WaterBodies
}

public enum StepMode
{
    SpaceKey,
    Automatic,
}

public class WaterSimulator : MonoBehaviour
{
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private float waterHeight;
    [SerializeField] private BlanketTypes blanketType;

    [Header("Tile Types")]
    [SerializeField] private TileType waterTileType;

    [Header("Simulation Stepping Trigger")]
    [SerializeField] private StepMode stepMode;

    [Header("Barrier Provider (optional)")]
    [SerializeField] private MonoBehaviour barrierProviderBehaviour;
    private IBarrierProvider _barrierProvider;

    [Header("Flood Spread Gating")]
    [SerializeField] private bool useSpreadGating = true;

    [Min(0.1f)]
    [SerializeField] private float spreadInterval = 3f;

    [SerializeField] private int spreadLayersPerTick = 1;

    [Tooltip("Only expand from cells with water above this threshold.")]
    [SerializeField] private float expandFromWaterThreshold = 0.001f;

    [SerializeField] private bool expandOnceImmediatelyOnStart = true;
    
    [Header("Visual Flood Persistence")]
    [SerializeField] private bool persistentFloodVisuals = true;

    private float _spreadTimer;
    private bool[,] _active;

    [Header("Automatic stepping")]
    [Min(0.05f)]
    [SerializeField] private float autoStepInterval = 0.5f;

    private float _timer;

    public event Action OnSimulationStep;
    public event Action OnSimulationStarted;

    [Header("Control")]
    [SerializeField] private bool startOnPlay = false;
    private bool _simulationEnabled = false;

    private float[,] _visualWater;

    private void Awake()
    {
        _barrierProvider = barrierProviderBehaviour as IBarrierProvider;
        if (barrierProviderBehaviour != null && _barrierProvider == null)
            Debug.LogError("[WaterSimulator] barrierProviderBehaviour does not implement IBarrierProvider.");
    }

    private void Start()
    {
        if (tileMapData == null)
        {
            Debug.LogError("[WaterSimulator] No TileMapData assigned!");
            return;
        }

        if (startOnPlay)
            BeginSimulationInternal();
    }

    public void BeginSimulationFromUI()
    {
        if (tileMapData == null)
        {
            Debug.LogError("[WaterSimulator] Cannot start: no TileMapData assigned.");
            return;
        }

        if (_simulationEnabled)
        {
            Debug.LogWarning("[WaterSimulator] Simulation already started.");
            return;
        }

        BeginSimulationInternal();
    }

    private void BeginSimulationInternal()
    {
        ApplyWaterBlanket(tileMapData.rangeX, tileMapData.rangeY, waterHeight, blanketType);
        Initialize();

        _simulationEnabled = true;
        Debug.Log("[WaterSimulator] Simulation started.");

        OnSimulationStarted?.Invoke();
    }

    private void Update()
    {
        if (!_simulationEnabled) return;

        if (useSpreadGating)
        {
            _spreadTimer += Time.deltaTime;
            if (_spreadTimer >= spreadInterval)
            {
                _spreadTimer = 0f;
                ExpandActiveRegion();
            }
        }

        switch (stepMode)
        {
            case StepMode.SpaceKey:
                if (Input.GetKeyDown(KeyCode.Space))
                    StepSimulation();
                break;

            case StepMode.Automatic:
                _timer += Time.deltaTime;
                if (_timer >= autoStepInterval)
                {
                    _timer = 0f;
                    StepSimulation();
                }
                break;
        }
    }

    public void Initialize()
    {
        int gridWidth = tileMapData.GridWidth;
        int gridHeight = tileMapData.GridHeight;
        int N = tileMapData.N;

        tileMapData.terrain = new float[gridWidth, gridHeight];
        tileMapData.water   = new float[gridWidth, gridHeight];
        tileMapData.flowX   = new float[gridWidth, gridHeight];
        tileMapData.flowY   = new float[gridWidth, gridHeight];
        _visualWater = new float[gridWidth, gridHeight];

        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                TileInstance tile = tileMapData.Get(new Vector2Int(x, y));
                if (tile == null) continue;

                tileMapData.terrain[x + 1, y + 1] = tile.elevation;
                tileMapData.water[x + 1, y + 1] = tile.waterHeight;

                _visualWater[x + 1, y + 1] = tile.waterHeight;
            }
        }

        SetupBoundaryWalls(gridWidth, gridHeight);

        _active = new bool[gridWidth, gridHeight];
        for (int x = 1; x <= N; x++)
            for (int y = 1; y <= N; y++)
                _active[x, y] = tileMapData.water[x, y] > expandFromWaterThreshold;

        _spreadTimer = 0f;

        if (useSpreadGating && expandOnceImmediatelyOnStart)
        {
            ExpandActiveRegion();
            _spreadTimer = 0f;
        }

        tileMapData.simInitialized = true;
    }

    private void SetupBoundaryWalls(int gridWidth, int gridHeight)
    {
        int N = tileMapData.N;
        float maxTerrain = float.MinValue;

        for (int y = 1; y <= N; y++)
            for (int x = 1; x <= N; x++)
                maxTerrain = Mathf.Max(maxTerrain, tileMapData.terrain[x, y]);

        float boundaryHeight = maxTerrain + 1f;

        for (int i = 0; i < gridWidth; i++)
        {
            tileMapData.terrain[i, 0] = boundaryHeight;
            tileMapData.terrain[i, gridHeight - 1] = boundaryHeight;
            tileMapData.water[i, 0] = 0f;
            tileMapData.water[i, gridHeight - 1] = 0f;
        }

        for (int i = 0; i < gridHeight; i++)
        {
            tileMapData.terrain[0, i] = boundaryHeight;
            tileMapData.terrain[gridWidth - 1, i] = boundaryHeight;
            tileMapData.water[0, i] = 0f;
            tileMapData.water[gridWidth - 1, i] = 0f;
        }
    }

    public void StepSimulation()
    {
        if (!tileMapData.simInitialized)
        {
            Debug.LogWarning("[WaterSimulator] Cannot step simulation: Not initialized!");
            return;
        }

        int N = tileMapData.N;
        float dx = tileMapData.dx;
        float dy = tileMapData.dy;
        float dt = tileMapData.dt;
        float g = tileMapData.g;
        float friction = tileMapData.friction;

        float frictionFactor = Mathf.Pow(1 - friction, dt);

        float Surface(int x, int y) => tileMapData.terrain[x, y] + tileMapData.water[x, y];

        bool BlockX(int x, int y) => _barrierProvider != null && _barrierProvider.IsBlockedX(x, y);
        bool BlockY(int x, int y) => _barrierProvider != null && _barrierProvider.IsBlockedY(x, y);

        float SeepX(int x, int y) => (_barrierProvider == null) ? 0f : _barrierProvider.GetSeepageX(x, y);
        float SeepY(int x, int y) => (_barrierProvider == null) ? 0f : _barrierProvider.GetSeepageY(x, y);

        // Accelerate X + seepage
        for (int y = 1; y <= N; ++y)
        {
            for (int x = 2; x <= N; ++x)
            {
                if (BlockX(x, y))
                {
                    float seep = SeepX(x, y);
                    if (seep <= 0f) { tileMapData.flowX[x, y] = 0f; continue; }

                    float dH = Surface(x - 1, y) - Surface(x, y);
                    float fx = seep * dH;

                    float wl = tileMapData.water[x - 1, y];
                    float wr = tileMapData.water[x, y];
                    float cap = 0.1f * Mathf.Max(wl, wr) * dx * dy / dt;

                    tileMapData.flowX[x, y] = Mathf.Clamp(fx, -cap, cap);
                    continue;
                }

                tileMapData.flowX[x, y] = tileMapData.flowX[x, y] * frictionFactor
                    + (Surface(x - 1, y) - Surface(x, y)) * g * dt / dx;
            }
        }

        // Accelerate Y + seepage
        for (int y = 2; y <= N; ++y)
        {
            for (int x = 1; x <= N; ++x)
            {
                if (BlockY(x, y))
                {
                    float seep = SeepY(x, y);
                    if (seep <= 0f) { tileMapData.flowY[x, y] = 0f; continue; }

                    float dH = Surface(x, y - 1) - Surface(x, y);
                    float fy = seep * dH;

                    float wd = tileMapData.water[x, y - 1];
                    float wu = tileMapData.water[x, y];
                    float cap = 0.1f * Mathf.Max(wd, wu) * dx * dy / dt;

                    tileMapData.flowY[x, y] = Mathf.Clamp(fy, -cap, cap);
                    continue;
                }

                tileMapData.flowY[x, y] = tileMapData.flowY[x, y] * frictionFactor
                    + (Surface(x, y - 1) - Surface(x, y)) * g * dt / dy;
            }
        }

        // Scale outflows (spread gating)
        for (int y = 1; y <= N; ++y)
        {
            for (int x = 1; x <= N; ++x)
            {
                if (useSpreadGating && !_active[x, y])
                {
                    tileMapData.water[x, y] = 0f;
                    tileMapData.flowX[x, y] = 0f;
                    tileMapData.flowY[x, y] = 0f;
                    continue;
                }

                bool leftActive  = !useSpreadGating || _active[x - 1, y];
                bool rightActive = !useSpreadGating || _active[x + 1, y];
                bool downActive  = !useSpreadGating || _active[x, y - 1];
                bool upActive    = !useSpreadGating || _active[x, y + 1];

                float outLeft  = leftActive  ? Mathf.Max(0f, -tileMapData.flowX[x, y]) : 0f;
                float outDown  = downActive  ? Mathf.Max(0f, -tileMapData.flowY[x, y]) : 0f;
                float outRight = rightActive ? Mathf.Max(0f,  tileMapData.flowX[x + 1, y]) : 0f;
                float outUp    = upActive    ? Mathf.Max(0f,  tileMapData.flowY[x, y + 1]) : 0f;

                float totalOutflow = outLeft + outDown + outRight + outUp;
                float maxOutflow = tileMapData.water[x, y] * dx * dy / dt;

                if (totalOutflow > 0f)
                {
                    float scale = Mathf.Min(1f, maxOutflow / totalOutflow);

                    if (leftActive  && tileMapData.flowX[x, y] < 0f) tileMapData.flowX[x, y] *= scale;
                    if (downActive  && tileMapData.flowY[x, y] < 0f) tileMapData.flowY[x, y] *= scale;
                    if (rightActive && tileMapData.flowX[x + 1, y] > 0f) tileMapData.flowX[x + 1, y] *= scale;
                    if (upActive    && tileMapData.flowY[x, y + 1] > 0f) tileMapData.flowY[x, y + 1] *= scale;
                }
            }
        }

        // Update water
        for (int y = 1; y <= N; ++y)
        {
            for (int x = 1; x <= N; ++x)
            {
                if (useSpreadGating && !_active[x, y])
                {
                    tileMapData.water[x, y] = 0f;

                    float visualDepth = persistentFloodVisuals ? _visualWater[x, y] : 0f;
                    tileMapData.SetWater(new Vector2Int(x - 1, y - 1), visualDepth);
                    continue;
                }

                tileMapData.water[x, y] += (
                tileMapData.flowX[x, y] + tileMapData.flowY[x, y]
                - tileMapData.flowX[x + 1, y] - tileMapData.flowY[x, y + 1]
            ) * dt / dx / dy;

            tileMapData.water[x, y] = Mathf.Max(0f, tileMapData.water[x, y]);

            if (persistentFloodVisuals)
            {
                _visualWater[x, y] = Mathf.Max(_visualWater[x, y], tileMapData.water[x, y]);
                tileMapData.SetWater(new Vector2Int(x - 1, y - 1), _visualWater[x, y]);
            }
            else
            {
                tileMapData.SetWater(new Vector2Int(x - 1, y - 1), tileMapData.water[x, y]);
            }
            }
        }

        // Keep boundary cells dry
        for (int i = 0; i < tileMapData.GridWidth; i++)
        {
            tileMapData.water[i, 0] = 0f;
            tileMapData.water[i, tileMapData.GridHeight - 1] = 0f;
        }
        for (int i = 0; i < tileMapData.GridHeight; i++)
        {
            tileMapData.water[0, i] = 0f;
            tileMapData.water[tileMapData.GridWidth - 1, i] = 0f;
        }

        OnSimulationStep?.Invoke();
    }

    private void ExpandActiveRegion()
    {
        if (_active == null) return;

        int N = tileMapData.N;

        for (int layer = 0; layer < spreadLayersPerTick; layer++)
        {
            bool[,] next = (bool[,])_active.Clone();

            for (int y = 1; y <= N; y++)
            {
                for (int x = 1; x <= N; x++)
                {
                    if (!_active[x, y]) continue;
                    if (tileMapData.water[x, y] <= expandFromWaterThreshold) continue;

                    next[x - 1, y] = true;
                    next[x + 1, y] = true;
                    next[x, y - 1] = true;
                    next[x, y + 1] = true;
                }
            }

            _active = next;
        }
    }

    private void ApplyWaterBlanket(Vector2Int rangeX, Vector2Int rangeY, float h, BlanketTypes bt)
    {
        bool TileExists(Vector2Int p)
        {
            var t = tileMapData.Get(p);
            if (t == null) return false;
            return true;
        }

        switch (bt)
        {
            case BlanketTypes.Full:
                for (int x = 0; x < rangeX.y; x++)
                    for (int y = 0; y < rangeY.y; y++)
                    {
                        var pos = new Vector2Int(x, y);
                        if (!TileExists(pos)) continue;
                        tileMapData.SetWater(pos, h);
                    }
                break;

            case BlanketTypes.Edges:
                int maxX = rangeX.y - 1;
                int maxY = rangeY.y - 1;
                for (int x = 0; x < rangeX.y; x++)
                {
                    if (x == 0 || x == maxX)
                    {
                        for (int y = 0; y < rangeY.y; y++)
                        {
                            var pos = new Vector2Int(x, y);
                            if (!TileExists(pos)) continue;
                            tileMapData.SetWater(pos, h);
                        }
                    }
                    else
                    {
                        var pos1 = new Vector2Int(x, 0);
                        var pos2 = new Vector2Int(x, maxY);
                        if (!TileExists(pos1) || !TileExists(pos2)) continue;
                        tileMapData.SetWater(pos1, h);
                        tileMapData.SetWater(pos2, h);
                    }
                }
                break;

            case BlanketTypes.Corners:
                maxX = rangeX.y - 1;
                maxY = rangeY.y - 1;
                var corners = new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, maxY),
                    new Vector2Int(maxX, 0),
                    new Vector2Int(maxX, maxY),
                };
                foreach (var c in corners)
                {
                    if (!TileExists(c)) break;
                    tileMapData.SetWater(c, h);
                }
                break;

            case BlanketTypes.WaterBodies:
                if (waterTileType == null) return;

                for (int x = 0; x < rangeX.y; x++)
                    for (int y = 0; y < rangeY.y; y++)
                    {
                        var pos = new Vector2Int(x, y);
                        if (!TileExists(pos)) continue;
                        var ti = tileMapData.Get(pos);
                        if (ti != null && ti.tileType == waterTileType)
                            tileMapData.SetWater(pos, h);
                    }
                break;
        }
    }
}
