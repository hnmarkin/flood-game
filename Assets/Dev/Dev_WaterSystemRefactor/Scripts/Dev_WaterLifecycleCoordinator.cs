using System;
using UnityEngine;

/// <summary>
/// Game State seam for water lifecycle events. Game State owns when completed preparation
/// effects are committed, then calls this coordinator at the turn boundary.
/// </summary>
public sealed class Dev_WaterLifecycleCoordinator : MonoBehaviour
{
    [SerializeField] private Dev_WaterController waterController;

    private bool _hasAppliedPreliminaryFlooding;
    private bool _preliminaryFloodingInProgress;

    public event Action OnPreliminaryFloodingApplied;

    public bool HasAppliedPreliminaryFlooding => _hasAppliedPreliminaryFlooding;

    public bool NotifyNewRun()
    {
        _hasAppliedPreliminaryFlooding = false;
        return waterController != null && waterController.ResetSimulation();
    }

    public bool NotifyLoadingCompleted()
    {
        return waterController != null && waterController.InitializeRuntimeState();
    }

    public void NotifyGameFlowChanged(Dev_WaterGameFlow gameFlow)
    {
        waterController?.SetGameFlow(gameFlow);
    }

    public bool NotifyGamePhaseChanged(Dev_WaterGamePhase gamePhase)
    {
        return waterController != null && waterController.SetGamePhase(gamePhase);
    }

    /// <summary>
    /// Must be called only after Game State has committed completed action, terrain, modifier,
    /// and explicit barrier changes. This method does not advance turn or construction state.
    /// </summary>
    public bool NotifyCompletedPreparationTurn(int completedPreparationTurns)
    {
        if (waterController == null || _hasAppliedPreliminaryFlooding ||
            _preliminaryFloodingInProgress ||
            !waterController.TryGetPreliminaryFlooding(out Dev_WaterPreliminaryFloodingConfig flooding))
            return false;

        if (completedPreparationTurns < flooding.CompletedPreparationTurnThreshold)
            return false;

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
            Debug.LogError("[Dev_WaterLifecycleCoordinator] Preliminary flooding failed; the lifecycle marker was not committed.");
            return false;
        }

        _hasAppliedPreliminaryFlooding = true;
        OnPreliminaryFloodingApplied?.Invoke();
        return true;
    }
}
