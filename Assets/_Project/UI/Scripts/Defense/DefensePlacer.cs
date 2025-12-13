using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DefensePlacer : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private Tilemap defenseTilemap;
    [SerializeField] private Tilemap hoverTilemap;
    [SerializeField] private TileBase hoverTile;

    private Vector3Int _lastCell = new(int.MinValue, int.MinValue, int.MinValue);
    private Vector3Int _lastPlacedCell = new(int.MinValue, int.MinValue, int.MinValue);

    // Update is called once per frame
    void Update()
    {
        if (!toolManager.IsPlacing)
        {
            // Not placing mode: clear any existing hover state
            ClearHover();
            return;
        }

        var tool = toolManager.ActiveTool;
        var mp = Input.mousePosition;
        mp.z = -cam.transform.position.z;   // distance from camera to z=0 plane (works if camera looks toward +z)
        Vector3 world = cam.ScreenToWorldPoint(mp);
        world.z = 0f;
        Vector3Int cell = defenseTilemap.WorldToCell(world);

        // If mouse is not held, we're only hovering
        if (!Input.GetMouseButton(0))
        {
            // If we're already hovering on this cell, nothing to do
            if (cell == _lastCell) return;

            // Otherwise, remove previous hover and place a new one
            ClearHover();
            PlaceHover(cell);
            Debug.Log("Hovering over cell: " + cell);
            return;
        }

        // Mouse is held down - attempt placement
        // If this is the same cell we've already placed on, skip to avoid spam
        if (cell == _lastPlacedCell) return;

        // Remove hover (if any) and place tile
        ClearHover();
        defenseTilemap.SetTile(cell, tool.tileToPaint);
        _lastPlacedCell = cell;
        Debug.Log("Placed defense at cell: " + cell);
        // TODO: apply simulation effects to your flood grid at [cell.x, cell.y]
    }

    private void ClearHover()
    {
        if (!IsInvalidCell(_lastCell))
        {
            // Only clear if a hover tile is present
            var existing = hoverTilemap.GetTile(_lastCell);
            if (existing == hoverTile)
                hoverTilemap.SetTile(_lastCell, null);
            _lastCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        }
    }

    private void PlaceHover(Vector3Int cell)
    {
        hoverTilemap.SetTile(cell, hoverTile);
        _lastCell = cell;
    }

    private static bool IsInvalidCell(Vector3Int c) => c.x == int.MinValue && c.y == int.MinValue;
}