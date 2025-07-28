using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class AutoJoinToggleGroup : MonoBehaviour
{
    void Awake()
    {
        var toggle = GetComponent<Toggle>();
        // climb the hierarchy until we meet a ToggleGroup
        toggle.group = GetComponentInParent<ToggleGroup>();
        if (toggle.group == null)
        {
            Debug.LogWarning("No ToggleGroup found in parent hierarchy. AutoJoinToggleGroup will not function correctly.");
        }
    }
}
