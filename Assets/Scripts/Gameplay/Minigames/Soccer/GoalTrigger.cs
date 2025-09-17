using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoalTrigger : NetworkBehaviour
{
    [Tooltip("Time dono desta trave: 0 = Azul(A), 1 = Vermelho(B)")]
    public int netOwnerTeam = 0;

    [SerializeField] private SoccerMinigameController controller;

    // Pequeno cooldown para evitar gols duplos na mesma entrada
    [SerializeField] private float scoreCooldown = 1.0f;
    private float _lastScoreTime;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true; // garantir trigger
    }

    public void BindController(SoccerMinigameController ctrl)
    {
        controller = ctrl;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!base.isServer) return;
        if (controller == null) return;
        if (Time.time - _lastScoreTime < scoreCooldown) return;

        // detecta bola
        if (other == null) return;
        var ball = other.GetComponentInParent<BallPhysics>() ?? other.GetComponent<BallPhysics>();
        if (ball == null) return;

        _lastScoreTime = Time.time;
        controller.ServerRegisterGoal(netOwnerTeam);
    }
}
