using UnityEngine;

[System.Serializable]
public class AlertStyleEntry
{
    public AlertType type;
    public Color mainColor;
    public Color accentColor;
    public Color iconBackgroundColor;
    public Sprite icon;
}

[CreateAssetMenu(menuName = "UI/Alert Style Library")]
public class AlertStyleLibrary : ScriptableObject
{
    public AlertStyleEntry[] entries;

    public AlertStyleEntry GetStyle(AlertType type)
    {
        foreach (var e in entries)
        {
            if (e.type == type) return e;
        }

        Debug.LogWarning($"No style found for AlertType {type}, using first entry as fallback.");
        return entries.Length > 0 ? entries[0] : null;
    }
}
