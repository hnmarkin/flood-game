using System.Collections.Generic;
using NUnit.Framework;

namespace FloodGame.Dev.GameState.Tests
{
    [TestFixture]
    public sealed class Dev_GameStateWaterHandoffTests
    {
        [Test]
        public void WaterAdapter_ReceivesLifecycleAndCrisisTimeCallsInOrder()
        {
            RecordingWaterAdapter water = new RecordingWaterAdapter();
            Dev_GameStateController controller = new Dev_GameStateController(
                new Dev_ScenarioInitializerSet(water: water));

            Assert.That(controller.TryEnterCampaignSelect().Succeeded, Is.True);
            Assert.That(
                controller.TryBeginScenarioInitialization(
                    new Dev_ScenarioConfiguration("scenario")).Succeeded,
                Is.True);
            Assert.That(controller.TryBeginCrisis().Succeeded, Is.True);
            Assert.That(water.Calls, Does.Not.Contain("NotifyCrisisTimeStarted"));

            Assert.That(controller.TryAcknowledgeCrisisStart().Succeeded, Is.True);
            Assert.That(controller.TryReportCrisisDurationElapsed().Succeeded, Is.True);

            Assert.That(water.Calls, Is.EqualTo(new[]
            {
                "NotifyGameFlowChanged:CampaignSelect",
                "NotifyGameFlowChanged:Loading",
                "NotifyNewRun",
                "Initialize",
                "NotifyGameFlowChanged:Gameplay",
                "NotifyGamePhaseChanged:Crisis",
                "NotifyCrisisTimeStarted",
                "NotifyCrisisTimeStopped",
                "NotifyGamePhaseChanged:Scoring"
            }));
        }

        private sealed class RecordingWaterAdapter : IDevGameStateWaterAdapter
        {
            public List<string> Calls { get; } = new List<string>();

            public string Name => "Water";

            public GameStateResult Initialize(Dev_ScenarioInitializationContext context)
            {
                Calls.Add("Initialize");
                return GameStateResult.Success();
            }

            public GameStateResult Teardown()
            {
                Calls.Add("Teardown");
                return GameStateResult.Success();
            }

            public GameStateResult NotifyNewRun()
            {
                Calls.Add("NotifyNewRun");
                return GameStateResult.Success();
            }

            public GameStateResult NotifyGameFlowChanged(GameFlow gameFlow)
            {
                Calls.Add("NotifyGameFlowChanged:" + gameFlow);
                return GameStateResult.Success();
            }

            public GameStateResult NotifyGamePhaseChanged(GamePhase gamePhase)
            {
                Calls.Add("NotifyGamePhaseChanged:" + gamePhase);
                return GameStateResult.Success();
            }

            public GameStateResult NotifyCompletedPreparationTurn(int completedPreparationTurns)
            {
                Calls.Add("NotifyCompletedPreparationTurn:" + completedPreparationTurns);
                return GameStateResult.Success();
            }

            public GameStateResult NotifyCrisisTimeStarted()
            {
                Calls.Add("NotifyCrisisTimeStarted");
                return GameStateResult.Success();
            }

            public GameStateResult NotifyCrisisTimeAdvanced(float simulatedDuration)
            {
                Calls.Add("NotifyCrisisTimeAdvanced:" + simulatedDuration);
                return GameStateResult.Success();
            }

            public GameStateResult NotifyCrisisTimeStopped()
            {
                Calls.Add("NotifyCrisisTimeStopped");
                return GameStateResult.Success();
            }
        }
    }
}
