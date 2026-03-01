using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ValueUpdater : MonoBehaviour
{
    [Header("Range & Current Value")]
    [SerializeField] private float minValue = -10f; // Minimum value for the resource
    [SerializeField] private float maxValue = 10f; // Maximum value for the resource
    public ResourcesData resourcesData; // Reference to CardData if needed
    [SerializeField] private ResourceType resourceName; // Name of the resource to be colorized
    [SerializeField] private float currentValue = 0f; // Current value to be colorized

    public TMP_Text text;

    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        text = GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = currentValue.ToString("F0"); // Format the value as needed
        }
    }

    public void OnEnable()
    {
        if (resourcesData != null)
        {
            resourcesData.OnResourcesChanged.AddListener(OnResourceChanged);
        }
        currentValue = resourcesData.GetResourceValue(resourceName.ToString());
        SetValue(currentValue);
    }

    public void OnDisable()
    {
        if (resourcesData != null)
        {
            resourcesData.OnResourcesChanged.RemoveListener(OnResourceChanged);
        }
    }

    private void OnResourceChanged(string changedResource, int newValue)
    {
        if (changedResource == resourceName.ToString())
        {
            SetValue(newValue);
        }
    }
}
