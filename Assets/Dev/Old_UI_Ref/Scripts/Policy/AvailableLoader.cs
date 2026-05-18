using UnityEngine;
using TMPro;

public class AvailableLoader : MonoBehaviour
{
    [SerializeField] private GameObject availablePrefab;
    [SerializeField] private Transform availableParent;
    [SerializeField] private CardData[] availableCards;
    [SerializeField] private TMP_Text infoText;

    private void Start()
    {
        LoadAvailableCards();
    }

    private void LoadAvailableCards()
    {
        foreach (CardData cardData in availableCards)
        {
            GameObject cardObject = Instantiate(availablePrefab, availableParent);
            CardLoader cardLoader = cardObject.GetComponent<CardLoader>();
            if (cardLoader != null)
            {
                cardLoader.LoadCard(cardData);
            }
            else
            {
                Debug.LogError("CardLoader component not found on the instantiated prefab!");
            }
        }
    }

    public void SetInfoText(string text)
    {
        if (infoText != null)
        {
            infoText.text = text;
        }
        else
        {
            Debug.LogError("InfoText component is not assigned!");
        }
    }
}
