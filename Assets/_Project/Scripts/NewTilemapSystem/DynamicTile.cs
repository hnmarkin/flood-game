using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/DynamicTile")]
public class DynamicTile : TileBase
{
    // Optional: default sprite if your lookup fails
    public Sprite fallbackSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // --- Retrieve your simulation data for this cell ---
        TileInstance type = TileManager.Instance.GetTileTypeAt(position);

        // --- Fill out Unity's render packet (TileData) ---
        if (type != null)
        {
            tileData.sprite = type.sprite;       // sprite chosen from TileType
            tileData.color = Color.white;             // or Color.white
            tileData.transform = Matrix4x4.identity;  // no skew/rotation unless desired
            tileData.flags = TileFlags.None;          // editable at runtime
            tileData.colliderType = Tile.ColliderType.None;
        }
        else
        {
            // fallback to avoid pink squares if data missing
            tileData.sprite = fallbackSprite;
            tileData.color = Color.white;
            tileData.transform = Matrix4x4.identity;
            tileData.flags = TileFlags.None;
            tileData.colliderType = Tile.ColliderType.None;
        }
    }
}
