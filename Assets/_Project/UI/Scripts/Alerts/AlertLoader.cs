using TMPro;
using Unity.VisualScripting;
using UnityEngine;

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
    [SerializeField] private GameObject alertPrefab;
    [SerializeField] private Transform alertParent;
    [SerializeField] private TMP_Text infoText;

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
        // Get to this later
    }
}
