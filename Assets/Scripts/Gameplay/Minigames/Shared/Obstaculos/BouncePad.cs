using System.Collections.Generic;
using Mirror;
using UnityEngine;

[AddComponentMenu("Minigames/Obstaculos/Bounce Pad (Trampolim)")]
public class BouncePad : NetworkBehaviour
{
    [Header("Forças do salto")]
    [Tooltip("Intensidade do empurrão horizontal aplicado no jogador ao tocar o trampolim.")]
    [SerializeField] private float horizontalStrength = 4f;
    [Tooltip("Intensidade do impulso vertical (altura do salto).")]
    [SerializeField] private float verticalStrength = 8f;
    [Tooltip("Duração do atordoamento (Stagger) opcional após o salto. 0 = sem atordoar.")]
    [SerializeField] private float stunDuration = 0.0f;
    [SerializeField] private bool useLocalForward = true;
    [SerializeField] private Vector3 worldDirection = Vector3.forward;

    [Header("Repetição (anti-spam)")]
    [Tooltip("Tempo mínimo entre ativações por jogador.")]
    [SerializeField] private float hitCooldown = 0.3f;
    private readonly Dictionary<uint, float> _lastHitByNetId = new();
    private readonly Dictionary<int, float> _lastHitOfflineByInstance = new();

    private Vector3 GetDir()
    {
        return useLocalForward ? transform.forward : (worldDirection.sqrMagnitude > 0 ? worldDirection.normalized : Vector3.forward);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Em jogo de rede: apenas o servidor processa (para replicar via RPC)
        if (NetworkClient.active && !NetworkServer.active)
            return;

        var root = other.transform.root;
        var ps = root.GetComponent<PlayerScript>();
        if (ps == null) return;

        Vector3 dir = GetDir();

        if (NetworkServer.active)
        {
            var ni = root.GetComponent<NetworkIdentity>();
            if (ni == null) return;

            if (_lastHitByNetId.TryGetValue(ni.netId, out var lastServer) && (Time.time - lastServer) < hitCooldown)
                return;

            _lastHitByNetId[ni.netId] = Time.time;
            ps.ServerApplyImpulse(dir, horizontalStrength, verticalStrength, stunDuration, setStagger: false);
        }
        else if (!NetworkClient.active)
        {
            // Modo offline/local: aplica diretamente no jogador local.
            int key = ps.GetInstanceID();
            if (_lastHitOfflineByInstance.TryGetValue(key, out var lastLocal) && (Time.time - lastLocal) < hitCooldown)
                return;

            _lastHitOfflineByInstance[key] = Time.time;
            ps.ApplyImpulseLocal(dir, horizontalStrength, verticalStrength, stunDuration, setStagger: false);
        }
    }
}
