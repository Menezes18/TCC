using System.Collections.Generic;
using Mirror;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIVoteInputProvider : NetworkBehaviour, IVoteInputProvider
{
    [Header("UI References")]
    [SerializeField] private GameObject votingPanel;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private VoteCard cardPrefab;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private RectTransform timerContainer;
    [SerializeField] private TMP_Text timerText;

    [Header("Settings")]
    [SerializeField] private bool autoHideOnVotingEnd = true;
    [SerializeField] private bool showTimer = true;

    [Header("Timer Animation")]
    [SerializeField] private bool enableShakeAnimation = true;
    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float shakeSpeed = 0.15f;
    [SerializeField] private float urgentThreshold = 5f;
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color urgentColor = Color.red;
    [SerializeField] private float popScaleAmount = 1.15f;
    [SerializeField] private float popDuration = 0.2f;

    private readonly List<VoteCard> _spawnedCards = new List<VoteCard>();
    private int _currentVote = -1;
    private bool _isActive;
    private VotingManager _registeredVotingManager;
    private float _votingDuration;
    private int _lastSecond = -1;
    private bool _isUrgent;
    private Vector3 _originalTimerPosition;
    private int _shakeTweenId = -1;

    public bool IsActive => _isActive;

    private void Awake()
    {
        if (votingPanel != null)
        {
            votingPanel.SetActive(false);
        }
        
        if (timerFillImage != null)
        {
            timerFillImage.gameObject.SetActive(false);
            timerFillImage.type = Image.Type.Filled;
            timerFillImage.fillMethod = Image.FillMethod.Radial360;
            timerFillImage.fillAmount = 1f;
        }
        
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
        
        if (timerContainer != null)
        {
            _originalTimerPosition = timerContainer.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        VotingManager.OnInstanceChanged += HandleVotingManagerChanged;
        HandleVotingManagerChanged(VotingManager.Instance);
    }

    private void OnDisable()
    {
        VotingManager.OnInstanceChanged -= HandleVotingManagerChanged;
        DetachFromVotingManager();
    }

    private void HandleVotingManagerChanged(VotingManager manager)
    {
        if (_registeredVotingManager == manager)
            return;

        DetachFromVotingManager();

        if (manager == null)
            return;

        _registeredVotingManager = manager;
        _registeredVotingManager.OnVotingStarted += InitializeOptions;
        _registeredVotingManager.OnVoteCountsUpdated += OnVoteCountsUpdated;
        _registeredVotingManager.OnVotingEnded += OnVotingEnded;
        _registeredVotingManager.OnVotingTimerUpdate += OnTimerUpdate;

        var currentOptions = _registeredVotingManager.GetCurrentOptions();
        if (currentOptions != null && currentOptions.Count > 0)
        {
            InitializeOptions(currentOptions);

            var counts = _registeredVotingManager.GetVoteCounts();
            if (counts != null && counts.Length > 0)
            {
                OnVoteCountsUpdated(counts);
            }
        }
    }

    private void DetachFromVotingManager()
    {
        if (_registeredVotingManager != null)
        {
            _registeredVotingManager.OnVotingStarted -= InitializeOptions;
            _registeredVotingManager.OnVoteCountsUpdated -= OnVoteCountsUpdated;
            _registeredVotingManager.OnVotingEnded -= OnVotingEnded;
            _registeredVotingManager.OnVotingTimerUpdate -= OnTimerUpdate;
            _registeredVotingManager = null;
        }

        if (_isActive)
        {
            CleanupVoting();
        }
    }

    public void InitializeOptions(List<MinigameOptionRuntime> options)
    {
        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("[UIVoteInputProvider] No options to display!");
            return;
        }

        // Clear previous cards
        CleanupCards();

        // Create new cards
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var card = Instantiate(cardPrefab, cardsContainer);
            card.Initialize(option, i);
            card.OnVoteClicked += OnCardClicked;
            _spawnedCards.Add(card);
        }

        // Show panel
        if (votingPanel != null)
        {
            votingPanel.SetActive(true);
        }

        // Show and initialize timer UI
        if (showTimer && _registeredVotingManager != null)
        {
            _votingDuration = _registeredVotingManager.VotingTimeRemaining;
            _lastSecond = -1; // deixa a primeira atualização disparar animação/popup
            _isUrgent = false;
            
            if (timerFillImage != null)
            {
                timerFillImage.gameObject.SetActive(true);
                timerFillImage.fillAmount = 1f;
                timerFillImage.color = normalColor;
                
                // Entrada animada com pop
                if (timerContainer != null)
                {
                    timerContainer.localScale = Vector3.zero;
                    LeanTween.scale(timerContainer.gameObject, Vector3.one, 0.5f)
                        .setEaseOutBack()
                        .setOvershoot(1.2f);
                }
            }
            
            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                timerText.alpha = 0f;
                LeanTween.alphaText(timerText.rectTransform, 1f, 0.3f).setEaseOutQuad();
                UpdateTimerDisplay(_registeredVotingManager.VotingTimeRemaining);
            }
        }

        // Unlock cursor for voting
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _isActive = true;
        _currentVote = -1;

        Debug.Log($"[UIVoteInputProvider] Initialized with {options.Count} options");
    }

    public void CleanupVoting()
    {
        _isActive = false;
        CleanupCards();

        if (votingPanel != null && autoHideOnVotingEnd)
        {
            votingPanel.SetActive(false);
        }
        
        // Cancel any active animations
        if (_shakeTweenId != -1)
        {
            LeanTween.cancel(_shakeTweenId);
            _shakeTweenId = -1;
        }
        
        if (timerContainer != null)
        {
            LeanTween.cancel(timerContainer.gameObject);
            timerContainer.anchoredPosition = _originalTimerPosition;
        }
        
        // Hide timer UI
        if (timerFillImage != null)
        {
            LeanTween.cancel(timerFillImage.gameObject);
            timerFillImage.gameObject.SetActive(false);
        }
        
        if (timerText != null)
        {
            LeanTween.cancel(timerText.gameObject);
            timerText.gameObject.SetActive(false);
        }

        // Lock cursor again after voting
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CleanupCards()
    {
        foreach (var card in _spawnedCards)
        {
            if (card != null)
            {
                card.OnVoteClicked -= OnCardClicked;
                Destroy(card.gameObject);
            }
        }
        _spawnedCards.Clear();
    }

    private void OnCardClicked(int optionIndex)
    {
        if (!_isActive)
        {
            Debug.LogWarning("[UIVoteInputProvider] Voting is not active!");
            return;
        }

        if (VotingManager.Instance == null)
        {
            Debug.LogWarning("[UIVoteInputProvider] VotingManager instance not found!");
            return;
        }

        // Get local player ID
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null)
        {
            Debug.LogWarning("[UIVoteInputProvider] Local player not found!");
            return;
        }

        var playerData = localPlayer.GetComponent<PlayerData>();
        if (playerData == null)
        {
            Debug.LogWarning("[UIVoteInputProvider] PlayerData component not found on local player!");
            return;
        }

        ulong playerId = playerData.playerInfo.steamId;

        // Update visual selection
        UpdateCardSelection(optionIndex);

        // Send vote to server via Command
        CmdRegisterVote(playerId, optionIndex);

        Debug.Log($"[UIVoteInputProvider] Local player {playerId} voted for option {optionIndex}");
    }

    [Command(requiresAuthority = false)]
    private void CmdRegisterVote(ulong playerId, int optionIndex)
    {
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.RegisterVote(playerId, optionIndex);
        }
    }

    private void UpdateCardSelection(int selectedIndex)
    {
        _currentVote = selectedIndex;

        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            _spawnedCards[i].SetSelected(i == selectedIndex);
        }
    }

    private void OnVoteCountsUpdated(int[] voteCounts)
    {
        // Update vote counts on all cards
        for (int i = 0; i < _spawnedCards.Count && i < voteCounts.Length; i++)
        {
            _spawnedCards[i].UpdateVoteCount(voteCounts[i]);
        }
    }

    private void OnVotingEnded(MinigameOptionRuntime winner)
    {
        Debug.Log($"[UIVoteInputProvider] Voting ended. Winner: {winner.displayName}");
        
        if (autoHideOnVotingEnd)
        {
            // Delay cleanup to show final results briefly
            LeanTween.delayedCall(gameObject, 2f, () => CleanupVoting());
        }
    }

    private void OnTimerUpdate(float timeRemaining)
    {
        if (!showTimer) return;
        
        // Se o cliente ainda não recebeu a duração total, inicializa aqui para as animações funcionarem
        if (_votingDuration <= 0f || timeRemaining > _votingDuration)
        {
            float inferred = timeRemaining;
            if (_registeredVotingManager != null && _registeredVotingManager.VotingTimeRemaining > 0f)
                inferred = _registeredVotingManager.VotingTimeRemaining;

            _votingDuration = Mathf.Max(inferred, 0.01f);
        }
        
        UpdateTimerDisplay(timeRemaining);
    }

    private void UpdateTimerDisplay(float timeRemaining)
    {
        int currentSecond = Mathf.CeilToInt(timeRemaining);
        
        // Update text
        if (timerText != null)
        {
            timerText.text = $"Tempo: {currentSecond}s";
            
            // Pop animation on second change
            if (currentSecond != _lastSecond && currentSecond > 0)
            {
                LeanTween.cancel(timerText.gameObject);
                timerText.rectTransform.localScale = Vector3.one;
                LeanTween.scale(timerText.gameObject, Vector3.one * popScaleAmount, popDuration * 0.5f)
                    .setEaseOutQuad()
                    .setOnComplete(() => {
                        LeanTween.scale(timerText.gameObject, Vector3.one, popDuration * 0.5f)
                            .setEaseInQuad();
                    });
            }
        }
        
        // Update fill amount
        if (timerFillImage != null && _votingDuration > 0)
        {
            float normalizedValue = Mathf.Clamp01(timeRemaining / _votingDuration);
            timerFillImage.fillAmount = normalizedValue;
            
            // Check if entering urgent state
            bool nowUrgent = timeRemaining <= urgentThreshold;
            if (nowUrgent && !_isUrgent)
            {
                _isUrgent = true;
                StartUrgentAnimation();
            }
            else if (!nowUrgent && _isUrgent)
            {
                _isUrgent = false;
                StopUrgentAnimation();
            }
        }
        
        _lastSecond = currentSecond;
    }
    
    private void StartUrgentAnimation()
    {
        if (timerFillImage != null)
        {
            // Mudança de cor suave
            LeanTween.cancel(timerFillImage.gameObject);
            LeanTween.value(timerFillImage.gameObject, timerFillImage.color, urgentColor, 0.3f)
                .setOnUpdate((Color c) => timerFillImage.color = c)
                .setEaseInOutQuad();
        }
        
        // Inicia tremor contínuo
        if (enableShakeAnimation && timerContainer != null)
        {
            StartShakeLoop();
        }
    }
    
    private void StopUrgentAnimation()
    {
        if (timerFillImage != null)
        {
            LeanTween.cancel(timerFillImage.gameObject);
            LeanTween.value(timerFillImage.gameObject, timerFillImage.color, normalColor, 0.3f)
                .setOnUpdate((Color c) => timerFillImage.color = c)
                .setEaseInOutQuad();
        }
        
        if (_shakeTweenId != -1)
        {
            LeanTween.cancel(_shakeTweenId);
            _shakeTweenId = -1;
        }
        
        if (timerContainer != null)
        {
            LeanTween.cancel(timerContainer.gameObject);
            LeanTween.moveLocal(timerContainer.gameObject, _originalTimerPosition, 0.2f)
                .setEaseOutQuad();
        }
    }
    
    private void StartShakeLoop()
    {
        if (timerContainer == null || !enableShakeAnimation) return;
        
        float randomX = Random.Range(-shakeIntensity, shakeIntensity);
        float randomY = Random.Range(-shakeIntensity, shakeIntensity);
        Vector3 shakePos = _originalTimerPosition + new Vector3(randomX, randomY, 0);
        
        _shakeTweenId = LeanTween.moveLocal(timerContainer.gameObject, shakePos, shakeSpeed)
            .setEaseInOutQuad()
            .setOnComplete(() => {
                if (_isUrgent && timerContainer != null)
                {
                    StartShakeLoop();
                }
            }).id;
    }

    private void OnDestroy()
    {
        CleanupCards();
    }
}
