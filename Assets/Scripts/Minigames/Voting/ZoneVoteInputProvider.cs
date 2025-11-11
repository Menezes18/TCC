using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Physical zone-based voting input provider.
/// Spawns vote zones in the world and tracks player presence for voting.
/// </summary>
public class ZoneVoteInputProvider : NetworkBehaviour, IVoteInputProvider
{
    [Header("Zone Configuration")]
    [SerializeField] private VoteZone zonePrefab;
    [SerializeField] private Transform[] zoneSpawnPositions;

    [Header("Settings")]
    [SerializeField] private bool autoCleanupOnVotingEnd = true;
    [SerializeField] private float zoneSpacing = 5f;

    private readonly List<VoteZone> _spawnedZones = new List<VoteZone>();
    private readonly Dictionary<ulong, int> _playerCurrentZone = new Dictionary<ulong, int>();
    private bool _isActive;
    private VotingManager _registeredVotingManager;

    public bool IsActive => _isActive;

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

        if (isServer)
        {
            var options = _registeredVotingManager.GetCurrentOptions();
            if (options != null && options.Count > 0)
            {
                InitializeOptions(options);
                OnVoteCountsUpdated(_registeredVotingManager.GetVoteCounts());
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
            _registeredVotingManager = null;
        }

        if (_isActive)
        {
            CleanupVoting();
        }
    }

    public void InitializeOptions(List<MinigameOptionRuntime> options)
    {
        // Only server spawns zones
        if (!isServer)
            return;

        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("[ZoneVoteInputProvider] No options to create zones for!");
            return;
        }

        // Clear previous zones
        CleanupZones();

        // Determine spawn positions
        Vector3[] positions = GetSpawnPositions(options.Count);

        // Spawn zones
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var position = positions[i];

            var zone = Instantiate(zonePrefab, position, Quaternion.identity);
            zone.Initialize(option, i, this);
            
            // Spawn on network
            NetworkServer.Spawn(zone.gameObject);
            
            _spawnedZones.Add(zone);

            Debug.Log($"[ZoneVoteInputProvider] Spawned vote zone for '{option.displayName}' at {position}");
        }

        _isActive = true;
    }

    public void CleanupVoting()
    {
        if (!isServer)
            return;

        _isActive = false;
        _playerCurrentZone.Clear();
        CleanupZones();
    }

    /// <summary>
    /// Called by VoteZone when a player enters.
    /// </summary>
    [Server]
    public void OnPlayerEnteredZone(ulong playerId, int zoneIndex)
    {
        if (!_isActive)
            return;

        // Check if player was in a different zone
        if (_playerCurrentZone.TryGetValue(playerId, out int previousZone))
        {
            if (previousZone == zoneIndex)
            {
                // Player is already in this zone, nothing to do
                return;
            }
            // Player moved from one zone to another - no need to remove vote,
            // RegisterVote will handle the transition
        }

        // Update tracking
        _playerCurrentZone[playerId] = zoneIndex;

        // Register vote
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.RegisterVote(playerId, zoneIndex);
        }
    }

    /// <summary>
    /// Called by VoteZone when a player exits.
    /// </summary>
    [Server]
    public void OnPlayerExitedZone(ulong playerId, int zoneIndex)
    {
        if (!_isActive)
            return;

        // Check if player is still tracked in this zone
        if (_playerCurrentZone.TryGetValue(playerId, out int currentZone) && currentZone == zoneIndex)
        {
            // Player left their voting zone completely
            _playerCurrentZone.Remove(playerId);
            
            // Remove their vote
            if (VotingManager.Instance != null)
            {
                VotingManager.Instance.RemovePlayerVote(playerId);
            }
        }
    }

    private void CleanupZones()
    {
        foreach (var zone in _spawnedZones)
        {
            if (zone != null)
            {
                zone.ClearPlayers();
                if (isServer)
                {
                    NetworkServer.Destroy(zone.gameObject);
                }
                else
                {
                    Destroy(zone.gameObject);
                }
            }
        }
        _spawnedZones.Clear();
    }

    private Vector3[] GetSpawnPositions(int count)
    {
        // If spawn positions are manually defined, use them
        if (zoneSpawnPositions != null && zoneSpawnPositions.Length >= count)
        {
            var positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = zoneSpawnPositions[i].position;
            }
            return positions;
        }

        // Otherwise, generate positions in a line
        var result = new Vector3[count];
        var startOffset = -(count - 1) * zoneSpacing / 2f;
        
        for (int i = 0; i < count; i++)
        {
            result[i] = transform.position + transform.right * (startOffset + i * zoneSpacing);
        }

        return result;
    }

    private void OnVoteCountsUpdated(int[] voteCounts)
    {
        // Update vote counts on all zones
        for (int i = 0; i < _spawnedZones.Count && i < voteCounts.Length; i++)
        {
            if (_spawnedZones[i] != null)
            {
                _spawnedZones[i].UpdateVoteCount(voteCounts[i]);
            }
        }
    }

    private void OnVotingEnded(MinigameOptionRuntime winner)
    {
        Debug.Log($"[ZoneVoteInputProvider] Voting ended. Winner: {winner.displayName}");
        
        if (autoCleanupOnVotingEnd)
        {
            CleanupVoting();
        }
    }

    private void OnDestroy()
    {
        CleanupZones();
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (zoneSpawnPositions != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var pos in zoneSpawnPositions)
            {
                if (pos != null)
                {
                    Gizmos.DrawWireSphere(pos.position, 1f);
                }
            }
        }
        else
        {
            // Draw default positions
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 3; i++)
            {
                var startOffset = -(3 - 1) * zoneSpacing / 2f;
                var pos = transform.position + transform.right * (startOffset + i * zoneSpacing);
                Gizmos.DrawWireSphere(pos, 1f);
            }
        }
    }
    #endif
}
