using System;
using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Smooth;
using Random = UnityEngine.Random;

public enum MatchStatus{
    
    Awaiting,
    Lobby,
    Ongoing,
    
}

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
    private MatchStatus _status;

    [SyncVar] float _prepareTimer;
    [SyncVar] float _freezeTimer;
    [SyncVar] float _matchTimer;
    
    [SerializeField] List<Transform> _spawns;
    List<Transform> _excludedSpawns = new List<Transform>();

    private bool _matchHasStarted;

    public bool Freeze => _freezeTimer > 0; 
    
    private void Start()
    {

        
        if(base.isServer == false) return;

        _matchTimer = -1;
        _freezeTimer = -1;
        _prepareTimer = -1;
        
    }

    private void Update()
    {
        if(base.isServer == false) return;
        
        if (_prepareTimer > 0)
            _prepareTimer -= Time.deltaTime;

        if (_prepareTimer <= 0 && _prepareTimer != -1){
            InternalStartMatch();
            _prepareTimer = -1;
        }
        
        if(_matchHasStarted == false) return;
        
        if(_prepareTimer >= 0) return;
        
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
        
        if(_prepareTimer > 0) return;
        if(_matchTimer > 0) return;
        
        
        InternalPrepareMath();
    }
    
    [Server]
    void InternalPrepareMath() 
    {

        _prepareTimer = db.serverPrepareDuration;

    }
    [Server]
    void InternalStartMatch() 
    {
        _freezeTimer = db.serverFreezeDuration;
        _prepareTimer = db.serverMatchDuration;
        _matchHasStarted = true;
        
        foreach (PlayerData pd in PlayerList.singleton.players)
        {
            PlayerScript ps = pd.transform.GetComponent<PlayerScript>();
            ps = pd.transform.GetComponent<PlayerScript>();
            NetworkConnection conn = pd.transform.GetComponent<NetworkIdentity>().connectionToClient;
            Transform randomSpawn = InternalGetRandomSpawnPoint();

            Debug.DrawRay(randomSpawn.position,Vector3.up * 100, Color.green, 10);
            
            ps.TargetRpcTeleport(conn, randomSpawn.position, randomSpawn.rotation);

        }
        
        
    }
    [Server]
    void InternalEndMatch()
    {
        
    }
    public Transform InternalGetRandomSpawnPoint()
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
    
}
