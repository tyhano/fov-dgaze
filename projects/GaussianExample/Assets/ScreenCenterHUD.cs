using UnityEngine;

public class ScreenCenterHUD : MonoBehaviour
{
    [Header("Display")]
    public bool showPixels = true;
    public bool showUV = true;

    [Header("Style")]
    public int fontSize = 18;
    public int padding = 10;

    GUIStyle _style;
    Rect _rect;

    void OnGUI()
    {
        // Lazy-init style INSIDE OnGUI (safe)
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.UpperRight
            };
        }

        // Right-top corner box (update each frame in case resolution changes)
        float width = 420f;
        float height = 80f;
        _rect = new Rect(Screen.width - width - padding, padding, width, height);

        Vector2 centerPx = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 centerUV = new Vector2(0.5f, 0.5f);

        string text = "";
        if (showPixels)
            text += $"Center(px): ({centerPx.x:F1}, {centerPx.y:F1})\n";
        if (showUV)
            text += $"Center(uv): ({centerUV.x:F3}, {centerUV.y:F3})";

        GUI.Label(_rect, text, _style);
    }
}