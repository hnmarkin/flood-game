using UnityEngine;

public class FloodDefenseFeedbackTester : MonoBehaviour
{
    [Header("Flood Defense Data")]
    [SerializeField]
    private FloodDefenseFeedbackDataBuilder floodDefenseDataBuilder;

    [Header("LLM Settings")]
    [SerializeField]
    private LLMPersona feedbackPersona;

    private bool requestInProgress = false;

    private void Update()
    {
        // Press B to generate flood-defense feedback
        if (Input.GetKeyDown(KeyCode.B) && !requestInProgress)
        {
            GenerateFloodDefenseFeedback();
        }
    }

    public void GenerateFloodDefenseFeedback()
    {
        if (floodDefenseDataBuilder == null)
        {
            Debug.LogError(
                "FloodDefenseFeedbackTester: Data Builder is not assigned."
            );
            return;
        }

        if (feedbackPersona == null)
        {
            Debug.LogError(
                "FloodDefenseFeedbackTester: Feedback Persona is not assigned."
            );
            return;
        }

        if (LLMManager.Instance == null)
        {
            Debug.LogError(
                "FloodDefenseFeedbackTester: LLMManager was not found."
            );
            return;
        }

        string validatedFacts =
            floodDefenseDataBuilder.BuildValidatedFloodDefenseFacts();

        if (string.IsNullOrWhiteSpace(validatedFacts))
        {
            Debug.LogError(
                "FloodDefenseFeedbackTester: No validated facts generated."
            );
            return;
        }

        string prompt =
            "Evaluate the player's flood-defense decisions using only " +
            "the validated Unity game-state facts below.\n\n" +

            validatedFacts +

            "\nPLAYER FEEDBACK INSTRUCTIONS:\n" +
            "- Treat all Unity-calculated values as authoritative.\n" +
            "- State the exact number of barriers placed.\n" +
            "- State the exact maximum number of barriers available.\n" +
            "- State how many barriers remain available.\n" +
            "- If all barriers have been placed, explicitly state that no barriers remain.\n" +
            "- If barriers remain, explain that the player still has unused barriers.\n" +
            "- Do not invent barrier locations or zone names.\n" +
            "- Do not invent water depth, flood severity, protected population, or effectiveness.\n" +
            "- Do not claim that a particular area is protected unless the data says so.\n" +
            "- Do not invent additional defenses or resources.\n" +
            "- Give practical feedback supported only by the supplied facts.\n" +
            "- Write approximately 120 to 160 words.\n" +
            "- Reply only with player-facing feedback.";

        Debug.Log(
            "Sending validated flood-defense feedback request to local LLM..."
        );

        requestInProgress = true;

        LLMManager.Instance.RequestEventResponse(
            feedbackPersona,
            prompt
        );

        Invoke(nameof(ResetRequest), 5f);
    }

    private void ResetRequest()
    {
        requestInProgress = false;
    }
}