using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages synchronized scene transitions across all clients.
/// Ensures all players preload the scene before activating it.
/// Implements robust ACK system with telemetry for debugging.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    #region Singleton
    public static SceneTransitionManager singleton;
    
    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(gameObject);
            RegisterHandlers();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Telemetry Events
    
    /// <summary>
    /// Telemetry event types for scene loading
    /// </summary>
    public enum TelemetryEventType
    {
        SceneLoadingStarted,
        SceneLoadACKReceived,
        SceneLoadTimeout,
        SceneActivationStarted,
        SceneActivationACKReceived,
        MatchStarted,
        PlayerDisconnectedDuringLoad
    }
    
    /// <summary>
    /// Telemetry data structure for scene loading events
    /// </summary>
    [Serializable]
    public struct SceneLoadTelemetry
    {
        public TelemetryEventType eventType;
        public int connectionId;
        public string playerName;
        public string sceneName;
        public float timestamp;
        public float loadDuration;
        public string additionalInfo;
        
        public override string ToString()
        {
            string durationStr = loadDuration > 0 ? $" (duration: {loadDuration:F2}s)" : "";
            string infoStr = !string.IsNullOrEmpty(additionalInfo) ? $" | {additionalInfo}" : "";
            return $"[Telemetry] {eventType} | Player: {playerName} (conn:{connectionId}) | Scene: {sceneName} | Time: {timestamp:F2}s{durationStr}{infoStr}";
        }
    }
    
    /// <summary>
    /// Player loading state tracking
    /// </summary>
    [Serializable]
    public class PlayerLoadState
    {
        public int connectionId;
        public string playerName;
        public ulong steamId;
        public float loadStartTime;
        public float loadEndTime;
        public bool hasPreloaded;
        public bool hasActivated;
        public bool timedOut;
        public bool disconnected;
        
        public float LoadDuration => hasPreloaded ? (loadEndTime - loadStartTime) : (Time.realtimeSinceStartup - loadStartTime);
    }
    
    // Telemetry log
    private readonly List<SceneLoadTelemetry> _telemetryLog = new List<SceneLoadTelemetry>();
    
    // Player load states (server-side)
    private readonly Dictionary<int, PlayerLoadState> _playerLoadStates = new Dictionary<int, PlayerLoadState>();
    
    /// <summary>
    /// Event fired when a telemetry event is logged
    /// </summary>
    public event Action<SceneLoadTelemetry> OnTelemetryEvent;
    
    /// <summary>
    /// Event fired when all players have loaded (for external systems to react)
    /// </summary>
    public event Action OnAllPlayersLoaded;
    
    /// <summary>
    /// Event fired when loading progress changes (for UI updates)
    /// </summary>
    public event Action<int, int> OnLoadingProgressChanged; // (loaded, total)
    
    #endregion

    #region Message Registration

    private void RegisterHandlers()
    {
        if (!_serverHandlersRegistered)
        {
            NetworkServer.RegisterHandler<ScenePreloadAckMessage>(OnServerReceivePreloadAck, false);
            NetworkServer.RegisterHandler<SceneActivationAckMessage>(OnServerReceiveActivationAck, false);
            _serverHandlersRegistered = true;
        }

        if (!_clientHandlersRegistered)
        {
            NetworkClient.RegisterHandler<ScenePreloadMessage>(OnClientReceivePreloadMessage, false);
            NetworkClient.RegisterHandler<SceneActivationMessage>(OnClientReceiveActivationMessage, false);
            NetworkClient.RegisterHandler<LoadingProgressUpdateMessage>(OnClientReceiveLoadingProgress, false);
            _clientHandlersRegistered = true;
        }
    }

    private void OnDestroy()
    {
        if (singleton == this)
        {
            UnregisterHandlers();
            singleton = null;
        }
    }

    private void UnregisterHandlers()
    {
        if (_serverHandlersRegistered)
        {
            NetworkServer.UnregisterHandler<ScenePreloadAckMessage>();
            NetworkServer.UnregisterHandler<SceneActivationAckMessage>();
            _serverHandlersRegistered = false;
        }

        if (_clientHandlersRegistered)
        {
            NetworkClient.UnregisterHandler<ScenePreloadMessage>();
            NetworkClient.UnregisterHandler<SceneActivationMessage>();
            NetworkClient.UnregisterHandler<LoadingProgressUpdateMessage>();
            _clientHandlersRegistered = false;
        }
    }

    #endregion

    #region Helper Methods

    private int CountConnectedClients()
    {
        int count = 0;

        foreach (var kvp in NetworkServer.connections)
        {
            var conn = kvp.Value;
            if (conn != null && conn.isAuthenticated)
            {
                count++;
            }
        }

        var localConn = NetworkServer.localConnection;
        if (localConn != null && localConn.isAuthenticated)
        {
            bool alreadyCounted = NetworkServer.connections.ContainsKey(localConn.connectionId);
            if (!alreadyCounted)
            {
                count++;
            }
        }

        if (count == 0 && NetworkClient.active && NetworkServer.active)
        {
            // Host fallback: treat local client as participant even if not listed yet
            count = 1;
        }

        return count;
    }

    private void BroadcastToAll<T>(T message) where T : struct, NetworkMessage
    {
        if (!NetworkServer.active)
            return;

        NetworkServer.SendToAll(message);

        var localConn = NetworkServer.localConnection;
        if (localConn != null)
        {
            bool alreadyCounted = NetworkServer.connections.ContainsKey(localConn.connectionId);
            if (!alreadyCounted)
            {
                localConn.Send(message);
            }
        }
    }

    private void SendPreloadAck()
    {
        if (!NetworkClient.active || NetworkClient.connection == null || _preloadAckSent)
            return;

        NetworkClient.Send(new ScenePreloadAckMessage());
        _preloadAckSent = true;
    }

    private void SendActivationAck()
    {
        if (!NetworkClient.active || NetworkClient.connection == null || _activationAckSent)
            return;

        NetworkClient.Send(new SceneActivationAckMessage());
        _activationAckSent = true;
    }

    private string ResolveClientName(NetworkConnectionToClient sender)
    {
        if (sender == null)
            return "Unknown";

        try
        {
            var identity = sender.identity;
            if (identity != null)
            {
                try
                {
                    if (identity.TryGetComponent(out PlayerData pd) && pd != null)
                    {
                        return string.IsNullOrWhiteSpace(pd.alias) ? $"Connection {sender.connectionId}" : pd.alias;
                    }
                }
                catch (MissingReferenceException)
                {
                    // fall through to default name
                }
            }
        }
        catch (MissingReferenceException)
        {
            // ignore
        }

        return $"Connection {sender.connectionId}";
    }

    private ulong ResolveSteamId(NetworkConnectionToClient sender)
    {
        if (sender == null) return 0;
        try
        {
            var identity = sender.identity;
            if (identity != null && identity.TryGetComponent(out PlayerData pd) && pd != null)
            {
                return pd.playerInfo.steamId;
            }
        }
        catch (MissingReferenceException) { }
        return 0;
    }

    private void LogTelemetry(TelemetryEventType eventType, int connectionId, string playerName, float loadDuration = 0, string additionalInfo = "")
    {
        var telemetry = new SceneLoadTelemetry
        {
            eventType = eventType,
            connectionId = connectionId,
            playerName = playerName,
            sceneName = _targetSceneName,
            timestamp = Time.realtimeSinceStartup,
            loadDuration = loadDuration,
            additionalInfo = additionalInfo
        };
        
        _telemetryLog.Add(telemetry);
        Debug.Log(telemetry.ToString());
        OnTelemetryEvent?.Invoke(telemetry);
    }

    #endregion

    [Header("Configuration")]
    [SerializeField] private float preloadTimeout = 30f;
    [SerializeField] private float activationTimeout = 10f;
    [SerializeField] private bool forceActivateAfterTimeout = true;
    
    [Header("Timeout Behavior")]
    [Tooltip("What to do when a player times out during loading")]
    [SerializeField] private TimeoutBehavior timeoutBehavior = TimeoutBehavior.DisconnectAndContinue;
    
    public enum TimeoutBehavior
    {
        DisconnectAndContinue,  // Remove o player e continua com os demais
        CancelAndReturnToLobby  // Cancela a transição e volta ao lobby
    }

    // Server-side tracking
    private readonly HashSet<int> _preloadAcks = new HashSet<int>();
    private readonly HashSet<int> _activationAcks = new HashSet<int>();
    private string _targetSceneName;
    private bool _isTransitioning = false;
    private Coroutine _transitionCoroutine;
    private float _transitionStartTime;
    
    // Client-side state
    private AsyncOperation _preloadOperation;
    private bool _isPreloading = false;
    private bool _waitingForActivation = false;

    private static bool _serverHandlersRegistered = false;
    private static bool _clientHandlersRegistered = false;

    // Network Messages
    [Serializable]
    private struct ScenePreloadMessage : NetworkMessage
    {
        public string SceneName;
        public int ExpectedPlayers;
    }

    private struct SceneActivationMessage : NetworkMessage { }

    private struct ScenePreloadAckMessage : NetworkMessage { }

    private struct SceneActivationAckMessage : NetworkMessage { }
    
    /// <summary>
    /// Message sent from server to clients to update loading progress UI
    /// </summary>
    private struct LoadingProgressUpdateMessage : NetworkMessage 
    { 
        public int LoadedPlayers;
        public int TotalPlayers;
        public string StatusMessage;
    }

    private int expectedClients = 0;
    private int preloadedClients = 0;
    private int activatedClients = 0;
    private bool _preloadAckSent = false;
    private bool _activationAckSent = false;
    
    // Client-side loading progress display
    private int _clientLoadedPlayers = 0;
    private int _clientTotalPlayers = 0;
    private string _clientStatusMessage = "";

    #region Public API

    /// <summary>
    /// Server: Initiates a synchronized scene change.
    /// All clients will preload the scene before it activates.
    /// </summary>
    public void ServerChangeSceneSynchronized(string sceneName)
    {
        Debug.Log($"[SceneTransition] ServerChangeSceneSynchronized called with sceneName='{sceneName}'");
        
        if (!NetworkServer.active)
        {
            Debug.LogError("[SceneTransition] ServerChangeSceneSynchronized can only be called when the server is active");
            return;
        }

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
        _transitionStartTime = Time.realtimeSinceStartup;

        // Reset tracking
        _preloadAcks.Clear();
        _activationAcks.Clear();
        _playerLoadStates.Clear();
        _preloadAckSent = false;
        _activationAckSent = false;
        
        expectedClients = CountConnectedClients();
        if (expectedClients == 0)
        {
            Debug.LogWarning("[SceneTransition] No connected clients detected. Falling back to standard scene change.");
            _isTransitioning = false;
            NetworkManager.singleton.ServerChangeScene(sceneName);
            return;
        }
        
        preloadedClients = 0;
        activatedClients = 0;

        // Initialize player load states for all connected clients
        InitializePlayerLoadStates();

        Debug.Log($"[SceneTransition] Expecting {expectedClients} clients to preload");

        // Freeze all players during transition
        if (PlayerList.singleton != null)
        {
            PlayerList.singleton.SetAllPlayersFrozen(true);
            Debug.Log("[SceneTransition] Players frozen during transition");
        }

        // Tell all clients to preload via network message
        BroadcastToAll(new ScenePreloadMessage { SceneName = sceneName, ExpectedPlayers = expectedClients });
        
        // Send initial loading progress
        BroadcastLoadingProgress("Carregando...");

        // Start timeout coroutine
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(PreloadTimeoutCoroutine());
    }
    
    /// <summary>
    /// Returns the current telemetry log for debugging
    /// </summary>
    public IReadOnlyList<SceneLoadTelemetry> GetTelemetryLog() => _telemetryLog;
    
    /// <summary>
    /// Clears the telemetry log
    /// </summary>
    public void ClearTelemetryLog() => _telemetryLog.Clear();
    
    /// <summary>
    /// Gets current player load states (server only)
    /// </summary>
    public IReadOnlyDictionary<int, PlayerLoadState> GetPlayerLoadStates() => _playerLoadStates;
    
    /// <summary>
    /// Client: Gets current loading status for UI display
    /// </summary>
    public (int loaded, int total, string status) GetClientLoadingStatus()
    {
        return (_clientLoadedPlayers, _clientTotalPlayers, _clientStatusMessage);
    }

    #endregion

    #region Server Methods
    
    private void InitializePlayerLoadStates()
    {
        foreach (var kvp in NetworkServer.connections)
        {
            var conn = kvp.Value;
            if (conn == null || !conn.isAuthenticated) continue;
            
            var state = new PlayerLoadState
            {
                connectionId = conn.connectionId,
                playerName = ResolveClientName(conn),
                steamId = ResolveSteamId(conn),
                loadStartTime = Time.realtimeSinceStartup,
                hasPreloaded = false,
                hasActivated = false,
                timedOut = false,
                disconnected = false
            };
            _playerLoadStates[conn.connectionId] = state;
            
            // Log telemetry
            LogTelemetry(TelemetryEventType.SceneLoadingStarted, conn.connectionId, state.playerName);
        }
    }
    
    private void BroadcastLoadingProgress(string statusMessage = null)
    {
        if (!NetworkServer.active) return;
        
        int loaded = _preloadAcks.Count;
        int total = expectedClients;
        string message = statusMessage ?? $"Aguardando jogadores... ({loaded}/{total} prontos)";
        
        BroadcastToAll(new LoadingProgressUpdateMessage
        {
            LoadedPlayers = loaded,
            TotalPlayers = total,
            StatusMessage = message
        });
        
        OnLoadingProgressChanged?.Invoke(loaded, total);
    }

    private IEnumerator PreloadTimeoutCoroutine()
    {
        float startTime = Time.time;
        float lastLogTime = Time.time;
        float lastProgressBroadcast = Time.time;
        bool noResponseWarningShown = false;

        while (_preloadAcks.Count < expectedClients)
        {
            float elapsed = Time.time - startTime;
            
            // Handle disconnections during load
            CheckForDisconnections();
            
            // Update expected clients count (may have changed due to disconnections)
            int activeExpected = GetActiveExpectedClients();
            if (_preloadAcks.Count >= activeExpected && activeExpected > 0)
            {
                Debug.Log($"[SceneTransition] All remaining clients preloaded ({_preloadAcks.Count}/{activeExpected}).");
                break;
            }
            
            // If no clients responded after 3 seconds, warn
            if (!noResponseWarningShown && elapsed >= 3f && _preloadAcks.Count == 0)
            {
                Debug.LogWarning($"[SceneTransition] No clients responded after 3 seconds. This may indicate the message system isn't working properly.");
                noResponseWarningShown = true;
            }
            
            // Broadcast loading progress every 0.5 seconds
            if (Time.time - lastProgressBroadcast >= 0.5f)
            {
                BroadcastLoadingProgress();
                lastProgressBroadcast = Time.time;
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
                HandlePreloadTimeout();
                yield break;
            }

            yield return null;
        }

        Debug.Log($"[SceneTransition] All clients preloaded ({_preloadAcks.Count}/{expectedClients}). Activating scene...");
        OnAllPlayersLoaded?.Invoke();
        ServerActivateScene();
    }
    
    private void CheckForDisconnections()
    {
        var toRemove = new List<int>();
        
        foreach (var kvp in _playerLoadStates)
        {
            int connId = kvp.Key;
            var state = kvp.Value;
            
            if (state.disconnected) continue;
            
            // Check if connection still exists
            bool exists = false;
            foreach (var connKvp in NetworkServer.connections)
            {
                if (connKvp.Value != null && connKvp.Value.connectionId == connId && connKvp.Value.isAuthenticated)
                {
                    exists = true;
                    break;
                }
            }
            
            if (!exists)
            {
                state.disconnected = true;
                LogTelemetry(TelemetryEventType.PlayerDisconnectedDuringLoad, connId, state.playerName, 
                    additionalInfo: $"Disconnected after {Time.realtimeSinceStartup - state.loadStartTime:F2}s");
                
                // Remove from expected acks
                _preloadAcks.Remove(connId);
                toRemove.Add(connId);
            }
        }
        
        // Update expected count
        if (toRemove.Count > 0)
        {
            expectedClients = GetActiveExpectedClients();
            Debug.Log($"[SceneTransition] {toRemove.Count} player(s) disconnected. Now expecting {expectedClients} clients.");
        }
    }
    
    private int GetActiveExpectedClients()
    {
        int count = 0;
        foreach (var kvp in _playerLoadStates)
        {
            if (!kvp.Value.disconnected && !kvp.Value.timedOut)
                count++;
        }
        return Mathf.Max(count, 0);
    }
    
    private void HandlePreloadTimeout()
    {
        var timedOutPlayers = new List<string>();
        
        foreach (var kvp in _playerLoadStates)
        {
            var state = kvp.Value;
            if (!state.hasPreloaded && !state.disconnected && !state.timedOut)
            {
                state.timedOut = true;
                timedOutPlayers.Add(state.playerName);
                
                LogTelemetry(TelemetryEventType.SceneLoadTimeout, kvp.Key, state.playerName,
                    loadDuration: Time.realtimeSinceStartup - state.loadStartTime);
            }
        }
        
        Debug.LogWarning($"[SceneTransition] Timeout! Players that didn't load: {string.Join(", ", timedOutPlayers)}");
        
        if (timeoutBehavior == TimeoutBehavior.DisconnectAndContinue)
        {
            // Disconnect timed out players
            foreach (var kvp in _playerLoadStates)
            {
                if (kvp.Value.timedOut)
                {
                    var conn = GetConnectionById(kvp.Key);
                    if (conn != null)
                    {
                        Debug.Log($"[SceneTransition] Disconnecting timed out player: {kvp.Value.playerName}");
                        conn.Disconnect();
                    }
                }
            }
            
            // Update expected and continue if we have anyone left
            expectedClients = GetActiveExpectedClients();
            if (expectedClients > 0 || _preloadAcks.Count > 0)
            {
                Debug.Log($"[SceneTransition] Continuing with {_preloadAcks.Count} loaded clients.");
                ServerActivateScene();
            }
            else
            {
                Debug.LogError($"[SceneTransition] No clients remaining! Falling back to standard scene change.");
                _isTransitioning = false;
                NetworkManager.singleton.ServerChangeScene(_targetSceneName);
            }
        }
        else // CancelAndReturnToLobby
        {
            Debug.Log("[SceneTransition] Canceling transition and returning to lobby...");
            _isTransitioning = false;
            _transitionCoroutine = null;
            
            // Unfreeze players
            if (PlayerList.singleton != null)
            {
                PlayerList.singleton.SetAllPlayersFrozen(false);
            }
            
            // Notify clients to hide loading screen
            BroadcastToAll(new LoadingProgressUpdateMessage
            {
                LoadedPlayers = 0,
                TotalPlayers = 0,
                StatusMessage = "Carregamento cancelado. Retornando ao lobby..."
            });
            
            // Return to lobby scene (RASCUNHO)
            NetworkManager.singleton.ServerChangeScene("RASCUNHO");
        }
    }
    
    private NetworkConnectionToClient GetConnectionById(int connectionId)
    {
        foreach (var kvp in NetworkServer.connections)
        {
            if (kvp.Value != null && kvp.Value.connectionId == connectionId)
                return kvp.Value;
        }
        return null;
    }

    private void ServerActivateScene()
    {
        if (!NetworkServer.active)
            return;

        Debug.Log($"[SceneTransition] SERVER: Activating scene '{_targetSceneName}' for all clients");
        
        // Log telemetry
        foreach (var kvp in _playerLoadStates)
        {
            if (kvp.Value.hasPreloaded)
            {
                LogTelemetry(TelemetryEventType.SceneActivationStarted, kvp.Key, kvp.Value.playerName);
            }
        }
        
        // Tell clients to activate their preloaded scenes
        BroadcastToAll(new SceneActivationMessage());
        BroadcastLoadingProgress("Iniciando partida...");

        // Server also needs to load the scene
        StartCoroutine(ServerLoadSceneCoroutine());
    }

    private IEnumerator ServerLoadSceneCoroutine()
    {
        Debug.Log($"[SceneTransition] SERVER: Loading scene '{_targetSceneName}'");
        
        // Use Mirror's standard scene change mechanism for the server
        NetworkManager.singleton.ServerChangeScene(_targetSceneName);

        // Wait for activation acknowledgments from clients
        float startTime = Time.time;
        float lastLogTime = Time.time;

        int activeExpected = GetActiveExpectedClients();
        
        while (_activationAcks.Count < activeExpected)
        {
            if (Time.time - lastLogTime >= 1f)
            {
                LogActivationProgress();
                lastLogTime = Time.time;
            }

            if (Time.time - startTime >= activationTimeout)
            {
                Debug.LogWarning($"[SceneTransition] Activation timeout! Only {_activationAcks.Count}/{activeExpected} clients activated.");
                break;
            }

            yield return null;
        }

        Debug.Log($"[SceneTransition] All clients activated scene ({_activationAcks.Count}/{activeExpected})");
        ServerFinishTransition();
    }

    private void ServerFinishTransition()
    {
        float totalDuration = Time.realtimeSinceStartup - _transitionStartTime;
        Debug.Log($"[SceneTransition] SERVER: Scene transition complete in {totalDuration:F2}s. Starting briefing flow...");
        
        // Log final telemetry
        LogTelemetry(TelemetryEventType.MatchStarted, -1, "ALL", loadDuration: totalDuration,
            additionalInfo: $"Players loaded: {_preloadAcks.Count}/{expectedClients}");
        
        _isTransitioning = false;
        _transitionCoroutine = null;

        // The standard Mirror flow will handle the rest via OnServerSceneChanged
        // which triggers WaitAllConnectionsReadyThenStart -> BriefingManager.TriggerBriefing
    }

    private void LogPreloadProgress()
    {
        var connections = NetworkServer.connections.Values;
        List<string> status = new List<string>();
        
        foreach (var conn in connections)
        {
            if (conn == null) continue;
            
            bool ready = _preloadAcks.Contains(conn.connectionId);
            string name = ResolveClientName(conn);
            
            if (_playerLoadStates.TryGetValue(conn.connectionId, out var state))
            {
                float elapsed = Time.realtimeSinceStartup - state.loadStartTime;
                string stateStr = ready ? "✓" : $"⏳ ({elapsed:F1}s)";
                status.Add($"{name}: {stateStr}");
            }
            else
            {
                status.Add($"{name}: {(ready ? "✓" : "⏳")}");
            }
        }

        Debug.Log($"[SceneTransition] Preload Progress ({_preloadAcks.Count}/{GetActiveExpectedClients()}):\n  " + string.Join("\n  ", status));
    }

    private void LogActivationProgress()
    {
        var connections = NetworkServer.connections.Values;
        List<string> status = new List<string>();
        
        foreach (var conn in connections)
        {
            if (conn == null) continue;
            
            bool activated = _activationAcks.Contains(conn.connectionId);
            string name = ResolveClientName(conn);
            status.Add($"{name}: {(activated ? "✓" : "⏳")}");
        }

        Debug.Log($"[SceneTransition] Activation Progress ({_activationAcks.Count}/{GetActiveExpectedClients()}):\n  " + string.Join("\n  ", status));
    }

    #endregion

    #region Client Message Handlers

    private void OnClientReceivePreloadMessage(ScenePreloadMessage message)
    {
        if (NetworkServer.active && !NetworkClient.active)
        {
            Debug.Log("[SceneTransition] Dedicated server - skipping client preload");
            return;
        }

        Debug.Log($"[SceneTransition] CLIENT: Starting preload of '{message.SceneName}' (IsHost: {NetworkServer.active && NetworkClient.active})");
        _isPreloading = true;
        _waitingForActivation = true;
        _preloadAckSent = false;
        _activationAckSent = false;
        _clientTotalPlayers = message.ExpectedPlayers;
        _clientLoadedPlayers = 0;

        // Show loading screen
        LoadingScreenUI.Ensure();
        LoadingScreenUI.Instance?.SetMirrorTargetScene(message.SceneName);
        LoadingScreenUI.Instance?.ShowForMirror();

        // Start preload
        StartCoroutine(ClientPreloadSceneCoroutine(message.SceneName));
    }

    private void OnClientReceiveActivationMessage(SceneActivationMessage _)
    {
        if (NetworkServer.active && !NetworkClient.active)
        {
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
    
    private void OnClientReceiveLoadingProgress(LoadingProgressUpdateMessage message)
    {
        _clientLoadedPlayers = message.LoadedPlayers;
        _clientTotalPlayers = message.TotalPlayers;
        _clientStatusMessage = message.StatusMessage;
        
        // Update loading screen with player progress
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.SetPlayerProgress(message.LoadedPlayers, message.TotalPlayers, message.StatusMessage);
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
            // Verifica se ainda estamos conectados
            if (!NetworkClient.active || !NetworkClient.isConnected)
            {
                Debug.LogWarning("[SceneTransition] CLIENT: Lost connection during preload, aborting");
                HandleClientDisconnectedDuringLoad();
                yield break;
            }
            
            // Update loading UI
            if (LoadingScreenUI.Instance != null)
            {
                LoadingScreenUI.Instance.SetProgress(_preloadOperation.progress);
            }
            yield return null;
        }

        Debug.Log($"[SceneTransition] CLIENT: Preload complete for '{sceneName}' (progress: {_preloadOperation.progress})");
        
        // CRITICAL: Only send ACK AFTER scene is fully preloaded
        // This ensures the server knows the client has truly finished loading
        SendPreloadAck();

        // Wait for server to signal activation
        Debug.Log("[SceneTransition] CLIENT: Waiting for server activation signal...");
        
        // Update UI to show waiting for others
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.SetProgress(1f);
        }
        
        // Timeout de segurança para evitar loop infinito caso o host desconecte
        float waitStartTime = Time.realtimeSinceStartup;
        float maxWaitForActivation = 60f; // 60 segundos de timeout
        
        while (!_preloadOperation.allowSceneActivation)
        {
            // Verifica se ainda estamos conectados
            if (!NetworkClient.active || !NetworkClient.isConnected)
            {
                Debug.LogWarning("[SceneTransition] CLIENT: Lost connection while waiting for activation signal");
                HandleClientDisconnectedDuringLoad();
                yield break;
            }
            
            // Verifica timeout
            if (Time.realtimeSinceStartup - waitStartTime > maxWaitForActivation)
            {
                Debug.LogWarning("[SceneTransition] CLIENT: Timeout waiting for activation signal");
                HandleClientDisconnectedDuringLoad();
                yield break;
            }
            
            yield return null;
        }

        // Wait for actual scene activation
        while (!_preloadOperation.isDone)
        {
            // Verifica se ainda estamos conectados
            if (!NetworkClient.active || !NetworkClient.isConnected)
            {
                Debug.LogWarning("[SceneTransition] CLIENT: Lost connection during scene activation");
                HandleClientDisconnectedDuringLoad();
                yield break;
            }
            
            yield return null;
        }

        Debug.Log($"[SceneTransition] CLIENT: Scene '{sceneName}' activated");
        _isPreloading = false;
        _waitingForActivation = false;

        // CRITICAL: Send activation ACK only after scene is fully activated and ready
        // Add a small delay to ensure all scene objects are initialized
        yield return new WaitForEndOfFrame();
        yield return null; // Extra frame for safety
        
        SendActivationAck();

        Debug.Log("[SceneTransition] CLIENT: Scene ready, waiting for briefing to hide loading screen");
    }
    
    /// <summary>
    /// Trata a situação em que o cliente é desconectado durante o carregamento.
    /// Limpa o estado e esconde a tela de loading.
    /// </summary>
    private void HandleClientDisconnectedDuringLoad()
    {
        Debug.Log("[SceneTransition] HandleClientDisconnectedDuringLoad - cleaning up");
        
        // Limpa o estado do cliente
        _isPreloading = false;
        _waitingForActivation = false;
        _preloadAckSent = false;
        _activationAckSent = false;
        
        // Se ainda temos uma operação de preload, permite que ela termine
        if (_preloadOperation != null && !_preloadOperation.isDone)
        {
            _preloadOperation.allowSceneActivation = true;
        }
        _preloadOperation = null;
        
        // Esconde a tela de loading
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.Hide();
        }
    }

    #endregion

    #region Server Handlers for Client Messages

    private void OnServerReceivePreloadAck(NetworkConnectionToClient sender, ScenePreloadAckMessage _)
    {
        if (!_isTransitioning || sender == null)
            return;

        if (_preloadAcks.Add(sender.connectionId))
        {
            preloadedClients = _preloadAcks.Count;

            string clientName = ResolveClientName(sender);
            
            // Update player state
            if (_playerLoadStates.TryGetValue(sender.connectionId, out var state))
            {
                state.hasPreloaded = true;
                state.loadEndTime = Time.realtimeSinceStartup;
                
                LogTelemetry(TelemetryEventType.SceneLoadACKReceived, sender.connectionId, clientName,
                    loadDuration: state.LoadDuration);
            }

            Debug.Log($"[SceneTransition] SERVER: Client '{clientName}' preloaded ({preloadedClients}/{GetActiveExpectedClients()})");
            
            // Broadcast updated progress to all clients
            BroadcastLoadingProgress();

            int activeExpected = GetActiveExpectedClients();
            if (_preloadAcks.Count >= activeExpected && activeExpected > 0)
            {
                if (_transitionCoroutine != null)
                {
                    StopCoroutine(_transitionCoroutine);
                    _transitionCoroutine = null;
                }
                OnAllPlayersLoaded?.Invoke();
                ServerActivateScene();
            }
        }
    }

    private void OnServerReceiveActivationAck(NetworkConnectionToClient sender, SceneActivationAckMessage _)
    {
        if (!_isTransitioning || sender == null)
            return;

        if (_activationAcks.Add(sender.connectionId))
        {
            activatedClients = _activationAcks.Count;

            string clientName = ResolveClientName(sender);
            
            // Update player state
            if (_playerLoadStates.TryGetValue(sender.connectionId, out var state))
            {
                state.hasActivated = true;
                
                LogTelemetry(TelemetryEventType.SceneActivationACKReceived, sender.connectionId, clientName);
            }

            Debug.Log($"[SceneTransition] SERVER: Client '{clientName}' activated scene ({activatedClients}/{GetActiveExpectedClients()})");
        }
    }

    #endregion

    #region Public Utility

    public bool IsTransitioning => _isTransitioning;
    
    /// <summary>
    /// Gets the current loading status as a formatted string for debugging
    /// </summary>
    public string GetLoadingStatusDebug()
    {
        if (!_isTransitioning)
            return "Not transitioning";
            
        var lines = new List<string>();
        lines.Add($"Scene: {_targetSceneName}");
        lines.Add($"Progress: {_preloadAcks.Count}/{GetActiveExpectedClients()} preloaded, {_activationAcks.Count} activated");
        lines.Add($"Time elapsed: {Time.realtimeSinceStartup - _transitionStartTime:F1}s");
        
        foreach (var kvp in _playerLoadStates)
        {
            var state = kvp.Value;
            string status = state.disconnected ? "DISCONNECTED" : 
                           state.timedOut ? "TIMED OUT" :
                           state.hasActivated ? "ACTIVATED" :
                           state.hasPreloaded ? "PRELOADED" : "LOADING";
            lines.Add($"  {state.playerName}: {status} ({state.LoadDuration:F1}s)");
        }
        
        return string.Join("\n", lines);
    }
    
    /// <summary>
    /// Prints the full telemetry log to the console
    /// </summary>
    public void PrintTelemetryLog()
    {
        Debug.Log("=== SCENE TRANSITION TELEMETRY LOG ===");
        foreach (var entry in _telemetryLog)
        {
            Debug.Log(entry.ToString());
        }
        Debug.Log("=== END TELEMETRY LOG ===");
    }
    
    /// <summary>
    /// Limpa o estado do cliente quando desconecta.
    /// Chamado pelo MyNetworkManager.OnStopClient para evitar que o cliente fique preso na tela de loading.
    /// </summary>
    public void CleanupClientState()
    {
        Debug.Log("[SceneTransition] CleanupClientState called - resetting client transition state");
        
        // Cancela qualquer coroutine de preload em andamento
        if (_preloadOperation != null)
        {
            // Se a operação de preload ainda está em andamento, tenta cancelar
            try
            {
                if (!_preloadOperation.isDone)
                {
                    _preloadOperation.allowSceneActivation = true; // Permite que termine para evitar estado inconsistente
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SceneTransition] Error cleaning up preload operation: {e.Message}");
            }
            _preloadOperation = null;
        }
        
        // Para todas as coroutines deste componente
        StopAllCoroutines();
        
        // Reseta estados de cliente
        _isPreloading = false;
        _waitingForActivation = false;
        _preloadAckSent = false;
        _activationAckSent = false;
        _clientLoadedPlayers = 0;
        _clientTotalPlayers = 0;
        _clientStatusMessage = "";
        
        // Se estamos no servidor (host), também limpa o estado do servidor
        if (NetworkServer.active)
        {
            _isTransitioning = false;
            _transitionCoroutine = null;
            _preloadAcks.Clear();
            _activationAcks.Clear();
            _playerLoadStates.Clear();
            _targetSceneName = null;
            expectedClients = 0;
            preloadedClients = 0;
            activatedClients = 0;
            
            // Descongela os jogadores se ainda estiverem congelados
            if (PlayerList.singleton != null)
            {
                PlayerList.singleton.SetAllPlayersFrozen(false);
            }
        }
        
        Debug.Log("[SceneTransition] Client state cleaned up successfully");
    }

    #endregion
}
