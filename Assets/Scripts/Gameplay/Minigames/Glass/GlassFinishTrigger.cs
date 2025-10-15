using Mirror;
using UnityEngine;

public class GlassFinishTrigger : NetworkBehaviour
{
    private void Awake()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var pd = other.transform.root.GetComponent<PlayerData>();
        if (pd == null) return;

        if (NetworkServer.active)
        {
            var ctrl = FindAnyObjectByType<GlassMinigameController>();
            if (ctrl != null)
                ctrl.ServerOnPlayerFinish(pd);
            return;
        }

        var ni = pd.GetComponent<NetworkIdentity>();
        if (ni != null)
        {
            CmdClientFinished(ni.netId);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdClientFinished(uint playerNetId)
    {
        if (!NetworkServer.active) return;
        if (!NetworkServer.spawned.TryGetValue(playerNetId, out var id)) return;
        var pd = id != null ? id.GetComponent<PlayerData>() : null;
        if (pd == null) return;

        var ctrl = FindAnyObjectByType<GlassMinigameController>();
        if (ctrl != null)
            ctrl.ServerOnPlayerFinish(pd);
    }
}

