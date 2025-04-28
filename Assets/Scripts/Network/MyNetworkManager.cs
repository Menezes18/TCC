using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using kcp2k;
using UnityEngine;
using Mirror.FizzySteam;



[System.Serializable]
public class PlayerData
{
    public ulong steamID;
    public string playerName;
    public int points;
}
[System.Serializable]
public class PlayerScoreboard
{
    public List<PlayerData> players = new List<PlayerData>();
}
[System.Serializable]
public class MyNetworkManager : NetworkManager 
{
    public static bool isMulitplayer;
    public static MyNetworkManager manager { get; internal set; }

    public List<MyClient> allClients = new List<MyClient>();
    public int minJogadores = 1;
    [SerializeField]
    public PlayerScoreboard scoreboard = new PlayerScoreboard();
    private Dictionary<ulong, PlayerData> pointsBoard = new Dictionary<ulong, PlayerData>();
    public HSteamNetConnection steamConnection = HSteamNetConnection.Invalid;
    [Header("Para funcionar sem a steam")]
    public bool testMode = false;
    static ulong nextFakeId = 1;
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
            //     if (UIManager.Instance != null)
            // UIManager.Instance.SpawnLocalUI();
        base.Awake();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (conn.identity != null && allClients.Exists(c => c == conn.identity.GetComponent<MyClient>()))
        return;

        base.OnServerAddPlayer(conn);

        MyClient client = conn.identity.GetComponent<MyClient>();
        // allClients.Add(client);
        if (testMode)
        {
            var fakeId= nextFakeId++;
            var fakeName = $"DevPlayer{fakeId:D2}";
            client.playerInfo = new PlayerInfoData(fakeName, fakeId);
        }
        else
        {
            CSteamID steamId = SteamLobby.LobbyID.m_SteamID == 0 
                ? SteamUser.GetSteamID() 
                : SteamMatchmaking.GetLobbyMemberByIndex(SteamLobby.LobbyID, allClients.Count);
            client.playerInfo = new PlayerInfoData(SteamFriends.GetFriendPersonaName(steamId), steamId.m_SteamID);
        }
        if (!pointsBoard.ContainsKey(client.playerInfo.steamId))
        {
            var playerData = new PlayerData 
            { 
                steamID = client.playerInfo.steamId,
                playerName = client.playerInfo.username,
                points = 0 
            };
            
            pointsBoard[client.playerInfo.steamId] = playerData;
            scoreboard.players.Add(playerData);
        }
        Debug.Log("Conectados" + allClients.Count);
        if(allClients.Count >= minJogadores) iniciaContador();
        CharacterSkinHandler.instance.DestroyMesh();
    }
    [Server]
    public void AddPoints(ulong steamID, int pointsToAdd)
    {
        if (pointsBoard.ContainsKey(steamID))
        {
            PlayerData data = pointsBoard[steamID];
            data.points += pointsToAdd;
            
            // Atualiza tanto o dicionário quanto a lista visível
            pointsBoard[steamID] = data;
            
            // Encontra e atualiza o jogador na lista do scoreboard
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
    }
    private void UpdatePointsBoardInspector()
    {
       
        
    }
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        var client = conn.identity?.GetComponent<MyClient>();
        if (client != null)
        {
            allClients.Remove(client);

            var sid = client.playerInfo.steamId;
            if (pointsBoard.ContainsKey(sid))
            {
                pointsBoard.Remove(sid);
                scoreboard.players.RemoveAll(p => p.steamID == sid);
            }
        }
        base.OnServerDisconnect(conn);
    }

    public override void OnValidate()
    {
        // if (testMode && allClients.Count > 0)
        // {
        //     ulong firstSteamID = allClients[0].playerInfo.steamId;
        //     AddPoints(firstSteamID, 10);
        //     Debug.Log($"Adicionados 10 pontos para o primeiro jogador: {firstSteamID}");
        //     testMode = false;
        // }
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

    public void iniciaContador(){
        ContadorTempo temp = GameObject.Find("Temporizador").GetComponent<ContadorTempo>();
        temp.IniciarContador();
    }
}
