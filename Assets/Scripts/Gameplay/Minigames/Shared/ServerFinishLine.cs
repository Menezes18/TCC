using System;
using Mirror;
using UnityEngine;

public class ServerFinishLine : NetworkBehaviour
{
    MatchManager MatchManager => MatchManager.singleton;


    private void Start()
    {
        Debug.Log($"🏁 [FINISH LINE] Ativado no objeto: {gameObject.name}");
    }

    void OnTriggerEnter(Collider other)
    {
        
        if (base.isServer == false) return;

        if(!other.transform.root.CompareTag("Player")) return;

        PlayerData target = other.transform.root.GetComponent<PlayerData>();

        MatchManager.AddWinnerPlayer(target);
    }
}
