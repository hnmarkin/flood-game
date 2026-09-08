using System.Collections.Generic;
using NUnit.Framework;

namespace FloodGame.Dev.GameState.Tests
{
    [TestFixture]
    public sealed class Dev_GameStateControllerTests
    {
        [Test]
        public void NewController_StartsInPersistentDefaultState()
        {
            Dev_GameStateController controller = new Dev_GameStateController();

            Assert.That(controller.GetGameFlow(), Is.EqualTo(GameFlow.MainMenu));
            Assert.That(controller.GetGamePhase(), Is.EqualTo(GamePhase.Preparation));
            Assert.That(controller.GetToolState(), Is.EqualTo(ToolState.Normal));
            Assert.That(controller.GetActiveToolId(), Is.Null);
            Assert.That(controller.IsCrisisAwaitingAcknowledgement(), Is.False);
        }

        [Test]
        public void InvalidAndRedundantFlowRequests_DoNotMutateOrPublish()
        {
            Dev_GameStateController controller = new Dev_GameStateController();
            int events = 0;
            controller.OnGameFlowChanged += _ => events++;

            ScenarioInitializationResult invalid = controller.TryBeginScenarioInitialization(
                new Dev_ScenarioConfiguration("scenario"));
            GameStateResult redundant = controller.TryEnterCampaignSelect();
            GameStateResult repeated = controller.TryEnterCampaignSelect();

            Assert.That(invalid.Succeeded, Is.False);
            Assert.That(redundant.Succeeded, Is.True);
            Assert.That(repeated.Succeeded, Is.False);
            Assert.That(controller.GetGameFlow(), Is.EqualTo(GameFlow.CampaignSelect));
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void Pause_ClearsActiveToolBeforeFlowEvent()
        {
            Dev_GameStateController controller = CreateGameplayController();
            List<string> eventOrder = new List<string>();
            controller.OnToolStateChanged += _ => eventOrder.Add("tool");
            controller.OnGameFlowChanged += _ => eventOrder.Add("flow");

            Assert.That(
                controller.TrySetToolState(ToolState.Placement, "barrier").Succeeded,
                Is.True);
            eventOrder.Clear();

            GameStateResult paused = controller.TryPause();

            Assert.That(paused.Succeeded, Is.True);
            Assert.That(controller.GetGameFlow(), Is.EqualTo(GameFlow.Pause));
            Assert.That(controller.GetToolState(), Is.EqualTo(ToolState.Normal));
            Assert.That(controller.GetActiveToolId(), Is.Null);
            Assert.That(eventOrder, Is.EqualTo(new[] { "tool", "flow" }));
        }

        [Test]
        public void PauseAndResume_AreLegalOnlyFromTheirOwningFlowStates()
        {
            Dev_GameStateController menuController = new Dev_GameStateController();
            GameStateResult invalidPause = menuController.TryPause();
            GameStateResult invalidResume = menuController.TryResume();
            Assert.That(invalidPause.FailureCode, Is.EqualTo(GameStateFailureCode.InvalidTransition));
            Assert.That(invalidResume.FailureCode, Is.EqualTo(GameStateFailureCode.InvalidTransition));

            Dev_GameStateController controller = CreateGameplayController();

            GameStateResult paused = controller.TryPause();
            GameStateResult repeatedPause = controller.TryPause();
            GameStateResult resumed = controller.TryResume();
            GameStateResult repeatedResume = controller.TryResume();

            Assert.That(paused.Succeeded, Is.True);
            Assert.That(paused.Changed, Is.True);
            Assert.That(repeatedPause.FailureCode, Is.EqualTo(GameStateFailureCode.RedundantRequest));
            Assert.That(resumed.Succeeded, Is.True);
            Assert.That(resumed.Changed, Is.True);
            Assert.That(repeatedResume.Succeeded, Is.False);
            Assert.That(repeatedResume.FailureCode, Is.EqualTo(GameStateFailureCode.RedundantRequest));
            Assert.That(controller.GetGameFlow(), Is.EqualTo(GameFlow.Gameplay));
        }

        [Test]
        public void ToolStates_ValidateActiveIdentityAndRejectRedundantRequests()
        {
            Dev_GameStateController controller = new Dev_GameStateController();

            GameStateResult outsideGameplay = controller.TrySetToolState(ToolState.Placement, "barrier");
            Assert.That(outsideGameplay.Succeeded, Is.False);

            controller = CreateGameplayController();
            GameStateResult missingIdentity = controller.TrySetToolState(ToolState.Inspection, null);
            GameStateResult placement = controller.TrySetToolState(ToolState.Placement, "barrier");
            GameStateResult repeatedPlacement = controller.TrySetToolState(ToolState.Placement, "barrier");
            GameStateResult inspection = controller.TrySetToolState(ToolState.Inspection, "inspection");
            GameStateResult normal = controller.TrySetToolState(ToolState.Normal, "ignored");
            GameStateResult repeatedNormal = controller.TrySetToolState(ToolState.Normal, null);

            Assert.That(missingIdentity.FailureCode, Is.EqualTo(GameStateFailureCode.InvalidArgument));
            Assert.That(placement.Succeeded, Is.True);
            Assert.That(repeatedPlacement.FailureCode, Is.EqualTo(GameStateFailureCode.RedundantRequest));
            Assert.That(inspection.Succeeded, Is.True);
            Assert.That(normal.Succeeded, Is.True);
            Assert.That(controller.GetToolState(), Is.EqualTo(ToolState.Normal));
            Assert.That(controller.GetActiveToolId(), Is.Null);
            Assert.That(repeatedNormal.FailureCode, Is.EqualTo(GameStateFailureCode.RedundantRequest));
        }

        [Test]
        public void PhaseLifecycle_ClearsToolAndRequiresCrisisAcknowledgement()
        {
            Dev_GameStateController controller = CreateGameplayController();
            Assert.That(
                controller.TrySetToolState(ToolState.Inspection, "inspection").Succeeded,
                Is.True);

            List<string> eventOrder = new List<string>();
            controller.OnToolStateChanged += _ => eventOrder.Add("tool");
            controller.OnGamePhaseChanged += _ => eventOrder.Add("phase");
            controller.OnCrisisPresentationRequested += _ => eventOrder.Add("presentation");

            GameStateResult crisis = controller.TryBeginCrisis();

            Assert.That(crisis.Succeeded, Is.True);
            Assert.That(controller.GetGamePhase(), Is.EqualTo(GamePhase.Crisis));
            Assert.That(controller.IsCrisisAwaitingAcknowledgement(), Is.True);
            Assert.That(controller.GetToolState(), Is.EqualTo(ToolState.Normal));
            Assert.That(eventOrder, Is.EqualTo(new[] { "tool", "phase", "presentation" }));

            GameStateResult acknowledged = controller.TryAcknowledgeCrisisStart();
            GameStateResult repeatedAcknowledgement = controller.TryAcknowledgeCrisisStart();

            Assert.That(acknowledged.Succeeded, Is.True);
            Assert.That(repeatedAcknowledgement.Succeeded, Is.False);
            Assert.That(controller.IsCrisisAwaitingAcknowledgement(), Is.False);
        }

        [Test]
        public void CrisisDurationExpiry_TransitionsImmediatelyToScoring()
        {
            Dev_GameStateController controller = CreateGameplayController();
            Assert.That(controller.TryBeginCrisis().Succeeded, Is.True);
            Assert.That(controller.TryAcknowledgeCrisisStart().Succeeded, Is.True);

            GameStateResult expired = controller.TryReportCrisisDurationElapsed();
            GameStateResult repeated = controller.TryReportCrisisDurationElapsed();

            Assert.That(expired.Succeeded, Is.True);
            Assert.That(repeated.Succeeded, Is.False);
            Assert.That(controller.GetGamePhase(), Is.EqualTo(GamePhase.Scoring));
            Assert.That(controller.IsCrisisAwaitingAcknowledgement(), Is.False);
        }

        [Test]
        public void PhaseRequests_FromWrongFlowOrPhase_DoNotMutateOrPublish()
        {
            Dev_GameStateController controller = new Dev_GameStateController();
            int phaseEvents = 0;
            controller.OnGamePhaseChanged += _ => phaseEvents++;

            GameStateResult invalidCrisis = controller.TryBeginCrisis();
            GameStateResult invalidAcknowledgement = controller.TryAcknowledgeCrisisStart();
            GameStateResult invalidExpiry = controller.TryReportCrisisDurationElapsed();

            Assert.That(invalidCrisis.Succeeded, Is.False);
            Assert.That(invalidAcknowledgement.Succeeded, Is.False);
            Assert.That(invalidExpiry.Succeeded, Is.False);
            Assert.That(controller.GetGamePhase(), Is.EqualTo(GamePhase.Preparation));
            Assert.That(phaseEvents, Is.Zero);

            controller = CreateGameplayController();
            Assert.That(controller.TryBeginCrisis().Succeeded, Is.True);
            GameStateResult repeatedCrisis = controller.TryBeginCrisis();
            Assert.That(repeatedCrisis.FailureCode, Is.EqualTo(GameStateFailureCode.InvalidTransition));
        }

        [Test]
        public void NewScenario_RestoresPreparationAndNormalAfterPriorScenario()
        {
            Dev_GameStateController controller = CreateGameplayController();
            Assert.That(
                controller.TrySetToolState(ToolState.Placement, "barrier").Succeeded,
                Is.True);
            Assert.That(controller.TryEndScenario().Succeeded, Is.True);
            Assert.That(controller.TryEnterCampaignSelect().Succeeded, Is.True);

            ScenarioInitializationResult initialized = controller.TryBeginScenarioInitialization(
                new Dev_ScenarioConfiguration("next-scenario"));

            Assert.That(initialized.Succeeded, Is.True);
            Assert.That(controller.GetGamePhase(), Is.EqualTo(GamePhase.Preparation));
            Assert.That(controller.GetToolState(), Is.EqualTo(ToolState.Normal));
            Assert.That(controller.GetActiveToolId(), Is.Null);
        }

        [Test]
        public void EndingScenario_PublishesEndingBeforeTeardownAndReturnsToMainMenu()
        {
            Dev_GameStateController controller = CreateGameplayController();
            Assert.That(
                controller.TrySetToolState(ToolState.Placement, "barrier").Succeeded,
                Is.True);

            List<string> eventOrder = new List<string>();
            controller.OnScenarioEnding += args =>
            {
                eventOrder.Add("ending");
                Assert.That(args.State.GameFlow, Is.EqualTo(GameFlow.Gameplay));
                Assert.That(args.ScenarioId, Is.EqualTo("scenario"));
            };
            controller.OnToolStateChanged += _ => eventOrder.Add("tool");
            controller.OnGameFlowChanged += args => eventOrder.Add(args.Current.ToString());

            GameStateResult ended = controller.TryEndScenario();

            Assert.That(ended.Succeeded, Is.True);
            Assert.That(controller.GetGameFlow(), Is.EqualTo(GameFlow.MainMenu));
            Assert.That(controller.GetGamePhase(), Is.EqualTo(GamePhase.Preparation));
            Assert.That(controller.GetToolState(), Is.EqualTo(ToolState.Normal));
            Assert.That(controller.GetScenarioId(), Is.Null);
            Assert.That(eventOrder, Is.EqualTo(new[]
            {
                "ending",
                "tool",
                GameFlow.MainMenu.ToString()
            }));
        }

        private static Dev_GameStateController CreateGameplayController()
        {
            Dev_GameStateController controller = new Dev_GameStateController();
            Assert.That(controller.TryEnterCampaignSelect().Succeeded, Is.True);
            ScenarioInitializationResult initialized = controller.TryBeginScenarioInitialization(
                new Dev_ScenarioConfiguration("scenario"));
            Assert.That(initialized.Succeeded, Is.True, initialized.Message);
            return controller;
        }
    }
}
