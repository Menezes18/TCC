using UnityEngine;
using Mirror;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
[RequireComponent(typeof(PlayerScriptBase))]
public class PartyPushSystem : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform pushOrigin;

    private PlayerScript _player;
    private Database _db;
    private CharacterController _characterController;
    private PlayerScriptBase _playerScript;
    private Vector3 _pushVelocity;
    private float _lastPushTime;
    private float _currentBounceForce;
    private float _pushStartTime;
    private Vector3 _pushDirection;
    private bool _isPushed = false;
    private float _distanceTraveled = 0f;
    private bool _targetInCone = false;
    
    private void Start()
    {
        _player = GetComponent<PlayerScript>();
        _characterController = GetComponent<CharacterController>();
        _playerScript = GetComponent<PlayerScriptBase>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.E) && CanPushNow())
        {
            CmdTryPush();
        }

        HandlePushPhysics();
        CheckForTargetsInCone();
    }

    private void CheckForTargetsInCone()
    {
        _targetInCone = false;
        
        if (pushOrigin == null) return;
        
        Collider[] hitColliders = Physics.OverlapSphere(pushOrigin.position, _db.pushRadius);
        
        foreach (Collider col in hitColliders)
        {
            if (col.gameObject == gameObject) continue;

            PlayerScriptBase targetPlayer = col.GetComponent<PlayerScriptBase>();
            
            if (targetPlayer != null)
            {
                Vector3 relativePos = col.transform.position - pushOrigin.position;
                Vector3 flatRelativePos = new Vector3(relativePos.x, 0, relativePos.z);
                float angle = Vector3.Angle(transform.forward, flatRelativePos);
                
                if (angle <= _db.pushAngle)
                {
                    _targetInCone = true;
                    break;
                }
            }
        }
    }

    private bool CanPushNow()
    {
        return _playerScript.IsInState(PlayerStates.Default) && Time.time >= _lastPushTime + _db.pushCooldown;
    }

    private void HandlePushPhysics()
    {
        if (_isPushed)
        {
            float pushProgress = (Time.time - _pushStartTime) / _db.slideDuration;
            
            if (pushProgress >= 1.0f || _distanceTraveled >= _db.pushDistance)
            {
                _isPushed = false;
                _pushVelocity = Vector3.zero;
                _distanceTraveled = 0f;
                return;
            }
            
            float remainingDistance = _db.pushDistance - _distanceTraveled;
            float currentSpeed = _db.pushForce * (remainingDistance / _db.pushDistance);
            currentSpeed = Mathf.Max(currentSpeed, _db.pushForce * 0.2f);
            
            Vector3 moveVelocity = _pushDirection * currentSpeed;
            
            if (!_characterController.isGrounded)
            {
                moveVelocity.y -= 9.81f * Time.deltaTime;
            }
            else
            {
                if (moveVelocity.y < -0.1f)
                {
                    moveVelocity.y = _currentBounceForce;
                    _currentBounceForce *= 0.5f;
                }
            }
            
            Vector3 movement = moveVelocity * Time.deltaTime;
            _characterController.Move(movement);
            
            Vector3 horizontalMovement = new Vector3(movement.x, 0, movement.z);
            _distanceTraveled += horizontalMovement.magnitude;
            
            _pushVelocity = moveVelocity;
        }
    }

    [Command]
    private void CmdTryPush()
    {
        if (!_playerScript.IsInState(PlayerStates.Default))
            return;
            
        _playerScript.State = PlayerStates.Pushing;
        _lastPushTime = Time.time;

        Collider[] hitColliders = Physics.OverlapSphere(pushOrigin.position, _db.pushRadius);
        
        foreach (Collider col in hitColliders)
        {
            if (col.gameObject == gameObject) continue;

            PartyPushSystem targetPush = col.GetComponent<PartyPushSystem>();
            PlayerScriptBase targetPlayer = col.GetComponent<PlayerScriptBase>();
            
            if (targetPush != null && targetPlayer != null)
            {
                Vector3 relativePos = col.transform.position - pushOrigin.position;
                
                Vector3 flatRelativePos = new Vector3(relativePos.x, 0, relativePos.z);
                
                // Calcula o ângulo entre a direção para frente do jogador e a direção para o alvo
                float angle = Vector3.Angle(transform.forward, flatRelativePos);
                
                // Verifica se o alvo está dentro do ângulo V na frente do jogador
                if (angle <= _db.pushAngle)
                {
                    Vector3 pushDir = transform.forward;
                    pushDir.y = 0.5f; 
                    
                    targetPush.RpcApplyPush(pushDir);
                }
            }
        }

        RpcPlayPushEffects();
        StartCoroutine(PushCooldownRoutine());
    }
    
    private IEnumerator PushCooldownRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        _playerScript.State = PlayerStates.PushCooldown;
        yield return new WaitForSeconds(_db.pushCooldown - 0.3f);
        _playerScript.State = PlayerStates.Default;
    }

    [ClientRpc]
    private void RpcApplyPush(Vector3 direction)
    {
        _playerScript.State = PlayerStates.BeingPushed;
        
        _pushDirection = direction;
        _pushStartTime = Time.time;
        _currentBounceForce = _db.bounceForce;
        _isPushed = true;
        _distanceTraveled = 0f;
        
        if (_db.pushVFX != null)
        {
            _db.pushVFX.Play();
        }
        
        if (_db.pushAudio != null)
        {
            _db.pushAudio.Play();
        }
        
        StartCoroutine(BeingPushedRoutine());
    }
    
    private IEnumerator BeingPushedRoutine()
    {
        yield return new WaitUntil(() => !_isPushed);
        _playerScript.State = PlayerStates.Default;
    }

    [ClientRpc]
    private void RpcPlayPushEffects()
    {
        if (_db.pushVFX != null)
        {
            _db.pushVFX.Play();
        }
        
        if (_db.pushAudio != null)
        {
            _db.pushAudio.Play();
        }
    }

    private void OnDrawGizmos()
    {
        if (_db.showGizmoAlways && pushOrigin != null)
        {
            DrawPushCone();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!_db.showGizmoAlways && pushOrigin != null)
        {
            DrawPushCone();
        }
    }
    private void DrawPushCone()
    {
        
        if (pushOrigin == null) return;

        Color coneColor = _playerScript != null && _playerScript.IsInState(PlayerStates.PushCooldown)
            ? _db.cooldownColor
            : (_targetInCone ? _db.targetDetectedColor : _db.normalConeColor);
        
        // Pegamos a direção do olhar da câmera
        Vector3 cameraForward = _player.cameraJogador.transform.forward;

        
        Vector3 forwardDirection = new Vector3(cameraForward.x, cameraForward.y, cameraForward.z).normalized;

#if UNITY_EDITOR
        Handles.color = coneColor;
        Handles.DrawSolidArc(pushOrigin.position, Vector3.up, 
            Quaternion.Euler(0, -_db.pushAngle, 0) * forwardDirection, 
            _db.pushAngle * 2, _db.pushRadius);
#endif

        Gizmos.color = Color.yellow;
        Vector3 leftDir = Quaternion.Euler(0, -_db.pushAngle, 0) * forwardDirection * _db.pushRadius;
        Vector3 rightDir = Quaternion.Euler(0, _db.pushAngle, 0) * forwardDirection * _db.pushRadius;

        Gizmos.DrawRay(pushOrigin.position, leftDir);
        Gizmos.DrawRay(pushOrigin.position, rightDir);
    }


}