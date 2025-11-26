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

    private static bool _serverHandlersRegistered = false;
    private static bool _clientHandlersRegistered = false;

    [Serializable]
    private struct ScenePreloadMessage : NetworkMessage
    {
        public string SceneName;
    }

    private struct SceneActivationMessage : NetworkMessage { }

    private struct ScenePreloadAckMessage : NetworkMessage { }

    private struct SceneActivationAckMessage : NetworkMessage { }

    private int expectedClients = 0;
    private int preloadedClients = 0;
    private int activatedClients = 0;
    private bool _preloadAckSent = false;
    private bool _activationAckSent = false;

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

        // Reset tracking
        _preloadAcks.Clear();
        _activationAcks.Clear();
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

        Debug.Log($"[SceneTransition] Expecting {expectedClients} clients to preload");

        // Freeze all players during transition
        if (PlayerList.singleton != null)
        {
            PlayerList.singleton.AtivarPlayer(true);
            Debug.Log("[SceneTransition] Players frozen during transition");
        }

        // Tell all clients to preload via network message
        BroadcastToAll(new ScenePreloadMessage { SceneName = sceneName });

        // Start timeout coroutine with shorter timeout
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(PreloadTimeoutCoroutine());
    }

    #endregion

    #region Server Methods

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
                    Debug.LogError($"[SceneTransition] TIMEOUT with ZERO responses! Forcing scene activation anyway.");
                    // Em vez de usar ServerChangeScene (que pode causar dessincronização),
                    // forçamos a ativação do preload mesmo sem confirmações
                    break;
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

    private void ServerActivateScene()
    {
        if (!NetworkServer.active)
            return;

        Debug.Log($"[SceneTransition] SERVER: Activating scene '{_targetSceneName}' for all clients");
        
        // Tell clients to activate their preloaded scenes
        BroadcastToAll(new SceneActivationMessage());

        // Server loads the scene using the synchronized approach (not Mirror's ServerChangeScene)
        StartCoroutine(ServerLoadSceneCoroutine());
    }

    private IEnumerator ServerLoadSceneCoroutine()
    {
        Debug.Log($"[SceneTransition] SERVER: Loading scene '{_targetSceneName}' via synchronized approach");
        
        // IMPORTANTE: NÃO usar NetworkManager.singleton.ServerChangeScene() aqui!
        // Isso causaria uma segunda transição de cena que conflita com o preload dos clientes.
        
        // Carrega a cena no servidor de forma assíncrona
        AsyncOperation serverLoad = SceneManager.LoadSceneAsync(_targetSceneName);
        if (serverLoad == null)
        {
            Debug.LogError($"[SceneTransition] SERVER: Failed to load scene '{_targetSceneName}'");
            _isTransitioning = false;
            yield break;
        }

        // Aguarda o servidor carregar a cena
        while (!serverLoad.isDone)
        {
            yield return null;
        }

        Debug.Log($"[SceneTransition] SERVER: Scene '{_targetSceneName}' loaded on server");

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
        
        // Notifica o Mirror que a cena mudou (para atualizar networkSceneName internamente)
        if (NetworkManager.singleton != null)
        {
            // Atualiza a referência de cena do Mirror sem disparar outra transição
            typeof(NetworkManager).GetField("networkSceneName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(NetworkManager.singleton, _targetSceneName);
        }
        
        ServerFinishTransition();
    }

    private void ServerFinishTransition()
    {
        Debug.Log("[SceneTransition] SERVER: Scene transition complete. Starting briefing flow...");
        _isTransitioning = false;
        _transitionCoroutine = null;

        // Trigger the briefing flow directly since we bypassed Mirror's OnServerSceneChanged
        StartCoroutine(WaitForSceneAndTriggerBriefing());
    }

    private IEnumerator WaitForSceneAndTriggerBriefing()
    {
        // Aguarda um frame para garantir que a cena está totalmente carregada
        yield return null;
        yield return new WaitForEndOfFrame();

        // Verifica se todos os clientes estão prontos
        float timeout = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            bool allReady = true;
            foreach (var kvp in NetworkServer.connections)
            {
                var conn = kvp.Value;
                if (conn == null) continue;
                if (!conn.isAuthenticated || !conn.isReady)
                {
                    allReady = false;
                    break;
                }
            }
            
            if (allReady)
            {
                Debug.Log("[SceneTransition] All connections ready after scene load");
                break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Inicia o briefing
        if (BriefingManager.singleton != null && NetworkServer.active)
        {
            Debug.Log("[SceneTransition] Triggering briefing after synchronized scene load");
            BriefingManager.singleton.TriggerBriefing();
        }
        else
        {
            Debug.LogWarning("[SceneTransition] BriefingManager not found or server not active");
        }
    }

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
        SendPreloadAck();

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
        SendActivationAck();

        // Hide loading screen (will be shown again by briefing if needed)
        LoadingScreenUI.Instance?.Hide();
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

            Debug.Log($"[SceneTransition] SERVER: Client '{clientName}' preloaded ({preloadedClients}/{expectedClients})");

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

    private void OnServerReceiveActivationAck(NetworkConnectionToClient sender, SceneActivationAckMessage _)
    {
        if (!_isTransitioning || sender == null)
            return;

        if (_activationAcks.Add(sender.connectionId))
        {
            activatedClients = _activationAcks.Count;

            string clientName = ResolveClientName(sender);

            Debug.Log($"[SceneTransition] SERVER: Client '{clientName}' activated scene ({activatedClients}/{expectedClients})");
        }
    }

    #endregion

    #region Utility

    public bool IsTransitioning => _isTransitioning;

    #endregion
}
