using UnityEngine;

public class RigLook : MonoBehaviour
{
    [Header("Assign the Camera Transform here")]
    public Transform cameraTransform;

    [Header("Look Settings")]
    public float mouseSensitivity = 2.0f;
    public bool lockCursor = true;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    float yaw;
    float pitch;

    void Start()
    {
        if (cameraTransform == null)
        {
            Debug.LogError("RigLook: cameraTransform is not assigned.");
            enabled = false;
            return;
        }

        // Initialize from current rotation
        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // Mouse look (turn head)
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Apply rotations: yaw on Rig, pitch on Camera
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Optional: Esc to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}