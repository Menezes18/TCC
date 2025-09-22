using System.Collections.Generic;
using Mirror;
using UnityEngine;

[AddComponentMenu("Minigames/Obstaculos/Ice Zone (Gelo)")]
public class IceZone : MonoBehaviour
{

    [Header("Pulso de impulso (ServerPulse)")]
    [Tooltip("Intervalo entre impulsos de deslizamento aplicados pelo servidor (segundos).")]
    [SerializeField] private float pulseInterval = 0.35f;
    [Tooltip("Atrito do gelo (quanto maior, mais rápido a velocidade de slide decai). Unidades: m/s por segundo.")]
    [SerializeField] private float slideFriction = 1.0f;
    [Tooltip("Velocidade máxima de deslizamento no gelo (planar)")]
    [SerializeField] private float maxSlideSpeed = 8.0f;
    [Tooltip("Se a velocidade planar do jogador exceder este limiar, capturamos/atualizamos a direção e magnitude do slide.")]
    [SerializeField] private float captureThreshold = 0.4f;
    [Tooltip("Mistura (0..1) entre velocidade atual capturada e a já acumulada (suaviza a transição)")]
    [Range(0f,1f)]
    [SerializeField] private float captureBlend = 0.6f;
    [Tooltip("Quanto a velocidade de slide tende a girar na direção do forward do jogador (0 = sem giro)")]
    [Range(0f,1f)]
    [SerializeField] private float steerTowardsForward = 0.15f;
    [Tooltip("Multiplicador de controle do jogador enquanto estiver no gelo (0 = sem controle, 1 = controle total)")]
    [Range(0f,1f)]
    [SerializeField] private float controlMultiplierOnIce = 0.4f;

    // Estado local para modo LocalOwner
    private readonly Dictionary<Transform, Vector3> _lastPosLocal = new();

    // Estado no servidor para modo ServerPulse
    private readonly Dictionary<uint, Vector3> _lastServerPos = new();
    private readonly Dictionary<uint, float> _lastServerTs = new();
    private readonly Dictionary<uint, float> _lastPulseTs = new();
    private readonly Dictionary<uint, Vector3> _slideVelByNetId = new();

    private void OnTriggerStay(Collider other)
    {
        
            if (!NetworkServer.active) return; 

            var root = other.transform.root;
            var id = root.GetComponent<NetworkIdentity>();
            if (id == null) return;

            float now = Time.time;
            Vector3 lastPos = _lastServerPos.TryGetValue(id.netId, out var lp) ? lp : root.position;
            float lastTs = _lastServerTs.TryGetValue(id.netId, out var lt) ? lt : (now - 0.02f);
            float dt = Mathf.Max(1e-4f, now - lastTs);
            Vector3 delta = root.position - lastPos;
            Vector3 planarVel = new Vector3(delta.x, 0f, delta.z) / dt;

            _lastServerPos[id.netId] = root.position;
            _lastServerTs[id.netId] = now;

            Vector3 slideVel = _slideVelByNetId.TryGetValue(id.netId, out var sv) ? sv : Vector3.zero;

            if (planarVel.magnitude > captureThreshold)
            {
                Vector3 captured = Vector3.ClampMagnitude(planarVel, maxSlideSpeed);
                slideVel = Vector3.Lerp(slideVel, captured, captureBlend);
            }

            
            if (steerTowardsForward > 0f)
            {
                Vector3 fwd = root.forward; fwd.y = 0f; fwd.Normalize();
                if (fwd.sqrMagnitude > 1e-6f && slideVel.sqrMagnitude > 1e-6f)
                {
                    Vector3 target = fwd * slideVel.magnitude;
                    slideVel = Vector3.Lerp(slideVel, target, steerTowardsForward * Mathf.Clamp01(dt * (1f / pulseInterval)));
                }
            }

           
            if (slideFriction > 0f)
            {
                float dec = slideFriction * dt;
                float mag = Mathf.Max(0f, slideVel.magnitude - dec);
                slideVel = slideVel.sqrMagnitude > 1e-6f ? slideVel.normalized * mag : Vector3.zero;
            }

            
            slideVel = Vector3.ClampMagnitude(slideVel, maxSlideSpeed);

            _slideVelByNetId[id.netId] = slideVel;

            float lastPulse = _lastPulseTs.TryGetValue(id.netId, out var lpulse) ? lpulse : 0f;
            if ((now - lastPulse) >= pulseInterval)
            {
                var ps = root.GetComponent<PlayerScript>();
                if (ps != null)
                {
                   
                    ps.ServerSetExternalGroundVelocity(slideVel, pulseInterval + 0.05f, additive: false);
                    ps.ServerSetControlMultiplier(controlMultiplierOnIce, pulseInterval + 0.05f);
                    _lastPulseTs[id.netId] = now;
                }
            }
        
    }

    private void OnTriggerExit(Collider other)
    {
        var root = other.transform.root;
        _lastPosLocal.Remove(root);

        var id = root.GetComponent<NetworkIdentity>();
        if (id != null)
        {
            _lastServerPos.Remove(id.netId);
            _lastServerTs.Remove(id.netId);
            _lastPulseTs.Remove(id.netId);
            _slideVelByNetId.Remove(id.netId);
        }
    }
}
