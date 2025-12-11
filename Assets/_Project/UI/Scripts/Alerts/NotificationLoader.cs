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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            LoadAlert();
        }
    }

    private void LoadAlert()
    {
           
    }
}
