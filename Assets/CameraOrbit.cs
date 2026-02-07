using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private Vector3 orbitCenter = Vector3.zero;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 50f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Input Settings")]
    [SerializeField] private bool useMouseDrag = true;
    [SerializeField] private int dragMouseButton = 1; // Right mouse button
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private bool invertYAxis = false;

    [Header("Smoothing")]
    [SerializeField] private float rotationDamping = 0.1f;
    [SerializeField] private bool useSmoothing = true;

    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private float targetYaw = 0f;
    private float targetPitch = 20f;

    void Start()
    {
        // Hide and lock cursor for camera control
        //Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.None;

        // Initialize rotation based on current camera position relative to orbit center
        Vector3 direction = transform.position - orbitCenter;
        distance = direction.magnitude;

        currentYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        currentPitch = Mathf.Atan2(direction.y, horizontalDistance) * Mathf.Rad2Deg;

        targetYaw = currentYaw;
        targetPitch = currentPitch;
    }

    void Update()
    {
        HandleInput();
        UpdateRotation();
        UpdateCameraPosition();
    }

    private void HandleInput()
    {
        // Toggle cursor lock with Escape
        //if (Input.GetKeyDown(KeyCode.Escape))
        //{
        //    if (Cursor.lockState == CursorLockMode.Locked)
        //    {
        //        Cursor.visible = true;
        //        Cursor.lockState = CursorLockMode.None;
        //    }
        //    else
        //    {
        //        Cursor.visible = false;
        //        Cursor.lockState = CursorLockMode.Locked;
        //    }
        //}

        // Only handle mouse input when cursor is locked
        //if (Cursor.lockState != CursorLockMode.Locked)
        //{
        //    return;
        //}

        // Handle mouse drag rotation
        if (useMouseDrag && Input.GetMouseButton(dragMouseButton))
        {
            float deltaX = Input.GetAxis("Mouse X");
            float deltaY = Input.GetAxis("Mouse Y");

            targetYaw += deltaX * rotationSpeed * Time.deltaTime;
            targetPitch += (invertYAxis ? deltaY : -deltaY) * rotationSpeed * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        // Handle zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }
    }

    private void UpdateRotation()
    {
        if (useSmoothing)
        {
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, rotationDamping);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationDamping);
        }
        else
        {
            currentYaw = targetYaw;
            currentPitch = targetPitch;
        }
    }

    private void UpdateCameraPosition()
    {
        float yawRad = currentYaw * Mathf.Deg2Rad;
        float pitchRad = currentPitch * Mathf.Deg2Rad;

        float horizontalDistance = distance * Mathf.Cos(pitchRad);
        float height = distance * Mathf.Sin(pitchRad);

        float xOffset = horizontalDistance * Mathf.Sin(yawRad);
        float zOffset = horizontalDistance * Mathf.Cos(yawRad);

        Vector3 newPosition = orbitCenter + new Vector3(xOffset, height, zOffset);
        transform.position = newPosition;
        transform.LookAt(orbitCenter);
    }

    /// <summary>
    /// Set the orbit center programmatically
    /// </summary>
    public void SetOrbitCenter(Vector3 center)
    {
        orbitCenter = center;
    }

    /// <summary>
    /// Set the camera distance from the orbit center
    /// </summary>
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }

    /// <summary>
    /// Instantly rotate to a specific pitch and yaw
    /// </summary>
    public void SetRotation(float yaw, float pitch)
    {
        targetYaw = yaw;
        targetPitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (!useSmoothing)
        {
            currentYaw = targetYaw;
            currentPitch = targetPitch;
        }
    }
}
