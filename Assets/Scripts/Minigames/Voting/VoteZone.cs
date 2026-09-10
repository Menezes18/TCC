using System.Collections.Generic;
using Mirror;
using UnityEngine;
using TMPro;

/// <summary>
/// Represents a physical zone in the game world where players can vote
/// by entering the trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VoteZone : NetworkBehaviour
{
    [Header("Configuration")]
    [SyncVar] [SerializeField] private int optionIndex = -1;
    [SyncVar(hook = nameof(OnOptionIdChanged))] private string optionId;
    [SyncVar(hook = nameof(OnOptionNameChanged))] private string optionDisplayName;
    [SyncVar(hook = nameof(OnVoteCountChanged))] private int syncedVoteCount;
    
    [Header("Visual Feedback")]
    [SerializeField] private TMP_Text voteCountText;
    [SerializeField] private TMP_Text minigameNameText;
    [SerializeField] private MeshRenderer iconRenderer;
    [SerializeField] private Material iconMaterial;

    [Header("Colors")]
    [SerializeField] private Color zoneColor = Color.blue;

    private MinigameOptionRuntime _option;
    private readonly HashSet<ulong> _playersInZone = new HashSet<ulong>();
    private int _currentVoteCount;

    // Reference to the provider that manages this zone
    private ZoneVoteInputProvider _provider;
    private VotingManager _presentationManager;

    public int OptionIndex => optionIndex;

    private void Awake()
    {
        // Ensure the collider is set as trigger
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    /// <summary>
    /// Initializes the vote zone with minigame option data.
    /// </summary>
    public void Initialize(MinigameOptionRuntime option, int index, ZoneVoteInputProvider provider)
    {
        _option = option;
        optionIndex = index;
        _provider = provider;
        optionId = option.id;
        optionDisplayName = option.displayName ?? option.id;

        if (minigameNameText != null)
        {
            minigameNameText.text = optionDisplayName;
        }

        // Apply icon if available
        ApplyIcon(option);

        UpdateVoteCount(0);
    }

    /// <summary>
    /// Updates the displayed vote count.
    /// </summary>
    public void UpdateVoteCount(int count)
    {
        if (isServer) syncedVoteCount = count;
        _currentVoteCount = count;
        
        if (voteCountText != null)
        {
            voteCountText.text = count == 1 ? "1 jogador" : $"{count} jogadores";
        }
    }

    private void OnOptionNameChanged(string _, string value)
    {
        optionDisplayName = value;
        if (minigameNameText != null) minigameNameText.text = value;
    }

    private void OnOptionIdChanged(string _, string __) => ApplyAvailableOptions();

    private void OnVoteCountChanged(int _, int value) => ApplyVoteCount(value);

    public override void OnStartClient()
    {
        base.OnStartClient();
        VotingManager.OnInstanceChanged += BindVotingManager;
        BindVotingManager(VotingManager.Instance);
        OnOptionNameChanged(null, optionDisplayName);
        ApplyVoteCount(syncedVoteCount);
    }

    public override void OnStopClient()
    {
        VotingManager.OnInstanceChanged -= BindVotingManager;
        BindVotingManager(null);
        base.OnStopClient();
    }

    private void BindVotingManager(VotingManager manager)
    {
        if (_presentationManager == manager) return;
        if (_presentationManager != null) _presentationManager.OnVotingStarted -= ApplyOptions;
        _presentationManager = manager;
        if (_presentationManager == null) return;
        _presentationManager.OnVotingStarted += ApplyOptions;
        ApplyOptions(_presentationManager.GetCurrentOptions());
    }

    private void ApplyAvailableOptions()
    {
        if (_presentationManager != null) ApplyOptions(_presentationManager.GetCurrentOptions());
    }

    private void ApplyOptions(List<MinigameOptionRuntime> options)
    {
        if (options == null) return;
        foreach (var option in options)
        {
            if (option != null && option.id == optionId)
            {
                _option = option;
                ApplyIcon(option);
                return;
            }
        }
    }

    private void ApplyIcon(MinigameOptionRuntime option)
    {
        if (iconRenderer == null || option?.icon == null) return;
        if (iconMaterial == null) iconMaterial = iconRenderer.material;
        iconMaterial.mainTexture = option.icon.texture;
        iconRenderer.material = iconMaterial;
    }

    private void ApplyVoteCount(int count)
    {
        _currentVoteCount = count;
        if (voteCountText != null)
            voteCountText.text = count == 1 ? "1 jogador" : $"{count} jogadores";
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only process on server
        if (!isServer)
            return;

        var playerData = other.GetComponent<PlayerData>();
        if (playerData == null)
            return;

        ulong playerId = playerData.playerInfo.steamId;

        if (_playersInZone.Add(playerId))
        {
            Debug.Log($"[VoteZone] Player {playerId} entered zone for option {optionIndex}");
            
            if (_provider != null)
            {
                _provider.OnPlayerEnteredZone(playerId, optionIndex);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Only process on server
        if (!isServer)
            return;

        var playerData = other.GetComponent<PlayerData>();
        if (playerData == null)
            return;

        ulong playerId = playerData.playerInfo.steamId;

        if (_playersInZone.Remove(playerId))
        {
            Debug.Log($"[VoteZone] Player {playerId} exited zone for option {optionIndex}");
            
            if (_provider != null)
            {
                _provider.OnPlayerExitedZone(playerId, optionIndex);
            }
        }
    }

    /// <summary>
    /// Clears all players from the zone (e.g., when voting ends).
    /// </summary>
    public void ClearPlayers()
    {
        _playersInZone.Clear();
    }

    /// <summary>
    /// Gets the count of players currently in this zone.
    /// </summary>
    public int GetPlayerCount()
    {
        return _playersInZone.Count;
    }
}
