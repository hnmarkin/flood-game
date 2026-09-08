using System;
using UnityEngine;
using FloodGame.Dev.GameState;

/// <summary>
/// Game State seam for water lifecycle events. Game State owns when completed preparation
/// effects are committed, then calls this coordinator at the turn boundary.
/// </summary>
public sealed class WaterLifecycleCoordinator : MonoBehaviour, IDevGameStateWaterAdapter
{
    [SerializeField] private WaterController waterController;

    private bool _hasAppliedPreliminaryFlooding;
    private bool _preliminaryFloodingInProgress;

    public event Action OnPreliminaryFloodingApplied;

    public bool HasAppliedPreliminaryFlooding => _hasAppliedPreliminaryFlooding;

    public string Name => "Water";

    public GameStateResult Initialize(Dev_ScenarioInitializationContext context)
    {
        return NotifyLoadingCompleted();
    }

    public GameStateResult Teardown()
    {
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        return waterController.TeardownRuntimeState()
            ? GameStateResult.Success(true)
            : GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller could not release runtime state.");
    }

    public GameStateResult NotifyNewRun()
    {
        _hasAppliedPreliminaryFlooding = false;
        _preliminaryFloodingInProgress = false;
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        waterController.SetGamePhase(GamePhase.Preparation);
        return waterController.ResetSimulation()
            ? GameStateResult.Success(true)
            : GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller could not reset the new run.");
    }

    public GameStateResult NotifyLoadingCompleted()
    {
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        return waterController.InitializeRuntimeState()
            ? GameStateResult.Success(true)
            : GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller could not initialize runtime state.");
    }

    public GameStateResult NotifyGameFlowChanged(GameFlow gameFlow)
    {
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        waterController.SetGameFlow(gameFlow);
        if (gameFlow != GameFlow.Gameplay)
            waterController.PauseSimulation();

        return GameStateResult.Success(true);
    }

    public GameStateResult NotifyGamePhaseChanged(GamePhase gamePhase)
    {
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        if (!waterController.SetGamePhase(gamePhase))
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller rejected the Game Phase transition.");
        }

        if (gamePhase != GamePhase.Crisis)
            waterController.PauseSimulation();

        return GameStateResult.Success(true);
    }

    /// <summary>
    /// Must be called only after Game State has committed completed action, terrain, modifier,
    /// and explicit barrier changes. This method does not advance turn or construction state.
    /// </summary>
    public GameStateResult NotifyCompletedPreparationTurn(int completedPreparationTurns)
    {
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        if (_hasAppliedPreliminaryFlooding || _preliminaryFloodingInProgress ||
            !waterController.TryGetPreliminaryFlooding(out WaterPreliminaryFloodingConfig flooding))
            return GameStateResult.Success();

        if (completedPreparationTurns < flooding.CompletedPreparationTurnThreshold)
            return GameStateResult.Success();

        _preliminaryFloodingInProgress = true;
        bool applied;
        try
        {
            applied = waterController.RunPreliminaryFlooding(flooding.SimulatedDuration);
        }
        finally
        {
            _preliminaryFloodingInProgress = false;
        }

        if (!applied)
        {
            Debug.LogError("[WaterLifecycleCoordinator] Preliminary flooding failed; the lifecycle marker was not committed.");
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Preliminary flooding failed and remains retryable.");
        }

        _hasAppliedPreliminaryFlooding = true;
        OnPreliminaryFloodingApplied?.Invoke();
        return GameStateResult.Success(true);
    }

    public GameStateResult NotifyCrisisTimeStarted()
    {
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        return waterController.BeginSimulation()
            ? GameStateResult.Success(true)
            : GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller could not start Crisis simulation.");
    }

    public GameStateResult NotifyCrisisTimeAdvanced(float simulatedDuration)
    {
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        if (float.IsNaN(simulatedDuration) || float.IsInfinity(simulatedDuration) || simulatedDuration <= 0f)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.InvalidArgument,
                "Crisis simulation duration must be finite and positive.");
        }

        if (!waterController.IsSimulationRunning)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.InvalidTransition,
                "Crisis simulation must be started before it can advance.");
        }

        return waterController.RunSimulationForDuration(simulatedDuration)
            ? GameStateResult.Success(true)
            : GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller could not advance Crisis simulation.");
    }

    public GameStateResult NotifyCrisisTimeStopped()
    {
        if (waterController == null)
        {
            return GameStateResult.Failure(
                GameStateFailureCode.WaterAdapterFailed,
                "Water Controller is not assigned.");
        }

        bool wasRunning = waterController.IsSimulationRunning;
        waterController.PauseSimulation();
        return GameStateResult.Success(wasRunning);
    }
}
