using UnityEngine;

public class BillboardBehaviourTool : MonoBehaviour
{
    Transform _cam;
    void Start()
    {
        _cam = Camera.main.transform;
    }
    
    void Update()
    {
        transform.forward = _cam.forward;
    }
}
