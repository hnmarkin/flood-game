using UnityEngine;

public class FloodFeedbackTester : MonoBehaviour
{
    [Header("Test Input")]
    [SerializeField] private TextAsset gameStateJson;

    [Header("Validated Data")]
    [SerializeField] private ShelterFeedbackDataBuilder shelterDataBuilder;

    [Header("LLM Settings")]
    [SerializeField] private LLMPersona feedbackPersona;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            GenerateFeedback();
        }
    }

    public void GenerateFeedback()
    {
        if (gameStateJson == null)
        {
            Debug.LogError(
                "FloodFeedbackTester: Game State JSON is not assigned."
            );

            return;
        }

        if (shelterDataBuilder == null)
        {
            Debug.LogError(
                "FloodFeedbackTester: ShelterFeedbackDataBuilder is not assigned."
            );

            return;
        }

        if (feedbackPersona == null)
        {
            Debug.LogError(
                "FloodFeedbackTester: Feedback Persona is not assigned."
            );

            return;
        }

        if (LLMManager.Instance == null)
        {
            Debug.LogError(
                "FloodFeedbackTester: LLMManager.Instance was not found."
            );

            return;
        }

        string validatedFacts =
            shelterDataBuilder.BuildValidatedShelterFacts();

        string prompt =
            "Analyze the shelter mitigation situation using the supplied JSON " +
            "and the validated calculations below.\n\n" +

            "JSON DATA:\n" +
            gameStateJson.text +
            "\n\n" +

            validatedFacts +
            "\n\n" +

            "Instructions:\n" +
            "- Treat the validated calculations as authoritative.\n" +
            "- Do not recalculate shelter capacity.\n" +
            "- Do not contradict the validated shelter status.\n" +
            "- Do not invent missing information.\n" +
            "- Explain what the player did well.\n" +
            "- Identify the most serious shelter problem.\n" +
            "- Recommend practical next actions.\n" +
            "- Write between 120 and 160 words.\n" +
            "- Use clear player-facing language.";

        Debug.Log(
            "Sending validated shelter feedback request to local LLM..."
        );

        LLMManager.Instance.RequestEventResponse(
            feedbackPersona,
            prompt
        );
    }
}