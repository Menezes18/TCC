using System.Collections.Generic;

/// <summary>
/// Interface for pluggable vote input providers.
/// Allows different voting mechanisms (UI clicks, physical zones, etc.)
/// to integrate with the VotingManager.
/// </summary>
public interface IVoteInputProvider
{
    /// <summary>
    /// Called when voting starts. The provider should display/enable the voting options.
    /// </summary>
    /// <param name="options">The list of minigame options available for voting</param>
    void InitializeOptions(List<MinigameOptionRuntime> options);

    /// <summary>
    /// Called when voting ends. The provider should hide/disable the voting interface.
    /// </summary>
    void CleanupVoting();

    /// <summary>
    /// Gets whether this provider is currently active and ready to accept votes.
    /// </summary>
    bool IsActive { get; }
}
