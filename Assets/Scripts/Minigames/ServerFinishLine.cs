using Mirror;
using UnityEngine;

public class ServerFinishLine : NetworkBehaviour
{
    MatchManager MatchManager => MatchManager.singleton;

    // Unity Message | 0 references
    void OnTriggerEnter(Collider other)
    {
        if (base.isServer == false) return;

        if(!other.transform.root.CompareTag("Player")) return;

        PlayerData target = other.transform.root.GetComponent<PlayerData>();

        MatchManager.AddWinnerPlayer(target);
    }
}
