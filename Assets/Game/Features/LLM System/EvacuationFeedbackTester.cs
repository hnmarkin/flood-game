using UnityEngine;

public class EvacuationFeedbackTester : MonoBehaviour
{
    [Header("Evacuation Data")]
    [SerializeField] private EvacuationFeedbackDataBuilder evacuationDataBuilder;

    [Header("LLM Settings")]
    [SerializeField] private LLMPersona feedbackPersona;

    private bool requestInProgress = false;

    private void Update()
    {
        // Press E to test evacuation feedback
        if (Input.GetKeyDown(KeyCode.E) && !requestInProgress)
        {
            GenerateEvacuationFeedback();
        }
    }

    public void GenerateEvacuationFeedback()
    {
        if (evacuationDataBuilder == null)
        {
            Debug.LogError(
                "EvacuationFeedbackTester: EvacuationFeedbackDataBuilder is not assigned."
            );
            return;
        }

        if (feedbackPersona == null)
        {
            Debug.LogError(
                "EvacuationFeedbackTester: Feedback Persona is not assigned."
            );
            return;
        }

        if (LLMManager.Instance == null)
        {
            Debug.LogError(
                "EvacuationFeedbackTester: LLMManager.Instance was not found."
            );
            return;
        }

        string validatedFacts =
            evacuationDataBuilder.BuildValidatedEvacuationFacts();

        if (string.IsNullOrWhiteSpace(validatedFacts))
        {
            Debug.LogError(
                "EvacuationFeedbackTester: No validated evacuation facts were generated."
            );
            return;
        }

        string prompt =
            "Evaluate the player's evacuation decisions using only the " +
            "validated evacuation facts below.\n\n" +

            validatedFacts +

            "\nInstructions:\n" +
            "- Treat all Unity-calculated values as authoritative.\n" +
            "- Do not recalculate or contradict them.\n" +
            "- Explain what the player did effectively.\n" +
            "- Identify the most important evacuation problem.\n" +
            "- State the exact remaining population that still needs evacuation.\n" +
            "- Comment on route usage using only the provided route counts.\n" +
            "- Do not invent route names, road conditions, flood depths, " +
            "travel times, locations, or destinations.\n" +
            "- Do not claim a route is safe or unsafe unless the data explicitly says so.\n" +
            "- Give practical recommendations supported by the supplied data.\n" +
            "- Write approximately 120 to 160 words.\n" +
            "- Selected routes means the number of routes currently selected by the player.\n" +
            "- If selectedRoutes is 2, explicitly state that 2 routes are selected; never describe it as 1 route.\n" +
            "- Use the word 'routes', not 'roads', unless the supplied data explicitly calls them roads.\n" +
            "- Do not suggest additional routes, shelters, vehicles, resources, or infrastructure unless they are present in the supplied data.\n" +
            "- When usable routes equal selected routes, state that all currently usable routes have been selected.\n" +
            "- Reply only with player-facing feedback.";

        Debug.Log(
            "Sending validated evacuation feedback request to local LLM..."
        );

        requestInProgress = true;

        LLMManager.Instance.RequestEventResponse(
            feedbackPersona,
            prompt
        );

        // Prevent accidental repeated E presses for a short period.
        Invoke(nameof(ResetRequest), 5f);
    }

    private void ResetRequest()
    {
        requestInProgress = false;
    }
}