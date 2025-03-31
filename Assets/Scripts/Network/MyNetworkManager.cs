using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyNetworkManager : NetworkManager 
{
    public static bool isMulitplayer;
    public static MyNetworkManager manager { get; internal set; }

    public List<MyClient> allClients = new List<MyClient>();
    public int minJogadores = 1;

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
        CSteamID steamId = SteamLobby.LobbyID.m_SteamID == 0 ? SteamUser.GetSteamID() : SteamMatchmaking.GetLobbyMemberByIndex(SteamLobby.LobbyID, allClients.Count);
        client.playerInfo = new PlayerInfoData(SteamFriends.GetFriendPersonaName(steamId), steamId.m_SteamID);
        Debug.Log("Conectados" + allClients.Count);
        if(allClients.Count >= minJogadores) iniciaContador();
        CharacterSkinHandler.instance.DestroyMesh();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
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
