using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FoveatedCompositeBlit : MonoBehaviour
{
    public Material compositeMat;
    public RigMouseLookModeController rigController;
    public bool flipY = false;

    [Header("Continuous Level Params")]
    public float maxRadius = 0.48f;
    public float levelGamma = 2.2f;
    public float startBlend = 0.30f;
    public float blendWidth = 0.35f;

    [Header("Debug")]
    public bool debugTint = false;
    [Range(0f, 1f)] public float tintStrength = 0.35f;

    private Vector2 cachedGazeUV = new Vector2(0.5f, 0.5f);
    public Vector2 CurrentGazeUV => cachedGazeUV;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (compositeMat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        bool observeMode = true;
        if (rigController != null)
            observeMode = rigController.IsObserveMode;

        if (observeMode)
        {
            cachedGazeUV = new Vector2(
                Mathf.Clamp01(Input.mousePosition.x / Screen.width),
                Mathf.Clamp01(Input.mousePosition.y / Screen.height)
            );
        }

        Vector2 gazeUV = cachedGazeUV;

        if (flipY)
            gazeUV.y = 1.0f - gazeUV.y;

        compositeMat.SetVector("_GazeUV", new Vector4(gazeUV.x, gazeUV.y, 0, 0));
        compositeMat.SetFloat("_MaxRadius", maxRadius);
        compositeMat.SetFloat("_LevelGamma", levelGamma);
        compositeMat.SetFloat("_StartBlend", startBlend);
        compositeMat.SetFloat("_BlendWidth", blendWidth);

        compositeMat.SetFloat("_DebugTint", debugTint ? 1f : 0f);
        compositeMat.SetFloat("_TintStrength", tintStrength);

        Graphics.Blit(null, dest, compositeMat);
    }
}