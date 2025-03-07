using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerScript))]
public class PlayerPushSystem : PlayerScriptBase
{
    public Database _db;
    
    [Header("Push Configuration")]
    [SerializeField] private Transform pushOrigin;
    
    [Header("Camera Control")]
    [SerializeField] private Camera playerCamera; 
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject pushEffectPrefab;
    [SerializeField] private AudioClip pushSound;
    [SerializeField] private GameObject pushIndicatorPrefab; 
    
    [Header("Debug Options")]
    [SerializeField] private bool enableDetailedLogs = false;
    [SerializeField] private LayerMask obstacleLayerMask = -1; // -1 significa "todas as camadas"
    
    private CharacterController characterController;
    private Vector3 pushVelocity;
    private float lastPushTime;
    private GameObject activeIndicator;
    
    private bool isPushRequestPending = false;
    private float pushRequestTime = 0f;
    private const float PUSH_REQUEST_TIMEOUT = 0.5f;
    
    [SyncVar(hook = nameof(OnCanPushChanged))]
    private bool canPush = true;
    private bool CanPush => canPush && Time.time >= lastPushTime + _db.pushCooldown;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }
    
    private void Start()
    {
        if (!isLocalPlayer) return;
        
        OnStateChangeEvent += HandleStateChange;
        
        
        if (playerCamera == null) Debug.LogError("Não tem camera");
        
    }
    
    private void OnDestroy()
    {
        if (isLocalPlayer)
        {
            OnStateChangeEvent -= HandleStateChange;
        }
        
        // Limpar indicadores visuais
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
        }
    }
    private void Update()
    {
        ProcessPushMovement();
        
        if (!isLocalPlayer) return;
        
        bool pushKeyPressed = Input.GetKeyDown(KeyCode.E);
        
        if (isPushRequestPending && Time.time - pushRequestTime > PUSH_REQUEST_TIMEOUT)
        {
            isPushRequestPending = false;
            if (enableDetailedLogs) Debug.Log("Push request timeout - resetando estado");
        }
        
        if (pushKeyPressed) ShowPushPreview();
        
        
        // Processar tentativa de push
        if (pushKeyPressed && CanPush && !isPushRequestPending)
        {
            lastPushTime = Time.time;
            isPushRequestPending = true;
            pushRequestTime = Time.time;
            
            if (playerCamera != null)
            {
                Vector3 pushDirection = playerCamera.transform.forward;
                
                Debug.DrawRay(pushOrigin.position, pushDirection * _db.pushRadius, Color.red, 1.0f);
                
                if (enableDetailedLogs) Debug.Log($"Enviando comando de push: direção={pushDirection}, origem={pushOrigin.position}");
                CmdAttemptPush(pushDirection);
                
                StartCoroutine(ShowLocalPushEffect(pushDirection));
            }
            else
            {
                Debug.LogWarning("Push System: Camera reference is missing!");
                CmdAttemptPush(transform.forward);
            }
        }
        
        UpdatePushCooldownVisual();
    }
    private void HandleStateChange(PlayerStates oldState, PlayerStates newState)
    {
        
        if (newState == PlayerStates.BeingPushed)
        {
            CmdSetCanPush(false);
        }
        else if (newState == PlayerStates.Default)
        {
            CmdSetCanPush(true);
        }
    }
    
    [Command]
    private void CmdSetCanPush(bool value)
    {
        canPush = value;
    }
    private void OnCanPushChanged(bool oldValue, bool newValue)
    {
        canPush = newValue;
        UpdatePushCooldownVisual();
    }
    private void ProcessPushMovement()
    {
        if (pushVelocity.magnitude > 0.1f)
        {
            if (!IsInState(PlayerStates.BeingPushed) && isLocalPlayer)
            {
                State = PlayerStates.BeingPushed;
            }
            if (characterController != null && characterController.enabled)
            {
                Vector3 movement = pushVelocity * Time.deltaTime;
                bool isGrounded = characterController.isGrounded;
                if (!isGrounded)
                {
                    movement += Physics.gravity * Time.deltaTime * 0.5f;
                }
                
                characterController.Move(movement);
            }
            
            float dampFactor = IsInState(PlayerStates.BeingPushed) ? 4f : 8f;
            pushVelocity = Vector3.Lerp(pushVelocity, Vector3.zero, Time.deltaTime * dampFactor);
            
            if (pushVelocity.magnitude < 0.3f && isLocalPlayer && IsInState(PlayerStates.BeingPushed))
            {
                CmdResetStateIfBeingPushed();
                pushVelocity = Vector3.zero; 
            }
        }
    }
    
    // Efeito local 
    private IEnumerator ShowLocalPushEffect(Vector3 direction)
    {
        if (pushEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                pushEffectPrefab,
                pushOrigin.position + direction * (_db.pushRadius * 0.5f),
                Quaternion.LookRotation(direction)
            );
            effect.transform.localScale = Vector3.one * _db.pushRadius * 0.3f;
            Destroy(effect, 0.5f);
        }
        
        // (shake leve) TODO mudar depois não esta funcionando muito bem:
        if (playerCamera != null)
        {
            Vector3 originalPos = playerCamera.transform.localPosition;
            for (float t = 0; t < 0.2f; t += Time.deltaTime)
            {
                playerCamera.transform.localPosition = originalPos + Random.insideUnitSphere * 0.05f;
                yield return null;
            }
            playerCamera.transform.localPosition = originalPos;
        }
        
        // Resetar flag após um pequeno delay
        yield return new WaitForSeconds(0.3f);
        isPushRequestPending = false;
    }
    
    [Command]
    private void CmdResetStateIfBeingPushed()
    {
        if (IsInState(PlayerStates.BeingPushed))
        {
            State = PlayerStates.Default;
        }
    }
    
    private void ShowPushPreview()
    {
        // preview da área de push
        if (playerCamera != null)
        {
            Vector3 direction = playerCamera.transform.forward;
            
            Debug.DrawRay(pushOrigin.position, direction * _db.pushRadius, 
                CanPush ? Color.green : Color.red, 0.5f);
        }
    }
    
    private void UpdatePushCooldownVisual()
    {
        //atualizar uma UI de cooldown 
        float remainingCooldown = (lastPushTime + _db.pushCooldown) - Time.time;
        float cooldownPercentage = Mathf.Clamp01(1 - (remainingCooldown / _db.pushCooldown));
        
    }
    
    [Command]
    private void CmdAttemptPush(Vector3 pushDirection)
    {
        if (!isServer) return;
        if (!canPush) return;
        
        State = PlayerStates.Pushing;
        
        pushDirection = pushDirection.normalized;
        
        bool hitSomeone = false;
        
        Vector3 origin = pushOrigin.position;
        Collider[] hitColliders = Physics.OverlapSphere(origin, _db.pushRadius * 1.2f);
        
        List<PlayerScript> targetsToPush = new List<PlayerScript>();
        
        foreach (Collider col in hitColliders)
        {
            // Ignorar a si mesmo
            if (col.gameObject == gameObject) continue;
            
            PlayerScript targetPlayer = col.GetComponentInParent<PlayerScript>();
            if (targetPlayer == null) targetPlayer = col.GetComponent<PlayerScript>();
            
            if (targetPlayer == null || targetPlayer.netId == netId) continue;
            
            Vector3 targetCenter = GetTargetCenter(targetPlayer);
            
            Vector3 directionToTarget = (targetCenter - origin);
            float distanceToTarget = directionToTarget.magnitude;
            directionToTarget = directionToTarget.normalized;
            
            float angle = Vector3.Angle(pushDirection, directionToTarget);
            
            bool inAngle = angle <= _db.pushAngle;
            Debug.DrawLine(origin, targetCenter, inAngle ? Color.yellow : Color.red, 1.0f);
            
            if (angle <= _db.pushAngle)
            {
                bool hasLineOfSight = true;
                
                // Verificar apenas se houver uma camada de obstáculos definida
                if (obstacleLayerMask != -1)
                {
                    RaycastHit obstacleHit;
                    if (Physics.Raycast(origin, directionToTarget, out obstacleHit, distanceToTarget, obstacleLayerMask))
                    {
                        if (obstacleHit.collider.gameObject != targetPlayer.gameObject && 
                            !obstacleHit.collider.transform.IsChildOf(targetPlayer.transform))
                        {
                            hasLineOfSight = false;
                            Debug.DrawLine(origin, obstacleHit.point, Color.red, 1.0f);
                        }
                    }
                }
                if (hasLineOfSight)
                {
                    targetsToPush.Add(targetPlayer);
                    Debug.DrawRay(targetCenter, Vector3.up * 2, Color.green, 1.0f);
                }
            }
        }
        foreach (PlayerScript targetPlayer in targetsToPush)
        {
            Vector3 pushForceVector = pushDirection.normalized * _db.pushForce;

            pushForceVector += Vector3.up * 0.5f;
            
            targetPlayer.State = PlayerStates.BeingPushed;
            targetPlayer.ApplyPush(pushForceVector);
            
            TargetApplyPushForce(targetPlayer.GetComponent<NetworkIdentity>().connectionToClient, pushForceVector);
            
            hitSomeone = true;
        }
        
        // Efeito visual do push no local
        if (pushEffectPrefab != null && hitSomeone)
        {
            GameObject effect = Instantiate(pushEffectPrefab, 
                                           origin + pushDirection * (_db.pushRadius * 0.5f), 
                                           Quaternion.LookRotation(pushDirection));
            effect.transform.localScale = Vector3.one * (_db.pushRadius * 0.5f);
            NetworkServer.Spawn(effect); // Spawn na rede para todos verem
            Destroy(effect, 1.0f);
        }
        //TODO DEBUG LOGS:
        DrawDebugPushCone(origin, pushDirection, hitSomeone);
        
        // Notificar todos os clientes sobre o push
        RpcShowPushEffect(origin, pushDirection, hitSomeone);
        
        StartCoroutine(PushStateCooldown(hitSomeone));
    }
    
    private Vector3 GetTargetCenter(PlayerScript targetPlayer)
    {
        if (targetPlayer.TryGetComponent(out Collider targetCollider))
        {
            return targetCollider.bounds.center;
        }
        
        Collider childCollider = targetPlayer.GetComponentInChildren<Collider>();
        if (childCollider != null)
        {
            return childCollider.bounds.center;
        }
        
        if (targetPlayer.TryGetComponent(out CharacterController cc))
        {
            return cc.bounds.center;
        }
        
        return targetPlayer.transform.position + new Vector3(0, 1.0f, 0);
    }
    
    [TargetRpc]
    private void TargetApplyPushForce(NetworkConnection target, Vector3 force)
    {
        if (!isLocalPlayer) return;
        
        pushVelocity = force;
        State = PlayerStates.BeingPushed;
        
        if (enableDetailedLogs) Debug.Log($"Cliente recebeu força de empurrão: {force}");
    }
    
    [ClientRpc]
    private void RpcShowPushEffect(Vector3 origin, Vector3 direction, bool successful)
    {
        // Mostrar efeito visual em todos os clientes
        if (successful && pushEffectPrefab != null)
        {
            Vector3 effectPosition = origin + direction * (_db.pushRadius * 0.5f);
            
            GameObject effect = Instantiate(pushEffectPrefab, effectPosition, Quaternion.LookRotation(direction));
            Destroy(effect, 1f);
        }
        
        if (pushSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(pushSound, successful ? 1.0f : 0.5f);
            }
        }
    }
    
    private IEnumerator PushStateCooldown(bool hitSomeone)
    {
        // Tempo da animação de empurrão
        yield return new WaitForSeconds(_db.pushDuration);
        
        State = PlayerStates.PushCooldown;
        
        float remainingCooldown = _db.pushCooldown - _db.pushDuration;
        if (remainingCooldown > 0)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }
        if (IsInState(PlayerStates.PushCooldown))
        {
            State = PlayerStates.Default;
        }
    }
    private void DrawDebugPushCone(Vector3 origin, Vector3 direction, bool hitSomeone)
    {
        Color coneColor = hitSomeone ? Color.green : Color.yellow;
        
        Debug.DrawRay(origin, direction * _db.pushRadius, coneColor, 1.0f);
        
        for (int i = 0; i < 8; i++)
        {
            float angle = (i / 8.0f) * 2 * Mathf.PI;
            Vector3 dir = Quaternion.AngleAxis(_db.pushAngle, new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle))) * direction;
            Debug.DrawRay(origin, dir * _db.pushRadius, coneColor, 1.0f);
        }
    }
    private void OnDrawGizmos() 
    {
        if (pushOrigin == null) return;
        
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawWireSphere(pushOrigin.position, _db.pushRadius);
        
        Vector3 forward = transform.forward;
        if (playerCamera != null && Application.isPlaying)
        {
            forward = playerCamera.transform.forward;
        }
        
        Gizmos.color = new Color(1, 0, 0, 0.6f);
        
        Vector3 leftLimit = Quaternion.AngleAxis(-_db.pushAngle, Vector3.up) * forward * _db.pushRadius;
        Vector3 rightLimit = Quaternion.AngleAxis(_db.pushAngle, Vector3.up) * forward * _db.pushRadius;
        Vector3 topLimit = Quaternion.AngleAxis(-_db.pushAngle, Vector3.right) * forward * _db.pushRadius;
        Vector3 bottomLimit = Quaternion.AngleAxis(_db.pushAngle, Vector3.right) * forward * _db.pushRadius;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(pushOrigin.position, forward * _db.pushRadius); 
        Gizmos.DrawRay(pushOrigin.position, leftLimit);
        Gizmos.DrawRay(pushOrigin.position, rightLimit);
        Gizmos.DrawRay(pushOrigin.position, topLimit);
        Gizmos.DrawRay(pushOrigin.position, bottomLimit);

        Gizmos.color = new Color(1, 0.5f, 0, 0.8f);
        Vector3 previousPoint = leftLimit;
        int segments = 16; 
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 currentPoint = Vector3.Slerp(leftLimit, rightLimit, t);
            Gizmos.DrawLine(pushOrigin.position, pushOrigin.position + currentPoint);
            Gizmos.DrawLine(pushOrigin.position + previousPoint, pushOrigin.position + currentPoint);
            previousPoint = currentPoint;
        }
        Gizmos.color = new Color(0, 0.8f, 1, 0.8f);
        previousPoint = topLimit;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 currentPoint = Vector3.Slerp(topLimit, bottomLimit, t);
            Gizmos.DrawLine(pushOrigin.position, pushOrigin.position + currentPoint);
            Gizmos.DrawLine(pushOrigin.position + previousPoint, pushOrigin.position + currentPoint);
            previousPoint = currentPoint;
        }
        Gizmos.color = new Color(1, 1, 0, 0.4f);
        int rings = 5;
        for (int r = 1; r <= rings; r++)
        {
            float distFactor = r / (float)rings;
            float ringRadius = Mathf.Sin(Mathf.Deg2Rad * _db.pushAngle) * _db.pushRadius * distFactor;
            Vector3 ringCenter = pushOrigin.position + forward * (_db.pushRadius * distFactor * Mathf.Cos(Mathf.Deg2Rad * _db.pushAngle));
            Vector3 lastPoint = ringCenter + new Vector3(ringRadius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * (2 * Mathf.PI / segments);
                Vector3 newPoint = ringCenter + new Vector3(
                    Mathf.Cos(angle) * ringRadius,
                    Mathf.Sin(angle) * ringRadius,
                    0);
                newPoint = ringCenter + Quaternion.FromToRotation(Vector3.forward, forward) * (newPoint - ringCenter);
                Gizmos.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
            }
        }
    }
}