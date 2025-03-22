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

    [SerializeField] private Image[] residentialStarImages;
    [SerializeField] private Image[] corporateStarImages;
    [SerializeField] private Image[] politicalStarImages;
    [SerializeField] private GameObject hiddenStarObject;

    //Text Boxes
    [SerializeField] private TMP_Text residentialScoreTextBox;
    [SerializeField] private TMP_Text corporateScoreTextBox;
    [SerializeField] private TMP_Text politicalScoreTextBox;

    public async void OnEvaluateButtonClicked()
    {
        try
        {
            (int r_Stars, string r_response) = await _LLMController.EvaluateResidentialStars();
            (int c_stars, string c_response) = await _LLMController.EvaluateCorporateStars();
            (int p_stars, string p_response) = await _LLMController.EvaluatePoliticalStars();
            ShowOverlay(r_Stars, c_stars, p_stars);
            residentialScoreTextBox.text = r_response;
            corporateScoreTextBox.text = c_response;
            politicalScoreTextBox.text = p_response;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to evaluate: {ex.Message}");
            residentialScoreTextBox.text = "Error occurred during evaluation";
        }
    }

    public async void ShowOverlay(int r_stars, int c_stars, int p_stars)
    {
        overlayPanel.SetActive(true);

        // Update stars display
        for (int i = 0; i < residentialStarImages.Length; i++)
        {
            residentialStarImages[i].enabled = i < r_stars;
        }
        for (int i = 0; i < corporateStarImages.Length; i++)
        {
            corporateStarImages[i].enabled = i < c_stars;
        }
        for (int i = 0; i < politicalStarImages.Length; i++)
        {
            politicalStarImages[i].enabled = i < p_stars;
        }

        // Show/hide hidden star bonus
        //hiddenStarObject.SetActive(hasHiddenStar);
    }

    public void HideOverlay()
    {
        overlayPanel.SetActive(false);
    }
}
