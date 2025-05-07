using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderLabel : MonoBehaviour
{
    //Assign Text(TMP) component
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private bool isPercentage = false;

    public void Awake()
    {
        //Get Text component on this GameObject
        label = GetComponent<TextMeshProUGUI>();
    }

    // Called automatically by the Slider (dynamic float) event
    public void UpdateLabel(float value)
    {
        if (isPercentage) {
            label.text = value.ToString("P1"); 
            //label.text += "%";
        }
        else {
            label.text = value.ToString("F0"); // e.g., 2 decimal places
        }
    }
}
