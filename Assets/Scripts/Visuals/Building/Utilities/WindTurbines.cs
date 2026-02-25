using UnityEngine;

public class WindTurbines : MonoBehaviour
{
    [Header("Assign turbine parts to rotate")]
    public Transform[] objectsToRotate;

    [Header("Rotation settings")]
    public Vector3 rotationAxis = Vector3.right;   // Axis of rotation
    public float rotationSpeed = 100f;          // Degrees per second

    void Update()
    {
        if (objectsToRotate == null) return;

        foreach (Transform obj in objectsToRotate)
        {
            if (obj != null)
            {
                obj.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}