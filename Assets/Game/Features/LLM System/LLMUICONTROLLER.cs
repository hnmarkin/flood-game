using UnityEngine;
using TMPro;

public class LLMUIController : MonoBehaviour
{
    [Header("LLM Feedback UI")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackText;

    private void Awake()
    {
        // Hide the panel when the game starts
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
    }

    public void ShowMessage(string message)
    {
        Debug.Log("LLM UI MESSAGE: " + message);

        if (feedbackPanel == null)
        {
            Debug.LogWarning("LLMUIController: Feedback Panel is not assigned.");
            return;
        }

        if (feedbackText == null)
        {
            Debug.LogWarning("LLMUIController: Feedback Text is not assigned.");
            return;
        }

        feedbackText.text = message;
        feedbackPanel.SetActive(true);
    }

    public void HideMessage()
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
    }
}