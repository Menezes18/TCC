using UnityEngine;

[AddComponentMenu("Environment/Airship Waypoint Mover")]
public class AirshipWaypointMover : MonoBehaviour
{
    public enum PathMode { Loop, PingPong }

    [Header("Waypoints")]
    public Transform[] waypoints;
    public PathMode mode = PathMode.Loop;
    public float speed = 3f;
    [Tooltip("Suavidade da rotação para olhar o caminho")] public float rotationLerp = 2f;
    [Tooltip("Tempo parado ao chegar em cada waypoint")] public float pauseAtWaypoint = 0f;
    [Tooltip("Distância para considerar que chegou ao waypoint")] public float arriveDistance = 0.5f;
    public bool orientToPath = true;
    public bool keepUpright = true;

    [Header("Variação Vertical (Bob)")]
    public bool enableBob = true;
    public float bobAmplitude = 0.6f;
    public float bobSpeed = 0.6f;

    private Vector3 logicalPos;     // posição ao longo do caminho (sem bob)
    private Vector3 lastLogicalPos; // posição anterior (para rotação)
    private int index;
    private int dir = 1; // usado no ping-pong
    private float waitTimer;
    private float bobPhase;

    void Start()
    {
        logicalPos = transform.position;
        lastLogicalPos = logicalPos;
        bobPhase = Random.value * Mathf.PI * 2f; // fase diferente por instância
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            ApplyTransform(logicalPos);
            return;
        }

        if (waypoints.Length == 1)
        {
            StepTowards(waypoints[0] ? waypoints[0].position : logicalPos);
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            ApplyTransform(logicalPos);
            return;
        }

        Transform wp = waypoints[index];
        Vector3 target = wp ? wp.position : logicalPos;

        float dist = Vector3.Distance(logicalPos, target);
        if (dist <= arriveDistance)
        {
            AdvanceIndex();
            waitTimer = pauseAtWaypoint;
            ApplyTransform(logicalPos);
            return;
        }

        StepTowards(target);
    }

    void StepTowards(Vector3 target)
    {
        lastLogicalPos = logicalPos;
        float step = speed * Time.deltaTime;
        logicalPos = Vector3.MoveTowards(logicalPos, target, step);

        if (orientToPath)
        {
            Vector3 faceDir = (target - lastLogicalPos);
            if (keepUpright) faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 1e-6f)
            {
                Quaternion look = Quaternion.LookRotation(faceDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * rotationLerp);
            }
        }

        ApplyTransform(logicalPos);
    }

    void ApplyTransform(Vector3 basePos)
    {
        float yOff = 0f;
        if (enableBob && bobAmplitude > 0f && bobSpeed > 0f)
            yOff = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;

        transform.position = basePos + Vector3.up * yOff;
    }

    void AdvanceIndex()
    {
        if (mode == PathMode.Loop)
        {
            index = (index + 1) % waypoints.Length;
        }
        else // PingPong
        {
            index += dir;
            if (index >= waypoints.Length)
            {
                dir = -1;
                index = waypoints.Length - 2;
            }
            else if (index < 0)
            {
                dir = 1;
                index = 1;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.cyan;
        Vector3? prev = null;
        for (int i = 0; i < waypoints.Length; i++)
        {
            var w = waypoints[i];
            if (!w) continue;
            Gizmos.DrawSphere(w.position, 0.2f);
            if (prev.HasValue) Gizmos.DrawLine(prev.Value, w.position);
            prev = w.position;
        }

        if (mode == PathMode.Loop && waypoints.Length > 1)
        {
            var a = waypoints[0];
            var b = waypoints[waypoints.Length - 1];
            if (a && b) Gizmos.DrawLine(b.position, a.position);
        }
    }
}

