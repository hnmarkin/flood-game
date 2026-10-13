using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningController : MonoBehaviour
{
    [Serializable]
    public class LightningPrefabEntry
    {
        public bool isEnabled = true;
        public string displayName = "Lightning Strike";
        public GameObject lightningPrefab;
        [Min(0)] public int strikeCount = 1;
        public Vector3 spawnPosition = Vector3.zero;
        public Vector3 spawnScale = Vector3.one;
        public bool useCustomDelayRange = false;
        [Min(0f)] public float minDelayBeforeNextStrike = 1f;
        [Min(0f)] public float maxDelayBeforeNextStrike = 2f;
    }

    [Header("Startup")]
    [SerializeField] private bool playOnStart = false;

    [Header("Global Timing")]
    [Min(0f)]
    [SerializeField] private float minTimeBetweenStrikes = 1f;

    [Min(0f)]
    [SerializeField] private float maxTimeBetweenStrikes = 3f;

    [Header("Lightning Prefabs")]
    [SerializeField] private List<LightningPrefabEntry> lightningPrefabs = new List<LightningPrefabEntry>();

    [Header("Optional Cleanup")]
    [SerializeField] private bool autoDestroySpawnedPrefabs = false;

    [Min(0f)]
    [SerializeField] private float destroyAfterSeconds = 3f;

    private Coroutine lightningSequenceCoroutine;

    public bool IsSequenceRunning { get; private set; }

    private void Start()
    {
        if (playOnStart)
            StartLightningSequence();
    }

    private void OnDisable()
    {
        StopSequenceInternal(false);
    }

    private void OnValidate()
    {
        if (maxTimeBetweenStrikes < minTimeBetweenStrikes)
            maxTimeBetweenStrikes = minTimeBetweenStrikes;

        if (lightningPrefabs == null)
            return;

        foreach (LightningPrefabEntry entry in lightningPrefabs)
        {
            if (entry == null)
                continue;

            if (entry.strikeCount < 0)
                entry.strikeCount = 0;

            if (entry.maxDelayBeforeNextStrike < entry.minDelayBeforeNextStrike)
                entry.maxDelayBeforeNextStrike = entry.minDelayBeforeNextStrike;
        }
    }

    // Future scripts can keep a reference to this controller and call:
    // lightningController.StartLightningSequence();
    // lightningController.StopLightningSequence();
    // lightningController.PlaySingleRandomStrike();
    // Starts a looping lightning sequence that keeps running until StopLightningSequence is called.
    public void StartLightningSequence()
    {
        if (IsSequenceRunning)
        {
            Debug.Log("LightningController: StartLightningSequence was called, but the sequence is already running.");
            return;
        }

        if (!HasPlayableEntries(true))
            return;

        lightningSequenceCoroutine = StartCoroutine(LightningSequenceRoutine(true));
    }

    public void StopLightningSequence()
    {
        StopSequenceInternal(true);
    }

    public void PlaySingleRandomStrike()
    {
        LightningPrefabEntry randomEntry = GetRandomPlayableEntry();
        if (randomEntry == null)
        {
            Debug.LogWarning("LightningController: No enabled lightning prefab entries are ready to spawn.");
            return;
        }

        SpawnStrike(randomEntry);
    }

    // Plays the configured list one time, which is useful for testing in the editor.
    public void PlayConfiguredSequence()
    {
        if (IsSequenceRunning)
        {
            Debug.Log("LightningController: PlayConfiguredSequence was called, but a sequence is already running.");
            return;
        }

        if (!HasPlayableEntries(true))
            return;

        lightningSequenceCoroutine = StartCoroutine(LightningSequenceRoutine(false));
    }

    [ContextMenu("Test Lightning Sequence")]
    private void TestLightningSequence()
    {
        PlayConfiguredSequence();
    }

    [ContextMenu("Test Single Random Strike")]
    private void TestSingleRandomStrike()
    {
        PlaySingleRandomStrike();
    }

    private IEnumerator LightningSequenceRoutine(bool loopSequence)
    {
        IsSequenceRunning = true;
        Debug.Log("LightningController: Lightning sequence started.");

        do
        {
            yield return PlayConfiguredPass(loopSequence);
        }
        while (loopSequence && HasPlayableEntries(false));

        IsSequenceRunning = false;
        lightningSequenceCoroutine = null;
        Debug.Log("LightningController: Lightning sequence ended.");
    }

    // Walk through the enabled entries in inspector order and spawn each one by its configured count.
    private IEnumerator PlayConfiguredPass(bool waitAfterFinalStrike)
    {
        List<LightningPrefabEntry> playableEntries = GetPlayableEntries();
        int totalStrikesToSpawn = GetTotalStrikeCount(playableEntries);
        int strikesSpawned = 0;

        foreach (LightningPrefabEntry entry in playableEntries)
        {
            for (int i = 0; i < entry.strikeCount; i++)
            {
                SpawnStrike(entry);
                strikesSpawned++;

                bool shouldWaitForNextStrike = waitAfterFinalStrike || strikesSpawned < totalStrikesToSpawn;
                if (!shouldWaitForNextStrike)
                    continue;

                float delay = GetDelayBeforeNextStrike(entry);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
                else
                    yield return null;
            }
        }
    }

    private void SpawnStrike(LightningPrefabEntry entry)
    {
        if (entry == null || entry.lightningPrefab == null)
            return;

        GameObject spawnedStrike = Instantiate(
            entry.lightningPrefab,
            entry.spawnPosition,
            entry.lightningPrefab.transform.rotation
        );

        spawnedStrike.transform.localScale = entry.spawnScale;

        Debug.Log(
            $"LightningController: Spawned '{GetEntryName(entry)}' at {entry.spawnPosition} with scale {entry.spawnScale}."
        );

        if (autoDestroySpawnedPrefabs)
            Destroy(spawnedStrike, destroyAfterSeconds);
    }

    private void StopSequenceInternal(bool logMessage)
    {
        bool wasRunning = IsSequenceRunning;

        if (lightningSequenceCoroutine != null)
        {
            StopCoroutine(lightningSequenceCoroutine);
            lightningSequenceCoroutine = null;
        }

        IsSequenceRunning = false;

        if (!logMessage)
            return;

        if (wasRunning)
            Debug.Log("LightningController: Lightning sequence stopped.");
        else
            Debug.Log("LightningController: StopLightningSequence was called, but no sequence was running.");
    }

    private bool HasPlayableEntries(bool logWarnings)
    {
        List<LightningPrefabEntry> playableEntries = GetPlayableEntries();
        if (playableEntries.Count > 0)
            return true;

        if (logWarnings)
            Debug.LogWarning("LightningController: No enabled lightning prefab entries with a prefab and strike count greater than zero were found.");

        return false;
    }

    private List<LightningPrefabEntry> GetPlayableEntries()
    {
        List<LightningPrefabEntry> playableEntries = new List<LightningPrefabEntry>();

        if (lightningPrefabs == null)
            return playableEntries;

        foreach (LightningPrefabEntry entry in lightningPrefabs)
        {
            if (entry == null)
                continue;

            if (!entry.isEnabled)
                continue;

            if (entry.lightningPrefab == null)
                continue;

            if (entry.strikeCount <= 0)
                continue;

            playableEntries.Add(entry);
        }

        return playableEntries;
    }

    private LightningPrefabEntry GetRandomPlayableEntry()
    {
        List<LightningPrefabEntry> playableEntries = GetPlayableEntries();
        if (playableEntries.Count == 0)
            return null;

        int randomIndex = UnityEngine.Random.Range(0, playableEntries.Count);
        return playableEntries[randomIndex];
    }

    private int GetTotalStrikeCount(List<LightningPrefabEntry> playableEntries)
    {
        int totalStrikes = 0;

        foreach (LightningPrefabEntry entry in playableEntries)
            totalStrikes += entry.strikeCount;

        return totalStrikes;
    }

    private float GetDelayBeforeNextStrike(LightningPrefabEntry entry)
    {
        float minDelay = minTimeBetweenStrikes;
        float maxDelay = maxTimeBetweenStrikes;

        if (entry.useCustomDelayRange)
        {
            minDelay = entry.minDelayBeforeNextStrike;
            maxDelay = entry.maxDelayBeforeNextStrike;
        }

        if (maxDelay < minDelay)
            maxDelay = minDelay;

        return UnityEngine.Random.Range(minDelay, maxDelay);
    }

    private string GetEntryName(LightningPrefabEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.displayName))
            return entry.displayName;

        if (entry.lightningPrefab != null)
            return entry.lightningPrefab.name;

        return "Lightning Strike";
    }
}
