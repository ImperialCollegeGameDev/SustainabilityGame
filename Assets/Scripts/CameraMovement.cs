using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public float cameraMoveSpeed = 2.0f;
    Vector3 currentVelocity;
    [SerializeField] public Transform tiltTarget;       // whatever you want to rotate - i.e. ground (and its children as buildings)

    [Header("Movement Bounds")]
    public float minX = -30f;
    public float maxX = -10f;
    public float minZ = -30f;
    public float maxZ = -10f;

    void Start()
    {
        
    }

    void Update()
    {
        HandleCameraMovement();
        HandleTilt();
        HandleZoom();
    }

    void HandleCameraMovement()
    {
        // 'Move' is a project-wide input action mapped to WASD and controller left stick. Get the Vector2.
        Vector2 cameraMoveAction = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();

        // smooth movement
        Vector3 targetDir = transform.TransformDirection(cameraMoveAction.x, 0f, cameraMoveAction.y);
        targetDir.y = 0f;
        Vector3 targetVelocity = targetDir * cameraMoveSpeed;
        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            Time.deltaTime * 8f);

        transform.position += currentVelocity * Time.deltaTime;

        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        transform.position = pos;
    }

    void HandleTilt()
    {
        float maxAngle = 5f;     // degrees
        float smooth = 5f;

        if (Mouse.current == null || tiltTarget == null) return;

        Vector2 mouse = Mouse.current.position.ReadValue();

        float mx = (mouse.x / Screen.width - 0.5f) * 2f;
        float my = (mouse.y / Screen.height - 0.5f) * 2f;

        Quaternion target =
            Quaternion.Euler(my * maxAngle, mx * maxAngle, 0f);

        tiltTarget.localRotation =
            Quaternion.Slerp(tiltTarget.localRotation, target, Time.deltaTime * smooth);
    }

    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        GetComponent<Camera>().orthographicSize -= scroll * 40f * Time.deltaTime;
        GetComponent<Camera>().orthographicSize = Mathf.Clamp(GetComponent<Camera>().orthographicSize, 1f, 20f);
    }
}
