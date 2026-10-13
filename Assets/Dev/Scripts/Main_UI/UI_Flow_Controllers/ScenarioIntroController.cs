using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class ScenarioIntroController : MonoBehaviour
{
    private const string DefaultPlayableSceneName = "PlayableTilemap";

    [Header("Scene")]
    [SerializeField] private string playableSceneName = DefaultPlayableSceneName;
    [SerializeField] private bool runOnStart = true;

    [Header("Timeline")]
    [SerializeField, Min(0f)] private float advisorDelaySeconds = 3f;
    [SerializeField, Min(0f)] private float lightRainDelayAfterInspectSeconds = 5f;
    [SerializeField, Min(0f)] private float heavyStormDelayAfterLightRainSeconds = 5f;

    [Header("References")]
    [SerializeField] private AdvisorMainController advisorMainController;
    [SerializeField] private WeatherSequenceController weatherSequenceController;
    [SerializeField] private HighRiskManager highRiskManager;
    [SerializeField] private ZoneBaselineRiskController baselineRiskController;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Coroutine _openingRoutine;
    private Coroutine _weatherTimelineRoutine;
    private bool _subscribedToAdvisor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneLoadedCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveScene()
    {
        TryBootstrapScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBootstrapScene(scene);
    }

    private static void TryBootstrapScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != DefaultPlayableSceneName)
            return;

#if UNITY_2023_1_OR_NEWER
        ScenarioIntroController[] controllers = UnityEngine.Object.FindObjectsByType<ScenarioIntroController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        ScenarioIntroController[] controllers = UnityEngine.Object.FindObjectsOfType<ScenarioIntroController>(true);
#pragma warning restore CS0618
#endif

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null && controllers[i].gameObject.scene == scene)
                return;
        }

        GameObject introObject = new GameObject(nameof(ScenarioIntroController));
        SceneManager.MoveGameObjectToScene(introObject, scene);
        introObject.AddComponent<ScenarioIntroController>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToAdvisor();

        if (runOnStart && IsPlayableSceneActive())
            StartOpeningSequence();
    }

    private void OnDisable()
    {
        UnsubscribeFromAdvisor();
        StopRoutine(ref _openingRoutine);
        StopRoutine(ref _weatherTimelineRoutine);
    }

    private void OnValidate()
    {
        advisorDelaySeconds = Mathf.Max(0f, advisorDelaySeconds);
        lightRainDelayAfterInspectSeconds = Mathf.Max(0f, lightRainDelayAfterInspectSeconds);
        heavyStormDelayAfterLightRainSeconds = Mathf.Max(0f, heavyStormDelayAfterLightRainSeconds);
    }

    public void StartOpeningSequence()
    {
        ResolveReferences();
        SubscribeToAdvisor();

        weatherSequenceController?.ClearWeather();
        HideBaselineRiskOverlay();
        advisorMainController?.HideOpeningBriefing();

        StopRoutine(ref _openingRoutine);
        StopRoutine(ref _weatherTimelineRoutine);
        _openingRoutine = StartCoroutine(OpeningRoutine());

        if (debugLogs)
            Debug.Log("[ScenarioIntroController] Opening sequence started.");
    }

    private IEnumerator OpeningRoutine()
    {
        if (advisorDelaySeconds > 0f)
            yield return new WaitForSeconds(advisorDelaySeconds);

        advisorMainController?.ShowOpeningBriefing();
        _openingRoutine = null;
    }

    private void OnInspectRiskRequested()
    {
        StopRoutine(ref _weatherTimelineRoutine);
        _weatherTimelineRoutine = StartCoroutine(WeatherTimelineRoutine());
    }

    private IEnumerator WeatherTimelineRoutine()
    {
        if (lightRainDelayAfterInspectSeconds > 0f)
            yield return new WaitForSeconds(lightRainDelayAfterInspectSeconds);

        weatherSequenceController?.BeginLightRain();

        if (heavyStormDelayAfterLightRainSeconds > 0f)
            yield return new WaitForSeconds(heavyStormDelayAfterLightRainSeconds);

        weatherSequenceController?.BeginHeavyStorm();
        _weatherTimelineRoutine = null;
    }

    private void ResolveReferences()
    {
        if (highRiskManager == null)
            highRiskManager = UnityEngine.Object.FindFirstObjectByType<HighRiskManager>();

        if (baselineRiskController == null)
            baselineRiskController = UnityEngine.Object.FindFirstObjectByType<ZoneBaselineRiskController>();

        if (weatherSequenceController == null)
            weatherSequenceController = UnityEngine.Object.FindFirstObjectByType<WeatherSequenceController>();

        if (weatherSequenceController == null)
            weatherSequenceController = gameObject.AddComponent<WeatherSequenceController>();

        weatherSequenceController.ResolveSceneReferences();

        if (advisorMainController == null)
            advisorMainController = UnityEngine.Object.FindFirstObjectByType<AdvisorMainController>();

        if (advisorMainController == null)
            advisorMainController = gameObject.AddComponent<AdvisorMainController>();

        advisorMainController.Configure(highRiskManager, baselineRiskController);
    }

    private void SubscribeToAdvisor()
    {
        if (advisorMainController == null || _subscribedToAdvisor)
            return;

        advisorMainController.InspectRiskRequested += OnInspectRiskRequested;
        _subscribedToAdvisor = true;
    }

    private void UnsubscribeFromAdvisor()
    {
        if (advisorMainController == null || !_subscribedToAdvisor)
            return;

        advisorMainController.InspectRiskRequested -= OnInspectRiskRequested;
        _subscribedToAdvisor = false;
    }

    private void HideBaselineRiskOverlay()
    {
        if (highRiskManager != null)
        {
            highRiskManager.HideBaselineRiskOverlay();
            return;
        }

        if (baselineRiskController != null)
            baselineRiskController.ClearRiskHighlights();
    }

    private bool IsPlayableSceneActive()
    {
        return SceneManager.GetActiveScene().name == playableSceneName;
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }
}
