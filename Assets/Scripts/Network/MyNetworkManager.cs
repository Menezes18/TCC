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

    [Header("Para funcionar sem a steam")]
    public bool testMode = false;
    static ulong nextFakeId = 1;
    public List<IObserverPontos> _observers = new List<IObserverPontos>();
    public event Action onClientsChanged;
    private void Awake()
    {
        MyNetworkManager[] managers = FindObjectsOfType<MyNetworkManager>();

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
    }
    public void RegisterNewPlayer(PlayerData pd)
    {
        ulong id = pd.playerInfo.steamId;
        string name = pd.playerInfo.username;

        if (!pointsBoard.ContainsKey(id))
        {
            int assignedColor = PlayerList.singleton.RequestRandomColor();
            var dp = new DataPlayer {
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
            Debug.LogWarning($"Jogador (SteamID: {steamID}) não consta no pointsBoard.");
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
        testMode = true;

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
        testMode = true;

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
        limparPontos();
        limparLista();
        listaAleatoria();
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
        minigames.RemoveAt(minigames.Count - 1);
    }
    
}
