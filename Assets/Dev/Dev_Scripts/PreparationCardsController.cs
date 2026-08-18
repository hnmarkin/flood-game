using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PreparationCardsController : MonoBehaviour
{
    private const string DefaultCardsJsonAssetPath = "Assets/_Project/Scripts/Dev_Scripts/Data/cards.json";
    private const string RootName = "prep_card_root";
    private const string ContainerName = "prep_card_container";
    private const string ScrollName = "prep_card_scroll";
    private const string GridName = "prep_card_grid";
    private const string CardShellName = "card_col1";
    private const string CardContentName = "card_col_container";
    private const string TitleLabelName = "title_label";
    private const string TypeLabelName = "type_label";
    private const string ResidentialLabelName = "residential_label";
    private const string MoneyLabelName = "money_label";
    private const string ActionPointsLabelName = "action_points_label";
    private const string PrereqsLabelName = "prereqs_label";
    private const string EffectsLabelName = "effects_label";
    private const string CommsFailureLabelName = "commsfailure_label";
    private const string DescriptionLabelName = "description_label";
    private const string CardClassName = "prep-card";
    private const string CardDisabledClassName = "prep-card-disabled";
    private const string CardContentClassName = "prep-card-content";
    private const string CardFieldClassName = "prep-card-field";
    private const string CardTitleClassName = "prep-card-title";
    private const string CardDescriptionClassName = "prep-card-description";
    private const string CardGridClassName = "prep-card-grid";
    private const string CardScrollClassName = "prep-card-scroll";
    private const string SandbagCardId = "sandbag_and_temporary_barrier_stockpile";
    private const string LegacySandbagCardId = "sandbag";
    private const string SandbagCardTitle = "Sandbag & Temporary Barrier Stockpile";

    public event Action<bool> CardsUIVisibilityChanged;

    [Header("References")]
    [SerializeField] private UIDocument cardsUIDocument;
    [SerializeField] private TextAsset cardsJsonFile;
    [SerializeField] private FloodDefenseBoxStamp floodDefenseBoxStamp;

    [Header("Card Availability")]
    [SerializeField] private bool defaultCardEnabled = true;
    [SerializeField] private bool hideDisabledCards = true;
    [SerializeField] private List<PreparationCardOverride> cardOverrides = new();

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<PreparationCardData> _loadedCards = new();
    private VisualElement _panelRoot;
    private VisualElement _grid;
    private Coroutine _bindRoutine;
    private bool _isBound;
    private bool _isVisible;

    public bool IsCardsUIVisible => _isVisible;

    private void Reset()
    {
        ResolveReferences();
        AutoAssignCardsJsonFile();
    }

    private void Awake()
    {
        ResolveReferences();
        AutoAssignCardsJsonFile();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (TryBindUI())
        {
            RefreshCards();
            HideCardsUI();
        }
        else if (_bindRoutine == null)
        {
            _bindRoutine = StartCoroutine(BindUIWhenReady());
        }
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        if (_grid != null)
            _grid.Clear();

        _panelRoot = null;
        _grid = null;
        _isBound = false;
        _isVisible = false;
    }

    private void OnValidate()
    {
        AutoAssignCardsJsonFile();
    }

    public void ShowCardsUI()
    {
        if (!TryBindUI())
        {
            Debug.LogWarning("[PreparationCardsController] Cannot show PreparationCards UI because the UI is not bound.");
            return;
        }

        RefreshCards();
        SetCardsUIVisible(true);
    }

    public void HideCardsUI()
    {
        if (!TryBindUI())
            return;

        SetCardsUIVisible(false);
    }

    public void ToggleCardsUI()
    {
        if (IsCardsUIVisible)
            HideCardsUI();
        else
            ShowCardsUI();
    }

    public void RefreshCards()
    {
        if (!TryBindUI())
        {
            Debug.LogWarning("[PreparationCardsController] Cannot refresh cards because the UI is not bound.");
            return;
        }

        LoadCardsFromJson();
        PopulateCards();
    }

    private void ResolveReferences()
    {
        if (cardsUIDocument == null)
            cardsUIDocument = GetComponent<UIDocument>();

        if (floodDefenseBoxStamp == null)
            floodDefenseBoxStamp = FindFirstObjectByType<FloodDefenseBoxStamp>();
    }

    private IEnumerator BindUIWhenReady()
    {
        const int maxFramesToWait = 30;
        int waitedFrames = 0;

        while (!_isBound && waitedFrames < maxFramesToWait)
        {
            ResolveReferences();

            if (TryBindUI())
                break;

            waitedFrames++;
            yield return null;
        }

        if (_isBound)
        {
            RefreshCards();
            HideCardsUI();
        }
        else
        {
            Debug.LogWarning("[PreparationCardsController] Could not bind PreparationCards UI from the assigned UIDocument.");
        }

        _bindRoutine = null;
    }

    private bool TryBindUI()
    {
        if (_isBound && _panelRoot != null && _grid != null)
            return true;

        if (cardsUIDocument == null)
            return false;

        VisualElement documentRoot = cardsUIDocument.rootVisualElement;

        if (documentRoot == null)
            return false;

        _panelRoot = documentRoot.Q<VisualElement>(RootName) ?? documentRoot;
        VisualElement container = _panelRoot.Q<VisualElement>(ContainerName) ?? _panelRoot;
        ScrollView scrollView = _panelRoot.Q<ScrollView>(ScrollName);

        if (scrollView == null)
        {
            scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = ScrollName
            };
            scrollView.AddToClassList(CardScrollClassName);
            container.Clear();
            container.Add(scrollView);
        }
        else
        {
            scrollView.AddToClassList(CardScrollClassName);
        }

        _grid = _panelRoot.Q<VisualElement>(GridName);

        if (_grid == null)
        {
            _grid = new VisualElement
            {
                name = GridName
            };
            _grid.AddToClassList(CardGridClassName);
            scrollView.contentContainer.Add(_grid);
        }
        else
        {
            _grid.AddToClassList(CardGridClassName);
        }

        _isBound = true;
        SetCardsUIVisible(false, false);

        return true;
    }

    private void LoadCardsFromJson()
    {
        _loadedCards.Clear();

        if (cardsJsonFile == null || string.IsNullOrWhiteSpace(cardsJsonFile.text))
        {
            Debug.LogWarning("[PreparationCardsController] cardsJsonFile is not assigned or is empty.");
            return;
        }

        try
        {
            PreparationCardLibrary library = JsonUtility.FromJson<PreparationCardLibrary>(cardsJsonFile.text);

            if (library?.cards != null)
                _loadedCards.AddRange(library.cards);

            if (debugLogs)
            {
                Debug.Log($"[PreparationCardsController] Cards JSON loaded from '{cardsJsonFile.name}'.");
                Debug.Log($"[PreparationCardsController] Number of cards loaded: {_loadedCards.Count}.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[PreparationCardsController] Failed to parse cards JSON. Error: {exception.Message}");
        }
    }

    private void PopulateCards()
    {
        _grid.Clear();

        int displayedCount = 0;

        foreach (PreparationCardData cardData in _loadedCards)
        {
            if (cardData == null)
                continue;

            bool isEnabled = IsCardEnabled(cardData);

            if (!isEnabled && hideDisabledCards)
                continue;

            VisualElement card = CreateCardElement(cardData, isEnabled);
            _grid.Add(card);
            displayedCount++;
        }

        if (debugLogs)
            Debug.Log($"[PreparationCardsController] Number of cards displayed: {displayedCount}.");
    }

    private VisualElement CreateCardElement(PreparationCardData cardData, bool isEnabled)
    {
        VisualElement card = new VisualElement
        {
            name = CardShellName,
            userData = cardData,
            pickingMode = PickingMode.Position
        };
        card.AddToClassList(CardClassName);

        VisualElement content = new VisualElement
        {
            name = CardContentName
        };
        content.AddToClassList(CardContentClassName);
        card.Add(content);

        content.Add(CreateLabel(TitleLabelName, CardTitleClassName));
        content.Add(CreateLabel(TypeLabelName, CardFieldClassName));
        content.Add(CreateLabel(ResidentialLabelName, CardFieldClassName));
        content.Add(CreateLabel(MoneyLabelName, CardFieldClassName));
        content.Add(CreateLabel(ActionPointsLabelName, CardFieldClassName));
        content.Add(CreateLabel(PrereqsLabelName, CardFieldClassName));
        content.Add(CreateLabel(EffectsLabelName, CardFieldClassName));
        content.Add(CreateLabel(CommsFailureLabelName, CardFieldClassName));
        content.Add(CreateLabel(DescriptionLabelName, CardDescriptionClassName));

        SetLabelText(card, TitleLabelName, Fallback(cardData.title, "Untitled Card"));
        SetLabelText(card, TypeLabelName, $"Type: {Fallback(cardData.type, "N/A")}");
        SetLabelText(card, ResidentialLabelName, $"Residential: {Fallback(cardData.residential, "N/A")}");
        SetLabelText(card, MoneyLabelName, $"Money: {Fallback(cardData.money, "N/A")}");
        SetLabelText(card, ActionPointsLabelName, $"Action Points: {Fallback(cardData.action_points, "N/A")}");
        SetLabelText(card, PrereqsLabelName, $"Prereqs: {Fallback(cardData.prereqs, "None")}");
        SetLabelText(card, EffectsLabelName, $"Effects: {Fallback(cardData.effects, "None")}");
        SetLabelText(card, CommsFailureLabelName, $"Comms Failure: {Fallback(GetCommsFailure(cardData), "N/A")}");
        SetLabelText(card, DescriptionLabelName, Fallback(cardData.description, string.Empty));

        if (isEnabled)
        {
            SetChildPickingMode(card, PickingMode.Ignore);
            card.RegisterCallback<ClickEvent>(OnGeneratedCardClicked);
        }
        else
        {
            card.AddToClassList(CardDisabledClassName);
            card.SetEnabled(false);
        }

        return card;
    }

    private Label CreateLabel(string labelName, string className)
    {
        Label label = new Label
        {
            name = labelName,
            pickingMode = PickingMode.Ignore
        };
        label.AddToClassList(className);
        return label;
    }

    private void SetLabelText(VisualElement card, string labelName, string text)
    {
        Label label = card.Q<Label>(labelName);

        if (label == null)
        {
            Debug.LogWarning($"[PreparationCardsController] Missing label '{labelName}' on generated preparation card.");
            return;
        }

        label.text = text;
    }

    private void OnGeneratedCardClicked(ClickEvent clickEvent)
    {
        VisualElement cardElement = clickEvent.currentTarget as VisualElement;

        if (cardElement == null)
            return;

        PreparationCardData cardData = cardElement.userData as PreparationCardData;

        if (cardData == null)
            return;

        clickEvent.StopPropagation();
        OnCardClicked(cardData);
    }

    private void OnCardClicked(PreparationCardData cardData)
    {
        string cardTitle = Fallback(cardData.title, "Untitled Card");

        if (debugLogs)
            Debug.Log($"[PreparationCardsController] Card clicked: {cardTitle} (id={cardData.id}).");

        HideCardsUI();

        if (IsSandbagCard(cardData))
        {
            TriggerPlaceBarriersFromSandbagCard();
            return;
        }

        // Future preparation card effects should be routed from here into their gameplay services.
    }

    private void TriggerPlaceBarriersFromSandbagCard()
    {
        if (floodDefenseBoxStamp == null)
            floodDefenseBoxStamp = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (floodDefenseBoxStamp == null)
        {
            Debug.LogWarning("[PreparationCardsController] Sandbag card clicked, but FloodDefenseBoxStamp is not assigned.");
            return;
        }

        floodDefenseBoxStamp.EnablePlaceBarrierMode();

        if (debugLogs)
            Debug.Log("[PreparationCardsController] Sandbag card triggered Place Barriers mode.");
    }

    private bool IsCardEnabled(PreparationCardData cardData)
    {
        bool isEnabled = defaultCardEnabled;

        if (cardOverrides == null || cardOverrides.Count == 0)
            return isEnabled;

        for (int i = 0; i < cardOverrides.Count; i++)
        {
            PreparationCardOverride cardOverride = cardOverrides[i];

            if (cardOverride == null || string.IsNullOrWhiteSpace(cardOverride.cardId))
                continue;

            if (MatchesCardKey(cardOverride.cardId, cardData))
                isEnabled = cardOverride.enabled;
        }

        return isEnabled;
    }

    private bool MatchesCardKey(string key, PreparationCardData cardData)
    {
        return StringEquals(key, cardData.id) || StringEquals(key, cardData.title);
    }

    private bool IsSandbagCard(PreparationCardData cardData)
    {
        return StringEquals(cardData.id, SandbagCardId)
            || StringEquals(cardData.id, LegacySandbagCardId)
            || StringEquals(cardData.title, SandbagCardTitle);
    }

    private void SetCardsUIVisible(bool isVisible, bool logChange = true)
    {
        if (_panelRoot == null)
            return;

        bool changed = _isVisible != isVisible;
        _isVisible = isVisible;
        _panelRoot.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

        if (changed)
            CardsUIVisibilityChanged?.Invoke(_isVisible);

        if (changed && logChange && debugLogs)
        {
            string state = isVisible ? "opened" : "closed";
            Debug.Log($"[PreparationCardsController] PreparationCards UI {state}.");
        }
    }

    private void SetChildPickingMode(VisualElement parent, PickingMode pickingMode)
    {
        if (parent == null)
            return;

        foreach (VisualElement child in parent.Children())
        {
            child.pickingMode = pickingMode;
            SetChildPickingMode(child, pickingMode);
        }
    }

    private string GetCommsFailure(PreparationCardData cardData)
    {
        return !string.IsNullOrWhiteSpace(cardData.commsfailure)
            ? cardData.commsfailure
            : cardData.comms_failure;
    }

    private string Fallback(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private bool StringEquals(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

#if UNITY_EDITOR
    private void AutoAssignCardsJsonFile()
    {
        if (cardsJsonFile != null)
            return;

        cardsJsonFile = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultCardsJsonAssetPath);
    }
#else
    private void AutoAssignCardsJsonFile()
    {
    }
#endif
}

[Serializable]
public class PreparationCardLibrary
{
    public List<PreparationCardData> cards;
}

[Serializable]
public class PreparationCardData
{
    public string id;
    public string title;
    public string type;
    public string residential;
    public string corporate;
    public string political;
    public string money;
    public string action_points;
    public string turns;
    public string prereqs;
    public string stacks;
    public string effects;
    public string commsfailure;
    public string comms_failure;
    public string description;
}

[Serializable]
public class PreparationCardOverride
{
    public string cardId;
    public bool enabled = true;
}
