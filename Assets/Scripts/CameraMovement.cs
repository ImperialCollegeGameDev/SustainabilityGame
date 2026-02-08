using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    private Camera camera;
    public float cameraMoveSpeed = 10.0f;

    void Start()
    {
        
    }

    void HandleCameraMovement()
    {
        // 'Move' is a project-wide input action mapped to WASD and controller left stick. Get the Vector2.
        Vector2 cameraMoveAction = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
        // This is the input vector transformed to the relative view of the camera.
        Vector3 cameraDelta = transform.TransformDirection(cameraMoveAction.x, 0.0f, cameraMoveAction.y);
        // We don't want to move the camera up and down, so remove the Y component.
        cameraDelta.y = 0.0f;
        // Now that we've removed the Y component, the vector's magnitude is less, so normalize it to make it go forward more.
        //Debug.Log(cameraDelta);
        cameraDelta.Normalize();
        // Move it relative to time and speed.
        transform.position += cameraDelta * cameraMoveSpeed * Time.deltaTime;
    }


    // Update is called once per frame
    void Update()
    {
        HandleCameraMovement();
    }
}
