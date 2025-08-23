using UnityEngine;

public class CameraUmprior : MonoBehaviour
{
    public Camera cam;
    void Start()
    {
        cam = GetComponent<Camera>();
        //cam.enabled = false;
        cam.targetDisplay = 2;
    }

    
}
