using UnityEngine;


public class AutoZoomIn : MonoBehaviour
{
    public float zoomSpeed = 5f;      
    public float minFOV = 30f;       

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (cam.fieldOfView > minFOV)
        {
            cam.fieldOfView -= zoomSpeed * Time.deltaTime;
            cam.fieldOfView = Mathf.Max(cam.fieldOfView, minFOV);
        }
    }
}
