using System.Collections.Generic;
using Mirror;
using UnityEngine;

[AddComponentMenu("Minigames/Obstaculos/Punching Glove (Luva de Soco)")]
[RequireComponent(typeof(Animator))] // Garante que o Animator exista
public class PunchingGlove : NetworkBehaviour
{
    [Header("Acerto (Hit)")]
    [Tooltip("Força horizontal do empurrão quando o jogador é atingido.")]
    [SerializeField] private float knockbackStrength = 10f;
    [Tooltip("Impulso vertical aplicado no acerto.")]
    [SerializeField] private float liftStrength = 5f;
    [Tooltip("Duração do atordoamento (Stagger) após o acerto.")]
    [SerializeField] private float stunDuration = 0.2f;
    [Tooltip("Tempo mínimo entre acertos por jogador para evitar spam (deve ser menor que a animação).")]
    [SerializeField] private float hitCooldown = 0.35f;

    // Dicionário para controlar o cooldown por jogador
    private readonly Dictionary<uint, float> _lastHitByNetId = new();
    
    // Variável de estado controlada pelo Animator
    private bool _isPunching = false;

    /// <summary>
    /// Método público chamado pelo StateMachineBehaviour do Animator.
    /// </summary>
    public void SetPunchingState(bool isPunching)
    {
        _isPunching = isPunching;
    }

    /// <summary>
    /// Limpa a lista de jogadores atingidos no início de cada soco.
    /// </summary>
    public void ResetHitPlayers()
    {
        _lastHitByNetId.Clear();
    }

   [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        // 1. O gatilho sequer disparou?
        Debug.Log($"[PunchingGlove] Triggered by: {other.name}");

        // 2. O estado _isPunching está correto?
        if (!_isPunching) 
        {
            Debug.Log("[PunchingGlove] Triggered, but NOT in punching state. Ignoring.");
            return;
        }

        if (!NetworkServer.active) return;

        var root = other.transform.root;
        var id = root.GetComponent<NetworkIdentity>();
        if (id == null) return;

        // Verifica o cooldown (exatamente como no seu código de referência)
        if (_lastHitByNetId.TryGetValue(id.netId, out var last) && (Time.time - last) < hitCooldown)
            return;

        var ps = root.GetComponent<PlayerScript>(); // Assumindo que você tem um "PlayerScript"
        if (ps == null) return;

        _lastHitByNetId[id.netId] = Time.time;

        // ===================================================================
        // AQUI ESTÁ A MUDANÇA PRINCIPAL
        // ===================================================================
        
        // Em vez de calcular tangentes, apenas pegamos a direção "para frente"
        // do objeto da luva.
        Vector3 punchDirection = transform.forward;
        
        // Achatamos o vetor para garantir que o impulso seja puramente horizontal
        // (igual a referência fazia com "radial.y = 0f")
        punchDirection.y = 0f;
        punchDirection.Normalize();
        
        // Aplica o impulso usando a nova direção
        Debug.Log($"[PunchingGlove] SUCCESS: Punching {other.name}!");
        ps.ServerApplyImpulse(punchDirection, knockbackStrength, liftStrength, stunDuration, setStagger: true);
    }
}