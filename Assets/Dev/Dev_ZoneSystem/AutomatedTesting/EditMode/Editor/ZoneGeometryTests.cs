using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class ZoneGeometryTests
{
    [Test]
    public void BoundaryTiles_UseFourNeighborAdjacency()
    {
        HashSet<Vector2Int> zoneTiles = new HashSet<Vector2Int>
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2),
        };

        List<Vector2Int> boundaryTiles = ZoneGeometry.CalculateBoundaryTiles(zoneTiles);

        Assert.That(boundaryTiles, Has.Count.EqualTo(8));
        Assert.That(boundaryTiles.Contains(new Vector2Int(1, 1)), Is.False);
    }

    [Test]
    public void BoundaryEdges_DeduplicateAndUseDeterministicOrder()
    {
        HashSet<Vector2Int> zoneTiles = new HashSet<Vector2Int>
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
        };

        List<ZoneBoundaryEdge> edges =
            ZoneGeometry.CalculateBoundaryEdges(zoneTiles, new Vector2Int(2, 1));

        Assert.That(edges, Has.Count.EqualTo(6));
        Assert.That(edges[0].Orientation, Is.EqualTo(ZoneBoundaryEdgeOrientation.X));
        Assert.That(edges[0].GridLine, Is.EqualTo(new Vector2Int(0, 0)));
        Assert.That(edges[1].Orientation, Is.EqualTo(ZoneBoundaryEdgeOrientation.X));
        Assert.That(edges[1].GridLine, Is.EqualTo(new Vector2Int(2, 0)));
        Assert.That(edges[2].Orientation, Is.EqualTo(ZoneBoundaryEdgeOrientation.Y));
        Assert.That(edges[2].GridLine, Is.EqualTo(new Vector2Int(0, 0)));
        Assert.That(edges[5].GridLine, Is.EqualTo(new Vector2Int(1, 1)));
    }

    [Test]
    public void BoundaryEdges_MarkMapExteriorSeparatelyFromInternalMapEdges()
    {
        HashSet<Vector2Int> zoneTiles = new HashSet<Vector2Int>
        {
            new Vector2Int(1, 1),
        };

        List<ZoneBoundaryEdge> edges =
            ZoneGeometry.CalculateBoundaryEdges(zoneTiles, new Vector2Int(3, 3));

        Assert.That(edges, Has.Count.EqualTo(4));

        foreach (ZoneBoundaryEdge edge in edges)
            Assert.That(edge.TouchesMapExterior, Is.False);
    }

    [Test]
    public void BoundaryEdge_LegacyCoordinate_IsDerivedFromNormalizedGridLine()
    {
        ZoneBoundaryEdge edge = new ZoneBoundaryEdge(
            ZoneBoundaryEdgeOrientation.X,
            new Vector2Int(2, 3),
            new Vector2Int(2, 3),
            new Vector2Int(1, 3),
            false);

        Assert.That(edge.ToLegacyBarrierCoordinate(), Is.EqualTo(new Vector2Int(3, 4)));
    }
}
