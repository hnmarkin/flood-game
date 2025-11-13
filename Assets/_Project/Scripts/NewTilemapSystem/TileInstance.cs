using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInstance
{
    private Sprite _sprite;
    public Sprite sprite {
        get => _sprite;
        set {
            _sprite = value;
            TileManager.Instance.RefreshAt(new Vector3Int(this.x, this.y, this.elevation));
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