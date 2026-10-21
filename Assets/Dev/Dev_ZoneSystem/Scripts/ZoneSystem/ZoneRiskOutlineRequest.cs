using UnityEngine;

public struct ZoneRiskOutlineRequest
{
    public readonly string Geoid;
    public readonly Color Color;

    public ZoneRiskOutlineRequest(string geoid, Color color)
    {
        Geoid = geoid;
        Color = color;
    }
}
