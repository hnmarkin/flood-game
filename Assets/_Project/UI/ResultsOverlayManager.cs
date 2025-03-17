/*RELATIVELY BASIC SCRIPT BUT THE STARS IS CURRENTLY NOT BEING ENTERED THROUGH THE UNITY EVENT SYSTEM IN THE EDITOR*/

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultsOverlayManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private TMP_Text residentialScoreText;
    [SerializeField] private Image[] starImages;
    [SerializeField] private GameObject hiddenStarObject;

    public void ShowOverlay(int stars)
    {
        overlayPanel.SetActive(true);

        // Update stars display
        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].enabled = i < stars;
        }

        // Show/hide hidden star bonus
        //hiddenStarObject.SetActive(hasHiddenStar);

        // Update score text
        residentialScoreText.text = $"Residential Score: {stars}/4 stars";
    }

    public void HideOverlay()
    {
        overlayPanel.SetActive(false);
    }
}
