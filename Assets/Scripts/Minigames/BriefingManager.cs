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

    
    [SerializeField] float briefingDuration = 5f;

    public UnityEvent onBriefingStarted;   // para ligar SFX, VFX, animações…
    public UnityEvent onBriefingEnded;     // para soltar texto na tela, iniciar countdown etc

    private Action _onCompleteCallback;

    [SerializeField] GameObject cameraBriefing;
    private BriefingScreenSO briefingData;

    private void Start()
    {
        canvasGroup.alpha = 0;
    }

    public void SetupBriefing()
    {
        cameraBriefing.SetActive(true);
        CmdAtivarPlayersNoServer(true);
        
    }
    
    public void ShowBriefing(BriefingScreenSO data)
    {
        briefingData = data;
        Debug.LogError("briefing foi ");
        CmdAtivarPlayersNoServer(true);
        imageUI.sprite = briefingData.image;
        titleText.text = briefingData.title;
        tipText.text = briefingData.tips[UnityEngine.Random.Range(0, briefingData.tips.Length)];

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
        SetupBriefing();
        onBriefingEnded?.Invoke();
        _onCompleteCallback?.Invoke();
        
    }
    
}