using System;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
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
        if (ColorsAvailable.Count == 0)
        {
            for (int i = 0; i < db.playerColors.Count; i++)
                ColorsAvailable.Add(i);
        }
        
        foreach (var pd in FindObjectsOfType<PlayerData>())
        {
            AddToList(pd);
        }

        
        SceneManager.sceneLoaded += (_, __) =>
        {
            foreach (var pd in FindObjectsOfType<PlayerData>())
                AddToList(pd);
        };
    }
    
    private void OnDestroy()
    {
        ColorsAvailable.Callback -= ColorsAvailable_Callback;
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
    
    [Server]
    public void AtivarPlayer(bool ativar)
    {
        var allPlayerData = FindObjectsOfType<PlayerData>();

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
