using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;

public class BriefingManager : NetworkBehaviour
{
    #region Singleton
    public static BriefingManager singleton;
    private void Awake()
    {
        if (singleton == null) singleton = this;
        else Destroy(gameObject);
    }
    #endregion

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image imageUI;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private GameObject cameraBriefing;
    [SerializeField] private float briefingDuration = 5f;
    [SerializeField] private BriefingScreenSO data;
    public UnityEvent onBriefingStarted;
    public UnityEvent onBriefingEnded;

    [SyncVar(hook = nameof(OnBriefingToggleChanged))]
    private bool briefingToggle;

    [SyncVar] private Sprite syncSprite;
    [SyncVar] private string syncTitle;
    [SyncVar] private string syncTip;
    [SyncVar] private int tipIndex;
    [SyncVar] private bool briefingStarted = false;
    public bool HasFinishedBriefing => briefingStarted;
    private bool _briefingRoundActive;
    private bool _briefingVisibleClient;

    // Client-side gate: allow pressing Ready only when server permits interaction
    [SerializeField]
    private bool readyInteractableClient = false;
    public bool ReadyInteractableClient => readyInteractableClient;
    public bool IsReadyInputBlocked => _briefingVisibleClient && !readyInteractableClient;

    [Header("Slots de Jogadores")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsParent;
    private readonly Dictionary<ulong, GameObject> slotsById = new();
    public readonly SyncListSlotData slots = new SyncListSlotData();

    // Server-side tracking: who already displayed the briefing
    private readonly HashSet<int> _briefingAcks = new HashSet<int>();
    [SyncVar] private int expectedBriefingAcks = 0;
    [SyncVar] private int receivedBriefingAcks = 0;

    public override void OnStartClient()
    {
        base.OnStartClient();
        slots.Callback += OnSlotsChanged;
        
        if (canvasGroup != null)
        {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
        }
        
        Debug.Log("[Briefing] OnStartClient - Canvas preparado, aguardando RpcShowBriefing");
    }

    private void Start()
    {
        // Não resetar prontos no Start do cliente; o servidor faz isso em TriggerBriefing
    }

    public void CheckAllReady()
    {
        if (!isServer) { Debug.Log("[Briefing] CheckAllReady called on client, ignoring"); return; }
        if (briefingStarted) { Debug.Log("[Briefing] Already started, ignoring"); return; }
        if (!_briefingRoundActive) return;
        if (expectedBriefingAcks == 0 || _briefingAcks.Count < expectedBriefingAcks)
            return;

        var (ready, total) = MyNetworkManager.manager.GetReadyCounts();
        bool allReady = ready == total && total > 0;
        Debug.Log($"[Briefing] Ready {ready}/{total} | allReady={allReady}");
        if (!allReady) return;
        _briefingRoundActive = false;
        // Reativa movimento antes de fechar briefing (descongela)
        PlayerList.singleton.SetAllPlayersFrozen(true);
        CmdFinishBriefing();
        RpcCloseBriefing();
        Debug.Log("[Briefing] RpcCloseBriefing dispatched");

        // Inicia a partida somente após todos estarem prontos
        // if (MatchManager.singleton != null)
        // {
        //     MatchManager.singleton.InternalStartMatch();
        // }
    }

    private void ShowLocalBriefing()
    {
        imageUI.sprite = data.image;
        titleText.text = data.title;
        tipText.text = data.tips[tipIndex];
        canvasGroup.alpha = 1;
        // Interação ficará bloqueada até todos clientes entrarem
        canvasGroup.interactable = false;
        readyInteractableClient = false;
        _briefingVisibleClient = true;
        onBriefingStarted?.Invoke();
        StopAllCoroutines();
    }

    private void ClearAllSlots()
    {
        foreach (Transform child in slotsParent)
            Destroy(child.gameObject);
    }

    private void RebuildAllSlots()
    {
        foreach (Transform c in slotsParent)
            Destroy(c.gameObject);

        foreach (var sd in slots)
        {
            var go = Instantiate(slotPrefab, slotsParent);
            go.GetComponent<SlotBriefing>().InitSlot(sd.steamId, sd.alias, sd.color, sd.isReady);
        }
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(briefingDuration);
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        readyInteractableClient = false;
        _briefingVisibleClient = false;
        cameraBriefing.SetActive(true);
        onBriefingEnded?.Invoke();
    }

    [Server]
    private void CmdFinishBriefing()
    {
        if (!briefingStarted)
            briefingStarted = true;
    }

    #region Server && Command
    [ServerCallback]
    public void CmdSetAllPlayersFrozensNoServer(bool ativar)
    {
        if (briefingStarted && PlayerList.singleton != null)
            PlayerList.singleton.SetAllPlayersFrozen(ativar);
    }

    // Existing scene UnityEvents still use this name.
    [ServerCallback]
    public void CmdAtivarPlayersNoServer(bool ativar) => CmdSetAllPlayersFrozensNoServer(ativar);

    [Server]
    public void OnPlayerDisconnected(NetworkConnectionToClient connection)
    {
        if (_briefingRoundActive)
        {
            _briefingAcks.Remove(connection.connectionId);
            expectedBriefingAcks = MyNetworkManager.manager.allClients.Count;
            receivedBriefingAcks = _briefingAcks.Count;
            if (!briefingStarted)
                RpcSetReadyInteractable(expectedBriefingAcks > 0 && receivedBriefingAcks >= expectedBriefingAcks);
        }
        UpdateAllClientsSlots();
        if (_briefingRoundActive)
            CheckAllReady();
    }

    [Server]
    public void UpdateAllClientsSlots()
    {
        var players = MyNetworkManager.manager.allClients;
        Debug.Log($"�[NETWORK] Clients conectados: {players.Count}");
        ulong[] steamIds = new ulong[players.Count];
        string[] aliases = new string[players.Count];
        int[] playerColor = new int[players.Count];
        bool[] readyStates = new bool[players.Count];

        for (int i = 0; i < players.Count; i++)
        {
            steamIds[i] = players[i].playerInfo.steamId;
            aliases[i] = players[i].alias;
            playerColor[i] = players[i].color;
            readyStates[i] = players[i].IsReady;
        }

        RpcRefreshAllSlots(steamIds, aliases, playerColor, readyStates);
    }

    [Server]
    public void TriggerBriefing()
    {
        briefingStarted = false;
        _briefingRoundActive = true;
        // Reset readiness e reconstrói UI antes de mostrar briefing
        MyNetworkManager.manager.ResetAllPlayersReady();
        UpdateAllClientsSlots();
        tipIndex = UnityEngine.Random.Range(0, data.tips.Length);
        briefingToggle = !briefingToggle;

        // Congela movimento enquanto briefing estiver ativo (congela)
        PlayerList.singleton.SetAllPlayersFrozen(true);

        // Reseta acks e define quantos clientes esperamos
        _briefingAcks.Clear();
        expectedBriefingAcks = MyNetworkManager.manager.allClients.Count;
        receivedBriefingAcks = 0;

        RpcShowBriefing(data.title, data.tips[tipIndex]);
        // Bloqueia interação até todos confirmarem que entraram
        RpcSetReadyInteractable(false);

        Debug.Log("[Briefing] TriggerBriefing -> reset ready, freeze players and RpcShowBriefing");
    }

    [ClientRpc]
    private void RpcShowBriefing(string syncedTitle, string syncedTip)
    {
        Debug.Log("[Briefing] RpcShowBriefing - Showing briefing UI and hiding loading screen");
        
        LoadingScreenUI.Instance?.Hide();

        if (canvasGroup != null)
        {
            canvasGroup.gameObject.SetActive(true);
        }
        
        titleText.text = syncedTitle;
        tipText.text = syncedTip;
        canvasGroup.alpha = 1;
        // Começa sem interação; será liberado quando todos entrarem
        canvasGroup.interactable = false;
        readyInteractableClient = false;
        _briefingVisibleClient = true;
        onBriefingStarted?.Invoke();
        StopAllCoroutines();

        // Usa o objeto de jogador pertencente ao cliente para que o servidor
        // consiga vincular o ACK à conexão autenticada correta.
        var localPlayerData = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<PlayerData>()
            : null;
        if (localPlayerData != null)
            localPlayerData.AcknowledgeBriefingShown();
        else
            StartCoroutine(AcknowledgeWhenLocalPlayerIsReady());
    }
    #endregion

    #region ClientRPC
    [ClientRpc]
    public void RpcHideLoadingUI()
    {
        LoadingScreenUI.Instance?.Hide();
    }

    [ClientRpc]
    private void RpcRefreshAllSlots(ulong[] steamIds, string[] aliases, int[] playerColor, bool[] readyStates)
    {
        ClearAllSlots();
        for (int i = 0; i < steamIds.Length; i++)
        {
            Debug.Log($"�Y�� [LOBBY] i={i} | steamId={steamIds[i]} | alias=\"{aliases[i]}\" | ready={readyStates[i]}");
            GameObject go = Instantiate(slotPrefab, slotsParent);
            SlotBriefing slot = go.GetComponent<SlotBriefing>();
            slot.InitSlot(steamIds[i], aliases[i], playerColor[i], readyStates[i]);
        }
    }

    [ClientRpc]
    private void RpcCloseBriefing()
    {
        Debug.Log("[Briefing] RpcCloseBriefing received");
        // Esconde UI para todos
        StopAllCoroutines();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        readyInteractableClient = false;
        _briefingVisibleClient = false;
        cameraBriefing.SetActive(true);
        onBriefingEnded?.Invoke();
    }

    [ClientRpc]
    private void RpcSetReadyInteractable(bool canInteract)
    {
        // Permite/nega interação na UI do briefing (ex: botão de pronto)
    canvasGroup.interactable = canInteract;
    readyInteractableClient = canInteract;
    }
    #endregion

    private IEnumerator AcknowledgeWhenLocalPlayerIsReady()
    {
        while (NetworkClient.active && NetworkClient.localPlayer == null)
            yield return null;

        NetworkClient.localPlayer?.GetComponent<PlayerData>()?.AcknowledgeBriefingShown();
    }

    [Server]
    public void ServerAcknowledgeBriefing(PlayerData player)
    {
        NetworkConnectionToClient sender = player != null ? player.connectionToClient : null;
        if (!_briefingRoundActive || sender == null || sender.identity != player.netIdentity || briefingStarted)
            return;

        if (_briefingAcks.Add(sender.connectionId))
        {
            receivedBriefingAcks = _briefingAcks.Count;
            Debug.Log($"[Briefing] Ack from connId={sender.connectionId} ({receivedBriefingAcks}/{expectedBriefingAcks})");
            if (receivedBriefingAcks >= expectedBriefingAcks && expectedBriefingAcks > 0)
            {
                // Todos entraram: liberar interação para que os jogadores possam ficar prontos
                RpcSetReadyInteractable(true);
            }
        }
    }

    // Novo: jogador tenta marcar pronto; bloqueia se nem todos entraram
    [Command(requiresAuthority = false)]
    public void CmdMarkClientReady(NetworkConnectionToClient sender = null)
    {
        if (!isServer) return;
        if (sender == null) return;

        // Impede ficar pronto enquanto todos não tiverem entrado no briefing
        if (_briefingRoundActive && (_briefingAcks.Count < expectedBriefingAcks || expectedBriefingAcks == 0))
        {
            Debug.Log("[Briefing] Ready ignored: nem todos os clientes entraram no briefing ainda");
            return;
        }

        var pd = sender.identity ? sender.identity.GetComponent<PlayerData>() : null;
        if (pd == null) return;

        pd.IsReady = true;
        UpdateAllClientsSlots();
        CheckAllReady();
    }

    [Server]
    public bool ServerTryToggleReady(PlayerData player)
    {
        if (player == null || briefingStarted)
            return false;
        if (_briefingRoundActive && (expectedBriefingAcks == 0 ||
            _briefingAcks.Count < expectedBriefingAcks))
            return false;
        player.IsReady = !player.IsReady;
        UpdateAllClientsSlots();
        CheckAllReady();
        return true;
    }

    private void OnSlotsChanged(SyncListSlotData.Operation op, int index, SlotData oldData, SlotData newData)
    {
        RebuildAllSlots();
    }

    private void OnBriefingToggleChanged(bool oldVal, bool newVal)
    {
        // Não mostrar a UI aqui; servidor chama RpcShowBriefing explicitamente
        Debug.Log("[Briefing] OnBriefingToggleChanged");
    }

    public class SyncListSlotData : SyncList<SlotData> { }
}

#region SlotData
[Serializable]
public struct SlotData : IEquatable<SlotData>
{
    public ulong steamId;
    public string alias;
    public bool isReady;
    public int color;

    public SlotData(ulong steamId, string alias, bool isReady, int color)
    {
        this.steamId = steamId;
        this.alias = alias;
        this.isReady = isReady;
        this.color = color;
    }

    public bool Equals(SlotData other)
    {
        return steamId == other.steamId && alias == other.alias && isReady == other.isReady;
    }
}
#endregion

