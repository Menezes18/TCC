using System;
using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Smooth;
using Random = UnityEngine.Random;


public class MatchManager : NetworkBehaviour
{
    
    #region Singleton Setup

    public static MatchManager singleton;
    private void Awake()
    {
        singleton = this;
    }
    

    #endregion

    PlayerList playerList => PlayerList.singleton;
    [SerializeField] Database db;
    [SerializeField] HUDSO HUDSO;


    [SyncVar (hook = nameof(HookOnFreezeTimerUpdated))] float _freezeTimer;
    [SyncVar (hook = nameof(HookOnMatchTimerUpdated))] float _matchTimer;
    
    [SerializeField] List<Transform> _spawns;
    List<Transform> _excludedSpawns = new List<Transform>();

    List<PlayerData> _activePlayers = new List<PlayerData>();
    List<PlayerData> _winnerPlayers = new List<PlayerData>();
    
    
    private bool _matchHasStarted;

    public bool Freeze => _freezeTimer > 0; 
    
    private void Start()
    {

        
        if(base.isServer == false) return;

        _matchTimer = -1;
        _freezeTimer = -1;

        LeanTween.delayedCall(2.0f, () =>
        { 
        InternalStartMatch();

        });

    }

    private void Update()
    {
        if(base.isServer == false) return;
        
        
        
        if(_matchHasStarted == false) return;
        
        
        if(_freezeTimer > 0)
            _freezeTimer -= Time.deltaTime;

        if (_freezeTimer <= 0 && _freezeTimer != -1)
        {
            // efeito talvez
            // ou som
            // mas é aqui 
            _freezeTimer = -1;
            
        }
        
        if(_freezeTimer >= 0) return;
        
        if(_matchTimer > 0)
            _matchTimer -= Time.deltaTime;
            
        if(_matchTimer <= 0 && _matchTimer != -1){

            InternalEndMatch();
            _matchTimer = -1;
            
            
        }
        
    }

    [Command(requiresAuthority = false)]
    public void CmdPrepareMath() {
        
        if(_matchTimer > 0) return;
        
        
        InternalPrepareMath();
    }
    
    [Server]
    void InternalPrepareMath() 
    {
        _activePlayers.Clear();
        _winnerPlayers.Clear();
        

    }
    [Server]
    void InternalStartMatch() 
    {
        _freezeTimer = db.serverFreezeDuration;
        _matchTimer = db.serverMatchDuration;
        _matchHasStarted = true;
        
        foreach (PlayerData pd in PlayerList.singleton.players)
        {
            if (_activePlayers.Contains(pd)) return;

            PlayerScript ps = pd.transform.GetComponent<PlayerScript>();
            ps = pd.transform.GetComponent<PlayerScript>();
            NetworkConnection conn = pd.transform.GetComponent<NetworkIdentity>().connectionToClient;
            Transform randomSpawn = InternalGetRandomSpawnPoint();

            Debug.DrawRay(randomSpawn.position,Vector3.up * 100, Color.green, 10);
            
            ps.TargetRpcTeleport(conn, randomSpawn.position, randomSpawn.rotation);


            _activePlayers.Add(pd);

        }
        
        
    }
    [Server]
    void InternalEndMatch()
    {
        
        
        
        LeanTween.delayedCall(2.0f, () =>
        {
            foreach (PlayerData pd in _activePlayers)
            {
                PlayerScript ps = pd.transform.GetComponent<PlayerScript>();
                ps = pd.transform.GetComponent<PlayerScript>();
                NetworkConnection conn = pd.transform.GetComponent<NetworkIdentity>().connectionToClient;
                Transform spawn = NetworkManager.startPositions[0];
                
                Debug.DrawRay(spawn.position,Vector3.up * 100, Color.green, 10);
            
                ps.TargetRpcTeleport(conn, spawn.position, spawn.rotation);
                

            }
        });
        
        _matchHasStarted = false;
        _matchTimer = -1;
        _freezeTimer = -1;
    }

    [Server]
    public void AddWinnerPlayer(PlayerData pd)
    {
        if(_winnerPlayers.Contains(pd)) return;
        
        _winnerPlayers.Add(pd);
        
        ServerCheckResults();
    }

    [Server]
    void ServerCheckResults()
    {
        if(_activePlayers.Count == _winnerPlayers.Count) 
        {
            InternalEndMatch();
        }
            
    }
    [Server]
    public Transform GetRandomSpawnPoint()
    {
        return InternalGetRandomSpawnPoint();
    }

    Transform InternalGetRandomSpawnPoint()
    {
        int randomIndex = Random.Range(0, _spawns.Count);
        Transform random = _spawns[randomIndex];

        _spawns.Remove(random);
        _excludedSpawns.Add(random);

        if (_spawns.Count == 0){
            _spawns = _excludedSpawns.ToList();
            _excludedSpawns.Clear();
        }
        
        return random;
    }

    void HookOnFreezeTimerUpdated(float oldValue, float newValue)
    {
        HUDSO.FreezeTimerUpdated(newValue);
    }  
    
    void HookOnMatchTimerUpdated(float oldValue, float newValue)
    {
        HUDSO.MatchTimerUpdate(newValue);
    }
    
}
