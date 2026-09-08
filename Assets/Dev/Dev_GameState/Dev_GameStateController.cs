using System;
using System.Collections.Generic;

namespace FloodGame.Dev.GameState
{
    /// <summary>
    /// Authoritative, scene-independent Game State implementation for the Dev lifecycle foundation.
    /// The owner of this object is responsible for keeping the instance alive across scene changes.
    /// </summary>
    public sealed class Dev_GameStateController : IDevGameState
    {
        private readonly Dev_ScenarioInitializerSet _initializers;
        private readonly List<IDevScenarioInitializer> _initializedScenarioSystems =
            new List<IDevScenarioInitializer>();

        private GameFlow _gameFlow = GameFlow.MainMenu;
        private GamePhase _gamePhase = GamePhase.Preparation;
        private ToolState _toolState = ToolState.Normal;
        private string _activeToolId;
        private string _scenarioId;
        private bool _scenarioActive;
        private bool _crisisAwaitingAcknowledgement;

        public Dev_GameStateController(Dev_ScenarioInitializerSet initializers = null)
        {
            _initializers = initializers ?? new Dev_ScenarioInitializerSet();
        }

        public event Action<GameFlowChangedEventArgs> OnGameFlowChanged;
        public event Action<GamePhaseChangedEventArgs> OnGamePhaseChanged;
        public event Action<ToolStateChangedEventArgs> OnToolStateChanged;
        public event Action<ScenarioInitializedEventArgs> OnScenarioInitialized;
        public event Action<ScenarioInitializationFailedEventArgs> OnScenarioInitializationFailed;
        public event Action<ScenarioEndingEventArgs> OnScenarioEnding;
        public event Action<CrisisPresentationRequestedEventArgs> OnCrisisPresentationRequested;
        public event Action<CrisisStartAcknowledgedEventArgs> OnCrisisStartAcknowledged;

        public GameFlow GetGameFlow()
        {
            return _gameFlow;
        }

        public GamePhase GetGamePhase()
        {
            return _gamePhase;
        }

        public ToolState GetToolState()
        {
            return _toolState;
        }

        public string GetActiveToolId()
        {
            return _activeToolId;
        }

        public string GetScenarioId()
        {
            return _scenarioId;
        }

        public bool IsCrisisAwaitingAcknowledgement()
        {
            return _crisisAwaitingAcknowledgement;
        }

        public GameStateSnapshot GetSnapshot()
        {
            return new GameStateSnapshot(
                _gameFlow,
                _gamePhase,
                _toolState,
                _activeToolId,
                _scenarioId,
                _crisisAwaitingAcknowledgement);
        }

        public GameStateResult TryEnterCampaignSelect()
        {
            if (_gameFlow != GameFlow.MainMenu)
                return FailureForCurrentFlow(GameFlow.CampaignSelect);

            return TryChangeFlow(GameFlow.CampaignSelect);
        }

        public ScenarioInitializationResult TryBeginScenarioInitialization(
            IScenarioConfiguration scenario)
        {
            if (_gameFlow != GameFlow.CampaignSelect)
            {
                return ScenarioInitializationResult.Failure(
                    GameStateFailureCode.InvalidTransition,
                    "Scenario initialization requires Campaign Select.",
                    null,
                    Array.Empty<string>());
            }

            if (scenario == null)
            {
                return ScenarioInitializationResult.Failure(
                    GameStateFailureCode.MissingScenario,
                    "Scenario configuration is required.",
                    null,
                    Array.Empty<string>());
            }

            GameStateResult loadingResult = TryChangeFlow(GameFlow.Loading);
            if (!loadingResult.Succeeded)
            {
                return FailedInitialization(
                    scenario.ScenarioId,
                    loadingResult.FailureCode,
                    loadingResult.Message,
                    null,
                    new List<IDevScenarioInitializer>());
            }

            ResetTransientScenarioState();

            if (!scenario.TryValidate(out string scenarioError))
            {
                return FailedInitialization(
                    scenario.ScenarioId,
                    GameStateFailureCode.InvalidScenario,
                    scenarioError ?? "Scenario configuration is invalid.",
                    null,
                    new List<IDevScenarioInitializer>());
            }

            _scenarioId = scenario.ScenarioId;
            List<IDevScenarioInitializer> attemptedInitializers =
                new List<IDevScenarioInitializer>();

            if (_initializers.Water != null)
            {
                GameStateResult newRunResult = _initializers.Water.NotifyNewRun();
                if (!newRunResult.Succeeded)
                {
                    attemptedInitializers.Add(_initializers.Water);
                    return FailedInitialization(
                        scenario.ScenarioId,
                        GameStateFailureCode.WaterAdapterFailed,
                        "Water new-run reset failed: " + newRunResult.Message,
                        _initializers.Water.Name,
                        attemptedInitializers);
                }
            }

            Dev_ScenarioInitializationContext context =
                new Dev_ScenarioInitializationContext(scenario, GetSnapshot());

            foreach (IDevScenarioInitializer initializer in _initializers.InDocumentedOrder())
            {
                attemptedInitializers.Add(initializer);
                GameStateResult initializerResult = initializer.Initialize(context);
                if (initializerResult.Succeeded)
                    continue;

                return FailedInitialization(
                    scenario.ScenarioId,
                    GameStateFailureCode.InitializerFailed,
                    initializer.Name + " initialization failed: " + initializerResult.Message,
                    initializer.Name,
                    attemptedInitializers);
            }

            _initializedScenarioSystems.Clear();
            _initializedScenarioSystems.AddRange(attemptedInitializers);
            _scenarioActive = true;

            GameStateResult gameplayResult = TryChangeFlow(GameFlow.Gameplay);
            if (!gameplayResult.Succeeded)
            {
                return FailedInitialization(
                    scenario.ScenarioId,
                    gameplayResult.FailureCode,
                    gameplayResult.Message,
                    null,
                    attemptedInitializers);
            }

            OnScenarioInitialized?.Invoke(new ScenarioInitializedEventArgs(
                scenario.ScenarioId,
                GetSnapshot()));
            return ScenarioInitializationResult.Success();
        }

        public GameStateResult TryPause()
        {
            if (_gameFlow != GameFlow.Gameplay)
                return FailureForCurrentFlow(GameFlow.Pause);

            return TryChangeFlow(GameFlow.Pause);
        }

        public GameStateResult TryResume()
        {
            if (_gameFlow != GameFlow.Pause)
                return FailureForCurrentFlow(GameFlow.Gameplay);

            return TryChangeFlow(GameFlow.Gameplay);
        }

        public GameStateResult TrySetToolState(ToolState toolState, string activeToolId)
        {
            if (!Enum.IsDefined(typeof(ToolState), toolState))
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.InvalidArgument,
                    "Tool State is not defined.");
            }

            if (_gameFlow != GameFlow.Gameplay)
                return FailureForCurrentFlow(GameFlow.Gameplay);

            if (toolState == ToolState.Normal)
            {
                activeToolId = null;
            }
            else if (string.IsNullOrWhiteSpace(activeToolId))
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.InvalidArgument,
                    "A non-Normal Tool State requires an active tool ID.");
            }

            if (_toolState == toolState && _activeToolId == activeToolId)
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.RedundantRequest,
                    "The requested Tool State is already active.");
            }

            PublishToolState(toolState, activeToolId);
            return GameStateResult.Success(true);
        }

        public GameStateResult TryBeginCrisis()
        {
            if (_gameFlow != GameFlow.Gameplay)
                return FailureForCurrentFlow(GameFlow.Gameplay);

            if (_gamePhase != GamePhase.Preparation)
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.InvalidTransition,
                    "Crisis can begin only from Preparation.");
            }

            if (_initializers.Water != null)
            {
                GameStateResult waterResult =
                    _initializers.Water.NotifyGamePhaseChanged(GamePhase.Crisis);
                if (!waterResult.Succeeded)
                {
                    return GameStateResult.Failure(
                        GameStateFailureCode.WaterAdapterFailed,
                        "Water Crisis transition failed: " + waterResult.Message);
                }
            }

            PublishToolState(ToolState.Normal, null);
            GamePhase previous = _gamePhase;
            _gamePhase = GamePhase.Crisis;
            _crisisAwaitingAcknowledgement = true;
            OnGamePhaseChanged?.Invoke(new GamePhaseChangedEventArgs(
                previous,
                _gamePhase,
                GetSnapshot()));
            OnCrisisPresentationRequested?.Invoke(new CrisisPresentationRequestedEventArgs(
                GetSnapshot()));
            return GameStateResult.Success(true);
        }

        public GameStateResult TryAcknowledgeCrisisStart()
        {
            if (_gameFlow != GameFlow.Gameplay || _gamePhase != GamePhase.Crisis)
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.InvalidTransition,
                    "Crisis acknowledgement requires active Gameplay and Crisis.");
            }

            if (!_crisisAwaitingAcknowledgement)
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.RedundantRequest,
                    "Crisis start has already been acknowledged.");
            }

            if (_initializers.Water != null)
            {
                GameStateResult waterResult = _initializers.Water.NotifyCrisisTimeStarted();
                if (!waterResult.Succeeded)
                {
                    return GameStateResult.Failure(
                        GameStateFailureCode.WaterAdapterFailed,
                        "Water Crisis start failed: " + waterResult.Message);
                }
            }

            _crisisAwaitingAcknowledgement = false;
            OnCrisisStartAcknowledged?.Invoke(new CrisisStartAcknowledgedEventArgs(
                GetSnapshot()));
            return GameStateResult.Success(true);
        }

        public GameStateResult TryReportCrisisDurationElapsed()
        {
            if (_gameFlow != GameFlow.Gameplay || _gamePhase != GamePhase.Crisis)
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.InvalidTransition,
                    "Crisis duration can expire only during active Crisis Gameplay.");
            }

            if (_initializers.Water != null)
            {
                GameStateResult waterStopResult = _initializers.Water.NotifyCrisisTimeStopped();
                if (!waterStopResult.Succeeded)
                {
                    return GameStateResult.Failure(
                        GameStateFailureCode.WaterAdapterFailed,
                        "Water Crisis stop failed: " + waterStopResult.Message);
                }

                GameStateResult waterPhaseResult =
                    _initializers.Water.NotifyGamePhaseChanged(GamePhase.Scoring);
                if (!waterPhaseResult.Succeeded)
                {
                    return GameStateResult.Failure(
                        GameStateFailureCode.WaterAdapterFailed,
                        "Water Scoring transition failed: " + waterPhaseResult.Message);
                }
            }

            PublishToolState(ToolState.Normal, null);
            GamePhase previous = _gamePhase;
            _gamePhase = GamePhase.Scoring;
            _crisisAwaitingAcknowledgement = false;
            OnGamePhaseChanged?.Invoke(new GamePhaseChangedEventArgs(
                previous,
                _gamePhase,
                GetSnapshot()));
            return GameStateResult.Success(true);
        }

        public GameStateResult TryEndScenario()
        {
            if (!_scenarioActive ||
                (_gameFlow != GameFlow.Gameplay && _gameFlow != GameFlow.Pause))
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.InvalidTransition,
                    "Scenario ending requires an active Gameplay or Pause state.");
            }

            OnScenarioEnding?.Invoke(new ScenarioEndingEventArgs(_scenarioId, GetSnapshot()));

            List<string> teardownErrors = TeardownInitializers(_initializedScenarioSystems);
            if (teardownErrors.Count > 0)
            {
                return GameStateResult.Failure(
                    GameStateFailureCode.TeardownFailed,
                    string.Join(" ", teardownErrors));
            }

            if (_initializers.Water != null)
            {
                GameStateResult waterResult =
                    _initializers.Water.NotifyGameFlowChanged(GameFlow.MainMenu);
                if (!waterResult.Succeeded)
                {
                    return GameStateResult.Failure(
                        GameStateFailureCode.WaterAdapterFailed,
                        "Water Main Menu transition failed: " + waterResult.Message);
                }
            }

            PublishToolState(ToolState.Normal, null);
            PublishPhase(GamePhase.Preparation);
            _scenarioActive = false;
            _scenarioId = null;
            _crisisAwaitingAcknowledgement = false;
            _initializedScenarioSystems.Clear();

            GameFlow previous = _gameFlow;
            _gameFlow = GameFlow.MainMenu;
            OnGameFlowChanged?.Invoke(new GameFlowChangedEventArgs(
                previous,
                _gameFlow,
                GetSnapshot()));
            return GameStateResult.Success(true);
        }

        private GameStateResult TryChangeFlow(GameFlow next)
        {
            if (!IsLegalFlowTransition(_gameFlow, next))
            {
                return GameStateResult.Failure(
                    _gameFlow == next
                        ? GameStateFailureCode.RedundantRequest
                        : GameStateFailureCode.InvalidTransition,
                    _gameFlow == next
                        ? "The requested Game Flow is already active."
                        : string.Format(
                            "The Game Flow transition {0} -> {1} is not legal.",
                            _gameFlow,
                            next));
            }

            if (_initializers.Water != null)
            {
                GameStateResult waterResult = _initializers.Water.NotifyGameFlowChanged(next);
                if (!waterResult.Succeeded)
                {
                    return GameStateResult.Failure(
                        GameStateFailureCode.WaterAdapterFailed,
                        "Water Game Flow transition failed: " + waterResult.Message);
                }
            }

            if (next == GameFlow.Pause)
                PublishToolState(ToolState.Normal, null);

            GameFlow previous = _gameFlow;
            _gameFlow = next;
            OnGameFlowChanged?.Invoke(new GameFlowChangedEventArgs(
                previous,
                _gameFlow,
                GetSnapshot()));
            return GameStateResult.Success(true);
        }

        private ScenarioInitializationResult FailedInitialization(
            string scenarioId,
            GameStateFailureCode failureCode,
            string message,
            string failedInitializer,
            List<IDevScenarioInitializer> attemptedInitializers)
        {
            List<string> teardownErrors = TeardownInitializers(attemptedInitializers);
            _scenarioActive = false;
            _scenarioId = null;
            _initializedScenarioSystems.Clear();
            _crisisAwaitingAcknowledgement = false;
            PublishToolState(ToolState.Normal, null);
            PublishPhase(GamePhase.Preparation);

            if (_gameFlow == GameFlow.Loading)
            {
                GameStateResult campaignResult = TryChangeFlow(GameFlow.CampaignSelect);
                if (!campaignResult.Succeeded)
                {
                    teardownErrors.Add("Could not return to Campaign Select: " + campaignResult.Message);
                }
            }

            ScenarioInitializationResult result = ScenarioInitializationResult.Failure(
                failureCode,
                message,
                failedInitializer,
                teardownErrors);
            OnScenarioInitializationFailed?.Invoke(new ScenarioInitializationFailedEventArgs(
                scenarioId,
                result,
                GetSnapshot()));
            return result;
        }

        private List<string> TeardownInitializers(
            IList<IDevScenarioInitializer> initializers)
        {
            List<string> errors = new List<string>();
            for (int i = initializers.Count - 1; i >= 0; i--)
            {
                IDevScenarioInitializer initializer = initializers[i];
                GameStateResult result = initializer.Teardown();
                if (!result.Succeeded)
                {
                    errors.Add(initializer.Name + " teardown failed: " + result.Message);
                }
            }

            return errors;
        }

        private void ResetTransientScenarioState()
        {
            PublishToolState(ToolState.Normal, null);
            PublishPhase(GamePhase.Preparation);
            _crisisAwaitingAcknowledgement = false;
            _scenarioActive = false;
            _scenarioId = null;
        }

        private void PublishToolState(ToolState next, string nextActiveToolId)
        {
            if (_toolState == next && _activeToolId == nextActiveToolId)
                return;

            ToolState previous = _toolState;
            string previousActiveToolId = _activeToolId;
            _toolState = next;
            _activeToolId = nextActiveToolId;
            OnToolStateChanged?.Invoke(new ToolStateChangedEventArgs(
                previous,
                _toolState,
                previousActiveToolId,
                _activeToolId,
                GetSnapshot()));
        }

        private void PublishPhase(GamePhase next)
        {
            if (_gamePhase == next)
                return;

            GamePhase previous = _gamePhase;
            _gamePhase = next;
            OnGamePhaseChanged?.Invoke(new GamePhaseChangedEventArgs(
                previous,
                _gamePhase,
                GetSnapshot()));
        }

        private GameStateResult FailureForCurrentFlow(GameFlow requestedFlow)
        {
            return GameStateResult.Failure(
                _gameFlow == requestedFlow
                    ? GameStateFailureCode.RedundantRequest
                    : GameStateFailureCode.InvalidTransition,
                _gameFlow == requestedFlow
                    ? "The requested Game Flow is already active."
                    : string.Format(
                        "The operation requires {0}, but the current Game Flow is {1}.",
                        requestedFlow,
                        _gameFlow));
        }

        private static bool IsLegalFlowTransition(GameFlow current, GameFlow next)
        {
            if (current == next)
                return false;

            switch (current)
            {
                case GameFlow.MainMenu:
                    return next == GameFlow.CampaignSelect;
                case GameFlow.CampaignSelect:
                    return next == GameFlow.Loading;
                case GameFlow.Loading:
                    return next == GameFlow.Gameplay || next == GameFlow.CampaignSelect;
                case GameFlow.Gameplay:
                    return next == GameFlow.Pause;
                case GameFlow.Pause:
                    return next == GameFlow.Gameplay;
                default:
                    return false;
            }
        }
    }
}
