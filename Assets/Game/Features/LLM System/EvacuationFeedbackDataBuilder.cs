using UnityEngine;

public class EvacuationFeedbackDataBuilder : MonoBehaviour
{
    [System.Serializable]
    public class EvacuationData
    {
        public int availableRoutes;
        public int selectedRoutes;
        public int blockedRoutes;
        public int totalPopulation;
        public int evacuatedPopulation;
    }

    [System.Serializable]
    public class EvacuationWrapper
    {
        public EvacuationData evacuation;
    }

    [Header("Evacuation Test Data")]
    [SerializeField] private TextAsset evacuationJson;

    public string BuildValidatedEvacuationFacts()
    {
        if (evacuationJson == null)
        {
            Debug.LogError("Evacuation JSON is not assigned.");
            return "";
        }

        EvacuationWrapper data =
            JsonUtility.FromJson<EvacuationWrapper>(evacuationJson.text);

        if (data == null || data.evacuation == null)
        {
            Debug.LogError("Could not parse evacuation JSON.");
            return "";
        }

        int remainingPopulation =
            Mathf.Max(
                0,
                data.evacuation.totalPopulation -
                data.evacuation.evacuatedPopulation
            );

        int usableRoutes =
            Mathf.Max(
                0,
                data.evacuation.availableRoutes -
                data.evacuation.blockedRoutes
            );

        bool routeSelectionProblem =
            data.evacuation.selectedRoutes > usableRoutes;

        string validatedFacts =
            "VALIDATED EVACUATION FACTS:\n\n" +

            "- Available routes: " +
            data.evacuation.availableRoutes + "\n" +

            "- Selected routes: " +
            data.evacuation.selectedRoutes + "\n" +

            "- Blocked routes: " +
            data.evacuation.blockedRoutes + "\n" +

            "- Usable routes: " +
            usableRoutes + "\n" +

            "- Total population: " +
            data.evacuation.totalPopulation + "\n" +

            "- Evacuated population: " +
            data.evacuation.evacuatedPopulation + "\n" +

            "- Remaining population to evacuate: " +
            remainingPopulation + "\n" +

            "- Selected routes exceed usable routes: " +
            routeSelectionProblem + "\n\n" +

            "IMPORTANT:\n" +
            "- These calculations were performed by Unity C#.\n" +
            "- Treat these values as authoritative.\n" +
            "- Do not recalculate or contradict these values.\n" +
            "- Do not invent road conditions, travel times, distances, " +
            "flood depths, or route safety.\n" +
            "- Do not recommend a specific route unless it is explicitly " +
            "provided in the data.\n";

        Debug.Log(validatedFacts);

        return validatedFacts;
    }
}