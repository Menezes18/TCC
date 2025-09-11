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
    [SerializeField] SettingsMiniGameData settingsMiniGameData; 
    [SerializeField] HUDSO HUDSO;
    private List<PlayerScoreEntry> _temporaryRanking = new List<PlayerScoreEntry>();
    private HashSet<NetworkConnectionToClient> _readyConnections = new();
    
    private IScoreRule scoreRule;

    private ISpawnPointProvider _spawnProvider; // fase 3 item 10
    private ITimerService _timerService; // fase 3 item 11
    
    [SyncVar (hook = nameof(HookOnFreezeTimerUpdated))] float _freezeTimer; // espelho do TimerService
    [SyncVar (hook = nameof(HookOnMatchTimerUpdated))] float _matchTimer;   // espelho do TimerService
    [SyncVar (hook = nameof(HookOnGameOver))] string _gameOver;
    
    
    [SerializeField] List<Transform> _spawns;

    List<PlayerData> _activePlayers = new List<PlayerData>();
    List<PlayerData> _winnerPlayers = new List<PlayerData>();
    public GameObject acabarFreezeTime;
    
    
    private bool _matchHasStarted;
    public float MatchTimer => _matchTimer; // legacy access

    [Server]
    public void SetMatchTimer(float value)
    {
        _matchTimer = value;
    }
    public bool Freeze => _timerService != null ? _timerService.Freeze > 0 : _freezeTimer > 0; 
    
    
    private void Start()
    {

        if(base.isServer == false) return;

    _matchTimer = -1; // mirrors start invalid
    _freezeTimer = -1;
        _gameOver = string.Empty;
        
        LeanTween.delayedCall(2.0f, () =>
        { 
            TeleportPlayer();
            scoreRule = FindFirstObjectByType<MinigameController>() as IScoreRule;
            

        });
        scoreRule = FindFirstObjectByType<MinigameController>() as IScoreRule;
        (scoreRule as MinigameController)?.SetupMiniGame();

    _spawnProvider = new RandomCycleSpawnPointProvider(_spawns);
    _timerService = new TimerService();
    }

    [ClientRpc]
    void RpcAtivarAcabarFreezeTime()
    {
        if (acabarFreezeTime != null)
            acabarFreezeTime.SetActive(false);
    }
    private void Update()
    {
        if(base.isServer == false) return;
        
        
        
        if(_matchHasStarted == false) return;
        scoreRule.UpdateScores();
        UpdateTemporaryRanking();
        
        // Novo fluxo: TimerService
        _timerService.Tick(Time.deltaTime,
            onFreezeEnd: () =>
            {
                Debug.Log("⏳ [MATCH] FreezeTime acabou, iniciando partida");
                (scoreRule as MinigameController)?.StartMatch();
                if (acabarFreezeTime != null) RpcAtivarAcabarFreezeTime();
            },
            onMatchEnd: () =>
            {
                InternalEndMatch();
            });

        // Espelha valores em SyncVars apenas se mudaram (reduz churn de rede)
        if (Mathf.Abs(_freezeTimer - _timerService.Freeze) > 0.001f) _freezeTimer = _timerService.Freeze;
        if (Mathf.Abs(_matchTimer - _timerService.Match) > 0.001f) _matchTimer = _timerService.Match;

        if (_timerService.Freeze >= 0) return; // ainda em freeze
        
    }

    [Obsolete("Correção de nome: use CmdPrepareMatch()", false)]
    [Command(requiresAuthority = false)]
    public void CmdPrepareMath() {
        CmdPrepareMatch();
    }

    [Command(requiresAuthority = false)]
    public void CmdPrepareMatch() {
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
        _timerService.Set(db.serverFreezeDuration, settingsMiniGameData.miniGameDuration);
        _freezeTimer = _timerService.Freeze; // initial mirror for clients connecting
        _matchTimer  = _timerService.Match;
        _matchHasStarted = true;
    }

    private void TeleportPlayer()
    {
        foreach (PlayerData pd in PlayerList.singleton.players)
        {
            if (_activePlayers.Contains(pd)) return;
            PlayerScript ps = pd.transform.GetComponent<PlayerScript>();
            NetworkConnection conn = pd.transform.GetComponent<NetworkIdentity>().connectionToClient;
            Transform randomSpawn = _spawnProvider != null ? _spawnProvider.GetNext() : null;
            if(randomSpawn == null) continue;
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
    _timerService.Set(-1,-1);
    _matchTimer = -1; _freezeTimer = -1;

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
    [Obsolete("Use spawn provider via TeleportPlayer flow", false)]
    [Server]
    public Transform GetRandomSpawnPoint() => _spawnProvider?.GetNext();

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
    void HookOnGameOver(string oldValue, string newValue)
    {
        HUDSO.GameOver(newValue);
    }
    
}

public interface ISpawnPointProvider
{
    Transform GetNext();
}
public class RandomCycleSpawnPointProvider : ISpawnPointProvider
{
    private readonly System.Collections.Generic.List<Transform> _available;
    private readonly System.Collections.Generic.List<Transform> _used = new();
    public RandomCycleSpawnPointProvider(System.Collections.Generic.List<Transform> points){ _available = points != null ? new System.Collections.Generic.List<Transform>(points) : new System.Collections.Generic.List<Transform>(); }
    public Transform GetNext(){
        if (_available.Count == 0){
            // recycle
            _available.AddRange(_used);
            _used.Clear();
        }
        if (_available.Count == 0) return null;
        int idx = UnityEngine.Random.Range(0, _available.Count);
        var t = _available[idx];
        _available.RemoveAt(idx);
        _used.Add(t);
        return t;
    }
}

public interface ITimerService
{
    void Set(float freeze, float match);
    void Tick(float delta, System.Action onFreezeEnd, System.Action onMatchEnd);
    float Freeze { get; }
    float Match { get; }
}
public class TimerService : ITimerService
{
    public float Freeze { get; private set; } = -1;
    public float Match { get; private set; } = -1;
    public void Set(float freeze, float match){ Freeze = freeze; Match = match; }
    public void Tick(float delta, System.Action onFreezeEnd, System.Action onMatchEnd){
        if (Freeze > 0) { Freeze -= delta; if (Freeze <= 0){ Freeze = -1; onFreezeEnd?.Invoke(); } }
        if (Freeze >= 0) return; // ainda congelado
        if (Match > 0){ Match -= delta; if (Match <= 0){ Match = -1; onMatchEnd?.Invoke(); } }
    }
}
