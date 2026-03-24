using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;  // Get the main camera's transform
    }

    private void LateUpdate()
    {
        // Make the health bar face the camera
        transform.LookAt(transform.position + cam.forward);
    }
}