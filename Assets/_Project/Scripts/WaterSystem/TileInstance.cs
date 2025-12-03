using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInstance
{
    private Sprite _sprite;
    public Sprite sprite {
        get => _sprite;
        set {
            _sprite = value;
            if (Application.isPlaying && TileManager.Instance != null)
            {
                TileManager.Instance.RefreshAt(new Vector3Int(x, y, elevation));
            }
        }
    }

    public TileType tileType;
    public int x;
    public int y;
    public int elevation;
    public float waterHeight;
    public int population;
    public int econVal;
    public int damage;
    public int casualties;
}