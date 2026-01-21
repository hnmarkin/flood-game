using UnityEngine;

public enum UIToolMode { None, Placing }
public class ToolManager : MonoBehaviour
{
    public UIToolMode Mode { get; private set; } = UIToolMode.None;
    public ToolDefinition ActiveTool { get; private set; } = null;

    public event System.Action<UIToolMode, ToolDefinition> OnToolChanged;

    public void EnterTool(ToolDefinition tool)
    {
        Mode = UIToolMode.Placing;
        ActiveTool = tool;
        OnToolChanged?.Invoke(Mode, ActiveTool);
    }

    public void ClearTool()
    {
        Mode = UIToolMode.None;
        ActiveTool = null;
        OnToolChanged?.Invoke(Mode, null);
    }

    public bool IsPlacing => Mode == UIToolMode.Placing && ActiveTool != null;
}
