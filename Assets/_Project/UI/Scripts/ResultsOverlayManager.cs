/*RELATIVELY BASIC SCRIPT BUT THE STARS IS CURRENTLY NOT BEING ENTERED THROUGH THE UNITY EVENT SYSTEM IN THE EDITOR*/

using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private GameObject residentialContainer;
    [SerializeField] private GameObject residentialStarContainer;
    [SerializeField] private GameObject residentialAvatar;
    [SerializeField] private GameObject residentialStarPrefab;
    [SerializeField] private GameObject residentialStarHolder;
    [SerializeField] private TMP_Text corporateScoreTextBox;
    [SerializeField] private TMP_Text politicalScoreTextBox;

    //Temporary Switch
    [SerializeField] private bool APIEnabled = true; // Set to false to use local evaluation

    [SerializeField] private float verticalMovement = 400f; // Amount to move the object vertically
    [SerializeField] private float starVertical = 400f; // Amount to move the object horizontally
    [SerializeField] private float starHorizontal = 400f; // Amount to move the object horizontally

    // Private Variable Declarations
    private int r_stars, c_stars, p_stars;
    private string r_response, c_response, p_response;

    private bool clickable = false;

    [SerializeField] private GameObject RESIDENTS;

    public async void OnEvaluateButtonClicked()
    {
        try
        {
            if (APIEnabled) {    
                (r_stars, r_response) = await _LLMController.EvaluateResidentialStars();
                (c_stars, c_response) = await _LLMController.EvaluateCorporateStars();
                (p_stars, p_response) = await _LLMController.EvaluatePoliticalStars();
            }
            else {
                // Local evaluation logic (for testing purposes)
                r_stars = 4; // Example value
                c_stars = 4; // Example value
                p_stars = 5; // Example value

                // Long example responses
                r_response = "This flood hit us hard. Over a third of our neighbors ended up with water in their homes, and many were caught off guard. Only 18% of us had flood insurance — that’s devastating for working families. On top of that, three lives were lost, and help didn’t come for three days. That’s just too long when you're watching your home wash away. The $10 million in relief funding helped a bit, but it’s not enough to rebuild trust or homes. We’re grateful for what we got, but next time, we need better preparation. and faster action.";
                c_response = "Corporate rating response"; // Example response
                p_response = "Political rating response"; // Example response
            }
            ShowOverlay(r_stars, c_stars, p_stars);

            //corporateScoreTextBox.text = c_response;
            //politicalScoreTextBox.text = p_response;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to evaluate: {ex.Message}");
            residentialScoreTextBox.text = "Error occurred during evaluation";
        }
    }

    public async void ShowOverlay(int r_stars, int c_stars, int p_stars)
    {
        ExpandOverlay();
    }

    public void HideOverlay()
    {
        LeanTween.sequence()
            .append(() => CollapseAnimation(residentialContainer, 0.5f))
            .append(() => SimpleCollapseAnimation(residentialStarContainer, 0.5f))
            .append(0.25f)
            .append(() => MoveStars(0.75f))
            .append(0.75f) // Wait for MoveStars to complete
            .append(() => ExpandAnimation(RESIDENTS, 0.75f));
    }

    //Animations
    public void ExpandOverlay()
    {
        overlayPanel.SetActive(true);
        overlayPanel.transform.localScale = Vector2.zero;

        LeanTween.cancel(overlayPanel); // Cancel any existing tweens
        LeanTween.scale(overlayPanel, Vector2.one, 0.5f).setEaseOutBack().setOnComplete(() =>
        {
            ExpandAnimation(residentialContainer, 1f);
            ExpandAnimation(residentialStarContainer, 1f);
        });

        var typewriterEffect = GetComponent<TypewriterEffect>();
            if (typewriterEffect != null)
            {
                typewriterEffect.Run(r_response, residentialScoreTextBox, () =>
                {
                    // Star animation
                    // First, clear out any existing stars
                    foreach (Transform child in residentialStarHolder.transform)
                    {
                        Destroy(child.gameObject);
                    }

                    // Instantiate the number of stars earned
                    for (int i = 0; i < r_stars; i++)
                    {
                        var star = Instantiate(residentialStarPrefab, residentialStarHolder.transform);
                        ExpandAnimation(star, 0.5f);
                    }
                });
            }
            else
            {
                Debug.LogError("TypewriterEffect component is missing.");
            }
            clickable = true;
    }

    public void ExpandAnimation(GameObject target, float speed)
    {
        target.SetActive(true);
        target.transform.localScale = Vector2.zero;
        LeanTween.scale(target, Vector2.one, speed).setEaseOutBack();
    }

    public void CollapseAnimation(GameObject target, float speed)
    {        
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"No RectTransform found on {target.name}");
            return;
        }

        // Store initial height
        float originalHeight = rectTransform.sizeDelta.y;

        // Create sequence
        LeanTween.sequence()
            // Step 1: Fade out all text and avatar
            .append(() => {
                // Fade text components
                TextMeshProUGUI[] textComponents = target.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (TextMeshProUGUI text in textComponents)
                {
                    CanvasGroup canvasGroup = text.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = text.gameObject.AddComponent<CanvasGroup>();
                    }
                    canvasGroup.alpha = 1f;
                    LeanTween.alphaCanvas(canvasGroup, 0f, speed).setEaseInBack();
                }

                // Shrink avatar GameObject
                LeanTween.scale(residentialAvatar, Vector3.zero, speed).setEaseInBack();
            })
            // Wait for fades to complete
            .append(speed)
            // Step 2: Collapse height and move up simultaneously
            .append(() => {
                // Move up
                LeanTween.moveLocalY(target, target.transform.localPosition.y + verticalMovement, speed)
                    .setEaseInBack();
                
                // Collapse height
                LeanTween.value(target.gameObject, originalHeight, originalHeight * 0.5f, speed)
                    .setEaseInBack()
                    .setOnUpdate((float value) => {
                        UpdateHeight(value, target.transform);
                    });
            });
    }

    public void SimpleCollapseAnimation(GameObject target, float speed)
    {
        target.transform.localScale = Vector2.one;
        LeanTween.scale(target, Vector2.zero, speed).setEaseInBack().setOnComplete(() =>
        {
            target.SetActive(false);
        });
    }
    
    public void MoveStars(float speed)
    {
        LeanTween.moveLocalX(residentialStarHolder, residentialStarHolder.transform.localPosition.x + starHorizontal, speed).setEaseInBack();
        LeanTween.moveLocalY(residentialStarHolder, residentialStarHolder.transform.localPosition.y + starVertical, speed).setEaseInBack();
    }

    // On Click event
    public void OnClick()
    {
        Debug.Log("Overlay clicked!");
        // Hide the overlay when clicked
        if (clickable)
        {
            HideOverlay();
            clickable = false;
        }
    }

    // Animate height of parent and all child RectTransforms
    void UpdateHeight(float value, Transform trans)
    {
        if (trans.TryGetComponent<RectTransform>(out var rt))
        {
            Vector2 sd = rt.sizeDelta;
            sd.y = value;
            rt.sizeDelta = sd;
        }
        
        // Update all children
        foreach (Transform child in trans)
        {
            UpdateHeight(value, child);
        }
    }
}

