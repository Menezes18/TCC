using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Server-authoritative voting manager that handles minigame voting.
/// Manages vote registration, counting, and winner determination.
/// </summary>
public class VotingManager : NetworkBehaviour
{
    #region Singleton

    public static VotingManager Instance { get; private set; }
    public static event Action<VotingManager> OnInstanceChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            OnInstanceChanged?.Invoke(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when voting options are initialized. Passes the list of options.
    /// </summary>
    public event Action<List<MinigameOptionRuntime>> OnVotingStarted;

    /// <summary>
    /// Fired when vote counts are updated. Passes the current vote counts.
    /// </summary>
    public event Action<int[]> OnVoteCountsUpdated;

    /// <summary>
    /// Fired when voting ends and a winner is determined.
    /// </summary>
    public event Action<MinigameOptionRuntime> OnVotingEnded;

    #endregion

    #region Network Synchronized State

    // SyncList to hold the voting options (serialized as strings for network compatibility)
    private readonly SyncList<string> _optionIds = new SyncList<string>();
    private readonly SyncList<string> _optionNames = new SyncList<string>();
    private readonly SyncList<string> _optionScenes = new SyncList<string>();

    // SyncList to hold vote counts for each option
    private readonly SyncList<int> _voteCounts = new SyncList<int>();

    #endregion

    #region Server-Only State

    // Server-side: maps playerId to the index they voted for (-1 = no vote)
    private readonly Dictionary<ulong, int> _playerVotes = new Dictionary<ulong, int>();

    // Server-side: cache of the actual MinigameCatalog entries for the current options
    private readonly List<MinigameCatalog.MinigameEntry> _currentOptionEntries = new List<MinigameCatalog.MinigameEntry>();

    // Client-side: cache of runtime options reconstructed from sync lists
    private readonly List<MinigameOptionRuntime> _clientOptions = new List<MinigameOptionRuntime>();

    #endregion

    #region Configuration

    [SerializeField, Tooltip("Reference to the catalog - can be set at runtime")]
    private MinigameCatalog _catalog;

    [SerializeField, Tooltip("Maximum number of voting options to present (typically 3)")]
    private int _maxOptions = 3;

    [SerializeField, Tooltip("Duration of voting in seconds")]
    private float _votingDuration = 15f;

    [SerializeField, Tooltip("Freeze players during voting")]
    private bool _freezePlayersDuringVoting = true;

    #endregion

    #region Voting Timer

    [SyncVar(hook = nameof(OnVotingTimeRemainingChanged))]
    private float _votingTimeRemaining;

    [SyncVar]
    private bool _isVotingActive;

    public event Action<float> OnVotingTimerUpdate;
    public float VotingTimeRemaining => _votingTimeRemaining;
    public bool IsVotingActive => _isVotingActive;

    private void OnVotingTimeRemainingChanged(float oldTime, float newTime)
    {
        OnVotingTimerUpdate?.Invoke(newTime);
    }

    #endregion

    #region Lifecycle

    private void Start()
    {
        // Subscribe to SyncList changes on clients
        if (!isServer)
        {
            _optionIds.Callback += OnOptionsChanged;
            _optionNames.Callback += OnOptionsChanged;
            _optionScenes.Callback += OnOptionsChanged;
            _voteCounts.Callback += OnVoteCountsChanged;

            if (_optionIds.Count > 0)
            {
                RebuildClientOptions();
            }
        }
    }

    private void Update()
    {
        if (isServer && _isVotingActive && _votingTimeRemaining > 0)
        {
            _votingTimeRemaining -= Time.deltaTime;
            
            if (_votingTimeRemaining <= 0)
            {
                _votingTimeRemaining = 0;
                _isVotingActive = false;
                Debug.Log("⏰ [VOTING] Timer expired!");
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ClearVotingState();
    }

    private void OnDestroy()
    {
        if (!isServer)
        {
            _optionIds.Callback -= OnOptionsChanged;
            _optionNames.Callback -= OnOptionsChanged;
            _optionScenes.Callback -= OnOptionsChanged;
            _voteCounts.Callback -= OnVoteCountsChanged;
        }

        if (Instance == this)
        {
            Instance = null;
            OnInstanceChanged?.Invoke(null);
        }
    }

    #endregion

    #region Public API


    [Server]
    public void SetVotingDuration(float duration)
    {
        _votingDuration = Mathf.Max(1f, duration);
        Debug.Log($"🗳️ [VOTING] Voting duration set to {_votingDuration} seconds");
    }

    /// <summary>
    /// Starts a new voting round. Call this from the server/host.
    /// Selects up to maxOptions eligible minigames and initializes voting.
    /// </summary>
    [Server]
    public bool StartVotingRound()
    {
        if (_catalog == null)
        {
            Debug.LogError("[VotingManager] Cannot start voting: catalog is null!");
            return false;
        }

        var rotationState = MinigameRotationState.Instance;
        if (rotationState == null)
        {
            Debug.LogError("[VotingManager] Cannot start voting: MinigameRotationState.Instance is null!");
            return false;
        }

        // Get eligible minigames
        var eligible = rotationState.GetEligibleMinigames();

        // Handle case where no minigames are eligible
        if (eligible.Count == 0)
        {
            Debug.LogWarning("⚠️ [VOTING] No eligible minigames found! Forcing rotation reset.");
            rotationState.Reset();
            eligible = rotationState.GetEligibleMinigames();

            if (eligible.Count == 0)
            {
                Debug.LogError("[VOTING] Even after reset, no eligible minigames found! Check catalog setup.");
                return false;
            }
        }

        // Clear previous state
        ClearVotingState();

        // Select up to maxOptions from eligible minigames
        int optionCount = Mathf.Min(_maxOptions, eligible.Count);
        var selectedOptions = SelectRandomOptions(eligible, optionCount);

        // Store server-side
        _currentOptionEntries.Clear();
        _currentOptionEntries.AddRange(selectedOptions);

        // Populate SyncLists for network replication
        Debug.Log($"🗳️ [VOTING SERVER] Populating SyncLists with {selectedOptions.Count} options:");
        foreach (var entry in selectedOptions)
        {
            _optionIds.Add(entry.id);
            // Ensure displayName is never null or empty - use id as fallback
            string displayName = entry.displayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = entry.id;
                Debug.LogWarning($"  ⚠️ Entry '{entry.id}' has no displayName, using ID as fallback");
            }
            _optionNames.Add(displayName);
            _optionScenes.Add(entry.SceneIdentifier);
            _voteCounts.Add(0);
            
            Debug.Log($"  ✅ Added: ID='{entry.id}', DisplayName='{displayName}', Scene='{entry.SceneIdentifier}'");
        }

        Debug.Log($"🗳️ [VOTING] Started voting round with {optionCount} options: {string.Join(", ", selectedOptions.Select(o => o.displayName))}");

        // Initialize voting timer
        _votingTimeRemaining = _votingDuration;
        _isVotingActive = true;

        // Freeze players if enabled
        if (_freezePlayersDuringVoting)
        {
            FreezeAllPlayers(true);
        }

        // Prepare data to send to clients via RPC
        string[] ids = selectedOptions.Select(e => e.id).ToArray();
        string[] names = selectedOptions.Select(e => {
            string name = e.displayName;
            return string.IsNullOrWhiteSpace(name) ? e.id : name;
        }).ToArray();
        string[] scenes = selectedOptions.Select(e => e.SceneIdentifier).ToArray();
        
        // Send to all clients via RPC (this will trigger OnVotingStarted for everyone including host)
        RpcSyncVotingOptions(ids, names, scenes);
        Debug.Log($"📡 [VOTING SERVER] Sent RPC with {names.Length} options");

        return true;
    }

    /// <summary>
    /// ClientRpc to synchronize voting options from server to all clients.
    /// This ensures clients receive the correct displayNames.
    /// </summary>
    [ClientRpc]
    private void RpcSyncVotingOptions(string[] ids, string[] names, string[] scenes)
    {
        // Don't skip on server - host needs UI updates too!
        Debug.Log($"📡 [VOTING CLIENT] Received RPC with {names.Length} options (isServer: {isServer})");
        
        // Only update SyncLists if we're a pure client (not server/host)
        if (!isServer)
        {
            // Clear and populate with received data
            _optionIds.Clear();
            _optionNames.Clear();
            _optionScenes.Clear();
            
            for (int i = 0; i < ids.Length; i++)
            {
                _optionIds.Add(ids[i]);
                _optionNames.Add(i < names.Length ? names[i] : ids[i]);
                _optionScenes.Add(i < scenes.Length ? scenes[i] : "");
                
                Debug.Log($"  📥 Option {i}: ID='{ids[i]}', Name='{names[i]}', Scene='{scenes[i]}'");
            }
        }
        
        // Rebuild client options with the new data (for both server and clients)
        RebuildClientOptions();
    }

    /// <summary>
    /// Registers or updates a player's vote.
    /// </summary>
    [Server]
    public void RegisterVote(ulong playerId, int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= _optionIds.Count)
        {
            Debug.LogWarning($"[VOTING] Invalid vote from player {playerId}: option index {optionIndex} out of range");
            return;
        }

        // Check if player already voted
        if (_playerVotes.TryGetValue(playerId, out int previousVote))
        {
            // Player is changing their vote
            if (previousVote == optionIndex)
            {
                // Same vote, nothing to do
                return;
            }

            // Decrement old vote
            if (previousVote >= 0 && previousVote < _voteCounts.Count)
            {
                _voteCounts[previousVote] = Mathf.Max(0, _voteCounts[previousVote] - 1);
            }
        }

        // Register new vote
        _playerVotes[playerId] = optionIndex;
        _voteCounts[optionIndex] = _voteCounts[optionIndex] + 1;

        Debug.Log($"🗳️ [VOTING] Player {playerId} voted for option {optionIndex} ({_optionNames[optionIndex]}). New count: {_voteCounts[optionIndex]}");

        // Notify listeners
        OnVoteCountsUpdated?.Invoke(_voteCounts.ToArray());
    }

    /// <summary>
    /// Removes a player's vote (e.g., when they disconnect).
    /// </summary>
    [Server]
    public void RemovePlayerVote(ulong playerId)
    {
        if (_playerVotes.TryGetValue(playerId, out int optionIndex))
        {
            if (optionIndex >= 0 && optionIndex < _voteCounts.Count)
            {
                _voteCounts[optionIndex] = Mathf.Max(0, _voteCounts[optionIndex] - 1);
                Debug.Log($"🗳️ [VOTING] Removed vote from disconnected player {playerId} (option {optionIndex})");
            }
            _playerVotes.Remove(playerId);
        }
    }

    /// <summary>
    /// Gets the current vote counts.
    /// </summary>
    public int[] GetVoteCounts()
    {
        return _voteCounts.ToArray();
    }

    /// <summary>
    /// Gets the current voting options (client-side).
    /// </summary>
    public List<MinigameOptionRuntime> GetCurrentOptions()
    {
        if (isServer)
        {
            return _currentOptionEntries.Select(MinigameOptionRuntime.FromCatalogEntry).ToList();
        }
        else
        {
            return new List<MinigameOptionRuntime>(_clientOptions);
        }
    }

    /// <summary>
    /// Ends the voting, determines the winner, and returns the winning minigame entry.
    /// Handles tie-breaking by random selection.
    /// </summary>
    [Server]
    public MinigameCatalog.MinigameEntry EndVoting()
    {
        if (_currentOptionEntries.Count == 0)
        {
            Debug.LogError("[VOTING] Cannot end voting: no options available!");
            return null;
        }

        // Special case: only one option
        if (_currentOptionEntries.Count == 1)
        {
            var winner = _currentOptionEntries[0];
            Debug.Log($"🏆 [VOTING] Only one option available, auto-selecting: {winner.displayName}");
            OnVotingEnded?.Invoke(MinigameOptionRuntime.FromCatalogEntry(winner));
            return winner;
        }

        // Find the maximum vote count
        int maxVotes = _voteCounts.Max();

        // Find all options with the maximum votes (for tie-breaking)
        var winningIndices = new List<int>();
        for (int i = 0; i < _voteCounts.Count; i++)
        {
            if (_voteCounts[i] == maxVotes)
            {
                winningIndices.Add(i);
            }
        }

        // Select winner
        int winnerIndex;
        if (winningIndices.Count == 1)
        {
            winnerIndex = winningIndices[0];
            Debug.Log($"🏆 [VOTING] Winner determined: {_optionNames[winnerIndex]} with {maxVotes} votes");
        }
        else
        {
            // Tie-breaking: random selection
            winnerIndex = winningIndices[Random.Range(0, winningIndices.Count)];
            Debug.Log($"🎲 [VOTING] TIE! {winningIndices.Count} options tied with {maxVotes} votes. Random winner: {_optionNames[winnerIndex]}");
        }

        var winningEntry = _currentOptionEntries[winnerIndex];

        // Stop voting timer and unfreeze players
        _isVotingActive = false;
        _votingTimeRemaining = 0;
        
        if (_freezePlayersDuringVoting)
        {
            FreezeAllPlayers(false);
        }

        // Notify listeners
        OnVotingEnded?.Invoke(MinigameOptionRuntime.FromCatalogEntry(winningEntry));

        return winningEntry;
    }

    /// <summary>
    /// Sets the catalog reference.
    /// </summary>
    public void SetCatalog(MinigameCatalog catalog)
    {
        _catalog = catalog;
    }

    #endregion

    #region Private Methods

    [Server]
    private void ClearVotingState()
    {
        _playerVotes.Clear();
        _currentOptionEntries.Clear();
        _optionIds.Clear();
        _optionNames.Clear();
        _optionScenes.Clear();
        _voteCounts.Clear();
        _isVotingActive = false;
        _votingTimeRemaining = 0;
    }

    [Server]
    private void FreezeAllPlayers(bool freeze)
    {
        var playerList = PlayerList.singleton;
        if (playerList == null)
        {
            Debug.LogWarning("[VOTING] PlayerList not found, cannot freeze/unfreeze players");
            return;
        }

        foreach (var playerData in playerList.players)
        {
            if (playerData == null) continue;
            
            var playerScript = playerData.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                playerScript.isFrozen = freeze;
            }
        }

        Debug.Log($"❄️ [VOTING] Players {(freeze ? "frozen" : "unfrozen")} for voting");
    }

    [Server]
    private List<MinigameCatalog.MinigameEntry> SelectRandomOptions(List<MinigameCatalog.MinigameEntry> eligible, int count)
    {
        // Fisher-Yates shuffle and take first 'count' elements
        var shuffled = new List<MinigameCatalog.MinigameEntry>(eligible);
        
        for (int i = 0; i < count && i < shuffled.Count - 1; i++)
        {
            int swapIndex = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
        }

        return shuffled.Take(count).ToList();
    }

    // Client-side callback when options change
    private void OnOptionsChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        RebuildClientOptions();
    }

    // Client-side callback when vote counts change
    private void OnVoteCountsChanged(SyncList<int>.Operation op, int index, int oldCount, int newCount)
    {
        OnVoteCountsUpdated?.Invoke(_voteCounts.ToArray());
    }

    private void RebuildClientOptions()
    {
        _clientOptions.Clear();
        
        Debug.Log($"🔄 [VOTING CLIENT] Rebuilding options. IDs: {_optionIds.Count}, Names: {_optionNames.Count}, Scenes: {_optionScenes.Count}");
        
        for (int i = 0; i < _optionIds.Count; i++)
        {
            string displayName = i < _optionNames.Count ? _optionNames[i] : _optionIds[i];
            
            var option = new MinigameOptionRuntime
            {
                id = _optionIds[i],
                displayName = displayName,
                sceneIdentifier = i < _optionScenes.Count ? _optionScenes[i] : ""
            };
            _clientOptions.Add(option);
            
            Debug.Log($"  Option {i}: ID='{option.id}', DisplayName='{option.displayName}', Scene='{option.sceneIdentifier}'");
        }

        // Try to load icons from catalog if available
        if (_catalog != null)
        {
            foreach (var option in _clientOptions)
            {
                if (_catalog.TryGetEntry(option.id, out var entry))
                {
                    option.icon = entry.icon;
                }
            }
        }

        OnVotingStarted?.Invoke(new List<MinigameOptionRuntime>(_clientOptions));
    }

    #endregion
}
