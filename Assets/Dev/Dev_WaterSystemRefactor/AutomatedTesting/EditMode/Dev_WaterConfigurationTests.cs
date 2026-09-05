using System;
using NUnit.Framework;
using UnityEngine;

[Category("WaterUnit")]
public sealed class Dev_WaterConfigurationTests : Dev_WaterEditModeFixture
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
        Dev_WaterSimulationSettings settings = CreateSettings();
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
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Is.Not.Empty);
        });
    }

    [Test]
    public void SimulationSettings_ZeroMaximumDepth_RemainsValidUnlimitedContract()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
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
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.northBoundary.mode = Dev_WaterBoundaryMode.Source;
        settings.northBoundary.sourceDepthPerSecond = 3f;

        // Act
        Dev_WaterSimulationSettings clone = settings.Clone();

        // Assert
        clone.northBoundary.sourceDepthPerSecond = 8f;
        Dev_WaterAssert.Multiple(() =>
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
        Dev_WaterBoundarySettings boundary = new Dev_WaterBoundarySettings
        {
            mode = Dev_WaterBoundaryMode.Source,
            sourceDepthPerSecond = 0f
        };

        // Act
        bool isValid = boundary.IsValid(out string error);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("positive"));
        });
    }

    [Test]
    public void SourceSpec_NonFiniteDepth_RejectsConfiguration()
    {
        // Arrange
        Dev_WaterSourceSpec source = new Dev_WaterSourceSpec { depth = float.PositiveInfinity };

        // Act
        bool isValid = source.IsValid(out string error);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("finite"));
        });
    }

    [Test]
    public void ModifierSnapshot_NonFiniteValues_RejectsConfiguration()
    {
        // Arrange
        Dev_WaterModifierSnapshot modifiers = Dev_WaterModifierSnapshot.Defaults();
        modifiers.RainfallRate = float.NaN;

        // Act
        bool isValid = modifiers.IsValid(out string error);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("finite"));
        });
    }

    [Test]
    public void ModifierSnapshot_InvalidValues_SanitizesToFiniteDefaults()
    {
        // Arrange
        Dev_WaterModifierSnapshot modifiers = Dev_WaterModifierSnapshot.Defaults();
        modifiers.DrainageEfficiency = -1f;
        modifiers.EventPacing = float.PositiveInfinity;
        modifiers.WindDirection = Vector2.zero;

        // Act
        modifiers.Sanitize();

        // Assert
        Dev_WaterAssert.Multiple(() =>
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
        Dev_WaterSimulationSettings settings = CreateSettings();
        settings.northBoundary.seepageDepthPerSecond = 0.2f;
        Dev_WaterSourceSpec source = new Dev_WaterSourceSpec { depth = 2f };
        Dev_WaterStormProfile profile = CreateProfile(settings, new[] { source }, "baseline");

        // Act
        Dev_WaterSimulationSettings settingsClone = profile.CreateSettingsInstance();
        Dev_WaterSourceSpec[] sourceClones = profile.CreateContinuousSourceInstances();

        // Assert
        settingsClone.northBoundary.seepageDepthPerSecond = 0.8f;
        sourceClones[0].depth = 9f;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(profile.ProfileName, Is.EqualTo("baseline"));
            Assert.That(settingsClone, Is.Not.SameAs(settings));
            Assert.That(sourceClones[0], Is.Not.SameAs(source));
            Assert.That(settings.northBoundary.seepageDepthPerSecond, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(source.depth, Is.EqualTo(2f).Within(Tolerance));
        });
    }

    [Test]
    public void ScenarioDefinition_ProfileAndInitialSources_ReturnIndependentClones()
    {
        // Arrange
        Dev_WaterSimulationSettings settings = CreateSettings();
        Dev_WaterSourceSpec continuous = new Dev_WaterSourceSpec { depth = 1f };
        Dev_WaterSourceSpec initial = new Dev_WaterSourceSpec { depth = 2f };
        Dev_ScenarioDef scenario = CreateScenario(
            baseline: CreateProfile(settings, new[] { continuous }),
            initialSources: new[] { initial });

        // Act
        bool created = scenario.TryCreateProfile(
            Dev_WaterProfileStage.Baseline,
            out Dev_WaterSimulationSettings settingsClone,
            out Dev_WaterSourceSpec[] continuousClones,
            out string error);
        Dev_WaterSourceSpec[] initialClones = scenario.CreateInitialSourceInstances();

        // Assert
        settingsClone.dt = 9f;
        continuousClones[0].depth = 9f;
        initialClones[0].depth = 9f;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(created, Is.True, error);
            Assert.That(settings.dt, Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(continuous.depth, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(initial.depth, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(scenario.IsValidForProduction(out _), Is.True);
        });
    }

    [TestCase(0, 1f)]
    [TestCase(1, 0f)]
    [TestCase(1, float.NaN)]
    public void PreliminaryFlooding_InvalidThresholdOrDuration_RejectsConfiguration(int threshold, float duration)
    {
        // Arrange
        Dev_WaterPreliminaryFloodingConfig configuration = CreatePreliminaryFlooding(threshold, duration);

        // Act
        bool isValid = configuration.IsValid(out string error);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("positive"));
        });
    }
}
