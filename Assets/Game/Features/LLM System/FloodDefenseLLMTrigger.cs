using UnityEngine;

public class FloodDefenseLLMTrigger : MonoBehaviour
{
    [Header("Flood Defense Feedback")]
    [SerializeField]
    private FloodDefenseFeedbackDataBuilder floodDefenseDataBuilder;

    [Header("LLM Settings")]
    [SerializeField]
    private LLMPersona feedbackPersona;

    private void OnEnable()
    {
        FloodDefenseBoxStamp.OnAllZoneBarriersPlaced +=
            GenerateAutomaticFeedback;

        Debug.Log(
            "[LLM AUTO] FloodDefenseLLMTrigger ENABLED and listening."
        );
    }

    private void OnDisable()
    {
        FloodDefenseBoxStamp.OnAllZoneBarriersPlaced -=
            GenerateAutomaticFeedback;

        Debug.Log(
            "[LLM AUTO] FloodDefenseLLMTrigger DISABLED."
        );
    }

    private void GenerateAutomaticFeedback()
    {
        Debug.Log(
            "[LLM AUTO] OnAllZoneBarriersPlaced EVENT RECEIVED!"
        );

        if (floodDefenseDataBuilder == null)
        {
            Debug.LogError(
                "[LLM AUTO] FloodDefenseFeedbackDataBuilder is missing."
            );
            return;
        }

        if (feedbackPersona == null)
        {
            Debug.LogError(
                "[LLM AUTO] Feedback Persona is missing."
            );
            return;
        }

        if (LLMManager.Instance == null)
        {
            Debug.LogError(
                "[LLM AUTO] LLMManager.Instance is missing."
            );
            return;
        }

        string validatedFacts =
            floodDefenseDataBuilder.BuildValidatedFloodDefenseFacts();

        if (string.IsNullOrWhiteSpace(validatedFacts))
        {
            Debug.LogError(
                "[LLM AUTO] Validated facts are empty."
            );
            return;
        }

        Debug.Log(
            "[LLM AUTO] Validated facts generated:\n" +
            validatedFacts
        );

        string prompt =
            "Provide player feedback after all available flood barriers " +
            "have been placed.\n\n" +

            validatedFacts +

            "\nINSTRUCTIONS:\n" +
            "- Treat Unity-calculated values as authoritative.\n" +
            "- State the exact number of barriers placed.\n" +
            "- State the exact maximum number available.\n" +
            "- State the exact number remaining.\n" +
            "- Do not invent barrier locations or protected zones.\n" +
            "- Do not invent flood depths or water levels.\n" +
            "- Do not invent barrier effectiveness.\n" +
            "- Write approximately 100 to 150 words.\n" +
            "- Reply only with player-facing feedback.";

        Debug.Log(
            "[LLM AUTO] Sending request to LLMManager..."
        );

        LLMManager.Instance.RequestEventResponse(
            feedbackPersona,
            prompt
        );
    }
}