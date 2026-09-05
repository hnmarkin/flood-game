using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>One depth band in a new water visual definition.</summary>
[Serializable]
public sealed class WaterVisualBand
{
    [Min(0f)] public float minimumDepth;
    [Min(0f)] public float maximumDepth = 1000f;
    public TileBase tile;
    public Color tint = Color.white;

    public bool Contains(float depth)
    {
        return depth >= minimumDepth && depth <= maximumDepth;
    }
}

/// <summary>
/// Persistent presentation data consumed by the new renderer.
/// It does not depend on the former water visual model.
/// </summary>
[CreateAssetMenu(fileName = "RendererDef", menuName = "Dev/Water System/Renderer Definition")]
public sealed class RendererDef : ScriptableObject
{
    [SerializeField] private TileBase dryTile;
    [SerializeField] private Color dryTint = Color.white;
    [SerializeField] private WaterVisualBand[] waterBands;

    public TileBase DryTile => dryTile;
    public Color DryTint => dryTint;

    public void Configure(TileBase dry, Color tint, WaterVisualBand[] bands)
    {
        dryTile = dry;
        dryTint = tint;
        waterBands = bands ?? Array.Empty<WaterVisualBand>();
    }

    public TileBase ResolveTile(float waterDepth)
    {
        if (waterDepth <= 0f || waterBands == null)
            return dryTile;

        for (int i = 0; i < waterBands.Length; i++)
        {
            WaterVisualBand band = waterBands[i];
            if (band != null && band.Contains(waterDepth) && band.tile != null)
                return band.tile;
        }

        return dryTile;
    }

    public Color ResolveTint(float waterDepth)
    {
        if (waterDepth <= 0f || waterBands == null)
            return dryTint;

        for (int i = 0; i < waterBands.Length; i++)
        {
            WaterVisualBand band = waterBands[i];
            if (band != null && band.Contains(waterDepth))
                return band.tint;
        }

        return dryTint;
    }
}
