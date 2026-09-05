using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category("WaterLifecycle")]
public sealed class Dev_WaterLifecyclePlayModeTests : Dev_WaterPlayModeFixture
{
    // Tests: configuration and initialization

    [UnityTest]
    public IEnumerator Initialization_ProductionWithoutScenario_RejectsConfiguration()
    {
        // Arrange
        LogAssert.Expect(
            LogType.Error,
            "[Dev_WaterController] Production configuration rejected scenario data: scenario definition is missing");
        Dev_WaterControllerFixture fixture = CreateControllerFixture(
            Dev_WaterConfigurationMode.Production,
            includeScenario: false,
            initialize: false);

        // Act
        bool initialized = fixture.Controller.InitializeRuntimeState();

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(initialized, Is.False);
            Assert.That(fixture.Controller.IsInitialized, Is.False);
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator Initialization_DevelopmentWithoutScenario_UsesDefaultsWithWarning()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture(
            includeScenario: false,
            initialize: false);

        // Act
        bool initialized = fixture.Controller.InitializeRuntimeState();

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(initialized, Is.True);
            Assert.That(fixture.Controller.IsInitialized, Is.True);
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Baseline));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator Initialization_RepeatedCalls_RebuildsStateAndRaisesOneEventPerCall()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture(initialize: false);
        int initializedEvents = 0;
        Action<Dev_WaterController> handler = _ => initializedEvents++;
        fixture.Controller.OnWaterInitialized += handler;

        // Act
        bool first = fixture.Controller.InitializeRuntimeState();
        fixture.Controller.TrySetWaterDepth(Vector2Int.zero, 5f);
        bool second = fixture.Controller.InitializeRuntimeState();

        // Assert
        fixture.Controller.OnWaterInitialized -= handler;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            Assert.That(initializedEvents, Is.EqualTo(2));
            Assert.That(fixture.Controller.GetWaterDepth(Vector2Int.zero), Is.Zero.Within(Tolerance));
        });
        yield return null;
    }

    // Tests: start, pause, resume, reset, and lifecycle gating

    [UnityTest]
    public IEnumerator Simulation_BeginPauseResumeRepeatedCalls_EmitsEventsOnlyOnTransitions()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture();
        int startedEvents = 0;
        int pausedEvents = 0;
        Action<Dev_WaterController> started = _ => startedEvents++;
        Action<Dev_WaterController> paused = _ => pausedEvents++;
        fixture.Controller.OnWaterSimulationStarted += started;
        fixture.Controller.OnWaterSimulationPaused += paused;

        // Act
        bool began = fixture.Controller.BeginSimulation();
        bool repeatedBegin = fixture.Controller.BeginSimulation();
        fixture.Controller.PauseSimulation();
        fixture.Controller.PauseSimulation();
        bool resumed = fixture.Controller.ResumeSimulation();

        // Assert
        fixture.Controller.OnWaterSimulationStarted -= started;
        fixture.Controller.OnWaterSimulationPaused -= paused;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(began, Is.True);
            Assert.That(repeatedBegin, Is.False);
            Assert.That(resumed, Is.True);
            Assert.That(fixture.Controller.IsSimulationRunning, Is.True);
            Assert.That(startedEvents, Is.EqualTo(2));
            Assert.That(pausedEvents, Is.EqualTo(1));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator Simulation_Reset_RebuildsBaselineStateAndRaisesResetOnce()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture();
        fixture.Controller.TrySetWaterDepth(Vector2Int.zero, 5f);
        int resetEvents = 0;
        Action<Dev_WaterController> handler = _ => resetEvents++;
        fixture.Controller.OnWaterSimulationReset += handler;

        // Act
        bool reset = fixture.Controller.ResetSimulation();

        // Assert
        fixture.Controller.OnWaterSimulationReset -= handler;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(reset, Is.True);
            Assert.That(fixture.Controller.GetWaterDepth(Vector2Int.zero), Is.Zero.Within(Tolerance));
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Baseline));
            Assert.That(fixture.Controller.IsSimulationRunning, Is.False);
            Assert.That(resetEvents, Is.EqualTo(1));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator Lifecycle_GameplayCrisisAndPause_ControlsProductionSteppingAndProfileEvents()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture(Dev_WaterConfigurationMode.Production);
        int profileEvents = 0;
        Action<Dev_WaterProfileStage> handler = _ => profileEvents++;
        fixture.Controller.OnWaterProfileChanged += handler;

        // Act
        fixture.Coordinator.NotifyGameFlowChanged(Dev_WaterGameFlow.Gameplay);
        bool phaseChanged = fixture.Coordinator.NotifyGamePhaseChanged(Dev_WaterGamePhase.Crisis);
        fixture.Coordinator.NotifyGameFlowChanged(Dev_WaterGameFlow.Pause);

        // Assert
        fixture.Controller.OnWaterProfileChanged -= handler;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(phaseChanged, Is.True);
            Assert.That(fixture.Controller.GamePhase, Is.EqualTo(Dev_WaterGamePhase.Crisis));
            Assert.That(fixture.Controller.GameFlow, Is.EqualTo(Dev_WaterGameFlow.Pause));
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Crisis));
            Assert.That(fixture.Controller.IsSimulationRunning, Is.False);
            Assert.That(profileEvents, Is.EqualTo(1));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator CrisisTransition_MissingProductionProfile_DoesNotCommitPhase()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture(includeCrisisProfile: false);
        ConfigureControllerMode(fixture.Controller, Dev_WaterConfigurationMode.Production);
        LogAssert.Expect(
            LogType.Error,
            "[Dev_WaterController] Production configuration rejected Crisis profile: The Crisis water profile is missing.");

        // Act
        bool changed = fixture.Coordinator.NotifyGamePhaseChanged(Dev_WaterGamePhase.Crisis);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(changed, Is.False);
            Assert.That(fixture.Controller.GamePhase, Is.EqualTo(Dev_WaterGamePhase.Preparation));
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Baseline));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator PublicSetters_InvalidValues_RejectWithoutMutatingState()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture();
        float before = fixture.Controller.GetWaterDepth(Vector2Int.zero);

        // Act
        bool waterChanged = fixture.Controller.TrySetWaterDepth(Vector2Int.zero, float.NaN);
        bool terrainChanged = fixture.Controller.TrySetTerrainElevation(Vector2Int.zero, -1f);
        bool outsideChanged = fixture.Controller.TrySetWaterDepth(new Vector2Int(99, 99), 1f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(waterChanged, Is.False);
            Assert.That(terrainChanged, Is.False);
            Assert.That(outsideChanged, Is.False);
            Assert.That(fixture.Controller.GetWaterDepth(Vector2Int.zero), Is.EqualTo(before).Within(Tolerance));
        });
        yield return null;
    }

    // Tests: preliminary flooding

    [UnityTest]
    public IEnumerator PreliminaryFlooding_ConfiguredThreshold_CommitsExactlyOnce()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture();
        int appliedEvents = 0;
        Action handler = () => appliedEvents++;
        fixture.Coordinator.OnPreliminaryFloodingApplied += handler;

        // Act
        bool beforeThreshold = fixture.Coordinator.NotifyCompletedPreparationTurn(1);
        bool atThreshold = fixture.Coordinator.NotifyCompletedPreparationTurn(2);
        bool repeated = fixture.Coordinator.NotifyCompletedPreparationTurn(3);

        // Assert
        fixture.Coordinator.OnPreliminaryFloodingApplied -= handler;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(beforeThreshold, Is.False);
            Assert.That(atThreshold, Is.True);
            Assert.That(repeated, Is.False);
            Assert.That(fixture.Coordinator.HasAppliedPreliminaryFlooding, Is.True);
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Preliminary));
            Assert.That(appliedEvents, Is.EqualTo(1));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator PreliminaryFlooding_ReentrantNotification_IsRejectedDuringTransaction()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture();
        bool reentrantResult = true;
        fixture.Provider.OnResolve = () =>
            reentrantResult = fixture.Coordinator.NotifyCompletedPreparationTurn(2);

        // Act
        bool applied = fixture.Coordinator.NotifyCompletedPreparationTurn(2);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(applied, Is.True);
            Assert.That(reentrantResult, Is.False);
            Assert.That(fixture.Coordinator.HasAppliedPreliminaryFlooding, Is.True);
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator PreliminaryFlooding_ModifierFailure_IsTransactionalAndRetryable()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture();
        ConfigureControllerMode(fixture.Controller, Dev_WaterConfigurationMode.Production);
        fixture.Provider.FailOnCall = fixture.Provider.CallCount + 2;
        float before = fixture.Controller.GetWaterDepth(Vector2Int.zero);
        LogAssert.Expect(
            LogType.Error,
            "[Dev_WaterController] Production simulation requires a resolved modifier provider: intentional test failure");

        // Act
        bool failed = fixture.Controller.RunPreliminaryFlooding(0.5f);
        float afterFailure = fixture.Controller.GetWaterDepth(Vector2Int.zero);
        Dev_WaterProfileStage stageAfterFailure = fixture.Controller.ActiveProfileStage;
        fixture.Provider.FailOnCall = 0;
        bool retried = fixture.Controller.RunPreliminaryFlooding(0.5f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(failed, Is.False);
            Assert.That(retried, Is.True);
            Assert.That(afterFailure, Is.EqualTo(before).Within(Tolerance));
            Assert.That(stageAfterFailure, Is.EqualTo(Dev_WaterProfileStage.Baseline));
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Preliminary));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator PreliminaryFlooding_NewRun_ClearsLifecycleMarkerAndRestoresBaseline()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture();
        Assert.That(fixture.Coordinator.NotifyCompletedPreparationTurn(2), Is.True);

        // Act
        bool reset = fixture.Coordinator.NotifyNewRun();

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(reset, Is.True);
            Assert.That(fixture.Coordinator.HasAppliedPreliminaryFlooding, Is.False);
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Baseline));
        });
        yield return null;
    }

    // Tests: projection

    [UnityTest]
    public IEnumerator Projection_BuildAndLaterLiveMutation_LeavesLiveAndPriorProjectionIndependent()
    {
        // Arrange
        Dev_WaterSourceSpec rainfall = new Dev_WaterSourceSpec
        {
            kind = Dev_WaterSourceKind.Rainfall,
            depth = 1f,
            scaleByExternalWaterLoad = false
        };
        Dev_WaterControllerFixture fixture = CreateControllerFixture(continuousSources: new[] { rainfall });
        fixture.Controller.TrySetWaterDepth(Vector2Int.zero, 2f);

        // Act
        bool built = fixture.Controller.TryBuildProjection(1f, out Dev_WaterProjection projection);
        float liveAfterProjection = fixture.Controller.GetWaterDepth(Vector2Int.zero);
        fixture.Controller.TrySetWaterDepth(Vector2Int.zero, 7f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(built, Is.True);
            Assert.That(liveAfterProjection, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(projection.GetWaterDepth(Vector2Int.zero), Is.EqualTo(3f).Within(Tolerance));
            Assert.That(projection.GetWaterDepth(Vector2Int.zero), Is.Not.EqualTo(7f));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProjectionController_Transaction_CoalescesMultipleInvalidationsIntoOneEvent()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture(addProjectionController: true);
        int replacedEvents = 0;
        Action<Dev_WaterProjection> handler = _ => replacedEvents++;
        fixture.ProjectionController.OnForecastReplaced += handler;

        // Act
        fixture.ProjectionController.BeginForecastChangeTransaction();
        fixture.ProjectionController.NotifyGameTimeAdvanced();
        fixture.ProjectionController.NotifyCompletedDefenseChanged();
        fixture.ProjectionController.NotifyWaterAffectingModifierChanged();
        fixture.ProjectionController.EndForecastChangeTransaction();

        // Assert
        fixture.ProjectionController.OnForecastReplaced -= handler;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(replacedEvents, Is.EqualTo(1));
            Assert.That(fixture.ProjectionController.CurrentForecast, Is.Not.Null);
            Assert.That(fixture.ProjectionController.CurrentForecast.SimulatedDuration, Is.EqualTo(1f).Within(Tolerance));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProjectionController_InvalidDuration_DoesNotReplaceForecast()
    {
        // Arrange
        Dev_WaterControllerFixture fixture = CreateControllerFixture(addProjectionController: true);
        Dev_WaterProjection before = fixture.ProjectionController.CurrentForecast;
        int replacedEvents = 0;
        Action<Dev_WaterProjection> handler = _ => replacedEvents++;
        fixture.ProjectionController.OnForecastReplaced += handler;

        // Act
        fixture.ProjectionController.SetForecastSimulatedDuration(float.NaN);

        // Assert
        fixture.ProjectionController.OnForecastReplaced -= handler;
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(replacedEvents, Is.Zero);
            Assert.That(fixture.ProjectionController.CurrentForecast, Is.SameAs(before));
        });
        yield return null;
    }
}
