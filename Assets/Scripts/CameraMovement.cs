using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public float cameraMoveSpeed = 2.0f;
    Vector3 currentVelocity;
    [SerializeField] public Transform tiltTarget;       // whatever you want to rotate - i.e. ground (and its children as buildings)

    void Start()
    {
        
    }

    void Update()
    {
        HandleCameraMovement();
        HandleTilt();
    }

    void HandleCameraMovement()
    {
        // 'Move' is a project-wide input action mapped to WASD and controller left stick. Get the Vector2.
        Vector2 cameraMoveAction = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();

        // This is the input vector transformed to the relative view of the camera.
        //Vector3 cameraDelta = transform.TransformDirection(cameraMoveAction.x, 0.0f, cameraMoveAction.y);
        // We don't want to move the camera up and down, so remove the Y component.
        //cameraDelta.y = 0.0f;
        // Now that we've removed the Y component, the vector's magnitude is less, so normalize it to make it go forward more.
        //Debug.Log(cameraDelta);
        //cameraDelta.Normalize();
        // Move it relative to time and speed.
        //transform.position += cameraDelta * cameraMoveSpeed * Time.deltaTime;

        // smooth movement
        Vector3 targetDir = transform.TransformDirection(cameraMoveAction.x, 0f, cameraMoveAction.y);
        targetDir.y = 0f;
        Vector3 targetVelocity = targetDir * cameraMoveSpeed;
        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            Time.deltaTime * 8f);

        transform.position += currentVelocity * Time.deltaTime;
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
