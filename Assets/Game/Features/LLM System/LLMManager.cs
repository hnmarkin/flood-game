using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMManager : MonoBehaviour
{
    public static LLMManager Instance;

    [Header("LM Studio Settings")]
    [SerializeField] private string endpointUrl =
        "http://127.0.0.1:1234/v1/chat/completions";

    [SerializeField] private string modelName =
        "phi-3.1-mini-4k-instruct";

    [SerializeField] private int maxTokens = 320;

    [SerializeField] private float temperature = 0.2f;

    [SerializeField] private int requestTimeoutSeconds = 300;


    [Header("Optional UI Fallback")]
    [SerializeField] private LLMUIController uiController;


    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }


    [Serializable]
    private class ChatCompletionRequest
    {
        public string model;
        public ChatMessage[] messages;
        public int max_tokens;
        public float temperature;
        public bool stream;
    }


    [Serializable]
    private class ChatCompletionResponse
    {
        public Choice[] choices;
    }


    [Serializable]
    private class Choice
    {
        public ChatMessage message;
    }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (uiController == null)
        {
            uiController = FindFirstObjectByType<LLMUIController>();
        }
    }


    public void RequestEventResponse(
        LLMPersona persona,
        string eventDescription)
    {
        if (persona == null)
        {
            Debug.LogError("LLMManager: Persona is missing.");
            ShowToGameUI("AI persona is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(eventDescription))
        {
            Debug.LogError("LLMManager: Event description is empty.");
            return;
        }

        string systemMessage = BuildSystemMessage(persona);
        string userMessage = BuildUserMessage(eventDescription);

        ShowToGameUI("Thinking...");

        StartCoroutine(
            SendRequest(systemMessage, userMessage)
        );
    }


    private string BuildSystemMessage(LLMPersona persona)
    {
        return
            "You are a flood emergency advisor in a Unity flood-resilience simulation game.\n\n" +

            "Persona information:\n" +
            "Name: " + persona.personaName + "\n" +
            "Type: " + persona.personaType + "\n" +
            "Tone: " + persona.tone + "\n" +
            "Urgency: " + persona.urgency + "\n" +
            "Empathy: " + persona.empathy + "\n" +
            "Harshness: " + persona.harshness + "/100\n" +
            "Description: " + persona.description + "\n\n" +

            "Instructions:\n" +
            "- Do not begin with a greeting or address the player by persona name.\n" +
            "- Use ONLY facts explicitly provided in the player's JSON data and validated calculations.\n" +
            "- Never invent geographic, elevation, weather, infrastructure, capacity, or safety information.\n" +
            "- Never assume one zone is safer than another unless the supplied data explicitly says so.\n" +
            "- Treat validated calculations as authoritative and do not recalculate them.\n" +
            "- Check shelter capacity and assigned population carefully.\n" +
            "- Identify effective mitigation decisions and existing problems.\n" +
            "- Recommend actions only when those actions are supported by the supplied data.\n" +
            "- If the data does not establish which zone is safer, do not recommend moving people to a specific zone.\n" +
            "- Generate approximately 150 words of feedback.\n" +
            "- Reply only with final player-facing feedback.\n" +
            "- Do not include thinking, reasoning, analysis, markdown, or special tokens.\n" +
            "- Never recommend moving more people into a shelter than its available capacity.\n" +
            "- Available shelter space = capacity - assigned population.\n" +
            "- Never choose a specific zone as the source of a transfer unless the supplied data explicitly instructs you to do so.\n" +
            "- Never suggest that another shelter exists unless that shelter is explicitly present in the supplied JSON.\n" +
            "- Never invent additional shelters, evacuation locations, routes, zones, resources, or infrastructure.\n" +
            "- When recommending redistribution, refer only to shelter IDs and validated transfer quantities, not specific source zones.\n" +
            "- If additional capacity is required but no additional shelter is provided in the data, say that additional capacity is required without inventing where it should come from.\n" +
            "- Do not state that a zone is likely to flood unless the supplied data explicitly says so.\n" +
            "- Do not infer rising water, elevation, hazard severity, accessibility, or zone safety from zone names alone.\n" +
            "- Use validated redistribution calculations exactly as provided by Unity.\n" +
            "- If S-01 has 150 excess people and S-02 has 80 available spaces, state that at most 80 can be transferred and 70 would still require accommodation.\n" +
            "- If an overcrowded shelter has more excess population than another shelter has available space, explicitly state how many people can be transferred and how many still require accommodation.\n" +
            "- Do not claim that a redistribution completely solves overcrowding unless the available capacity is sufficient.\n" +
            "- Do not assign populations from one zone to another shelter unless the supplied game data explicitly permits that assignment.\n" +
            "- Describe maximum possible transfer as being limited by available shelter capacity, not as a predefined transfer limit.\n" +
            "- Never invent zone safety, elevation, distance, accessibility, or shelter eligibility information";
            
    }


    private string BuildUserMessage(string eventDescription)
    {
        return
            "Analyze the following flood mitigation game-state data.\n\n" +
            eventDescription +
            "\n\nGenerate accurate instructional feedback for the player.";
    }


    private IEnumerator SendRequest(
        string systemMessage,
        string userMessage)
    {
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            Debug.LogError("LLMManager: Endpoint URL is empty.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(modelName))
        {
            Debug.LogError("LLMManager: Model name is empty.");
            yield break;
        }


        ChatCompletionRequest requestData =
            new ChatCompletionRequest
            {
                model = modelName.Trim(),

                messages = new ChatMessage[]
                {
                    new ChatMessage
                    {
                        role = "system",
                        content = systemMessage
                    },

                    new ChatMessage
                    {
                        role = "user",
                        content = userMessage
                    }
                },

                max_tokens = maxTokens,
                temperature = temperature,
                stream = false
            };


        string jsonBody = JsonUtility.ToJson(requestData);


        Debug.Log("======================================");
        Debug.Log("LLMManager: Sending request to LM Studio");
        Debug.Log("Endpoint: " + endpointUrl);
        Debug.Log("Model: " + modelName);
        Debug.Log("Max Tokens: " + maxTokens);
        Debug.Log("REQUEST JSON: " + jsonBody);
        Debug.Log("======================================");


        using (UnityWebRequest request =
               new UnityWebRequest(
                   endpointUrl.Trim(),
                   UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw =
                Encoding.UTF8.GetBytes(jsonBody);


            request.uploadHandler =
                new UploadHandlerRaw(bodyRaw);

            request.downloadHandler =
                new DownloadHandlerBuffer();


            request.SetRequestHeader(
                "Content-Type",
                "application/json");

            request.SetRequestHeader(
                "Accept",
                "application/json");


            request.timeout =
                requestTimeoutSeconds;


            float startTime =
                Time.realtimeSinceStartup;


            Debug.Log(
                "LLMManager: Request started at " +
                DateTime.Now.ToLongTimeString()
            );


            yield return request.SendWebRequest();


            float duration =
                Time.realtimeSinceStartup - startTime;


            string rawResponse = "";

            if (request.downloadHandler != null)
            {
                rawResponse =
                    request.downloadHandler.text;
            }


            Debug.Log(
                "LLMManager: Request finished after " +
                duration.ToString("F2") +
                " seconds."
            );

            Debug.Log(
                "HTTP Response Code: " +
                request.responseCode
            );

            Debug.Log(
                "Request Result: " +
                request.result
            );

            Debug.Log(
                "Request Error: " +
                request.error
            );

            Debug.Log(
                "RAW RESPONSE: " +
                rawResponse
            );


            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                string errorMessage =
                    "Local AI error: " +
                    request.error;

                Debug.LogError(errorMessage);

                Debug.LogError(
                    "LM Studio Response: " +
                    rawResponse
                );

                ShowToGameUI(errorMessage);

                yield break;
            }


            string reply =
                ExtractReply(rawResponse);

            reply =
                CleanReply(reply);


            if (string.IsNullOrWhiteSpace(reply))
            {
                Debug.LogError(
                    "LLMManager: LM Studio returned no usable message."
                );

                ShowToGameUI(
                    "Local AI returned empty feedback."
                );

                yield break;
            }


            Debug.Log(
                "======================================"
            );

            Debug.Log(
                "LLM RESPONSE: " +
                reply
            );

            Debug.Log(
                "======================================"
            );


            ShowToGameUI(reply);
        }
    }


    private string ExtractReply(string json)
    {
        try
        {
            ChatCompletionResponse response =
                JsonUtility.FromJson<ChatCompletionResponse>(json);


            if (response == null)
            {
                Debug.LogError(
                    "LLMManager: Response object is null."
                );

                return "";
            }


            if (response.choices == null ||
                response.choices.Length == 0)
            {
                Debug.LogError(
                    "LLMManager: No choices found in LM Studio response."
                );

                return "";
            }


            if (response.choices[0] == null ||
                response.choices[0].message == null)
            {
                Debug.LogError(
                    "LLMManager: No message found in first response choice."
                );

                return "";
            }


            return
                response.choices[0].message.content;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "LLM Parse Error: " +
                e.Message
            );

            return "";
        }
    }


    private string CleanReply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return "";
        }


        reply =
            reply.Replace(
                "<|channel|>thought",
                ""
            );

        reply =
            reply.Replace(
                "<|channel|>final",
                ""
            );

        reply =
            reply.Replace(
                "<|start|>",
                ""
            );

        reply =
            reply.Replace(
                "<|end|>",
                ""
            );

        reply =
            reply.Replace(
                "Thinking Process:",
                ""
            );

        reply =
            reply.Replace(
                "Thought:",
                ""
            );

        reply =
            reply.Replace(
                "Plan:",
                ""
            );

        reply =
            reply.Replace(
                "Final Answer:",
                ""
            );


        return reply.Trim();
    }


    private void ShowToGameUI(string message)
    {
        if (uiController == null)
        {
            uiController =
                FindFirstObjectByType<LLMUIController>();
        }


        if (uiController != null)
        {
            uiController.ShowMessage(message);
        }
        else
        {
            Debug.LogWarning(
                "LLMUIController not found. " +
                "Message only shown in Console: " +
                message
            );
        }
    }
}