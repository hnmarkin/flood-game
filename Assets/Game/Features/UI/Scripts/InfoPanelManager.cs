using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoPanelManager : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private CardData policyCard;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI description;

    private void Start()
    {
        if (infoPanel == null)
        {
            Debug.LogError("Info Panel is not assigned in the Inspector.");
            return;
        }

        if (policyCard == null)
        {
            Debug.LogError("Policy Card is not assigned in the Inspector.");
            return;
        }

        UpdateInfoPanel();
    }

    public void UpdateInfoPanel()
    {
        //infoPanel.SetActive(true);
        description.text = policyCard.description;
    }

}
