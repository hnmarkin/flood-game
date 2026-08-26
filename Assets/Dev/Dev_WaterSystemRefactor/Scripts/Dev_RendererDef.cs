using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>One depth band in a new water visual definition.</summary>
[Serializable]
public sealed class Dev_WaterVisualBand
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
[CreateAssetMenu(fileName = "Dev_RendererDef", menuName = "Dev/Water System/Renderer Definition")]
public sealed class Dev_RendererDef : ScriptableObject
{
    [SerializeField] private TileBase dryTile;
    [SerializeField] private Color dryTint = Color.white;
    [SerializeField] private Dev_WaterVisualBand[] waterBands;

    public TileBase DryTile => dryTile;
    public Color DryTint => dryTint;

    public void Configure(TileBase dry, Color tint, Dev_WaterVisualBand[] bands)
    {
        dryTile = dry;
        dryTint = tint;
        waterBands = bands ?? Array.Empty<Dev_WaterVisualBand>();
    }

    public TileBase ResolveTile(float waterDepth)
    {
        if (waterDepth <= 0f || waterBands == null)
            return dryTile;

        for (int i = 0; i < waterBands.Length; i++)
        {
            Dev_WaterVisualBand band = waterBands[i];
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
            Dev_WaterVisualBand band = waterBands[i];
            if (band != null && band.Contains(waterDepth))
                return band.tint;
        }

        return dryTint;
    }
}
