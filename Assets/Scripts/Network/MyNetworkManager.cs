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
    public List<string> minigames;
    public int indexScene = 0;
    public int minJogadores = 1;
    public Dictionary<ulong, int> lastGameResults = new Dictionary<ulong, int>();
    [SerializeField]
    public PlayerScoreboard scoreboard = new PlayerScoreboard();
    public Dictionary<ulong, DataPlayer> pointsBoard = new Dictionary<ulong, DataPlayer>();
    public HSteamNetConnection steamConnection = HSteamNetConnection.Invalid;
    public bool startGame = false;

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

        listaAleatoria();

        //     if (UIManager.Instance != null)
        // UIManager.Instance.SpawnLocalUI();
        base.Awake();
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

    private int i = 0;
    private void UpdatePointsBoardInspector()
    {


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
        MainMenu.instance.gameObject.SetActive(false);
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
        MainMenu.instance.gameObject.SetActive(false);
    }

    public override void OnStartClient()
    {
        if (isMulitplayer)
        {
            MainMenu.instance.SetMenuState(MenuState.InParty);
            PopupManager.instance.Popup_Close();
        }

        base.OnStartClient();
    }

    public override void OnStopClient()
    {
        if (isMulitplayer)
        {
            MainMenu.instance.SetMenuState(MenuState.Home);
        }

        base.OnStopClient();
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
        _observers.Add(observer);
    }

    public void Retira(IObserverPontos observer)
    {
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

        foreach (IObserverPontos observer in _observers)
        {
            observer.Atualizacao(this, pontosJogadores, nomesJogadores);
        }
    }
    public void listaAleatoria()
    {
        int count = minigames.Count;
        for (int i = 0; i < count - 1; i++)
        {
            int rnd = Random.Range(i, count);
            // troca elementos
            string temp = minigames[i];
            minigames[i] = minigames[rnd];
            minigames[rnd] = temp;
        }

        minigames.Add("Vitoria");
    }

    public void ReiniciarJogo()
    {
        startGame = false;
        limparPontos();
        limparLista();
        listaAleatoria();
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
        minigames.Clear();
        minigames.Add("MN_Rua");
        minigames.Add("MN_Queda");
        minigames.Add("MN_Sumo");
        minigames.Add("Vitoria");
        minigames.RemoveAt(minigames.Count - 1);
    }

    public void tirarMiniGames(string minigame)
    {
        Debug.Log($"🎮 [MINIGAME] {minigame}");
        minigames.Remove(minigame);
        minigames.RemoveAt(minigames.Count - 1);
        listaAleatoria();
    }
    public void AdicionarMiniGames(string minigame)
    {
        minigames.Add(minigame);
        minigames.Remove("Vitoria");
        listaAleatoria();
    }

    // ===== Mirror scene hooks to integrate loading UI and wait-for-all =====
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        LoadingScreenUI.Ensure();
        LoadingScreenUI.Instance?.SetMirrorTargetScene(newSceneName);
        LoadingScreenUI.Instance?.ShowForMirror();

        // Safety: if Mirror skips async or finishes instantly (e.g., host already on scene), hide after short grace
        LeanTween.delayedCall(2.0f, () =>
        {
            if (NetworkManager.loadingSceneAsync == null || NetworkManager.loadingSceneAsync.isDone)
                LoadingScreenUI.Instance?.Hide();
        });
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }

    public override void OnClientSceneChanged()
    {
        LoadingScreenUI.Instance?.Hide();
        base.OnClientSceneChanged();
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        // reset telemetry for new scene
        _clientLoadProgress.Clear();
        _clientLoadStartTs.Clear();
        StartCoroutine(WaitAllConnectionsReadyThenStart());
    }

    private IEnumerator WaitAllConnectionsReadyThenStart()
    {
        float lastLog = 0f;
        // Wait until all authenticated connections became ready after the load
        while (!AreAllConnectionsReady())
        {
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
            BriefingManager.singleton.TriggerBriefing();
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
