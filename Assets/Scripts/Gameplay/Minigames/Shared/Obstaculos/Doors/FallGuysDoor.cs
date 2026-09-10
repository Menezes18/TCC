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
    [Header("Ocultação da porta real")]
    [SerializeField, Tooltip("Tempo após abrir antes de iniciar o sumiço.")] private float hideDelaySeconds = 2f;
    [SerializeField, Tooltip("Duração do scale down até o tamanho mínimo.")] private float hideShrinkDuration = 0.35f;
    [SerializeField, Tooltip("Escala final usada ao esconder a porta.")] private Vector3 hiddenScale = Vector3.one * 0.01f;

    [Header("Fisica empurrão")]
    [SerializeField] private float physicsPushForce = 10f;
    [SerializeField] private bool disableSolidOnOpen = true;
    [SerializeField] private float disableSolidDelay = 0.15f;

    [Header("Impacto em porta falsa")] 
    [SerializeField] private float backStrength = 4f;
    [SerializeField] private float liftStrength = 1.0f;
    [SerializeField] private float stunDuration = 0.1f;

    private bool isReal = false;
    [SyncVar(hook = nameof(OnOpenedChanged))] private bool opened = false;

    private Quaternion _initialRot;
    private Vector3 _initialScale;
    private bool _hideRoutineStarted;
    private Coroutine _localHideCoroutine;
    private Coroutine _serverHideCoroutine;

    private void Awake()
    {
        doorVisual = doorVisual != null ? doorVisual : transform;
        if (doorRb == null) doorRb = GetComponent<Rigidbody>();
        _initialRot = doorVisual.localRotation;
        _initialScale = doorVisual.localScale;
    }

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
        if (_serverHideCoroutine != null)
        {
            StopCoroutine(_serverHideCoroutine);
            _serverHideCoroutine = null;
        }
        opened = false;
        _hideRoutineStarted = false;
        ApplyAuthoritativeCollisionState(false);
        RpcResetDoor();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (opened)
            ApplyOpenedPresentation();
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
            ApplyDoorPhysics(hitPoint, hitDir, physicsPushForce);
            if (hitTrigger != null) hitTrigger.enabled = false;
            if (disableSolidOnOpen)
                StartCoroutine(ServerDisableSolidAfterDelay());
            RpcOpenDoor(hitPoint, hitDir, physicsPushForce);
            if (!_hideRoutineStarted)
                _serverHideCoroutine = StartCoroutine(ServerHideDoorRoutine());
        }
        else
        {
            // Direção do empurrão: do centro da porta para o ponto de impacto do player
            Vector3 hitPoint = other.ClosestPoint(doorVisual != null ? doorVisual.position : transform.position);
            Vector3 dir = (ps.transform.position - hitPoint).normalized;
            dir.y = 0f;
            if (dir == Vector3.zero)
                dir = (ps.transform.position - transform.position).normalized;
            ps.ServerApplyImpulse(dir, backStrength, liftStrength, stunDuration, setStagger: true);
        }
    }

    [ClientRpc]
    private void RpcResetDoor()
    {
        _hideRoutineStarted = false;
        if (_localHideCoroutine != null)
        {
            StopCoroutine(_localHideCoroutine);
            _localHideCoroutine = null;
        }
        if (doorVisual != null)
        {
            if (doorVisual != transform)
                doorVisual.gameObject.SetActive(true);
            doorVisual.localScale = _initialScale;
        }
        if (doorVisual != null) doorVisual.localRotation = _initialRot;
        if (solidCollider != null) solidCollider.enabled = true;
        if (hitTrigger != null) hitTrigger.enabled = true;
        if (doorRb != null)
        {
            doorRb.isKinematic = true;
            doorRb.linearVelocity = Vector3.zero;
            doorRb.angularVelocity = Vector3.zero;
        }
    }

    private void OnOpenedChanged(bool oldValue, bool newValue)
    {
        if (newValue && !isServer)
            ApplyOpenedPresentation();
    }

    private void ApplyRealMode(bool real)
    {
        if (doorRb == null) return;
        if (real)
        {
            doorRb.constraints = RigidbodyConstraints.None;
            doorRb.isKinematic = true;
        }
        else
        {
            doorRb.isKinematic = true;
            doorRb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    [ClientRpc]
    private void RpcOpenDoor(Vector3 hitPoint, Vector3 hitDir, float forceScale)
    {
        if (!isServer)
            ApplyDoorPhysics(hitPoint, hitDir, forceScale);
    }

    private void ApplyDoorPhysics(Vector3 hitPoint, Vector3 hitDir, float forceScale)
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

    private void ApplyAuthoritativeCollisionState(bool isOpen)
    {
        if (hitTrigger != null) hitTrigger.enabled = !isOpen;
        if (solidCollider != null && (isOpen && disableSolidOnOpen || !isOpen))
            solidCollider.enabled = !isOpen;
    }

    [Server]
    private IEnumerator ServerDisableSolidAfterDelay()
    {
        if (disableSolidDelay > 0f)
            yield return new WaitForSeconds(disableSolidDelay);
        if (solidCollider != null)
            solidCollider.enabled = false;
    }

    private void ApplyOpenedPresentation()
    {
        if (hitTrigger != null) hitTrigger.enabled = false;
        if (solidCollider != null && disableSolidOnOpen) solidCollider.enabled = false;
    }

    [Server]
    private IEnumerator ServerHideDoorRoutine()
    {
        _hideRoutineStarted = true;
        yield return new WaitForSeconds(hideDelaySeconds);
        ApplyAuthoritativeCollisionState(true);
        RpcShrinkAndDisable();
        _serverHideCoroutine = null;
    }

    [ClientRpc]
    private void RpcShrinkAndDisable()
    {
        if (_localHideCoroutine != null)
            StopCoroutine(_localHideCoroutine);
        _localHideCoroutine = StartCoroutine(ShrinkAndDisableLocal());
    }

    private IEnumerator ShrinkAndDisableLocal()
    {
        if (doorVisual == null)
            yield break;

        Vector3 startScale = doorVisual.localScale;
        Vector3 targetScale = hiddenScale;
        float duration = Mathf.Max(0.01f, hideShrinkDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            doorVisual.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        doorVisual.localScale = targetScale;

        if (solidCollider != null) solidCollider.enabled = false;
        if (hitTrigger != null) hitTrigger.enabled = false;
        if (doorRb != null)
        {
            doorRb.linearVelocity = Vector3.zero;
            doorRb.angularVelocity = Vector3.zero;
            doorRb.isKinematic = true;
        }

        if (doorVisual != transform)
            doorVisual.gameObject.SetActive(false);
    }

}
