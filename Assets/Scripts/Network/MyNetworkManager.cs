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
    private MyClient partyOwner;
    private ContadorTempo temporizador;
    public override void Awake()
    {
        base.Awake();
        manager = this;
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        MyClient client = conn.identity.GetComponent<MyClient>();
        CSteamID steamId = SteamLobby.LobbyID.m_SteamID == 0 ? SteamUser.GetSteamID() : SteamMatchmaking.GetLobbyMemberByIndex(SteamLobby.LobbyID, allClients.Count);
        client.playerInfo = new PlayerInfoData(SteamFriends.GetFriendPersonaName(steamId), steamId.m_SteamID);

        // Define o primeiro jogador como owner
        if (allClients.Count == 0)
        {
            partyOwner = client;
            client.isPartyOwner = true;
        }
        
        allClients.Add(client);
        
        if (client.isLocalPlayer)
        {
            PartyMenuUIManager.Manager.SetLobbyPlayer(client);
        }

        Debug.Log("Conectados: " + allClients.Count);
        if(allClients.Count >= minJogadores) iniciaContador();
        CharacterSkinHandler.instance.DestroyMesh();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        MyClient disconnectedClient = conn.identity.GetComponent<MyClient>();
        
        // Se o owner desconectou, passa a ownership para o próximo jogador
        if (disconnectedClient == partyOwner && allClients.Count > 1)
        {
            // Remove o cliente desconectado da lista
            allClients.Remove(disconnectedClient);
            
            // Define o próximo cliente como owner
            partyOwner = allClients[0];
            partyOwner.isPartyOwner = true;
            
            if (partyOwner.isLocalPlayer)
            {
                PartyMenuUIManager.Manager.SetLobbyPlayer(partyOwner);
            }
        }
        else
        {
            allClients.Remove(disconnectedClient);
        }

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

    public void iniciaContador()
    {
        if (temporizador != null)
        {
            temporizador.IniciarContador();
        }
        else
        {
            // Tenta encontrar novamente caso tenha sido perdido
            temporizador = FindObjectOfType<ContadorTempo>();
            if (temporizador != null)
            {
                temporizador.IniciarContador();
            }
            else
            {
                Debug.LogError("Não foi possível encontrar o ContadorTempo na cena!");
            }
        }
    }
    public bool IsPartyOwner(MyClient client)
    {
        return client == partyOwner;
    }
}
