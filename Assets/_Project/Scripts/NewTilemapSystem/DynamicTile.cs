using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/DynamicTile")]
public class DynamicTile : TileBase
{
    public Sprite fallbackSprite;
    public Color fallbackColor = Color.white;

    public override void GetTileData(
        Vector3Int position,
        ITilemap tilemap,
        ref TileData tileData)
    {
        // Default values
        tileData.sprite = fallbackSprite;
        tileData.color = fallbackColor;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.None;
        tileData.colliderType = Tile.ColliderType.None;

        // Only use runtime data when the game is actually playing
        if (!Application.isPlaying)
            return;

        if (TileManager.Instance == null)
            return;

        TileInstance type = TileManager.Instance.GetTileTypeAt(position);
        if (type == null)
            return;

        // Override with simulation-driven visuals
        tileData.sprite = type.sprite;
        tileData.color = Color.white;
    }
}
