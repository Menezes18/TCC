using Mirror;
using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Minigames/Obstaculos/Conveyor (Esteira) Server")]
public class ConveyorZone : NetworkBehaviour
{
    [Header("Direção da Esteira")]
    [SerializeField] private bool useLocalForward = true;
    [SerializeField] private Vector3 worldDirection = Vector3.forward;

    public enum PushMode { Fixo, OporFrenteDoJogador }
    [SerializeField] private PushMode pushMode = PushMode.Fixo;

    [Header("Parametros (Server)")]
    [SerializeField, Min(0f)] private float beltSpeed = 4f;
    [SerializeField, Min(0.05f)] private float pulseInterval = 0.15f;

    private Vector3 Dir => useLocalForward ? transform.forward : (worldDirection.sqrMagnitude > 0 ? worldDirection.normalized : Vector3.forward);

    private readonly Dictionary<uint, float> _lastPulseByNetId = new();
    private readonly Dictionary<uint, Vector3> _lastServerPos = new();
    private readonly Dictionary<uint, float> _lastServerTs = new();

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;
        var id = other.transform.root.GetComponent<NetworkIdentity>();
        if (id == null) return;
        _lastPulseByNetId[id.netId] = 0f; // force pulse on next tick
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        if (!NetworkServer.active) return;
        var id = other.transform.root.GetComponent<NetworkIdentity>();
        if (id == null) return;
        _lastPulseByNetId.Remove(id.netId);
    }

    [ServerCallback]
    private void Update()
    {
        if (!NetworkServer.active) return;
        if (_lastPulseByNetId.Count == 0) return;

        float now = Time.time;
        Vector3 beltDir = Dir;

        // Iterate over a copy to avoid collection modification during loop
        var keys = new List<uint>(_lastPulseByNetId.Keys);
        foreach (var netId in keys)
        {
            float last = _lastPulseByNetId.TryGetValue(netId, out var t) ? t : 0f;
            if (now - last < pulseInterval) continue;

            var obj = NetworkServer.spawned.TryGetValue(netId, out var ni) ? ni : null;
            if (obj == null)
            {
                _lastPulseByNetId.Remove(netId);
                continue;
            }

            var ps = obj.GetComponent<PlayerScript>();
            if (ps == null)
            {
                _lastPulseByNetId.Remove(netId);
                continue;
            }

            Vector3 dirToApply = beltDir;

            if (pushMode == PushMode.OporFrenteDoJogador)
            {
                dirToApply = -obj.transform.forward;
                dirToApply.y = 0f;
                if (dirToApply == Vector3.zero) dirToApply = -beltDir;
            }
        

            // Aplica velocidade de esteira como "força externa de solo" no cliente dono
            ps.ServerSetExternalGroundVelocity(dirToApply.normalized * beltSpeed, pulseInterval + 0.05f, additive: false);
            _lastPulseByNetId[netId] = now;
        }
    }
}
