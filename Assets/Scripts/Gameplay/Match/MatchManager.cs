using System;
using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Smooth;
using Random = UnityEngine.Random;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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

    [ClientRpc]
    void RpcShowSimpleResults(string[] names, int[] totals, int[] gains, Color32[] colors)
    {
        StartCoroutine(ShowResultsRoutine(names, totals, gains, colors));
    }

    IEnumerator ShowResultsRoutine(string[] names, int[] totals, int[] gains, Color32[] colors)
    {
        const string overlayScene = "ResultsOverlay"; 
        var scn = SceneManager.GetSceneByName(overlayScene);
        if (!scn.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(overlayScene, LoadSceneMode.Additive);
            if (op != null) while (!op.isDone) yield return null;
            // aguarda um frame para inicializar
            yield return null;
        }

        var ui = FindAnyObjectByType<ResultsUI>();
        if (ui == null)
        {
            Debug.LogWarning("[ResultsUI] SimpleResultsUI não encontrado na cena aditiva 'ResultsOverlay'.");
            yield break;
        }
        ui.Show(names, totals, gains, colors);
    }

    [Server]
    public IEnumerator WaitAndReturnToLobby(float wait)
    {
        yield return new WaitForSeconds(wait);
        _activePlayers.Clear();
        _winnerPlayers .Clear();
        
        // Always return to lobby (RASCUNHO) after each minigame
        MyNetworkManager.manager.ServerChangeSceneSynchronized("RASCUNHO");
    }

    #endregion

    PlayerList playerList => PlayerList.singleton;
    [SerializeField] Database db;
    [SerializeField] SettingsMiniGameData settingsMiniGameData; 
    [SerializeField] HUDSO HUDSO;
    [SerializeField, Min(0f)] private float resultsOverlayDelaySeconds = 5f;
    private List<PlayerScoreEntry> _temporaryRanking = new List<PlayerScoreEntry>();
    private HashSet<NetworkConnectionToClient> _readyConnections = new();
    
    private IScoreRule scoreRule;
    // Evita processar o fim de partida mais de uma vez
    private bool _resultsFinalized;

    
    [SyncVar (hook = nameof(HookOnFreezeTimerUpdated))] float _freezeTimer;
    [SyncVar (hook = nameof(HookOnMatchTimerUpdated))] float _matchTimer;
    [SyncVar (hook = nameof(HookOnGameOver))] string _gameOver;
    
    
    [SerializeField] List<Transform> _spawns;
    List<Transform> _excludedSpawns = new List<Transform>();

    List<PlayerData> _activePlayers = new List<PlayerData>();
    List<PlayerData> _winnerPlayers = new List<PlayerData>();
    public GameObject acabarFreezeTime;
    
    
    private bool _matchHasStarted;
    public float MatchTimer => _matchTimer;

    [Server]
    public void SetMatchTimer(float value)
    {
        _matchTimer = value;
    }
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
            Debug.Log("❄️<color=blue> [MATCH] FreezeTime acabou</color>");
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
        _matchTimer = settingsMiniGameData.miniGameDuration;
        //_matchHasStarted = true;
        _resultsFinalized = false;
    }

    [Server]
    public void StartMatch()
    {
        _freezeTimer = db.serverFreezeDuration;
        _matchHasStarted = true;
    }


    [Command(requiresAuthority = false)]
    public void CmdStartMatchAfterCamera()
    {
        StartMatch();
    }
    private void TeleportPlayer()
    {
        var mc = FindFirstObjectByType<MinigameController>();
        if (mc != null && mc.HandlesInitialSpawns)
        {
            // Minigame vai cuidar do teleporte inicial
            return;
        }
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
        // Proteção contra chamadas repetidas (ex.: morte tardia após resultados)
        if (_resultsFinalized)
        {
            Debug.Log("⚠️ [MATCH] InternalEndMatch já processado – ignorando chamada duplicada");
            return;
        }

        Debug.Log("🏁 [MATCH] Fim de partida – encerrando e atribuindo pontos");
        _matchHasStarted = false;
        _matchTimer = -1;
        _freezeTimer = -1;

        scoreRule.AssignFinalPoints();

        UpdateTemporaryRanking();

        // Guarda resultados (ganhos) deste minigame
        var miniResults = scoreRule.GetResults();
        MyNetworkManager.manager.StoreLastResults(miniResults);

        // Aplica pontos finais na tabela
        foreach (var entry in _temporaryRanking)
            MyNetworkManager.manager.AddPoints(entry.steamId, entry.score);

        // Congela jogadores
        foreach (PlayerData pd in PlayerList.singleton.players)
        {
            PlayerScript ps = pd.transform.GetComponent<PlayerScript>();
            ps.isFrozen = true;
        }

        var sb = MyNetworkManager.manager.scoreboard.players;
        int n = sb.Count;
        
        var sortedPlayers = new List<(string name, int total, int gain, Color32 color)>();
        
        for (int i = 0; i < n; i++)
        {
            var p = sb[i];
            string name = p.playerName;
            int total = p.points;
            int gain = (miniResults != null && miniResults.TryGetValue(p.steamID, out var g)) ? g : 0;
            
            Color32 color = Color.white;
            if (db != null && db.playerColors != null && p.color >= 0 && p.color < db.playerColors.Count)
                color = db.playerColors[p.color].color;
                
            sortedPlayers.Add((name, total, gain, color));
        }
        
        // Ordena apenas pelos pontos totais (maior para menor)
        sortedPlayers.Sort((a, b) => b.total.CompareTo(a.total));
        
        string[] names = new string[n];
        int[] totals = new int[n];
        int[] gains = new int[n];
        Color32[] colors = new Color32[n];
        
        for (int i = 0; i < n; i++)
        {
            names[i] = sortedPlayers[i].name;
            totals[i] = sortedPlayers[i].total;
            gains[i] = sortedPlayers[i].gain;
            colors[i] = sortedPlayers[i].color;
        }
        
        StartCoroutine(ServerSendResultsAfterDelay(names, totals, gains, colors));

        _gameOver = "Acabou!";

        // Marca como finalizado para evitar reentrada
        _resultsFinalized = true;
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
    void HookOnGameOver(string oldValue, string newValue)
    {
        HUDSO.GameOver(newValue);
    }
    
    [Server]
    private IEnumerator ServerSendResultsAfterDelay(string[] names, int[] totals, int[] gains, Color32[] colors)
    {
        float delay = Mathf.Max(0f, resultsOverlayDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        RpcShowSimpleResults(names, totals, gains, colors);

        float exitTimer = ResultsUI.singleton != null ? ResultsUI.singleton.exitTimerSeconds : 10f;
        StartCoroutine(WaitAndReturnToLobby(exitTimer));
    }
    
}
