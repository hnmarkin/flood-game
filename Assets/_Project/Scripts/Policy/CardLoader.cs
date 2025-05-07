using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CardLoader : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text residentialOpinionText;
    [SerializeField] private TMP_Text corporateOpinionText;
    [SerializeField] private TMP_Text politicalOpinionText;
    //Money and action points are lists of images
    [SerializeField] private List<Image> moneyImages;
    [SerializeField] private List<Image> actionPointsImages;

    public CardData _cardData { get; private set; }

    public void LoadCard(CardData cardData)
    {
        // Load the card data in
        _cardData = cardData;

        if (_cardData == null)
        {
            Debug.LogError("Card data is null!");
            return;
        }

        cardNameText.text = _cardData.cardName;
        residentialOpinionText.text = _cardData.residentialOpinion.ToString();
        corporateOpinionText.text = _cardData.corporateOpinion.ToString();
        politicalOpinionText.text = _cardData.politicalOpinion.ToString();

        // Set money pips
        for (int i = 0; i < _cardData.money; i++)
        {
            if (i < moneyImages.Count)
            {
                moneyImages[i].gameObject.SetActive(true);
            }
        }

        // Set action points pips
        for (int i = 0; i < _cardData.actionPoints; i++)
        {
            if (i < actionPointsImages.Count)
            {
                actionPointsImages[i].gameObject.SetActive(true);
            }
        }
    }
}
