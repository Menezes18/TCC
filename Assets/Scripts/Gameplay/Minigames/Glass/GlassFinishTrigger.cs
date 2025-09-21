using Mirror;
using UnityEngine;

public class GlassFinishTrigger : NetworkBehaviour
{
    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;
        var pd = other.transform.root.GetComponent<PlayerData>();
        if (pd == null) return;

        var ctrl = FindAnyObjectByType<GlassMinigameController>();
        if (ctrl != null)
            ctrl.ServerOnPlayerFinish(pd);
    }
}

