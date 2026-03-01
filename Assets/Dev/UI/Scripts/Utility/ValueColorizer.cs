using UnityEngine;
using UnityEngine.UI;
using TMPro;


public enum ResourceType
{
    Money,
    ActionPoints,
    ResidentialOpinion,
    CorporateOpinion,
    PoliticalOpinion
}
public class ValueColorizer : MonoBehaviour
{
    [Header("Range & Current Value")]
    [SerializeField] private float minValue = -10f;
    [SerializeField] private float maxValue = 10f;
    public ResourcesData resourcesData; // Reference to CardData if needed
    [SerializeField] private ResourceType resourceName; // Name of the resource to be colorized

    [SerializeField] private float currentValue = 0f; // Current value to be colorized

    [Header("Target Colors")]
    public TMP_Text text;
    public Color lowColor = Color.red;
    public Color highColor = Color.green;
    public Color midColor = Color.yellow;

    // Update is called once per frame
    private void UpdateColor()
    {
        float normalizedValue = Mathf.InverseLerp(minValue, maxValue, currentValue);
        Color targetColor = Color.Lerp(lowColor, highColor, normalizedValue);
        //Check if the value is in the lower or upper half of the range
        if (normalizedValue < 0.5f)
        {
            targetColor = Color.Lerp(lowColor, midColor, normalizedValue * 2);
        }
        else
        {
            targetColor = Color.Lerp(midColor, highColor, (normalizedValue - 0.5f) * 2);
        }

        if (text != null) text.color = targetColor;
    }

    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        text = GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = currentValue.ToString("F0"); // Format the value as needed
        }
        UpdateColor(); // Update the color immediately
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
