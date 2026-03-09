using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    public float updateInterval = 0.5f;

    private float accum = 0f;
    private int frames = 0;
    private float timeLeft;
    private float fps = 0f;
    private float ms = 0f;

    void Start()
    {
        timeLeft = updateInterval;
    }

    void Update()
    {
        timeLeft -= Time.unscaledDeltaTime;
        accum += Time.unscaledDeltaTime;
        frames++;

        if (timeLeft <= 0f)
        {
            fps = frames / accum;
            ms = (accum / frames) * 1000f;

            timeLeft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        string text = $"FPS: {fps:F1}\nFrame Time: {ms:F2} ms";
        GUI.Label(new Rect(20, 20, 250, 60), text, style);
    }
}