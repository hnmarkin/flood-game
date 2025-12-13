using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq; // Needs Newtonsoft JSON package
public class GeminiAPIManager : MonoBehaviour

{
    [Header("Gemini API Settings")]
    public string apiKey = "AIzaSyBPG_eQBdWUxBAJbs_GWEs9u09ClUSkpn0";   // ← Replace this
    private string model = "gemini-2.0-flash";    // model name

    // This coroutine sends a prompt to Gemini and prints the reply
    public IEnumerator GenerateText(string prompt)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        // Build the JSON body that Gemini expects
        var jsonData = new JObject
        {
            ["contents"] = new JArray(
                new JObject
                {
                    ["parts"] = new JArray(
                        new JObject { ["text"] = prompt }
                    )
                }
            )
        };

        // Prepare the UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the web request
            yield return request.SendWebRequest();

            // Handle the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                string result = request.downloadHandler.text;
                Debug.Log("Full Gemini JSON response:\n" + result);

                // Extract the plain text part
                var parsed = JObject.Parse(result);
                string text = parsed["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                Debug.Log("💬 Gemini says: " + text);
            }
            else
            {
                Debug.LogError("❌ Error from Gemini: " + request.error);
            }
        }
    }
}
