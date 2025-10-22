using UnityEngine;
using Mirror;
using Smooth;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(SmoothSyncMirror))]
public class BallPhysics : NetworkBehaviour, IDamageable
{
    [Header("Custom Ball Physics")]
    [SerializeField, Min(0.05f)] private float radius = 0.5f;
    [SerializeField, Range(0f, 1f)] private float restitution = 0.8f;          // quique
    [SerializeField, Range(0f, 1f)] private float frictionCoefficient = 0.1f;  // atrito horizontal por segundo
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private LayerMask collisionMask = ~0;                     // colisão com mundo
    [SerializeField] private LayerMask groundMask = ~0;                        // checagem de solo
    [SerializeField] private float groundCheckOffset = 0.02f;
    [SerializeField] private float groundPadding = 0.1f;
    [SerializeField] private float velocityEpsilon = 0.01f;
    [Header("Anti-Atravessar")]
    [SerializeField, Min(1)] private int maxCollisionSubsteps = 4;             // divide deslocamento longo em subpassos
    [SerializeField, Min(1)] private int depenetrationPasses = 2;              // quantas tentativas para sair de sobreposição
    [SerializeField, Min(0f)] private float shellOffset = 0.001f;              // folga para afastar da superfície

    [Header("Push (Server)")]
    [SerializeField, Min(0f)] private float pushForce = 10f;   // força horizontal agregada por tick
    [SerializeField, Min(0f)] private float upwardForce = 5f;  // impulso vertical agregado por tick

    [Header("Condução por Corpo")] 
    [SerializeField] private bool enableBodyConduction = true;
    [SerializeField, Min(0f)] private float bodyConductionPadding = 0.1f;
    [SerializeField, Min(0f)] private float bodyConductionAcceleration = 20f;
    [SerializeField, Min(0.5f)] private float bodyConductionMaxHorizSpeed = 12f;
    [SerializeField, Range(0f, 1f)] private float bodyConductionVerticalDamping = 0.2f;
    [SerializeField, Min(0f)] private float bodyConductionLockoutAfterPush = 0.12f; // evita somar push + condução
    [SerializeField, Min(0.05f)] private float playersRefreshInterval = 0.35f;
    [Header("Lag Compensation")]
    [SerializeField, Min(0f)] private float conductionLagPadding = 0.35f; // acolchoa raio p/ clientes remotos
    [Header("Caps")]
    [SerializeField, Min(0.5f)] private float maxHorizontalSpeed = 16f;
    [SerializeField, Min(1f)] private float maxVerticalSpeed = 20f;

    private Vector3 _velocity;                // estado de velocidade (server)
    private Transform _t;                     // cache
    private Rigidbody _rb;                    // kinematic para triggers
    private SphereCollider _col;

    // aggregator de push por tick (server)
    private Vector3 _pendingPushDirSum;
    private bool _hasPendingPush;
    private float _conductionLockoutTimer;

    // cache de jogadores (server)
    private PlayerData[] _playersCache = System.Array.Empty<PlayerData>();
    private float _playersRefreshTimer;

    [SyncVar] private ulong _lastTouchSteamId; // crédito de gol
    private double _lastTouchTime;

    public float Radius => radius;

    private void Awake()
    {
        _t = transform;
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<SphereCollider>();

        // garante trigger de gol: Rigidbody cinemático + collider sólido
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        _col.isTrigger = false;
    }


    [ServerCallback]
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // cache barato de jogadores
        _playersRefreshTimer -= dt;
        if (_playersRefreshTimer <= 0f)
        {
            if (PlayerList.singleton != null && PlayerList.singleton.players != null && PlayerList.singleton.players.Count > 0)
                _playersCache = PlayerList.singleton.players.ToArray();
            else
                _playersCache = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            _playersRefreshTimer = Mathf.Max(0.05f, playersRefreshInterval);
        }

        ProcessPendingPushes();
        SimulateGravity(dt);
        ApplyFriction(dt);
        if (_conductionLockoutTimer > 0f) _conductionLockoutTimer -= dt;
        ServerBodyConduction(dt);
        LimitVelocity();
        MoveAndCollide(dt);
        CheckGroundLayer();
    }

    [ClientCallback]
    private void LateUpdate()
    {
        UpdateVisualSpin();
    }

    [Server] private void LimitVelocity()
    {
        // Limita velocidades para evitar discrepâncias visuais entre host/clients
        Vector3 hv = new Vector3(_velocity.x, 0f, _velocity.z);
        float hm = hv.magnitude;
        if (hm > maxHorizontalSpeed)
        {
            hv = hv.normalized * maxHorizontalSpeed;
            _velocity.x = hv.x; _velocity.z = hv.z;
        }
        _velocity.y = Mathf.Clamp(_velocity.y, -maxVerticalSpeed, maxVerticalSpeed);
    }

    // ======== Física ========
    [Server] private void ProcessPendingPushes()
    {
        if (!_hasPendingPush) return;
        Vector3 horizontal = new Vector3(_pendingPushDirSum.x, 0f, _pendingPushDirSum.z);
        if (horizontal.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = horizontal.normalized;
            _velocity += dir * pushForce;
            _velocity.y += upwardForce;
        }
        _pendingPushDirSum = Vector3.zero;
        _hasPendingPush = false;
        _conductionLockoutTimer = bodyConductionLockoutAfterPush;
    }

    [Server] private void SimulateGravity(float dt)
    {
        if (!IsGrounded())
            _velocity.y += gravity * dt;
    }

    [Server] private void ApplyFriction(float dt)
    {
        Vector3 hv = new Vector3(_velocity.x, 0f, _velocity.z);
        float speed = hv.magnitude;
        if (speed > velocityEpsilon)
        {
            float decel = Mathf.Clamp01(frictionCoefficient) * dt;
            float newSpeed = Mathf.Max(0f, speed - decel);
            hv = hv.normalized * newSpeed;
            _velocity.x = hv.x; _velocity.z = hv.z;
        }
        else
        {
            _velocity.x = 0f; _velocity.z = 0f;
        }
    }

    [Server] private void MoveAndCollide(float dt)
    {
        Vector3 start = _t.position;
        Vector3 disp = _velocity * dt;
        float totalDist = disp.magnitude;
        if (totalDist < Mathf.Epsilon)
        {
            _t.position = start + disp;
            return;
        }

        Vector3 dir = disp.normalized;
        int steps = Mathf.Clamp(Mathf.CeilToInt(totalDist / Mathf.Max(radius * 0.5f, 0.05f)), 1, maxCollisionSubsteps);
        float stepDist = totalDist / steps;
        Vector3 pos = start;

        for (int i = 0; i < steps; i++)
        {
            if (Physics.SphereCast(pos, radius, dir, out var hit, stepDist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + hit.normal * (radius + shellOffset);
                _velocity = Vector3.Reflect(_velocity, hit.normal) * restitution;
                if (Mathf.Abs(_velocity.y) < velocityEpsilon && IsGrounded()) _velocity.y = 0f;
                dir = _velocity.normalized;
            }
            else
            {
                pos += dir * stepDist;
            }
        }

        _t.position = pos;
        ResolveOverlaps();
    }

    [Server] private void ResolveOverlaps()
    {
        if (depenetrationPasses <= 0) return;
        for (int pass = 0; pass < depenetrationPasses; pass++)
        {
            var hits = Physics.OverlapSphere(_t.position, radius, collisionMask, QueryTriggerInteraction.Ignore);
            bool moved = false;
            for (int i = 0; i < hits.Length; i++)
            {
                var other = hits[i];
                if (other.attachedRigidbody == _rb && other == _col) continue;
                if (!other.enabled) continue;

                if (Physics.ComputePenetration(
                        _col, _t.position, _t.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 separationDir, out float separationDist))
                {
                    if (separationDist > 0f)
                    {
                        _t.position += separationDir * (separationDist + shellOffset);
                        moved = true;
                    }
                }
            }
            if (!moved) break;
        }
    }

    [Server] private void CheckGroundLayer()
    {
        Vector3 origin = _t.position + Vector3.up * groundCheckOffset;
        float checkDistance = radius + groundPadding;
        if (Physics.CheckSphere(origin, checkDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (Physics.Raycast(origin, Vector3.down, out var groundHit, checkDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                float distanceToGround = groundHit.distance - groundCheckOffset;
                if (distanceToGround < radius)
                {
                    _t.position = groundHit.point + Vector3.up * radius;
                    if (_velocity.y < 0f) _velocity.y = 0f;
                }
            }
        }
    }

    [Server] private bool IsGrounded()
    {
        Vector3 origin = _t.position + Vector3.up * groundCheckOffset;
        float checkDistance = radius + groundPadding;
        return Physics.CheckSphere(origin, checkDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    [Server] private void ServerBodyConduction(float dt)
    {
        if (!enableBodyConduction) return;
        if (_conductionLockoutTimer > 0f) return;
        if (_playersCache == null || _playersCache.Length == 0) return;

        Vector3 ballPos = _t.position;
        PlayerScript best = null;
        float bestSqr = float.MaxValue;
        float bestThreshold = 0f;

        for (int i = 0; i < _playersCache.Length; i++)
        {
            var pd = _playersCache[i]; if (pd == null) continue;
            var ps = pd.GetComponent<PlayerScript>(); if (ps == null) continue;
            if (ps.State == PlayerState.Death) continue;

            float pr = 0.5f; var cc = ps.GetComponent<CharacterController>(); if (cc) pr = Mathf.Max(0.3f, cc.radius);
            bool isRemoteClient = ps != null && ps.connectionToClient != null;
            float lagPad = isRemoteClient ? conductionLagPadding : 0f;
            float threshold = radius + pr + bodyConductionPadding + lagPad;

            Vector3 p = ps.transform.position;
            float sqr = (new Vector3(ballPos.x, 0, ballPos.z) - new Vector3(p.x, 0, p.z)).sqrMagnitude;
            if (sqr <= threshold * threshold && sqr < bestSqr)
            { bestSqr = sqr; best = ps; bestThreshold = threshold; }
        }

        if (best == null) return;

        Vector3 pbest = best.transform.position;
        Vector3 toBall = new Vector3(ballPos.x - pbest.x, 0f, ballPos.z - pbest.z);
        float dist = Mathf.Max(0.0001f, toBall.magnitude);
        Vector3 n = toBall / dist;

        float penetration = bestThreshold - dist;
        if (penetration > 0f)
        {
            _t.position += n * Mathf.Min(penetration, 0.02f);
            ballPos = _t.position;
        }

        _velocity += n * (bodyConductionAcceleration * dt);
        _velocity.y = Mathf.Lerp(_velocity.y, 0f, bodyConductionVerticalDamping * dt);

        Vector3 hv = new Vector3(_velocity.x, 0f, _velocity.z);
        if (hv.magnitude > bodyConductionMaxHorizSpeed)
        {
            hv = hv.normalized * bodyConductionMaxHorizSpeed;
            _velocity.x = hv.x; _velocity.z = hv.z;
        }
    }

    // ======== Visual ========
    [Client] private void UpdateVisualSpin()
    {
        // Estima vel. horizontal pelo delta de posição (clientes) ou usa _velocity (server)
        Vector3 hv;
        if (isServer)
            hv = new Vector3(_velocity.x, 0f, _velocity.z);
        else
        {
            hv = (transform.position - _lastVisualPos) / Mathf.Max(0.0001f, Time.deltaTime);
            hv = new Vector3(hv.x, 0f, hv.z);
        }

        float speed = hv.magnitude;
        if (speed > velocityEpsilon)
        {
            Vector3 axis = Vector3.Cross(hv.normalized, Vector3.up);
            float angularVelocity = (speed / (2f * Mathf.PI * radius)) * 360f;
            transform.Rotate(axis, angularVelocity * Time.deltaTime, Space.World);
        }
        _lastVisualPos = transform.position;
    }
    private Vector3 _lastVisualPos;

    // ======== Interface ========
    [Server] public void ReceiveDamage(DamageType damageType, Vector3 direction)
    {
        if (damageType != DamageType.Push) return;
        Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z).normalized;
        if (horizontalDir.sqrMagnitude < Mathf.Epsilon) return;
        _pendingPushDirSum += horizontalDir;
        _hasPendingPush = true;
    }

    [Server] public void ResetBall(Vector3 position)
    {
        _velocity = Vector3.zero;
        _t.position = position;
        _lastTouchSteamId = 0UL;
        _lastTouchTime = 0.0;
    }

    [Server] public void ServerRegisterTouch(ulong steamId)
    {
        _lastTouchSteamId = steamId;
        _lastTouchTime = NetworkTime.time;
    }

    [Server] public ulong GetLastTouchSteamId() => _lastTouchSteamId;
    [Server] public double GetLastTouchTime() => _lastTouchTime;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
