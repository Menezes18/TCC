using System.Collections.Generic;
using Mirror;
using UnityEngine;

[AddComponentMenu("Minigames/Obstaculos/Rotating Hammer (Martelo)")]
public class RotatingHammer : NetworkBehaviour
{
    [Header("Rotação")]
    [Tooltip("Objeto que será rotacionado (braço do martelo). Se não setado, usa este Transform.")]
    [SerializeField] private Transform rotateTarget;
    [Tooltip("Velocidade de rotação em graus por segundo.")]
    [SerializeField] private float angularSpeed = 90f; // deg/s
    [Tooltip("Se verdadeiro, gira no sentido horário.")]
    [SerializeField] private bool clockwise = true;

    [Header("Acerto (Hit)")]
    [Tooltip("Pivô usado para calcular a direção tangencial do empurrão (centro do giro). Se vazio, usa este Transform.")]
    [SerializeField] private Transform pivot;
    [Tooltip("Força horizontal do empurrão quando o jogador é atingido.")]
    [SerializeField] private float knockbackStrength = 6f;
    [Tooltip("Impulso vertical aplicado no acerto.")]
    [SerializeField] private float liftStrength = 4f;
    [Tooltip("Duração do atordoamento (Stagger) após o acerto.")]
    [SerializeField] private float stunDuration = 0.2f;
    [Tooltip("Tempo mínimo entre acertos por jogador para evitar spam.")]
    [SerializeField] private float hitCooldown = 0.35f;
    private readonly Dictionary<uint, float> _lastHitByNetId = new();

    private void Reset()
    {
        if (rotateTarget == null) rotateTarget = transform;
        if (pivot == null) pivot = transform;
    }

    private void Update()
    {
        if (rotateTarget == null) return;
        float dir = clockwise ? -1f : 1f;
        rotateTarget.Rotate(Vector3.up, angularSpeed * dir * Time.deltaTime, Space.World);
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;

        var root = other.transform.root;
        var id = root.GetComponent<NetworkIdentity>();
        if (id == null) return;

        if (_lastHitByNetId.TryGetValue(id.netId, out var last) && (Time.time - last) < hitCooldown)
            return;

        var ps = root.GetComponent<PlayerScript>();
        if (ps == null) return;

        _lastHitByNetId[id.netId] = Time.time;

        Vector3 center = pivot != null ? pivot.position : transform.position;
        Vector3 radial = (root.position - center); radial.y = 0f; radial.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, radial) * (clockwise ? -1f : 1f);

        ps.ServerApplyImpulse(tangent, knockbackStrength, liftStrength, stunDuration, setStagger: true);
    }
}
