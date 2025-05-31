using System;
using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Smooth;
using Random = UnityEngine.Random;
using UnityEngine.Events;
public struct PlayerScoreEntry
{
    public ulong steamId;
    public string displayName;
    public int score;
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
    private List<PlayerScoreEntry> _temporaryRanking = new List<PlayerScoreEntry>();
    private HashSet<NetworkConnectionToClient> _readyConnections = new();
    
    private IScoreRule scoreRule;

    
    [SyncVar (hook = nameof(HookOnFreezeTimerUpdated))] float _freezeTimer;
    [SyncVar (hook = nameof(HookOnMatchTimerUpdated))] float _matchTimer;
    
    [SerializeField] List<Transform> _spawns;
    List<Transform> _excludedSpawns = new List<Transform>();

    List<PlayerData> _activePlayers = new List<PlayerData>();
    List<PlayerData> _winnerPlayers = new List<PlayerData>();
    public GameObject acabarFreezeTime;
    
    
    private bool _matchHasStarted;

    public bool Freeze => _freezeTimer > 0; 
    
    
    private void Start()
    {

        if(base.isServer == false) return;

        _matchTimer = -1;
        _freezeTimer = -1;
        
        LeanTween.delayedCall(2.0f, () =>
        { 
            TeleportPlayer();
            scoreRule = FindObjectOfType<MinigameController>() as IScoreRule;
            

        });
        scoreRule = FindObjectOfType<MinigameController>() as IScoreRule;
        (scoreRule as MinigameController)?.SetupMiniGame();
        //InternalStartMatch();

    }

    [ClientRpc]
    void RpcAtivarAcabarFreezeTime()
    {
        if (acabarFreezeTime != null)
            acabarFreezeTime.SetActive(true);
    }
    private void Update()
    {
        if(base.isServer == false) return;
        
        
        
        if(_matchHasStarted == false) return;
        scoreRule.UpdateScores();
        UpdateTemporaryRanking();
        
        if(_freezeTimer > 0)
            _freezeTimer -= Time.deltaTime;

        if (_freezeTimer <= 0 && _freezeTimer != -1)
        {
            // efeito talvez
            // ou som
            // mas é aqui 
            Debug.LogError("Acabou");
            (scoreRule as MinigameController)?.StartMatch();
            _freezeTimer = -1;
            if(acabarFreezeTime != null) RpcAtivarAcabarFreezeTime();

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
        InternalStartMatch();


    }
    [Server]
    public void InternalStartMatch() 
    {
        _freezeTimer = db.serverFreezeDuration;
        _matchTimer = db.serverMatchDuration;
        _matchHasStarted = true;
    }

    private void TeleportPlayer()
    {
        foreach (PlayerData pd in PlayerList.singleton.players)
        {
            if (_activePlayers.Contains(pd)) return;

            PlayerScript ps = pd.transform.GetComponent<PlayerScript>();
            ps = pd.transform.GetComponent<PlayerScript>();
            NetworkConnection conn = pd.transform.GetComponent<NetworkIdentity>().connectionToClient;
            Transform randomSpawn = InternalGetRandomSpawnPoint();

            Debug.DrawRay(randomSpawn.position,Vector3.up * 100, Color.green, 10);
            
            ps.TargetRpcTeleport(conn, randomSpawn.position, this.transform.rotation);

            _activePlayers.Add(pd);

        }
    }

    [Server]
    public void InternalEndMatch()
    {
        Debug.LogError("Acabou o endMatch");
        _matchHasStarted = false;
        _matchTimer = -1;
        _freezeTimer = -1;

        scoreRule.AssignFinalPoints();

        UpdateTemporaryRanking();

        foreach (var entry in _temporaryRanking)
        {
            MyNetworkManager.manager.AddPoints(entry.steamId, entry.score);
        }
        
        LeanTween.delayedCall(2.0f, () =>
        {
            _activePlayers.Clear();
            _winnerPlayers .Clear();
            
            NetworkManager.singleton.ServerChangeScene("RASCUNHO");
            
            
        });
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

    private void UpdateTemporaryRanking()
    {
        var results = scoreRule.GetResults();
        _temporaryRanking = playerList.players
            .Select(pd =>
            {
                var sid = pd.playerInfo.steamId;
                return new PlayerScoreEntry {
                    steamId = sid,
                    displayName = pd.playerInfo.username,
                    score = results.TryGetValue(sid, out var s) ? s : 0
                };
            })
            .OrderByDescending(e => e.score)
            .ToList();
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
