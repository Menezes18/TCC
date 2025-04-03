using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




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
    public bool testMode = false;
    private void Awake()
    {
        // Encontra todos os MyNetworkManager na cena
        MyNetworkManager[] managers = FindObjectsOfType<MyNetworkManager>();

        // Se este não é o primeiro (mais antigo) MyNetworkManager
        if (managers.Length > 1)
        {
            // Ordena os managers por ordem de criação (TimeStamp)
            System.Array.Sort(managers, (a, b) => a.gameObject.GetInstanceID().CompareTo(b.gameObject.GetInstanceID()));

            // Se este não é o primeiro da lista (mais antigo)
            if (managers[0] != this)
            {
                // Destrói este objeto pois não é o primeiro
                Destroy(gameObject);
                return;
            }
        }

        // Se chegou aqui, este é o primeiro/único manager
        if (manager == null)
        {
            manager = this;
            DontDestroyOnLoad(gameObject);
        }
        
        base.Awake();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        MyClient client = conn.identity.GetComponent<MyClient>();
        
        CSteamID steamId = SteamLobby.LobbyID.m_SteamID == 0 
            ? SteamUser.GetSteamID() 
            : SteamMatchmaking.GetLobbyMemberByIndex(SteamLobby.LobbyID, allClients.Count);
        client.playerInfo = new PlayerInfoData(SteamFriends.GetFriendPersonaName(steamId), steamId.m_SteamID);
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
        base.OnServerDisconnect(conn);
    }

    public override void OnValidate()
    {
        if (testMode && allClients.Count > 0)
        {
            ulong firstSteamID = allClients[0].playerInfo.steamId;
            AddPoints(firstSteamID, 10);
            Debug.Log($"Adicionados 10 pontos para o primeiro jogador: {firstSteamID}");
            testMode = false;
        }
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
