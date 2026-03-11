using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class DecisionScreenController : MonoBehaviour
{
    [Header("UXML Template")]
    [SerializeField] private VisualTreeAsset cardTemplate;   // Assign Card.uxml in Inspector

    [Header("Card Data (JSON)")]
    [SerializeField] private TextAsset cardsJson;            // Assign cards.json in Inspector

    [Header("Flow References")]
    [SerializeField] private FloodDefenseBoxStamp floodDefenseBoxStamp;   // drag your stamp object here
    [SerializeField] private GameObject decisionScreenUI;                // drag DecisionScreen_UI here (the same GO this script is on)
    [SerializeField] private ZoneActionToast zoneActionToast;            // optional tiny popup script (below)

    private VisualElement cardsContainer;

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator Pulse(VisualElement cardRoot)
    {
        // start state
        cardRoot.AddToClassList("featured-on");

        while (true)
        {
            cardRoot.ToggleInClassList("featured-on");
            cardRoot.ToggleInClassList("featured-off");
            yield return new WaitForSeconds(0.6f);
        }
    }

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;

        // DecisionScreen container name
        cardsContainer = root.Q<VisualElement>("cards_container");

        if (cardsContainer == null)
        {
            Debug.LogError("DecisionScreenController: Could not find #cards_container in DecisionScreen.uxml");
            return;
        }

        if (cardTemplate == null)
        {
            Debug.LogError("DecisionScreenController: cardTemplate is not assigned (Card.uxml).");
            return;
        }

        var cards = LoadCardsFromJson();
        Populate(cards);
    }

    private List<CardDefinition> LoadCardsFromJson()
    {
        if (cardsJson == null || string.IsNullOrWhiteSpace(cardsJson.text))
        {
            Debug.LogWarning("DecisionScreenController: cardsJson not assigned or empty. No cards will be created.");
            return new List<CardDefinition>();
        }

        try
        {
            var wrapper = JsonUtility.FromJson<CardListWrapper>(cardsJson.text);
            return wrapper?.cards ?? new List<CardDefinition>();
        }
        catch (Exception e)
        {
            Debug.LogError($"DecisionScreenController: Failed to parse cards JSON. Error: {e.Message}");
            return new List<CardDefinition>();
        }
    }

    private void Populate(List<CardDefinition> cards)
    {
        cardsContainer.Clear();

        foreach (var data in cards)
        {
            // Create instance from Card.uxml
            var cardInstance = cardTemplate.Instantiate();

            // Fill labels by your Card.uxml element NAMES
            cardInstance.Q<Label>("card_title").text = data.title;
            cardInstance.Q<Label>("card_type").text = data.type;
            cardInstance.Q<Label>("money").text = $"Money: {data.money}";
            cardInstance.Q<Label>("action_points").text = $"Action Points: {data.action_points}";
            cardInstance.Q<Label>("effects").text = $"Effects: {data.effects}";
            cardInstance.Q<Label>("card_description").text = $"Description: {data.description}";

            // Making the entire card clickable
            var cardRoot = cardInstance.Q<VisualElement>("card_root");
            if (cardRoot == null)
            {
                Debug.LogWarning("DecisionScreenController: Card template missing #card_root. Click handling skipped.");
            }
            else
            {
                // Optional: let hover/click feel work nicely
                cardRoot.pickingMode = PickingMode.Position;

                cardRoot.RegisterCallback<ClickEvent>(_ =>
                {
                    Debug.Log($"Card selected: {data.title} (id={data.id})");

                    if (data.id == "sandbag")
                    {
                        // 1) Hide decision screen UI
                        if (decisionScreenUI != null)
                            decisionScreenUI.SetActive(false);
                        else
                            gameObject.SetActive(false); // fallback

                        // 2) Tell player what to do
                        if (zoneActionToast != null)
                            zoneActionToast.Show("Choose a Zone to sandbag");
                        else
                            Debug.Log("Choose a Zone to sandbag");

                        // 3) Enter zone-boundary sandbag mode
                        if (floodDefenseBoxStamp != null)
                            floodDefenseBoxStamp.EnterZoneBoundaryPlacementMode();
                        else
                            Debug.LogError("DecisionScreenController: floodDefenseBoxStamp is not assigned.");
                    }

                    // else other cards later...
                    StopAllCoroutines();
                });
            }

            if (data.id == "sandbag" && cardRoot != null)
            {
                StartCoroutine(Pulse(cardRoot));
            }

            cardsContainer.Add(cardInstance);
            Debug.Log($"Added card: {data.id}");
        }
        Debug.Log($"Creating {cards.Count} cards");
    }

    [Serializable]
    private class CardListWrapper
    {
        public List<CardDefinition> cards;
    }

    [Serializable]
    private class CardDefinition
    {
        public string id;
        public string title;
        public string type;
        public string money;
        public string action_points;
        public string effects;
        public string description;
    }
}