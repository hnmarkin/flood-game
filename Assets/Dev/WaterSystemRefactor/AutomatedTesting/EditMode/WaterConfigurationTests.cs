using System;
using NUnit.Framework;
using UnityEngine;

[Category("WaterUnit")]
public sealed class WaterConfigurationTests : WaterEditModeFixture
{
    // Tests: settings and value validation

    [TestCase("dt")]
    [TestCase("gravity")]
    [TestCase("friction")]
    [TestCase("spreadLayers")]
    [TestCase("overtop")]
    public void SimulationSettings_InvalidField_RejectsConfiguration(string invalidField)
    {
        // Arrange
        WaterSimulationSettings settings = CreateSettings();
        switch (invalidField)
        {
            case "dt": settings.dt = float.NaN; break;
            case "gravity": settings.gravity = -1f; break;
            case "friction": settings.friction = 1f; break;
            case "spreadLayers": settings.spreadLayersPerTick = 0; break;
            case "overtop": settings.overtopDepthForFullFlow = 0f; break;
        }

        // Act
        bool isValid = settings.IsValid(out string error);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Is.Not.Empty);
        });
    }

    [Test]
    public void SimulationSettings_ZeroMaximumDepth_RemainsValidUnlimitedContract()
    {
        // Arrange
        WaterSimulationSettings settings = CreateSettings();
        settings.maxWaterDepth = 0f;

        // Act
        bool isValid = settings.IsValid(out string error);

        // Assert
        Assert.That(isValid, Is.True, error);
    }

    [Test]
    public void SimulationSettings_Clone_DeepCopiesBoundaryConfiguration()
    {
        // Arrange
        WaterSimulationSettings settings = CreateSettings();
        settings.northBoundary.mode = WaterBoundaryMode.Source;
        settings.northBoundary.sourceDepthPerSecond = 3f;

        // Act
        WaterSimulationSettings clone = settings.Clone();

        // Assert
        clone.northBoundary.sourceDepthPerSecond = 8f;
        WaterAssert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(settings));
            Assert.That(clone.northBoundary, Is.Not.SameAs(settings.northBoundary));
            Assert.That(settings.northBoundary.sourceDepthPerSecond, Is.EqualTo(3f).Within(Tolerance));
        });
    }

    [Test]
    public void BoundarySettings_SourceWithoutPositiveRate_RejectsConfiguration()
    {
        // Arrange
        WaterBoundarySettings boundary = new WaterBoundarySettings
        {
            mode = WaterBoundaryMode.Source,
            sourceDepthPerSecond = 0f
        };

        // Act
        bool isValid = boundary.IsValid(out string error);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("positive"));
        });
    }

    [Test]
    public void SourceSpec_NonFiniteDepth_RejectsConfiguration()
    {
        // Arrange
        WaterSourceSpec source = new WaterSourceSpec { initialDepth = float.PositiveInfinity };

        // Act
        bool isValid = source.IsValid(out string error);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("finite"));
        });
    }

    [Test]
    public void ModifierSnapshot_NonFiniteValues_RejectsConfiguration()
    {
        // Arrange
        WaterModifierSnapshot modifiers = WaterModifierSnapshot.Defaults();
        modifiers.RainfallRate = float.NaN;

        // Act
        bool isValid = modifiers.IsValid(out string error);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("finite"));
        });
    }

    [Test]
    public void ModifierSnapshot_InvalidValues_SanitizesToFiniteDefaults()
    {
        // Arrange
        WaterModifierSnapshot modifiers = WaterModifierSnapshot.Defaults();
        modifiers.DrainageEfficiency = -1f;
        modifiers.EventPacing = float.PositiveInfinity;
        modifiers.WindDirection = Vector2.zero;

        // Act
        modifiers.Sanitize();

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(modifiers.DrainageEfficiency, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(modifiers.EventPacing, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(modifiers.WindDirection, Is.EqualTo(Vector2.right));
            Assert.That(modifiers.IsValid(out _), Is.True);
        });
    }

    [Test]
    public void StormProfile_CreateInstances_ClonesSettingsSourcesAndBoundaries()
    {
        // Arrange
        WaterSimulationSettings settings = CreateSettings();
        settings.northBoundary.seepageDepthPerSecond = 0.2f;
        WaterSourceSpec source = new WaterSourceSpec { continuousDepthPerSecond = 2f };
        WaterStormProfile profile = CreateProfile(settings, new[] { source }, "baseline");

        // Act
        WaterSimulationSettings settingsClone = profile.CreateSettingsInstance();
        WaterSourceSpec[] sourceClones = profile.CreateContinuousSourceInstances();

        // Assert
        settingsClone.northBoundary.seepageDepthPerSecond = 0.8f;
        sourceClones[0].continuousDepthPerSecond = 9f;
        WaterAssert.Multiple(() =>
        {
            Assert.That(profile.ProfileName, Is.EqualTo("baseline"));
            Assert.That(settingsClone, Is.Not.SameAs(settings));
            Assert.That(sourceClones[0], Is.Not.SameAs(source));
            Assert.That(settings.northBoundary.seepageDepthPerSecond, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(source.continuousDepthPerSecond, Is.EqualTo(2f).Within(Tolerance));
        });
    }

    [Test]
    public void ScenarioDefinition_ProfileAndInitialSources_ReturnIndependentClones()
    {
        // Arrange
        WaterSimulationSettings settings = CreateSettings();
        WaterSourceSpec continuous = new WaterSourceSpec { continuousDepthPerSecond = 1f };
        WaterSourceSpec initial = new WaterSourceSpec { initialDepth = 2f };
        ScenarioDef scenario = CreateScenario(
            baseline: CreateProfile(settings, new[] { continuous }),
            initialSources: new[] { initial });

        // Act
        bool created = scenario.TryCreateProfile(
            WaterProfileStage.Baseline,
            out WaterSimulationSettings settingsClone,
            out WaterSourceSpec[] continuousClones,
            out string error);
        WaterSourceSpec[] initialClones = scenario.CreateInitialSourceInstances();

        // Assert
        settingsClone.dt = 9f;
        continuousClones[0].continuousDepthPerSecond = 9f;
        initialClones[0].initialDepth = 9f;
        WaterAssert.Multiple(() =>
        {
            Assert.That(created, Is.True, error);
            Assert.That(settings.dt, Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(continuous.continuousDepthPerSecond, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(initial.initialDepth, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(scenario.IsValidForProduction(out _), Is.True);
        });
    }

    [TestCase(0, 1f)]
    [TestCase(1, 0f)]
    [TestCase(1, float.NaN)]
    public void PreliminaryFlooding_InvalidThresholdOrDuration_RejectsConfiguration(int threshold, float duration)
    {
        // Arrange
        WaterPreliminaryFloodingConfig configuration = CreatePreliminaryFlooding(threshold, duration);

        // Act
        bool isValid = configuration.IsValid(out string error);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("positive"));
        });
    }
}
