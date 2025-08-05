using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TileHeightRange
{
    [Header("Tile Configuration")]
    public TileBase tile;
    
    [Header("Height Range")]
    public float minHeight = 0f;
    public float maxHeight = 1f;
    
    [Header("Display")]
    public string name = "Terrain Type";
    
    public TileHeightRange()
    {
        tile = null;
        minHeight = 0f;
        maxHeight = 1f;
        name = "Terrain Type";
    }
    
    public TileHeightRange(TileBase tile, float minHeight, float maxHeight, string name = "")
    {
        this.tile = tile;
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;
        this.name = string.IsNullOrEmpty(name) ? (tile ? tile.name : "Terrain Type") : name;
    }
    
    /// <summary>
    /// Checks if this tile type should be used for the given height
    /// </summary>
    public bool ContainsHeight(float height) => height >= minHeight && height <= maxHeight;
    
    /// <summary>
    /// Gets the height range as a string for display
    /// </summary>
    public string GetRangeString() => $"{minHeight:F2} - {maxHeight:F2}";
}

[CreateAssetMenu(fileName = "TileHeightConfiguration", menuName = "Flood/Tile Height Configuration")]
public class TileHeightConfiguration : ScriptableObject
{
    [Header("Height-Based Tile Selection")]
    [SerializeField] private List<TileHeightRange> heightRanges = new List<TileHeightRange>();
    
    [Header("Settings")]
    [SerializeField] private bool enableHeightBasedSelection = true;
    [SerializeField] private TileBase fallbackTile;
    
    public List<TileHeightRange> HeightRanges => heightRanges;
    public bool EnableHeightBasedSelection => enableHeightBasedSelection;
    public TileBase FallbackTile => fallbackTile;
    
    /// <summary>
    /// Gets the appropriate tile for a given height value using height ranges
    /// </summary>
    /// <param name="height">The height value to find a tile for</param>
    /// <returns>The TileBase that best matches the height, or fallback tile if no match found</returns>
    public TileBase GetTileForHeight(float height)
    {
        if (!enableHeightBasedSelection || heightRanges == null || heightRanges.Count == 0)
            return fallbackTile;
        
        // First try: Find exact range match
        foreach (var range in heightRanges)
        {
            if (range.tile != null && range.ContainsHeight(height))
            {
                return range.tile;
            }
        }
        
        // Second try: Find closest by distance to range
        TileBase bestTile = null;
        float closestDistance = float.MaxValue;
        
        foreach (var range in heightRanges)
        {
            if (range.tile == null) continue;
            
            // Calculate distance to the range
            float distance;
            if (height < range.minHeight)
                distance = range.minHeight - height;
            else if (height > range.maxHeight)
                distance = height - range.maxHeight;
            else
                distance = 0f; // Within range
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTile = range.tile;
            }
        }
        
        return bestTile ?? fallbackTile;
    }
    
    /// <summary>
    /// Gets the height range data for a given height value
    /// </summary>
    /// <param name="height">The height value to find range data for</param>
    /// <returns>The TileHeightRange that best matches the height, or null if no match found</returns>
    public TileHeightRange GetRangeForHeight(float height)
    {
        if (!enableHeightBasedSelection || heightRanges == null || heightRanges.Count == 0)
            return null;
        
        // First try: Find exact range match
        foreach (var range in heightRanges)
        {
            if (range.tile != null && range.ContainsHeight(height))
            {
                return range;
            }
        }
        
        // Second try: Find closest by distance to range
        TileHeightRange bestRange = null;
        float closestDistance = float.MaxValue;
        
        foreach (var range in heightRanges)
        {
            if (range.tile == null) continue;
            
            // Calculate distance to the range
            float distance;
            if (height < range.minHeight)
                distance = range.minHeight - height;
            else if (height > range.maxHeight)
                distance = height - range.maxHeight;
            else
                distance = 0f; // Within range
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestRange = range;
            }
        }
        
        return bestRange;
    }
    
    /// <summary>
    /// Validates height ranges for overlaps and gaps
    /// </summary>
    /// <returns>List of validation messages</returns>
    public List<string> ValidateHeightRanges()
    {
        var issues = new List<string>();
        
        if (heightRanges == null || heightRanges.Count == 0)
        {
            issues.Add("No height ranges defined");
            return issues;
        }
        
        // Check for invalid ranges
        for (int i = 0; i < heightRanges.Count; i++)
        {
            var range = heightRanges[i];
            if (range.minHeight > range.maxHeight)
            {
                issues.Add($"Range {i} ({range.name}): Min height ({range.minHeight:F2}) is greater than max height ({range.maxHeight:F2})");
            }
            
            if (range.tile == null)
            {
                issues.Add($"Range {i} ({range.name}): No tile assigned");
            }
        }
        
        // Check for overlaps
        for (int i = 0; i < heightRanges.Count; i++)
        {
            for (int j = i + 1; j < heightRanges.Count; j++)
            {
                var range1 = heightRanges[i];
                var range2 = heightRanges[j];
                
                if (range1.tile == null || range2.tile == null) continue;
                
                // Check if ranges overlap
                if (!(range1.maxHeight < range2.minHeight || range2.maxHeight < range1.minHeight))
                {
                    issues.Add($"Range {i} ({range1.name}) and {j} ({range2.name}) have overlapping height ranges");
                }
            }
        }
        
        return issues;
    }
    
    /// <summary>
    /// Adds a new height range
    /// </summary>
    public void AddHeightRange(TileBase tile, float minHeight, float maxHeight, string name = "")
    {
        heightRanges.Add(new TileHeightRange(tile, minHeight, maxHeight, name));
    }
    
    /// <summary>
    /// Removes a height range at the specified index
    /// </summary>
    public void RemoveHeightRange(int index)
    {
        if (index >= 0 && index < heightRanges.Count)
        {
            heightRanges.RemoveAt(index);
        }
    }
    
    /// <summary>
    /// Auto-distributes height ranges evenly across 0-1 range
    /// </summary>
    public void AutoDistributeRanges()
    {
        if (heightRanges.Count <= 1) return;
        
        float rangeSize = 1f / heightRanges.Count;
        
        for (int i = 0; i < heightRanges.Count; i++)
        {
            heightRanges[i].minHeight = i * rangeSize;
            heightRanges[i].maxHeight = (i + 1) * rangeSize;
        }
    }
}
