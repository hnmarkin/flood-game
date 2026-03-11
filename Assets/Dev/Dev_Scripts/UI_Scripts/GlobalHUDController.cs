using UnityEngine;
using UnityEngine.UIElements;

public class GlobalHUDController : MonoBehaviour
{
    public static GlobalHUDController Instance { get; private set; }

    private Label _money;
    private Label _time;
    private Label _title;
    private Label _pop;
    private Label _zones;

    [Header("Default Values")]
    [SerializeField] private int money = 0;
    [SerializeField] private int turn = 1;
    [SerializeField] private int maxTurns = 8;
    [SerializeField] private int population = 0;
    [SerializeField] private int zoneCount = 0;
    [SerializeField] private string pageTitle = "Game";

    private void Awake()
    {
        // Singleton + persist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;

        _money = root.Q<Label>("money_label");
        _time  = root.Q<Label>("time_label");
        _title = root.Q<Label>("page_title");
        _pop   = root.Q<Label>("pop_label");
        _zones = root.Q<Label>("zone_label");

        // Apply defaults on enable
        SetMoney(money);
        SetTime(turn, maxTurns);
        SetPopulation(population);
        SetZoneCount(zoneCount);
        SetPageTitle(pageTitle);
    }

    public void SetMoney(int dollars)
    {
        money = dollars;
        if (_money != null) _money.text = $"${dollars}";
    }

    public void SetTime(int currentTurn, int totalTurns)
    {
        turn = currentTurn;
        maxTurns = totalTurns;
        if (_time != null) _time.text = $"{currentTurn}/{totalTurns}";
    }

    public void SetPageTitle(string title)
    {
        pageTitle = title;
        if (_title != null) _title.text = title;
    }

    public void SetPopulation(int pop)
    {
        population = pop;
        if (_pop != null) _pop.text = $"Population: {pop}";
    }

    public void SetZoneCount(int zones)
    {
        zoneCount = zones;
        if (_zones != null) _zones.text = $"Zones: {zones}";
    }
}