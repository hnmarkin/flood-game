using UnityEngine;
using UnityEngine.UI;

public class LLMUIController : MonoBehaviour
{
    public Text actionText;
    public Text commentaryText;

    // This function will be called by LLMManager
    public void UpdateUI(string action, string commentary)
    {
        actionText.text = "Action: " + action;
        commentaryText.text = "Commentary: " + commentary;
    }
}
