using System.Collections.Generic;
using Mirror;
using UnityEngine;

[AddComponentMenu("Minigames/Obstaculos/Bounce Pad (Trampolim)")]
public class BouncePad : NetworkBehaviour
{
    [Header("Forcas do salto")]
    [Tooltip("Intensidade do empurrao horizontal aplicado no jogador ao tocar o trampolim.")]
    [SerializeField] private float horizontalStrength = 4f;
    [Tooltip("Intensidade do impulso vertical (altura do salto).")]
    [SerializeField] private float verticalStrength = 8f;
    [Tooltip("Duracao do atordoamento (Stagger) opcional apos o salto. 0 = sem atordoar.")]
    [SerializeField] private float stunDuration = 0.0f;
    [SerializeField] private bool useLocalForward = true;
    [SerializeField] private Vector3 worldDirection = Vector3.forward;

    [Header("Repeticao (anti-spam)")]
    [Tooltip("Tempo minimo entre ativacoes por jogador.")]
    [SerializeField] private float hitCooldown = 0.3f;
    private readonly Dictionary<uint, float> _lastHitByNetId = new();
    private readonly Dictionary<int, float> _lastHitOfflineByInstance = new();
    // Predição local por cliente (cooldown do dono para evitar múltiplas aplicações)
    private readonly Dictionary<int, float> _lastClientPredHit = new();
    [SerializeField] private Animator BounceAnim;

    private Vector3 GetDir()
    {
        return useLocalForward ? transform.forward : (worldDirection.sqrMagnitude > 0 ? worldDirection.normalized : Vector3.forward);
    }

    private bool IsAirborneAuthorized(PlayerScript ps)
    {
        if (NetworkServer.active) return ps.IsAirborneServerFlag;
        return ps.IsAirborne;
    }

    private readonly Dictionary<uint, float> _lastYByNetId = new();
    private readonly Dictionary<int, float> _lastYOfflineByInstance = new();

    private bool IsDescendingServer(Transform root, uint netId)
    {
        float y = root.position.y;
        if (_lastYByNetId.TryGetValue(netId, out float lastY))
        {
            _lastYByNetId[netId] = y;
            return y < lastY - 0.003f; 
        }
        _lastYByNetId[netId] = y;
        return false;
    }

    private bool IsDescendingOffline(Transform root, int instanceId)
    {
        float y = root.position.y;
        if (_lastYOfflineByInstance.TryGetValue(instanceId, out float lastY))
        {
            _lastYOfflineByInstance[instanceId] = y;
            return y < lastY - 0.003f;
        }
        _lastYOfflineByInstance[instanceId] = y;
        return false;
    }

    private void TryBounce(PlayerScript ps)
    {
        Vector3 dir = GetDir();
        // var identity = root.GetComponent<NetworkIdentity>();

        if (NetworkServer.active)
        {
            var ni = ps.GetComponent<NetworkIdentity>();
            if (ni == null) return;

            if (_lastHitByNetId.TryGetValue(ni.netId, out var lastServer) && (Time.time - lastServer) < hitCooldown)
                return;

            _lastHitByNetId[ni.netId] = Time.time;
            ps.ServerApplyImpulse(dir, horizontalStrength, verticalStrength, stunDuration, setStagger: false);
        }
        else if (!NetworkClient.active)
        {
            int key = ps.GetInstanceID();
            if (_lastHitOfflineByInstance.TryGetValue(key, out var lastLocal) && (Time.time - lastLocal) < hitCooldown)
                return;

            _lastHitOfflineByInstance[key] = Time.time;
            ps.ApplyImpulseLocal(dir, horizontalStrength, verticalStrength, stunDuration, setStagger: false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (NetworkClient.active && !NetworkServer.active)
        {
            var rootC = other.transform.root;
            var psC = rootC.GetComponent<PlayerScript>();
            if (psC == null) return;
            if (!psC.isOwned) return;

            int keyC = psC.GetInstanceID();
            if (_lastClientPredHit.TryGetValue(keyC, out var lastLocalPred) && (Time.time - lastLocalPred) < hitCooldown)
                return;

            _lastClientPredHit[keyC] = Time.time;
            Vector3 dirPred = GetDir();
            psC.ApplyImpulseLocal(dirPred, horizontalStrength, verticalStrength, stunDuration, setStagger: false);
            psC.MarkPredictedImpulse();
            
            return;
        }

        var root = other.transform.root;
        var ps = root.GetComponent<PlayerScript>();
        if (ps == null) return;

        TryBounce(ps);
    }

    private void OnTriggerStay(Collider other)
    {
        if (NetworkClient.active && !NetworkServer.active)
        {
            var rootC = other.transform.root;
            var psC = rootC.GetComponent<PlayerScript>();
            if (psC == null) return;
            if (!psC.isOwned) return;

            int keyC = psC.GetInstanceID();
            if (_lastClientPredHit.TryGetValue(keyC, out var lastLocalPred) && (Time.time - lastLocalPred) < hitCooldown)
                return;

            _lastClientPredHit[keyC] = Time.time;
            Vector3 dirPred = GetDir();
            psC.ApplyImpulseLocal(dirPred, horizontalStrength, verticalStrength, stunDuration, setStagger: false);
            psC.MarkPredictedImpulse();
            return;
        }

        var root = other.transform.root;
        var ps = root.GetComponent<PlayerScript>();
        if (ps == null) return;

        //Animation
        if(BounceAnim != null)
        BounceAnim.SetBool("Bounce", true);

        TryBounce(ps);
    }

    private void OnTriggerExit(Collider other)
    {
        var root = other.transform.root;
        var ps = root.GetComponent<PlayerScript>();
        if (ps == null) return;

        if (NetworkServer.active)
        {
            var ni = ps.GetComponent<NetworkIdentity>();
            if (ni != null)
                _lastYByNetId.Remove(ni.netId);
        }
        else if (!NetworkClient.active)
        {
            _lastYOfflineByInstance.Remove(ps.GetInstanceID());
            _lastClientPredHit.Remove(ps.GetInstanceID());
        }

        //Set Anim
        if (BounceAnim != null)BounceAnim.SetBool("Bounce", false);
    }
}
