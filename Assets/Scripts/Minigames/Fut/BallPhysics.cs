using UnityEngine;
using Mirror;

public class BallPhysics : NetworkBehaviour, IDamageable
{
    [SerializeField] float radius = 0.5f;
    [SerializeField] float restitution = 0.8f;

    [Tooltip("Coeficiente de atrito horizontal por segundo")]
    [Range(0f, 1f)]
    [SerializeField] float frictionCoefficient = 0.1f;

    [SerializeField] float gravity = -9.81f;

    [SerializeField] LayerMask collisionMask = ~0;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundCheckOffset = 0.02f;
    [SerializeField] float groundPadding = 0.1f;
    [SerializeField] float pushForce = 10f;
    [SerializeField] float upwardForce = 5f;
    [SerializeField] float velocityEpsilon = 0.01f;

    private Vector3 _velocity = Vector3.zero;
    private Transform _cachedTransform;


    private LayerMask _groundMaskCombined;

    #region UNITY CALLBACKS

    private void Awake()
    {
        _cachedTransform = transform;
       
        _groundMaskCombined = groundMask;
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        SimulateGravity(dt);
        ApplyFriction(dt);
        MoveAndCollide(dt);
        CheckGroundLayer(dt);
    }

    [ClientCallback]
    private void LateUpdate()
    {
        UpdateVisualRotation();
    }

    #endregion

    #region PHYSICS METHODS

    [Server]
    private void SimulateGravity(float dt)
    {
        if (!IsGrounded())
        {
            _velocity.y += gravity * dt;
        }
    }

    [Server]
    private void ApplyFriction(float dt)
    {
        Vector3 horizontalVel = new Vector3(_velocity.x, 0f, _velocity.z);
        float speed = horizontalVel.magnitude;

        if (speed > velocityEpsilon)
        {
            float deceleration = frictionCoefficient * dt;
            float newSpeed = Mathf.MoveTowards(speed, 0f, deceleration);
            horizontalVel = horizontalVel.normalized * newSpeed;
            _velocity.x = horizontalVel.x;
            _velocity.z = horizontalVel.z;
        }
        else
        {
            _velocity.x = 0f;
            _velocity.z = 0f;
        }
    }

    [Server]
    private void MoveAndCollide(float dt)
    {
        Vector3 currentPos = _cachedTransform.position;
        Vector3 displacement = _velocity * dt;
        float dist = displacement.magnitude;

        if (dist < Mathf.Epsilon)
        {
            _cachedTransform.position = currentPos + displacement;
            return;
        }

        Vector3 dir = displacement.normalized;
        RaycastHit hitInfo;

        if (Physics.SphereCast(currentPos, radius, dir, out hitInfo, dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 hitPoint = hitInfo.point;
            Vector3 hitNormal = hitInfo.normal;

            Vector3 newPos = hitPoint + hitNormal * radius;
            _cachedTransform.position = newPos;

            _velocity = Vector3.Reflect(_velocity, hitNormal) * restitution;

            if (Mathf.Abs(_velocity.y) < velocityEpsilon && IsGrounded())
            {
                _velocity.y = 0f;
            }
        }
        else
        {
            _cachedTransform.position = currentPos + displacement;
        }
    }

    [Server]
    private void CheckGroundLayer(float dt)
    {
        Vector3 origin = _cachedTransform.position + Vector3.up * groundCheckOffset;
        float checkDistance = radius + groundPadding;

        if (Physics.CheckSphere(origin, checkDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            RaycastHit groundHit;
            if (Physics.Raycast(origin, Vector3.down, out groundHit, checkDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                float distanceToGround = groundHit.distance - groundCheckOffset; 
                if (distanceToGround < radius)
                {
                    Vector3 correctedPos = groundHit.point + Vector3.up * radius;
                    _cachedTransform.position = correctedPos;
                    if (_velocity.y < 0f)
                        _velocity.y = 0f;
                }
            }
        }
    }

    [Server]
    private bool IsGrounded()
    {
        Vector3 origin = _cachedTransform.position + Vector3.up * groundCheckOffset;
        float checkDistance = radius + groundPadding;
        return Physics.CheckSphere(origin, checkDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    #endregion

    #region VISUAL METHODS

    [Client]
    private void UpdateVisualRotation()
    {
        Vector3 horizontalVel = new Vector3(_velocity.x, 0f, _velocity.z);
        float speed = horizontalVel.magnitude;

        if (speed > velocityEpsilon)
        {
            Vector3 axis = Vector3.Cross(horizontalVel.normalized, Vector3.up);
            float angularVelocity = (speed / (2f * Mathf.PI * radius)) * 360f;
            _cachedTransform.Rotate(axis, angularVelocity * Time.deltaTime, Space.World);
        }
    }

    #endregion

    #region PUSH / KICK (IDamageable)

    [Server]
    public void ReceiveDamage(DamageType damageType, Vector3 direction)
    {
        if (damageType != DamageType.Push)
            return;

        Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z).normalized;
        if (horizontalDir.sqrMagnitude < Mathf.Epsilon)
            return;

        _velocity += horizontalDir * pushForce;
        _velocity.y += upwardForce;
    }

    #endregion
    
    private void OnDrawGizmosSelected()
    {
        
        Transform t = (Application.isPlaying ? _cachedTransform : transform);


        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(t.position, radius);


        Vector3 origin = t.position + Vector3.up * groundCheckOffset;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(origin, radius + groundPadding);
    }
}
