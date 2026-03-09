using UnityEngine;

public class ScreenCenterHUD : MonoBehaviour
{
    [Header("Display")]
    public bool showPixels = true;
    public bool showUV = true;

    [Header("Prediction")]
    [Tooltip("Predict the next-frame gaze point by linear mouse velocity extrapolation.")]
    public bool showPredictedNextFrame = true;

    [Header("Style")]
    public int fontSize = 18;
    public int padding = 10;

    GUIStyle _style;
    Rect _rect;

    Vector2 _currentMousePx;
    Vector2 _predictedMousePx;
    Vector2 _previousMousePx;
    bool _hasPrevious;

    void Update()
    {
        _currentMousePx = Input.mousePosition;

        if (showPredictedNextFrame && _hasPrevious)
        {
            Vector2 velocityPxPerFrame = _currentMousePx - _previousMousePx;
            _predictedMousePx = _currentMousePx + velocityPxPerFrame;
        }
        else
        {
            _predictedMousePx = _currentMousePx;
        }

        _previousMousePx = _currentMousePx;
        _hasPrevious = true;
    }

    void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.UpperRight
            };
        }

        float width = 520f;
        float height = 140f;
        _rect = new Rect(Screen.width - width - padding, padding, width, height);

        Vector2 currentMouseUV = ScreenToUV(_currentMousePx);
        Vector2 predictedUV = ScreenToUV(_predictedMousePx);

        string text = "";
        if (showPixels)
        {
            text += $"Mouse Gaze (px): ({_currentMousePx.x:F1}, {_currentMousePx.y:F1})\n";
            if (showPredictedNextFrame)
                text += $"Pred Next (px): ({_predictedMousePx.x:F1}, {_predictedMousePx.y:F1})\n";
        }

        if (showUV)
        {
            text += $"Mouse Gaze (uv): ({currentMouseUV.x:F3}, {currentMouseUV.y:F3})";
            if (showPredictedNextFrame)
                text += $"\nPred Next (uv): ({predictedUV.x:F3}, {predictedUV.y:F3})";
        }

        GUI.Label(_rect, text, _style);
    }

    Vector2 ScreenToUV(Vector2 screenPos)
    {
        return new Vector2(
            Mathf.Clamp01(screenPos.x / Screen.width),
            Mathf.Clamp01(screenPos.y / Screen.height)
        );
    }
}
