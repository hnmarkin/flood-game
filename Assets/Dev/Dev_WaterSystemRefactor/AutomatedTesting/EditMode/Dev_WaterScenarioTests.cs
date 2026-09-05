using NUnit.Framework;

[Category("WaterScenario")]
public sealed class Dev_WaterScenarioTests : Dev_WaterEditModeFixture
{
    // Tests: near-zero starting water

    [Test]
    public void NearZeroStartingWater_ThresholdBoundary_ClearsInsignificantGroundSeepageOnly()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.useSpreadGating = true;
        settings.expandFromWaterThreshold = 0.001f;
        settings.spreadInterval = 10f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(
            3,
            1,
            new[] { 0.0009f, 0.001f, 0.0011f },
            settings: settings);
        fixture.Physics.InitializeActiveRegion();

        // Act
        Dev_WaterStepSummary summary = fixture.Physics.Step(null, Dev_WaterModifierSnapshot.Defaults(), 0.25f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[2, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[3, 1], Is.EqualTo(0.0011f).Within(Tolerance));
            Assert.That(fixture.State.Active[1, 1], Is.False);
            Assert.That(fixture.State.Active[2, 1], Is.False);
            Assert.That(fixture.State.Active[3, 1], Is.True);
            Assert.That(summary.WetTileCount, Is.EqualTo(1));
        });
    }

    // Tests: heavy external inflow

    [Test]
    public void HeavyExternalInflow_EdgeSource_LoadsEachEdgeCellOnceAndKeepsStateFinite()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.maxWaterDepth = 0f;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(3, 3, settings: settings);
        Dev_WaterSourceSpec source = new Dev_WaterSourceSpec
        {
            kind = Dev_WaterSourceKind.Edges,
            depth = 1000f,
            scaleByExternalWaterLoad = true
        };
        Dev_WaterModifierSnapshot modifiers = Dev_WaterModifierSnapshot.Defaults();
        modifiers.ExternalWaterLoad = 20f;

        // Act
        Dev_WaterStepSummary summary = fixture.Physics.Step(new[] { source }, modifiers, 0.5f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(10000f).Within(0.01f));
            Assert.That(fixture.State.Water[2, 1], Is.EqualTo(10000f).Within(0.01f));
            Assert.That(fixture.State.Water[2, 2], Is.Zero.Within(Tolerance));
            Assert.That(summary.WetTileCount, Is.EqualTo(8));
            Assert.That(summary.TotalWater, Is.EqualTo(80000f).Within(0.1f));
            Assert.That(summary.MaxDepth, Is.EqualTo(10000f).Within(0.01f));
            Assert.That(float.IsNaN(summary.TotalWater) || float.IsInfinity(summary.TotalWater), Is.False);
            Assert.That(fixture.State.Water[0, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[4, 3], Is.Zero.Within(Tolerance));
        });
    }

    // Tests: rainfall and drainage balance

    [TestCase(1f, 2f, 2f, 3f, 1f, 1, 2f, TestName = "HighRainfallAndDrainage_PositiveNetChange_AddsHandCalculatedDepth")]
    [TestCase(1f, 2f, 2f, 2f, 2f, 1, 1f, TestName = "HighRainfallAndDrainage_ZeroNetChange_PreservesDepth")]
    [TestCase(2f, 1f, 1f, 3f, 1f, 1, 0f, TestName = "HighRainfallAndDrainage_NegativeNetChange_ClampsAtZero")]
    [TestCase(1f, 3f, 1f, 1f, 1f, 4, 3f, TestName = "HighRainfallAndDrainage_MultipleQuarterSteps_RemainHandCalculable")]
    public void HighRainfallAndDrainage_NetChange_RemainsBoundedAndNonNegative(
        float initialDepth,
        float rainDepthPerSecond,
        float rainfallRate,
        float drainageDepthPerSecond,
        float drainageEfficiency,
        int stepCount,
        float expectedDepth)
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.baseDrainageDepthPerSecond = drainageDepthPerSecond;
        Dev_WaterRuntimeFixture fixture = CreateRuntime(1, 1, new[] { initialDepth }, settings: settings);
        Dev_WaterSourceSpec rainfall = new Dev_WaterSourceSpec
        {
            kind = Dev_WaterSourceKind.Rainfall,
            depth = rainDepthPerSecond,
            scaleByExternalWaterLoad = false
        };
        Dev_WaterModifierSnapshot modifiers = Dev_WaterModifierSnapshot.Defaults();
        modifiers.RainfallRate = rainfallRate;
        modifiers.DrainageEfficiency = drainageEfficiency;
        float deltaTime = stepCount == 1 ? 1f : 0.25f;

        // Act
        Dev_WaterStepSummary summary = default;
        for (int i = 0; i < stepCount; i++)
            summary = fixture.Physics.Step(new[] { rainfall }, modifiers, deltaTime);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.State.Water[1, 1], Is.EqualTo(expectedDepth).Within(Tolerance));
            Assert.That(fixture.State.Water[1, 1], Is.GreaterThanOrEqualTo(0f));
            Assert.That(summary.TotalWater, Is.EqualTo(expectedDepth).Within(Tolerance));
            Assert.That(summary.StepIndex, Is.EqualTo(stepCount));
            Assert.That(float.IsNaN(summary.TotalWater) || float.IsInfinity(summary.TotalWater), Is.False);
        });
    }
}
