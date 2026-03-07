using UnityEngine;

public class RigMouseLookModeController : MonoBehaviour
{
    public enum RotationAxes { MouseXAndY, MouseX, MouseY }

    [Header("Mouse Look")]
    public RotationAxes axes = RotationAxes.MouseXAndY;
    public float sensitivityX = 15f;
    public float sensitivityY = 15f;

    [Header("Clamp")]
    public float minimumX = -360f;
    public float maximumX = 360f;
    public float minimumY = -60f;
    public float maximumY = 60f;

    [Header("Mode Toggle")]
    public KeyCode toggleModeKey = KeyCode.Alpha1;
    public bool startInObserveMode = true;

    float rotationY = 0f;

    public bool IsObserveMode { get; private set; }

    void Start()
    {
        IsObserveMode = startInObserveMode;
        ApplyCursorState();

        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleModeKey))
        {
            IsObserveMode = !IsObserveMode;
            ApplyCursorState();
        }

        if (!IsObserveMode)
            return;

        if (axes == RotationAxes.MouseXAndY)
        {
            float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
        }
        else if (axes == RotationAxes.MouseX)
        {
            transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
        }
        else
        {
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
        }
    }

    void ApplyCursorState()
    {
        if (IsObserveMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}