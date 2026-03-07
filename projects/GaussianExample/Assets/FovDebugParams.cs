using UnityEngine;

public class FovDebugParams : MonoBehaviour
{
    public bool debugTint = true;
    public KeyCode toggleTintKey = KeyCode.H;

    public Vector3 radii = new Vector3(0.12f, 0.22f, 0.35f);
    public bool useMouse = true;

    public float debugKeepLayer = -1f; // 你之前加的那个（如果有）

    void Update()
    {
        if (Input.GetKeyDown(toggleTintKey))
            debugTint = !debugTint;

        Vector2 gazeUV = useMouse
            ? new Vector2(Mathf.Clamp01(Input.mousePosition.x / Screen.width),
                          Mathf.Clamp01(Input.mousePosition.y / Screen.height))
            : new Vector2(0.5f, 0.5f);

        Shader.SetGlobalVector("_GazeUV", gazeUV);
        Shader.SetGlobalVector("_FovRadii", radii);

        Shader.SetGlobalFloat("_DebugFovTint", debugTint ? 1f : 0f);
        Shader.SetGlobalFloat("_DebugKeepLayer", debugKeepLayer);
    }
}