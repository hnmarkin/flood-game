using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[System.Serializable]

public enum AlertType
{
    Info,
    Warning,
    Critical,
    ChatRes,
    ChatCorp,
    ChatPol
}

public class AlertData
{
    public AlertType type;
    public string message;
}

public class AlertLoader : MonoBehaviour
{
    [SerializeField] private AlertAnimation alertAnimation;
    [SerializeField] private GameObject alertPrefab;
    [SerializeField] private Transform alertParent;
    [SerializeField] private float offsetX = 240f;
    [SerializeField] private float finalX = 0f;
    // [SerializeField] private TMP_Text infoText;

    private void OnEnable() 
    {
        AlertBus.AlertRaised += OnAlertRaised;
    }

    private void OnDisable() 
    {
        AlertBus.AlertRaised -= OnAlertRaised;
    }

    private void OnAlertRaised(AlertData alertData)
    {
        var contentRT = (RectTransform)alertParent;
        var row = new GameObject("Row", typeof(RectTransform));
        var rowRT = (RectTransform)row.transform;
        rowRT.SetParent(contentRT, false);
        rowRT.localScale = Vector3.one;

        // Instantiate alert under row
        var go = Instantiate(alertPrefab, rowRT, false);
        var rt = go.GetComponent<RectTransform>();

        var viewer = go.GetComponent<AlertViewer>();
        viewer.Setup(alertData);
        // Start indented
        rt.anchoredPosition = new Vector2(offsetX, 0f);

        // (Optional but often helpful) rebuild so sizes are correct before tween
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);

        // Animate to zero X (LeanTween example)
        alertAnimation.PlaySlideIn(go, new Vector2(finalX, 0f));
    }
}
