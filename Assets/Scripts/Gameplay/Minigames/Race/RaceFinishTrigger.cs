using Mirror;
using UnityEngine;


public class RaceFinishTrigger : NetworkBehaviour
{
    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;
        var pd = other.transform.root.GetComponent<PlayerData>();
        if (pd == null) return;

        var ctrl = RaceMinigameController.singleton;
        if (ctrl != null)
            ctrl.ServerOnPlayerFinish(pd);
    }
}

