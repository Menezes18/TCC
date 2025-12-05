using Mirror;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class Lobby
{
    public CSteamID lobbyID;
    public string name;
    public string roomCode;
    public int memberCount;
    public int maxMembers;
    public string hostAddress;

    public Lobby(CSteamID lobbyID, string name, string roomCode, int memberCount, int maxMembers, string hostAddress)
    {
        this.lobbyID = lobbyID;
        this.name = name;
        this.roomCode = roomCode;
        this.memberCount = memberCount;
        this.maxMembers = maxMembers;
        this.hostAddress = hostAddress;
    }
}

public class SteamLobby : MonoBehaviour
{
    private const string HOST_ADDRESS_KEY = "HostAddress";
    private const string ROOM_CODE_KEY = "roomCode";
    private const int DEFAULT_CODE_LENGTH = 6;
                      
    public static SteamLobby instance;
    public static CSteamID LobbyID;

    public List<Lobby> allLobbies = new List<Lobby>();

    [Header("Room Code")]
    [SerializeField, Tooltip("Comprimento padrão ao gerar um código de sala.")]
    private int defaultRoomCodeLength = DEFAULT_CODE_LENGTH;
    [SerializeField, Tooltip("Se maior que 0, substitui maxConnections do NetworkManager.")]
    private int maxPlayersOverride = 0;

    public event Action<string> RoomCodeGenerated;       // Disparado no host quando uma sala com código é criada.
    public event Action<string> RoomCreationFailed;      // Disparado quando a criação do lobby falha.
    public event Action<string> JoinByCodeFailed;        // Disparado no cliente quando nenhum lobby corresponde ao código digitado.
    public event Action JoinByCodeStarted;               // Disparado quando a busca por código é iniciada.
    public event Action<List<Lobby>> LobbyListUpdated;   // Disparado quando a lista de lobbies é recarregada.
    public event Action<string> RoomCodeUpdated;

    // Callbacks da Steam API
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> joinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyMatchList;

    private string _pendingRoomCode;
    private string _pendingJoinCode;
    private bool _searchingByCode;
    private int _pendingMaxPlayers;
    public string CurrentRoomCode { get; private set; }


    private void Awake()
    {
        if (instance == null)
            instance = this;

            lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }

    private void Start()
    {
        if (!SteamManager.Initialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        ReloadLobbyList();
    }


    #region Steam Lobby
    public void ReloadLobbyList()
    {        
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[SteamLobby] Steam não está inicializado, não é possível carregar a lista de lobbies.");
            return;
        }

        allLobbies.Clear();
        _searchingByCode = false;
        _pendingJoinCode = null;

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.AddRequestLobbyListStringFilter("displayable", "true", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }
    void OnLobbyMatchList(LobbyMatchList_t param)
    {
        if (_searchingByCode)
        {
            HandleJoinByCodeResult(param);
            return;
        }

        allLobbies.Clear();
        for (int i = 0; i < param.m_nLobbiesMatching; i++)
        {                        
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            string name = SteamMatchmaking.GetLobbyData(lobbyID, "name");
            string code = SteamMatchmaking.GetLobbyData(lobbyID, ROOM_CODE_KEY);
            string maxPlayersData = SteamMatchmaking.GetLobbyData(lobbyID, "maxPlayers");
            string hostAddr = SteamMatchmaking.GetLobbyData(lobbyID, HOST_ADDRESS_KEY);
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
            int capacity = ResolveMaxPlayers();
            if (int.TryParse(maxPlayersData, out var parsedMax) && parsedMax > 0)
                capacity = parsedMax;

            // Ignora salas com 0 membros ou host offline
            if (memberCount <= 0) continue;
            if (string.IsNullOrWhiteSpace(hostAddr)) continue;

            allLobbies.Add(new Lobby(lobbyID, name, code, memberCount, capacity, hostAddr));
        }

        allLobbies.Sort((a, b) => SteamMatchmaking.GetNumLobbyMembers(b.lobbyID).CompareTo(SteamMatchmaking.GetNumLobbyMembers(a.lobbyID)));
        LobbyListUpdated?.Invoke(allLobbies);

        /*for (int i = 0; i < allLobbies.Count; i++)
        {

            if (SteamMatchmaking.GetLobbyData(allLobbies[i].lobbyID, "displayable") == "true")
            {
                var lobbyElement = Instantiate(MainMenu.instance.lobbyElementPrefab, MainMenu.instance.lobbyListContainer).GetComponent<LobbyElement>();
                lobbyElement.Initialize(allLobbies[i]);
                allLobbies[i].listElement = lobbyElement.gameObject;
            }
        }*/
    }

    private readonly float _delaySeconds = 2.0f;
    public void CreateLobby(bool showPopup = true)
    {
        _pendingRoomCode = null;
        int maxPlayers = ResolveMaxPlayers();
        BeginLobbyCreation(showPopup, useDelay: showPopup, maxPlayers);
    }

    public void CreateLobbyWithCode(int? codeLength = null, int? maxPlayers = null, bool showPopup = false)
    {
        int length = Mathf.Max(3, codeLength ?? defaultRoomCodeLength);
        _pendingRoomCode = GenerateRoomCode(length);
        _pendingMaxPlayers = maxPlayers.HasValue && maxPlayers.Value > 0
            ? maxPlayers.Value
            : ResolveMaxPlayers();

        BeginLobbyCreation(showPopup, useDelay: showPopup, _pendingMaxPlayers);
    }

    /// <summary>
    /// Sai do lobby atual (remove da listagem).
    /// </summary>
    public void CloseCurrentLobby()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[SteamLobby] Steam não está inicializado, não é possível sair do lobby.");
            return;
        }

        if (LobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(LobbyID);
            LobbyID = CSteamID.Nil;
            _pendingRoomCode = null;
            _pendingJoinCode = null;
            CurrentRoomCode = null;
            RoomCodeUpdated?.Invoke(CurrentRoomCode);
        }
    }

    private void BeginLobbyCreation(bool showPopup, bool useDelay, int maxPlayers)
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[SteamLobby] Steam não está inicializado, não é possível criar lobby.");
            RoomCreationFailed?.Invoke("Steam not initialized.");
            return;
        }

        _pendingMaxPlayers = maxPlayers;

        if (showPopup && PopupManager.instance != null)
            PopupManager.instance.Popup_Show("Criando Partida", false, true);

        IEnumerator Routine()
        {
            if (useDelay)
                yield return new WaitForSeconds(_delaySeconds);

            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _pendingMaxPlayers);

            if (MainMenu.instance != null)
                MainMenu.instance.gameObject.SetActive(false);
        }

        StartCoroutine(Routine());
    }

    private int ResolveMaxPlayers()
    {
        if (maxPlayersOverride > 0)
            return maxPlayersOverride;

        if (NetworkManager.singleton != null)
            return NetworkManager.singleton.maxConnections;

        return 4;
    }


    public void JoinLobby(CSteamID lobby)
    {
        SteamMatchmaking.JoinLobby(lobby);
    }

    public void JoinLobbyByCode(string code)
    {
        _pendingJoinCode = SanitizeCode(code);

        if (string.IsNullOrEmpty(_pendingJoinCode))
        {
            Debug.LogError("[SteamLobby] Código de sala inválido.");
            JoinByCodeFailed?.Invoke("Invalid room code.");
            return;
        }

        if (!SteamManager.Initialized)
        {
            Debug.LogError("[SteamLobby] Steam não está inicializado, não é possível consultar lobbies.");
            JoinByCodeFailed?.Invoke("Steam not initialized.");
            return;
        }

        _searchingByCode = true;
        JoinByCodeStarted?.Invoke();

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.AddRequestLobbyListStringFilter("displayable", "true", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(ROOM_CODE_KEY, _pendingJoinCode, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(10);
        SteamMatchmaking.RequestLobbyList();
    }


    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[SteamLobby] Criação de lobby falhou: {callback.m_eResult}");
            RoomCreationFailed?.Invoke("Failed to create lobby.");
            return;
        }

        string lobbyName = SteamFriends.GetFriendPersonaName(SteamUser.GetSteamID());

        LobbyID = new CSteamID(callback.m_ulSteamIDLobby);


        var manager = NetworkManager.singleton as MyNetworkManager;
        if (manager != null)
            manager.StartHost();
        else
            NetworkManager.singleton?.StartHost();

        SteamMatchmaking.SetLobbyData(LobbyID, HOST_ADDRESS_KEY, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(LobbyID, "name", lobbyName);
        SteamMatchmaking.SetLobbyData(LobbyID, "displayable", "true");
        SteamMatchmaking.SetLobbyData(LobbyID, "maxPlayers", _pendingMaxPlayers.ToString());
        if (!string.IsNullOrEmpty(_pendingRoomCode))
        {
            SteamMatchmaking.SetLobbyData(LobbyID, ROOM_CODE_KEY, _pendingRoomCode);
            CurrentRoomCode = _pendingRoomCode;
            RoomCodeGenerated?.Invoke(_pendingRoomCode);
            RoomCodeUpdated?.Invoke(CurrentRoomCode);

            Debug.Log($"<color=green> [SteamLobby] Lobby criado com código: {_pendingRoomCode}</color>");
        }
        _pendingRoomCode = null;

        SetLobbyLocation();
    }


    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        PopupManager.instance.Popup_Show("Entrando na Partida", false, true);
        StartCoroutine(DelayAction(_delaySeconds, () => {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }));
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // Se lobby estiver cheio, sai imediatamente
        if (callback.m_bLocked)
        {
            SteamMatchmaking.LeaveLobby((CSteamID)callback.m_ulSteamIDLobby);
            PopupManager.instance.Popup_Close();
            return;
        }

        Debug.Log($"Entrou no lobby {LobbyID}");
        LobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Evita iniciar cliente múltiplas vezes
        if (NetworkClient.active)
        {
            Debug.LogWarning("[SteamLobby] Client already started, ignorando nova conexão.");
            return;
        }

        if (NetworkServer.active)
            return;

        var hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(LobbyID.m_SteamID), HOST_ADDRESS_KEY);
        var lobbyCode = SteamMatchmaking.GetLobbyData(new CSteamID(LobbyID.m_SteamID), ROOM_CODE_KEY);

        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            Debug.LogError("[SteamLobby] Lobby sem host. Abortando conexão.");
            JoinByCodeFailed?.Invoke("Host offline.");
            SteamMatchmaking.LeaveLobby(LobbyID);
            return;
        }

        CurrentRoomCode = string.IsNullOrWhiteSpace(lobbyCode) ? null : lobbyCode;
        RoomCodeUpdated?.Invoke(CurrentRoomCode);

        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(true);
        ((MyNetworkManager)NetworkManager.singleton).networkAddress = hostAddress;
        ((MyNetworkManager)NetworkManager.singleton).StartClient();
    }
    #endregion

    /// <summary>
    /// Sai da partida atual de forma limpa, retornando ao menu offline.
    /// Não fecha o jogo, apenas desconecta e limpa o estado.
    /// </summary>
    public void Leave()
    {
        Debug.Log("[SteamLobby] Leave called - disconnecting from lobby");
        
        if (PopupManager.instance != null)
            PopupManager.instance.Popup_Show("Saindo da Partida", false, true);
        
        StartCoroutine(LeaveCoroutine());
    }
    
    private IEnumerator LeaveCoroutine()
    {
        yield return new WaitForSeconds(_delaySeconds);
        
        // Sai do lobby Steam
        if (LobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(LobbyID);
            LobbyID = CSteamID.Nil;
        }
        
        // Limpa os códigos de sala
        _pendingRoomCode = null;
        _pendingJoinCode = null;
        CurrentRoomCode = null;
        RoomCodeUpdated?.Invoke(CurrentRoomCode);
        
        // Para a conexão de rede
        if (NetworkServer.active && NetworkClient.active)
        {
            // Host
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            // Client
            NetworkManager.singleton.StopClient();
        }
        else if (NetworkServer.active)
        {
            // Dedicated server (improvável neste caso)
            NetworkManager.singleton.StopServer();
        }
        
        // Fecha popup
        if (PopupManager.instance != null)
            PopupManager.instance.Popup_Close();
        
        Debug.Log("[SteamLobby] Left lobby successfully");
    }
    
    /// <summary>
    /// Método antigo mantido para compatibilidade, mas agora apenas chama Leave().
    /// Use Leave() para sair sem fechar o jogo.
    /// </summary>
    public void LeaveAndQuit()
    {
        if (!NetworkClient.active) 
        {
            Application.Quit();
            return;
        }

        if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.isServer)
            NetworkManager.singleton.StopHost();
        else
            NetworkManager.singleton.StopClient();

        NetworkClient.Shutdown();
        NetworkManager.ResetStatics();
        Leave();
        Application.Quit();
    }


    public static void SetLobbyLocation()
    {
        SteamNetworkingUtils.GetLocalPingLocation(out SteamNetworkPingLocation_t pingLocation);
        SteamNetworkingUtils.ConvertPingLocationToString(ref pingLocation, out string result, 1024);
        SteamMatchmaking.SetLobbyData(LobbyID, "location", result);
    }

    public void FindMatch() 
    {
        StartCoroutine(FindMatchRoutine());
    }

    private void HandleJoinByCodeResult(LobbyMatchList_t param)
    {
        _searchingByCode = false;

        if (param.m_nLobbiesMatching <= 0)
        {
            Debug.LogError($"[SteamLobby] Nenhum lobby encontrado para o código '{_pendingJoinCode}'.");
            JoinByCodeFailed?.Invoke("Room not found or already closed.");
            _pendingJoinCode = null;
            return;
        }

        bool found = false;
        CSteamID targetLobby = default;

        for (int i = 0; i < param.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            string lobbyCode = SteamMatchmaking.GetLobbyData(lobbyID, ROOM_CODE_KEY);
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
            int capacity = ResolveMaxPlayers();
            string maxPlayersData = SteamMatchmaking.GetLobbyData(lobbyID, "maxPlayers");
            if (int.TryParse(maxPlayersData, out var parsedMax) && parsedMax > 0)
                capacity = parsedMax;

            if (string.Equals(lobbyCode, _pendingJoinCode, StringComparison.OrdinalIgnoreCase) &&
                memberCount < capacity)
            {
                targetLobby = lobbyID;
                found = true;
                break;
            }
        }

        _pendingJoinCode = null;

        if (!found)
        {
            Debug.LogError("[SteamLobby] Lobby encontrado, mas já está cheio ou sem metadata de código.");
            JoinByCodeFailed?.Invoke("Room not found or already closed.");
            return;
        }

        SteamMatchmaking.JoinLobby(targetLobby);
    }

    private static readonly char[] RoomCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    private string GenerateRoomCode(int length)
    {
        var buffer = new char[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = RoomCodeAlphabet[UnityEngine.Random.Range(0, RoomCodeAlphabet.Length)];
        }

        return new string(buffer);
    }

    private string SanitizeCode(string code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
    }

    IEnumerator FindMatchRoutine()
    {
        PopupManager.instance.Popup_Show("Procurando Partida...", false, true);
        bool foundMatch = false;
        float elapsedTime = 0f;
        float maxTime = 3f;

        while (!foundMatch && elapsedTime < maxTime)
        {
            ReloadLobbyList();
            yield return new WaitForSeconds(1f);
            elapsedTime += 1f;

            foreach (var lobby in allLobbies)
            {
                if (SteamMatchmaking.GetNumLobbyMembers(lobby.lobbyID) < NetworkManager.singleton.maxConnections)
                {
                    JoinLobby(lobby.lobbyID);
                    foundMatch = true;
                    MainMenu.instance.gameObject.SetActive(false);
                    break;
                }
            }
        }
        

        if (!foundMatch)
        {
            PopupManager.instance.Popup_Show("Nenhuma partida encontrada.", false, true);
        }
        StartCoroutine(DelayAction(1.7f, () => {
            PopupManager.instance.Popup_Close();
            
        }));
    }

    
    private IEnumerator DelayAction(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
