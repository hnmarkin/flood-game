using UnityEngine;
using UnityEngine;

public class TestGemini : MonoBehaviour
{
    public GeminiAPIManager gemini; // drag the manager here in Inspector

    void Start()
    {
        // Start the coroutine that calls Gemini
        StartCoroutine(gemini.GenerateText("Explain reinforcement learning in simple terms"));
    }
}



