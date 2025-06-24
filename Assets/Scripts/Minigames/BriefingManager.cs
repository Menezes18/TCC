using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Mirror;
using System.Collections.Generic;

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
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image imageUI;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI tipText;
    [SerializeField] GameObject cameraBriefing;
    [SerializeField] float briefingDuration = 5f;
    [SerializeField] private BriefingScreenSO data;
    public UnityEvent onBriefingStarted;
    public UnityEvent onBriefingEnded;
    
    [SyncVar(hook = nameof(OnBriefingToggleChanged))]
    private bool briefingToggle;
    
    [SyncVar] Sprite syncSprite;
    [SyncVar] string syncTitle;
    [SyncVar] string syncTip;
    [SyncVar] int tipIndex;
    
    
    [Header("Slots de Jogadores")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsParent;
    private Dictionary<ulong, GameObject> slotsById = new();
    public readonly SyncListSlotData slots = new SyncListSlotData();
    public override void OnStartClient()
    {
        base.OnStartClient();
        slots.Callback += OnSlotsChanged;
        PlayerList.singleton.AtivarPlayer(true);
        canvasGroup.alpha = 1;
    }
    
    public void CheckAllReady()
    {
        if (!isServer) return;

        bool allReady = AllPlayersReady();

        if (!allReady) return;
        RpcCloseBriefing();
        
    }
    private bool AllPlayersReady() 
    {
        foreach (PlayerData client in ((MyNetworkManager)NetworkManager.singleton).allClients)
            if (!client.IsReady)
                return false;
        return true;
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
        {
            Destroy(child.gameObject);
        }
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
        //yield return new WaitForSeconds(10f);
        yield return new WaitForSeconds(briefingDuration);

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        cameraBriefing.SetActive(true);
        onBriefingEnded?.Invoke();
    }

    #region Server && Comand 
    
    [Command(requiresAuthority = false)]
    public void CmdAtivarPlayersNoServer(bool ativar)
    {
        
        PlayerList.singleton.AtivarPlayer(ativar);
    }
    [Server]
    public void UpdateAllClientsSlots()
    {
        var players = MyNetworkManager.manager.allClients;
        Debug.LogError(MyNetworkManager.manager.allClients.Count);
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
        tipIndex = UnityEngine.Random.Range(0, data.tips.Length);
        briefingToggle = !briefingToggle;
    }
    #endregion

    #region ClientRPC
    
    [ClientRpc]
    private void RpcRefreshAllSlots(ulong[] steamIds, string[] aliases, int[] playerColor, bool[] readyStates)
    {
        ClearAllSlots(); 

        for (int i = 0; i < steamIds.Length; i++)
        {
            Debug.LogError(steamIds[i] + " " + aliases[i] + " " + readyStates[i]);
            GameObject go = Instantiate(slotPrefab, slotsParent);
            SlotBriefing slot = go.GetComponent<SlotBriefing>();
            slot.InitSlot(steamIds[i], aliases[i], playerColor[i], readyStates[i]);
        }
    }
    [ClientRpc]
    void RpcCloseBriefing()
    {
        StopAllCoroutines();
        StartCoroutine(CloseAfterDelay());
    }
    #endregion
    private void OnSlotsChanged(SyncListSlotData.Operation op, int index, SlotData oldData, SlotData newData)
    {
        RebuildAllSlots();
    }
    private void OnBriefingToggleChanged(bool oldVal, bool newVal)
    {
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
            return steamId == other.steamId
                   && alias == other.alias
                   && isReady == other.isReady;
        }
    }
    #endregion
