using UnityEngine;

public class AvailableLoader : MonoBehaviour
{
    [SerializeField] private GameObject availablePrefab;
    [SerializeField] private Transform availableParent;
    [SerializeField] private CardData[] availableCards;

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
}
