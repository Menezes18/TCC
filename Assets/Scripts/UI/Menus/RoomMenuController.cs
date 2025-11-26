using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class RoomMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public MyNetworkManager networkManager;
    [SerializeField] public SteamLobby steamLobby;
    [SerializeField] public TMP_InputField roomCodeInput;
    [SerializeField] public TMP_Text createdRoomCodeText;
    [SerializeField] public TMP_Text statusText;
    [SerializeField] public Button createButton;
    [SerializeField] public Button joinButton;
    [Header("Popup (opcional)")]
    [SerializeField]
    private bool usePopupMessages = true;
    [SerializeField] private string popupCreateMessage = "Criando Sala...";
    [SerializeField] private string popupJoinMessage = "Procurando Sala...";

    [Header("Settings")]
    [SerializeField]
    private int roomCodeLength = 6;
    [SerializeField]
    private int maxPlayersOverride = 0;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = MyNetworkManager.manager ?? FindObjectOfType<MyNetworkManager>();

        if (steamLobby == null)
            steamLobby = SteamLobby.instance ?? FindObjectOfType<SteamLobby>();
    }

    private void OnEnable()
    {
        SubscribeToLobbyEvents(true);
    }

    private void OnDisable()
    {
        SubscribeToLobbyEvents(false);
    }

    public void OnClickCreateRoom()
    {
        ClearStatus();
        ToggleButtons(false);

        if (!EnsureDependencies())
        {
            ToggleButtons(true);
            return;
        }

        networkManager?.SetMultiplayer(true);

        int targetMaxPlayers = maxPlayersOverride > 0
            ? maxPlayersOverride
            : networkManager != null
                ? networkManager.maxConnections
                : NetworkManager.singleton != null
                    ? NetworkManager.singleton.maxConnections
            : 4;

        SetStatus("Criando sala...");
        ShowPopup(popupCreateMessage);
        steamLobby.CreateLobbyWithCode(roomCodeLength, targetMaxPlayers, showPopup: true);
    }

    public void OnClickJoinRoom()
    {
        ClearStatus();
        ToggleButtons(false);

        if (!EnsureDependencies())
        {
            ToggleButtons(true);
            return;
        }

        string sanitizedCode = SanitizeCode(roomCodeInput != null ? roomCodeInput.text : null);
        if (string.IsNullOrEmpty(sanitizedCode))
        {
            ShowError("Digite um código de sala válido.");
            ToggleButtons(true);
            return;
        }

        networkManager?.SetMultiplayer(true);

        SetStatus("Procurando sala...");
        ShowPopup(popupJoinMessage);
        steamLobby.JoinLobbyByCode(sanitizedCode);
    }

    public void OnClickRefreshList()
    {
        ClearStatus();

        steamLobby.ReloadLobbyList();

    }

    private void SubscribeToLobbyEvents(bool subscribe)
    {
        if (steamLobby == null)
            return;

        if (subscribe)
        {
            steamLobby.RoomCodeGenerated += HandleRoomCodeGenerated;
            steamLobby.RoomCreationFailed += HandleRoomCreationFailed;
            steamLobby.JoinByCodeFailed += HandleJoinByCodeFailed;
            steamLobby.JoinByCodeStarted += HandleJoinByCodeStarted;
        }
        else
        {
            steamLobby.RoomCodeGenerated -= HandleRoomCodeGenerated;
            steamLobby.RoomCreationFailed -= HandleRoomCreationFailed;
            steamLobby.JoinByCodeFailed -= HandleJoinByCodeFailed;
            steamLobby.JoinByCodeStarted -= HandleJoinByCodeStarted;
        }
    }

    private void HandleRoomCodeGenerated(string code)
    {
        if (createdRoomCodeText != null)
            createdRoomCodeText.text = code;

        SetStatus("Sala criada. Compartilhe o código.");

        steamLobby?.ReloadLobbyList();
        ClosePopup();
        ToggleButtons(true);
    }

    private void HandleRoomCreationFailed(string reason)
    {
        ShowError(string.IsNullOrWhiteSpace(reason) ? "Falha ao criar a sala." : reason);
        ClosePopup();
    }

    private void HandleJoinByCodeFailed(string reason)
    {
        ShowError(string.IsNullOrWhiteSpace(reason) ? "Sala não encontrada ou já fechada." : reason);
        ClosePopup();
    }

    private void HandleJoinByCodeStarted()
    {
        SetStatus("Procurando sala...");
        ShowPopup(popupJoinMessage);
    }

    private void ToggleButtons(bool enabled)
    {
        if (createButton != null)
            createButton.interactable = enabled;
        if (joinButton != null)
            joinButton.interactable = enabled;
    }

    private void ShowError(string message)
    {
        SetStatus(message);
        ToggleButtons(true);
        ClosePopup();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message ?? string.Empty;
    }

    private void ClearStatus()
    {
        SetStatus(string.Empty);
    }

    private bool EnsureDependencies()
    {
        if (steamLobby == null)
        {
            steamLobby = SteamLobby.instance ?? FindObjectOfType<SteamLobby>();
            if (steamLobby == null)
            {
                Debug.LogError("[RoomMenuController] Referência ao SteamLobby está ausente.");
                ShowError("Serviço de lobby indisponível.");
                return false;
            }

            SubscribeToLobbyEvents(true);
        }

        if (networkManager == null)
            networkManager = MyNetworkManager.manager ?? FindObjectOfType<MyNetworkManager>();


        return true;
    }

    private void ShowPopup(string text)
    {
        if (!usePopupMessages)
            return;
        if (PopupManager.instance != null)
            PopupManager.instance.Popup_Show(text, false, true);
    }

    private void ClosePopup()
    {
        if (!usePopupMessages)
            return;
        PopupManager.instance?.Popup_Close();
    }

    private string SanitizeCode(string raw)
    {
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim().ToUpperInvariant();
    }
}
