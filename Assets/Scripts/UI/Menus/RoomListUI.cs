using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class RoomListUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] public SteamLobby steamLobby;
    [SerializeField] public MyNetworkManager networkManager;
    [SerializeField] public RoomMenuController roomMenuController;
    [SerializeField] public RectTransform listContainer;
    [SerializeField] public GameObject itemPrefab;
    [SerializeField]
    public float refreshIntervalSeconds = 5f;

    private readonly List<GameObject> _spawned = new();
    private Lobby _selectedLobby;

    private void Awake()
    {
        if (steamLobby == null) steamLobby = SteamLobby.instance ?? FindObjectOfType<SteamLobby>();
        if (networkManager == null) networkManager = MyNetworkManager.manager ?? FindObjectOfType<MyNetworkManager>();
        if (roomMenuController == null) roomMenuController = FindObjectOfType<RoomMenuController>();
    }

    private void OnEnable()
    {
        if (steamLobby != null && SteamManager.Initialized)
        {
            steamLobby.LobbyListUpdated += HandleListUpdated;
            steamLobby.ReloadLobbyList();
            InvokeRepeating(nameof(RefreshLoop), refreshIntervalSeconds, refreshIntervalSeconds);
        }
        else if (!SteamManager.Initialized)
        {
            Debug.LogError("[RoomListUI] Steam não está inicializado.");
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshLoop));
        if (steamLobby != null)
            steamLobby.LobbyListUpdated -= HandleListUpdated;
    }

    private void RefreshLoop()
    {
        if (steamLobby == null || !SteamManager.Initialized)
            return;

        steamLobby.ReloadLobbyList();
    }

    private void HandleListUpdated(List<Lobby> lobbies)
    {
        if (listContainer == null || itemPrefab == null)
            return;

        foreach (var go in _spawned)
            Destroy(go);
        _spawned.Clear();

        foreach (var lobby in lobbies)
        {
            if (string.IsNullOrWhiteSpace(lobby.hostAddress))
                continue;

            var go = Instantiate(itemPrefab, listContainer);
            go.SetActive(true);
            _spawned.Add(go);


            var texts = go.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                var lower = t.name.ToLower();
                if (lower.Contains("name"))
                    t.text = string.IsNullOrWhiteSpace(lobby.name) ? "Sala" : lobby.name;
                else if (lower.Contains("code"))
                    t.text = string.IsNullOrWhiteSpace(lobby.roomCode) ? "—" : lobby.roomCode;
                else if (lower.Contains("players"))
                    t.text = $"{lobby.memberCount}/{lobby.maxMembers}";
            }

            var legacyTexts = go.GetComponentsInChildren<Text>(true);
            foreach (var t in legacyTexts)
            {
                var lower = t.name.ToLower();
                if (lower.Contains("name"))
                    t.text = string.IsNullOrWhiteSpace(lobby.name) ? "Sala" : lobby.name;
                else if (lower.Contains("code"))
                    t.text = string.IsNullOrWhiteSpace(lobby.roomCode) ? "—" : lobby.roomCode;
                else if (lower.Contains("players"))
                    t.text = $"{lobby.memberCount}/{lobby.maxMembers}";
            }

            // Botão de entrar
            var joinBtn = go.GetComponentInChildren<Button>();
            if (joinBtn != null)
            {
                SetupJoinButton(joinBtn, lobby);
            }
        }
    }
    public void StepupCutsceneJoinListRoom()
    {
        ManagerCutscene.Instance.setCutsceneID(CutsceneID.JoinListRoom);
        ManagerCutscene.Instance.callCutsceneJoinListRoomEvent();
    }


    public void JoinSelectedLobby()
    {
        if (_selectedLobby == null)
        {
            Debug.LogError("[RoomListUI] Nenhum lobby foi selecionado para entrar.");
            return;
        }

        networkManager?.SetMultiplayer(true);
        
        if (!string.IsNullOrWhiteSpace(_selectedLobby.roomCode))
        {
            if (roomMenuController != null && roomMenuController.roomCodeInput != null)
                roomMenuController.roomCodeInput.text = _selectedLobby.roomCode;
            roomMenuController?.OnClickJoinRoom();
        }
        else
        {
            steamLobby.JoinLobby(_selectedLobby.lobbyID);
        }
        
        // Limpa a referência após usar
        _selectedLobby = null;
    }

    private void SetupJoinButton(Button joinBtn, Lobby lobby)
    {
        joinBtn.onClick.RemoveAllListeners();
        joinBtn.onClick.AddListener(() =>
        {
            
            _selectedLobby = lobby;
            
            StepupCutsceneJoinListRoom();
        });
    }
}
