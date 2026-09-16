using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class AlertViewer : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image mainBackground;
    [SerializeField] private Image accentBar;
    [SerializeField] private Image iconBackground;
    [SerializeField] private AlertStyleLibrary styleLibrary;

    [SerializeField] private Button accentButton;
    [SerializeField] private RectTransform slideRoot;
    private RectTransform _rowRT;
    private bool _closing;

    public void SetRow(RectTransform rowRT) => _rowRT = rowRT;

    private void Awake()
    {
        accentButton.onClick.AddListener(() =>
        {
            if (_closing) return;
            _closing = true;
            float duration = GetComponent<AlertAnimation>().PlaySlideOut(gameObject, new Vector2(720f, 0f));
            if (_rowRT != null) Destroy(_rowRT.gameObject, duration);
        });
    }

    public void Setup(AlertData data)
    {
        if (messageText != null) messageText.text = data.message;
        
        var style = styleLibrary != null ? styleLibrary.GetStyle(data.type) : null;

        if (iconImage != null)          iconImage.sprite = style.icon;
        if (mainBackground != null)     mainBackground.color = style.mainColor;
        if (accentBar != null)          accentBar.color = style.accentColor;
        if (iconBackground != null)      iconBackground.color = style.iconBackgroundColor;
    }
}
