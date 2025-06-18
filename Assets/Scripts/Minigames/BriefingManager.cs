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



    public void Init()
    {
        Debug.Log($"[BriefingManager] Init - Total de players: {PlayerList.singleton.players.Count}");
        MyNetworkManager.manager.ResetAllPlayersReady();
        foreach (var pd in PlayerList.singleton.players)
        {
            Debug.Log($"[BriefingManager] Checando player: {pd.alias} | SteamID: {pd.playerInfo.steamId}");

            if (pd.playerInfo.steamId == 0) continue;
            if (slotsById.ContainsKey(pd.playerInfo.steamId)) continue;

            GameObject go = Instantiate(slotPrefab, slotsParent);
            slotsById[pd.playerInfo.steamId] = go;

            AtualizarSlot(pd.playerInfo.steamId, pd.alias, pd.IsReady);
        }
    }
    public void AtualizarSlot(ulong steamId, string alias, bool isReady)
    {
        if (!slotsById.TryGetValue(steamId, out var slot)) return;

        TextMeshProUGUI nameText = slot.transform.Find("NameText")
            .GetComponent<TextMeshProUGUI>();
        GameObject readyIndicator = slot.transform.Find("ReadyIcon").gameObject;

        nameText.text = alias;
        nameText.color = isReady ? Color.green : Color.red;

        readyIndicator.SetActive(isReady);
    }
    public override void OnStartClient()
    {
        
        Invoke("Init", 1);
        base.OnStartClient();
        PlayerList.singleton.AtivarPlayer(true);
        canvasGroup.alpha = 1;
    }

    [Server]
    public void TriggerBriefing()
    {
        tipIndex = UnityEngine.Random.Range(0, data.tips.Length);
        briefingToggle = !briefingToggle;
    }
    
    private void OnBriefingToggleChanged(bool oldVal, bool newVal)
    {
        CmdAtivarPlayersNoServer(true);
        ShowLocalBriefing();
    }
    public void CheckAllReady()
    {
        if (!isServer) return;

        bool allReady = AllPlayersReady();

        if (!allReady) return;
        StartCoroutine(CloseAfterDelay());
        
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
    
    [Command(requiresAuthority = false)]
    public void CmdAtivarPlayersNoServer(bool ativar)
    {
        
        PlayerList.singleton.AtivarPlayer(ativar);
    }
    private System.Collections.IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(briefingDuration);

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        cameraBriefing.SetActive(true);
        onBriefingEnded?.Invoke();
    }
}
