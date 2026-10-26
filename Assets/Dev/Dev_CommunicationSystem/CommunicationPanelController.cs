using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CommunicationPanelController : MonoBehaviour
{
    private const string PanelRootName = "com_panel_root";
    private const string FullPanelName = "com_full_panel_container";
    private const string MinimizeButtonName = "com_panel_minimize_btn";
    private const string CollapsedPanelName = "com_panel_collapsed";
    private const string CollapsedButtonName = "com_panel_collapsed_btn";
    private const string ZoneTabContainerName = "zone_tab";
    private const string ZoneTabTemplateName = "zone_tab_1";
    private const string SendWarningButtonName = "send_warning_btn";
    private const string SelectedZonesLabelName = "selected_zones_label";
    private const string SelectedPopulationLabelName = "selected_population_count_label";

    private const string ZoneIdLabelName = "zone_id_label";
    private const string PopulationLabelName = "population_count_label";
    private const string ShelterIdLabelName = "shelter_id_label";
    private const string ShelterCapacityLabelName = "shelter_capacity_label";
    private const string EvacStatusIconName = "evac_status_icon";
    private const string EvacStatusLabelName = "evac_status_label";
    private const string CommunicationStatusIconName = "com_status_icon";
    private const string CommunicationStatusLabelName = "com_status_label";
    private const string WarningButtonName = "warn_box_btn";
    private const string RiskIconName = "risk_icon";

    private sealed class ZoneTabBinding
    {
        public VisualElement root;
        public Label zoneIdLabel;
        public Label populationLabel;
        public Label shelterIdLabel;
        public Label shelterCapacityLabel;
        public VisualElement evacuationStatusIcon;
        public Label evacuationStatusLabel;
        public VisualElement communicationStatusIcon;
        public Label communicationStatusLabel;
        public Button warningButton;
        public VisualElement riskIcon;
        public Action warningClickHandler;
        public string geoid;
        public int population;
    }

    [Header("References")]
    [SerializeField] private UIDocument communicationUIDocument;
    [SerializeField] private VisualTreeAsset communicationPanelTemplate;
    [SerializeField] private CommunicationController communicationTowerController;
    [SerializeField] private ShelterController shelterController;
    [SerializeField] private EvacuationController evacuationController;
    [SerializeField] private ZoneBaselineRiskController zoneBaselineRiskController;

    [Header("Status Icons")]
    [SerializeField] private Sprite evacuationMissingIcon;
    [SerializeField] private Sprite evacuationAvailableIcon;
    [SerializeField] private Sprite communicationPendingIcon;
    [SerializeField] private Sprite communicationCompletedIcon;
    [SerializeField] private Sprite warningUncheckedIcon;
    [SerializeField] private Sprite warningCheckedIcon;
    [SerializeField] private Sprite riskCriticalIcon;
    [SerializeField] private Sprite riskMediumIcon;
    [SerializeField] private Sprite riskLowIcon;

    [Header("Send Warning Button")]
    [SerializeField] private Sprite sendWarningNormalSprite;
    [SerializeField] private Sprite sendWarningHoverSprite;
    [SerializeField] private Sprite sendWarningSelectedSprite;
    [SerializeField, Min(0f)] private float sendWarningSuccessSeconds = 0.35f;
    [SerializeField, Min(1f)] private float sendWarningHoverScale = 1.04f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<ZoneTabBinding> _zoneTabs = new();
    private readonly HashSet<string> _selectedZoneGeoids = new(StringComparer.Ordinal);
    private readonly HashSet<string> _missingSpriteWarnings = new(StringComparer.Ordinal);

    private VisualElement _root;
    private VisualElement _panelRoot;
    private VisualElement _fullPanel;
    private VisualElement _collapsedPanel;
    private VisualElement _zoneTabContainer;
    private Button _minimizeButton;
    private Button _collapsedButton;
    private Button _sendWarningButton;
    private Label _selectedZonesLabel;
    private Label _selectedPopulationLabel;
    private CommunicationTowerData _currentTower;
    private Coroutine _bindRoutine;
    private Coroutine _sendWarningSuccessRoutine;
    private bool _isBound;
    private bool _isSendWarningSuccessState;

    private void Reset()
    {
        communicationUIDocument = GetComponent<UIDocument>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (!TryBindUI(false) && _bindRoutine == null)
            _bindRoutine = StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        if (_sendWarningSuccessRoutine != null)
        {
            StopCoroutine(_sendWarningSuccessRoutine);
            _sendWarningSuccessRoutine = null;
        }

        UnbindCallbacks();
        _isBound = false;
    }

    public void ShowForTower(CommunicationTowerData tower)
    {
        if (tower == null)
        {
            Debug.LogWarning("[CommunicationPanelController] Cannot show communication panel because tower data is null.");
            return;
        }

        if (!_isBound && !TryBindUI(true))
            return;

        _currentTower = tower;
        ClearSelection();

        SetPanelRootVisible(true);

        if (_fullPanel != null)
            _fullPanel.style.display = DisplayStyle.Flex;

        if (_collapsedPanel != null)
            _collapsedPanel.style.display = DisplayStyle.None;

        if (debugLogs)
        {
            Debug.Log($"[CommunicationPanelController] Showing panel for tower {GetTowerId(tower)}.");
            Debug.Log($"[CommunicationPanelController] Tower {GetTowerId(tower)} serves {GetTowerZoneCount(tower)} zones.");
        }

        RefreshPanel();
    }

    public void HidePanel()
    {
        SetPanelRootVisible(false);

        if (_fullPanel != null)
            _fullPanel.style.display = DisplayStyle.None;

        if (_collapsedPanel != null)
            _collapsedPanel.style.display = DisplayStyle.None;

        if (debugLogs)
            Debug.Log("[CommunicationPanelController] Communication panel hidden.");
    }

    public void MinimizePanel()
    {
        SetPanelRootVisible(true);

        if (_fullPanel != null)
            _fullPanel.style.display = DisplayStyle.None;

        if (_collapsedPanel != null)
            _collapsedPanel.style.display = DisplayStyle.Flex;

        if (debugLogs)
            Debug.Log("[CommunicationPanelController] Communication panel minimized.");
    }

    public void RestorePanel()
    {
        SetPanelRootVisible(true);

        if (_fullPanel != null)
            _fullPanel.style.display = DisplayStyle.Flex;

        if (_collapsedPanel != null)
            _collapsedPanel.style.display = DisplayStyle.None;

        if (debugLogs)
            Debug.Log("[CommunicationPanelController] Communication panel restored.");
    }

    private void SetPanelRootVisible(bool isVisible)
    {
        if (_panelRoot != null)
            _panelRoot.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void RefreshPanel()
    {
        if (!_isBound && !TryBindUI(true))
            return;

        EnsureZoneTabCapacity(GetTowerZoneCount(_currentTower));
        PopulateZoneTabs();
        UpdateSelectedSummary();
        UpdateSendWarningButtonSprite();
    }

    public void ClearSelection()
    {
        _selectedZoneGeoids.Clear();
        UpdateSelectedSummary();
    }

    private IEnumerator BindWhenReady()
    {
        const int maxFramesToWait = 30;
        int waitedFrames = 0;

        while (!_isBound && waitedFrames < maxFramesToWait)
        {
            if (TryBindUI(false))
                break;

            waitedFrames++;
            yield return null;
        }

        if (!_isBound)
            TryBindUI(true);

        _bindRoutine = null;
    }

    private bool TryBindUI(bool logWarnings)
    {
        if (_isBound)
            return true;

        ResolveReferences();

        if (communicationUIDocument == null)
        {
            if (logWarnings)
                Debug.LogWarning("[CommunicationPanelController] Communication UIDocument is not assigned.");

            return false;
        }

        _root = communicationUIDocument.rootVisualElement;

        if (_root == null)
            return false;

        _panelRoot = _root.Q<VisualElement>(PanelRootName);

        if (_panelRoot == null && communicationPanelTemplate != null)
        {
            TemplateContainer clone = communicationPanelTemplate.CloneTree();
            _root.Add(clone);
            _panelRoot = clone.Q<VisualElement>(PanelRootName);
        }

        _fullPanel = _root.Q<VisualElement>(FullPanelName);
        _collapsedPanel = _root.Q<VisualElement>(CollapsedPanelName);
        _minimizeButton = _root.Q<Button>(MinimizeButtonName);
        _collapsedButton = _root.Q<Button>(CollapsedButtonName);
        _zoneTabContainer = _root.Q<VisualElement>(ZoneTabContainerName);
        _sendWarningButton = _root.Q<Button>(SendWarningButtonName);
        _selectedZonesLabel = _root.Q<Label>(SelectedZonesLabelName);
        _selectedPopulationLabel = _root.Q<Label>(SelectedPopulationLabelName);

        bool isValid =
            _panelRoot != null &&
            _fullPanel != null &&
            _collapsedPanel != null &&
            _minimizeButton != null &&
            _collapsedButton != null &&
            _zoneTabContainer != null &&
            _sendWarningButton != null;

        if (!isValid)
        {
            if (logWarnings)
                LogMissingElementWarnings();

            return false;
        }

        RegisterCallbacks();
        RebuildExistingZoneTabBindings();
        _isBound = true;
        _isSendWarningSuccessState = false;

        HidePanel();
        UpdateSelectedSummary();
        UpdateSendWarningButtonSprite();

        if (debugLogs)
            Debug.Log("[CommunicationPanelController] Communication panel initialized.");

        return true;
    }

    private void RegisterCallbacks()
    {
        _minimizeButton.clicked -= MinimizePanel;
        _minimizeButton.clicked += MinimizePanel;

        _collapsedButton.clicked -= RestorePanel;
        _collapsedButton.clicked += RestorePanel;

        _sendWarningButton.clicked -= OnSendWarningClicked;
        _sendWarningButton.clicked += OnSendWarningClicked;

        _sendWarningButton.UnregisterCallback<PointerEnterEvent>(OnSendWarningPointerEnter);
        _sendWarningButton.RegisterCallback<PointerEnterEvent>(OnSendWarningPointerEnter);

        _sendWarningButton.UnregisterCallback<PointerLeaveEvent>(OnSendWarningPointerLeave);
        _sendWarningButton.RegisterCallback<PointerLeaveEvent>(OnSendWarningPointerLeave);
    }

    private void UnbindCallbacks()
    {
        if (_minimizeButton != null)
            _minimizeButton.clicked -= MinimizePanel;

        if (_collapsedButton != null)
            _collapsedButton.clicked -= RestorePanel;

        if (_sendWarningButton != null)
        {
            _sendWarningButton.clicked -= OnSendWarningClicked;
            _sendWarningButton.UnregisterCallback<PointerEnterEvent>(OnSendWarningPointerEnter);
            _sendWarningButton.UnregisterCallback<PointerLeaveEvent>(OnSendWarningPointerLeave);
            _sendWarningButton.transform.scale = Vector3.one;
        }

        for (int i = 0; i < _zoneTabs.Count; i++)
            UnbindZoneTabWarningButton(_zoneTabs[i]);
    }

    private void RebuildExistingZoneTabBindings()
    {
        _zoneTabs.Clear();

        if (_zoneTabContainer == null)
            return;

        foreach (VisualElement child in _zoneTabContainer.Children())
        {
            if (child == null || !IsZoneTabElement(child))
                continue;

            _zoneTabs.Add(CreateZoneTabBinding(child));
        }
    }

    private void EnsureZoneTabCapacity(int requiredCount)
    {
        if (_zoneTabContainer == null || requiredCount <= _zoneTabs.Count)
            return;

        while (_zoneTabs.Count < requiredCount)
        {
            VisualElement clone = CreateZoneTabClone(_zoneTabs.Count + 1);

            if (clone == null)
            {
                Debug.LogWarning(
                    "[CommunicationPanelController] Could not clone zone_tab_1 for additional communication zones. " +
                    "Assign Communication Panel Template to support towers with more zones than the existing tabs.");
                return;
            }

            _zoneTabContainer.Add(clone);
            _zoneTabs.Add(CreateZoneTabBinding(clone));
        }
    }

    private VisualElement CreateZoneTabClone(int tabNumber)
    {
        if (communicationPanelTemplate == null)
            return null;

        TemplateContainer panelClone = communicationPanelTemplate.CloneTree();
        VisualElement clonedTab = panelClone.Q<VisualElement>(ZoneTabTemplateName);

        if (clonedTab == null)
            return null;

        clonedTab.RemoveFromHierarchy();
        clonedTab.name = $"zone_tab_{tabNumber}";

        VisualElement innerTab = clonedTab.Q<VisualElement>("tab_1");

        if (innerTab != null)
            innerTab.name = $"tab_{tabNumber}";

        return clonedTab;
    }

    private ZoneTabBinding CreateZoneTabBinding(VisualElement tabRoot)
    {
        ZoneTabBinding binding = new()
        {
            root = tabRoot,
            zoneIdLabel = tabRoot.Q<Label>(ZoneIdLabelName),
            populationLabel = tabRoot.Q<Label>(PopulationLabelName),
            shelterIdLabel = tabRoot.Q<Label>(ShelterIdLabelName),
            shelterCapacityLabel = tabRoot.Q<Label>(ShelterCapacityLabelName),
            evacuationStatusIcon = tabRoot.Q<VisualElement>(EvacStatusIconName),
            evacuationStatusLabel = tabRoot.Q<Label>(EvacStatusLabelName),
            communicationStatusIcon = tabRoot.Q<VisualElement>(CommunicationStatusIconName),
            communicationStatusLabel = tabRoot.Q<Label>(CommunicationStatusLabelName),
            warningButton = tabRoot.Q<Button>(WarningButtonName),
            riskIcon = tabRoot.Q<VisualElement>(RiskIconName),
        };

        LogMissingZoneTabElementWarnings(binding, tabRoot.name);
        return binding;
    }

    private void PopulateZoneTabs()
    {
        IList<string> zones = _currentTower != null ? _currentTower.associatedZoneGeoids : null;
        int zoneCount = zones != null ? zones.Count : 0;

        for (int i = 0; i < _zoneTabs.Count; i++)
        {
            ZoneTabBinding binding = _zoneTabs[i];

            if (binding == null || binding.root == null)
                continue;

            if (i >= zoneCount)
            {
                binding.root.style.display = DisplayStyle.None;
                UnbindZoneTabWarningButton(binding);
                binding.geoid = null;
                binding.population = 0;
                continue;
            }

            string geoid = NormalizeGeoid(zones[i]);

            if (string.IsNullOrEmpty(geoid))
            {
                binding.root.style.display = DisplayStyle.None;
                UnbindZoneTabWarningButton(binding);
                binding.geoid = null;
                binding.population = 0;
                continue;
            }

            binding.root.style.display = DisplayStyle.Flex;
            PopulateZoneTab(binding, geoid);
        }
    }

    private void PopulateZoneTab(ZoneTabBinding binding, string geoid)
    {
        int population = GetPopulationForZone(geoid);
        binding.geoid = geoid;
        binding.population = population;

        if (binding.zoneIdLabel != null)
            binding.zoneIdLabel.text = $"Zone {DemoDisplayIdFormatter.GetZoneDisplayId(geoid)}";

        if (binding.populationLabel != null)
            binding.populationLabel.text = $"Population: {population:N0}";

        if (TryGetActiveShelterForZone(geoid, out PlacedShelterData shelter))
        {
            if (binding.shelterIdLabel != null)
                binding.shelterIdLabel.text = $"Shelter: {DemoDisplayIdFormatter.GetShelterDisplayId(GetSafeShelterId(shelter))}";

            if (binding.shelterCapacityLabel != null)
                binding.shelterCapacityLabel.text = $"Capacity: {Mathf.Max(0, shelter.capacity):N0}";
        }
        else
        {
            if (binding.shelterIdLabel != null)
                binding.shelterIdLabel.text = "Shelter: No Shelter";

            if (binding.shelterCapacityLabel != null)
                binding.shelterCapacityLabel.text = "Capacity: 0";
        }

        UpdateEvacuationStatus(binding, geoid);
        UpdateCommunicationStatus(binding, geoid, out ZoneCommunicationStatus status);
        UpdateRiskIcon(binding, geoid);
        BindZoneWarningButton(binding, status);

        if (debugLogs)
            Debug.Log($"[CommunicationPanelController] Populated zone tab for zone {geoid}.");
    }

    private void UpdateEvacuationStatus(ZoneTabBinding binding, string geoid)
    {
        bool hasSelectedRoute = evacuationController != null && evacuationController.HasSelectedEvacuationRouteForZone(geoid);
        bool hasRoute = hasSelectedRoute ||
                        (evacuationController != null && evacuationController.HasEvacuationRouteForZone(geoid));

        SetBackgroundSprite(
            binding.evacuationStatusIcon,
            hasRoute ? evacuationAvailableIcon : evacuationMissingIcon,
            hasRoute ? nameof(evacuationAvailableIcon) : nameof(evacuationMissingIcon));

        if (binding.evacuationStatusLabel != null)
            binding.evacuationStatusLabel.text = hasRoute ? "Established" : "Missing";
    }

    private void UpdateCommunicationStatus(
        ZoneTabBinding binding,
        string geoid,
        out ZoneCommunicationStatus status)
    {
        status = ZoneCommunicationStatus.Unreached;

        if (communicationTowerController != null)
            communicationTowerController.TryGetCommunicationStatus(geoid, out status);

        Sprite statusSprite;

        switch (status)
        {
            case ZoneCommunicationStatus.Warned:
                statusSprite = communicationCompletedIcon;
                break;

            case ZoneCommunicationStatus.Pending:
                statusSprite = communicationPendingIcon;
                break;

            default:
                // TODO: Use a dedicated unreached communication icon when one exists.
                statusSprite = communicationPendingIcon;
                break;
        }

        SetBackgroundSprite(
            binding.communicationStatusIcon,
            statusSprite,
            status == ZoneCommunicationStatus.Warned ? nameof(communicationCompletedIcon) : nameof(communicationPendingIcon));

        if (binding.communicationStatusLabel != null)
            binding.communicationStatusLabel.text = status.ToString();
    }

    private void UpdateRiskIcon(ZoneTabBinding binding, string geoid)
    {
        Sprite riskSprite = riskMediumIcon;

        if (zoneBaselineRiskController != null)
        {
            if (!zoneBaselineRiskController.HasCalculatedBaselineRisk)
                zoneBaselineRiskController.EnsureBaselineRiskCalculated();

            if (zoneBaselineRiskController.TryGetRiskData(geoid, out ZoneBaselineRiskData riskData))
            {
                switch (riskData.adjustedRiskLevel)
                {
                    case RiskLevel.Critical:
                    case RiskLevel.High:
                        riskSprite = riskCriticalIcon;
                        break;

                    case RiskLevel.Low:
                        riskSprite = riskLowIcon;
                        break;

                    default:
                        riskSprite = riskMediumIcon;
                        break;
                }
            }
        }

        SetBackgroundSprite(binding.riskIcon, riskSprite, nameof(riskMediumIcon));
    }

    private void BindZoneWarningButton(ZoneTabBinding binding, ZoneCommunicationStatus status)
    {
        if (binding.warningButton == null)
            return;

        UnbindZoneTabWarningButton(binding);

        bool isSelected = !string.IsNullOrEmpty(binding.geoid) && _selectedZoneGeoids.Contains(binding.geoid);
        bool canSelect = status == ZoneCommunicationStatus.Pending;

        if (!canSelect && isSelected)
        {
            _selectedZoneGeoids.Remove(binding.geoid);
            isSelected = false;
        }

        binding.warningButton.SetEnabled(canSelect);
        SetBackgroundSprite(
            binding.warningButton,
            isSelected ? warningCheckedIcon : warningUncheckedIcon,
            isSelected ? nameof(warningCheckedIcon) : nameof(warningUncheckedIcon));

        binding.warningClickHandler = () => ToggleZoneSelection(binding);
        binding.warningButton.clicked += binding.warningClickHandler;
    }

    private void UnbindZoneTabWarningButton(ZoneTabBinding binding)
    {
        if (binding == null || binding.warningButton == null || binding.warningClickHandler == null)
            return;

        binding.warningButton.clicked -= binding.warningClickHandler;
        binding.warningClickHandler = null;
    }

    private void ToggleZoneSelection(ZoneTabBinding binding)
    {
        if (binding == null || string.IsNullOrEmpty(binding.geoid))
            return;

        ZoneCommunicationStatus status = ZoneCommunicationStatus.Unreached;

        if (communicationTowerController != null)
            communicationTowerController.TryGetCommunicationStatus(binding.geoid, out status);

        if (status != ZoneCommunicationStatus.Pending)
        {
            if (debugLogs)
                Debug.Log($"[CommunicationPanelController] Zone {binding.geoid} cannot be selected because its status is {status}.");

            return;
        }

        if (_selectedZoneGeoids.Contains(binding.geoid))
        {
            _selectedZoneGeoids.Remove(binding.geoid);
            SetBackgroundSprite(binding.warningButton, warningUncheckedIcon, nameof(warningUncheckedIcon));

            if (debugLogs)
                Debug.Log($"[CommunicationPanelController] Zone {binding.geoid} deselected from warning.");
        }
        else
        {
            _selectedZoneGeoids.Add(binding.geoid);
            SetBackgroundSprite(binding.warningButton, warningCheckedIcon, nameof(warningCheckedIcon));

            if (debugLogs)
                Debug.Log($"[CommunicationPanelController] Zone {binding.geoid} selected for warning.");
        }

        UpdateSelectedSummary();
    }

    private void UpdateSelectedSummary()
    {
        int selectedPopulation = 0;

        for (int i = 0; i < _zoneTabs.Count; i++)
        {
            ZoneTabBinding binding = _zoneTabs[i];

            if (binding == null ||
                string.IsNullOrEmpty(binding.geoid) ||
                !_selectedZoneGeoids.Contains(binding.geoid))
            {
                continue;
            }

            selectedPopulation += Mathf.Max(0, binding.population);
        }

        if (_selectedZonesLabel != null)
            _selectedZonesLabel.text = $"Selected Zones: {_selectedZoneGeoids.Count}";

        if (_selectedPopulationLabel != null)
            _selectedPopulationLabel.text = $"Selected Population: {selectedPopulation:N0}";
    }

    private void OnSendWarningClicked()
    {
        if (communicationTowerController == null)
        {
            Debug.LogWarning("[CommunicationPanelController] Cannot send warnings because CommunicationTowerController is not assigned.");
            return;
        }

        List<string> selectedGeoids = new(_selectedZoneGeoids);

        if (debugLogs)
            Debug.Log($"[CommunicationPanelController] Sending warnings to {selectedGeoids.Count} zone(s).");

        for (int i = 0; i < selectedGeoids.Count; i++)
        {
            string geoid = selectedGeoids[i];

            if (string.IsNullOrEmpty(geoid))
                continue;

            communicationTowerController.MarkZoneWarned(geoid);

            if (debugLogs)
                Debug.Log($"[CommunicationPanelController] Zone {geoid} marked as Warned.");
        }

        ClearSelection();
        RefreshPanel();
        ShowSendWarningSuccessState();
    }

    private void OnSendWarningPointerEnter(PointerEnterEvent evt)
    {
        if (_sendWarningButton == null || _isSendWarningSuccessState)
            return;

        SetBackgroundSprite(_sendWarningButton, sendWarningHoverSprite, nameof(sendWarningHoverSprite));
        _sendWarningButton.transform.scale = Vector3.one * sendWarningHoverScale;
    }

    private void OnSendWarningPointerLeave(PointerLeaveEvent evt)
    {
        if (_sendWarningButton == null)
            return;

        _sendWarningButton.transform.scale = Vector3.one;
        UpdateSendWarningButtonSprite();
    }

    private void ShowSendWarningSuccessState()
    {
        _isSendWarningSuccessState = true;
        UpdateSendWarningButtonSprite();

        if (_sendWarningSuccessRoutine != null)
            StopCoroutine(_sendWarningSuccessRoutine);

        if (sendWarningSuccessSeconds > 0f)
            _sendWarningSuccessRoutine = StartCoroutine(ClearSendWarningSuccessStateAfterDelay());
    }

    private IEnumerator ClearSendWarningSuccessStateAfterDelay()
    {
        yield return new WaitForSeconds(sendWarningSuccessSeconds);
        _isSendWarningSuccessState = false;
        _sendWarningSuccessRoutine = null;
        UpdateSendWarningButtonSprite();
    }

    private void UpdateSendWarningButtonSprite()
    {
        if (_sendWarningButton == null)
            return;

        Sprite sprite = _isSendWarningSuccessState ? sendWarningSelectedSprite : sendWarningNormalSprite;
        SetBackgroundSprite(
            _sendWarningButton,
            sprite,
            _isSendWarningSuccessState ? nameof(sendWarningSelectedSprite) : nameof(sendWarningNormalSprite));
    }

    private int GetPopulationForZone(string geoid)
    {
        if (zoneBaselineRiskController == null)
            return 0;

        if (!zoneBaselineRiskController.HasCalculatedBaselineRisk)
            zoneBaselineRiskController.EnsureBaselineRiskCalculated();

        return zoneBaselineRiskController.TryGetRiskData(geoid, out ZoneBaselineRiskData riskData)
            ? Mathf.Max(0, riskData.rawPopulation)
            : 0;
    }

    private bool TryGetActiveShelterForZone(string geoid, out PlacedShelterData shelter)
    {
        shelter = null;
        geoid = NormalizeGeoid(geoid);

        if (string.IsNullOrEmpty(geoid) || shelterController == null)
            return false;

        return shelterController.TryGetActiveShelterForZone(geoid, out shelter);
    }

    private void SetBackgroundSprite(VisualElement element, Sprite sprite, string spriteLabel)
    {
        if (element == null)
            return;

        if (sprite == null)
        {
            WarnMissingSpriteOnce(spriteLabel);
            return;
        }

        element.style.backgroundImage = new StyleBackground(Background.FromSprite(sprite));
    }

    private void WarnMissingSpriteOnce(string spriteLabel)
    {
        if (string.IsNullOrEmpty(spriteLabel) || !_missingSpriteWarnings.Add(spriteLabel))
            return;

        Debug.LogWarning($"[CommunicationPanelController] Sprite '{spriteLabel}' is not assigned. Keeping existing UI background.");
    }

    private void ResolveReferences()
    {
        if (communicationUIDocument == null)
            communicationUIDocument = GetComponent<UIDocument>();

        if (communicationTowerController == null)
            communicationTowerController = FindFirstObjectByType<CommunicationController>();

        if (shelterController == null)
            shelterController = FindFirstObjectByType<ShelterController>();

        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (zoneBaselineRiskController == null)
            zoneBaselineRiskController = FindFirstObjectByType<ZoneBaselineRiskController>();

        if (communicationUIDocument == null)
            communicationUIDocument = FindUIDocumentContaining(PanelRootName);
    }

    private void LogMissingElementWarnings()
    {
        LogMissingElement(_panelRoot, PanelRootName);
        LogMissingElement(_fullPanel, FullPanelName);
        LogMissingElement(_collapsedPanel, CollapsedPanelName);
        LogMissingElement(_minimizeButton, MinimizeButtonName);
        LogMissingElement(_collapsedButton, CollapsedButtonName);
        LogMissingElement(_zoneTabContainer, ZoneTabContainerName);
        LogMissingElement(_sendWarningButton, SendWarningButtonName);
        LogMissingElement(_selectedZonesLabel, SelectedZonesLabelName);
        LogMissingElement(_selectedPopulationLabel, SelectedPopulationLabelName);
    }

    private static void LogMissingElement(VisualElement element, string elementName)
    {
        if (element == null)
            Debug.LogWarning($"[CommunicationPanelController] Required UXML element '{elementName}' is missing.");
    }

    private static void LogMissingZoneTabElementWarnings(ZoneTabBinding binding, string tabName)
    {
        if (binding.zoneIdLabel == null)
            Debug.LogWarning($"[CommunicationPanelController] Zone tab '{tabName}' is missing '{ZoneIdLabelName}'.");

        if (binding.populationLabel == null)
            Debug.LogWarning($"[CommunicationPanelController] Zone tab '{tabName}' is missing '{PopulationLabelName}'.");

        if (binding.shelterIdLabel == null)
            Debug.LogWarning($"[CommunicationPanelController] Zone tab '{tabName}' is missing '{ShelterIdLabelName}'.");

        if (binding.shelterCapacityLabel == null)
            Debug.LogWarning($"[CommunicationPanelController] Zone tab '{tabName}' is missing '{ShelterCapacityLabelName}'.");

        if (binding.evacuationStatusIcon == null)
            Debug.LogWarning($"[CommunicationPanelController] Zone tab '{tabName}' is missing '{EvacStatusIconName}'.");

        if (binding.communicationStatusIcon == null)
            Debug.LogWarning($"[CommunicationPanelController] Zone tab '{tabName}' is missing '{CommunicationStatusIconName}'.");

        if (binding.warningButton == null)
            Debug.LogWarning($"[CommunicationPanelController] Zone tab '{tabName}' is missing '{WarningButtonName}'.");

        if (binding.riskIcon == null)
            Debug.LogWarning($"[CommunicationPanelController] Zone tab '{tabName}' is missing '{RiskIconName}'.");
    }

    private static UIDocument FindUIDocumentContaining(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
            return null;

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        UIDocument[] documents = UnityEngine.Object.FindObjectsOfType<UIDocument>(true);
#endif

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];

            if (document == null || document.rootVisualElement == null)
                continue;

            if (document.rootVisualElement.Q<VisualElement>(elementName) != null)
                return document;
        }

        return null;
    }

    private static bool IsZoneTabElement(VisualElement element)
    {
        return element != null &&
               !string.IsNullOrEmpty(element.name) &&
               element.name.StartsWith("zone_tab_", StringComparison.Ordinal);
    }

    private static int GetTowerZoneCount(CommunicationTowerData tower)
    {
        return tower?.associatedZoneGeoids != null ? tower.associatedZoneGeoids.Count : 0;
    }

    private static string GetTowerId(CommunicationTowerData tower)
    {
        return tower != null && !string.IsNullOrWhiteSpace(tower.towerId)
            ? tower.towerId.Trim()
            : "Unknown Tower";
    }

    private static string GetSafeShelterId(PlacedShelterData shelter)
    {
        return shelter != null && !string.IsNullOrWhiteSpace(shelter.shelterId)
            ? shelter.shelterId.Trim()
            : "No Shelter";
    }

    private static string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? string.Empty : geoid.Trim();
    }
}
