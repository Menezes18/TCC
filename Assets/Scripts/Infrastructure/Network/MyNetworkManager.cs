using System;
using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using kcp2k;
using UnityEngine;
using Mirror.FizzySteam;
using Random = UnityEngine.Random;


[System.Serializable]
public class DataPlayer
{
    public ulong steamID;
    public string playerName;
    public int points;
    public int color;
}
[System.Serializable]
public class PlayerScoreboard
{
    public List<DataPlayer> players = new List<DataPlayer>();
}
[System.Serializable]
public class MyNetworkManager : NetworkManager, ISubjectPontos
{

    public static bool isMulitplayer;
    public static MyNetworkManager manager { get; internal set; }

    public List<PlayerData> allClients = new List<PlayerData>();
    public int indexScene = 0;
    public int minJogadores = 1;
    public Dictionary<ulong, int> lastGameResults = new Dictionary<ulong, int>();
    [SerializeField]
    public PlayerScoreboard scoreboard = new PlayerScoreboard();
    public Dictionary<ulong, DataPlayer> pointsBoard = new Dictionary<ulong, DataPlayer>();
    public HSteamNetConnection steamConnection = HSteamNetConnection.Invalid;
    public bool startGame = false;

    [Header("Minigame Flow")]
    [SerializeField] private MinigameCatalog minigameCatalog;

    [SerializeField, Tooltip("Ordem atual de cenas a serem carregadas pelo fluxo de minigames.")]
    private List<string> _sceneRotation = new List<string>();
    [SerializeField, Tooltip("Quando verdadeiro, a ordem em `_sceneRotation` pode ser ajustada manualmente no inspector para fins de debug.")]
    private bool debugManualRotation = false;
    private readonly List<string> _activeMinigameIds = new List<string>();
    private readonly Dictionary<string, MinigameCatalog.MinigameEntry> _catalogById =
        new Dictionary<string, MinigameCatalog.MinigameEntry>(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _workingSceneBuffer = new List<string>();

    public IReadOnlyList<string> SceneRotation => _sceneRotation;
    
    /// <summary>
    /// Gets the list of currently active minigame IDs (enabled in MinigameSelection).
    /// </summary>
    public IReadOnlyList<string> ActiveMinigameIds => _activeMinigameIds;

    // Telemetry: per-client load progress and start times (server-side)
    private readonly Dictionary<ulong, float> _clientLoadProgress = new();
    private readonly Dictionary<ulong, float> _clientLoadStartTs = new();

    static ulong nextFakeId = 1;
    public List<IObserverPontos> _observers = new List<IObserverPontos>();
    public event Action onClientsChanged;
    public override void Awake()
    {
        var managers = FindObjectsByType<MyNetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        if (managers.Length > 1)
        {
            System.Array.Sort(managers, (a, b) => a.gameObject.GetInstanceID().CompareTo(b.gameObject.GetInstanceID()));

            if (managers[0] != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (manager == null)
        {
            manager = this;
            DontDestroyOnLoad(gameObject);
        }

        InitializeMinigameFlow();
        EnsureSceneTransitionManager();

        //     if (UIManager.Instance != null)
        // UIManager.Instance.SpawnLocalUI();
        base.Awake();
    }

    /// <summary>
    /// Ensures SceneTransitionManager exists in the scene.
    /// </summary>
    private void EnsureSceneTransitionManager()
    {
        if (SceneTransitionManager.singleton != null)
            return;

        GameObject go = new GameObject("SceneTransitionManager");
        go.AddComponent<SceneTransitionManager>();
        Debug.Log("[MyNetworkManager] Created SceneTransitionManager singleton");
    }

    /// <summary>
    /// Called when server starts. Ensures SceneTransitionManager is created and spawned.
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureSceneTransitionManager();
        
        // Register server handlers for scene transition
        if (SceneTransitionManager.singleton != null)
        {
            SceneTransitionManager.singleton.RegisterServerHandlers();
        }
        
        Debug.Log("[MyNetworkManager] OnStartServer - SceneTransitionManager ensured and handlers registered");
    }

    [Server]
    public void StoreLastResults(Dictionary<ulong, int> results)
    {
        lastGameResults = new Dictionary<ulong, int>(results);
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (conn.identity != null && allClients.Exists(c => c == conn.identity.GetComponent<PlayerData>()))
            return;

        base.OnServerAddPlayer(conn);
        if (BriefingManager.singleton != null)
        {
            Debug.Log($"🧭 [BRIEFING] Singleton = {BriefingManager.singleton}");
            Debug.Log($"🧭 [BRIEFING] GameObject = {BriefingManager.singleton.gameObject.name}");
            Invoke("UpdateSlots", 0.8f);

        }
    }

    public void UpdateSlots()
    {
        BriefingManager.singleton.UpdateAllClientsSlots();
    }
    public void RegisterNewPlayer(PlayerData pd)
    {
        ulong id = pd.playerInfo.steamId;
        string name = pd.playerInfo.username;

        if (!pointsBoard.ContainsKey(id))
        {
            int assignedColor = PlayerList.singleton.RequestRandomColor();
            var dp = new DataPlayer
            {
                steamID = id,
                playerName = name,
                points = 0,
                color = assignedColor
            };
            pointsBoard[id] = dp;
            scoreboard.players.Add(dp);

            pd.color = assignedColor;
            pd.score = 0;
            pd.alias = name;
        }
        else
        {
            var stored = pointsBoard[id];
            pd.color = stored.color;
            pd.score = stored.points;
            pd.alias = stored.playerName;
        }

        Notifica();
    }
    [Server]
    public void AddPoints(ulong steamID, int pointsToAdd)
    {
        if (pointsBoard.ContainsKey(steamID))
        {
            DataPlayer data = pointsBoard[steamID];
            data.points += pointsToAdd;

            pointsBoard[steamID] = data;

            var player = scoreboard.players.Find(p => p.steamID == steamID);
            if (player != null)
            {
                player.points = data.points;
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ [POINTS] Jogador SteamID={steamID} não consta no pointsBoard.");
        }
        Notifica();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        var client = conn.identity?.GetComponent<PlayerData>();
        if (client != null)
        {
            allClients.Remove(client);

            var sid = client.playerInfo.steamId;
            if (pointsBoard.ContainsKey(sid))
            {
                pointsBoard.Remove(sid);
                onClientsChanged?.Invoke();
                scoreboard.players.RemoveAll(p => p.steamID == sid);
                PlayerList.singleton.players.Remove(client);
            }
            PlayerList.singleton.RemoveFromList(client);
        }
        base.OnServerDisconnect(conn);
    }

    public void StartDevHost()
    {

        var fizzy = GetComponent<FizzySteamworks>();
        if (fizzy != null) Destroy(fizzy);
        var steamLobby = GetComponent<SteamLobby>();
        if (steamLobby != null) Destroy(steamLobby);
        var lobbycontroller = GetComponent<LobbyController>();
        if (lobbycontroller != null) Destroy(lobbycontroller);

        var kcp = GetComponent<KcpTransport>();

        transport = kcp;
        Transport.active = kcp;

        StartHost();
        if (MainMenu.instance != null) MainMenu.instance.gameObject.SetActive(false);
    }

    public void StartDevClient(string address = "localhost")
    {

        var fizzy = GetComponent<FizzySteamworks>();
        if (fizzy != null) Destroy(fizzy);
        var kcp = GetComponent<KcpTransport>();

        transport = kcp;
        Transport.active = kcp;

        networkAddress = address;
        StartClient();
        if (MainMenu.instance != null) MainMenu.instance.gameObject.SetActive(false);
    }

    public override void OnStartClient()
    {
        if (isMulitplayer)
        {
            if (MainMenu.instance != null)
                MainMenu.instance.SetMenuState(MenuState.InParty);
            
            if (PopupManager.instance != null)
                PopupManager.instance.Popup_Close();
        }

        base.OnStartClient();
        
        // Register client handlers for scene transition
        if (SceneTransitionManager.singleton != null)
        {
            SceneTransitionManager.singleton.RegisterClientHandlers();
        }
        
        StartCoroutine(HideLoadingScreenAfterClientStart());
    }
    
    private IEnumerator HideLoadingScreenAfterClientStart()
    {

        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.5f);
        

        if (LoadingScreenUI.Instance != null && LoadingScreenUI.Instance.gameObject.activeSelf)
        {

            if (BriefingManager.singleton == null)
            {
                Debug.Log("[MyNetworkManager] OnStartClient - Hiding loading screen (no scene change detected)");
                LoadingScreenUI.Instance.Hide();
            }
        }
    }

    public override void OnStopClient()
    {
        Debug.Log("[MyNetworkManager] OnStopClient called - cleaning up client state");
        
        // Unregister client handlers
        if (SceneTransitionManager.singleton != null)
        {
            SceneTransitionManager.singleton.UnregisterClientHandlers();
        }
        
        // Limpa o estado do SceneTransitionManager para evitar que o cliente fique preso
        if (SceneTransitionManager.singleton != null)
        {
            SceneTransitionManager.singleton.CleanupClientState();
        }
        
        // Esconde a tela de loading se estiver visível
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.Hide();
            Debug.Log("[MyNetworkManager] Hiding loading screen on client disconnect");
        }
        
        if (isMulitplayer)
        {
            if (MainMenu.instance != null)
                MainMenu.instance.SetMenuState(MenuState.Home);
        }

        // Ensure local state is cleaned up even if OnClientDisconnect wasn't called (e.g. voluntary leave)
        CleanupClientLocalState();

        base.OnStopClient();
    }
    
    /// <summary>
    /// Chamado quando o cliente é desconectado do servidor (voluntária ou involuntariamente).
    /// Este é o ponto onde fazemos cleanup quando o host fecha a conexão.
    /// </summary>
    public override void OnClientDisconnect()
    {
        Debug.Log("[MyNetworkManager] OnClientDisconnect called - client was disconnected from server");
        
        // Limpa o estado do SceneTransitionManager
        if (SceneTransitionManager.singleton != null)
        {
            SceneTransitionManager.singleton.CleanupClientState();
        }
        
        // Esconde a tela de loading imediatamente
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.Hide();
            Debug.Log("[MyNetworkManager] Hiding loading screen on client disconnect");
        }
        
        // Limpa o SteamLobby
        if (SteamLobby.instance != null)
        {
            SteamLobby.instance.CloseCurrentLobby();
        }
        
        // Limpa estados locais do cliente
        CleanupClientLocalState();
        
        base.OnClientDisconnect();
    }
    
    /// <summary>
    /// Limpa estados locais do cliente que podem causar problemas ao reconectar.
    /// </summary>
    private void CleanupClientLocalState()
    {
        Debug.Log("[MyNetworkManager] CleanupClientLocalState - cleaning up local client state");
        
        // Limpa a lista de clientes local
        allClients.Clear();
        
        // Reseta flags
        startGame = false;
        
        // Limpa eventos dos ScriptableObjects para evitar referências a objetos destruídos
        ClearAllScriptableObjectEvents();
        
        // Limpa o estado do PlayerList se existir
        if (PlayerList.singleton != null)
        {
            // Não chama ClearAllPlayers aqui pois isso é uma operação de servidor
            // Apenas reseta o singleton se necessário
        }
    }
    
    /// <summary>
    /// Limpa todos os eventos dos ScriptableObjects de input/controle.
    /// Isso é necessário porque os SOs persistem entre sessões e podem manter
    /// referências a objetos destruídos quando o jogador desconecta e reconecta.
    /// </summary>
    private void ClearAllScriptableObjectEvents()
    {
        Debug.Log("[MyNetworkManager] Clearing all ScriptableObject events");
        
        // Encontra e limpa PlayerInputSO
        var playerInputSOs = Resources.FindObjectsOfTypeAll<PlayerInputSO>();
        foreach (var so in playerInputSOs)
        {
            if (so != null)
            {
                so.ClearAllEvents();
            }
        }
        
        // Encontra e limpa PlayerControlsSO
        var playerControlsSOs = Resources.FindObjectsOfTypeAll<PlayerControlsSO>();
        foreach (var so in playerControlsSOs)
        {
            if (so != null)
            {
                so.ClearAllEvents();
            }
        }
        
        // Encontra e limpa HUDSO
        var hudSOs = Resources.FindObjectsOfTypeAll<HUDSO>();
        foreach (var so in hudSOs)
        {
            if (so != null)
            {
                so.ClearAllEvents();
            }
        }
        
        // Encontra e limpa PlayerDataSO
        var playerDataSOs = Resources.FindObjectsOfTypeAll<PlayerDataSO>();
        foreach (var so in playerDataSOs)
        {
            if (so != null)
            {
                so.ClearAllEvents();
            }
        }
        
        Debug.Log("[MyNetworkManager] All ScriptableObject events cleared");
    }
    
    public override void OnStopHost()
    {
        Debug.Log("[MyNetworkManager] OnStopHost called - cleaning up host state");
        CleanupNetworkState();
        base.OnStopHost();
    }
    
    public override void OnStopServer()
    {
        Debug.Log("[MyNetworkManager] OnStopServer called - cleaning up server state");
        
        // Unregister server handlers
        if (SceneTransitionManager.singleton != null)
        {
            SceneTransitionManager.singleton.UnregisterServerHandlers();
        }
        
        CleanupNetworkState();
        base.OnStopServer();
    }
    
    /// <summary>
    /// Limpa todo o estado de rede para permitir uma nova conexão limpa.
    /// Chamado quando o host/servidor para.
    /// </summary>
    private void CleanupNetworkState()
    {
        Debug.Log("[MyNetworkManager] CleanupNetworkState - resetting all network state");
        
        // Limpa a lista de clientes
        allClients.Clear();
        
        // Limpa o scoreboard e pointsBoard
        scoreboard.players.Clear();
        pointsBoard.Clear();
        lastGameResults.Clear();
        
        // Limpa os observers
        _observers.Clear();
        
        // Limpa telemetry
        _clientLoadProgress.Clear();
        _clientLoadStartTs.Clear();
        
        // Reseta o índice de cena
        indexScene = 0;
        startGame = false;
        
        // Limpa a rotação de cenas
        _sceneRotation.Clear();
        _activeMinigameIds.Clear();
        
        // Limpa o estado do SceneTransitionManager
        if (SceneTransitionManager.singleton != null)
        {
            SceneTransitionManager.singleton.CleanupClientState();
        }
        
        // Limpa o PlayerList
        if (PlayerList.singleton != null)
        {
            PlayerList.singleton.ClearAllPlayers();
        }
        
        // Esconde tela de loading
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.Hide();
        }
        
        Debug.Log("[MyNetworkManager] Network state cleaned up successfully");
    }

    public void SetMultiplayer(bool value)
    {
        isMulitplayer = value;

        if (isMulitplayer)

            NetworkServer.dontListen = false;
        else

            NetworkServer.dontListen = true;
    }


    public void Adicionar(IObserverPontos observer)
    {
        if (observer == null)
            return;

        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Retira(IObserverPontos observer)
    {
        if (observer == null)
            return;

        _observers.Remove(observer);
    }

    public void Notifica()
    {
        string[] nomesJogadores = new string[scoreboard.players.Count];
        int[] pontosJogadores = new int[scoreboard.players.Count];
        int[] corplayer = new int[scoreboard.players.Count];

        for (int i = 0; i < scoreboard.players.Count; i++)
        {
            nomesJogadores[i] = scoreboard.players[i].playerName;
            pontosJogadores[i] = scoreboard.players[i].points;
            corplayer[i] = scoreboard.players[i].color;
        }

        for (int i = _observers.Count - 1; i >= 0; i--)
        {
            if (_observers[i] is UnityEngine.Object unityObj && unityObj == null)
            {
                _observers.RemoveAt(i);
            }
        }

        foreach (IObserverPontos observer in _observers)
        {
            observer.Atualizacao(this, pontosJogadores, nomesJogadores);
        }
    }
    public void listaAleatoria()
    {
        if (!EnsureCatalogAssigned())
            return;

        RebuildMinigameScenes(true);
    }

    public void ReiniciarJogo()
    {
        limparPontos();
        limparLista();
        startGame = false;
        
        // Limpar dados antigos de vitória
        if (VictoryDataManager.Instance != null)
        {
            VictoryDataManager.Instance.ClearVictoryData();
            Debug.Log("🧹 [MyNetworkManager] Dados de vitória limpos ao reiniciar jogo");
        }
    }
    [Server]
    public void ResetAllPlayersReady()
    {
        foreach (PlayerData pd in allClients)
        {
            pd.IsReady = false;
        }
    }
    public bool AllPlayersReady()
    {
        foreach (PlayerData client in allClients)
            if (!client.IsReady)
                return false;
        return true;
    }
    public (int ready, int total) GetReadyCounts()
    {
        int total = allClients.Count;
        int ready = 0;
        foreach (var pd in allClients)
            if (pd.IsReady) ready++;
        return (ready, total);
    }
    public void limparPontos()
    {
        for (int i = 0; i < scoreboard.players.Count; i++)
        {
            scoreboard.players[i].points = 0;
        }
    }
    public void limparLista()
    {
        indexScene = 0;

        if (!EnsureCatalogAssigned())
            return;

        ResetActiveMinigamesToDefaults();
        RebuildMinigameScenes(true);
    }

    public void tirarMiniGames(string minigameId)
    {
        if (string.IsNullOrWhiteSpace(minigameId))
            return;

        if (!EnsureCatalogAssigned())
            return;

        if (!_catalogById.ContainsKey(minigameId))
        {
            Debug.LogWarning($"🎮 [MINIGAME] Tentativa de remover minigame desconhecido: '{minigameId}'.");
            return;
        }

        if (_activeMinigameIds.Remove(minigameId))
        {
            Debug.Log($"🎮 [MINIGAME] {minigameId} removido da rotação ");
            RebuildMinigameScenes(true);
        }
    }
    public void AdicionarMiniGames(string minigameId)
    {
        if (string.IsNullOrWhiteSpace(minigameId))
            return;

        if (!EnsureCatalogAssigned())
            return;

        if (!_catalogById.TryGetValue(minigameId, out var entry))
        {
            Debug.LogWarning($"🎮 [MINIGAME] Tentativa de adicionar minigame desconhecido: '{minigameId}'.");
            return;
        }

        if (_activeMinigameIds.Contains(minigameId))
            return;

        _activeMinigameIds.Add(minigameId);
        Debug.Log($"🎮 [MINIGAME] {entry.displayName ?? minigameId} adicionado à rotação");
        RebuildMinigameScenes(true);
    }

    private void InitializeMinigameFlow()
    {
        if (!EnsureCatalogAssigned())
        {
            _sceneRotation.Clear();
            return;
        }

        _catalogById.Clear();
        foreach (var entry in minigameCatalog.Entries)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.id) || !entry.HasValidScene)
            {
                Debug.LogWarning($"[MyNetworkManager] Entrada de minigame ignorada por falta de ID ou cena. ID='{entry?.id}'.", minigameCatalog);
                continue;
            }

            _catalogById[entry.id] = entry;
        }

        ResetActiveMinigamesToDefaults();
        RebuildMinigameScenes(true);
    }

    private void ResetActiveMinigamesToDefaults()
    {
        _activeMinigameIds.Clear();
        foreach (var entry in minigameCatalog.GetDefaultEntries())
        {
            if (!_activeMinigameIds.Contains(entry.id))
                _activeMinigameIds.Add(entry.id);
        }

        if (_activeMinigameIds.Count == 0)
        {
            foreach (var entry in minigameCatalog.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;

                if (!_activeMinigameIds.Contains(entry.id))
                    _activeMinigameIds.Add(entry.id);
            }
        }
    }

    private void RebuildMinigameScenes(bool shuffle)
    {
        _workingSceneBuffer.Clear();

        foreach (var id in _activeMinigameIds)
        {
            if (_catalogById.TryGetValue(id, out var entry) && entry.HasValidScene)
            {
                _workingSceneBuffer.Add(entry.SceneIdentifier);
            }
        }

        if (debugManualRotation)
        {
            EnsureManualRotationConsistency();
        }
        else
        {
            _sceneRotation.Clear();
            if (shuffle)
                ShuffleList(_workingSceneBuffer);
            _sceneRotation.AddRange(_workingSceneBuffer);
        }

        AppendVictoryScene();

        indexScene = Mathf.Clamp(indexScene, 0, Mathf.Max(0, _sceneRotation.Count - 1));
    }

    private void ShuffleList(List<string> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int swapIndex = Random.Range(i, list.Count);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    private void EnsureManualRotationConsistency()
    {
        _sceneRotation.RemoveAll(scene => !_workingSceneBuffer.Contains(scene));

        foreach (var scene in _workingSceneBuffer)
        {
            if (!_sceneRotation.Contains(scene))
                _sceneRotation.Add(scene);
        }
    }

    private void AppendVictoryScene()
    {
        var victoryScene = GetVictorySceneIdentifier();
        if (string.IsNullOrWhiteSpace(victoryScene))
            return;

        _sceneRotation.RemoveAll(scene => string.Equals(scene, victoryScene, StringComparison.OrdinalIgnoreCase));
        _sceneRotation.Add(victoryScene);
    }

    private string GetVictorySceneIdentifier()
    {
        var identifier = minigameCatalog.VictorySceneIdentifier;
        return string.IsNullOrWhiteSpace(identifier) ? null : identifier;
    }

    private bool IsVictoryScene(string sceneName)
    {
        var victoryScene = GetVictorySceneIdentifier();
        return !string.IsNullOrWhiteSpace(victoryScene) &&
               string.Equals(sceneName, victoryScene, StringComparison.OrdinalIgnoreCase);
    }

    private void HandleVictorySceneLoaded()
    {
        indexScene = 0;

        if (!debugManualRotation)
        {
            listaAleatoria();
        }

        // Reset the minigame rotation state for voting system
        if (MinigameRotationState.Instance != null)
        {
            MinigameRotationState.Instance.Reset();
            Debug.Log("🔄 [VOTING] MinigameRotationState reset after victory scene loaded");
        }

        // Reset game state - require all players to ready up again
        Debug.Log("🔄 [VICTORY] Resetting game state - players must ready up again");
        startGame = false;
        ResetAllPlayersReady();
        
        // IMPORTANTE: Garantir que VictoryDataManager está spawnado e detectar vencedor
        StartCoroutine(EnsureVictoryDataManagerAndDetectWinner());
    }
    
    /// <summary>
    /// Detecta o vencedor quando a cena de vitória carrega
    /// SIMPLIFICADO: VictoryDataManager é um Scene Object, Mirror sincroniza automaticamente!
    /// </summary>
    private System.Collections.IEnumerator EnsureVictoryDataManagerAndDetectWinner()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🏆 [MyNetworkManager] Detectando vencedor na cena de vitória");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        // Aguardar um pouco para garantir que a cena está carregada
        yield return new WaitForSeconds(0.5f);
        
        // Verificar se VictoryDataManager existe (deve estar na cena de vitória)
        if (VictoryDataManager.Instance == null)
        {
            Debug.LogError("❌ [MyNetworkManager] VictoryDataManager.Instance é NULL!");
            Debug.LogError("   → Adicione o GameObject VictoryDataManager na CENA DE VITÓRIA");
            Debug.LogError("   → Com componentes: VictoryDataManager.cs + NetworkIdentity");
            yield break;
        }
        
        Debug.Log("✅ [MyNetworkManager] VictoryDataManager encontrado (Scene Object)");
        
        // Verificar NetworkIdentity (deve estar configurado na cena)
        var netIdentity = VictoryDataManager.Instance.GetComponent<NetworkIdentity>();
        if (netIdentity == null)
        {
            Debug.LogError("❌ [MyNetworkManager] VictoryDataManager não tem NetworkIdentity!");
            Debug.LogError("   → Adicione NetworkIdentity ao GameObject no Inspector");
            yield break;
        }
        
        Debug.Log($"✅ [MyNetworkManager] NetworkIdentity configurado");
        Debug.Log($"   → netId: {netIdentity.netId}");
        Debug.Log($"   → sceneId: {netIdentity.sceneId}");
        Debug.Log($"   → isServer: {netIdentity.isServer}");
        Debug.Log($"   → observers: {netIdentity.observers?.Count ?? 0}");
        
        // Scene Objects são automaticamente sincronizados pelo Mirror!
        // Não precisa spawnar manualmente
        
        // Detectar e sincronizar vencedor
        Debug.Log("🏆 [MyNetworkManager] Chamando DetectAndSyncWinner...");
        VictoryDataManager.Instance.DetectAndSyncWinner();
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("✅ [MyNetworkManager] Detecção concluída");
        Debug.Log("   → Mirror vai sincronizar automaticamente (Scene Object)");
        Debug.Log("═══════════════════════════════════════════════════════");
    }

    private bool EnsureCatalogAssigned()
    {
        if (minigameCatalog != null)
            return true;

        Debug.LogError("[MyNetworkManager] MinigameCatalog não atribuído. Configure um MinigameCatalog no inspector para gerenciar a rotação de minigames.", this);
        return false;
    }

    public void AdvanceScenePointer()
    {
        if (_sceneRotation.Count == 0)
        {
            indexScene = 0;
            return;
        }

        indexScene++;
        if (indexScene >= _sceneRotation.Count)
            indexScene = 0;
    }

    public bool TryGetSceneNameAt(int orderIndex, out string sceneName)
    {
        if (orderIndex >= 0 && orderIndex < _sceneRotation.Count)
        {
            sceneName = _sceneRotation[orderIndex];
            return true;
        }

        sceneName = null;
        return false;
    }

    /// <summary>
    /// Server: Changes scene using synchronized preload system.
    /// All clients preload the scene before it activates.
    /// </summary>
    [Server]
    public void ServerChangeSceneSynchronized(string sceneName)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("[MyNetworkManager] ServerChangeSceneSynchronized can only be called on server!");
            return;
        }

        Debug.Log($"[MyNetworkManager] ServerChangeSceneSynchronized called for scene '{sceneName}'");

        // Try to ensure SceneTransitionManager exists
        EnsureSceneTransitionManager();

        // Check again after ensuring
        if (SceneTransitionManager.singleton == null)
        {
            Debug.LogWarning("[MyNetworkManager] SceneTransitionManager STILL not found after ensure, falling back to standard scene change");
            NetworkManager.singleton.ServerChangeScene(sceneName);
            return;
        }

        Debug.Log($"[MyNetworkManager] SceneTransitionManager found! Starting synchronized scene change to '{sceneName}'");
        SceneTransitionManager.singleton.ServerChangeSceneSynchronized(sceneName);
    }

    // ===== Mirror scene hooks to integrate loading UI and wait-for-all =====
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        LoadingScreenUI.Ensure();
        LoadingScreenUI.Instance?.SetMirrorTargetScene(newSceneName);
        LoadingScreenUI.Instance?.ShowForMirror();

        
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        
        StartCoroutine(CheckAndHideLoadingScreenIfNoBriefing());
    }
    
    private IEnumerator CheckAndHideLoadingScreenIfNoBriefing()
    {
        yield return null;
        yield return null;
        
        if (BriefingManager.singleton == null)
        {
            Debug.Log("[MyNetworkManager] No BriefingManager in scene - hiding loading screen on client");
            LoadingScreenUI.Instance?.Hide();
        }
        else
        {
            Debug.Log("[MyNetworkManager] BriefingManager found - waiting for RpcShowBriefing to hide loading");
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        // reset telemetry for new scene
        _clientLoadProgress.Clear();
        _clientLoadStartTs.Clear();

        if (IsVictoryScene(sceneName))
            HandleVictorySceneLoaded();

        StartCoroutine(WaitAllConnectionsReadyThenStart());
    }

    private IEnumerator WaitAllConnectionsReadyThenStart()
    {
        float lastLog = 0f;
        float startTime = Time.realtimeSinceStartup;
        float maxWaitTime = 60f; // Timeout máximo de 60 segundos
        
        // Wait until all authenticated connections became ready after the load
        while (!AreAllConnectionsReady())
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            
            // Check for timeout
            if (elapsed >= maxWaitTime)
            {
                Debug.LogWarning($"[MyNetworkManager] Timeout waiting for all connections to be ready after {maxWaitTime}s. Proceeding anyway.");
                LogProgressSnapshot(final: true);
                break;
            }
            
            // every ~1s, log a telemetry snapshot
            if (Time.realtimeSinceStartup - lastLog > 1f)
            {
                lastLog = Time.realtimeSinceStartup;
                LogProgressSnapshot();
            }
            yield return null;
        }

        Debug.Log("[MyNetworkManager] All clients loaded and are ready.");
        LogProgressSnapshot(final: true);
        
        // Start the briefing flow so clients can confirm readiness
        if (BriefingManager.singleton != null && NetworkServer.active)
        {
            // Cena com BriefingManager (minigame): congelar jogadores e mostrar briefing
            if (PlayerList.singleton != null)
            {
                PlayerList.singleton.SetAllPlayersFrozen(true);
                Debug.Log("[MyNetworkManager] All players frozen before briefing");
            }
            BriefingManager.singleton.TriggerBriefing();
        }
        else
        {
            // Cena SEM BriefingManager (lobby, RASCUNHO, etc): descongelar jogadores
            // A loading screen será escondida pelo cliente em CheckAndHideLoadingScreenIfNoBriefing
            Debug.Log("[MyNetworkManager] No BriefingManager in scene - clients will hide loading screen");
            
            if (PlayerList.singleton != null)
            {
                PlayerList.singleton.SetAllPlayersFrozen(false);
                Debug.Log("[MyNetworkManager] Players unfrozen in lobby scene");
            }
        }
    }

    private void LogProgressSnapshot(bool final = false)
    {
        // Build readable telemetry per player
        var list = PlayerList.singleton?.players;
        if (list == null) return;
        List<string> lines = new List<string>();
        float min = 1f, max = 0f, sum = 0f; int count = 0;
        foreach (var pd in list)
        {
            ulong sid = pd.playerInfo.steamId;
            string alias = string.IsNullOrEmpty(pd.alias) ? sid.ToString() : pd.alias;
            float prog = _clientLoadProgress.TryGetValue(sid, out var p) ? p : 0f;
            min = Mathf.Min(min, prog); max = Mathf.Max(max, prog); sum += prog; count++;
            float startTs = _clientLoadStartTs.TryGetValue(sid, out var ts) ? ts : -1f;
            string dur = "";
            if (final && startTs >= 0f)
            {
                float total = Time.realtimeSinceStartup - startTs;
                dur = $" | took {total:0.00}s";
            }
            lines.Add($" - {alias} ({sid}): {(int)(prog*100)}%{dur}");
        }
        float avg = count > 0 ? sum / count : 0f;
        string header = final ? "[Telemetry] Final load progress:" : "[Telemetry] Load progress:";
        Debug.Log(header + $" min={(int)(min*100)}% avg={(int)(avg*100)}% max={(int)(max*100)}%\n" + string.Join("\n", lines));
    }

    private bool AreAllConnectionsReady()
    {
        foreach (var kvp in NetworkServer.connections)
        {
            var conn = kvp.Value;
            if (conn == null) continue;
            if (!conn.isAuthenticated) return false;
            if (!conn.isReady) return false;
        }
        return true;
    }

    // Called by clients right after briefing UI is shown (via RpcShowBriefing)
    public void RecordClientBriefingShown()
    {
        // Redirect to BriefingManager's Command (requiresAuthority=false)
        BriefingManager.singleton?.CmdMarkClientReady();
    }

    [Server]
    public void ServerMarkAllReady()
    {
        foreach (var pd in allClients)
            pd.IsReady = true;
        BriefingManager.singleton?.UpdateAllClientsSlots();
        BriefingManager.singleton?.CheckAllReady();
    }

    // [Server] entrypoint for per-client progress reports
    [Server]
    public void ServerRecordClientLoadProgress(ulong steamId, string sceneName, float progress)
    {
        if (!_clientLoadStartTs.ContainsKey(steamId))
        {
            _clientLoadStartTs[steamId] = Time.realtimeSinceStartup;
            Debug.Log($"[Telemetry] Client {steamId} started loading '{sceneName}'");
        }
        _clientLoadProgress[steamId] = Mathf.Clamp01(progress);
        if (progress >= 0.999f)
        {
            float start = _clientLoadStartTs.TryGetValue(steamId, out var ts) ? ts : Time.realtimeSinceStartup;
            float dur = Time.realtimeSinceStartup - start;
            Debug.Log($"[Telemetry] Client {steamId} finished loading '{sceneName}' in {dur:0.00}s");
        }
    }
}
