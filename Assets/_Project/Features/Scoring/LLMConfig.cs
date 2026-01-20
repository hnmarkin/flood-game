// Purpose: Configuration settings for the Language Model API. ALSO JUST PASTED IN FROM API DEMO. PROBABLY WORKS FINE BUT NEEDS TO BE UPDATED TO MATCH OUR API SETTINGS.

using UnityEngine;

[CreateAssetMenu(fileName = "LLMConfig", menuName = "Config/LLMConfig")]
public class LLMConfig : ScriptableObject
{
    [Header("API Settings")]
    public string model = "gpt-3.5-turbo";
    public float temperature = 1f;
    public string apiEndpoint = "https://api.openai.com/v1/chat/completions";
    
    [Header("Rate Limiting")]
    public float minRequestInterval = 1.0f; // Minimum time between requests in seconds
    public int maxRetries = 3;             // Maximum number of retry attempts
    public float retryDelay = 2.0f;        // Delay between retries in seconds
}
