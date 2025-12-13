using UnityEngine;

// These enums control personality behavior
public enum PersonaType
{
    Residential,
    Business,
    Government
}

public enum Tone
{
    Soft = 0,
    Medium = 1,
    Harsh = 2
}

public enum Urgency
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum Empathy
{
    Low = 0,
    Medium = 1,
    High = 2
}

[CreateAssetMenu(fileName = "NewLLMPersona", menuName = "SurgeCity/LLM Persona")]
public class LLMPersona : ScriptableObject
{
    [Header("Persona Identity")]
    public string personaName;
    public PersonaType personaType;

    [Header("Behavior Controls")]
    public Tone tone;
    public Urgency urgency;
    public Empathy empathy;

    [Range(0, 100)]
    public int harshness;

    [TextArea(3, 6)]
    public string description;
}
