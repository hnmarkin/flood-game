using UnityEngine;

public class FloodDefenseFeedbackDataBuilder : MonoBehaviour
{
    [Header("Existing Flood Defense System")]
    [SerializeField] private FloodDefenseBoxStamp floodDefenseSystem;

    public string BuildValidatedFloodDefenseFacts()
    {
        if (floodDefenseSystem == null)
        {
            Debug.LogError(
                "FloodDefenseFeedbackDataBuilder: FloodDefenseBoxStamp is not assigned."
            );

            return "";
        }

        int barriersPlaced =
            floodDefenseSystem.BarriersPlaced;

        int maximumBarriers =
            floodDefenseSystem.ActualMaxZoneBarriers;

        int barriersRemaining =
            Mathf.Max(0, maximumBarriers - barriersPlaced);

        bool allBarriersPlaced =
            barriersPlaced >= maximumBarriers;

        string validatedFacts =
            "VALIDATED FLOOD DEFENSE FACTS:\n\n" +

            "- Barriers placed: " +
            barriersPlaced + "\n" +

            "- Maximum available barriers: " +
            maximumBarriers + "\n" +

            "- Barriers remaining: " +
            barriersRemaining + "\n" +

            "- All available barriers placed: " +
            allBarriersPlaced + "\n\n" +

            "IMPORTANT:\n" +
            "- These values come directly from the Unity flood defense system.\n" +
            "- Treat these values as authoritative.\n" +
            "- Do not recalculate or contradict these values.\n" +
            "- Do not invent barrier locations.\n" +
            "- Do not invent protected zones.\n" +
            "- Do not invent flood depths or water levels.\n" +
            "- Do not claim a barrier prevented flooding unless the supplied data proves it.\n";

        Debug.Log(validatedFacts);

        return validatedFacts;
    }
}
