using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// UI-based voting input provider.
/// Displays vote cards and handles click-based voting.
/// </summary>
public class UIVoteInputProvider : NetworkBehaviour, IVoteInputProvider
{
    [Header("UI References")]
    [SerializeField] private GameObject votingPanel;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private VoteCard cardPrefab;

    [Header("Settings")]
    [SerializeField] private bool autoHideOnVotingEnd = true;

    private readonly List<VoteCard> _spawnedCards = new List<VoteCard>();
    private int _currentVote = -1;
    private bool _isActive;

    public bool IsActive => _isActive;

    private void Awake()
    {
        if (votingPanel != null)
        {
            votingPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Subscribe to VotingManager events
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.OnVotingStarted += InitializeOptions;
            VotingManager.Instance.OnVoteCountsUpdated += OnVoteCountsUpdated;
            VotingManager.Instance.OnVotingEnded += OnVotingEnded;
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

    private void OnDestroy()
    {
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.OnVotingStarted -= InitializeOptions;
            VotingManager.Instance.OnVoteCountsUpdated -= OnVoteCountsUpdated;
            VotingManager.Instance.OnVotingEnded -= OnVotingEnded;
        }

        CleanupCards();
    }
}
