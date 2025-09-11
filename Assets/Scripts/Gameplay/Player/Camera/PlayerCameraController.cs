using UnityEngine;

// Fase 2: Camera dedicada (remove lógica de LateUpdate do PlayerScript posteriormente).
public class PlayerCameraController
{
    private readonly Transform _cameraTransform;
    private readonly Database _db;
    private Transform _target;

    public PlayerCameraController(Transform cam, Database db, Transform initialTarget)
    {
        _cameraTransform = cam;
        _db = db;
        _target = initialTarget;
    }

    public void SetTarget(Transform t) => _target = t;

    public void Tick(float pitch, float yaw)
    {
        if (_cameraTransform == null || _target == null) return;
        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0f);
        _cameraTransform.rotation = camRotation;

        Vector3 desiredPos = _target.position + _cameraTransform.rotation * _db.orbitalOffset;
        Vector3 dir = desiredPos - _target.position;
        float maxDist = _db.orbitalOffset.magnitude;

        if (Physics.SphereCast(_target.position, _db.cameraSphereRadius, dir.normalized,
                out RaycastHit hit, maxDist, _db.cameraColliderMash, QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Clamp(hit.distance - _db.cameraSphereRadius, 0.1f, maxDist);
            _cameraTransform.position = _target.position + dir.normalized * safeDist;
        }
        else
        {
            _cameraTransform.position = desiredPos;
        }
    }
}
