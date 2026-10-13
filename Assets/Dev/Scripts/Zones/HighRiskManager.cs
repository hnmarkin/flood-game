using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class HighRiskManager : MonoBehaviour
{
    public event Action BaselineRiskInspectionShown;

    private const string InspectZoneButtonName = "action_Button3";

    [Header("References")]
    [SerializeField] private ZoneBaselineRiskController baselineRiskController;
    [SerializeField] private ZoneThinOutlineByHover zoneOutlineController;
    [SerializeField] private UIDocument actionsUIDocument;
    [SerializeField] private GameObject riskWarningMarkerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private FloodImpactOverlayManager liveFloodOverlayManager;

    [Header("Display Settings")]
    [SerializeField, Min(1)] private int maxRiskZonesToShow = 3;
    [SerializeField] private float markerHeightOffset = 1.25f;
    [SerializeField] private bool showWarningMarkersForTopZones;
    [SerializeField] private bool showLabelsForAllZones = true;
    [SerializeField, Min(1)] private int maxLabelsToShow = 50;
    [SerializeField] private bool includeRiskLevelInLabels;
    [SerializeField] private float labelHeightOffset = 0.95f;
    [SerializeField] private float labelFontSize = 4f;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private int labelSortingOrder = 2100;
    [SerializeField] private bool clearOnSecondClick;
    [SerializeField] private int markerSortingOrder = 2000;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<GameObject> _spawnedMarkers = new();
    private readonly List<GameObject> _spawnedLabels = new();

    private Button _inspectZoneButton;
    private Coroutine _bindButtonRoutine;
    private bool _isBound;
    private bool _isShowingRiskZones;
    private TMP_Text _labelTemplate;

    public bool IsBaselineRiskOverlayVisible => _isShowingRiskZones;

    private void Awake()
    {
        if (baselineRiskController == null)
            baselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (zoneOutlineController == null)
            zoneOutlineController = FindFirstObjectByType<ZoneThinOutlineByHover>();

        if (actionsUIDocument == null)
            actionsUIDocument = GetComponent<UIDocument>();

        if (markerParent == null)
            markerParent = transform;

        if (liveFloodOverlayManager == null)
            liveFloodOverlayManager = FindFirstObjectByType<FloodImpactOverlayManager>();
    }

    private void OnEnable()
    {
        TryBindInspectButton();

        if (!_isBound && _bindButtonRoutine == null)
            _bindButtonRoutine = StartCoroutine(BindInspectButtonWhenReady());
    }

    private void OnDisable()
    {
        if (_bindButtonRoutine != null)
        {
            StopCoroutine(_bindButtonRoutine);
            _bindButtonRoutine = null;
        }

        UnbindInspectButton();
        ClearShownRiskZones();
    }

    private void OnValidate()
    {
        maxRiskZonesToShow = Mathf.Max(1, maxRiskZonesToShow);
        maxLabelsToShow = Mathf.Max(1, maxLabelsToShow);
        labelFontSize = Mathf.Max(0.1f, labelFontSize);
    }

    public void ShowTopBaselineRiskZones()
    {
        ShowBaselineRiskOverlay();
    }

    public void ToggleBaselineRiskOverlay()
    {
        if (_isShowingRiskZones)
        {
            HideBaselineRiskOverlay();
            return;
        }

        ShowBaselineRiskOverlay();
    }

    public void ShowBaselineRiskOverlay()
    {
        if (!ValidateReferences())
            return;

        if (liveFloodOverlayManager != null && liveFloodOverlayManager.IsLiveFloodOverlayVisible)
            liveFloodOverlayManager.HideLiveFloodOverlay();

        ClearShownRiskZones();

        if (!baselineRiskController.EnsureBaselineRiskCalculated())
        {
            Debug.LogWarning("[HighRiskManager] Baseline risk is not ready yet, so no risk markers were shown.");
            return;
        }

        IReadOnlyList<ZoneBaselineRiskData> allRiskZones = baselineRiskController.GetAllRiskResults();

        if (allRiskZones == null || allRiskZones.Count == 0)
        {
            Debug.LogWarning("[HighRiskManager] Baseline risk did not return any zones to display.");
            return;
        }

        ShowRiskOutlines(allRiskZones);
        SpawnRiskLabels(allRiskZones);

        if (showWarningMarkersForTopZones)
            SpawnTopRiskMarkers();

        _isShowingRiskZones = true;
        BaselineRiskInspectionShown?.Invoke();

        if (debugLogs)
            Debug.Log($"[HighRiskManager] Showing baseline risk overlay for {allRiskZones.Count} zones.");
    }

    public void ClearShownRiskZones()
    {
        for (int i = 0; i < _spawnedMarkers.Count; i++)
        {
            if (_spawnedMarkers[i] != null)
                Destroy(_spawnedMarkers[i]);
        }

        _spawnedMarkers.Clear();

        for (int i = 0; i < _spawnedLabels.Count; i++)
        {
            if (_spawnedLabels[i] != null)
                Destroy(_spawnedLabels[i]);
        }

        _spawnedLabels.Clear();
        _isShowingRiskZones = false;

        if (zoneOutlineController != null)
            zoneOutlineController.ClearBaselineRiskOutlines();
    }

    private IEnumerator BindInspectButtonWhenReady()
    {
        const int maxFramesToWait = 30;
        int framesWaited = 0;

        while (!_isBound && framesWaited < maxFramesToWait)
        {
            TryBindInspectButton();

            if (_isBound)
                break;

            framesWaited++;
            yield return null;
        }

        if (!_isBound)
            Debug.LogWarning("[HighRiskManager] Could not bind action_Button3 from the assigned UIDocument.");

        _bindButtonRoutine = null;
    }

    private void TryBindInspectButton()
    {
        if (_isBound || actionsUIDocument == null)
            return;

        VisualElement root = actionsUIDocument.rootVisualElement;

        if (root == null)
            return;

        _inspectZoneButton = root.Q<Button>(InspectZoneButtonName);

        if (_inspectZoneButton == null)
            return;

        _inspectZoneButton.clicked += OnInspectZoneClicked;
        _isBound = true;
    }

    private void UnbindInspectButton()
    {
        if (!_isBound || _inspectZoneButton == null)
            return;

        _inspectZoneButton.clicked -= OnInspectZoneClicked;
        _inspectZoneButton = null;
        _isBound = false;
    }

    private void OnInspectZoneClicked()
    {
        ToggleBaselineRiskOverlay();
    }

    private void ShowRiskOutlines(IReadOnlyList<ZoneBaselineRiskData> riskResults)
    {
        List<ZoneRiskOutlineRequest> outlineRequests = new();

        if (riskResults != null)
        {
            for (int i = 0; i < riskResults.Count; i++)
            {
                ZoneBaselineRiskData riskData = riskResults[i];

                if (string.IsNullOrWhiteSpace(riskData.geoid))
                    continue;

                Color outlineColor = baselineRiskController.GetRiskColorForLevel(riskData.riskLevel);
                outlineRequests.Add(new ZoneRiskOutlineRequest(riskData.geoid, outlineColor));
            }
        }

        if (outlineRequests.Count == 0)
        {
            Debug.LogWarning("[HighRiskManager] No valid GEOIDs were available for the baseline risk overlay.");
            return;
        }

        zoneOutlineController.ShowBaselineRiskOutlines(outlineRequests);
    }

    private void SpawnRiskLabels(IReadOnlyList<ZoneBaselineRiskData> riskResults)
    {
        if (riskResults == null)
            return;

        int labelLimit = showLabelsForAllZones ? riskResults.Count : Mathf.Min(maxLabelsToShow, riskResults.Count);

        for (int i = 0; i < labelLimit; i++)
            SpawnRiskLabel(riskResults[i]);
    }

    private void SpawnTopRiskMarkers()
    {
        if (riskWarningMarkerPrefab == null)
        {
            Debug.LogWarning("[HighRiskManager] Warning markers are enabled, but no risk warning marker prefab is assigned.");
            return;
        }

        List<ZoneBaselineRiskData> topRiskZones = baselineRiskController.GetTopRiskZones(maxRiskZonesToShow);

        for (int i = 0; i < topRiskZones.Count; i++)
            SpawnRiskMarker(topRiskZones[i]);
    }

    private void SpawnRiskMarker(ZoneBaselineRiskData riskData)
    {
        if (!TryGetZoneCenter(riskData, out Vector3 zoneCenter))
        {
            Debug.LogWarning($"[HighRiskManager] Could not resolve a zone center for GEOID '{riskData.geoid}'.");
            return;
        }

        Transform parent = markerParent != null ? markerParent : transform;
        Vector3 spawnPosition = zoneCenter + new Vector3(0f, markerHeightOffset, 0f);

        Camera mainCamera = Camera.main;

        if (mainCamera != null && spawnPosition.z <= mainCamera.transform.position.z)
            spawnPosition.z = mainCamera.transform.position.z + 1f;

        GameObject markerInstance = Instantiate(riskWarningMarkerPrefab, spawnPosition, Quaternion.identity, parent);
        markerInstance.name = $"RiskWarningMarker_{riskData.geoid}";
        markerInstance.SetActive(true);

        if (markerInstance.transform.localScale == Vector3.zero)
            markerInstance.transform.localScale = Vector3.one;

        ApplyMarkerRendererSettings(markerInstance);
        ApplyMarkerLabel(markerInstance, riskData);

        _spawnedMarkers.Add(markerInstance);
    }

    private void ApplyMarkerRendererSettings(GameObject markerInstance)
    {
        Renderer[] renderers = markerInstance.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            renderer.enabled = true;
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, markerSortingOrder);
        }

        SpriteRenderer[] spriteRenderers = markerInstance.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].enabled = true;
        }
    }

    private void ApplyMarkerLabel(GameObject markerInstance, ZoneBaselineRiskData riskData)
    {
        TMP_Text label = markerInstance.GetComponentInChildren<TMP_Text>(true);

        if (label == null)
        {
            Debug.LogWarning($"[HighRiskManager] Spawned risk marker for GEOID '{riskData.geoid}' is missing a TMP label.");
            return;
        }

        label.gameObject.SetActive(true);
        label.text = BuildMarkerLabel(riskData);
    }

    private void SpawnRiskLabel(ZoneBaselineRiskData riskData)
    {
        if (!TryGetZoneCenter(riskData, out Vector3 zoneCenter))
            return;

        Transform parent = markerParent != null ? markerParent : transform;
        GameObject labelObject = new GameObject($"RiskPercentLabel_{riskData.geoid}");
        labelObject.transform.SetParent(parent, false);
        Vector3 labelPosition = zoneCenter + new Vector3(0f, labelHeightOffset, 0f);

        Camera mainCamera = Camera.main;

        if (mainCamera != null && labelPosition.z <= mainCamera.transform.position.z)
            labelPosition.z = mainCamera.transform.position.z + 1f;

        labelObject.transform.position = labelPosition;

        if (mainCamera != null)
            labelObject.transform.rotation = mainCamera.transform.rotation;

        TextMeshPro textLabel = labelObject.AddComponent<TextMeshPro>();
        ApplyOverlayLabelStyle(textLabel, riskData);

        _spawnedLabels.Add(labelObject);
    }

    private void ApplyOverlayLabelStyle(TextMeshPro textLabel, ZoneBaselineRiskData riskData)
    {
        TMP_Text templateLabel = GetLabelTemplate();

        if (templateLabel != null)
        {
            textLabel.font = templateLabel.font;
            textLabel.fontSharedMaterial = templateLabel.fontSharedMaterial;
            textLabel.fontStyle = templateLabel.fontStyle;
        }

        textLabel.alignment = TextAlignmentOptions.Center;
        textLabel.fontSize = labelFontSize;
        textLabel.enableWordWrapping = false;
        textLabel.overflowMode = TextOverflowModes.Overflow;
        textLabel.text = BuildOverlayLabel(riskData);
        textLabel.color = labelColor;

        Renderer textRenderer = textLabel.GetComponent<Renderer>();

        if (textRenderer != null)
            textRenderer.sortingOrder = labelSortingOrder;
    }

    private TMP_Text GetLabelTemplate()
    {
        if (_labelTemplate == null && riskWarningMarkerPrefab != null)
            _labelTemplate = riskWarningMarkerPrefab.GetComponentInChildren<TMP_Text>(true);

        return _labelTemplate;
    }

    private string BuildMarkerLabel(ZoneBaselineRiskData riskData)
    {
        string riskName = riskData.riskLevel.ToString().ToUpperInvariant();
        int riskPercent = Mathf.RoundToInt(Mathf.Clamp01(riskData.baselineRiskScore) * 100f);
        return $"{riskName}\n{riskPercent}%";
    }

    private string BuildOverlayLabel(ZoneBaselineRiskData riskData)
    {
        int riskPercent = Mathf.RoundToInt(Mathf.Clamp01(riskData.baselineRiskScore) * 100f);

        if (includeRiskLevelInLabels)
            return $"{riskData.riskLevel.ToString().ToUpperInvariant()} {riskPercent}%";

        return $"{riskPercent}%";
    }

    private bool TryGetZoneCenter(ZoneBaselineRiskData riskData, out Vector3 zoneCenter)
    {
        if (riskData.hasWorldCenter)
        {
            zoneCenter = riskData.worldCenter;
            return true;
        }

        return baselineRiskController.TryGetZoneCenterWorld(riskData.geoid, out zoneCenter);
    }

    public void HideBaselineRiskOverlay()
    {
        ClearShownRiskZones();
    }

    public Color GetRiskColorForLevel(RiskLevel riskLevel)
    {
        return baselineRiskController != null
            ? baselineRiskController.GetRiskColorForLevel(riskLevel)
            : Color.white;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (baselineRiskController == null)
        {
            Debug.LogError("[HighRiskManager] ZoneBaselineRiskController is not assigned.");
            isValid = false;
        }

        if (zoneOutlineController == null)
        {
            Debug.LogError("[HighRiskManager] ZoneThinOutlineByHover is not assigned.");
            isValid = false;
        }

        return isValid;
    }
}
