using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class HUDManager : MonoBehaviour
{
    [SerializeField] HUDSO HUDSO;

    [SerializeField] TMP_Text _matchTimer, _freezeTimer, _respawnTimer, _gameover;
    [SerializeField] TMP_Text _potatoHolder;

    [Header("Player HUD Elements")]
    [SerializeField] GameObject playerHUDContainer;
    private string playerHUDContainerName = "HUD:UIPlayer";
    
    private CanvasGroup _playerHUDCanvasGroup;
    private int _panelsOpenCount = 0;
    
    [Header("Countdown FX")]
    [SerializeField] float spinDuration = 0.45f;
    [SerializeField] Ease spinEase = Ease.OutBack;
    [SerializeField] float popOvershoot = 1.15f; // escala > 1 e volta
    [SerializeField] float popBackDuration = 0.12f;

    [Header("Match Timer FX")]
    [SerializeField] float timerPulseScale = 1.15f;
    [SerializeField] float timerPulseDuration = 0.5f;
    [SerializeField] Color timerNormalColor = Color.white;
    [SerializeField] Color timerWarningColor = new Color(1f, 0.84f, 0.30f); 
    [SerializeField] Color timerCriticalColor = new Color(1f, 0.36f, 0.36f); 
    [SerializeField] int warningThreshold = 30; 
    [SerializeField] int criticalThreshold = 10; 

    int _lastFreezeShown = int.MinValue;
    int _lastMatchTimerSecond = -1;
    Sequence _freezeSeq;
    Sequence _matchTimerSeq;
    Dictionary<int, Color> _numColors;
    TMP_Text tmp;
    void Start()
    {
        // Timer events
        HUDSO.EventOnMatchTimerUpdated += HUDSOOnEventOnMatchTimerUpdated;
        HUDSO.EventOnPrepareTimerUpdated += HUDSOOnEventOnPrepareTimerUpdated;
        HUDSO.EventOnFreezeTimerUpdated += HUDSOOnEventOnFreezeTimerUpdated;
        HUDSO.EventOnRespawnTimerUpdated += HUDSOOnEventOnRespawnTimerUpdated;
        HUDSO.EventOnGameOver += HUDSOOnEventOnGameOver;
        HUDSO.EventOnPotatoHolderUpdated += OnPotatoHolderUpdated;
        
        // Spectator mode
        HUDSO.EventOnSpectatorModeChanged += OnSpectatorModeChanged;
        
        // Panel visibility events
        HUDSO.EventOnShowColorChangePanel += OnPanelOpened;
        HUDSO.EventOnHideColorChangePanel += OnPanelClosed;
        HUDSO.EventOnShowMinigameSelectionPanel += OnPanelOpened;
        HUDSO.EventOnHideMinigameSelectionPanel += OnPanelClosed;
        HUDSO.EventOnShowBriefing += OnPanelOpened;
        HUDSO.EventOnHideBriefing += OnPanelClosed;
        HUDSO.EventOnShowVotingPanel += OnPanelOpened;
        HUDSO.EventOnHideVotingPanel += OnPanelClosed;
        HUDSO.EventOnShowMenuPanel += OnPanelOpened;
        HUDSO.EventOnHideMenuPanel += OnPanelClosed;
        
        _matchTimer.text = _freezeTimer.text = _respawnTimer.text = _gameover.text = "";

        _numColors = new Dictionary<int, Color> {
            {5, new Color(1f, 0.36f, 0.67f)}, // rosa
            {4, new Color(0.50f, 0.90f, 0.82f)}, // menta
            {3, new Color(1.00f, 0.84f, 0.30f)}, // amarelo
            {2, new Color(0.50f, 0.83f, 1.00f)}, // azul
            {1, new Color(0.55f, 0.35f, 1.00f)}, // violeta
        };
        
        // Inicializa PlayerHUD container
        InitializePlayerHUDContainer();
    }

    void OnDestroy()
    {
        // Timer events
        HUDSO.EventOnMatchTimerUpdated -= HUDSOOnEventOnMatchTimerUpdated;
        HUDSO.EventOnPrepareTimerUpdated -= HUDSOOnEventOnPrepareTimerUpdated;
        HUDSO.EventOnFreezeTimerUpdated -= HUDSOOnEventOnFreezeTimerUpdated;
        HUDSO.EventOnRespawnTimerUpdated -= HUDSOOnEventOnRespawnTimerUpdated;
        HUDSO.EventOnGameOver -= HUDSOOnEventOnGameOver;
        
        // Spectator mode
        HUDSO.EventOnSpectatorModeChanged -= OnSpectatorModeChanged;
        
        // Panel visibility events
        HUDSO.EventOnShowColorChangePanel -= OnPanelOpened;
        HUDSO.EventOnHideColorChangePanel -= OnPanelClosed;
        HUDSO.EventOnShowMinigameSelectionPanel -= OnPanelOpened;
        HUDSO.EventOnHideMinigameSelectionPanel -= OnPanelClosed;
        HUDSO.EventOnShowBriefing -= OnPanelOpened;
        HUDSO.EventOnHideBriefing -= OnPanelClosed;
        HUDSO.EventOnShowVotingPanel -= OnPanelOpened;
        HUDSO.EventOnHideVotingPanel -= OnPanelClosed;
        HUDSO.EventOnShowMenuPanel -= OnPanelOpened;
        HUDSO.EventOnHideMenuPanel -= OnPanelClosed;
    }

    void HUDSOOnEventOnRespawnTimerUpdated(float obj)
    {
        int s = Mathf.RoundToInt(obj);
        if (s == 0) { _respawnTimer.text = ""; return; }
        _respawnTimer.text = "Renascendo em " + s + " segundos";
    }

    void HUDSOOnEventOnPrepareTimerUpdated(float obj)
    {
        HandleAnimation(obj);



    }

    void HUDSOOnEventOnFreezeTimerUpdated(float obj)
    {
        HandleAnimation(obj);
    }

    void HandleAnimation(float obj)
    {
        int s = Mathf.RoundToInt(obj);
        if (s == 0)
        {
            _freezeTimer.text = "VAI!"; 
            return;
        }
        if (s == -1) { _freezeTimer.text = ""; _lastFreezeShown = int.MinValue; return; }

        _freezeTimer.text = s.ToString();

        if (s != _lastFreezeShown && s >= 1 && s <= 5)
        {
            _lastFreezeShown = s;
            AnimateCountdownTMP(_freezeTimer, s);
        }
    }
    private void OnPotatoHolderUpdated(string name)
    {
        if (_potatoHolder)
            _potatoHolder.text = name;
    }
    void HUDSOOnEventOnMatchTimerUpdated(float obj)
    {
        if (Mathf.RoundToInt(obj) == -1) 
        { 
            _matchTimer.text = "";
            _matchTimerSeq?.Kill();
            return; 
        }
        
        int seconds = Mathf.RoundToInt(obj);
        _matchTimer.text = CustomMath.FormatTimer(obj);

        if (seconds != _lastMatchTimerSecond)
        {
            _lastMatchTimerSecond = seconds;
            AnimateMatchTimer(seconds);
        }
    }

    void HUDSOOnEventOnGameOver(string obj) => _gameover.text = obj;

    void AnimateCountdownTMP(TMP_Text label, int number)
    {
        RectTransform rt = label.rectTransform;

        tmp = label.GetComponent<TMP_Text>();
        
        if (_numColors.TryGetValue(number, out var face))
        {
           
            SetOutline(face);
        }

        _freezeSeq?.Kill();
        rt.DOKill();

        rt.localScale = Vector3.zero;
        rt.localRotation = Quaternion.identity;

        _freezeSeq = DOTween.Sequence()
            .Append(rt.DOScale(popOvershoot, spinDuration * 0.8f).SetEase(spinEase))
            .Join(rt.DORotate(new Vector3(0, 0, -360f), spinDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutCubic))
            .Append(rt.DOScale(1f, popBackDuration).SetEase(Ease.OutQuad));
    }

    void AnimateMatchTimer(int seconds)
    {
        RectTransform rt = _matchTimer.rectTransform;
        
        // Determina a cor baseado no tempo restante
        Color targetColor = timerNormalColor;
        bool shouldPulse = false;

        if (seconds <= criticalThreshold)
        {
            targetColor = timerCriticalColor;
            shouldPulse = true;
        }
        else if (seconds <= warningThreshold)
        {
            targetColor = timerWarningColor;
        }

        // Aplica a cor
        _matchTimer.color = targetColor;

        // Cancela animação anterior
        _matchTimerSeq?.Kill();
        rt.DOKill();

        // Animação de pulso suave a cada segundo
        _matchTimerSeq = DOTween.Sequence()
            .Append(rt.DOScale(1.1f, 0.15f).SetEase(Ease.OutQuad))
            .Append(rt.DOScale(1f, 0.15f).SetEase(Ease.InQuad));

        // Se estiver em tempo crítico, adiciona pulso contínuo
        if (shouldPulse)
        {
            _matchTimerSeq.Append(rt.DOScale(timerPulseScale, timerPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));
        }
    }
    
    public void SetOutline(Color color)
    {
        var mat = tmp.fontMaterial;

        mat.EnableKeyword("OUTLINE_ON");

        mat.SetColor(ShaderUtilities.ID_OutlineColor, color);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 1);

        tmp.UpdateMeshPadding();
        tmp.havePropertiesChanged = true;
        tmp.SetMaterialDirty();
    }

    #region Player HUD Visibility Management
    
    private void InitializePlayerHUDContainer()
    {
        // Se não configurado, busca por nome
        if (playerHUDContainer == null)
        {
            playerHUDContainer = GameObject.Find(playerHUDContainerName);
            
            if (playerHUDContainer == null)
            {
                Debug.LogWarning($"[HUDManager] GameObject '{playerHUDContainerName}' não encontrado. PlayerHUD não será escondido automaticamente.");
                return;
            }
        }
        
        // Obtém ou adiciona CanvasGroup
        _playerHUDCanvasGroup = playerHUDContainer.GetComponent<CanvasGroup>();
        if (_playerHUDCanvasGroup == null)
        {
            _playerHUDCanvasGroup = playerHUDContainer.AddComponent<CanvasGroup>();
            Debug.Log($"[HUDManager] CanvasGroup adicionado automaticamente ao '{playerHUDContainer.name}'");
        }
        
        // Garante que começa visível
        _playerHUDCanvasGroup.alpha = 1f;
    }
    
    private void OnPanelOpened()
    {
        _panelsOpenCount++;
        UpdatePlayerHUDVisibility();
    }
    
    private void OnPanelClosed()
    {
        _panelsOpenCount--;
        if (_panelsOpenCount < 0) _panelsOpenCount = 0; // Segurança
        UpdatePlayerHUDVisibility();
    }
    
    private void UpdatePlayerHUDVisibility()
    {
        if (_playerHUDCanvasGroup == null) return;
        
        // Esconde se algum painel estiver aberto, mostra se todos fechados
        float targetAlpha = _panelsOpenCount > 0 ? 0f : 1f;
        _playerHUDCanvasGroup.alpha = targetAlpha;
        
        if (targetAlpha == 0f)
            Debug.Log($"[HUDManager] PlayerHUD escondido ({_panelsOpenCount} painéis abertos)");
        else
            Debug.Log("[HUDManager] PlayerHUD mostrado (nenhum painel aberto)");
    }
    
    private void OnSpectatorModeChanged(bool isSpectating)
    {
        if (_playerHUDCanvasGroup == null) return;
        
        if (isSpectating)
        {
            // Esconde o PlayerHUD quando entrar em modo espectador
            _playerHUDCanvasGroup.alpha = 0f;
            Debug.Log("👁️ [HUDManager] PlayerHUD escondido (modo espectador ativado)");
        }
        else
        {
            // Mostra o PlayerHUD quando sair do modo espectador
            _playerHUDCanvasGroup.alpha = 1f;
            Debug.Log("✅ [HUDManager] PlayerHUD mostrado (modo espectador desativado)");
        }
    }
    
    #endregion

}