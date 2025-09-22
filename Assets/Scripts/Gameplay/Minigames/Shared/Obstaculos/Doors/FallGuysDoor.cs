using System.Collections;
using Mirror;
using UnityEngine;

[AddComponentMenu("Minigames/Obstaculos/Portas/Fall Guys Door")]
public class FallGuysDoor : NetworkBehaviour
{

    [Header("Referências")]
    [SerializeField] private Collider solidCollider;
    [SerializeField] private Collider hitTrigger;
    [SerializeField] private Transform doorVisual;
    [Tooltip("Rigidbody opcional para modo Física.")]
    [SerializeField] private Rigidbody doorRb;
    [SerializeField] private Vector3 fallAxis = Vector3.forward;
    [SerializeField] private float physicsAngularImpulse = 5f;

    [Header("Fisica empurrão")]
    [SerializeField] private float physicsPushForce = 10f;
    [SerializeField] private bool disableSolidOnOpen = true;
    [SerializeField] private float disableSolidDelay = 0.15f;

    [Header("Impacto em porta falsa")] 
    [SerializeField] private float backStrength = 4f;
    [SerializeField] private float liftStrength = 1.0f;
    [SerializeField] private float stunDuration = 0.1f;

    [SyncVar] private bool isReal = false;   
    [SyncVar] private bool opened = false;

    private Quaternion _initialRot;

    private void Reset()
    {
        solidCollider = GetComponent<Collider>();
        if (solidCollider != null && solidCollider.isTrigger)
            solidCollider = null; // não usar um trigger como sólido
        doorVisual = doorVisual != null ? doorVisual : transform;
        if (doorRb == null) doorRb = GetComponent<Rigidbody>();
    }


    public override void OnStartServer()
    {
        base.OnStartServer();
        opened = false;
        RpcResetDoor();
    }

    [Server]
    public void ServerSetReal(bool value) => isReal = value;

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        HandleHitServer(other);
    }

    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        HandleHitServer(collision.collider);
    }

    [ServerCallback]
    private void OnTriggerStay(Collider other) => HandleHitServer(other);

    [Server]
    private void HandleHitServer(Collider other)
    {
        if (opened) return;
        var ps = other.transform.root.GetComponent<PlayerScript>();
        if (ps == null) return;

        if (isReal)
        {
            opened = true;
            
            Vector3 hitDir = ps.transform.forward; hitDir.y = 0f; if (hitDir == Vector3.zero) hitDir = (ps.transform.position - transform.position).normalized;
            Vector3 hitPoint = other.ClosestPoint(doorVisual != null ? doorVisual.position : transform.position);
            RpcOpenDoor(hitPoint, hitDir, physicsPushForce);
        }
        else
        {
            Vector3 dir = -ps.transform.forward; dir.y = 0f;
            if (dir == Vector3.zero) dir = (ps.transform.position - transform.position).normalized;
            ps.ServerApplyImpulse(dir, backStrength, liftStrength, stunDuration, setStagger: true);
        }
    }

    [ClientRpc]
    private void RpcResetDoor()
    {
        if (doorVisual != null) doorVisual.localRotation = _initialRot;
        if (solidCollider != null) solidCollider.enabled = true;
        if (doorRb != null)
        {
            doorRb.isKinematic = true;
            doorRb.linearVelocity = Vector3.zero;
            doorRb.angularVelocity = Vector3.zero;
        }
    }

    [ClientRpc]
    private void RpcOpenDoor(Vector3 hitPoint, Vector3 hitDir, float forceScale)
    {
        if (doorRb != null)
        {
            doorRb.isKinematic = false;
            Vector3 push = (hitDir.normalized) * Mathf.Max(0f, forceScale);
            doorRb.AddForceAtPosition(push, hitPoint, ForceMode.Impulse);
            if (physicsAngularImpulse > 0f)
            {
                Vector3 worldAxis = (doorVisual != null ? doorVisual : transform).TransformDirection(fallAxis.normalized);
                doorRb.AddTorque(worldAxis * physicsAngularImpulse, ForceMode.Impulse);
            }
        }
    
    }

}
