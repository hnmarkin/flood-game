using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class FloodImpactOverlayManager : MonoBehaviour
{
    private const string LiveFloodButtonName = "action_Button4";

    [Header("References")]
    [SerializeField] private FloodImpactController floodImpactController;
    [SerializeField] private ZoneThinOutlineByHover zoneOutlineController;
    [SerializeField] private UIDocument actionsUIDocument;
    [SerializeField] private HighRiskManager highRiskManager;
    [SerializeField] private GameObject labelTemplatePrefab;
    [SerializeField] private Transform labelParent;

    [Header("Label Settings")]
    [SerializeField] private bool showLabelsForAllZones = true;
    [SerializeField, Min(1)] private int maxLabelsToShow = 50;
    [SerializeField] private float riskLabelHeightOffset = 0.95f;
    [SerializeField] private float damageLabelHeightOffset = 1.35f;
    [SerializeField] private float labelFontSize = 4f;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private int labelSortingOrder = 2200;

    [Header("Behavior")]
    [SerializeField] private bool refreshVisibleOverlayOnImpactRefresh = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<GameObject> _spawnedRiskLabels = new List<GameObject>();
    private readonly List<GameObject> _spawnedDamageLabels = new List<GameObject>();

    private Button _liveFloodButton;
    private Coroutine _bindButtonRoutine;
    private bool _isBound;
    private bool _isOverlayVisible;
    private TMP_Text _labelTemplate;

    public bool IsLiveFloodOverlayVisible => _isOverlayVisible;

    private void Awake()
    {
        if (floodImpactController == null)
            floodImpactController = FindFirstObjectByType<FloodImpactController>();

        if (zoneOutlineController == null)
            zoneOutlineController = FindFirstObjectByType<ZoneThinOutlineByHover>();

        if (actionsUIDocument == null)
            actionsUIDocument = GetComponent<UIDocument>();

        if (highRiskManager == null)
            highRiskManager = FindFirstObjectByType<HighRiskManager>();

        if (labelParent == null)
            labelParent = transform;
    }

    private void OnEnable()
    {
        TryBindLiveFloodButton();

        if (!_isBound && _bindButtonRoutine == null)
            _bindButtonRoutine = StartCoroutine(BindButtonWhenReady());

        if (floodImpactController != null)
            floodImpactController.FloodImpactRefreshed += OnFloodImpactRefreshed;
    }

    private void OnDisable()
    {
        if (floodImpactController != null)
            floodImpactController.FloodImpactRefreshed -= OnFloodImpactRefreshed;

        if (_bindButtonRoutine != null)
        {
            StopCoroutine(_bindButtonRoutine);
            _bindButtonRoutine = null;
        }

        UnbindLiveFloodButton();
        HideLiveFloodOverlay();
    }

    private void OnValidate()
    {
        maxLabelsToShow = Mathf.Max(1, maxLabelsToShow);
        labelFontSize = Mathf.Max(0.1f, labelFontSize);
    }

    public void ToggleLiveFloodOverlay()
    {
        if (_isOverlayVisible)
        {
            HideLiveFloodOverlay();
            return;
        }

        ShowLiveFloodOverlay();
    }

    public void ToggleZoneDamagePopup()
    {
        ToggleLiveFloodOverlay();
    }

    public void ShowLiveFloodOverlay()
    {
        if (!ValidateReferences())
            return;

        if (highRiskManager != null && highRiskManager.IsBaselineRiskOverlayVisible)
            highRiskManager.HideBaselineRiskOverlay();

        floodImpactController.RefreshFloodImpact();
        RenderLiveFloodOverlay(floodImpactController.GetAllFloodImpactResults());
    }

    public void HideLiveFloodOverlay()
    {
        ClearLiveFloodLabels();
        _isOverlayVisible = false;

        if (zoneOutlineController != null)
            zoneOutlineController.ClearLiveFloodRiskOutlines();
    }

    private IEnumerator BindButtonWhenReady()
    {
        const int maxFramesToWait = 30;
        int waitedFrames = 0;

        while (!_isBound && waitedFrames < maxFramesToWait)
        {
            TryBindLiveFloodButton();

            if (_isBound)
                break;

            waitedFrames++;
            yield return null;
        }

        if (!_isBound)
            Debug.LogWarning("[FloodImpactOverlayManager] Could not bind action_Button4 from the assigned UIDocument.");

        _bindButtonRoutine = null;
    }

    private void TryBindLiveFloodButton()
    {
        if (_isBound || actionsUIDocument == null)
            return;

        VisualElement root = actionsUIDocument.rootVisualElement;
        if (root == null)
            return;

        _liveFloodButton = root.Q<Button>(LiveFloodButtonName);
        if (_liveFloodButton == null)
            return;

        _liveFloodButton.clicked += OnLiveFloodButtonClicked;
        _isBound = true;
    }

    private void UnbindLiveFloodButton()
    {
        if (!_isBound || _liveFloodButton == null)
            return;

        _liveFloodButton.clicked -= OnLiveFloodButtonClicked;
        _liveFloodButton = null;
        _isBound = false;
    }

    private void OnLiveFloodButtonClicked()
    {
        ToggleLiveFloodOverlay();
    }

    private void OnFloodImpactRefreshed()
    {
        if (!_isOverlayVisible || !refreshVisibleOverlayOnImpactRefresh)
            return;

        RenderLiveFloodOverlay(floodImpactController.GetAllFloodImpactResults());
    }

    private void RenderLiveFloodOverlay(IReadOnlyList<ZoneFloodImpactResult> impactResults)
    {
        ClearLiveFloodLabels();

        List<ZoneRiskOutlineRequest> outlineRequests = new List<ZoneRiskOutlineRequest>();

        if (impactResults != null)
        {
            int labelLimit = showLabelsForAllZones ? impactResults.Count : Mathf.Min(maxLabelsToShow, impactResults.Count);

            for (int i = 0; i < impactResults.Count; i++)
            {
                ZoneFloodImpactResult result = impactResults[i];

                if (string.IsNullOrWhiteSpace(result.geoid))
                    continue;

                Color outlineColor = highRiskManager != null
                    ? highRiskManager.GetRiskColorForLevel(result.riskLevel)
                    : Color.white;

                outlineRequests.Add(new ZoneRiskOutlineRequest(result.geoid, outlineColor));

                if (i < labelLimit)
                {
                    SpawnWorldLabel(
                        result,
                        BuildRiskLabel(result),
                        riskLabelHeightOffset,
                        _spawnedRiskLabels);

                    SpawnWorldLabel(
                        result,
                        BuildDamageLabel(result),
                        damageLabelHeightOffset,
                        _spawnedDamageLabels);
                }
            }
        }

        zoneOutlineController.ShowLiveFloodRiskOutlines(outlineRequests);
        _isOverlayVisible = outlineRequests.Count > 0;

        if (debugLogs)
            Debug.Log($"[FloodImpactOverlayManager] Rendered live flood overlay for {outlineRequests.Count} zones.");
    }

    private void SpawnWorldLabel(
        ZoneFloodImpactResult result,
        string text,
        float heightOffset,
        List<GameObject> targetList)
    {
        if (!TryGetLabelPosition(result, heightOffset, out Vector3 labelPosition))
            return;

        Transform parent = labelParent != null ? labelParent : transform;
        GameObject labelObject = new GameObject($"FloodImpactLabel_{result.geoid}_{targetList.Count}");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = labelPosition;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            labelObject.transform.rotation = mainCamera.transform.rotation;

        TextMeshPro textLabel = labelObject.AddComponent<TextMeshPro>();
        ApplyLabelStyle(textLabel, text);
        targetList.Add(labelObject);
    }

    private bool TryGetLabelPosition(ZoneFloodImpactResult result, float heightOffset, out Vector3 labelPosition)
    {
        labelPosition = Vector3.zero;

        if (!result.hasWorldCenter)
            return false;

        labelPosition = result.worldCenter + new Vector3(0f, heightOffset, 0f);

        Camera mainCamera = Camera.main;
        if (mainCamera != null && labelPosition.z <= mainCamera.transform.position.z)
            labelPosition.z = mainCamera.transform.position.z + 1f;

        return true;
    }

    private void ApplyLabelStyle(TextMeshPro textLabel, string text)
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
        textLabel.color = labelColor;
        textLabel.text = text;

        Renderer textRenderer = textLabel.GetComponent<Renderer>();
        if (textRenderer != null)
            textRenderer.sortingOrder = labelSortingOrder;
    }

    private TMP_Text GetLabelTemplate()
    {
        if (_labelTemplate == null && labelTemplatePrefab != null)
            _labelTemplate = labelTemplatePrefab.GetComponentInChildren<TMP_Text>(true);

        return _labelTemplate;
    }

    private string BuildRiskLabel(ZoneFloodImpactResult result)
    {
        int riskPercent = Mathf.RoundToInt(Mathf.Clamp01(result.liveFloodRisk) * 100f);
        return $"Risk: {riskPercent}%";
    }

    private string BuildDamageLabel(ZoneFloodImpactResult result)
    {
        return $"Damage: {FormatCurrency(result.estimatedDamage)}";
    }

    private string FormatCurrency(float value)
    {
        float clampedValue = Mathf.Max(0f, value);

        if (clampedValue >= 1000000f)
            return $"${clampedValue / 1000000f:0.#}M";

        if (clampedValue >= 1000f)
            return $"${clampedValue / 1000f:0.#}k";

        return $"${clampedValue:0}";
    }

    private void ClearLiveFloodLabels()
    {
        DestroySpawnedObjects(_spawnedRiskLabels);
        DestroySpawnedObjects(_spawnedDamageLabels);
    }

    private void DestroySpawnedObjects(List<GameObject> objects)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] != null)
                Destroy(objects[i]);
        }

        objects.Clear();
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (floodImpactController == null)
        {
            Debug.LogError("[FloodImpactOverlayManager] FloodImpactController is not assigned.");
            isValid = false;
        }

        if (zoneOutlineController == null)
        {
            Debug.LogError("[FloodImpactOverlayManager] ZoneThinOutlineByHover is not assigned.");
            isValid = false;
        }

        return isValid;
    }
}
