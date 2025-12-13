using UnityEngine;
using UnityEngine.UI;

public class ToolToggleAnim : MonoBehaviour
{
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
        if (isOn) buttonAnim.MoveButtonDown();
        else buttonAnim.ReturnButtonToOriginalPosition();
    }
}
