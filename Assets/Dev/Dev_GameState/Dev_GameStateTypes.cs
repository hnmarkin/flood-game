using System;
using System.Collections.Generic;

namespace FloodGame.Dev.GameState
{
    public enum GameFlow
    {
        MainMenu,
        CampaignSelect,
        Loading,
        Gameplay,
        Pause
    }

    public enum GamePhase
    {
        Preparation,
        Crisis,
        Scoring
    }

    public enum ToolState
    {
        Normal,
        Placement,
        Inspection
    }

    public enum GameStateFailureCode
    {
        None,
        InvalidTransition,
        RedundantRequest,
        InvalidArgument,
        MissingScenario,
        InvalidScenario,
        InitializerFailed,
        WaterAdapterFailed,
        TeardownFailed
    }

    public sealed class GameStateResult
    {
        private GameStateResult(
            bool succeeded,
            GameStateFailureCode failureCode,
            string message,
            bool changed)
        {
            Succeeded = succeeded;
            FailureCode = failureCode;
            Message = message ?? string.Empty;
            Changed = changed;
        }

        public bool Succeeded { get; }
        public GameStateFailureCode FailureCode { get; }
        public string Message { get; }
        public bool Changed { get; }

        public static GameStateResult Success()
        {
            return Success(false);
        }

        public static GameStateResult Success(bool changed)
        {
            return new GameStateResult(true, GameStateFailureCode.None, string.Empty, changed);
        }

        public static GameStateResult Failure(GameStateFailureCode failureCode, string message)
        {
            if (failureCode == GameStateFailureCode.None)
                throw new ArgumentException("A failed result requires a failure code.", nameof(failureCode));

            return new GameStateResult(false, failureCode, message, false);
        }
    }

    public sealed class ScenarioInitializationResult
    {
        private ScenarioInitializationResult(
            bool succeeded,
            GameStateFailureCode failureCode,
            string message,
            string failedInitializer,
            IReadOnlyList<string> teardownErrors)
        {
            Succeeded = succeeded;
            FailureCode = failureCode;
            Message = message ?? string.Empty;
            FailedInitializer = failedInitializer;
            TeardownErrors = teardownErrors ?? Array.Empty<string>();
        }

        public bool Succeeded { get; }
        public GameStateFailureCode FailureCode { get; }
        public string Message { get; }
        public string FailedInitializer { get; }
        public IReadOnlyList<string> TeardownErrors { get; }

        internal static ScenarioInitializationResult Success()
        {
            return new ScenarioInitializationResult(
                true,
                GameStateFailureCode.None,
                string.Empty,
                null,
                Array.Empty<string>());
        }

        internal static ScenarioInitializationResult Failure(
            GameStateFailureCode failureCode,
            string message,
            string failedInitializer,
            IReadOnlyList<string> teardownErrors)
        {
            return new ScenarioInitializationResult(
                false,
                failureCode,
                message,
                failedInitializer,
                teardownErrors);
        }
    }

    public struct GameStateSnapshot
    {
        public GameStateSnapshot(
            GameFlow gameFlow,
            GamePhase gamePhase,
            ToolState toolState,
            string activeToolId,
            string scenarioId,
            bool crisisAwaitingAcknowledgement)
        {
            GameFlow = gameFlow;
            GamePhase = gamePhase;
            ToolState = toolState;
            ActiveToolId = activeToolId;
            ScenarioId = scenarioId;
            CrisisAwaitingAcknowledgement = crisisAwaitingAcknowledgement;
        }

        public GameFlow GameFlow { get; }
        public GamePhase GamePhase { get; }
        public ToolState ToolState { get; }
        public string ActiveToolId { get; }
        public string ScenarioId { get; }
        public bool CrisisAwaitingAcknowledgement { get; }
    }

    public sealed class GameFlowChangedEventArgs : EventArgs
    {
        public GameFlowChangedEventArgs(GameFlow previous, GameFlow current, GameStateSnapshot state)
        {
            Previous = previous;
            Current = current;
            State = state;
        }

        public GameFlow Previous { get; }
        public GameFlow Current { get; }
        public GameStateSnapshot State { get; }
    }

    public sealed class GamePhaseChangedEventArgs : EventArgs
    {
        public GamePhaseChangedEventArgs(GamePhase previous, GamePhase current, GameStateSnapshot state)
        {
            Previous = previous;
            Current = current;
            State = state;
        }

        public GamePhase Previous { get; }
        public GamePhase Current { get; }
        public GameStateSnapshot State { get; }
    }

    public sealed class ToolStateChangedEventArgs : EventArgs
    {
        public ToolStateChangedEventArgs(
            ToolState previous,
            ToolState current,
            string previousActiveToolId,
            string currentActiveToolId,
            GameStateSnapshot state)
        {
            Previous = previous;
            Current = current;
            PreviousActiveToolId = previousActiveToolId;
            CurrentActiveToolId = currentActiveToolId;
            State = state;
        }

        public ToolState Previous { get; }
        public ToolState Current { get; }
        public string PreviousActiveToolId { get; }
        public string CurrentActiveToolId { get; }
        public GameStateSnapshot State { get; }
    }

    public sealed class ScenarioInitializedEventArgs : EventArgs
    {
        public ScenarioInitializedEventArgs(string scenarioId, GameStateSnapshot state)
        {
            ScenarioId = scenarioId;
            State = state;
        }

        public string ScenarioId { get; }
        public GameStateSnapshot State { get; }
    }

    public sealed class ScenarioInitializationFailedEventArgs : EventArgs
    {
        public ScenarioInitializationFailedEventArgs(
            string scenarioId,
            ScenarioInitializationResult result,
            GameStateSnapshot state)
        {
            ScenarioId = scenarioId;
            Result = result;
            State = state;
        }

        public string ScenarioId { get; }
        public ScenarioInitializationResult Result { get; }
        public GameStateSnapshot State { get; }
    }

    public sealed class ScenarioEndingEventArgs : EventArgs
    {
        public ScenarioEndingEventArgs(string scenarioId, GameStateSnapshot state)
        {
            ScenarioId = scenarioId;
            State = state;
        }

        public string ScenarioId { get; }
        public GameStateSnapshot State { get; }
    }

    public sealed class CrisisPresentationRequestedEventArgs : EventArgs
    {
        public CrisisPresentationRequestedEventArgs(GameStateSnapshot state)
        {
            State = state;
        }

        public GameStateSnapshot State { get; }
    }

    public sealed class CrisisStartAcknowledgedEventArgs : EventArgs
    {
        public CrisisStartAcknowledgedEventArgs(GameStateSnapshot state)
        {
            State = state;
        }

        public GameStateSnapshot State { get; }
    }

    public interface IDevGameState
    {
        event Action<GameFlowChangedEventArgs> OnGameFlowChanged;
        event Action<GamePhaseChangedEventArgs> OnGamePhaseChanged;
        event Action<ToolStateChangedEventArgs> OnToolStateChanged;
        event Action<ScenarioInitializedEventArgs> OnScenarioInitialized;
        event Action<ScenarioInitializationFailedEventArgs> OnScenarioInitializationFailed;
        event Action<ScenarioEndingEventArgs> OnScenarioEnding;
        event Action<CrisisPresentationRequestedEventArgs> OnCrisisPresentationRequested;
        event Action<CrisisStartAcknowledgedEventArgs> OnCrisisStartAcknowledged;

        GameFlow GetGameFlow();
        GamePhase GetGamePhase();
        ToolState GetToolState();
        string GetActiveToolId();
        string GetScenarioId();
        bool IsCrisisAwaitingAcknowledgement();
        GameStateSnapshot GetSnapshot();

        GameStateResult TryEnterCampaignSelect();
        ScenarioInitializationResult TryBeginScenarioInitialization(IScenarioConfiguration scenario);
        GameStateResult TryPause();
        GameStateResult TryResume();
        GameStateResult TrySetToolState(ToolState toolState, string activeToolId);
        GameStateResult TryBeginCrisis();
        GameStateResult TryAcknowledgeCrisisStart();
        GameStateResult TryReportCrisisDurationElapsed();
        GameStateResult TryEndScenario();
    }

    public interface IScenarioConfiguration
    {
        string ScenarioId { get; }

        bool TryValidate(out string error);
    }

    public class Dev_ScenarioConfiguration : IScenarioConfiguration
    {
        public Dev_ScenarioConfiguration(string scenarioId)
        {
            ScenarioId = scenarioId;
        }

        public string ScenarioId { get; }

        public virtual bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(ScenarioId))
            {
                error = "Scenario ID is required.";
                return false;
            }

            error = null;
            return true;
        }
    }

    public sealed class Dev_ScenarioInitializationContext
    {
        internal Dev_ScenarioInitializationContext(IScenarioConfiguration scenario, GameStateSnapshot startingState)
        {
            Scenario = scenario;
            StartingState = startingState;
        }

        public IScenarioConfiguration Scenario { get; }
        public GameStateSnapshot StartingState { get; }
    }

    public interface IDevScenarioInitializer
    {
        string Name { get; }

        GameStateResult Initialize(Dev_ScenarioInitializationContext context);

        GameStateResult Teardown();
    }

    public interface IDevGameStateWaterAdapter : IDevScenarioInitializer
    {
        GameStateResult NotifyNewRun();

        GameStateResult NotifyGameFlowChanged(GameFlow gameFlow);

        GameStateResult NotifyGamePhaseChanged(GamePhase gamePhase);

        GameStateResult NotifyCompletedPreparationTurn(int completedPreparationTurns);

        GameStateResult NotifyCrisisTimeStarted();

        GameStateResult NotifyCrisisTimeAdvanced(float simulatedDuration);

        GameStateResult NotifyCrisisTimeStopped();
    }

    public sealed class Dev_ScenarioInitializerSet
    {
        public Dev_ScenarioInitializerSet(
            IDevScenarioInitializer modifiers = null,
            IDevScenarioInitializer resources = null,
            IDevScenarioInitializer preparationActions = null,
            IDevScenarioInitializer llm = null,
            IDevGameStateWaterAdapter water = null,
            IDevScenarioInitializer riskOverlay = null)
        {
            Modifiers = modifiers;
            Resources = resources;
            PreparationActions = preparationActions;
            Llm = llm;
            Water = water;
            RiskOverlay = riskOverlay;
        }

        public IDevScenarioInitializer Modifiers { get; }
        public IDevScenarioInitializer Resources { get; }
        public IDevScenarioInitializer PreparationActions { get; }
        public IDevScenarioInitializer Llm { get; }
        public IDevGameStateWaterAdapter Water { get; }
        public IDevScenarioInitializer RiskOverlay { get; }

        internal IEnumerable<IDevScenarioInitializer> InDocumentedOrder()
        {
            if (Modifiers != null) yield return Modifiers;
            if (Resources != null) yield return Resources;
            if (PreparationActions != null) yield return PreparationActions;
            if (Llm != null) yield return Llm;
            if (Water != null) yield return Water;
            if (RiskOverlay != null) yield return RiskOverlay;
        }
    }
}
