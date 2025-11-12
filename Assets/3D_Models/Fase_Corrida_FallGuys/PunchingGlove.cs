using System.Collections.Generic;
using Mirror;
using UnityEngine;

[AddComponentMenu("Minigames/Obstaculos/Punching Glove (Luva de Soco)")]
[RequireComponent(typeof(Animator))]
public class PunchingGlove : NetworkBehaviour
{
    // NÃO PRECISAMOS MAIS DA REFERÊNCIA DO COLLIDER AQUI
    
    [Header("Acerto (Hit)")]
    [Tooltip("Força horizontal do empurrão.")]
    [SerializeField] private float knockbackStrength = 10f;
    [Tooltip("Impulso vertical aplicado.")]
    [SerializeField] private float liftStrength = 5f;
    [Tooltip("Duração do atordoamento.")]
    [SerializeField] private float stunDuration = 0.2f;
    [Tooltip("Tempo mínimo entre acertos por jogador (para este soco).")]
    [SerializeField] private float hitCooldown = 0.35f;

    public GameObject VFX_HitEffect;

    private readonly Dictionary<uint, float> _lastHitByNetId = new();
    
    // Variável de estado controlada pelos eventos da animação
    private bool _isPunching = false;

    private void Awake()
    {
        // Garante que o collider principal (o que tem este script)
        // seja um trigger.
        var mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("Objeto da Luva não tem um Collider!", this);
        }
        VFX_HitEffect.SetActive(false);
    }

    /// <summary>
    /// Evento de Animação: Chamado no frame em que o soco começa
    /// </summary>
    [ServerCallback] // Garante que só rode no servidor
    public void StartPunch()
    {
        _isPunching = true;
        // Limpa a lista para que o novo soco possa acertar
        _lastHitByNetId.Clear(); 
        
        Debug.Log("Punch STATE: START");
    }

    /// <summary>
    /// Evento de Animação: Chamado no frame em que o soco termina
    /// </summary>
    [ServerCallback] // Garante que só rode no servidor
    public void EndPunch()
    {
        _isPunching = false;

        Debug.Log("Punch STATE: END");
        VFX_HitEffect.SetActive(false);
    }

    /// <summary>
    /// Disparado continuamente enquanto o jogador está no trigger
    /// </summary>
    [ServerCallback]
    private void OnTriggerStay(Collider other)
    {
        // Só fazemos algo se a animação de soco estiver ativa
        if (!_isPunching) return;
        
        // O resto da lógica é idêntico ao que você já tinha:

        if (!NetworkServer.active) return;

        var root = other.transform.root;
        var id = root.GetComponent<NetworkIdentity>();
        if (id == null) return;

        // O Cooldown é ESSENCIAL aqui para evitar 60 acertos por segundo
        if (_lastHitByNetId.TryGetValue(id.netId, out var last) && (Time.time - last) < hitCooldown)
            return; // Já acertamos este jogador neste soco

        var ps = root.GetComponent<PlayerScript>();
        if (ps == null) return;

        // Registra o acerto
        _lastHitByNetId[id.netId] = Time.time;

        // Calcula a direção (mantendo sua correção)
        Vector3 punchDirection = -transform.forward; 
        punchDirection.y = 0f;
        punchDirection.Normalize();

        VFX_HitEffect.SetActive(true);
        
        Debug.Log($"[PunchingGlove] Acertou {other.name} (via Stay) com força {knockbackStrength}");
        ps.ServerApplyImpulse(punchDirection, knockbackStrength, liftStrength, stunDuration, setStagger: true);
    }
}