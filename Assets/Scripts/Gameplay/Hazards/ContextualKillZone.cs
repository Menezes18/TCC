using UnityEngine;


public class ContextualKillZone : MonoBehaviour
{
    [Header("Config")]
    public DeathCause cause = DeathCause.Default;
    public bool permanent = false; // true = spectate

    private void OnTriggerEnter(Collider other)
    {
        var player = other.transform.root.GetComponent<PlayerScript>();
        if (player == null) return;

        // Dispara morte contextual. O PlayerScript (owner) cuidará de replicar na rede.
        player.OnContextualHit(cause, permanent);
    }
}

