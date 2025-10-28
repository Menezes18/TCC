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
    [SerializeField] private int optionIndex = -1;
    
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

        if (minigameNameText != null)
        {
            minigameNameText.text = option.displayName ?? option.id;
        }

        // Apply icon if available
        if (iconRenderer != null && option.icon != null)
        {
            if (iconMaterial == null)
            {
                iconMaterial = iconRenderer.material;
            }
            iconMaterial.mainTexture = option.icon.texture;
            iconRenderer.material = iconMaterial;
        }

        UpdateVoteCount(0);
    }

    /// <summary>
    /// Updates the displayed vote count.
    /// </summary>
    public void UpdateVoteCount(int count)
    {
        _currentVoteCount = count;
        
        if (voteCountText != null)
        {
            voteCountText.text = count == 1 ? "1 jogador" : $"{count} jogadores";
        }
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
