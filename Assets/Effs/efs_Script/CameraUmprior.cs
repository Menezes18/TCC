using Org.BouncyCastle.Asn1.Cmp;
using UnityEngine;

public class CameraUmprior : MonoBehaviour
{
    public Camera camera;
    void Start()
    {
        camera = GetComponent<Camera>();
        //camera.enabled = false;
        camera.targetDisplay = 2;
    }

    
}
