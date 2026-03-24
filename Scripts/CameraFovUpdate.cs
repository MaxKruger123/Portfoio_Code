using UnityEngine;

public class CameraFovUpdate : MonoBehaviour
{

    public Camera cam2;
    public Camera mainCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cam2.fieldOfView = mainCamera.fieldOfView;
    }
}
