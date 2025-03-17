using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Services.Authentication;
using Unity.Services.Core;

using Utp;

namespace Network 
{
    public class MyNetworkManager : RelayNetworkManager
    {
        /// <summary>
        /// The local player object that spawns in.
        /// </summary>
        public PlayerScript localPlayer;
        private string m_SessionId = "";
        private string m_Username;
        private string m_UserId;

        /// <summary>
        /// Flag to determine if the user is logged into the backend.
        /// </summary>
        public bool isLoggedIn = false;

        /// <summary>
        /// List of players currently connected to the server.
        /// </summary>
        private List<PlayerScript> m_Players;

        public override void Awake()
        {
            base.Awake();
            m_Players = new List<PlayerScript>();

            m_Username = SystemInfo.deviceName;
        }

        public async void UnityLogin()
		{
			try
			{
				await UnityServices.InitializeAsync();
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Logged into Unity, player ID: " + AuthenticationService.Instance.PlayerId);
                isLoggedIn = true;
            }
			catch (Exception e)
			{
                isLoggedIn = false;
                Debug.Log(e);
			}
		}

        private void Update()
        {
            if (NetworkManager.singleton.isNetworkActive)
            {
                if (localPlayer == null)
                {
                    FindLocalPlayer();
                }
            }
            else
            {
                localPlayer = null;
                m_Players.Clear();
            }
        }


        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<ChatMessage>(OnChatMessageReceived);
            Debug.Log("MyNetworkManager: Server Started!");
            //
            // m_SessionId = System.Guid.NewGuid().ToString();
            //
            // if (NetworkManager.singleton.transport is UtpTransport utpTransport)
            // {
            //     // Garante que o handler seja registrado apenas uma vez
            //     utpTransport.OnServerDataReceived -= HandleServerDataReceived;
            //     utpTransport.OnServerDataReceived += HandleServerDataReceived;
            // }
        }
        void OnChatMessageReceived(NetworkConnectionToClient conn, ChatMessage message)
        {
            Debug.Log($"Received message from {conn.connectionId}: {message.content}");
        }
        // No método HandleServerDataReceived em MyNetworkManager.cs
        private void HandleServerDataReceived(int connId, ArraySegment<byte> data, int channel)
        {
            if (data.Count == 0)
            {
                Debug.LogWarning($"[SERVER] Pacote vazio recebido de {connId}, ignorando.");
                return;
            }

            // Em vez de tentar converter para string, apenas registre o recebimento de dados binários
            Debug.Log($"[SERVER] Pacote recebido de {connId}: {data.Count} bytes");
    
            // Deixe o Mirror lidar com o processamento da mensagem
            // Os dados serão devidamente desserializados pelos manipuladores de mensagens do Mirror
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);

            foreach (KeyValuePair<uint, NetworkIdentity> kvp in NetworkServer.spawned)
            {
                PlayerScript comp = kvp.Value.GetComponent<PlayerScript>();

                // Add to player list if new
                if (comp != null && !m_Players.Contains(comp))
                {
                    comp.sessionId = m_SessionId;
                    m_Players.Add(comp);
                }
            }

            if (m_Players.Count >= 2) {
                ContadorTempo temp = GameObject.Find("Temporizador").GetComponent<ContadorTempo>();
                temp.IniciarContador();
            }
        }

        public override void OnStopServer()
        {
            Debug.Log("MyNetworkManager: Server Stopped!");
            m_SessionId = "";
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            Dictionary<uint, NetworkIdentity> spawnedPlayers = NetworkServer.spawned;
            
            // Update players list on client disconnect
            foreach (PlayerScript player in m_Players)
            {
                bool playerFound = false;

                foreach (KeyValuePair<uint, NetworkIdentity> kvp in spawnedPlayers)
                {
                    PlayerScript comp = kvp.Value.GetComponent<PlayerScript>();

                    // Verify the player is still in the match
                    if (comp != null && player == comp)
                    {
                        playerFound = true;
                        break;
                    }
                }

                if (!playerFound)
                {
                    m_Players.Remove(player);
                    break;
                }
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            Debug.Log("MyNetworkManager: Left the Server!");

            localPlayer = null;

            m_SessionId = "";
        }

        public void OnClientConnect(NetworkConnection conn)
        {
            Debug.Log($"MyNetworkManager: {m_Username} Connected to Server!");
        }

        public void OnClientDisconnect(NetworkConnection conn)
        {

            Debug.Log("MyNetworkManager: Disconnected from Server!");
        }

        /// <summary>
        /// Finds the local player if they are spawned in the scene.
        /// </summary>
        void FindLocalPlayer()
        {
            //Check to see if the player is loaded in yet
            if (NetworkClient.localPlayer == null)
                return;

            localPlayer = NetworkClient.localPlayer.GetComponent<PlayerScript>();
        }
    }
}

