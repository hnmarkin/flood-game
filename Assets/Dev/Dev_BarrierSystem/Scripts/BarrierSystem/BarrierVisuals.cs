using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class BarrierVisuals : MonoBehaviour
{
    private Tilemap previewTilemap;
    private TileBase previewTile;
    private Color previewColor = new Color(0.2f, 1f, 0.2f, 0.55f);

    private Tilemap zoneOutlineTilemap;
    private TileBase zoneOutlineTile;
    private Color zoneOutlineColor = new Color(1f, 0.85f, 0.15f, 1f);
    private bool drawCommittedOutline = true;

    private bool debugLogs = true;

    public bool HasPreviewSetup => previewTilemap != null && previewTile != null;

    public void Configure(
        Tilemap previewMap,
        TileBase preview,
        Color previewTint,
        Tilemap outlineMap,
        TileBase outline,
        Color outlineTint,
        bool drawOutline,
        bool enableDebugLogs)
    {
        previewTilemap = previewMap != null ? previewMap : previewTilemap;
        previewTile = preview != null ? preview : previewTile;
        previewColor = previewTint;
        zoneOutlineTilemap = outlineMap != null ? outlineMap : zoneOutlineTilemap;
        zoneOutlineTile = outline != null ? outline : zoneOutlineTile;
        zoneOutlineColor = outlineTint;
        drawCommittedOutline = drawOutline;
        debugLogs = enableDebugLogs;
    }

    public void DrawZoneBoundaryPreview(string geoid, ZoneController zoneController)
    {
        if (previewTilemap == null || previewTile == null)
        {
            Debug.LogWarning("[BarrierVisuals] Cannot draw preview. Preview Tilemap or Tile is missing.");
            return;
        }

        if (zoneController == null ||
            !zoneController.TryGetBoundaryTiles(geoid, out IReadOnlyList<Vector2Int> boundaryTiles))
        {
            return;
        }

        previewTilemap.ClearAllTiles();
        previewTilemap.color = previewColor;

        foreach (Vector2Int tile in boundaryTiles)
            previewTilemap.SetTile(zoneController.NormalizedTileToCell(tile), previewTile);

        previewTilemap.RefreshAllTiles();

        if (debugLogs)
            Debug.Log($"[BarrierVisuals] Preview drawn. Boundary preview tiles={boundaryTiles.Count}");
    }

    public void PaintCommittedZoneOutline(string geoid, ZoneController zoneController)
    {
        if (!drawCommittedOutline)
            return;

        if (zoneOutlineTilemap == null || zoneOutlineTile == null)
        {
            Debug.LogWarning("[BarrierVisuals] Cannot paint committed outline. Outline Tilemap or Tile is missing.");
            return;
        }

        if (zoneController == null ||
            !zoneController.TryGetBoundaryTiles(geoid, out IReadOnlyList<Vector2Int> boundaryTiles))
        {
            return;
        }

        zoneOutlineTilemap.color = zoneOutlineColor;

        foreach (Vector2Int tile in boundaryTiles)
            zoneOutlineTilemap.SetTile(zoneController.NormalizedTileToCell(tile), zoneOutlineTile);

        zoneOutlineTilemap.RefreshAllTiles();

        if (debugLogs)
            Debug.Log($"[BarrierVisuals] Committed zone outline painted. Tiles={boundaryTiles.Count}");
    }

    public void ClearPreview()
    {
        if (previewTilemap == null)
            return;

        previewTilemap.ClearAllTiles();
        previewTilemap.RefreshAllTiles();
    }
}
