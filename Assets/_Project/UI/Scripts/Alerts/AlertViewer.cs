using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.VersionControl;
using Unity.VisualScripting;

public class AlertViewer : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image mainBackground;
    [SerializeField] private Image accentBar;
    [SerializeField] private AlertStyleLibrary styleLibrary;

    public void Setup(AlertData data)
    {
        if (messageText != null) messageText.text = data.message;
        
        var style = styleLibrary != null ? styleLibrary.GetStyle(data.type) : null;

        if (iconImage != null)          iconImage.sprite = style.icon;
        if (mainBackground != null)     mainBackground.color = style.mainColor;
        if (accentBar != null)          accentBar.color = style.accentColor;
    }
}
