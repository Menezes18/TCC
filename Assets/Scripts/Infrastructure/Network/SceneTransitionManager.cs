using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages synchronized scene transitions across all clients.
/// Ensures all players preload the scene before activating it.
/// </summary>
public class SceneTransitionManager : NetworkBehaviour
{
    #region Singleton
    public static SceneTransitionManager singleton;
    
    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    [Header("Configuration")]
    [SerializeField] private float preloadTimeout = 10f;
    [SerializeField] private float activationTimeout = 5f;
    [SerializeField] private bool forceActivateAfterTimeout = true;

    // Server-side tracking
    private readonly HashSet<int> _preloadAcks = new HashSet<int>();
    private readonly HashSet<int> _activationAcks = new HashSet<int>();
    private string _targetSceneName;
    private bool _isTransitioning = false;
    private Coroutine _transitionCoroutine;
    
    // Client-side state
    private AsyncOperation _preloadOperation;
    private bool _isPreloading = false;
    private bool _waitingForActivation = false;

    [SyncVar] private int expectedClients = 0;
    [SyncVar] private int preloadedClients = 0;
    [SyncVar] private int activatedClients = 0;

    #region Public API

    /// <summary>
    /// Server: Initiates a synchronized scene change.
    /// All clients will preload the scene before it activates.
    /// </summary>
    [Server]
    public void ServerChangeSceneSynchronized(string sceneName)
    {
        Debug.Log($"[SceneTransition] ServerChangeSceneSynchronized called with sceneName='{sceneName}'");
        
        if (_isTransitioning)
        {
            Debug.LogWarning($"[SceneTransition] Already transitioning to {_targetSceneName}. Ignoring request for {sceneName}");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneTransition] Scene name is null or empty!");
            return;
        }

        Debug.Log($"[SceneTransition] SERVER: Initiating synchronized transition to '{sceneName}'");
        _targetSceneName = sceneName;
        _isTransitioning = true;

        // Reset tracking
        _preloadAcks.Clear();
        _activationAcks.Clear();
        
        // Count expected clients (all authenticated connections)
        // If host, count server as a client too
        expectedClients = NetworkServer.connections.Count;
        if (NetworkClient.active && NetworkServer.active)
        {
            // Host counts as both server and client - don't double count
            Debug.Log("[SceneTransition] Running as Host - server will also load scene");
        }
        
        preloadedClients = 0;
        activatedClients = 0;

        Debug.Log($"[SceneTransition] Expecting {expectedClients} clients to preload");

        // Freeze all players during transition
        if (PlayerList.singleton != null)
        {
            PlayerList.singleton.AtivarPlayer(true);
            Debug.Log("[SceneTransition] Players frozen during transition");
        }

        // Tell all clients to preload
        RpcStartPreload(sceneName);

        // Start timeout coroutine with shorter timeout
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(PreloadTimeoutCoroutine());
    }

    #endregion

    #region Server Methods

    [Server]
    private IEnumerator PreloadTimeoutCoroutine()
    {
        float startTime = Time.time;
        float lastLogTime = Time.time;
        bool noResponseWarningShown = false;

        while (_preloadAcks.Count < expectedClients)
        {
            float elapsed = Time.time - startTime;
            
            // If no clients responded after 2 seconds and we have a fallback option, warn and prepare to use standard scene change
            if (!noResponseWarningShown && elapsed >= 2f && _preloadAcks.Count == 0)
            {
                Debug.LogWarning($"[SceneTransition] No clients responded after 2 seconds. This may indicate the RPC system isn't working properly.");
                noResponseWarningShown = true;
                
                if (forceActivateAfterTimeout)
                {
                    Debug.LogWarning($"[SceneTransition] Will fall back to standard scene change at {preloadTimeout}s if no responses received.");
                }
            }
            
            // Log progress every second
            if (Time.time - lastLogTime >= 1f)
            {
                LogPreloadProgress();
                lastLogTime = Time.time;
            }

            // Check timeout
            if (elapsed >= preloadTimeout)
            {
                if (_preloadAcks.Count == 0 && forceActivateAfterTimeout)
                {
                    Debug.LogError($"[SceneTransition] TIMEOUT with ZERO responses! Falling back to standard Mirror scene change.");
                    // Use standard Mirror scene change as fallback
                    _isTransitioning = false; // Reset state
                    NetworkManager.singleton.ServerChangeScene(_targetSceneName);
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"[SceneTransition] Preload timeout! Only {_preloadAcks.Count}/{expectedClients} clients ready. Proceeding anyway...");
                    break;
                }
            }

            yield return null;
        }

        Debug.Log($"[SceneTransition] All clients preloaded ({_preloadAcks.Count}/{expectedClients}). Activating scene...");
        ServerActivateScene();
    }

    [Server]
    private void ServerActivateScene()
    {
        Debug.Log($"[SceneTransition] SERVER: Activating scene '{_targetSceneName}' for all clients");
        
        // Tell clients to activate their preloaded scenes
        RpcActivatePreloadedScene();

        // Server also needs to load the scene
        StartCoroutine(ServerLoadSceneCoroutine());
    }

    [Server]
    private IEnumerator ServerLoadSceneCoroutine()
    {
        Debug.Log($"[SceneTransition] SERVER: Loading scene '{_targetSceneName}'");
        
        // Use Mirror's standard scene change mechanism for the server
        NetworkManager.singleton.ServerChangeScene(_targetSceneName);

        // Wait for activation acknowledgments from clients
        float startTime = Time.time;
        float lastLogTime = Time.time;

        while (_activationAcks.Count < expectedClients)
        {
            if (Time.time - lastLogTime >= 1f)
            {
                LogActivationProgress();
                lastLogTime = Time.time;
            }

            if (Time.time - startTime >= activationTimeout)
            {
                Debug.LogWarning($"[SceneTransition] Activation timeout! Only {_activationAcks.Count}/{expectedClients} clients activated.");
                break;
            }

            yield return null;
        }

        Debug.Log($"[SceneTransition] All clients activated scene ({_activationAcks.Count}/{expectedClients})");
        ServerFinishTransition();
    }

    [Server]
    private void ServerFinishTransition()
    {
        Debug.Log("[SceneTransition] SERVER: Scene transition complete. Starting briefing flow...");
        _isTransitioning = false;
        _transitionCoroutine = null;

        // The standard Mirror flow will handle the rest via OnServerSceneChanged
        // which triggers WaitAllConnectionsReadyThenStart -> BriefingManager.TriggerBriefing
    }

    [Server]
    private void LogPreloadProgress()
    {
        var connections = NetworkServer.connections.Values;
        List<string> status = new List<string>();
        
        foreach (var conn in connections)
        {
            bool ready = _preloadAcks.Contains(conn.connectionId);
            var pd = conn.identity?.GetComponent<PlayerData>();
            string name = pd != null ? pd.alias : $"Conn{conn.connectionId}";
            status.Add($"{name}: {(ready ? "✓" : "⏳")}");
        }

        Debug.Log($"[SceneTransition] Preload Progress ({_preloadAcks.Count}/{expectedClients}):\n  " + string.Join("\n  ", status));
    }

    [Server]
    private void LogActivationProgress()
    {
        var connections = NetworkServer.connections.Values;
        List<string> status = new List<string>();
        
        foreach (var conn in connections)
        {
            bool activated = _activationAcks.Contains(conn.connectionId);
            var pd = conn.identity?.GetComponent<PlayerData>();
            string name = pd != null ? pd.alias : $"Conn{conn.connectionId}";
            status.Add($"{name}: {(activated ? "✓" : "⏳")}");
        }

        Debug.Log($"[SceneTransition] Activation Progress ({_activationAcks.Count}/{expectedClients}):\n  " + string.Join("\n  ", status));
    }

    #endregion

    #region Client RPC

    [ClientRpc]
    private void RpcStartPreload(string sceneName)
    {
        // Skip if this is a dedicated server (no client)
        if (NetworkServer.active && !NetworkClient.active)
        {
            Debug.Log("[SceneTransition] Dedicated server - skipping client preload");
            return;
        }

        Debug.Log($"[SceneTransition] CLIENT: Starting preload of '{sceneName}' (IsHost: {NetworkServer.active && NetworkClient.active})");
        _isPreloading = true;
        _waitingForActivation = true;

        // Show loading screen
        LoadingScreenUI.Ensure();
        LoadingScreenUI.Instance?.SetMirrorTargetScene(sceneName);
        LoadingScreenUI.Instance?.ShowForMirror();

        // Start preload
        StartCoroutine(ClientPreloadSceneCoroutine(sceneName));
    }

    [ClientRpc]
    private void RpcActivatePreloadedScene()
    {
        if (NetworkServer.active && !NetworkClient.active)
        {
            // Server-only, activation is handled separately
            return;
        }

        Debug.Log("[SceneTransition] CLIENT: Received activation signal");
        
        if (!_waitingForActivation)
        {
            Debug.LogWarning("[SceneTransition] CLIENT: Received activation but not waiting for it!");
            return;
        }

        if (_preloadOperation != null && !_preloadOperation.isDone)
        {
            Debug.Log("[SceneTransition] CLIENT: Preload still in progress, allowing scene activation...");
            _preloadOperation.allowSceneActivation = true;
        }
        else
        {
            Debug.LogWarning("[SceneTransition] CLIENT: No preload operation found!");
        }
    }

    #endregion

    #region Client Methods

    private IEnumerator ClientPreloadSceneCoroutine(string sceneName)
    {
        // Start async load but don't activate yet
        _preloadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (_preloadOperation == null)
        {
            Debug.LogError($"[SceneTransition] CLIENT: Failed to start loading scene '{sceneName}'");
            yield break;
        }

        _preloadOperation.allowSceneActivation = false;

        // Wait for preload to reach 90% (Unity stops at 0.9 when allowSceneActivation is false)
        while (_preloadOperation.progress < 0.9f)
        {
            // Update loading UI
            if (LoadingScreenUI.Instance != null)
            {
                LoadingScreenUI.Instance.SetProgress(_preloadOperation.progress);
            }
            yield return null;
        }

        Debug.Log($"[SceneTransition] CLIENT: Preload complete for '{sceneName}' (progress: {_preloadOperation.progress})");
        
        // Notify server that this client is ready
        CmdNotifyPreloadComplete();

        // Wait for server to signal activation
        Debug.Log("[SceneTransition] CLIENT: Waiting for server activation signal...");
        while (!_preloadOperation.allowSceneActivation)
        {
            yield return null;
        }

        // Wait for actual scene activation
        while (!_preloadOperation.isDone)
        {
            yield return null;
        }

        Debug.Log($"[SceneTransition] CLIENT: Scene '{sceneName}' activated");
        _isPreloading = false;
        _waitingForActivation = false;

        // Notify server that activation is complete
        CmdNotifyActivationComplete();

        // Hide loading screen (will be shown again by briefing if needed)
        LoadingScreenUI.Instance?.Hide();
    }

    #endregion

    #region Commands (Client -> Server)

    [Command(requiresAuthority = false)]
    private void CmdNotifyPreloadComplete(NetworkConnectionToClient sender = null)
    {
        if (!isServer || sender == null) return;

        if (_preloadAcks.Add(sender.connectionId))
        {
            preloadedClients = _preloadAcks.Count;
            
            var pd = sender.identity?.GetComponent<PlayerData>();
            string clientName = pd != null ? pd.alias : $"Connection {sender.connectionId}";
            
            Debug.Log($"[SceneTransition] SERVER: Client '{clientName}' preloaded ({preloadedClients}/{expectedClients})");

            // If all clients are ready, activate immediately (don't wait for timeout)
            if (_preloadAcks.Count >= expectedClients && expectedClients > 0)
            {
                if (_transitionCoroutine != null)
                {
                    StopCoroutine(_transitionCoroutine);
                    _transitionCoroutine = null;
                }
                ServerActivateScene();
            }
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdNotifyActivationComplete(NetworkConnectionToClient sender = null)
    {
        if (!isServer || sender == null) return;

        if (_activationAcks.Add(sender.connectionId))
        {
            activatedClients = _activationAcks.Count;
            
            var pd = sender.identity?.GetComponent<PlayerData>();
            string clientName = pd != null ? pd.alias : $"Connection {sender.connectionId}";
            
            Debug.Log($"[SceneTransition] SERVER: Client '{clientName}' activated scene ({activatedClients}/{expectedClients})");
        }
    }

    #endregion

    #region Utility

    public bool IsTransitioning => _isTransitioning;

    #endregion
}
