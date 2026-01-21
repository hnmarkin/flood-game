using UnityEngine;
using UnityEngine.Events;
using System;

[CreateAssetMenu(menuName = "Policy/Player Resources")]
public class ResourcesData : ScriptableObject
{
    [Serializable]
    public class ResourcesChangedEvent : UnityEvent<string, int> { }

    public ResourcesChangedEvent OnResourcesChanged = new ResourcesChangedEvent();

    // Backing variables
    [Header("DO NOT EDIT THESE VALUES IN THE INSPECTOR DURING PLAY MODE")]
    // These values are meant to be modified through gameplay, not directly in the inspector.
    [SerializeField] private int _money;
    [SerializeField] private int _actionPoints;
    [SerializeField] private int _residentialOpinion;
    [SerializeField] private int _corporateOpinion;
    [SerializeField] private int _politicalOpinion;

    // Public properties with event triggers
    // If you don't understand, look up Unity events and getters/setters in C#.
    // These properties will automatically trigger the OnResourcesChanged event when their values are set.
    public int Money
    {
        get => _money;
        set
        {
            _money = value;
            OnResourcesChanged.Invoke("Money", _money);
        }
    }

    public int ActionPoints
    {
        get => _actionPoints;
        set
        {
            _actionPoints = value;
            OnResourcesChanged.Invoke("ActionPoints", _actionPoints);
        }
    }

    public int ResidentialOpinion
    {
        get => _residentialOpinion;
        set
        {
            _residentialOpinion = value;
            OnResourcesChanged.Invoke("ResidentialOpinion", _residentialOpinion);
        }
    }

    public int CorporateOpinion
    {
        get => _corporateOpinion;
        set
        {
            _corporateOpinion = value;
            OnResourcesChanged.Invoke("CorporateOpinion", _corporateOpinion);
        }
    }

    public int PoliticalOpinion
    {
        get => _politicalOpinion;
        set
        {
            _politicalOpinion = value;
            OnResourcesChanged.Invoke("PoliticalOpinion", _politicalOpinion);
        }
    }

    public int GetResourceValue(string resourceName)
    {
        return resourceName switch
        {
            "Money" => Money,
            "ActionPoints" => ActionPoints,
            "ResidentialOpinion" => ResidentialOpinion,
            "CorporateOpinion" => CorporateOpinion,
            "PoliticalOpinion" => PoliticalOpinion,
            _ => throw new ArgumentException($"Unknown resource: {resourceName}")
        };
    }
}
