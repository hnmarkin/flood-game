using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One complete terrain visual variant for a water-depth range. The tile swap is
/// the primary flooding presentation; tint is an optional finishing treatment.
/// </summary>
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

/// <summary>A complete tilemap visual resolved for one water depth.</summary>
public readonly struct WaterVisual
{
    public WaterVisual(TileBase tile, Color tint)
    {
        Tile = tile;
        Tint = tint;
    }

    public TileBase Tile { get; }
    public Color Tint { get; }
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

    /// <summary>
    /// Resolves the complete authored visual for a depth. Below the first band
    /// uses the dry visual; depths beyond the final band retain that final
    /// flooded variant. Adjacent bands share their boundary with the earlier
    /// band, matching their serialized order.
    /// </summary>
    public WaterVisual ResolveVisual(float waterDepth)
    {
        if (waterDepth <= 0f || waterBands == null || waterBands.Length == 0)
            return new WaterVisual(dryTile, dryTint);

        WaterVisualBand finalBand = null;

        for (int i = 0; i < waterBands.Length; i++)
        {
            WaterVisualBand band = waterBands[i];
            if (band == null || band.tile == null)
                continue;

            finalBand = band;
            if (band.Contains(waterDepth))
                return new WaterVisual(band.tile, band.tint);
        }

        if (finalBand != null && waterDepth > finalBand.maximumDepth)
            return new WaterVisual(finalBand.tile, finalBand.tint);

        return new WaterVisual(dryTile, dryTint);
    }

    public TileBase ResolveTile(float waterDepth)
    {
        return ResolveVisual(waterDepth).Tile;
    }

    public Color ResolveTint(float waterDepth)
    {
        return ResolveVisual(waterDepth).Tint;
    }

    public bool IsValidForProduction(out string error)
    {
        if (dryTile == null)
        {
            error = "Renderer Definition needs a dry tile.";
            return false;
        }

        if (waterBands == null || waterBands.Length == 0)
        {
            error = "Renderer Definition needs at least one flooded visual band.";
            return false;
        }

        float previousMaximum = 0f;
        for (int i = 0; i < waterBands.Length; i++)
        {
            WaterVisualBand band = waterBands[i];
            if (band == null)
            {
                error = $"Flooded visual band {i} is missing.";
                return false;
            }

            if (float.IsNaN(band.minimumDepth) || float.IsInfinity(band.minimumDepth) ||
                float.IsNaN(band.maximumDepth) || float.IsInfinity(band.maximumDepth) ||
                band.minimumDepth < 0f || band.maximumDepth < band.minimumDepth)
            {
                error = $"Flooded visual band {i} has an invalid depth range.";
                return false;
            }

            if (band.tile == null)
            {
                error = $"Flooded visual band {i} needs a replacement tile.";
                return false;
            }

            if (i > 0)
            {
                if (band.minimumDepth > previousMaximum)
                {
                    error = $"Flooded visual bands have a gap before band {i}.";
                    return false;
                }

                if (band.minimumDepth < previousMaximum)
                {
                    error = $"Flooded visual bands overlap before band {i}.";
                    return false;
                }
            }

            previousMaximum = band.maximumDepth;
        }

        error = null;
        return true;
    }
}
