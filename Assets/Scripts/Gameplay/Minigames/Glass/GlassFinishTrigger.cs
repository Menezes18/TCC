using Mirror;
using UnityEngine;

public class GlassFinishTrigger : NetworkBehaviour
{
    private void Awake()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        var pd = other.transform.root.GetComponent<PlayerData>();
        if (pd == null) return;
        var ctrl = FindAnyObjectByType<GlassMinigameController>();
        if (ctrl != null) ctrl.ServerOnPlayerFinish(pd);
    }
}
