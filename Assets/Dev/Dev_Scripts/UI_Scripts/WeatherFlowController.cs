using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class WeatherFlowController : MonoBehaviour
{
    [Header("Decision Screen UI Document (GameObject)")]
    [SerializeField] private GameObject decisionScreenUI;

    [Header("Weather Broadcast Text")]
    [TextArea(5, 20)]
    [SerializeField] private string weatherBroadcast =
        "Centered off the southwest coast of Florida on Sunday afternoon, a storm surge is in effect for the southern Alabama border, with winds expected to reach between 40 and 70 mph once the storm hits land. Forecasts predict Sally will hit land near New Orleans and eventually travel northeast, eventually through Alabama. This will likely be a prolonged event, according to the National Weather Service, with heavy rainfall and dangerous surf.";

    [Header("NPC Guidance")]
    [SerializeField] private string npcNameText = "Operations Lead";
    [TextArea(3, 12)]
    [SerializeField] private string npcGuidance =
        "The forecast isn't encouraging. Rising water brings difficult choices, and there's rarely a single perfect answer. Based on the current conditions, I've identified several actions that could reduce flood risk. Your goal is simple, even if the decisions are not: protect residents, preserve homes, and help the community recover faster after the storm. The right decisions now could save lives and prevent lasting damage.";

    [Header("NPC Typewriter")]
    [SerializeField] private float npcDelayBeforeStart = 0.4f;
    [SerializeField] private float npcSecondsPerChar = 0.02f;
    [SerializeField] private float npcPunctuationPausePeriod = 0.18f;
    [SerializeField] private float npcPunctuationPauseComma = 0.08f;
    [SerializeField] private bool lockNpcOkUntilTyped = true;

    private Button weatherButton;
    private VisualElement weatherModal;
    private Label weatherText;
    private Button weatherOkButton;

    // NPC modal
    private VisualElement npcModal;
    private Label npcName;
    private Label npcDialog;
    private Button npcOkButton;

    private Coroutine npcRoutine;
    private bool npcTyping;

    private void OnEnable()
    {
        // Decision screen starts hidden until flow completes
        if (decisionScreenUI != null)
            decisionScreenUI.SetActive(false);

        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;

        // Make the fullscreen HUD root NOT block world clicks
        var hudRoot = root.Q<VisualElement>("hud_root");
        if (hudRoot == null)
        {
            Debug.LogError("GameHUD: Missing #hud_root (VisualElement name must be 'hud_root').");
            hudRoot = root;
        }
        hudRoot.pickingMode = PickingMode.Ignore;

        // --- Weather queries ---
        weatherButton = root.Q<Button>("weather_button");
        weatherModal  = root.Q<VisualElement>("weather_modal");
        weatherText   = root.Q<Label>("weather_text");
        weatherOkButton = root.Q<Button>("weather_ok");

        // --- NPC queries ---
        npcModal   = root.Q<VisualElement>("npc_modal");
        npcName    = root.Q<Label>("npc_name");
        npcDialog  = root.Q<Label>("npc_dialog");
        npcOkButton = root.Q<Button>("npc_ok");

        // Safety checks
        if (weatherButton == null) Debug.LogError("GameHUD: Missing #weather_button");
        if (weatherModal == null)  Debug.LogError("GameHUD: Missing #weather_modal");
        if (weatherText == null)   Debug.LogError("GameHUD: Missing #weather_text");
        if (weatherOkButton == null) Debug.LogError("GameHUD: Missing #weather_ok");

        if (npcModal == null) Debug.LogError("GameHUD: Missing #npc_modal (add it to GameHUD.uxml)");
        if (npcName == null) Debug.LogError("GameHUD: Missing #npc_name");
        if (npcDialog == null) Debug.LogError("GameHUD: Missing #npc_dialog");
        if (npcOkButton == null) Debug.LogError("GameHUD: Missing #npc_ok");

        // Setup initial texts
        if (weatherText != null) weatherText.text = weatherBroadcast;
        if (npcName != null) npcName.text = npcNameText;
        if (npcDialog != null) npcDialog.text = "";

        // Only weather button should catch clicks while modal hidden
        if (weatherButton != null)
            weatherButton.pickingMode = PickingMode.Position;

        // Helpers to show/hide modals and stop them blocking clicks when hidden
        void SetWeatherVisible(bool visible)
        {
            if (weatherModal == null) return;
            weatherModal.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            weatherModal.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            if (weatherOkButton != null)
                weatherOkButton.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;

        }

        void SetNpcVisible(bool visible)
        {
            if (npcModal == null) return;
            npcModal.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            npcModal.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            if (npcOkButton != null)
                npcOkButton.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        // Start hidden
        SetWeatherVisible(false);
        SetNpcVisible(false);

        // Remove old listeners (avoid duplicates)
        if (weatherButton != null)
        {
            weatherButton.clicked -= ShowWeather;
            weatherButton.clicked += ShowWeather;
        }
        if (weatherOkButton != null)
        {
            weatherOkButton.clicked -= WeatherOk;
            weatherOkButton.clicked += WeatherOk;
        }
        if (npcOkButton != null)
        {
            npcOkButton.clicked -= NpcOk;
            npcOkButton.clicked += NpcOk;
        }

        void ShowWeather()
        {
            // Weather is NOT typewritten now
            if (weatherText != null) weatherText.text = weatherBroadcast;

            SetWeatherVisible(true);
        }

        void WeatherOk()
        {
            // Close weather modal
            SetWeatherVisible(false);

            // Show NPC guidance modal with typewriter
            ShowNpcGuidance(npcNameText, npcGuidance);
        }

        void NpcOk()
        {
            if (npcTyping) return; // (optional) block until finished typing

            SetNpcVisible(false);

            if (decisionScreenUI != null)
                decisionScreenUI.SetActive(true);

            // Optional: hide weather button forever after first acknowledgement
            if (weatherButton != null)
                weatherButton.style.display = DisplayStyle.None;
        }

        void ShowNpcGuidance(string name, string message)
        {
            if (npcName != null) npcName.text = name;
            if (npcDialog != null) npcDialog.text = "";

            if (npcOkButton != null && lockNpcOkUntilTyped)
                npcOkButton.SetEnabled(false);

            SetNpcVisible(true);

            StopNpcTyping();
            npcRoutine = StartCoroutine(TypeNpcRoutine(message));
        }

        void StopNpcTyping()
        {
            if (npcRoutine != null)
            {
                StopCoroutine(npcRoutine);
                npcRoutine = null;
            }
            npcTyping = false;
        }

        IEnumerator TypeNpcRoutine(string message)
        {
            npcTyping = true;

            if (npcDelayBeforeStart > 0f)
                yield return new WaitForSeconds(npcDelayBeforeStart);

            if (npcDialog == null)
            {
                npcTyping = false;
                yield break;
            }

            for (int i = 1; i <= message.Length; i++)
            {
                npcDialog.text = message.Substring(0, i);

                char ch = message[i - 1];
                float extraPause =
                    (ch == '.' || ch == '!' || ch == '?') ? npcPunctuationPausePeriod :
                    (ch == ',' || ch == ';' || ch == ':') ? npcPunctuationPauseComma : 0f;

                float wait = npcSecondsPerChar + extraPause;
                if (wait > 0f) yield return new WaitForSeconds(wait);
                else yield return null;
            }

            npcTyping = false;
            npcRoutine = null;

            if (npcOkButton != null && lockNpcOkUntilTyped)
                npcOkButton.SetEnabled(true);
        }
    }
}