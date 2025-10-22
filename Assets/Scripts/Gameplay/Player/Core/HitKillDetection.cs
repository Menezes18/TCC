using UnityEngine;
using Mirror;
using System.Reflection;


public class HitKillDetection : MonoBehaviour
{
    [Header("Contexto de Morte")]
    public DeathCause cause = DeathCause.Default;
    public bool permanent = false;

    [Header("Modos de detecção")]
    public bool useTrigger = true;
    public bool useCollision = false;
    public bool usePeriodicCheck = false;

    [Header("Periodic Check (OverlapBox)")]
    public LayerMask targetMask;
    public Vector3 checkHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 checkOffset = Vector3.zero;
    public float checkInterval = 0.1f;
    private float _checkTimer;

    private void Reset()
    {
        useTrigger = true;
        useCollision = false;
        usePeriodicCheck = false;
        checkHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
        checkInterval = 0.1f;
        targetMask = ~0;
    }

    private void Update()
    {
        if (!usePeriodicCheck) return;
        _checkTimer -= Time.deltaTime;
        if (_checkTimer > 0f) return;
        _checkTimer = checkInterval;

        Vector3 center = transform.TransformPoint(checkOffset);
        var hits = Physics.OverlapBox(center, checkHalfExtents, transform.rotation, targetMask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return;
        foreach (var col in hits)
        {
            TryKill(col);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        TryKill(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!useCollision) return;
        var other = collision.collider;
        TryKill(other);
    }

    private void TryKill(Collider other)
    {
        if (other == null) return;


        var ps = other.GetComponentInParent<PlayerScript>();
        var pd = other.GetComponentInParent<PlayerData>();

        if (ps != null && ps.isOwned)
        {
            ps.OnContextualHit(cause, permanent);
        }

        if (NetworkServer.active && pd != null)
        {
            var controller = FindAnyObjectByType<MinigameController>();
            if (controller != null)
            {
                var elim = controller.GetType().GetMethod("Eliminate", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(PlayerData) }, null);
                if (elim != null)
                {
                    try { elim.Invoke(controller, new object[] { pd }); }
                    catch { /* ignore reflection errors */ }
                }
            }

            if (ps != null)
            {
                var conn = pd.GetComponent<NetworkIdentity>()?.connectionToClient;
                if (conn != null)
                {
                    ps.TargetRpcContextualDeath(conn, cause, permanent, ps.transform.position, ps.transform.rotation);
                }
            }
        }


        IHitKillable ik = other.GetComponent<IHitKillable>();
        if (ik != null)
        {
            if (permanent) ik.OnHitSpectate();
            else ik.OnHitKill();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!usePeriodicCheck) return;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Vector3 center = transform.TransformPoint(checkOffset);
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, checkHalfExtents * 2f);
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
        Gizmos.DrawWireCube(Vector3.zero, checkHalfExtents * 2f);
    }
}
