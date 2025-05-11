using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerList : NetworkBehaviour{

    #region Singleton Setup

    public static PlayerList singleton;
    private void Awake()
    {
        singleton = this;
    }
    

    #endregion

    [SerializeField] Database db;
    
    public readonly SyncList<PlayerData> players = new SyncList<PlayerData>();
    
    public readonly SyncList<int> ColorsAvailable = new SyncList<int>();

    private void Start()
    {
        ColorsAvailable.Callback += ColorsAvailable_Callback;
        
        for(int i = 0; i < db.playerColors.Count; i++){
            ColorsAvailable.Add(i);
        }
    }

    private void OnDestroy()
    {
        ColorsAvailable.Callback -= ColorsAvailable_Callback;
    }

    [Server]
    public void AddToList(PlayerData data)
    {
        if(players.Contains(data) == true) return;
        
        if(players.Contains(data) == true) return;
        
        players.Add(data);
    }
    [Server]
    public void RemoveFromList(PlayerData data)
    {
        if(data == null) return;
        
        if(players.Contains(data) == false) return;
        
        ColorsAvailable.Add(data.color);
        players.Remove(data);
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
    public int ServerRequestColor(int color)
    {
        bool avaiable = ColorsAvailable.Contains(color);

        if (avaiable == true){
            
            ColorsAvailable.Remove(color);
            return color;
        }
        
        int randomIndex = Random.Range(0, ColorsAvailable.Count);
        int randomColor = ColorsAvailable[randomIndex];

        ColorsAvailable.Remove(randomColor);
        return randomColor;
    }
    
    //
    void ColorsAvailable_Callback(SyncList<int>.Operation op, int itemindex, int olditem, int newitem)
    {
       
    }
}
