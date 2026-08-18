using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class ZoneOutlineByHover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileMapData tileMapData;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private JsonMapLoader jsonMapLoader;

    [Header("Hover Outline Tilemap")]
    [SerializeField] private Tilemap hoverOutlineTilemap;
    [SerializeField] private TileBase hoverOutlineTile;
    [SerializeField] private Color hoverOutlineColor = new Color(1f, 0.85f, 0.15f, 0.9f);

    [Header("Behavior")]
    [SerializeField] private bool outliningEnabled = true;
    [SerializeField] private bool clearWhenPointerOverUI = true;

    private Vector2Int _tileOrigin;

    private readonly Dictionary<string, HashSet<Vector2Int>> _geoidToTiles = new();

    private string _hoverGeoid = null;
    private HashSet<Vector2Int> _hoverZoneTiles = null;

    private IEnumerator Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        InitializeOrigin();

        yield return WaitForZoneLoaderReady();

        BuildZoneIndex();

        Debug.Log($"[ZoneOutlineByHover] Ready. Zones indexed={_geoidToTiles.Count}");
    }

    private void Update()
    {
        if (!outliningEnabled)
        {
            ClearHoverOutline();
            return;
        }

        if (!ValidateRuntimeReferences())
            return;

        if (clearWhenPointerOverUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            ClearHoverOutline();
            return;
        }

        UpdateHoverOutline();
    }

    private IEnumerator WaitForZoneLoaderReady()
    {
        int attempts = 0;
        int maxAttempts = 60;

        while (!IsZoneLoaderReady() && attempts < maxAttempts)
        {
            attempts++;
            yield return null;
        }

        if (!IsZoneLoaderReady())
        {
            Debug.LogWarning("[ZoneOutlineByHover] JsonMapLoader was not ready after waiting.");
        }
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

    private bool ValidateRuntimeReferences()
    {
        if (tileMapData == null)
            return false;

        if (terrainTilemap == null)
            return false;

        if (mainCamera == null)
            return false;

        if (jsonMapLoader == null)
            return false;

        if (hoverOutlineTilemap == null)
            return false;

        if (hoverOutlineTile == null)
            return false;

        return true;
    }

    private void InitializeOrigin()
    {
        if (terrainTilemap == null)
        {
            _tileOrigin = Vector2Int.zero;
            Debug.LogError("[ZoneOutlineByHover] terrainTilemap is missing. Tile origin set to zero.");
            return;
        }

        terrainTilemap.CompressBounds();
        BoundsInt bounds = terrainTilemap.cellBounds;

        _tileOrigin = new Vector2Int(bounds.xMin, bounds.yMin);

        Debug.Log($"[ZoneOutlineByHover] Tile origin={_tileOrigin}");
    }

    public void BuildZoneIndex()
    {
        _geoidToTiles.Clear();

        if (!IsZoneLoaderReady())
        {
            Debug.LogWarning("[ZoneOutlineByHover] BuildZoneIndex failed. JsonMapLoader is not ready.");
            return;
        }

        if (tileMapData == null)
        {
            Debug.LogWarning("[ZoneOutlineByHover] BuildZoneIndex failed. tileMapData is missing.");
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

        Debug.Log($"[ZoneOutlineByHover] BuildZoneIndex OK. Zones found={_geoidToTiles.Count}");
    }

    private void UpdateHoverOutline()
    {
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

        if (!hit || string.IsNullOrEmpty(geoid))
        {
            ClearHoverOutline();
            return;
        }

        if (!_geoidToTiles.TryGetValue(geoid, out HashSet<Vector2Int> zoneTiles))
        {
            ClearHoverOutline();
            return;
        }

        if (_hoverGeoid == geoid && _hoverZoneTiles == zoneTiles)
            return;

        _hoverGeoid = geoid;
        _hoverZoneTiles = zoneTiles;

        DrawHoverZoneBoundary(zoneTiles);
    }

    private void DrawHoverZoneBoundary(HashSet<Vector2Int> zoneTiles)
    {
        if (hoverOutlineTilemap == null || hoverOutlineTile == null)
            return;

        hoverOutlineTilemap.ClearAllTiles();
        hoverOutlineTilemap.color = hoverOutlineColor;

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

            hoverOutlineTilemap.SetTile(cell, hoverOutlineTile);
            paintedCount++;
        }

        hoverOutlineTilemap.RefreshAllTiles();

        Debug.Log($"[ZoneOutlineByHover] Hover outline painted. Boundary tiles={paintedCount}");
    }

    private bool IsBoundaryTile(Vector2Int tile, HashSet<Vector2Int> zoneTiles)
    {
        if (!zoneTiles.Contains(new Vector2Int(tile.x - 1, tile.y))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x + 1, tile.y))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x, tile.y - 1))) return true;
        if (!zoneTiles.Contains(new Vector2Int(tile.x, tile.y + 1))) return true;

        return false;
    }

    private void ClearHoverOutline()
    {
        _hoverGeoid = null;
        _hoverZoneTiles = null;

        if (hoverOutlineTilemap != null)
        {
            hoverOutlineTilemap.ClearAllTiles();
            hoverOutlineTilemap.RefreshAllTiles();
        }
    }

    public void SetOutliningEnabled(bool enabled)
    {
        outliningEnabled = enabled;

        if (!outliningEnabled)
            ClearHoverOutline();
    }

    public void ToggleOutlining()
    {
        outliningEnabled = !outliningEnabled;

        if (!outliningEnabled)
            ClearHoverOutline();
    }
}