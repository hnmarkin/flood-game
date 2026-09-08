using System.Collections.Generic;
using NUnit.Framework;

namespace FloodGame.Dev.GameState.Tests
{
    [TestFixture]
    public sealed class Dev_ScenarioInitializationTests
    {
        [Test]
        public void Initialization_UsesDocumentedOrderAndPublishesOnlyAfterGameplay()
        {
            List<string> calls = new List<string>();
            Dev_ScenarioInitializerSet initializers = CreateInitializerSet(calls);
            Dev_GameStateController controller = new Dev_GameStateController(initializers);
            List<string> events = new List<string>();
            controller.OnGameFlowChanged += args => events.Add(args.Current.ToString());
            controller.OnScenarioInitialized += _ => events.Add("ScenarioInitialized");

            Assert.That(controller.TryEnterCampaignSelect().Succeeded, Is.True);
            ScenarioInitializationResult result = controller.TryBeginScenarioInitialization(
                new Dev_ScenarioConfiguration("scenario"));

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(calls, Is.EqualTo(new[]
            {
                "Modifiers.Initialize",
                "Resources.Initialize",
                "Preparation Actions.Initialize",
                "LLM.Initialize",
                "Water.Initialize",
                "Risk Overlay.Initialize"
            }));
            Assert.That(events, Is.EqualTo(new[]
            {
                GameFlow.CampaignSelect.ToString(),
                GameFlow.Loading.ToString(),
                GameFlow.Gameplay.ToString(),
                "ScenarioInitialized"
            }));
        }

        [Test]
        public void InitializationFailure_TearsDownInReverseAndReturnsToCampaignSelect()
        {
            List<string> calls = new List<string>();
            Dev_ScenarioInitializerSet initializers = CreateInitializerSet(calls, "Water");
            Dev_GameStateController controller = new Dev_GameStateController(initializers);
            int initializedEvents = 0;
            ScenarioInitializationFailedEventArgs failureEvent = null;
            controller.OnScenarioInitialized += _ => initializedEvents++;
            controller.OnScenarioInitializationFailed += args => failureEvent = args;

            Assert.That(controller.TryEnterCampaignSelect().Succeeded, Is.True);
            ScenarioInitializationResult result = controller.TryBeginScenarioInitialization(
                new Dev_ScenarioConfiguration("scenario"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailedInitializer, Is.EqualTo("Water"));
            Assert.That(controller.GetGameFlow(), Is.EqualTo(GameFlow.CampaignSelect));
            Assert.That(initializedEvents, Is.Zero);
            Assert.That(failureEvent, Is.Not.Null);
            Assert.That(calls, Is.EqualTo(new[]
            {
                "Modifiers.Initialize",
                "Resources.Initialize",
                "Preparation Actions.Initialize",
                "LLM.Initialize",
                "Water.Initialize",
                "Water.Teardown",
                "LLM.Teardown",
                "Preparation Actions.Teardown",
                "Resources.Teardown",
                "Modifiers.Teardown"
            }));
        }

        [Test]
        public void InvalidScenario_IsRejectedWithoutRunningInitializers()
        {
            List<string> calls = new List<string>();
            Dev_GameStateController controller = new Dev_GameStateController(
                CreateInitializerSet(calls));
            Assert.That(controller.TryEnterCampaignSelect().Succeeded, Is.True);

            ScenarioInitializationResult result = controller.TryBeginScenarioInitialization(
                new InvalidScenarioConfiguration());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailedInitializer, Is.Null);
            Assert.That(calls, Is.Empty);
            Assert.That(controller.GetGameFlow(), Is.EqualTo(GameFlow.CampaignSelect));
        }

        private static Dev_ScenarioInitializerSet CreateInitializerSet(
            List<string> calls,
            string failureName = null)
        {
            return new Dev_ScenarioInitializerSet(
                new FakeInitializer("Modifiers", calls, failureName),
                new FakeInitializer("Resources", calls, failureName),
                new FakeInitializer("Preparation Actions", calls, failureName),
                new FakeInitializer("LLM", calls, failureName),
                new FakeWaterInitializer("Water", calls, failureName),
                new FakeInitializer("Risk Overlay", calls, failureName));
        }

        private sealed class InvalidScenarioConfiguration : IScenarioConfiguration
        {
            public string ScenarioId => "invalid";

            public bool TryValidate(out string error)
            {
                error = "invalid scenario";
                return false;
            }
        }

        private class FakeInitializer : IDevScenarioInitializer
        {
            private readonly List<string> _calls;
            private readonly string _failureName;

            public FakeInitializer(string name, List<string> calls, string failureName)
            {
                Name = name;
                _calls = calls;
                _failureName = failureName;
            }

            public string Name { get; }

            public virtual GameStateResult Initialize(Dev_ScenarioInitializationContext context)
            {
                _calls.Add(Name + ".Initialize");
                return Name == _failureName
                    ? GameStateResult.Failure(GameStateFailureCode.InitializerFailed, "intentional failure")
                    : GameStateResult.Success();
            }

            public virtual GameStateResult Teardown()
            {
                _calls.Add(Name + ".Teardown");
                return GameStateResult.Success();
            }
        }

        private sealed class FakeWaterInitializer : FakeInitializer, IDevGameStateWaterAdapter
        {
            public FakeWaterInitializer(string name, List<string> calls, string failureName)
                : base(name, calls, failureName)
            {
            }

            public GameStateResult NotifyNewRun() => GameStateResult.Success();
            public GameStateResult NotifyGameFlowChanged(GameFlow gameFlow) => GameStateResult.Success();
            public GameStateResult NotifyGamePhaseChanged(GamePhase gamePhase) => GameStateResult.Success();
            public GameStateResult NotifyCompletedPreparationTurn(int completedPreparationTurns) => GameStateResult.Success();
            public GameStateResult NotifyCrisisTimeStarted() => GameStateResult.Success();
            public GameStateResult NotifyCrisisTimeAdvanced(float simulatedDuration) => GameStateResult.Success();
            public GameStateResult NotifyCrisisTimeStopped() => GameStateResult.Success();
        }
    }
}
