using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

[DefaultExecutionOrder(-900)]
[RequireComponent(typeof(UIVisibilityGroupController))]
[RequireComponent(typeof(UITypewriterController))]
[RequireComponent(typeof(UIToolkitFadeTransition))]
public class GameIntroOutroSequenceController : MonoBehaviour
{
    private const string InspectRiskButtonName = "inspect_btn";

    private sealed class AdvisorMainButtonBinding
    {
        public Button button;
        public string buttonName;
        public Action handler;
    }

    [Header("Controllers")]
    [SerializeField] private UIVisibilityGroupController visibilityController;
    [SerializeField] private UITypewriterController typewriterController;
    [SerializeField] private UIToolkitFadeTransition fadeTransition;
    [SerializeField] private WaterSimulator waterSimulator;

    [Header("Startup")]
    [SerializeField] private bool showIntroOnStart = true;
    [SerializeField] private bool autoFindMissingReferences = true;
    [SerializeField, Min(0)] private int bindRetryFrames = 60;

    [Header("Fade Transitions")]
    [SerializeField] private bool useFadeTransitions = true;
    [SerializeField, Min(0f)] private float advisorMainFadeInSeconds = 0.45f;
    [SerializeField, Min(0f)] private float advisorMainFadeOutSeconds = 0.25f;
    [SerializeField, Min(0f)] private float scorePanelFadeInSeconds = 0.5f;
    [SerializeField, Min(0f)] private float scorePanelFadeOutSeconds = 0.25f;

    [Header("AdvisorMain Continue")]
    [SerializeField] private string advisorMainCloseButtonName;
    [SerializeField] private bool bindAllMatchingAdvisorMainCloseButtons = true;
    [FormerlySerializedAs("advisorMainCloseButtonCandidateNames")]
    [SerializeField]
    private string[] advisorMainContinueButtonNames =
    {
        InspectRiskButtonName,
        "close_btn",
        "close_button",
        "continue_btn",
        "start_btn",
        "start_game_btn",
        "advisor_main_close_btn",
        "advisor_continue_btn",
        "prepare_btn",
        "begin_preparation_btn",
        "begin_preperation_btn"
    };

    [Header("AdvisorMain Typewriter")]
    [SerializeField] private string advisorPara1LabelName = "para1";
    [SerializeField] private string advisorPara2LabelName = "para2";
    [SerializeField, Min(0f)] private float advisorCharactersPerSecond = 45f;
    [SerializeField, Min(0f)] private float advisorPauseBetweenLabelsSeconds = 0.25f;

    [Header("Score Sequence")]
    [SerializeField, Min(0f)] private float scorePanelDelaySeconds = 7f;
    [SerializeField] private string scoreContentLabelName = "score_panel_content_label";
    [SerializeField, Min(0f)] private float scoreCharactersPerSecond = 40f;
    [SerializeField, Min(0f)] private float scoreTypewriterStartDelaySeconds = 0.1f;
    [SerializeField] private bool subscribeToWaterSimulatorStarted = true;
    [SerializeField] private bool startScoreTimerIfFloodAlreadyRunning = true;

    [Header("Score Pause")]
    [SerializeField] private bool pauseGameWhenScorePanelShows = true;
    [SerializeField] private bool restoreTimeScaleWhenScorePanelCloses = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<AdvisorMainButtonBinding> _advisorMainButtonBindings = new List<AdvisorMainButtonBinding>();
    private Coroutine _introBindRoutine;
    private Coroutine _introCompleteRoutine;
    private Coroutine _scoreTimerRoutine;
    private Coroutine _scoreHideRoutine;
    private bool _subscribedToWaterSimulator;
    private bool _introTypewriterStarted;
    private bool _scoreTimerStarted;
    private bool _introCompleted;
    private bool _timeScalePausedByScorePanel;
    private float _timeScaleBeforeScorePanel = 1f;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToWaterSimulator();

        if (showIntroOnStart)
            BeginIntroSequence();
        else
            StartIntroBindingOnly();

        if (startScoreTimerIfFloodAlreadyRunning &&
            waterSimulator != null &&
            waterSimulator.IsSimulationRunning)
        {
            StartFloodEndSequenceTimer();
        }
    }

    private void Start()
    {
        if (!showIntroOnStart || _introCompleted)
            return;

        ResolveReferences();
        visibilityController?.ShowIntroState();
        StartAdvisorMainFadeIn();
    }

    private void OnDisable()
    {
        UnsubscribeFromWaterSimulator();
        UnbindAdvisorMainContinueButtons();
        StopRoutine(ref _introBindRoutine);
        StopRoutine(ref _introCompleteRoutine);
        StopRoutine(ref _scoreTimerRoutine);
        StopRoutine(ref _scoreHideRoutine);
        ResumeGameplayTimeScale();
    }

    private void OnValidate()
    {
        bindRetryFrames = Mathf.Max(0, bindRetryFrames);
        advisorMainFadeInSeconds = Mathf.Max(0f, advisorMainFadeInSeconds);
        advisorMainFadeOutSeconds = Mathf.Max(0f, advisorMainFadeOutSeconds);
        scorePanelFadeInSeconds = Mathf.Max(0f, scorePanelFadeInSeconds);
        scorePanelFadeOutSeconds = Mathf.Max(0f, scorePanelFadeOutSeconds);
        advisorCharactersPerSecond = Mathf.Max(0f, advisorCharactersPerSecond);
        advisorPauseBetweenLabelsSeconds = Mathf.Max(0f, advisorPauseBetweenLabelsSeconds);
        scorePanelDelaySeconds = Mathf.Max(0f, scorePanelDelaySeconds);
        scoreCharactersPerSecond = Mathf.Max(0f, scoreCharactersPerSecond);
        scoreTypewriterStartDelaySeconds = Mathf.Max(0f, scoreTypewriterStartDelaySeconds);
    }

    public void BeginIntroSequence()
    {
        ResolveReferences();

        _introCompleted = false;
        _introTypewriterStarted = false;
        visibilityController?.ShowIntroState();
        StartAdvisorMainFadeIn();

        StopRoutine(ref _introBindRoutine);
        _introBindRoutine = StartCoroutine(BindIntroWhenReady(true));

        if (debugLogs)
            Debug.Log("[GameIntroOutroSequenceController] Intro state shown.");
    }

    public void ShowGameplayState()
    {
        CompleteIntroAndShowGameplay();
    }

    public void CompleteIntroAndShowGameplay()
    {
        if (_introCompleted)
            return;

        _introCompleted = true;
        StopRoutine(ref _introCompleteRoutine);
        _introCompleteRoutine = StartCoroutine(CompleteIntroRoutine());
    }

    public void OnFloodStarted()
    {
        StartFloodEndSequenceTimer();
    }

    public void StartFloodEndSequenceTimer()
    {
        if (_scoreTimerStarted)
        {
            if (debugLogs)
                Debug.Log("[GameIntroOutroSequenceController] Score timer already started; ignoring duplicate flood start.");

            return;
        }

        ResolveReferences();
        _scoreTimerStarted = true;
        StopRoutine(ref _scoreTimerRoutine);
        _scoreTimerRoutine = StartCoroutine(ScoreTimerRoutine());

        if (debugLogs)
            Debug.Log($"[GameIntroOutroSequenceController] Score panel timer started for {scorePanelDelaySeconds:0.##} seconds.");
    }

    public void ShowScoreStateNow()
    {
        ResolveReferences();
        PauseGameplayForScorePanel();
        visibilityController?.ShowScoreState();
        StartScorePanelFadeIn();
        StartScoreTypewriter(true);

        if (debugLogs)
            Debug.Log("[GameIntroOutroSequenceController] Score state shown.");
    }

    public void HideScorePanelAndResumeGameplay()
    {
        StopRoutine(ref _scoreHideRoutine);
        _scoreHideRoutine = StartCoroutine(HideScorePanelRoutine());
    }

    public void ResumeGameplayTimeScale()
    {
        if (!_timeScalePausedByScorePanel)
            return;

        Time.timeScale = _timeScaleBeforeScorePanel;
        _timeScalePausedByScorePanel = false;

        if (debugLogs)
            Debug.Log($"[GameIntroOutroSequenceController] Restored Time.timeScale to {_timeScaleBeforeScorePanel:0.###}.");
    }

    public void ResetSequenceForTesting()
    {
        StopRoutine(ref _introBindRoutine);
        StopRoutine(ref _introCompleteRoutine);
        StopRoutine(ref _scoreTimerRoutine);
        StopRoutine(ref _scoreHideRoutine);

        _introCompleted = false;
        _introTypewriterStarted = false;
        _scoreTimerStarted = false;

        ResumeGameplayTimeScale();
        typewriterController?.FinishInstantly();
        typewriterController?.ClearCachedOriginals();
        BeginIntroSequence();
    }

    private IEnumerator BindIntroWhenReady(bool startTypewriter)
    {
        bool continueButtonsBound = false;

        for (int waitedFrames = 0; waitedFrames <= bindRetryFrames; waitedFrames++)
        {
            ResolveReferences();

            if (!continueButtonsBound)
                continueButtonsBound = BindAdvisorMainContinueButtons(false);

            if (startTypewriter && !_introTypewriterStarted)
                _introTypewriterStarted = StartAdvisorMainTypewriter(false);

            if (continueButtonsBound && (!startTypewriter || _introTypewriterStarted))
                break;

            yield return null;
        }

        if (!continueButtonsBound)
            continueButtonsBound = BindAdvisorMainContinueButtons(true);

        if (startTypewriter && !_introTypewriterStarted)
            _introTypewriterStarted = StartAdvisorMainTypewriter(true);

        _introBindRoutine = null;
    }

    private IEnumerator CompleteIntroRoutine()
    {
        typewriterController?.FinishInstantly();

        if (useFadeTransitions &&
            fadeTransition != null &&
            visibilityController != null &&
            visibilityController.TryGetAdvisorMainRoot(out VisualElement advisorMainRoot))
        {
            yield return fadeTransition.FadeOut(advisorMainRoot, advisorMainFadeOutSeconds);
        }

        visibilityController?.ShowGameplayState();

        if (debugLogs)
            Debug.Log("[GameIntroOutroSequenceController] Intro completed; gameplay HUD shown.");

        _introCompleteRoutine = null;
    }

    private void StartIntroBindingOnly()
    {
        StopRoutine(ref _introBindRoutine);
        _introBindRoutine = StartCoroutine(BindIntroWhenReady(false));
    }

    private bool BindAdvisorMainContinueButtons(bool logWarnings)
    {
        if (_advisorMainButtonBindings.Count > 0)
            return true;

        if (visibilityController == null || visibilityController.AdvisorMainDocument == null)
        {
            if (logWarnings)
                Debug.LogWarning("[GameIntroOutroSequenceController] AdvisorMain UIDocument is not assigned. Assign it on UIVisibilityGroupController.");

            return false;
        }

        VisualElement root = visibilityController.AdvisorMainDocument.rootVisualElement;

        if (root == null)
            return false;

        List<string> candidateNames = BuildAdvisorMainContinueButtonNames();
        bool foundAnyButton = false;
        bool foundInspectButton = false;

        for (int i = 0; i < candidateNames.Count; i++)
        {
            string buttonName = candidateNames[i];

            if (string.IsNullOrWhiteSpace(buttonName))
                continue;

            Button button = root.Q<Button>(buttonName);

            if (button == null)
                continue;

            foundAnyButton = true;
            foundInspectButton |= buttonName == InspectRiskButtonName;

            string capturedButtonName = buttonName;
            Action handler = () => OnAdvisorMainContinueClicked(capturedButtonName);
            button.clicked += handler;

            _advisorMainButtonBindings.Add(new AdvisorMainButtonBinding
            {
                button = button,
                buttonName = capturedButtonName,
                handler = handler
            });

            if (debugLogs)
                Debug.Log($"[GameIntroOutroSequenceController] Registered AdvisorMain continue button '{buttonName}'.");

            if (!bindAllMatchingAdvisorMainCloseButtons)
                break;
        }

        if (!foundInspectButton && logWarnings)
            Debug.LogWarning("AdvisorMain inspect_btn was not found. Check the button name in AdvisorMain.uxml.");

        if (!foundAnyButton && logWarnings)
        {
            Debug.LogWarning(
                "[GameIntroOutroSequenceController] Could not find an AdvisorMain close/start/continue button. " +
                "Fill advisorMainCloseButtonName or advisorMainContinueButtonNames in the Inspector if this UXML uses a custom button name.");
        }

        return foundAnyButton;
    }

    private void UnbindAdvisorMainContinueButtons()
    {
        for (int i = 0; i < _advisorMainButtonBindings.Count; i++)
        {
            AdvisorMainButtonBinding binding = _advisorMainButtonBindings[i];

            if (binding?.button != null && binding.handler != null)
                binding.button.clicked -= binding.handler;
        }

        _advisorMainButtonBindings.Clear();
    }

    private void OnAdvisorMainContinueClicked(string buttonName)
    {
        if (debugLogs)
            Debug.Log($"[GameIntroOutroSequenceController] AdvisorMain continue button '{buttonName}' clicked.");

        CompleteIntroAndShowGameplay();
    }

    private bool StartAdvisorMainTypewriter(bool logWarnings)
    {
        if (visibilityController == null || visibilityController.AdvisorMainDocument == null)
        {
            if (logWarnings)
                Debug.LogWarning("[GameIntroOutroSequenceController] AdvisorMain UIDocument is missing, so intro typewriter could not start.");

            return false;
        }

        VisualElement root = visibilityController.AdvisorMainDocument.rootVisualElement;

        if (root == null)
            return false;

        Label para1 = root.Q<Label>(advisorPara1LabelName);
        Label para2 = root.Q<Label>(advisorPara2LabelName);

        if (para1 == null || para2 == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning(
                    $"[GameIntroOutroSequenceController] Could not find AdvisorMain Labels '{advisorPara1LabelName}' and/or '{advisorPara2LabelName}'.");
            }

            return false;
        }

        if (typewriterController == null)
        {
            if (logWarnings)
                Debug.LogWarning("[GameIntroOutroSequenceController] UITypewriterController is missing.");

            return false;
        }

        typewriterController.TypeLabelsInSequence(
            new[] { para1, para2 },
            advisorCharactersPerSecond,
            advisorPauseBetweenLabelsSeconds);

        return true;
    }

    private IEnumerator ScoreTimerRoutine()
    {
        if (scorePanelDelaySeconds > 0f)
            yield return new WaitForSeconds(scorePanelDelaySeconds);

        _scoreTimerRoutine = null;
        ShowScoreStateNow();
    }

    private IEnumerator HideScorePanelRoutine()
    {
        if (useFadeTransitions &&
            fadeTransition != null &&
            visibilityController != null &&
            visibilityController.TryGetScorePanelRoot(out VisualElement scoreRoot))
        {
            yield return fadeTransition.FadeOut(scoreRoot, scorePanelFadeOutSeconds);
        }

        visibilityController?.SetScorePanelVisible(false);

        if (restoreTimeScaleWhenScorePanelCloses)
            ResumeGameplayTimeScale();

        _scoreHideRoutine = null;
    }

    private bool StartScoreTypewriter(bool logWarnings)
    {
        if (typewriterController == null)
        {
            if (logWarnings)
                Debug.LogWarning("[GameIntroOutroSequenceController] UITypewriterController is missing.");

            return false;
        }

        if (visibilityController == null || visibilityController.ScorePanelDocument == null)
        {
            if (logWarnings)
                Debug.LogWarning("[GameIntroOutroSequenceController] ScorePanel UIDocument is missing, so score typewriter could not start.");

            return false;
        }

        VisualElement root = visibilityController.ScorePanelDocument.rootVisualElement;

        if (root == null)
            return false;

        Label scoreLabel = root.Q<Label>(scoreContentLabelName);

        if (scoreLabel == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[GameIntroOutroSequenceController] Could not find ScorePanel Label '{scoreContentLabelName}'.");

            return false;
        }

        typewriterController.TypeLabel(scoreLabel, scoreCharactersPerSecond, scoreTypewriterStartDelaySeconds);
        return true;
    }

    private void StartAdvisorMainFadeIn()
    {
        if (visibilityController == null || !visibilityController.TryGetAdvisorMainRoot(out VisualElement advisorMainRoot))
            return;

        if (visibilityController.TryGetAdvisorMainHost(out VisualElement advisorMainHost))
        {
            advisorMainHost.style.display = DisplayStyle.Flex;
            advisorMainHost.style.opacity = 1f;
        }

        if (useFadeTransitions && fadeTransition != null)
            fadeTransition.FadeIn(advisorMainRoot, advisorMainFadeInSeconds);
        else
            advisorMainRoot.style.opacity = 1f;
    }

    private void StartScorePanelFadeIn()
    {
        if (visibilityController == null || !visibilityController.TryGetScorePanelRoot(out VisualElement scoreRoot))
            return;

        if (useFadeTransitions && fadeTransition != null)
            fadeTransition.FadeIn(scoreRoot, scorePanelFadeInSeconds);
        else
            scoreRoot.style.opacity = 1f;
    }

    private void PauseGameplayForScorePanel()
    {
        if (!pauseGameWhenScorePanelShows || _timeScalePausedByScorePanel)
            return;

        _timeScaleBeforeScorePanel = Time.timeScale;
        Time.timeScale = 0f;
        _timeScalePausedByScorePanel = true;

        if (debugLogs)
            Debug.Log($"[GameIntroOutroSequenceController] Paused gameplay for ScorePanel. Previous Time.timeScale was {_timeScaleBeforeScorePanel:0.###}.");
    }

    private void SubscribeToWaterSimulator()
    {
        if (!subscribeToWaterSimulatorStarted || _subscribedToWaterSimulator)
            return;

        ResolveReferences();

        if (waterSimulator == null)
        {
            if (debugLogs)
                Debug.LogWarning("[GameIntroOutroSequenceController] WaterSimulator was not found. Call OnFloodStarted() manually or assign WaterSimulator.");

            return;
        }

        waterSimulator.OnSimulationStarted -= OnFloodStarted;
        waterSimulator.OnSimulationStarted += OnFloodStarted;
        _subscribedToWaterSimulator = true;
    }

    private void UnsubscribeFromWaterSimulator()
    {
        if (waterSimulator == null || !_subscribedToWaterSimulator)
            return;

        waterSimulator.OnSimulationStarted -= OnFloodStarted;
        _subscribedToWaterSimulator = false;
    }

    private void ResolveReferences()
    {
        if (visibilityController == null)
        {
            if (!TryGetComponent<UIVisibilityGroupController>(out visibilityController))
                visibilityController = gameObject.AddComponent<UIVisibilityGroupController>();
        }

        if (typewriterController == null)
        {
            if (!TryGetComponent<UITypewriterController>(out typewriterController))
                typewriterController = gameObject.AddComponent<UITypewriterController>();
        }

        if (fadeTransition == null)
        {
            if (!TryGetComponent<UIToolkitFadeTransition>(out fadeTransition))
                fadeTransition = gameObject.AddComponent<UIToolkitFadeTransition>();
        }

        if (autoFindMissingReferences && waterSimulator == null)
            waterSimulator = FindFirstObjectByType<WaterSimulator>();

        visibilityController?.ResolveDocuments();
    }

    private List<string> BuildAdvisorMainContinueButtonNames()
    {
        List<string> candidateNames = new List<string>();

        AddCandidateName(candidateNames, advisorMainCloseButtonName);
        AddDefaultAdvisorMainContinueButtonNames(candidateNames);

        if (advisorMainContinueButtonNames != null)
        {
            for (int i = 0; i < advisorMainContinueButtonNames.Length; i++)
                AddCandidateName(candidateNames, advisorMainContinueButtonNames[i]);
        }

        return candidateNames;
    }

    private void AddDefaultAdvisorMainContinueButtonNames(List<string> candidateNames)
    {
        AddCandidateName(candidateNames, InspectRiskButtonName);
        AddCandidateName(candidateNames, "close_btn");
        AddCandidateName(candidateNames, "close_button");
        AddCandidateName(candidateNames, "continue_btn");
        AddCandidateName(candidateNames, "start_btn");
        AddCandidateName(candidateNames, "start_game_btn");
        AddCandidateName(candidateNames, "advisor_main_close_btn");
        AddCandidateName(candidateNames, "advisor_continue_btn");
        AddCandidateName(candidateNames, "prepare_btn");
        AddCandidateName(candidateNames, "begin_preparation_btn");
        AddCandidateName(candidateNames, "begin_preperation_btn");
    }

    private void AddCandidateName(List<string> candidateNames, string candidateName)
    {
        if (string.IsNullOrWhiteSpace(candidateName))
            return;

        if (!candidateNames.Contains(candidateName))
            candidateNames.Add(candidateName);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }
}
