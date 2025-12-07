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
    void RpcShowSimpleResults(string[] names, int[] totals, int[] gains, Color32[] colors, int[] hatIndices, int[] glassesIndices, int[] shirtIndices)
    {
        StartCoroutine(ShowResultsRoutine(names, totals, gains, colors, hatIndices, glassesIndices, shirtIndices));
    }

    IEnumerator ShowResultsRoutine(string[] names, int[] totals, int[] gains, Color32[] colors, int[] hatIndices, int[] glassesIndices, int[] shirtIndices)
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
        ui.Show(names, totals, gains, colors, hatIndices, glassesIndices, shirtIndices);
    }

    [Server]
    public IEnumerator WaitAndReturnToLobby(float wait)
    {
        yield return new WaitForSeconds(wait);
        _activePlayers.Clear();
        _winnerPlayers .Clear();
        
        // Tenta iniciar a votação
        if (VotingManager.Instance != null)
        {
            Debug.Log("🗳️ [MATCH] Tentando iniciar votação...");
            bool votingStarted = VotingManager.Instance.StartVotingRound();
            
            if (votingStarted)
            {
                Debug.Log("🗳️ [MATCH] Votação iniciada! Aguardando conclusão...");
                
                // Aguarda o tempo da votação
                // Adicionamos um pequeno buffer para garantir sincronia
                yield return new WaitForSeconds(VotingManager.Instance.VotingTimeRemaining + 1.0f);
                
                // Finaliza a votação e pega o vencedor
                var winner = VotingManager.Instance.EndVoting();
                
                if (winner != null && !string.IsNullOrEmpty(winner.SceneIdentifier))
                {
                    // Mark as played
                    if (MinigameRotationState.Instance != null)
                    {
                        MinigameRotationState.Instance.MarkAsPlayed(winner.id);
                    }

                    Debug.Log($"🗳️ [MATCH] Vencedor da votação: {winner.displayName}. Carregando cena: {winner.SceneIdentifier}");
                    MyNetworkManager.manager.ServerChangeSceneSynchronized(winner.SceneIdentifier);
                    yield break;
                }
                else
                {
                    Debug.LogError("❌ [MATCH] Votação terminou sem vencedor válido!");
                }
            }
            else
            {
                Debug.Log("⚠️ [MATCH] Não foi possível iniciar votação (sem minigames elegíveis?). Indo para Vitória.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [MATCH] VotingManager não encontrado!");
        }

        // Se não houve votação ou falhou, tenta ir para a cena de vitória
        // A cena de vitória geralmente é a última na rotação do MyNetworkManager
        var rotation = MyNetworkManager.manager.SceneRotation;
        if (rotation != null && rotation.Count > 0)
        {
            string lastScene = rotation[rotation.Count - 1];
            Debug.Log($"🏁 [MATCH] Carregando cena final (Vitória): {lastScene}");
            MyNetworkManager.manager.ServerChangeSceneSynchronized(lastScene);
        }
        else
        {
            // Fallback para o Lobby se tudo falhar
            Debug.LogWarning("⚠️ [MATCH] Fallback para o Lobby (RASCUNHO)");
            MyNetworkManager.manager.ServerChangeSceneSynchronized("RASCUNHO");
        }
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
        
        // Esconde PlayerHUD durante briefing/câmera inicial (com delay para garantir que HUDManager inicializou)
        LeanTween.delayedCall(0.5f, () => RpcHidePlayerHUD());
        
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
            InternalStartMatch();
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
        PlayerList.singleton.SetAllPlayersFrozen(false);
        _freezeTimer = db.serverFreezeDuration;
        _matchHasStarted = true;
        
        // Mostra PlayerHUD quando o match começar
        RpcShowPlayerHUD();
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
            
            ps.TargetRpcTeleport(conn, randomSpawn.position, randomSpawn.rotation);

            _activePlayers.Add(pd);

        }
    }

    /// <summary>
    /// Registra todos os jogadores ativos na lista _activePlayers.
    /// Usado quando o minigame gerencia seus próprios spawns ou precisa garantir que todos os jogadores estão registrados.
    /// </summary>
    [Server]
    public void RegisterAllActivePlayers()
    {
        foreach (PlayerData pd in PlayerList.singleton.players)
        {
            if (!_activePlayers.Contains(pd))
            {
                _activePlayers.Add(pd);
            }
        }
        Debug.Log($"[MatchManager] {_activePlayers.Count} jogadores registrados como ativos para verificação de término antecipado");
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
        
        var sortedPlayers = new List<(string name, int total, int gain, Color32 color, int hatIndex, int glassesIndex, int shirtIndex)>();
        
        for (int i = 0; i < n; i++)
        {
            var p = sb[i];
            string name = p.playerName;
            int total = p.points;
            int gain = (miniResults != null && miniResults.TryGetValue(p.steamID, out var g)) ? g : 0;
            
            Color32 color = Color.white;
            if (db != null && db.playerColors != null && p.color >= 0 && p.color < db.playerColors.Count)
                color = db.playerColors[p.color].color;
            
            // Busca a customização do jogador no PlayerData
            int hatIndex = -1;
            int glassesIndex = -1;
            int shirtIndex = -1;
            
            var playerData = FindPlayerDataBySteamId(p.steamID);
            if (playerData != null)
            {
                hatIndex = playerData.hatIndex;
                glassesIndex = playerData.glassesIndex;
                shirtIndex = playerData.shirtIndex;
                Debug.Log($"[MatchManager] Customização coletada para {name}: Hat={hatIndex}, Glasses={glassesIndex}, Shirt={shirtIndex}");
            }
            else
            {
                Debug.LogWarning($"[MatchManager] PlayerData não encontrado para {name} (SteamID: {p.steamID})");
            }
                
            sortedPlayers.Add((name, total, gain, color, hatIndex, glassesIndex, shirtIndex));
        }
        
        // Ordena apenas pelos pontos totais (maior para menor)
        sortedPlayers.Sort((a, b) => b.total.CompareTo(a.total));
        
        string[] names = new string[n];
        int[] totals = new int[n];
        int[] gains = new int[n];
        Color32[] colors = new Color32[n];
        int[] hatIndices = new int[n];
        int[] glassesIndices = new int[n];
        int[] shirtIndices = new int[n];
        
        for (int i = 0; i < n; i++)
        {
            names[i] = sortedPlayers[i].name;
            totals[i] = sortedPlayers[i].total;
            gains[i] = sortedPlayers[i].gain;
            colors[i] = sortedPlayers[i].color;
            hatIndices[i] = sortedPlayers[i].hatIndex;
            glassesIndices[i] = sortedPlayers[i].glassesIndex;
            shirtIndices[i] = sortedPlayers[i].shirtIndex;
        }
        
        StartCoroutine(ServerSendResultsAfterDelay(names, totals, gains, colors, hatIndices, glassesIndices, shirtIndices));

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
    
    [ClientRpc]
    private void RpcHidePlayerHUD()
    {
        HUDSO.ShowBriefing();
    }
    
    [ClientRpc]
    private void RpcShowPlayerHUD()
    {
        HUDSO.HideBriefing();
    }
    
    [Server]
    private IEnumerator ServerSendResultsAfterDelay(string[] names, int[] totals, int[] gains, Color32[] colors, int[] hatIndices, int[] glassesIndices, int[] shirtIndices)
    {
        float delay = Mathf.Max(0f, resultsOverlayDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        RpcShowSimpleResults(names, totals, gains, colors, hatIndices, glassesIndices, shirtIndices);

        float exitTimer = ResultsUI.singleton != null ? ResultsUI.singleton.exitTimerSeconds : 10f;
        // StartCoroutine(WaitAndReturnToLobby(exitTimer));
    }
    
    [Server]
    private PlayerData FindPlayerDataBySteamId(ulong steamId)
    {
        if (steamId == 0) return null;
        
        foreach (var pd in PlayerList.singleton.players)
        {
            if (pd.playerInfo.steamId == steamId)
                return pd;
        }
        
        return null;
    }
    
}
