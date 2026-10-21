using System;
using UnityEngine;

[Serializable]
public class ShelterTypeDefinition
{
    public string shelterTypeName = "Community Shelter";
    public Sprite shelterSprite;
    public int capacity = 250;
    public int cost = 100;
    [Range(0f, 2f)] public float safetyMultiplier = 1f;
    [TextArea] public string description;
}
