using System.Collections;
using UnityEngine;

public class WeatherSequenceController : MonoBehaviour
{
    public enum WeatherSequenceState
    {
        Clear,
        LightRain,
        HeavyStorm,
    }

    [Header("References")]
    [SerializeField] private ParticleSystem rainfallParticleSystem;
    [SerializeField] private LightningController lightningController;

    [Header("Rain Settings")]
    [SerializeField, Min(0f)] private float lightRainEmissionMultiplier = 0.25f;
    [SerializeField, Min(0f)] private float lightRainSpeedMultiplier = 0.55f;
    [SerializeField, Min(0f)] private float heavyRainEmissionMultiplier = 1f;
    [SerializeField, Min(0f)] private float heavyRainSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float rainTransitionDuration = 2f;
    [SerializeField, Min(0f)] private float lightningDelayAfterHeavyRainBegins = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Coroutine _rainTransitionRoutine;
    private Coroutine _lightningDelayRoutine;
    private bool _capturedRainDefaults;
    private float _authoredRateOverTimeMultiplier;
    private float _authoredRateOverDistanceMultiplier;
    private float _authoredStartSpeedMultiplier;

    public WeatherSequenceState CurrentState { get; private set; } = WeatherSequenceState.Clear;

    private void Awake()
    {
        ResolveSceneReferences();
        CaptureRainDefaults();
    }

    private void OnDisable()
    {
        StopRainTransition();
        StopLightningDelay();
    }

    private void OnValidate()
    {
        lightRainEmissionMultiplier = Mathf.Max(0f, lightRainEmissionMultiplier);
        lightRainSpeedMultiplier = Mathf.Max(0f, lightRainSpeedMultiplier);
        heavyRainEmissionMultiplier = Mathf.Max(0f, heavyRainEmissionMultiplier);
        heavyRainSpeedMultiplier = Mathf.Max(0f, heavyRainSpeedMultiplier);
        rainTransitionDuration = Mathf.Max(0f, rainTransitionDuration);
        lightningDelayAfterHeavyRainBegins = Mathf.Max(0f, lightningDelayAfterHeavyRainBegins);
    }

    public void ResolveSceneReferences()
    {
        if (rainfallParticleSystem == null)
            rainfallParticleSystem = FindRainfallParticleSystem();

        if (lightningController == null)
            lightningController = FindLightningController();
    }

    public void ClearWeather()
    {
        ResolveSceneReferences();
        CaptureRainDefaults();
        StopRainTransition();
        StopLightningDelay();
        StopLightning();

        if (rainfallParticleSystem != null)
        {
            if (!rainfallParticleSystem.gameObject.activeSelf)
                rainfallParticleSystem.gameObject.SetActive(true);

            ApplyRainSettings(0f, 0f, _authoredStartSpeedMultiplier);
            rainfallParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rainfallParticleSystem.gameObject.SetActive(false);
        }

        CurrentState = WeatherSequenceState.Clear;

        if (debugLogs)
            Debug.Log("[WeatherSequenceController] Weather state set to CLEAR.");
    }

    public void BeginLightRain()
    {
        ResolveSceneReferences();
        StopLightningDelay();
        StopLightning();
        BeginRainState(
            WeatherSequenceState.LightRain,
            _authoredRateOverTimeMultiplier * lightRainEmissionMultiplier,
            _authoredRateOverDistanceMultiplier * lightRainEmissionMultiplier,
            _authoredStartSpeedMultiplier * lightRainSpeedMultiplier);
    }

    public void BeginHeavyStorm()
    {
        ResolveSceneReferences();
        BeginRainState(
            WeatherSequenceState.HeavyStorm,
            _authoredRateOverTimeMultiplier * heavyRainEmissionMultiplier,
            _authoredRateOverDistanceMultiplier * heavyRainEmissionMultiplier,
            _authoredStartSpeedMultiplier * heavyRainSpeedMultiplier);

        StopLightningDelay();
        _lightningDelayRoutine = StartCoroutine(StartLightningAfterDelayRoutine());
    }

    private void BeginRainState(WeatherSequenceState state, float targetRateOverTime, float targetRateOverDistance, float targetStartSpeed)
    {
        CaptureRainDefaults();

        if (rainfallParticleSystem == null)
        {
            Debug.LogWarning($"[WeatherSequenceController] Cannot enter {state} because Rainfall ParticleSystem was not found.");
            return;
        }

        if (!rainfallParticleSystem.gameObject.activeSelf)
            rainfallParticleSystem.gameObject.SetActive(true);

        ParticleSystem.EmissionModule emission = rainfallParticleSystem.emission;
        emission.enabled = true;

        if (!rainfallParticleSystem.isPlaying)
            rainfallParticleSystem.Play(true);

        StopRainTransition();
        _rainTransitionRoutine = StartCoroutine(TransitionRainRoutine(targetRateOverTime, targetRateOverDistance, targetStartSpeed));
        CurrentState = state;

        if (debugLogs)
            Debug.Log($"[WeatherSequenceController] Weather state set to {state}.");
    }

    private IEnumerator TransitionRainRoutine(float targetRateOverTime, float targetRateOverDistance, float targetStartSpeed)
    {
        ParticleSystem.EmissionModule emission = rainfallParticleSystem.emission;
        ParticleSystem.MainModule main = rainfallParticleSystem.main;

        float startRateOverTime = emission.rateOverTimeMultiplier;
        float startRateOverDistance = emission.rateOverDistanceMultiplier;
        float startSpeed = main.startSpeedMultiplier;

        if (rainTransitionDuration <= 0f)
        {
            ApplyRainSettings(targetRateOverTime, targetRateOverDistance, targetStartSpeed);
            _rainTransitionRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < rainTransitionDuration)
        {
            float t = Mathf.Clamp01(elapsed / rainTransitionDuration);
            ApplyRainSettings(
                Mathf.Lerp(startRateOverTime, targetRateOverTime, t),
                Mathf.Lerp(startRateOverDistance, targetRateOverDistance, t),
                Mathf.Lerp(startSpeed, targetStartSpeed, t));

            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyRainSettings(targetRateOverTime, targetRateOverDistance, targetStartSpeed);
        _rainTransitionRoutine = null;
    }

    private IEnumerator StartLightningAfterDelayRoutine()
    {
        if (lightningDelayAfterHeavyRainBegins > 0f)
            yield return new WaitForSeconds(lightningDelayAfterHeavyRainBegins);

        _lightningDelayRoutine = null;

        if (CurrentState != WeatherSequenceState.HeavyStorm)
            yield break;

        StartLightning();
    }

    private void ApplyRainSettings(float rateOverTimeMultiplier, float rateOverDistanceMultiplier, float startSpeedMultiplier)
    {
        if (rainfallParticleSystem == null)
            return;

        ParticleSystem.EmissionModule emission = rainfallParticleSystem.emission;
        ParticleSystem.MainModule main = rainfallParticleSystem.main;

        emission.rateOverTimeMultiplier = Mathf.Max(0f, rateOverTimeMultiplier);
        emission.rateOverDistanceMultiplier = Mathf.Max(0f, rateOverDistanceMultiplier);
        main.startSpeedMultiplier = Mathf.Max(0f, startSpeedMultiplier);
    }

    private void CaptureRainDefaults()
    {
        if (_capturedRainDefaults)
            return;

        ResolveSceneReferences();

        if (rainfallParticleSystem == null)
            return;

        ParticleSystem.EmissionModule emission = rainfallParticleSystem.emission;
        ParticleSystem.MainModule main = rainfallParticleSystem.main;

        _authoredRateOverTimeMultiplier = emission.rateOverTimeMultiplier;
        _authoredRateOverDistanceMultiplier = emission.rateOverDistanceMultiplier;
        _authoredStartSpeedMultiplier = main.startSpeedMultiplier;
        _capturedRainDefaults = true;
    }

    private void StopLightning()
    {
        if (lightningController == null)
            return;

        if (lightningController.IsSequenceRunning)
            lightningController.StopLightningSequence();

        lightningController.enabled = false;
    }

    private void StartLightning()
    {
        if (lightningController == null)
        {
            Debug.LogWarning("[WeatherSequenceController] Cannot start lightning because LightningController was not found.");
            return;
        }

        if (!lightningController.gameObject.activeSelf)
            lightningController.gameObject.SetActive(true);

        lightningController.enabled = true;

        if (!lightningController.IsSequenceRunning)
            lightningController.StartLightningSequence();
    }

    private void StopRainTransition()
    {
        if (_rainTransitionRoutine == null)
            return;

        StopCoroutine(_rainTransitionRoutine);
        _rainTransitionRoutine = null;
    }

    private void StopLightningDelay()
    {
        if (_lightningDelayRoutine == null)
            return;

        StopCoroutine(_lightningDelayRoutine);
        _lightningDelayRoutine = null;
    }

    private static ParticleSystem FindRainfallParticleSystem()
    {
#if UNITY_2023_1_OR_NEWER
        ParticleSystem[] particleSystems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        ParticleSystem[] particleSystems = UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true);
#pragma warning restore CS0618
#endif

        ParticleSystem fallback = null;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem candidate = particleSystems[i];

            if (candidate == null)
                continue;

            if (fallback == null)
                fallback = candidate;

            if (candidate.name == "Rainfall")
                return candidate;
        }

        return fallback;
    }

    private static LightningController FindLightningController()
    {
#if UNITY_2023_1_OR_NEWER
        LightningController[] controllers = UnityEngine.Object.FindObjectsByType<LightningController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        LightningController[] controllers = UnityEngine.Object.FindObjectsOfType<LightningController>(true);
#pragma warning restore CS0618
#endif

        return controllers.Length > 0 ? controllers[0] : null;
    }
}
