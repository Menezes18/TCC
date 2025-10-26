using UnityEngine;

public class BobAndSway : MonoBehaviour
{
    public Transform target;

    [Header("Balanço (posição)")]
    public bool enableBob = true;
    public Vector3 bobAxis = Vector3.up;
    public float bobAmplitude = 0.25f;
    public float bobFrequency = 1.2f;
    public bool bobInLocalSpace = false;

    [Header("Oscilação (rotação)")]
    public bool enableSway = true;
    public Vector3 swayAxis = Vector3.forward;
    public float swayDegrees = 10f;
    public float swayFrequency = 0.8f;

    [Header("Tempo e Fase")]
    public bool useUnscaledTime = false;
    public float phaseOffsetPos = 0f;
    public float phaseOffsetRot = 0f;
    public bool randomizePhaseOnEnable = true;

    // Estado
    private Transform _t;
    private Vector3 _baseWorldPos;
    private Vector3 _baseLocalPos;
    private Quaternion _baseLocalRot;

    private void Reset()
    {
        target = transform;
    }

    private void OnEnable()
    {
        if (target == null) target = transform;
        _t = target;
        CacheBases();
        if (randomizePhaseOnEnable)
        {
            phaseOffsetPos += Random.Range(0f, Mathf.PI * 2f);
            phaseOffsetRot += Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void CacheBases()
    {
        if (_t == null) _t = target != null ? target : transform;
        _baseWorldPos = _t.position;
        _baseLocalPos = _t.localPosition;
        _baseLocalRot = _t.localRotation;
    }

    private void OnDisable()
    {
        if (_t == null) return;
        _t.localPosition = _baseLocalPos;
        _t.localRotation = _baseLocalRot;
    }

    private void Update()
    {
        if (_t == null) _t = target != null ? target : transform;
        var dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        var t = useUnscaledTime ? Time.unscaledTime : Time.time;

        if (enableBob && bobAmplitude > 0f && bobFrequency > 0f)
        {
            float posSin = Mathf.Sin((t * (Mathf.PI * 2f) * bobFrequency) + phaseOffsetPos);
            Vector3 offset = (bobAxis.sqrMagnitude > 1e-6f ? bobAxis.normalized : Vector3.up) * (posSin * bobAmplitude);
            if (bobInLocalSpace)
                _t.localPosition = _baseLocalPos + offset;
            else
                _t.position = _baseWorldPos + offset;
        }

        if (enableSway && swayDegrees > 0.01f && swayFrequency > 0f)
        {
            float rotSin = Mathf.Sin((t * (Mathf.PI * 2f) * swayFrequency) + phaseOffsetRot);
            float angle = rotSin * swayDegrees;
            Vector3 axis = (swayAxis.sqrMagnitude > 1e-6f ? swayAxis.normalized : Vector3.forward);
            _t.localRotation = _baseLocalRot * Quaternion.AngleAxis(angle, axis);
        }
    }
}

