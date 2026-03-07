using UnityEngine;

public class RigRotateModeController : MonoBehaviour
{
    [Header("Assign a child pivot for pitch (vertical rotation)")]
    public Transform pitchPivot;

    [Header("Rotation Speed")]
    public float yawSpeed = 90f;   // A/D 左右转，度/秒
    public float pitchSpeed = 90f; // W/S 上下转，度/秒

    [Header("Pitch Clamp")]
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Mode Toggle")]
    public KeyCode toggleModeKey = KeyCode.Alpha1;

    [Header("Startup")]
    public bool startInObserveMode = true;

    float yaw;
    float pitch;

    public bool IsObserveMode { get; private set; }

    void Start()
    {
        if (pitchPivot == null)
        {
            Debug.LogError("RigRotateModeController: pitchPivot is not assigned.");
            enabled = false;
            return;
        }

        yaw = transform.eulerAngles.y;

        pitch = pitchPivot.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        IsObserveMode = startInObserveMode;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleModeKey))
        {
            IsObserveMode = !IsObserveMode;
        }

        if (IsObserveMode)
        {
            HandleRotation();
        }
    }

    void HandleRotation()
    {
        float yawInput = 0f;
        float pitchInput = 0f;

        if (Input.GetKey(KeyCode.A)) yawInput -= 1f;
        if (Input.GetKey(KeyCode.D)) yawInput += 1f;
        if (Input.GetKey(KeyCode.W)) pitchInput -= 1f;
        if (Input.GetKey(KeyCode.S)) pitchInput += 1f;

        yaw += yawInput * yawSpeed * Time.deltaTime;
        pitch += pitchInput * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}