using UnityEngine;


public class LoopRotate : MonoBehaviour
{
    public Transform target;
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 720f;
    public Space rotateSpace = Space.Self;
    public bool useUnscaledTime = false;
    private Transform _t;

    private void Reset()
    {
        target = transform;
    }

    private void Awake()
    {
        _t = target != null ? target : transform;
    }

    private void OnValidate()
    {
        if (target == null) target = transform;
        if (degreesPerSecond < 0f) degreesPerSecond = 0f; 
    }

    private void Update()
    {
        var dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (_t == null) _t = target != null ? target : transform;
        if (_t == null || axis.sqrMagnitude < 1e-6f) return;

        _t.Rotate(axis.normalized, degreesPerSecond * dt, rotateSpace);
    }
}

