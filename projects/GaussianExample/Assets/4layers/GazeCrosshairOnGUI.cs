using UnityEngine;

public class GazeCrosshairOnGUI : MonoBehaviour
{
    public FoveatedCompositeBlit gazeSource;

    public Color color = Color.red;
    public float size = 24f;
    public float thickness = 3f;

    Texture2D _tex;

    void Start()
    {
        _tex = new Texture2D(1, 1);
        _tex.SetPixel(0, 0, Color.white);
        _tex.Apply();
    }

    void OnGUI()
    {
        if (gazeSource == null || _tex == null)
            return;

        Vector2 uv = gazeSource.CurrentGazeUV;

        float x = uv.x * Screen.width;
        float y = (1.0f - uv.y) * Screen.height; // OnGUI 左上角是原点

        Color oldColor = GUI.color;
        GUI.color = color;

        // 横线
        GUI.DrawTexture(
            new Rect(x - size * 0.5f, y - thickness * 0.5f, size, thickness),
            _tex
        );

        // 竖线
        GUI.DrawTexture(
            new Rect(x - thickness * 0.5f, y - size * 0.5f, thickness, size),
            _tex
        );

        GUI.color = oldColor;
    }

    void OnDestroy()
    {
        if (_tex != null)
            Destroy(_tex);
    }
}