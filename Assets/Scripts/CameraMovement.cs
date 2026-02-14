using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public float cameraMoveSpeed = 2.0f;
    public float cameraZoom = 5.0f;
    public float cameraZoomMin;
    public float cameraZoomMax;
    Transform pivotTransform;
    Vector3 currentVelocity;
    Camera camera;
    [SerializeField] public Transform tiltTarget;       // whatever you want to rotate - i.e. ground (and its children as buildings)
    public float distance = 40f;

    void Start()
    {
        pivotTransform = transform.parent;
        camera = transform.GetComponent<Camera>();
        transform.localPosition = -transform.forward * cameraZoom * distance;
    }

    void Update()
    {
        HandleCameraMovement();
        HandleCameraZoom();
        HandleTilt();
    }

    void HandleCameraZoom()
    {
        float zoomAction = InputSystem.actions.FindAction("Zoom").ReadValue<float>();
        cameraZoom = Mathf.Clamp(cameraZoom + cameraZoom * zoomAction * -0.1f, cameraZoomMin, cameraZoomMax);
        
        //transform.localPosition = -transform.forward * distance;
        camera.orthographicSize = cameraZoom;
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

        pivotTransform.position += currentVelocity * Time.deltaTime;
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
}
