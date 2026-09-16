using System;
using UnityEngine;

/// <summary>
/// Game State seam for water lifecycle events. Game State owns when completed preparation
/// effects are committed, then calls this coordinator at the turn boundary.
/// </summary>
public sealed class WaterLifecycleCoordinator : MonoBehaviour
{
    [SerializeField] private WaterController waterController;

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

    public void NotifyGameFlowChanged(WaterGameFlow gameFlow)
    {
        waterController?.SetGameFlow(gameFlow);
    }

    public bool NotifyGamePhaseChanged(WaterGamePhase gamePhase)
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
            !waterController.TryGetPreliminaryFlooding(out WaterPreliminaryFloodingConfig flooding))
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
            Debug.LogError("[WaterLifecycleCoordinator] Preliminary flooding failed; the lifecycle marker was not committed.");
            return false;
        }

        _hasAppliedPreliminaryFlooding = true;
        OnPreliminaryFloodingApplied?.Invoke();
        return true;
    }
}
