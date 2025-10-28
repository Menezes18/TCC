using UnityEngine;
using Mirror;

/// <summary>
/// Example script showing how to manually trigger voting or integrate with custom game flow.
/// This is for reference/testing purposes.
/// </summary>
public class VotingSystemExample : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MinigameCatalog catalog;

    private void Start()
    {
        // Example: Subscribe to voting events
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.OnVotingStarted += OnVotingStarted;
            VotingManager.Instance.OnVoteCountsUpdated += OnVoteCountsUpdated;
            VotingManager.Instance.OnVotingEnded += OnVotingEnded;
        }
    }

    private void OnDestroy()
    {
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.OnVotingStarted -= OnVotingStarted;
            VotingManager.Instance.OnVoteCountsUpdated -= OnVoteCountsUpdated;
            VotingManager.Instance.OnVotingEnded -= OnVotingEnded;
        }
    }

    // Example: Manually start voting (server only)
    [ContextMenu("Start Voting (Server Only)")]
    public void ManuallyStartVoting()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("Can only start voting on server!");
            return;
        }

        // Ensure systems are initialized
        if (MinigameRotationState.Instance == null)
        {
            var go = new GameObject("MinigameRotationState");
            go.AddComponent<MinigameRotationState>();
        }

        if (catalog != null)
        {
            MinigameRotationState.Instance.SetCatalog(catalog);
        }

        if (VotingManager.Instance != null)
        {
            if (catalog != null)
            {
                VotingManager.Instance.SetCatalog(catalog);
            }
            VotingManager.Instance.StartVotingRound();
        }
    }

    // Example: Manually end voting (server only)
    [ContextMenu("End Voting (Server Only)")]
    public void ManuallyEndVoting()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("Can only end voting on server!");
            return;
        }

        if (VotingManager.Instance != null)
        {
            var winner = VotingManager.Instance.EndVoting();
            if (winner != null)
            {
                Debug.Log($"Winner: {winner.displayName}");
                
                // Mark as played
                if (MinigameRotationState.Instance != null)
                {
                    MinigameRotationState.Instance.MarkAsPlayed(winner.id);
                }
            }
        }
    }

    // Example: Reset rotation state
    [ContextMenu("Reset Rotation State")]
    public void ManuallyResetRotation()
    {
        if (MinigameRotationState.Instance != null)
        {
            MinigameRotationState.Instance.Reset();
            Debug.Log("Rotation state reset!");
        }
    }

    // Event handlers
    private void OnVotingStarted(System.Collections.Generic.List<MinigameOptionRuntime> options)
    {
        Debug.Log($"[Example] Voting started with {options.Count} options:");
        foreach (var option in options)
        {
            Debug.Log($"  - {option.displayName}");
        }
    }

    private void OnVoteCountsUpdated(int[] counts)
    {
        Debug.Log($"[Example] Vote counts updated: {string.Join(", ", counts)}");
    }

    private void OnVotingEnded(MinigameOptionRuntime winner)
    {
        Debug.Log($"[Example] Voting ended. Winner: {winner.displayName}");
    }

    // Example: Simulate a vote from code (for testing)
    [ContextMenu("Simulate Vote (Client)")]
    public void SimulateVote()
    {
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null)
        {
            Debug.LogWarning("No local player found!");
            return;
        }

        var playerData = localPlayer.GetComponent<PlayerData>();
        if (playerData == null)
        {
            Debug.LogWarning("PlayerData not found on local player!");
            return;
        }

        // Vote for option 0 (first option)
        if (VotingManager.Instance != null)
        {
            // In a real scenario, this would be called via Command from the input provider
            Debug.Log("In real usage, votes are sent via the input providers (UI or Zones)");
        }
    }
}
