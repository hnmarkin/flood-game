using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GlobalHUDController : MonoBehaviour
{
    public static GlobalHUDController Instance { get; private set; }

    private Label _money;
    private Label _barriers;
    private Label _title;
    private Label _pop;
    private Label _zone;

    [Header("UI Label Names")]
    [SerializeField] private string moneyLabelName = "money_label";
    [SerializeField] private string barrierLabelName = "barrier_label";
    [SerializeField] private string pageTitleName = "page_title";
    [SerializeField] private string populationLabelName = "pop_label";
    [SerializeField] private string zoneLabelName = "zone_label";

    [Header("Default Values")]
    [SerializeField] private int money = 200;
    [SerializeField] private int barriersPlaced = 0;
    [SerializeField] private int maxZoneBarriers = 3;
    [SerializeField] private int hoveredPopulation = 0;
    [SerializeField] private string hoveredZoneId = "--";
    [SerializeField] private string pageTitle = "GAME";

    [Header("Text Prefixes")]
    [SerializeField] private string moneyPrefix = "Money: $";
    [SerializeField] private string barrierPrefix = "Turns";
    [SerializeField] private string populationPrefix = "Population";
    [SerializeField] private string zonePrefix = "Zone";

    [Header("Scene Behavior")]
    [SerializeField] private bool persistAcrossScenes = true;

    private UIDocument _document;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        _document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        BindUI();
        RefreshAll();
    }

    private void BindUI()
    {
        if (_document == null)
            _document = GetComponent<UIDocument>();

        if (_document == null || _document.rootVisualElement == null)
        {
            Debug.LogWarning("[GlobalHUDController] UIDocument or rootVisualElement is missing.");
            return;
        }

        VisualElement root = _document.rootVisualElement;

        _money = root.Q<Label>(moneyLabelName);

        // This lets you use the new name "barrier_label",
        // but it also supports your old "time_label" name as a fallback.
        _barriers = root.Q<Label>(barrierLabelName);
        if (_barriers == null)
            _barriers = root.Q<Label>("time_label");

        _title = root.Q<Label>(pageTitleName);
        _pop = root.Q<Label>(populationLabelName);
        _zone = root.Q<Label>(zoneLabelName);

        if (_money == null)
            Debug.LogWarning($"[GlobalHUDController] Could not find Label named '{moneyLabelName}'.");

        if (_barriers == null)
            Debug.LogWarning($"[GlobalHUDController] Could not find Label named '{barrierLabelName}' or fallback 'time_label'.");

        if (_title == null)
            Debug.LogWarning($"[GlobalHUDController] Could not find Label named '{pageTitleName}'.");

        if (_pop == null)
            Debug.LogWarning($"[GlobalHUDController] Could not find Label named '{populationLabelName}'.");

        if (_zone == null)
            Debug.LogWarning($"[GlobalHUDController] Could not find Label named '{zoneLabelName}'.");
    }

    private void RefreshAll()
    {
        SetMoney(money);
        SetBarrierProgress(barriersPlaced, maxZoneBarriers);
        SetPageTitle(pageTitle);
        SetHoveredPopulation(hoveredPopulation);
        SetHoveredZoneId(hoveredZoneId);
    }

    public void SetMoney(int dollars)
    {
        money = dollars;

        if (_money != null)
            _money.text = $"{moneyPrefix}{dollars}";
    }

    public void SetBarrierProgress(int placed, int max)
    {
        barriersPlaced = Mathf.Max(0, placed);
        maxZoneBarriers = Mathf.Max(1, max);

        if (_barriers != null)
            _barriers.text = $"{barrierPrefix}: {barriersPlaced}/{maxZoneBarriers}";
    }

    public void SetPageTitle(string title)
    {
        pageTitle = string.IsNullOrWhiteSpace(title) ? "GAME" : title;

        if (_title != null)
            _title.text = pageTitle;
    }

    public void SetHoveredZoneInfo(string zoneId, int population)
    {
        SetHoveredZoneId(zoneId);
        SetHoveredPopulation(population);
    }

    public void SetHoveredPopulation(int population)
    {
        hoveredPopulation = Mathf.Max(0, population);

        if (_pop != null)
            _pop.text = $"{populationPrefix}: {hoveredPopulation:N0}";
    }

    public void SetHoveredZoneId(string zoneId)
    {
        hoveredZoneId = string.IsNullOrWhiteSpace(zoneId) ? "--" : zoneId;

        if (_zone != null)
            _zone.text = $"{zonePrefix}: {hoveredZoneId}";
    }

    public void ClearHoveredZoneInfo()
    {
        hoveredPopulation = 0;
        hoveredZoneId = "--";

        if (_pop != null)
            _pop.text = $"{populationPrefix}: --";

        if (_zone != null)
            _zone.text = $"{zonePrefix}: --";
    }

    // Backward-compatible wrappers, in case older scripts still call these.

    public void SetTime(int currentTurn, int totalTurns)
    {
        SetBarrierProgress(currentTurn, totalTurns);
    }

    public void SetPopulation(int pop)
    {
        SetHoveredPopulation(pop);
    }

    public void SetZoneCount(int zones)
    {
        if (_zone != null)
            _zone.text = $"Zones: {zones}";
    }
}