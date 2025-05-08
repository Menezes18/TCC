using System;
using Mirror;
using UnityEngine;

public class PlayerList : NetworkBehaviour{

    #region Singleton Setup

    public static PlayerList singleton;
    private void Awake()
    {
        singleton = this;
    }
    

    #endregion

    private SyncList<PlayerData> players;

    [Server]
    public void AddToList(PlayerData data)
    {
        if(players.Contains(data) == true) return;
        
        if(players.Contains(data) == true) return;
        
        players.Add(data);
    }
    
    public void RemoveFromList(PlayerData data)
    
}
