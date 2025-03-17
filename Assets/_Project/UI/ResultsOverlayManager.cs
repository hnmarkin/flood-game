/*RELATIVELY BASIC SCRIPT BUT THE STARS IS CURRENTLY NOT BEING ENTERED THROUGH THE UNITY EVENT SYSTEM IN THE EDITOR*/

using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultsOverlayManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject overlayPanel;
    // [SerializeField] private string residentialRating;
    // [SerializeField] private string corporateRating;
    // [SerializeField] private string politicalRating;

    [SerializeField] LLMController _LLMController;

    [SerializeField] private Image[] starImages;
    [SerializeField] private GameObject hiddenStarObject;

    //Text Boxes
    [SerializeField] private TMP_Text residentialScoreTextBox;
    [SerializeField] private TMP_Text corporateScoreTextBox;
    [SerializeField] private TMP_Text politicalScoreTextBox;

    public async void OnEvaluateButtonClicked()
    {
        try
        {
            (int stars, string response) = await _LLMController.EvaluateResidentialStars();
            ShowOverlay(stars);
            residentialScoreTextBox.text = response;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to evaluate: {ex.Message}");
            residentialScoreTextBox.text = "Error occurred during evaluation";
        }
    }

    public async void ShowOverlay(int stars)
    {
        overlayPanel.SetActive(true);

        // Update stars display
        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].enabled = i < stars;
        }

        // Show/hide hidden star bonus
        //hiddenStarObject.SetActive(hasHiddenStar);
    }

    public void HideOverlay()
    {
        overlayPanel.SetActive(false);
    }
}
