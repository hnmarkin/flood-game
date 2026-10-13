using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class BarrierPlacementStateTests
{
    [Test]
    public void RecordCommittedZone_WritesLegacyProviderValues()
    {
        BarrierPlacementState state = new BarrierPlacementState();
        Assert.That(state.InitializeLegacyGrid(6, 6), Is.True);

        List<ZoneBoundaryEdge> edges = new List<ZoneBoundaryEdge>
        {
            new ZoneBoundaryEdge(
                ZoneBoundaryEdgeOrientation.X,
                new Vector2Int(1, 1),
                new Vector2Int(1, 1),
                new Vector2Int(0, 1),
                false),
            new ZoneBoundaryEdge(
                ZoneBoundaryEdgeOrientation.Y,
                new Vector2Int(2, 1),
                new Vector2Int(2, 1),
                new Vector2Int(2, 0),
                false)
        };

        bool committed = state.TryRecordCommittedZone(
            "zone-a",
            edges,
            7f,
            0.2f,
            out int blockedEdges,
            out string error);

        Assert.That(committed, Is.True, error);
        Assert.That(blockedEdges, Is.EqualTo(2));
        Assert.That(state.CommittedZoneCount, Is.EqualTo(1));
        Assert.That(state.IsBlockedX(2, 2), Is.True);
        Assert.That(state.GetBarrierHeightX(2, 2), Is.EqualTo(7f));
        Assert.That(state.GetSeepageX(2, 2), Is.EqualTo(0.2f));
        Assert.That(state.IsBlockedY(3, 2), Is.True);
    }

    [Test]
    public void RecordCommittedZone_RejectsDuplicateGeoid()
    {
        BarrierPlacementState state = new BarrierPlacementState();
        Assert.That(state.InitializeLegacyGrid(4, 4), Is.True);

        List<ZoneBoundaryEdge> edges = new List<ZoneBoundaryEdge>
        {
            new ZoneBoundaryEdge(
                ZoneBoundaryEdgeOrientation.X,
                new Vector2Int(1, 1),
                new Vector2Int(1, 1),
                new Vector2Int(0, 1),
                false)
        };

        Assert.That(state.TryRecordCommittedZone("zone-a", edges, 4f, 0f, out _, out _), Is.True);
        Assert.That(state.TryRecordCommittedZone("zone-a", edges, 4f, 0f, out _, out string error), Is.False);
        Assert.That(error, Does.Contain("already"));
    }

    [Test]
    public void CanStoreBoundaryEdges_RejectsOutOfRangeLegacyCoordinate()
    {
        BarrierPlacementState state = new BarrierPlacementState();
        Assert.That(state.InitializeLegacyGrid(3, 3), Is.True);

        List<ZoneBoundaryEdge> edges = new List<ZoneBoundaryEdge>
        {
            new ZoneBoundaryEdge(
                ZoneBoundaryEdgeOrientation.X,
                new Vector2Int(3, 1),
                new Vector2Int(2, 1),
                new Vector2Int(3, 1),
                true)
        };

        Assert.That(state.CanStoreBoundaryEdges(edges, out string error), Is.False);
        Assert.That(error, Does.Contain("outside"));
    }
}
