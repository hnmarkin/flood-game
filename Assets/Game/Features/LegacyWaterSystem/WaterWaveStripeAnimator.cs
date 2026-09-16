using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterWaveStripeAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileMapData grid;
    [SerializeField] private Tilemap waterTilemap;

    [Header("Sprites")]
    [SerializeField] private Sprite plainWater;

    [Tooltip("Optional: wave shades for fading (assign what you have).")]
    [SerializeField] private Sprite waveLight;
    [SerializeField] private Sprite waveMedium;
    [SerializeField] private Sprite waveDark;

    [Header("Wave motion")]
    [SerializeField] private int stripeSpacing = 8;      // distance between stripes
    [SerializeField] private int stripeThickness = 4;    // thickness of the moving band
    [SerializeField] private float waveSpeed = 3.0f;     // tiles/sec downwards


    [Header("Variation")]
    [Range(0f, 1f)]
    [SerializeField] private float coverage = 0.65f;     // 1 = solid stripe, 0.5 = patchy
    [SerializeField] private int wiggle = 1;             // 0=straight, 1-2=slight waviness
    [SerializeField] private float phaseJitter = 0.6f;   // desync tiles (prevents barcode feel)
    [SerializeField] private int seed = 12345;

    [Header("Skip small water bodies")]
    [SerializeField] private int minTotalWaterTilesToAnimate = 10;

    private readonly List<Vector2Int> waterCells = new();
    private float offset;
    private bool isRunning = true;

    void Start()
    {
        CacheWaterCells();

        if (waterCells.Count < minTotalWaterTilesToAnimate)
            enabled = false;
    }

    void Update()
    {
        if (!isRunning) return;

        offset += waveSpeed * Time.deltaTime;
        Animate();
    }

    public void Recache() => CacheWaterCells();

    // Hook this to your Start Flood button (OnClick)
    public void StopWaves()
    {
        isRunning = false;

        // Optional: snap visuals back to plain water immediately
        ResetToPlainWater();

        // Optional: disable the component entirely
        // enabled = false;
    }

    private void ResetToPlainWater()
    {
        if (plainWater == null) return;

        foreach (var p in waterCells)
        {
            var ti = grid.Get(p);
            if (ti == null) continue;

            if (ti.sprite != plainWater)
                ti.sprite = plainWater;
        }
    }


    private void CacheWaterCells()
    {
        waterCells.Clear();
        if (grid == null || waterTilemap == null) return;

        var b = waterTilemap.cellBounds;
        for (int x = b.xMin; x < b.xMax; x++)
        for (int y = b.yMin; y < b.yMax; y++)
        {
            var cell = new Vector3Int(x, y, 0);
            if (!waterTilemap.HasTile(cell)) continue;

            var ti = grid.Get(new Vector2Int(x, y));
            if (ti?.tileType == null) continue;
            if (!ti.tileType.isWater) continue;

            waterCells.Add(new Vector2Int(x, y));
        }

        Debug.Log($"[WaterWaveStripeAnimator] Found {waterCells.Count} water tiles.");
    }

    private void Animate()
    {
        if (plainWater == null) return;
        if (stripeSpacing <= 0) stripeSpacing = 1;
        if (stripeThickness <= 0) stripeThickness = 1;

        int baseOff = Mathf.FloorToInt(offset);

        foreach (var p in waterCells)
        {
            var ti = grid.Get(p);
            if (ti == null) continue;

            // ---- 1) WIGGLE: shift the stripe slightly per column so it’s not perfectly straight
            int columnShift = wiggle <= 0 ? 0 : Mathf.RoundToInt((Hash01(p.x, seed) - 0.5f) * 2f * wiggle);

            // ---- 2) DESYNC: per-tile phase offset so tiles don't flip in sync
            int localOff = baseOff + Mathf.FloorToInt(Hash01(p.x, p.y) * phaseJitter * stripeSpacing);

            // Stripe position along Y
            int yIndex = p.y + localOff + columnShift;

            // Position within the stripe cycle (0..stripeSpacing-1)
            int m = Mod(yIndex, stripeSpacing);

            // Stripe occupies [0..stripeThickness-1]
            bool inBand = (m < stripeThickness);

            // ---- 3) PATCHINESS: only some tiles in the band become wave tiles (no solid barcode)
            bool passesCoverage = Hash01(p.x + seed, p.y - seed) < coverage;

            if (!inBand || !passesCoverage)
            {
                if (ti.sprite != plainWater) ti.sprite = plainWater;
                continue;
            }

            // ---- 4) FADING: choose light/med/dark based on distance from band center
            // Normalize 0 at center, 1 at edges
            float center = (stripeThickness - 1) * 0.5f;
            float dist = Mathf.Abs(m - center);
            float t = (center <= 0.0001f) ? 0f : Mathf.Clamp01(dist / center);

            // t=0 (center) -> darkest, t=1 (edge) -> lightest
            Sprite target = PickFadeSprite(t);

            if (target == null) target = waveMedium != null ? waveMedium : plainWater;

            if (ti.sprite != target) ti.sprite = target;
        }
    }

    private Sprite PickFadeSprite(float t)
    {
        // Prefer 3 shades if available
        if (waveLight != null && waveMedium != null && waveDark != null)
        {
            // edges light, middle dark
            if (t > 0.66f) return waveLight;
            if (t > 0.33f) return waveMedium;
            return waveDark;
        }

        // Fallback if only one wave sprite assigned
        if (waveMedium != null) return waveMedium;
        if (waveDark != null) return waveDark;
        if (waveLight != null) return waveLight;
        return null;
    }

    private static int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }

    private static float Hash01(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h ^= (uint)(y * 2654435761u);
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= (h >> 16);
            return (h & 0x00FFFFFF) / 16777215f;
        }
    }

    
}
