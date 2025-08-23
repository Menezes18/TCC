using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

    [Header("Slots de Jogadores")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsParent;
    private readonly Dictionary<ulong, GameObject> slotsById = new();
    public readonly SyncListSlotData slots = new SyncListSlotData();

    public override void OnStartClient()
    {
        base.OnStartClient();
        slots.Callback += OnSlotsChanged;
        PlayerList.singleton.AtivarPlayer(true);
        canvasGroup.alpha = 1;
    }

    private void Start()
    {
        MyNetworkManager.manager.ResetAllPlayersReady();
    }

    public void CheckAllReady()
    {
        if (!isServer) { Debug.Log("[Briefing] CheckAllReady called on client, ignoring"); return; }
        if (briefingStarted) { Debug.Log("[Briefing] Already started, ignoring"); return; }

        var (ready, total) = MyNetworkManager.manager.GetReadyCounts();
        bool allReady = ready == total && total > 0;
        Debug.Log($"[Briefing] Ready {ready}/{total} | allReady={allReady}");
        if (!allReady) return;
        CmdFinishBriefing();
        RpcCloseBriefing();
        Debug.Log("[Briefing] RpcCloseBriefing dispatched");
    }

    private void ShowLocalBriefing()
    {
        imageUI.sprite = data.image;
        titleText.text = data.title;
        tipText.text = data.tips[tipIndex];
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
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
        cameraBriefing.SetActive(true);
        onBriefingEnded?.Invoke();
    }

    [Command(requiresAuthority = false)]
    private void CmdFinishBriefing()
    {
        if (!briefingStarted)
            briefingStarted = true;
    }

    #region Server && Command
    [Command(requiresAuthority = false)]
    public void CmdAtivarPlayersNoServer(bool ativar)
    {
        PlayerList.singleton.AtivarPlayer(ativar);
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
        // Reset readiness and rebuild UI before showing briefing
        MyNetworkManager.manager.ResetAllPlayersReady();
        UpdateAllClientsSlots();
        tipIndex = UnityEngine.Random.Range(0, data.tips.Length);
        briefingToggle = !briefingToggle;
        RpcShowBriefing(data.title, data.tips[tipIndex]);
        Debug.Log("[Briefing] TriggerBriefing -> reset ready and RpcShowBriefing");
        
        // Mark host as ready immediately (host receives Rpc too, but this guarantees progress)
        var hostConn = NetworkServer.localConnection;
        if (hostConn != null)
        {
            var hostPd = hostConn.identity ? hostConn.identity.GetComponent<PlayerData>() : null;
            if (hostPd != null) hostPd.IsReady = true;
        }
        // After a short delay, re-check – gives time for clients to send their ready command
        StartCoroutine(ServerDelayedRecheck());
    }

    [Server]
    private IEnumerator ServerDelayedRecheck()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateAllClientsSlots();
        CheckAllReady();
    }

    [ClientRpc]
    private void RpcShowBriefing(string syncedTitle, string syncedTip)
    {
        Debug.Log("[Briefing] RpcShowBriefing");
        LoadingScreenUI.Instance?.Hide();
        // Ensure we notify server this client is present/ready for briefing
        MyNetworkManager.manager?.RecordClientBriefingShown();

        titleText.text = syncedTitle;
        tipText.text = syncedTip;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        onBriefingStarted?.Invoke();
        StopAllCoroutines();
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
        // Immediately hide UI for everyone
        StopAllCoroutines();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        cameraBriefing.SetActive(true);
        onBriefingEnded?.Invoke();
    }
    #endregion

    // New: client notifies server that briefing is visible and this player is ready
    [Command(requiresAuthority = false)]
    public void CmdMarkClientReady(NetworkConnectionToClient sender = null)
    {
        if (!isServer) return;
        if (sender == null) return;
        var pd = sender.identity ? sender.identity.GetComponent<PlayerData>() : null;
        if (pd == null) return;

        pd.IsReady = true;
        UpdateAllClientsSlots();
        CheckAllReady();
    }

    private void OnSlotsChanged(SyncListSlotData.Operation op, int index, SlotData oldData, SlotData newData)
    {
        RebuildAllSlots();
    }

    private void OnBriefingToggleChanged(bool oldVal, bool newVal)
    {
        Debug.Log("[Briefing] OnBriefingToggleChanged -> ShowLocalBriefing");
        CmdAtivarPlayersNoServer(true);
        ShowLocalBriefing();
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

