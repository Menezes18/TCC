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
    private readonly Dictionary<int, float> _lastHitLocalByInstance = new();

    private Vector3 GetDir()
    {
        return useLocalForward ? transform.forward : (worldDirection.sqrMagnitude > 0 ? worldDirection.normalized : Vector3.forward);
    }

    private void OnTriggerEnter(Collider other)
    {
        var root = other.transform.root;
        var ps = root.GetComponent<PlayerScript>();
        if (ps == null) return;

        Vector3 dir = GetDir();
        var identity = root.GetComponent<NetworkIdentity>();

        if (NetworkServer.active)
        {
            if (identity != null && TryRegisterServerHit(identity))
            {
                ApplyBounceServer(ps, dir);
            }
        }
        else if (!NetworkClient.active)
        {
            if (TryRegisterLocalHit(ps))
            {
                ps.ApplyImpulseLocal(dir, horizontalStrength, verticalStrength, stunDuration, setStagger: false);
            }
        }

        if (NetworkClient.active && ps.isOwned && identity != null)
        {
            if (TryRegisterLocalHit(ps))
            {
                CmdRequestBounce(identity);
            }
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestBounce(NetworkIdentity playerIdentity)
    {
        if (playerIdentity == null)
            return;

        var ps = playerIdentity.GetComponent<PlayerScript>();
        if (ps == null)
            return;

        if (!TryRegisterServerHit(playerIdentity))
            return;

        ApplyBounceServer(ps, GetDir());
    }

    private void ApplyBounceServer(PlayerScript ps, Vector3 dir)
    {
        ps.ServerApplyImpulse(dir, horizontalStrength, verticalStrength, stunDuration, setStagger: false);
    }

    private bool TryRegisterServerHit(NetworkIdentity identity)
    {
        if (identity == null)
            return false;

        if (_lastHitByNetId.TryGetValue(identity.netId, out var lastServer) && (Time.time - lastServer) < hitCooldown)
            return false;

        _lastHitByNetId[identity.netId] = Time.time;
        return true;
    }

    private bool TryRegisterLocalHit(PlayerScript ps)
    {
        if (ps == null)
            return false;

        int key = ps.GetInstanceID();
        if (_lastHitLocalByInstance.TryGetValue(key, out var lastLocal) && (Time.time - lastLocal) < hitCooldown)
            return false;

        _lastHitLocalByInstance[key] = Time.time;
        return true;
    }
}
