using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category("WaterLifecycle")]
public sealed class WaterLifecyclePlayModeTests : WaterPlayModeFixture
{
    // Tests: configuration and initialization

    [UnityTest]
    public IEnumerator Initialization_ProductionWithoutScenario_RejectsConfiguration()
    {
        // Arrange
        LogAssert.Expect(
            LogType.Error,
            "[WaterController] Production configuration rejected scenario data: scenario definition is missing");
        WaterControllerFixture fixture = CreateControllerFixture(
            WaterConfigurationMode.Production,
            includeScenario: false,
            initialize: false);

        // Act
        bool initialized = fixture.Controller.InitializeRuntimeState();

        // Assert
        WaterAssert.Multiple(() =>
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
        WaterControllerFixture fixture = CreateControllerFixture(
            includeScenario: false,
            initialize: false);

        // Act
        bool initialized = fixture.Controller.InitializeRuntimeState();

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(initialized, Is.True);
            Assert.That(fixture.Controller.IsInitialized, Is.True);
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(WaterProfileStage.Baseline));
        });
        yield return null;
    }

    // TODO(Game State): Re-enable and adapt this test after ScenarioInitialized exists.
    // Initialization completion is now coordinated through explicit results rather than
    // an OnWaterInitialized acknowledgement event.
    /*
    [UnityTest]
    public IEnumerator Initialization_RepeatedCalls_RebuildsState()
    {
        // Arrange
        WaterControllerFixture fixture = CreateControllerFixture(initialize: false);
        // TODO(Game State): Adapt initialization-completion coverage after Game State integration.
        // The OnWaterInitialized event was removed; required initialization should be coordinated
        // through explicit bool/result-returning calls and ScenarioInitialized.
        // int initializedEvents = 0;
        // Action<WaterController> handler = _ => initializedEvents++;
        // fixture.Controller.OnWaterInitialized += handler;

        // Act
        bool first = fixture.Controller.InitializeRuntimeState();
        fixture.Controller.TrySetWaterDepth(Vector2Int.zero, 5f);
        bool second = fixture.Controller.InitializeRuntimeState();

        // Assert
        // fixture.Controller.OnWaterInitialized -= handler;
        WaterAssert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            // Assert.That(initializedEvents, Is.EqualTo(2));
            Assert.That(fixture.Controller.GetWaterDepth(Vector2Int.zero), Is.Zero.Within(Tolerance));
        });
        yield return null;
    }
    */

    // Tests: start, pause, resume, reset, and lifecycle gating

    // TODO(Game State): Re-enable and adapt this test after Game Flow/Phase events exist.
    // Water no longer mirrors Game State with Started/Paused events.
    /*
    [UnityTest]
    public IEnumerator Simulation_BeginPauseResumeRepeatedCalls_PreservesStateTransitions()
    {
        // Arrange
        WaterControllerFixture fixture = CreateControllerFixture();
        // TODO(Game State): Adapt lifecycle notification coverage after Game State integration.
        // Water no longer mirrors Game State with Started/Paused events.
        // int startedEvents = 0;
        // int pausedEvents = 0;
        // Action<WaterController> started = _ => startedEvents++;
        // Action<WaterController> paused = _ => pausedEvents++;
        // fixture.Controller.OnWaterSimulationStarted += started;
        // fixture.Controller.OnWaterSimulationPaused += paused;

        // Act
        bool began = fixture.Controller.BeginSimulation();
        bool repeatedBegin = fixture.Controller.BeginSimulation();
        fixture.Controller.PauseSimulation();
        fixture.Controller.PauseSimulation();
        bool resumed = fixture.Controller.ResumeSimulation();

        // Assert
        // fixture.Controller.OnWaterSimulationStarted -= started;
        // fixture.Controller.OnWaterSimulationPaused -= paused;
        WaterAssert.Multiple(() =>
        {
            Assert.That(began, Is.True);
            Assert.That(repeatedBegin, Is.False);
            Assert.That(resumed, Is.True);
            Assert.That(fixture.Controller.IsSimulationRunning, Is.True);
            // Assert.That(startedEvents, Is.EqualTo(2));
            // Assert.That(pausedEvents, Is.EqualTo(1));
        });
        yield return null;
    }
    */

    [UnityTest]
    public IEnumerator Simulation_Reset_RebuildsBaselineStateAndRaisesResetOnce()
    {
        // Arrange
        WaterControllerFixture fixture = CreateControllerFixture();
        fixture.Controller.TrySetWaterDepth(Vector2Int.zero, 5f);
        int resetEvents = 0;
        Action<WaterController> handler = _ => resetEvents++;
        fixture.Controller.OnWaterSimulationReset += handler;

        // Act
        bool reset = fixture.Controller.ResetSimulation();

        // Assert
        fixture.Controller.OnWaterSimulationReset -= handler;
        WaterAssert.Multiple(() =>
        {
            Assert.That(reset, Is.True);
            Assert.That(fixture.Controller.GetWaterDepth(Vector2Int.zero), Is.Zero.Within(Tolerance));
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(WaterProfileStage.Baseline));
            Assert.That(fixture.Controller.IsSimulationRunning, Is.False);
            Assert.That(resetEvents, Is.EqualTo(1));
        });
        yield return null;
    }

    // TODO(Game State): Re-enable and adapt this test after TimeProfileChanged exists.
    // Profile transitions are now coordinated by Game State, including explicit forecast invalidation.
    /*
    [UnityTest]
    public IEnumerator Lifecycle_GameplayCrisisAndPause_ControlsProductionSteppingAndProfile()
    {
        // Arrange
        WaterControllerFixture fixture = CreateControllerFixture(WaterConfigurationMode.Production);
        // TODO(Game State): Adapt time/profile notification coverage after Game State integration.
        // Game State will notify forecast consumers explicitly after coordinating the transition.
        // int profileEvents = 0;
        // Action<WaterProfileStage> handler = _ => profileEvents++;
        // fixture.Controller.OnWaterProfileChanged += handler;

        // Act
        fixture.Coordinator.NotifyGameFlowChanged(WaterGameFlow.Gameplay);
        bool phaseChanged = fixture.Coordinator.NotifyGamePhaseChanged(WaterGamePhase.Crisis);
        fixture.Coordinator.NotifyGameFlowChanged(WaterGameFlow.Pause);

        // Assert
        // fixture.Controller.OnWaterProfileChanged -= handler;
        WaterAssert.Multiple(() =>
        {
            Assert.That(phaseChanged, Is.True);
            Assert.That(fixture.Controller.GamePhase, Is.EqualTo(WaterGamePhase.Crisis));
            Assert.That(fixture.Controller.GameFlow, Is.EqualTo(WaterGameFlow.Pause));
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(WaterProfileStage.Crisis));
            Assert.That(fixture.Controller.IsSimulationRunning, Is.False);
            // Assert.That(profileEvents, Is.EqualTo(1));
        });
        yield return null;
    }
    */

    [UnityTest]
    public IEnumerator CrisisTransition_MissingProductionProfile_DoesNotCommitPhase()
    {
        // Arrange
        WaterControllerFixture fixture = CreateControllerFixture(includeCrisisProfile: false);
        ConfigureControllerMode(fixture.Controller, WaterConfigurationMode.Production);
        LogAssert.Expect(
            LogType.Error,
            "[WaterController] Production configuration rejected Crisis profile: The Crisis water profile is missing.");

        // Act
        bool changed = fixture.Coordinator.NotifyGamePhaseChanged(WaterGamePhase.Crisis);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(changed, Is.False);
            Assert.That(fixture.Controller.GamePhase, Is.EqualTo(WaterGamePhase.Preparation));
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(WaterProfileStage.Baseline));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator PublicSetters_InvalidValues_RejectWithoutMutatingState()
    {
        // Arrange
        WaterControllerFixture fixture = CreateControllerFixture();
        float before = fixture.Controller.GetWaterDepth(Vector2Int.zero);

        // Act
        bool waterChanged = fixture.Controller.TrySetWaterDepth(Vector2Int.zero, float.NaN);
        bool terrainChanged = fixture.Controller.TrySetTerrainElevation(Vector2Int.zero, -1f);
        bool outsideChanged = fixture.Controller.TrySetWaterDepth(new Vector2Int(99, 99), 1f);

        // Assert
        WaterAssert.Multiple(() =>
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
        WaterControllerFixture fixture = CreateControllerFixture();
        int appliedEvents = 0;
        Action handler = () => appliedEvents++;
        fixture.Coordinator.OnPreliminaryFloodingApplied += handler;

        // Act
        bool beforeThreshold = fixture.Coordinator.NotifyCompletedPreparationTurn(1);
        bool atThreshold = fixture.Coordinator.NotifyCompletedPreparationTurn(2);
        bool repeated = fixture.Coordinator.NotifyCompletedPreparationTurn(3);

        // Assert
        fixture.Coordinator.OnPreliminaryFloodingApplied -= handler;
        WaterAssert.Multiple(() =>
        {
            Assert.That(beforeThreshold, Is.False);
            Assert.That(atThreshold, Is.True);
            Assert.That(repeated, Is.False);
            Assert.That(fixture.Coordinator.HasAppliedPreliminaryFlooding, Is.True);
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(WaterProfileStage.Preliminary));
            Assert.That(appliedEvents, Is.EqualTo(1));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator PreliminaryFlooding_ReentrantNotification_IsRejectedDuringTransaction()
    {
        // Arrange
        WaterControllerFixture fixture = CreateControllerFixture();
        bool reentrantResult = true;
        fixture.Provider.OnResolve = () =>
            reentrantResult = fixture.Coordinator.NotifyCompletedPreparationTurn(2);

        // Act
        bool applied = fixture.Coordinator.NotifyCompletedPreparationTurn(2);

        // Assert
        WaterAssert.Multiple(() =>
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
        WaterControllerFixture fixture = CreateControllerFixture();
        ConfigureControllerMode(fixture.Controller, WaterConfigurationMode.Production);
        fixture.Provider.FailOnCall = fixture.Provider.CallCount + 2;
        float before = fixture.Controller.GetWaterDepth(Vector2Int.zero);
        LogAssert.Expect(
            LogType.Error,
            "[WaterController] Production simulation requires a resolved modifier provider: intentional test failure");

        // Act
        bool failed = fixture.Controller.RunPreliminaryFlooding(0.5f);
        float afterFailure = fixture.Controller.GetWaterDepth(Vector2Int.zero);
        WaterProfileStage stageAfterFailure = fixture.Controller.ActiveProfileStage;
        fixture.Provider.FailOnCall = 0;
        bool retried = fixture.Controller.RunPreliminaryFlooding(0.5f);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(failed, Is.False);
            Assert.That(retried, Is.True);
            Assert.That(afterFailure, Is.EqualTo(before).Within(Tolerance));
            Assert.That(stageAfterFailure, Is.EqualTo(WaterProfileStage.Baseline));
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(WaterProfileStage.Preliminary));
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator PreliminaryFlooding_NewRun_ClearsLifecycleMarkerAndRestoresBaseline()
    {
        // Arrange
        WaterControllerFixture fixture = CreateControllerFixture();
        Assert.That(fixture.Coordinator.NotifyCompletedPreparationTurn(2), Is.True);

        // Act
        bool reset = fixture.Coordinator.NotifyNewRun();

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(reset, Is.True);
            Assert.That(fixture.Coordinator.HasAppliedPreliminaryFlooding, Is.False);
            Assert.That(fixture.Controller.ActiveProfileStage, Is.EqualTo(WaterProfileStage.Baseline));
        });
        yield return null;
    }

    // Tests: projection

    [UnityTest]
    public IEnumerator Projection_BuildAndLaterLiveMutation_LeavesLiveAndPriorProjectionIndependent()
    {
        // Arrange
        WaterSourceSpec rainfall = new WaterSourceSpec
        {
            kind = WaterSourceKind.Rainfall,
            continuousDepthPerSecond = 1f,
            scaleByExternalWaterLoad = false
        };
        WaterControllerFixture fixture = CreateControllerFixture(continuousSources: new[] { rainfall });
        fixture.Controller.TrySetWaterDepth(Vector2Int.zero, 2f);

        // Act
        bool built = fixture.Controller.TryBuildProjection(1f, out WaterProjection projection);
        float liveAfterProjection = fixture.Controller.GetWaterDepth(Vector2Int.zero);
        fixture.Controller.TrySetWaterDepth(Vector2Int.zero, 7f);

        // Assert
        WaterAssert.Multiple(() =>
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
        WaterControllerFixture fixture = CreateControllerFixture(addProjectionController: true);
        int replacedEvents = 0;
        Action<WaterProjection> handler = _ => replacedEvents++;
        fixture.ProjectionController.OnForecastReplaced += handler;

        // Act
        fixture.ProjectionController.BeginForecastChangeTransaction();
        fixture.ProjectionController.NotifyGameTimeAdvanced();
        fixture.ProjectionController.NotifyTimeProfileChanged();
        fixture.ProjectionController.NotifyCompletedDefenseChanged();
        fixture.ProjectionController.NotifyWaterAffectingModifierChanged();
        fixture.ProjectionController.EndForecastChangeTransaction();

        // Assert
        fixture.ProjectionController.OnForecastReplaced -= handler;
        WaterAssert.Multiple(() =>
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
        WaterControllerFixture fixture = CreateControllerFixture(addProjectionController: true);
        WaterProjection before = fixture.ProjectionController.CurrentForecast;
        int replacedEvents = 0;
        Action<WaterProjection> handler = _ => replacedEvents++;
        fixture.ProjectionController.OnForecastReplaced += handler;

        // Act
        fixture.ProjectionController.SetForecastSimulatedDuration(float.NaN);

        // Assert
        fixture.ProjectionController.OnForecastReplaced -= handler;
        WaterAssert.Multiple(() =>
        {
            Assert.That(replacedEvents, Is.Zero);
            Assert.That(fixture.ProjectionController.CurrentForecast, Is.SameAs(before));
        });
        yield return null;
    }
}
