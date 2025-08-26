using System;
using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Smooth;
using Unity.Profiling;
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
    [SerializeField] SettingsMiniGameData settingsMiniGameData; 
    [SerializeField] HUDSO HUDSO;
    private List<PlayerScoreEntry> _temporaryRanking = new List<PlayerScoreEntry>();
    private HashSet<NetworkConnectionToClient> _readyConnections = new();
    
    private IScoreRule scoreRule;

    
    [SyncVar (hook = nameof(HookOnFreezeTimerUpdated))] float _freezeTimer;
    [SyncVar (hook = nameof(HookOnMatchTimerUpdated))] float _matchTimer;
    [SyncVar (hook = nameof(HookOnGameOver))] string _gameOver;
    
    
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
        _gameOver = string.Empty;
        
        LeanTween.delayedCall(2.0f, () =>
        { 
            TeleportPlayer();
            scoreRule = FindFirstObjectByType<MinigameController>() as IScoreRule;
            

        });
        scoreRule = FindFirstObjectByType<MinigameController>() as IScoreRule;
        (scoreRule as MinigameController)?.SetupMiniGame();
        //InternalStartMatch();

    }

    [ClientRpc]
    void RpcAtivarAcabarFreezeTime()
    {
        if (acabarFreezeTime != null)
            acabarFreezeTime.SetActive(false);
    }
    private float _rankingNextUpdate;
    [SerializeField] private float rankingUpdatesPerSecond = 4f; // throttle ranking rebuilds
    private static readonly Unity.Profiling.ProfilerMarker PM_RebuildRanking = new("MatchManager.RebuildRanking");
    private void Update()
    {
        if(base.isServer == false) return;
        
        
        
        if(_matchHasStarted == false) return;
        scoreRule.UpdateScores();
        // throttle ranking recompute to reduce CPU/GC
        if (Time.unscaledTime >= _rankingNextUpdate)
        {
            _rankingNextUpdate = Time.unscaledTime + (1f / Mathf.Max(1f, rankingUpdatesPerSecond));
            UpdateTemporaryRanking_NoAlloc();
        }
        
        if(_freezeTimer > 0)
            _freezeTimer -= Time.deltaTime;

        if (_freezeTimer <= 0 && _freezeTimer != -1)
        {
            // efeito talvez
            // ou som
            // mas é aqui 
            Debug.Log("⏳ [MATCH] FreezeTime acabou, iniciando partida");
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
        _matchTimer = settingsMiniGameData.miniGameDuration;
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
        Debug.Log("🏁 [MATCH] Fim de partida – encerrando e atribuindo pontos");
        _matchHasStarted = false;
        _matchTimer = -1;
        _freezeTimer = -1;

        scoreRule.AssignFinalPoints();

        UpdateTemporaryRanking();

        foreach (var entry in _temporaryRanking)
        {
            MyNetworkManager.manager.AddPoints(entry.steamId, entry.score);
        }
        foreach (PlayerData pd in PlayerList.singleton.players)
        {
            PlayerScript ps = pd.transform.GetComponent<PlayerScript>();
            ps.isFrozen = true;
        }
        
        _gameOver = "Acabou!";
        LeanTween.delayedCall(2.0f, () =>
        {
            _activePlayers.Clear();
            _winnerPlayers .Clear();
            
            // Centralized network scene change
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

    // non-allocating ranking rebuild
    static readonly List<PlayerScoreEntry> _tmpRanking = new List<PlayerScoreEntry>(32);
    private void UpdateTemporaryRanking_NoAlloc()
    {
        using (PM_RebuildRanking.Auto())
        {
            var results = scoreRule.GetResults();
            _tmpRanking.Clear();
            // build
            for (int i = 0; i < playerList.players.Count; i++)
            {
                var pd = playerList.players[i];
                ulong sid = pd.playerInfo.steamId;
                int s = 0;
                if (results != null)
                    results.TryGetValue(sid, out s);
                _tmpRanking.Add(new PlayerScoreEntry
                {
                    steamId = sid,
                    displayName = pd.playerInfo.username,
                    score = s
                });
            }
            // sort by score desc
            _tmpRanking.Sort((a, b) => b.score.CompareTo(a.score));
            // assign
            _temporaryRanking = new List<PlayerScoreEntry>(_tmpRanking);
        }
    }

    // keep compatibility for any existing callers
    private void UpdateTemporaryRanking()
    {
        UpdateTemporaryRanking_NoAlloc();
    }
    
    void HookOnFreezeTimerUpdated(float oldValue, float newValue)
    {
        HUDSO.FreezeTimerUpdated(newValue);
    }  
    
    void HookOnMatchTimerUpdated(float oldValue, float newValue)
    {
        HUDSO.MatchTimerUpdate(newValue);
    }
    void HookOnGameOver(string oldValue, string newValue)
    {
        HUDSO.GameOver(newValue);
    }
    
}
