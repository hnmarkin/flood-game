using UnityEngine;

public class ShelterFeedbackDataBuilder : MonoBehaviour
{
    [Header("Shelter Test Data")]
    [SerializeField] private string shelter1Id = "S-01";
    [SerializeField] private int shelter1Capacity = 500;
    [SerializeField] private int shelter1AssignedPopulation = 650;

    [SerializeField] private string shelter2Id = "S-02";
    [SerializeField] private int shelter2Capacity = 600;
    [SerializeField] private int shelter2AssignedPopulation = 520;


    public string BuildValidatedShelterFacts()
    {
        // Calculate whether Shelter 1 is over or under capacity
        int shelter1Excess =
            Mathf.Max(0, shelter1AssignedPopulation - shelter1Capacity);

        int shelter1Available =
            Mathf.Max(0, shelter1Capacity - shelter1AssignedPopulation);


        // Calculate whether Shelter 2 is over or under capacity
        int shelter2Excess =
            Mathf.Max(0, shelter2AssignedPopulation - shelter2Capacity);

        int shelter2Available =
            Mathf.Max(0, shelter2Capacity - shelter2AssignedPopulation);


        // Calculate how many people could potentially be transferred
        // from Shelter 1 to Shelter 2.
        int possibleTransfer =
            Mathf.Min(shelter1Excess, shelter2Available);

        // Calculate how many people would still need accommodation
        // after using all available capacity in Shelter 2.
        int remainingExcess =
            shelter1Excess - possibleTransfer;


        string shelter1Status;

        if (shelter1Excess > 0)
        {
            shelter1Status =
                "OVER CAPACITY by " +
                shelter1Excess +
                " people";
        }
        else
        {
            shelter1Status =
                "UNDER CAPACITY with " +
                shelter1Available +
                " spaces remaining";
        }


        string shelter2Status;

        if (shelter2Excess > 0)
        {
            shelter2Status =
                "OVER CAPACITY by " +
                shelter2Excess +
                " people";
        }
        else
        {
            shelter2Status =
                "UNDER CAPACITY with " +
                shelter2Available +
                " spaces remaining";
        }


        string validatedFacts =
            "VALIDATED SHELTER FACTS:\n\n" +

            shelter1Id + ":\n" +
            "- Capacity: " + shelter1Capacity + "\n" +
            "- Assigned population: " + shelter1AssignedPopulation + "\n" +
            "- Status: " + shelter1Status + "\n\n" +

            shelter2Id + ":\n" +
            "- Capacity: " + shelter2Capacity + "\n" +
            "- Assigned population: " + shelter2AssignedPopulation + "\n" +
            "- Status: " + shelter2Status + "\n\n" +

            "VALIDATED REDISTRIBUTION CALCULATIONS:\n" +
            "- Excess population at " + shelter1Id + ": " +
            shelter1Excess + "\n" +

            "- Available capacity at " + shelter2Id + ": " +
            shelter2Available + "\n" +

            "- Maximum possible transfer: " +
            possibleTransfer + "\n" +

            "- Remaining excess population after maximum transfer: " +
            remainingExcess + "\n\n" +

            "IMPORTANT:\n" +
            "- These calculations were performed by Unity C# and are authoritative.\n" +
            "- Do not recalculate these values.\n" +
            "- Do not recommend transferring more than the maximum possible transfer.\n" +
            "- Do not select a specific source zone unless the game data explicitly permits it.\n";


        Debug.Log(validatedFacts);

        return validatedFacts;
    }
}