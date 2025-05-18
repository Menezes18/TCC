using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Mirror;

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
    
    private void Start()
    {
        
    }
    public override void OnStartClient()
    {
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
    
    private void ShowLocalBriefing()
    {
        imageUI.sprite = data.image;
        titleText.text = data.title;
        tipText.text = data.tips[tipIndex];
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;

        onBriefingStarted?.Invoke();
        StopAllCoroutines();
        StartCoroutine(CloseAfterDelay());
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
