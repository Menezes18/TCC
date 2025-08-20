using System;
using Mirror;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using kcp2k;
using UnityEngine;
using Mirror.FizzySteam;
using Object = System.Object;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class PunchBridgeController : MinigameController, IObserver
{
    [SerializeField] List<PlayerData> alivePlayers = new List<PlayerData>();
    [SerializeField] List<PlayerData> eliminationOrder = new List<PlayerData>();
    private readonly HashSet<ulong> aliveIds = new HashSet<ulong>();
    PlayerList playerList => PlayerList.singleton;
    void OnEnable()
    {
        if (NetworkServer.active && MyNetworkManager.manager != null)
            MyNetworkManager.manager.onClientsChanged += TryAddLatePlayers;
    }

    void OnDisable()
    {
        if (NetworkServer.active && MyNetworkManager.manager != null)
            MyNetworkManager.manager.onClientsChanged -= TryAddLatePlayers;
    }
    
    [ServerCallback]
    void Start()
    {
        RebuildAlivePlayers();
    }
    
    [Server]
    void RebuildAlivePlayers()
    {
        alivePlayers.Clear();

        foreach (var pd in MyNetworkManager.manager.allClients)
        {
            if (pd == null) continue;
            var id = pd.playerInfo.steamId;
            if (aliveIds.Add(id))
                alivePlayers.Add(pd);
            
            
            Debug.LogError(alivePlayers);
        }
    }
    [Server]
    private void TryAddLatePlayers()
    {
        foreach (var pd in MyNetworkManager.manager.allClients)
        {
            if (pd == null) continue;
            var id = pd.playerInfo.steamId;
            if (!aliveIds.Contains(id))
            {
                aliveIds.Add(id);
                alivePlayers.Add(pd);
            }
        }

        alivePlayers.RemoveAll(p => p == null || !aliveIds.Contains(p.playerInfo.steamId));
    }

    public override void UpdateScores()
    {
        throw new System.NotImplementedException();
    }

    public override void AssignFinalPoints()
    {
        throw new System.NotImplementedException();
    }

    public override Dictionary<ulong, int> GetResults()
    {
        throw new System.NotImplementedException();
    }

    public override Dictionary<ulong, int> GetLiveScores()
    {
        throw new System.NotImplementedException();
    }
}
