using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class LLMManager : MonoBehaviour
{
    public static LLMManager Instance;

    [Header("Active Personas")]
    public LLMPersona residentialPersona;
    public LLMPersona businessPersona;
    public LLMPersona governmentPersona;

    [Header("Server Settings")]
    public string serverUrl = "http://127.0.0.1:8000/event";

    [Header("UI References")]
    public Text actionText;
    public Text commentaryText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // 🚩 MAIN FUNCTION your game system will call
    // Example: LLMManager.Instance.RequestEventResponse(residentialPersona, "event text");
    public void RequestEventResponse(LLMPersona persona, string gameEvent)
    {
        StartCoroutine(SendEventToServer(persona, gameEvent));
    }

    // Build PersonaPayload from LLMPersona
    PersonaPayload BuildPersonaPayload(LLMPersona persona)
    {
        var payload = new PersonaPayload();
        payload.personaName   = persona.personaName;
        payload.personaType   = persona.personaType.ToString();
        payload.tone          = persona.tone.ToString();
        payload.urgency       = persona.urgency.ToString();
        payload.empathy       = persona.empathy.ToString();
        payload.harshness     = persona.harshness;
        payload.description   = persona.description;
        return payload;
    }

    IEnumerator SendEventToServer(LLMPersona persona, string gameEvent)
    {
        // 1) Build EventRequestPayload
        EventRequestPayload req = new EventRequestPayload();
        req.persona = BuildPersonaPayload(persona);
        req.eventText = gameEvent;

        string json = JsonUtility.ToJson(req);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("📨 Sending to Gemini backend: " + json);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ LLM /event error: " + www.error);
            }
            else
            {
                string respText = www.downloadHandler.text;
           Debug.Log("📩 LLM /event response JSON: " + respText);

                EventResultPayload result = JsonUtility.FromJson<EventResultPayload>(respText);

                Debug.Log("✅ ACTION: " + result.action);
Debug.Log("✅ COMMENTARY: " + result.commentary);

// Update UI in the scene
if (actionText != null)
{
    actionText.text = "Action: " + result.action;
}
if (commentaryText != null)
{
    commentaryText.text = "Commentary: " + result.commentary;
}

}

                // TODO: You can now:
                // - Update UI
                // - Trigger animations
                // - Modify game state
            }
        }
    }

