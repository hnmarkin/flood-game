/*I JUST PASTED THIS SCRIPT FROM THE API DEMO. LOTS OF THINGS DON'T WORK, FOR ONE NEWTONSOFT.JSON NEEDS TO BE INSTALLED*/

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

// Extension method to add cancellation support to UnityWebRequestAsyncOperation
public static class UnityWebRequestExtensions
{
    public static async Task<UnityWebRequest> WithCancellation(
        this UnityWebRequestAsyncOperation operation,
        CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();
        operation.completed += ao => tcs.TrySetResult(((UnityWebRequestAsyncOperation)ao).webRequest);

        using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            return await tcs.Task;
        }
    }
}

public class LLMService : MonoBehaviour
{
    [SerializeField] private LLMConfig config;
    
    private string apiKey = "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";
    private DateTime lastRequestTime;
    
    // Serializable classes for JSON handling
    [System.Serializable]
    private class ChatRequest
    {
        public string model;
        public Message[] messages;
        public float temperature;
        
        public ChatRequest(string model, Message[] messages, float temperature)
        {
            this.model = model;
            this.messages = messages;
            this.temperature = temperature;
        }
    }

    [System.Serializable]
    private class Message
    {
        public string role;
        public string content;

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [System.Serializable]
    private class ChatResponse
    {
        public Choice[] choices;
        public ErrorResponse error;
    }

    [System.Serializable]
    private class Choice
    {
        public Message message;
    }

    [System.Serializable]
    private class ErrorResponse
    {
        public string message;
        public string type;
    }

    private void Awake()
    {        
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key not found!");
        }
        else
        {
            Debug.Log($"API Key successfully retrieved: {apiKey}");
        }
    }

    public async Task<string> AskQuestion(string question, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (string.IsNullOrEmpty(question))
            throw new ArgumentException("Question cannot be empty");

        // Rate limiting
        var timeSinceLastRequest = (DateTime.Now - lastRequestTime).TotalSeconds;
        if (timeSinceLastRequest < config.minRequestInterval)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(config.minRequestInterval - timeSinceLastRequest),
                cancellationToken
            );
        }

        int retryCount = 0;
        while (retryCount <= config.maxRetries)
        {
            try
            {
                return await SendRequest(question, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                if (retryCount == config.maxRetries)
                    throw;

                retryCount++;
                await Task.Delay(
                    TimeSpan.FromSeconds(config.retryDelay * retryCount),
                    cancellationToken
                );
            }
        }

        throw new Exception("Maximum retry attempts exceeded");
    }

    private async Task<string> SendRequest(string question, CancellationToken cancellationToken)
    {
        var messages = new Message[]
        {
            new Message("system", "residents affected by the flood"),
            new Message("user", question)
        };

        var request = new ChatRequest(config.model, messages, config.temperature);
        string jsonRequest = JsonConvert.SerializeObject(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(config.apiEndpoint, "POST"))
        {            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            Debug.Log($"Authorization Header: Bearer {apiKey}");    // Debug log for the authorization header

            try
            {
                // Send request with cancellation support
                await webRequest.SendWebRequest().WithCancellation(cancellationToken);
                lastRequestTime = DateTime.Now;

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Response: {webRequest.downloadHandler.text}");  // Log the response for debugging

                    var response = JsonConvert.DeserializeObject<ChatResponse>(webRequest.downloadHandler.text);
                    
                    // Check for API-level errors
                    if (response.error != null)
                    {
                        throw new Exception($"API Error: {response.error.type} - {response.error.message}");
                    }

                    return response.choices[0].message.content;
                }
                else
                {
                    string errorDetails = string.IsNullOrEmpty(webRequest.downloadHandler.text)
                        ? webRequest.error
                        : $"{webRequest.error}: {webRequest.downloadHandler.text}";
                    
                    throw new Exception($"Request failed: {errorDetails}");
                }
            }
            finally
            {
                webRequest.uploadHandler?.Dispose();
                webRequest.downloadHandler?.Dispose();
            }
        }
    }
}