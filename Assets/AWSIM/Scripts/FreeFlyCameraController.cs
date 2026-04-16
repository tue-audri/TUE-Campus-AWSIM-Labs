using UnityEngine;

public class FreeFlyCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Rotation")]
    public float yawSpeed = 100f;
    public float pitchSpeed = 100f;

    private float yaw;
    private float pitch;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        // Yaw (left/right arrows)
        if (Input.GetKey(KeyCode.LeftArrow))
            yaw -= yawSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.RightArrow))
            yaw += yawSpeed * Time.deltaTime;

        // Pitch (up/down arrows)
        if (Input.GetKey(KeyCode.UpArrow))
            pitch -= pitchSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.DownArrow))
            pitch += pitchSpeed * Time.deltaTime;

        // Clamp pitch to avoid flipping
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMovement()
    {
        Vector3 move = Vector3.zero;

        // Forward / Backward (relative to orientation)
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;

        // Left / Right (strafe)
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;

        // Up / Down (world space)
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        transform.position += move * moveSpeed * Time.deltaTime;
    }
}