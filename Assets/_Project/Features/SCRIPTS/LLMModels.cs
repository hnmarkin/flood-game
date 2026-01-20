using System;
using UnityEngine;

// This matches the Persona class used in Python
[Serializable]
public class PersonaPayload
{
    public string personaName;
    public string personaType;
    public string tone;
    public string urgency;
    public string empathy;
    public int harshness;
    public string description;
}

// This matches EventRequest in Python
[Serializable]
public class EventRequestPayload
{
    public PersonaPayload persona;
    public string eventText;
}

// This matches EventResult in Python
[Serializable]
public class EventResultPayload
{
    public string action;
    public string commentary;
}
