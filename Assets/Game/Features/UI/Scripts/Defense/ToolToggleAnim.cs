using UnityEngine;
using UnityEngine.UI;

public class ToolToggleAnim : MonoBehaviour
{
    [SerializeField] private ToolDefinition tool;
    [SerializeField] private ToolManager toolManager;
    
    [SerializeField] private Toggle toggle;
    [SerializeField] private ButtonClickAnimation buttonAnim;

    private void Reset() => toggle = GetComponent<Toggle>();

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isOn) {
            toolManager.EnterTool(tool);   
            buttonAnim.MoveButtonDown();
            Debug.Log("Tool selected: " + tool.toolName);
        }
        else buttonAnim.ReturnButtonToOriginalPosition();
    }
}
