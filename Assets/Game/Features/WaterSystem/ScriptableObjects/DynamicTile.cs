using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/DynamicTile")]
public class DynamicTile : TileBase
{
    public Sprite fallbackSprite;
    public Color fallbackColor = Color.white;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = fallbackSprite;
        tileData.color = fallbackColor;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.None;
        tileData.colliderType = Tile.ColliderType.None;

        if (!Application.isPlaying) return;
        if (TileManager.Instance == null) return;

        TileInstance ti = TileManager.Instance.GetTileTypeAt(position);
        if (ti == null) return;

        // sprite (static fallback — animation will override at runtime if enabled)
        if (ti.tileType != null &&
            ti.tileType.isAnimated &&
            ti.tileType.animationFrames != null &&
            ti.tileType.animationFrames.Length > 0)
        {
            tileData.sprite = ti.tileType.animationFrames[0];
        }
        else
        {
            tileData.sprite = ti.sprite;
        }

        // IMPORTANT: use per-tile tint (for flood depth)
        tileData.color = ti.tint;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData data)
    {
        if (!Application.isPlaying) return false;
        if (TileManager.Instance == null) return false;

        TileInstance ti = TileManager.Instance.GetTileTypeAt(position);
        if (ti == null || ti.tileType == null) return false;

        var tt = ti.tileType;
        if (!tt.isWater) return false;
        if (!tt.isAnimated) return false;
        if (tt.animationFrames == null || tt.animationFrames.Length == 0) return false;

        data.animatedSprites = tt.animationFrames;
        data.animationSpeed = tt.animationSpeed;

        // Optional: stable per-cell offset so waves aren't perfectly in sync
        data.animationStartTime = (position.x * 0.37f + position.y * 0.73f) % 1f;

        return true;
    }
}