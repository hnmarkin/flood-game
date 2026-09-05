using NUnit.Framework;
using UnityEngine;

[Category("WaterUnit")]
public sealed class Dev_WaterPhysicsTests : Dev_WaterEditModeFixture
{
    // Tests: initialization and barriers

    [Test]
    public void Physics_Initialize_ClearsFlowAndConfiguresWallGhostTerrain()
    {
        // Arrange
        Dev_WaterRuntimeFixture fixture = CreateRuntime(2, 1, elevations: new[] { 2, 5 });
        fixture.State.FlowX[1, 1] = 9f;
        fixture.State.FlowY[1, 1] = 9f;
        fixture.Settings.boundaryHeightPadding = 3f;

        // Act
        fixture.Physics.Initialize(fixture.State, fixture.Settings);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.FlowX[1, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.FlowY[1, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Terrain[0, 1], Is.EqualTo(8f).Within(Tolerance));
            Assert.That(fixture.State.Terrain[3, 1], Is.EqualTo(8f).Within(Tolerance));
            Assert.That(fixture.State.Water[0, 1], Is.Zero.Within(Tolerance));
        });
    }

    [Test]
    public void BarrierGrid_ValidSetAndClear_PreservesAxisSpecificValues()
    {
        // Arrange
        Dev_WaterPhysicsBarrier barriers = new Dev_WaterPhysicsBarrier();
        Assert.That(barriers.InitializeForSimulation(4, 3), Is.True);

        // Act
        bool setX = barriers.TrySetBarrierX(2, 1, 3f, 0.25f);
        bool setY = barriers.TrySetBarrierY(1, 2, 4f, 0.5f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(setX, Is.True);
            Assert.That(setY, Is.True);
            Assert.That(barriers.GetBarrierHeightX(2, 1), Is.EqualTo(3f).Within(Tolerance));
            Assert.That(barriers.GetSeepageX(2, 1), Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(barriers.GetBarrierHeightY(1, 2), Is.EqualTo(4f).Within(Tolerance));
            Assert.That(barriers.GetSeepageY(1, 2), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(barriers.IsBlockedX(1, 2), Is.False);
        });

        Assert.That(barriers.TryClearBarrierX(2, 1), Is.True);
        Assert.That(barriers.IsBlockedX(2, 1), Is.False);
    }

    [TestCase(0f, 0f)]
    [TestCase(float.NaN, 0f)]
    [TestCase(1f, -0.1f)]
    public void BarrierGrid_InvalidHeightOrSeepage_RejectsMutation(float height, float seepage)
    {
        // Arrange
        Dev_WaterPhysicsBarrier barriers = new Dev_WaterPhysicsBarrier();
        Assert.That(barriers.InitializeForSimulation(3, 3), Is.True);

        // Act
        bool set = barriers.TrySetBarrierX(1, 1, height, seepage);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(set, Is.False);
            Assert.That(barriers.IsBlockedX(1, 1), Is.False);
        });
    }

    // Tests: sources and modifiers

    [Test]
    public void InitialSources_AllRegions_ApplyAbsoluteDepthToIntendedCells()
    {
        // Arrange
        bool[] waterBodies = { false, false, false, false, true, false, false, false, false };
        Dev_WaterRuntimeFixture fullMap = CreateRuntime(3, 3, waterBodies: waterBodies);
        Dev_WaterRuntimeFixture edges = CreateRuntime(3, 3, waterBodies: waterBodies);
        Dev_WaterRuntimeFixture corners = CreateRuntime(3, 3, waterBodies: waterBodies);
        Dev_WaterRuntimeFixture bodies = CreateRuntime(3, 3, waterBodies: waterBodies);

        // Act
        fullMap.Physics.ApplyInitialSources(new[] { Source(Dev_WaterSourceKind.FullMap, 2f) }, Defaults());
        edges.Physics.ApplyInitialSources(new[] { Source(Dev_WaterSourceKind.Edges, 2f) }, Defaults());
        corners.Physics.ApplyInitialSources(new[] { Source(Dev_WaterSourceKind.Corners, 2f) }, Defaults());
        bodies.Physics.ApplyInitialSources(new[] { Source(Dev_WaterSourceKind.ExistingWaterBodies, 2f) }, Defaults());

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(SumWater(fullMap.State), Is.EqualTo(18f).Within(Tolerance));
            Assert.That(SumWater(edges.State), Is.EqualTo(16f).Within(Tolerance));
            Assert.That(edges.State.Water[2, 2], Is.Zero.Within(Tolerance));
            Assert.That(SumWater(corners.State), Is.EqualTo(8f).Within(Tolerance));
            Assert.That(SumWater(bodies.State), Is.EqualTo(2f).Within(Tolerance));
            Assert.That(bodies.State.Water[2, 2], Is.EqualTo(2f).Within(Tolerance));
        });
    }

    [Test]
    public void InitialEdgeSource_OneByOneMap_DoesNotDuplicateCorner()
    {
        // Arrange
        Dev_WaterRuntimeFixture fixture = CreateRuntime(1, 1);

        // Act
        fixture.Physics.ApplyInitialSources(new[] { Source(Dev_WaterSourceKind.Edges, 3f) }, Defaults());

        // Assert
        Assert.That(fixture.State.Water[1, 1], Is.EqualTo(3f).Within(Tolerance));
    }

    [Test]
    public void ContinuousSource_ModifierScalingAndDeltaTime_AppliesResolvedDepthUnits()
    {
        // Arrange
        Dev_WaterRuntimeFixture fixture = CreateRuntime(1, 1);
        Dev_WaterSourceSpec source = Source(Dev_WaterSourceKind.FullMap, 2f);
        source.scaleByRainfallRate = true;
        source.scaleByExternalWaterLoad = true;
        source.scaleByAntecedentWetness = true;
        Dev_WaterModifierSnapshot modifiers = Defaults();
        modifiers.RainfallRate = 2f;
        modifiers.ExternalWaterLoad = 3f;
        modifiers.AntecedentWetness = 0.5f;

        // Act
        Dev_WaterStepSummary summary = fixture.Physics.Step(new[] { source }, modifiers, 0.25f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(1.5f).Within(Tolerance));
            Assert.That(summary.TotalWater, Is.EqualTo(1.5f).Within(Tolerance));
            Assert.That(summary.DeltaTime, Is.EqualTo(0.25f).Within(Tolerance));
        });
    }

    // Tests: flow, drainage, and bounds

    [Test]
    public void Physics_InternalFlowWithoutSourcesOrDrainage_ConservesWater()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.gravity = 9.81f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(3, 1, new[] { 3f, 0f, 0f }, settings: settings);
        float before = SumWater(fixture.State);

        // Act
        fixture.Physics.Step(null, Defaults(), 0.1f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(SumWater(fixture.State), Is.EqualTo(before).Within(Tolerance));
            Assert.That(fixture.State.Water[1, 1], Is.GreaterThanOrEqualTo(0f));
            Assert.That(fixture.State.Water[2, 1], Is.GreaterThanOrEqualTo(0f));
            Assert.That(fixture.State.Water[3, 1], Is.GreaterThanOrEqualTo(0f));
        });
    }

    [Test]
    public void Physics_DrainageEfficiency_RemovesTimeScaledDepthWithoutGoingNegative()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.baseDrainageDepthPerSecond = 2f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(2, 1, new[] { 2f, 0.5f }, settings: settings);
        Dev_WaterModifierSnapshot modifiers = Defaults();
        modifiers.DrainageEfficiency = 1.5f;

        // Act
        Dev_WaterStepSummary summary = fixture.Physics.Step(null, modifiers, 0.5f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(fixture.State.Water[2, 1], Is.Zero.Within(Tolerance));
            Assert.That(summary.TotalWater, Is.EqualTo(0.5f).Within(Tolerance));
        });
    }

    [Test]
    public void Physics_MaximumDepthConfigured_ClampsSourceInputAndSummary()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.maxWaterDepth = 2f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(1, 1, settings: settings);

        // Act
        Dev_WaterStepSummary summary = fixture.Physics.Step(
            new[] { Source(Dev_WaterSourceKind.FullMap, 100f) }, Defaults(), 1f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(2f).Within(Tolerance));
            Assert.That(summary.MaxDepth, Is.EqualTo(2f).Within(Tolerance));
        });
    }

    [Test]
    public void Physics_UnlimitedMaximumDepth_LargeFiniteSourceRemainsFiniteAndUnclamped()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.maxWaterDepth = 0f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(1, 1, settings: settings);

        // Act
        fixture.Physics.Step(new[] { Source(Dev_WaterSourceKind.FullMap, 1000000f) }, Defaults(), 1f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(1000000f).Within(0.1f));
            Assert.That(float.IsNaN(fixture.State.Water[1, 1]), Is.False);
            Assert.That(float.IsInfinity(fixture.State.Water[1, 1]), Is.False);
        });
    }

    [Test]
    public void Physics_SpreadGateInactiveNeighbor_PreventsFlowUntilIntervalExpansion()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.gravity = 9.81f;
        settings.useSpreadGating = true;
        settings.expandFromWaterThreshold = 0.1f;
        settings.spreadInterval = 1f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(2, 1, new[] { 1f, 0f }, settings: settings);
        fixture.Physics.InitializeActiveRegion();

        // Act
        fixture.Physics.Step(null, Defaults(), 0.25f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(1f).Within(Tolerance));
            Assert.That(fixture.State.Water[2, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.FlowX[2, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Active[2, 1], Is.False);
        });
    }

    [Test]
    public void Physics_ProjectionSpreadTimer_ExpandsAfterRemainingInterval()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.useSpreadGating = true;
        settings.expandFromWaterThreshold = 0.1f;
        settings.spreadInterval = 1f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(2, 1, new[] { 1f, 0f }, settings: settings);
        fixture.Physics.InitializeActiveRegion();

        // Act
        fixture.Physics.InitializeProjection(fixture.State, settings, 0.75f);
        fixture.Physics.TickSpreadGate(0.25f);

        // Assert
        Assert.That(fixture.State.Active[2, 1], Is.True);
    }

    // Tests: barriers and boundaries

    [TestCase(false)]
    [TestCase(true)]
    public void Physics_BarrierBelowSurface_OvertopsInBothAxes(bool yAxis)
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.gravity = 1f;
        settings.overtopDepthForFullFlow = 1f;
        Dev_WaterRuntimeFixture fixture = yAxis
            ? CreateRuntime(1, 2, new[] { 2f, 0f }, settings: settings)
            : CreateRuntime(2, 1, new[] { 2f, 0f }, settings: settings);
        if (yAxis)
            fixture.Barriers.TrySetBarrierY(1, 2, 1f);
        else
            fixture.Barriers.TrySetBarrierX(2, 1, 1f);

        // Act
        fixture.Physics.Step(null, Defaults(), 0.1f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(1.98f).Within(Tolerance));
            Assert.That(fixture.State.Water[yAxis ? 1 : 2, yAxis ? 2 : 1], Is.EqualTo(0.02f).Within(Tolerance));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Physics_BarrierAboveSurfaceWithSeepage_TransfersOnlySeepageInBothAxes(bool yAxis)
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.gravity = 1f;
        Dev_WaterRuntimeFixture fixture = yAxis
            ? CreateRuntime(1, 2, new[] { 1f, 0f }, settings: settings)
            : CreateRuntime(2, 1, new[] { 1f, 0f }, settings: settings);
        if (yAxis)
            fixture.Barriers.TrySetBarrierY(1, 2, 10f, 0.5f);
        else
            fixture.Barriers.TrySetBarrierX(2, 1, 10f, 0.5f);

        // Act
        fixture.Physics.Step(null, Defaults(), 0.1f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(0.95f).Within(Tolerance));
            Assert.That(fixture.State.Water[yAxis ? 1 : 2, yAxis ? 2 : 1], Is.EqualTo(0.05f).Within(Tolerance));
        });
    }

    [Test]
    public void Physics_BoundarySource_AddsConfiguredDepthPerSecondOnlyToSelectedEdge()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.northBoundary.mode = Dev_WaterBoundaryMode.Source;
        settings.northBoundary.sourceDepthPerSecond = 2f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(3, 2, settings: settings);

        // Act
        fixture.Physics.Step(null, Defaults(), 0.5f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 2], Is.EqualTo(1f).Within(Tolerance));
            Assert.That(fixture.State.Water[2, 2], Is.EqualTo(1f).Within(Tolerance));
            Assert.That(fixture.State.Water[3, 2], Is.EqualTo(1f).Within(Tolerance));
            Assert.That(fixture.State.Water[2, 1], Is.Zero.Within(Tolerance));
        });
    }

    [Test]
    public void Physics_BoundarySink_RemovesAllWaterFromSelectedEdge()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.northBoundary.mode = Dev_WaterBoundaryMode.Sink;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(3, 2, new[] { 1f, 1f, 1f, 1f, 1f, 1f }, settings: settings);

        // Act
        fixture.Physics.Step(null, Defaults(), 0.25f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 2], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[2, 2], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[3, 2], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[2, 1], Is.EqualTo(1f).Within(Tolerance));
        });
    }

    [Test]
    public void Physics_BoundaryWallSeepage_DrainsOnlyConfiguredTimeScaledDepth()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.northBoundary.seepageDepthPerSecond = 2f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(3, 2, new[] { 1f, 1f, 1f, 1f, 1f, 1f }, settings: settings);

        // Act
        fixture.Physics.Step(null, Defaults(), 0.25f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 2], Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(fixture.State.Water[2, 2], Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(fixture.State.Water[3, 2], Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(fixture.State.Water[2, 1], Is.EqualTo(1f).Within(Tolerance));
        });
    }

    [Test]
    public void Physics_StepSummary_ReportsStateAndKeepsGuardCellsDry()
    {
        // Arrange
        Dev_WaterRuntimeFixture fixture = CreateRuntime(2, 1, new[] { 1f, 0f });

        // Act
        Dev_WaterStepSummary summary = fixture.Physics.Step(null, Defaults(), 0.25f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(summary.StepIndex, Is.EqualTo(1));
            Assert.That(summary.WetTileCount, Is.EqualTo(1));
            Assert.That(summary.TotalWater, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(summary.MaxDepth, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(summary.DirtyTileCount, Is.EqualTo(fixture.State.DirtyCells.Count));
            Assert.That(fixture.State.Water[0, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[3, 1], Is.Zero.Within(Tolerance));
        });
    }

    // Fixture helpers

    private static Dev_WaterSourceSpec Source(Dev_WaterSourceKind kind, float depth)
    {
        return new Dev_WaterSourceSpec
        {
            kind = kind,
            depth = depth,
            scaleByExternalWaterLoad = false
        };
    }

    private static Dev_WaterModifierSnapshot Defaults()
    {
        return Dev_WaterModifierSnapshot.Defaults();
    }

    private static float SumWater(Dev_WaterState state)
    {
        float total = 0f;
        for (int y = 1; y <= state.Height; y++)
        {
            for (int x = 1; x <= state.Width; x++)
            {
                if (state.HasMapCellAtSim(x, y))
                    total += state.Water[x, y];
            }
        }

        return total;
    }
}
