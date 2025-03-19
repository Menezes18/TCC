using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.InputSystem;
using System.Linq;

[RequireComponent(typeof(PlayerScriptBase), typeof(CharacterController))]
public class PartyPushSystem : PlayerScriptBase
{
    [Header("References")]
    [SerializeField] private Transform pushOrigin;
    [SerializeField] private PlayerControlSO _playerSO;
    [SerializeField] private Database _db;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color normalConeColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color targetDetectedColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private Color cooldownColor = new Color(0f, 0f, 1f, 0.3f);
    [SerializeField] private int coneSegments = 20;
    
    
    
    // Cached components
    private PlayerScript _player;
    private CharacterController _characterController;
    private PlayerScriptBase _playerScript;
    private PlayerManagerUI _playerManagerUI;

    // Push state
    private Vector3 _pushDirection;
    private Vector3 _pushVelocity;
    private float _pushStartTime;
    private float _lastPushTime;  // Último momento em que o push foi realizado (para o cooldown)
    private float _currentBounceForce;
    private float _distanceTraveled;
    private bool _isPushed;
    private bool _targetInCone;

    // Network sync variable para o cooldown
    [SyncVar] private bool _isInPushCooldown;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        if (_playerSO != null)
        {
            _playerSO.EventOnPush += HandlePushInput;
        }
        _playerScript.OnStateChangeEvent += HandleStateChange;
    }

    private void OnDisable()
    {
        if (_playerSO != null)
        {
            _playerSO.EventOnPush -= HandlePushInput;
        }
    }
    private void HandleStateChange(PlayerStates oldState, PlayerStates newState)
    {
        if (newState == PlayerStates.PushCooldown && isLocalPlayer && _playerManagerUI != null)
        {
            StartCoroutine(_playerManagerUI.FillOverTime(_db.pushCooldown - 0.3f));
        }
    }
    private void CacheComponents()
    {
        _player = GetComponent<PlayerScript>();
        _characterController = GetComponent<CharacterController>();
        _playerScript = GetComponent<PlayerScriptBase>();
        _playerManagerUI = GetComponent<PlayerManagerUI>();
        
        if (!pushOrigin)
        {
            Debug.LogError($"Push Origin não configurado em {gameObject.name}");
        }

        if (_db == null)
        {
            Debug.LogError($"Database não configurado em {gameObject.name}");
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (_isPushed)
        {
            HandlePushMovement();
        }
        
        UpdateTargetDetection();
    }

    private void UpdateTargetDetection()
    {
        _targetInCone = false;
        if (pushOrigin == null) return;

        var hitColliders = Physics.OverlapSphere(pushOrigin.position, _db.pushRadius);
        foreach (var col in hitColliders)
        {
            if (col.gameObject == gameObject) continue;

            var targetPlayer = col.GetComponent<PlayerScriptBase>();
            if (targetPlayer != null && IsTargetInPushCone(col.transform.position))
            {
                _targetInCone = true;
                break;
            }
        }
    }

    private void HandlePushInput(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || _isInPushCooldown)  return;
        
        CmdTryPush();
    }

    // Agora utilizando _lastPushTime e _db.pushCooldown
    private bool CanPush()
    {
        bool recargaCompleta = Time.time >= _lastPushTime + _db.pushCooldown;
        bool estadoValido = _playerScript.State == PlayerStates.Default 
                            || _playerScript.State == PlayerStates.ShootCooldown;

        return recargaCompleta && !_isInPushCooldown;
    }

    private void HandlePushMovement()
    {
        float pushProgress = (Time.time - _pushStartTime) / _db.slideDuration;
        
        if (ShouldStopPush(pushProgress))
        {
            StopPush();
            return;
        }

        Vector3 moveVelocity = CalculatePushVelocity();
        ApplyGravityAndBounce(ref moveVelocity);
        ExecuteMovement(moveVelocity);
    }

    private bool ShouldStopPush(float progress)
    {
        return progress >= 1.0f || _distanceTraveled >= _db.pushDistance;
    }

    private Vector3 CalculatePushVelocity()
    {
        float remainingDistance = _db.pushDistance - _distanceTraveled;
        float currentSpeed = Mathf.Max(_db.pushForce * (remainingDistance / _db.pushDistance), 
                                     _db.pushForce * 0.2f);
        
        return _pushDirection * currentSpeed;
    }

    private void ApplyGravityAndBounce(ref Vector3 velocity)
    {
        if (!_characterController.isGrounded)
        {
            velocity.y -= Physics.gravity.y * Time.deltaTime;
        }
        else if (velocity.y < -0.1f)
        {
            velocity.y = _currentBounceForce;
            _currentBounceForce *= 0.5f;
        }
    }

    private void ExecuteMovement(Vector3 velocity)
    {
        Vector3 movement = velocity * Time.deltaTime;
        _characterController.Move(movement);
        
        _distanceTraveled += new Vector3(movement.x, 0, movement.z).magnitude;
        _pushVelocity = velocity;
    }

    [Command(requiresAuthority = false)]
    private void CmdTryPush()
    {
        if (!CanPush()) return;

        _lastPushTime = Time.time;
        _isInPushCooldown = true;
        _playerScript.State = PlayerStates.Pushing;
    
        bool targetHit = false;
        Vector3 contactPoint = pushOrigin.position; // Default position

        var hitColliders = Physics.OverlapSphere(pushOrigin.position, _db.pushRadius);
        foreach (var col in hitColliders)
        {
            if (col.gameObject == gameObject) continue;

            var targetPush = col.GetComponent<PartyPushSystem>();
            var targetPlayer = col.GetComponent<PlayerScriptBase>();

            if (targetPush != null && targetPlayer != null && IsTargetInPushCone(col.transform.position))
            {
                Vector3 pushDir = CalculatePushDirection(col.transform.position);
                // Calculate actual contact point between pushing player and target
                contactPoint = col.ClosestPoint(pushOrigin.position);
                targetPush.RpcApplyPush(pushDir, contactPoint);
                targetHit = true;
            }
        }

        // Use the contact point for VFX, or default to push origin if no target was hit
        RpcPlayPushEffects(contactPoint);
        StartCoroutine(PushCooldownRoutine());
    }

    private bool IsTargetInPushCone(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - pushOrigin.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= _db.pushAngle;
    }

    private Vector3 CalculatePushDirection(Vector3 targetPosition)
    {
        Vector3 pushDir = transform.forward;
        pushDir.y = 0.5f;
        return pushDir.normalized;
    }

    [ClientRpc]
    private void RpcApplyPush(Vector3 direction, Vector3 pushPosition)
    {
        _playerScript.State = PlayerStates.BeingPushed;
        _pushDirection = direction;
        _pushStartTime = Time.time;
        _currentBounceForce = _db.bounceForce;
        _isPushed = true;
        _distanceTraveled = 0f;
    
        PlayEffects(pushPosition); // Pass the push position
        StartCoroutine(BeingPushedRoutine());
    }
    [Header("VFX")]
    public GameObject pushVFX;
    [ClientRpc]
    private void RpcPlayPushEffects(Vector3 pushPosition)
    {
        PlayEffects(pushPosition);
    }
    private void PlayEffects(Vector3 position)
    {
        if (pushVFX != null)
        {
            GameObject vfxInstance = Instantiate(pushVFX, position, Quaternion.identity);
            vfxInstance.transform.SetParent(vfxInstance.transform);
        }
        else
        {
            Debug.LogWarning("pushVFX is not assigned in the inspector.");
        }

        if (_db.pushAudio != null) 
            _db.pushAudio.Play();
    }


    private void StopPush()
    {
        _isPushed = false;
        _pushVelocity = Vector3.zero;
        _distanceTraveled = 0f;
        _playerScript.State = PlayerStates.Default;
    }

    private IEnumerator PushCooldownRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        _playerScript.State = PlayerStates.PushCooldown;
        yield return new WaitForSeconds(_db.pushCooldown - 0.3f);
        _playerScript.State = PlayerStates.Default;
        _isInPushCooldown = false;
    }

    private IEnumerator BeingPushedRoutine()
    {
        yield return new WaitUntil(() => !_isPushed);
        _playerScript.State = PlayerStates.Default;
    }

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (showGizmos && pushOrigin != null)
        {
            DrawPushCone();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || pushOrigin == null) return;
        DrawPushCone();
    }

    private void DrawPushCone()
    {
        if (pushOrigin == null) return;

        Color coneColor = (_playerScript != null && _playerScript.State == PlayerStates.PushCooldown)
            ? cooldownColor
            : (_targetInCone ? targetDetectedColor : normalConeColor);


        Vector3 cameraForward = _player != null && _player.cameraJogador != null 
            ? _player.cameraJogador.transform.forward 
            : transform.forward;

        Vector3 forwardDirection = cameraForward.normalized;

        #if UNITY_EDITOR
        UnityEditor.Handles.color = coneColor;
        UnityEditor.Handles.DrawSolidArc(pushOrigin.position, Vector3.up, 
            Quaternion.Euler(0, -_db.pushAngle, 0) * forwardDirection, 
            _db.pushAngle * 2, _db.pushRadius);
        #endif

        Gizmos.color = Color.yellow;
        Vector3 leftDir = Quaternion.Euler(0, -_db.pushAngle, 0) * forwardDirection * _db.pushRadius;
        Vector3 rightDir = Quaternion.Euler(0, _db.pushAngle, 0) * forwardDirection * _db.pushRadius;

        Gizmos.DrawRay(pushOrigin.position, leftDir);
        Gizmos.DrawRay(pushOrigin.position, rightDir);
    }
    #endregion
}