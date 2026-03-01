using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public float cameraMoveSpeed = 2.0f;
    public float cameraZoom = 40.0f;
    public float cameraZoomMin;
    public float cameraZoomMax;
    public Vector2 lowerBounds;
    public Vector2 upperBounds;
    Transform pivotTransform;
    Vector3 currentVelocity;
    Camera cam;
    private float distance = 120f;


    // base rotation the camera should return to; tilt will be applied as a small offset on top of this
    private Quaternion baseLocalRotation;

    void Start()
    {
        pivotTransform = transform.parent;
        cam = transform.GetComponent<Camera>();
        transform.localPosition = -transform.forward * distance;
        //transform.TransformDirection();

        // capture the camera's stable isometric rotation so tilt becomes a small additive offset
        baseLocalRotation = transform.localRotation;
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
        
        // keep current behavior (orthographic zoom). Position logic left unchanged.
        cam.orthographicSize = cameraZoom;
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
        pivotTransform.position = new Vector3(Mathf.Clamp(pivotTransform.position.x, lowerBounds.x, upperBounds.x), pivotTransform.position.y, Mathf.Clamp(pivotTransform.position.z, lowerBounds.y, upperBounds.y));
    }

    void HandleTilt()
    {
        float maxAngle = 3.5f;     // degrees
        float smooth = 7f;

        if (Mouse.current == null) return;

        Vector2 mouse = Mouse.current.position.ReadValue();

        float mx = (mouse.x / Screen.width - 0.5f) * 2f;
        float my = (mouse.y / Screen.height - 0.5f) * 2f;

        // Invert horizontal tilt only: keep vertical (pitch) as before, negate horizontal (yaw)
        Quaternion offset = Quaternion.Euler(-my * maxAngle, mx * maxAngle, 0f);
        Quaternion targetRot = baseLocalRotation * offset;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smooth);
    }
}
