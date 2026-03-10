using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "BusMayhem/ColorPalette")]
public class ColorMatchPalette : ScriptableObject
{
    [System.Serializable]
    public struct ColorEntry
    {
        public StickmanColor color;
        public Color unityColor;
    }

    // Fields
    [SerializeField] private ColorEntry[] entries;
    private Dictionary<StickmanColor, Color> lookup;

    // Methods
    public void Initialize()
    {
        lookup = new Dictionary<StickmanColor, Color>();
        foreach (var entry in entries)
            lookup[entry.color] = entry.unityColor;
    }

    public Color GetColor(StickmanColor stickmanColor)
    {
        if (lookup == null)
            Initialize();

        if (lookup.TryGetValue(stickmanColor, out Color c))
            return c;

        Debug.LogWarning($"[ColorMatchPalette] No color found for {stickmanColor}");
        return Color.white;
    }
}

public static class ColorConverter
{
    private static ColorMatchPalette palette;

    public static void Initialize(ColorMatchPalette colorMatchPalette)
    {
        if (colorMatchPalette == null)
        {
            Debug.LogError("[ColorConverter] ColorMatchPalette is null.");
            return;
        }
        palette = colorMatchPalette;
        palette.Initialize();
    }

    public static Color GetColor(StickmanColor stickmanColor)
    {
        if (palette == null)
        {
            Debug.LogError("[ColorConverter] ColorMatchPalette not initialized. Call Initialize() first.");
            return Color.white;
        }
        return palette.GetColor(stickmanColor);
    }
}