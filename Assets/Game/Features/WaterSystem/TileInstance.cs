using UnityEngine;

public class TileInstance
{
    private Sprite _sprite;
    public Sprite sprite {
        get => _sprite;
        set {
            _sprite = value;
            if (Application.isPlaying && TileManager.Instance != null)
                TileManager.Instance.RefreshAt(new UnityEngine.Vector3Int(x, y, elevation));
        }
    }

    public TileType tileType;
    public int x;
    public int y;

    // JSON elev_level_1_10 maps to elevation
    public int elevation;

    public float waterHeight;

    
    public int population;

    public string geoid;     // JSON "GEOID"
    public string category;  // JSON "category" (building/road/land/water/etc)

    // Optional future fields
    public int econVal;
    public int damage;
    public int casualties;

    public Color tint = Color.white;

    public void SetTint(Color c)
    {
        tint = c;
        if (Application.isPlaying && TileManager.Instance != null)
            TileManager.Instance.RefreshAt(new UnityEngine.Vector3Int(x, y, elevation));
    }
}