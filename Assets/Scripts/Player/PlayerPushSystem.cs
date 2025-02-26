using Mirror;
using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerScript))]
public class PlayerPushSystem : PlayerScriptBase
{
    [Header("Push Configuration")]
    [SerializeField] private float pushForce = 5f;
    [SerializeField] private float pushCooldown = 1f;
    [SerializeField] private float pushRadius = 2f;
    [SerializeField] private float pushAngle = 45f;
    [SerializeField] private Transform pushOrigin;
    [SerializeField] private float pushDuration = 0.3f;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject pushEffectPrefab;
    [SerializeField] private AudioClip pushSound;
    
    // Referências aos componentes
    private CharacterController characterController;
    private Vector3 pushVelocity;
    private float lastPushTime;
    
    [SyncVar]
    private bool canPush = true;
    
    // Propriedade para verificar se pode empurrar
    public bool CanPush => canPush && Time.time >= lastPushTime + pushCooldown;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }
    
    private void Start()
    {
        if (!isOwned) return;
        
            OnStateChangeEvent += HandleStateChange;
        
    }
    
    private void OnDestroy()
    {
        
        
        OnStateChangeEvent -= HandleStateChange;
        
    }
    
    private void HandleStateChange(PlayerStates oldState, PlayerStates newState)
    {
        if (newState == PlayerStates.BeingPushed)
        {
            canPush = false;
        }
        else if (newState == PlayerStates.Default)
        {
            canPush = true;
        }
    }
    
    private void Update()
    {
        if (!isOwned) return;
        
        if (Input.GetKeyDown(KeyCode.E) && CanPush)
        {
            lastPushTime = Time.time;
            CmdAttemptPush();
        }
        
        if (pushVelocity.magnitude > 0.1f && IsInState(PlayerStates.BeingPushed))
        {
            characterController.Move(pushVelocity * Time.deltaTime);
            pushVelocity = Vector3.Lerp(pushVelocity, Vector3.zero, Time.deltaTime * 5f);
        }
        
        // Mostrar cooldown visual (pode ser implementado com UI)
        UpdatePushCooldownVisual();
    }
    
    private void UpdatePushCooldownVisual()
    {
        // Implementar feedback visual de cooldown
        // Ex: UI de cooldown ou efeito no personagem
    }
    
    [Command]
    private void CmdAttemptPush()
    {
        if (!isServer) return;
        
        State = PlayerStates.Pushing;
        
        Collider[] colliders = Physics.OverlapSphere(pushOrigin.position, pushRadius);
        bool hitSomeone = false;
        
        foreach (Collider col in colliders)
        {
            if (col.TryGetComponent(out PlayerScript targetPlayer) && targetPlayer.netId != netId)
            {
                Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToTarget);
                
                if (angle <= pushAngle)
                {
                    hitSomeone = true;
                    Vector3 pushDirection = (directionToTarget + Vector3.up * 0.2f).normalized * pushForce;
                    
                    // Aplicar o empurrão ao jogador alvo
                    targetPlayer.State = PlayerStates.BeingPushed;
                    targetPlayer.ApplyPush(pushDirection);
                    
                    // Spawnar efeito visual de empurrão
                    RpcSpawnPushEffect(col.transform.position);
                }
            }
        }
        
        StartCoroutine(PushStateCooldown(hitSomeone));
    }
    
    [ClientRpc]
    private void RpcSpawnPushEffect(Vector3 position)
    {
        // if (pushEffectPrefab != null)
        // {
        //     GameObject effect = Instantiate(pushEffectPrefab, position, Quaternion.identity);
        //     Destroy(effect, 1f);
        // }
        
        // if (pushSound != null && AudioSource.main != null)
        // {
        //     AudioSource.main.PlayOneShot(pushSound);
        // }
    }
    
    private IEnumerator PushStateCooldown(bool hitSomeone)
    {
        // Tempo da animação de empurrão
        yield return new WaitForSeconds(0.3f);
        
        State = PlayerStates.PushCooldown;
        
        float remainingCooldown = pushCooldown - 0.3f;
        if (remainingCooldown > 0)
            yield return new WaitForSeconds(remainingCooldown);
        
        if (IsInState(PlayerStates.PushCooldown))
            State = PlayerStates.Default;
    }
    
    private void OnDrawGizmos()
    {
        if (pushOrigin == null) return;
        
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(pushOrigin.position, pushRadius);
        
        Gizmos.color = Color.red;
        Vector3 leftLimit = Quaternion.Euler(0, -pushAngle, 0) * transform.forward * pushRadius;
        Vector3 rightLimit = Quaternion.Euler(0, pushAngle, 0) * transform.forward * pushRadius;
        
        Gizmos.DrawRay(pushOrigin.position, leftLimit);
        Gizmos.DrawRay(pushOrigin.position, rightLimit);
        
        Vector3 previousPoint = leftLimit;
        for (int i = 1; i <= 10; i++)
        {
            float t = i / 10f;
            Vector3 currentPoint = Vector3.Slerp(leftLimit, rightLimit, t);
            Gizmos.DrawLine(pushOrigin.position, pushOrigin.position + currentPoint);
            Gizmos.DrawLine(pushOrigin.position + previousPoint, pushOrigin.position + currentPoint);
            previousPoint = currentPoint;
        }
    }
}