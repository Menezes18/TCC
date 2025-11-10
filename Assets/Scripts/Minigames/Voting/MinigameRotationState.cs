using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the state of which minigames have been played during the current match.
/// Persists during the match and resets after the victory screen.
/// </summary>
public class MinigameRotationState : MonoBehaviour
{
    #region Singleton

    public static MinigameRotationState Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    [SerializeField, Tooltip("Reference to the minigame catalog")]
    private MinigameCatalog _catalog;

    // HashSet of minigame IDs that have been played this match
    private readonly HashSet<string> _playedMinigames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all minigames that have NOT been played yet this match AND are currently active.
    /// Only includes minigames that are enabled in the MinigameSelection UI.
    /// </summary>
    public List<MinigameCatalog.MinigameEntry> GetEligibleMinigames()
    {
        if (_catalog == null)
        {
            Debug.LogError("[MinigameRotationState] Catalog is not assigned!");
            return new List<MinigameCatalog.MinigameEntry>();
        }

        // Get the list of active minigames from NetworkManager
        var manager = MyNetworkManager.manager;
        var activeIds = manager?.ActiveMinigameIds;
        
        if (activeIds == null || activeIds.Count == 0)
        {
            Debug.LogWarning("[MinigameRotationState] No active minigames found in NetworkManager!");
            return new List<MinigameCatalog.MinigameEntry>();
        }

        Debug.Log($"🔍 [ROTATION] Checking eligibility - Active IDs: [{string.Join(", ", activeIds)}]");
        Debug.Log($"🔍 [ROTATION] Already played: [{string.Join(", ", _playedMinigames)}]");

        var eligible = _catalog.Entries
            .Where(entry => entry != null && 
                           entry.HasValidScene && 
                           !string.IsNullOrWhiteSpace(entry.id) &&
                           activeIds.Contains(entry.id) &&  // Only include active minigames
                           !_playedMinigames.Contains(entry.id))
            .ToList();
        
        Debug.Log($"✅ [ROTATION] Found {eligible.Count} eligible minigames: [{string.Join(", ", eligible.Select(e => e.displayName ?? e.id))}]");
        
        return eligible;
    }

    /// <summary>
    /// Marks a minigame as played so it won't appear in voting again this match.
    /// </summary>
    public void MarkAsPlayed(string minigameId)
    {
        if (string.IsNullOrWhiteSpace(minigameId))
            return;

        if (_playedMinigames.Add(minigameId))
        {
            Debug.Log($"✅ [ROTATION] Minigame '{minigameId}' marked as played. Total played: {_playedMinigames.Count}");
        }
    }

    /// <summary>
    /// Resets the played minigames list, making all minigames eligible again.
    /// Should be called when the victory screen is reached.
    /// </summary>
    public void Reset()
    {
        Debug.Log($"🔄 [ROTATION] Resetting minigame rotation state. Previously played: {_playedMinigames.Count}");
        _playedMinigames.Clear();
    }

    /// <summary>
    /// Gets the current count of played minigames.
    /// </summary>
    public int PlayedCount => _playedMinigames.Count;

    /// <summary>
    /// Checks if a specific minigame has been played this match.
    /// </summary>
    public bool HasBeenPlayed(string minigameId)
    {
        return _playedMinigames.Contains(minigameId);
    }

    /// <summary>
    /// Sets the catalog reference (useful for runtime initialization).
    /// </summary>
    public void SetCatalog(MinigameCatalog catalog)
    {
        _catalog = catalog;
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (_catalog == null)
        {
            var managers = FindObjectsByType<MyNetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (managers.Length > 0 && managers[0] != null)
            {
                // Try to get catalog from NetworkManager via reflection
                var field = managers[0].GetType().GetField("minigameCatalog", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    _catalog = field.GetValue(managers[0]) as MinigameCatalog;
                }
            }
        }
    }
    #endif
}
