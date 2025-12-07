using System;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

using System.Collections;
using System.Collections.Generic;


public class PlayerList : NetworkBehaviour{

    #region Singleton Setup

    public static PlayerList singleton;
    private void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        DontDestroyOnLoad(gameObject);

        // for(int i = 0; i < db.playerColors.Count; i++){
        //     ColorsAvailable.Add(i);
        // }
    }
    

    #endregion

    [SerializeField] Database db;
    
    public readonly SyncList<PlayerData> players = new SyncList<PlayerData>();
    
    public readonly SyncList<int> ColorsAvailable = new SyncList<int>();

    
    
    private void Start()
    {
        ColorsAvailable.Callback += ColorsAvailable_Callback;
        
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        RebuildColorPool(); 
        SceneManager.sceneLoaded += OnSceneLoadedServer;
    }
    
    public override void OnStopServer()
    {
        base.OnStopServer();
        SceneManager.sceneLoaded -= OnSceneLoadedServer;
    }
    
    private void OnDestroy()
    {
        ColorsAvailable.Callback -= ColorsAvailable_Callback;
        SceneManager.sceneLoaded -= OnSceneLoadedServer;
    }

    [Server]
    public void AddToList(PlayerData data)
    {
        if(players.Contains(data) == true) return;
        
        
        players.Add(data);
        if (data.color >= 0 && ColorsAvailable.Contains(data.color))
            ColorsAvailable.Remove(data.color);
    }
    [Server]
    public void RemoveFromList(PlayerData data)
    {
        if(data == null) return;
        
        if(players.Contains(data) == false) return;
        
        ColorsAvailable.Add(data.color);
        players.Remove(data);
    }

    [Server]
    public void ReturnColor(int color)
    {
        if(ColorsAvailable.Contains(color)) return;
        
        ColorsAvailable.Add(color);
    }
    public bool CheckDuplicateAlias(string target)
    {
        foreach (PlayerData data in players ){
            if (data.alias == target) 
                return true;
        }
        return false;
    }
    [Server]
    private void RebuildColorPool()
    {
        ColorsAvailable.Clear();

        var used = new HashSet<int>();
        foreach (var kv in MyNetworkManager.manager.pointsBoard)
            if (kv.Value.color >= 0) used.Add(kv.Value.color);

        // reconstroi pool com TODAS as cores do Database menos as usadas
        for (int i = 0; i < db.playerColors.Count; i++)
            if (!used.Contains(i))
                ColorsAvailable.Add(i);
    }
    [Server]
    private void OnSceneLoadedServer(Scene _, LoadSceneMode __)
    {
        if (!NetworkServer.active) return;
        RebuildColorPool();
    }
    [Server]
    public int ServerRequestColor(int oldColor, int newColor )
    {

        ReturnColor(oldColor);
        
        bool avaiable = ColorsAvailable.Contains(newColor);
        
        
        if (avaiable == true){
            
            ColorsAvailable.Remove(newColor);

            return newColor;
        }
        
        int randomIndex = Random.Range(0, ColorsAvailable.Count);
        int randomColor = ColorsAvailable[randomIndex];

        ColorsAvailable.Remove(randomColor);
        return randomColor;
    }
    
    /// <summary>
    /// Limpa todos os jogadores e reseta o pool de cores.
    /// Chamado pelo MyNetworkManager ao parar o host/servidor.
    /// </summary>
    public void ClearAllPlayers()
    {
        Debug.Log("[PlayerList] ClearAllPlayers called - resetting player list");
        
        // Only clear SyncLists if server is active to avoid "InitSyncObject: IsWritable" error
        if (NetworkServer.active)
        {
            // Limpa a lista de jogadores
            players.Clear();
            
            // Reseta o pool de cores
            ColorsAvailable.Clear();
            if (db != null)
            {
                for (int i = 0; i < db.playerColors.Count; i++)
                {
                    ColorsAvailable.Add(i);
                }
            }
            Debug.Log("[PlayerList] Player list cleared and colors reset (Server Active)");
        }
        else
        {
            Debug.LogWarning("[PlayerList] NetworkServer not active - skipping SyncList clear to avoid errors");
        }
    }
    
    [Server]
    public void SetAllPlayersFrozen(bool ativar)
    {
    var allPlayerData = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);

        foreach (var data in allPlayerData)
        {
            var ps = data.GetComponent<PlayerScript>();
            if (ps != null)
            {
                ps.isFrozen = ativar;
            }
        }
    }
    public bool AllPlayersReady()
    {
        foreach (var p in players)
        {
            if (!p.IsReady)
                return false;
        }
        return true;
    }
    [Server]
    public int RequestRandomColor()
    {
        int idx = Random.Range(0, ColorsAvailable.Count);
        int color = ColorsAvailable[idx];
        ColorsAvailable.RemoveAt(idx);
        return color;
    }
    //
    void ColorsAvailable_Callback(SyncList<int>.Operation op, int itemindex, int olditem, int newitem)
    {
       
    }
}
