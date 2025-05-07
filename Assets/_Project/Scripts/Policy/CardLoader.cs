using UnityEngine;
using TMPro;

public class CardLoader : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text residentialOpinionText;
    [SerializeField] private TMP_Text corporateOpinionText;
    [SerializeField] private TMP_Text politicalOpinionText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text actionPointsText;

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

        moneyText.text = _cardData.money.ToString();
        actionPointsText.text = _cardData.actionPoints.ToString();
    }
}
