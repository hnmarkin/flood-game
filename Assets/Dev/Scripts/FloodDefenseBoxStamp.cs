using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FloodDefenseBoxStamp : MonoBehaviour, IBarrierProvider
{
    public static event Action OnAllZoneBarriersPlaced;

    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;

    private NpcToastController _npcToast;

    [Header("Zone Boundary Placement")]
    [SerializeField] private bool zoneBoundaryMode = false;

    [Tooltip("Maximum number of zone barriers allowed for this map.")]
    [SerializeField, Min(1)] private int maxZoneBarriers = 1;

    [Header("Barrier Logic")]
    [SerializeField] private float barrierHeight = 40f;

    [Tooltip("Use 0 for no seepage. Use a small value like 0.001 for slow seepage.")]
    [SerializeField] private float seepage = 0.001f;

    [Header("Preview")]
    [SerializeField] private Tilemap previewTilemap;
    [SerializeField] private TileBase previewTile;
    [SerializeField] private Color previewColor = new Color(0.2f, 1f, 0.2f, 0.55f);

    [Header("Committed Zone Outline")]
    [SerializeField] private Tilemap zoneOutlineTilemap;
    [SerializeField] private TileBase zoneOutlineTile;
    [SerializeField] private Color zoneOutlineColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private bool drawCommittedOutline = true;

    private float[,] _barrierHX;
    private float[,] _barrierHY;
    private float[,] _seepX;
    private float[,] _seepY;

    private int[,] _blockedX;
    private int[,] _blockedY;

    private Vector2Int _tileOrigin;

    private readonly HashSet<string> _committedZones = new();
    private readonly Dictionary<string, HashSet<Vector2Int>> _geoidToTiles = new();

    private string _hoverGeoid = null;
    private HashSet<Vector2Int> _hoverZoneTiles = null;

    private Coroutine _enterZoneModeCoroutine;

    public int BarriersPlaced => _committedZones.Count;

    public int ActualMaxZoneBarriers
    {
        get
        {
            int requestedLimit = Mathf.Max(1, maxZoneBarriers);

            if (_geoidToTiles == null || _geoidToTiles.Count == 0)
                return requestedLimit;

            return Mathf.Clamp(requestedLimit, 1, _geoidToTiles.Count);
        }
    }

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        InitializeArraysAndOrigin();

        // Important:
        // This prevents Unity from starting the scene with zoneBoundaryMode already ON
        // due to a previously serialized Inspector value.
        zoneBoundaryMode = false;

        ClearHoverAndPreview();

        _npcToast = FindFirstObjectByType<NpcToastController>();
        UpdateGlobalHUDBarrierProgress();

        if (_npcToast == null)
            Debug.LogWarning("[FloodDefenseBoxStamp] NpcToastController not found in scene.");
        else
            Debug.Log("[FloodDefenseBoxStamp] NpcToastController found OK.");

        Debug.Log("[FloodDefenseBoxStamp] Startup complete. Zone boundary mode forced OFF.");
    }

    private void Update()
    {
        if (!zoneBoundaryMode)
            return;

        ZoneBoundaryUpdate();
    }

    private void OnDisable()
    {
        zoneBoundaryMode = false;
        ClearHoverAndPreview();

        if (_enterZoneModeCoroutine != null)
        {
            StopCoroutine(_enterZoneModeCoroutine);
            _enterZoneModeCoroutine = null;
        }
    }

    private void OnValidate()
    {
        if (maxZoneBarriers < 1)
            maxZoneBarriers = 1;

        if (barrierHeight < 0f)
            barrierHeight = 0f;

        if (seepage < 0f)
            seepage = 0f;
    }

    public void ToggleBuildModeFromUI()
    {
        if (zoneBoundaryMode || _enterZoneModeCoroutine != null)
        {
            ExitZoneBoundaryPlacementMode();
        }
        else
        {
            EnterZoneBoundaryPlacementMode();
        }
    }

    public void EnterBuildModeFromUI()
    {
        EnterZoneBoundaryPlacementMode();
    }

    public void ExitBuildModeFromUI()
    {
        ExitZoneBoundaryPlacementMode();
    }

    public void EnterZoneBoundaryPlacementMode()
    {
        if (_enterZoneModeCoroutine != null)
            StopCoroutine(_enterZoneModeCoroutine);

        _enterZoneModeCoroutine = StartCoroutine(EnterZoneBoundaryPlacementModeRoutine());
    }

    private IEnumerator EnterZoneBoundaryPlacementModeRoutine()
    {
        if (!ValidateRequiredReferences())
        {
            _enterZoneModeCoroutine = null;
            yield break;
        }

        zoneBoundaryMode = false;
        ClearHoverAndPreview();

        int attempts = 0;
        int maxAttempts = 30;

        while (!IsZoneLoaderReady() && attempts < maxAttempts)
        {
            attempts++;
            yield return null;
        }

        BuildZoneIndex();
        UpdateGlobalHUDBarrierProgress();

        if (_geoidToTiles.Count == 0)
        {
            ShowMessage("Zone boundary mode could not start because no zones were found.");
            _enterZoneModeCoroutine = null;
            yield break;
        }

        if (BarriersPlaced >= ActualMaxZoneBarriers)
        {
            ShowMessage($"Barrier limit reached: {BarriersPlaced}/{ActualMaxZoneBarriers} zone barriers placed.");
            _enterZoneModeCoroutine = null;
            yield break;
        }

        zoneBoundaryMode = true;
        ClearHoverAndPreview();

        Debug.Log($"[FloodDefenseBoxStamp] Zone boundary mode ON. Hover a zone and left-click to barricade. " +
                  $"Placed={BarriersPlaced}/{ActualMaxZoneBarriers}, Total zones={_geoidToTiles.Count}");

        _enterZoneModeCoroutine = null;
    }

    public void ExitZoneBoundaryPlacementMode()
    {
        if (_enterZoneModeCoroutine != null)
        {
            StopCoroutine(_enterZoneModeCoroutine);
            _enterZoneModeCoroutine = null;
        }

        zoneBoundaryMode = false;
        ClearHoverAndPreview();

        Debug.Log("[FloodDefenseBoxStamp] Zone boundary mode OFF.");
    }

    private bool IsZoneLoaderReady()
    {
        if (jsonMapLoader == null)
            return false;

        if (jsonMapLoader.payload == null)
            return false;

        if (jsonMapLoader.cellToRC == null || jsonMapLoader.cellToRC.Count == 0)
            return false;

        if (jsonMapLoader.geoidGrid == null)
            return false;

        return true;
    }

    private bool ValidateRequiredReferences()
    {
        bool ok = true;

        if (tileMapData == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] tileMapData is not assigned.");
            ok = false;
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] terrainTilemap is not assigned.");
            ok = false;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] mainCamera is not assigned.");
            ok = false;
        }

        if (jsonMapLoader == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] jsonMapLoader is not assigned.");
            ok = false;
        }

        if (previewTilemap == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] previewTilemap is not assigned.");
            ok = false;
        }

        if (previewTile == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] previewTile is not assigned.");
            ok = false;
        }

        return ok;
    }

    private void InitializeArraysAndOrigin()
    {
        if (tileMapData == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] tileMapData is null. Barrier arrays were not initialized.");
            return;
        }

        int w = tileMapData.GridWidth;
        int h = tileMapData.GridHeight;

        _barrierHX = new float[w, h];
        _barrierHY = new float[w, h];
        _seepX = new float[w, h];
        _seepY = new float[w, h];

        _blockedX = new int[w, h];
        _blockedY = new int[w, h];

        if (terrainTilemap != null)
        {
            terrainTilemap.CompressBounds();
            BoundsInt bounds = terrainTilemap.cellBounds;
            _tileOrigin = new Vector2Int(bounds.xMin, bounds.yMin);
        }
        else
        {
            _tileOrigin = Vector2Int.zero;
            Debug.LogError("[FloodDefenseBoxStamp] terrainTilemap is null. Tile origin set to zero.");
        }

        Debug.Log($"[FloodDefenseBoxStamp] Initialized barrier arrays. Tile origin={_tileOrigin}.");
    }

    private void BuildZoneIndex()
    {
        _geoidToTiles.Clear();

        if (jsonMapLoader == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] BuildZoneIndex failed: jsonMapLoader is null.");
            return;
        }

        if (tileMapData == null)
        {
            Debug.LogError("[FloodDefenseBoxStamp] BuildZoneIndex failed: tileMapData is null.");
            return;
        }

        if (jsonMapLoader.payload == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] BuildZoneIndex: jsonMapLoader.payload is null.");
            return;
        }

        if (jsonMapLoader.cellToRC == null || jsonMapLoader.cellToRC.Count == 0)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] BuildZoneIndex: jsonMapLoader.cellToRC is empty.");
            return;
        }

        if (jsonMapLoader.geoidGrid == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] BuildZoneIndex: jsonMapLoader.geoidGrid is null.");
            return;
        }

        foreach (var kvp in jsonMapLoader.cellToRC)
        {
            Vector3Int cell = kvp.Key;
            Vector2Int rc = kvp.Value;

            int r = rc.x;
            int c = rc.y;

            string geoid = jsonMapLoader.geoidGrid[r, c];

            if (string.IsNullOrEmpty(geoid))
                continue;

            int tx = cell.x - _tileOrigin.x;
            int ty = cell.y - _tileOrigin.y;

            int n = tileMapData.N;

            if (tx < 0 || ty < 0 || tx >= n || ty >= n)
                continue;

            if (!_geoidToTiles.TryGetValue(geoid, out HashSet<Vector2Int> zoneTiles))
            {
                zoneTiles = new HashSet<Vector2Int>();
                _geoidToTiles[geoid] = zoneTiles;
            }

            zoneTiles.Add(new Vector2Int(tx, ty));
        }

        Debug.Log($"[FloodDefenseBoxStamp] BuildZoneIndex OK. Zones found={_geoidToTiles.Count}. " +
                  $"Requested limit={maxZoneBarriers}. Actual limit={ActualMaxZoneBarriers}.");
    }

    private void ZoneBoundaryUpdate()
    {
        if (!ValidateRuntimeReferences())
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitZoneBoundaryPlacementMode();
            return;
        }

        if (BarriersPlaced >= ActualMaxZoneBarriers)
        {
            ShowMessage($"Barrier limit reached: {BarriersPlaced}/{ActualMaxZoneBarriers} zone barriers placed.");
            ExitZoneBoundaryPlacementMode();
            return;
        }

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        int r;
        int c;
        int pop;
        string category;
        string geoid;

        bool hit = jsonMapLoader.TryGetTileInfoAtWorld(
            world,
            out r,
            out c,
            out category,
            out geoid,
            out pop
        );

        if (!hit || string.IsNullOrEmpty(geoid) || !_geoidToTiles.TryGetValue(geoid, out HashSet<Vector2Int> zoneTiles))
        {
            ClearHoverAndPreview();
            return;
        }

        bool alreadyCommitted = _committedZones.Contains(geoid);

        if (_hoverGeoid != geoid || _hoverZoneTiles != zoneTiles)
        {
            _hoverGeoid = geoid;
            _hoverZoneTiles = zoneTiles;

            if (alreadyCommitted)
            {
                ClearPreviewOnly();
            }
            else
            {
                DrawZoneBoundaryPreview(zoneTiles);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryCommitZoneBoundaryBarrier(geoid, zoneTiles);
        }
    }

    private bool ValidateRuntimeReferences()
    {
        if (jsonMapLoader == null || terrainTilemap == null || mainCamera == null || tileMapData == null)
            return false;

        if (_blockedX == null || _blockedY == null || _barrierHX == null || _barrierHY == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] Barrier arrays were null. Reinitializing.");
            InitializeArraysAndOrigin();
        }

        return true;
    }

    private void TryCommitZoneBoundaryBarrier(string geoid, HashSet<Vector2Int> zoneTiles)
    {
        if (string.IsNullOrEmpty(geoid))
            return;

        if (zoneTiles == null || zoneTiles.Count == 0)
            return;

        if (_committedZones.Contains(geoid))
        {
            ShowMessage("This zone already has a boundary barrier.");
            return;
        }

        if (BarriersPlaced >= ActualMaxZoneBarriers)
        {
            ShowMessage($"Barrier limit reached: {BarriersPlaced}/{ActualMaxZoneBarriers} zone barriers placed.");
            ExitZoneBoundaryPlacementMode();
            return;
        }

        int blockedEdges = CommitZoneBoundaryEdges(zoneTiles);

        _committedZones.Add(geoid);
        UpdateGlobalHUDBarrierProgress();

        PaintCommittedZoneOutline(zoneTiles);

        ShowMessage($"Zone barrier placed: {BarriersPlaced}/{ActualMaxZoneBarriers}");

        Debug.Log($"[FloodDefenseBoxStamp] Zone boundary committed. Zone={geoid}, Zone tiles={zoneTiles.Count}, " +
                  $"Blocked edges={blockedEdges}, Placed={BarriersPlaced}/{ActualMaxZoneBarriers}");

        ClearHoverAndPreview();
        
        if (BarriersPlaced >= ActualMaxZoneBarriers)
        {
            ShowMessage($"All available barriers have been placed: {BarriersPlaced}/{ActualMaxZoneBarriers}");

            OnAllZoneBarriersPlaced?.Invoke();

            ExitZoneBoundaryPlacementMode();
        }
    }

    private int CommitZoneBoundaryEdges(HashSet<Vector2Int> zoneTiles)
    {
        int blockedEdges = 0;

        foreach (Vector2Int t in zoneTiles)
        {
            Vector2Int left = new Vector2Int(t.x - 1, t.y);
            Vector2Int right = new Vector2Int(t.x + 1, t.y);
            Vector2Int down = new Vector2Int(t.x, t.y - 1);
            Vector2Int up = new Vector2Int(t.x, t.y + 1);

            int sx = t.x + 1;
            int sy = t.y + 1;

            if (!zoneTiles.Contains(left))
            {
                SetBlockedX_NoVisual(sx, sy, true);
                blockedEdges++;
            }

            if (!zoneTiles.Contains(right))
            {
                SetBlockedX_NoVisual(sx + 1, sy, true);
                blockedEdges++;
            }

            if (!zoneTiles.Contains(down))
            {
                SetBlockedY_NoVisual(sx, sy, true);
                blockedEdges++;
            }

            if (!zoneTiles.Contains(up))
            {
                SetBlockedY_NoVisual(sx, sy + 1, true);
                blockedEdges++;
            }
        }

        return blockedEdges;
    }

    private void DrawZoneBoundaryPreview(HashSet<Vector2Int> zoneTiles)
    {
        if (previewTilemap == null || previewTile == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] Cannot draw preview. previewTilemap or previewTile is missing.");
            return;
        }

        previewTilemap.ClearAllTiles();
        previewTilemap.color = previewColor;

        int previewCount = 0;

        foreach (Vector2Int t in zoneTiles)
        {
            if (!IsBoundaryTile(t, zoneTiles))
                continue;

            SetPreviewTile(t.x, t.y, previewTile);
            previewCount++;
        }

        previewTilemap.RefreshAllTiles();

        Debug.Log($"[FloodDefenseBoxStamp] Preview drawn. Boundary preview tiles={previewCount}");
    }

    private void PaintCommittedZoneOutline(HashSet<Vector2Int> zoneTiles)
    {
        if (!drawCommittedOutline)
            return;

        if (zoneOutlineTilemap == null || zoneOutlineTile == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] Cannot paint committed outline. zoneOutlineTilemap or zoneOutlineTile is missing.");
            return;
        }

        zoneOutlineTilemap.color = zoneOutlineColor;

        int paintedCount = 0;

        foreach (Vector2Int t in zoneTiles)
        {
            if (!IsBoundaryTile(t, zoneTiles))
                continue;

            Vector3Int cell = new Vector3Int(
                t.x + _tileOrigin.x,
                t.y + _tileOrigin.y,
                0
            );

            zoneOutlineTilemap.SetTile(cell, zoneOutlineTile);
            paintedCount++;
        }

        zoneOutlineTilemap.RefreshAllTiles();

        Debug.Log($"[FloodDefenseBoxStamp] Committed zone outline painted. Tiles={paintedCount}");
    }

    private bool IsBoundaryTile(Vector2Int tile, HashSet<Vector2Int> zoneTiles)
    {
        if (!zoneTiles.Contains(new Vector2Int(tile.x - 1, tile.y))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x + 1, tile.y))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x, tile.y - 1))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x, tile.y + 1))) return true;

        return false;
    }

    private void SetPreviewTile(int tx, int ty, TileBase tile)
    {
        if (previewTilemap == null)
            return;

        Vector3Int cell = new Vector3Int(
            tx + _tileOrigin.x,
            ty + _tileOrigin.y,
            0
        );

        previewTilemap.SetTile(cell, tile);
    }

    private void ClearHoverAndPreview()
    {
        _hoverGeoid = null;
        _hoverZoneTiles = null;
        ClearPreviewOnly();
    }

    private void ClearPreviewOnly()
    {
        if (previewTilemap != null)
        {
            previewTilemap.ClearAllTiles();
            previewTilemap.RefreshAllTiles();
        }
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[FloodDefenseBoxStamp] {message}");

        if (_npcToast != null)
            _npcToast.Show(message);
    }

    private void SetBlockedX_NoVisual(int x, int y, bool blocked)
    {
        if (_blockedX == null || _barrierHX == null || _seepX == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] SetBlockedX_NoVisual failed because X barrier arrays are null.");
            return;
        }

        if (x < 0 || y < 0 || x >= _blockedX.GetLength(0) || y >= _blockedX.GetLength(1))
            return;

        _blockedX[x, y] = blocked ? 1 : 0;
        _barrierHX[x, y] = blocked ? barrierHeight : 0f;
        _seepX[x, y] = blocked ? seepage : 0f;
    }

    private void SetBlockedY_NoVisual(int x, int y, bool blocked)
    {
        if (_blockedY == null || _barrierHY == null || _seepY == null)
        {
            Debug.LogWarning("[FloodDefenseBoxStamp] SetBlockedY_NoVisual failed because Y barrier arrays are null.");
            return;
        }

        if (x < 0 || y < 0 || x >= _blockedY.GetLength(0) || y >= _blockedY.GetLength(1))
            return;

        _blockedY[x, y] = blocked ? 1 : 0;
        _barrierHY[x, y] = blocked ? barrierHeight : 0f;
        _seepY[x, y] = blocked ? seepage : 0f;
    }

    private void UpdateGlobalHUDBarrierProgress()
    {
        if (GlobalHUDController.Instance != null)
        {
            GlobalHUDController.Instance.SetBarrierProgress(
                BarriersPlaced,
                ActualMaxZoneBarriers
            );
        }
    }

    // ---------- IBarrierProvider ----------

    public bool IsBlockedX(int x, int y)
    {
        if (_blockedX == null) return false;
        if (x < 0 || y < 0 || x >= _blockedX.GetLength(0) || y >= _blockedX.GetLength(1)) return false;

        return _blockedX[x, y] > 0;
    }

    public bool IsBlockedY(int x, int y)
    {
        if (_blockedY == null) return false;
        if (x < 0 || y < 0 || x >= _blockedY.GetLength(0) || y >= _blockedY.GetLength(1)) return false;

        return _blockedY[x, y] > 0;
    }

    public float GetBarrierHeightX(int x, int y)
    {
        if (_barrierHX == null) return 0f;
        if (x < 0 || y < 0 || x >= _barrierHX.GetLength(0) || y >= _barrierHX.GetLength(1)) return 0f;

        return _barrierHX[x, y];
    }

    public float GetBarrierHeightY(int x, int y)
    {
        if (_barrierHY == null) return 0f;
        if (x < 0 || y < 0 || x >= _barrierHY.GetLength(0) || y >= _barrierHY.GetLength(1)) return 0f;

        return _barrierHY[x, y];
    }

    public float GetSeepageX(int x, int y)
    {
        if (_seepX == null) return 0f;
        if (x < 0 || y < 0 || x >= _seepX.GetLength(0) || y >= _seepX.GetLength(1)) return 0f;

        return _seepX[x, y];
    }

    public float GetSeepageY(int x, int y)
    {
        if (_seepY == null) return 0f;
        if (x < 0 || y < 0 || x >= _seepY.GetLength(0) || y >= _seepY.GetLength(1)) return 0f;

        return _seepY[x, y];
    }
}