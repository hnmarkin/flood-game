using UnityEngine;

public class LLMTester : MonoBehaviour
{
    public LLMPersona testPersona;

    void Start()
    {
        string floodEvent = "My family is in danger—my little brother is just two years old. If the water rises any further, we won’t be able to escape in time.";

        // Call LLMManager with persona + event text
        LLMManager.Instance.RequestEventResponse(testPersona, floodEvent);
    }
}

