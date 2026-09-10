using UnityEngine;
using Mirror;


public class ContextualKillZone : MonoBehaviour
{
    [Header("Config")]
    public DeathCause cause = DeathCause.Default;
    public bool permanent = false; // true = spectate

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active)
            return;

        var player = other.transform.root.GetComponent<PlayerScript>();
        if (player == null) return;

        player.ServerHandleContextualHit(cause, permanent);
    }
}

