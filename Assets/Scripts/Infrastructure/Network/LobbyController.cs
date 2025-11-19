using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : NetworkBehaviour
{
    #region Singleton Setup

    public static LobbyController singleton;

    private void Awake()
    {
        singleton = this;
    }

    #endregion

    [SyncVar(hook = nameof(HookOnPrepareTimerUpdated))] float _prepareTimer;
    [SyncVar(hook = nameof(HookOnStartTimerUpdated))] float _startTimer;
    [SyncVar(hook = nameof(HookOnVotingTimerUpdated))] float _votingTimer;
    [SerializeField] Database db;
    [SerializeField] HUDSO HUDSO;

    [Header("Voting System")]
    [SerializeField] private bool enableVoting = true;
    [SerializeField] private float votingDuration = 10f;
    [SerializeField] private MinigameCatalog minigameCatalog;

    private bool _votingInProgress = false;
    private MinigameCatalog.MinigameEntry _votingWinner = null;

    // TODO:
    //Melhor chamar quando a pessoa da pronto, arrumar para depos 
    private bool startgame = true;
    private void Start()
    {
        _prepareTimer = -1;
        _startTimer = -1;
        _votingTimer = -1;
        
        // Start checking for ready players periodically
        if (isServer)
        {
            InvokeRepeating(nameof(CheckPlayersReady), 0.5f, 1.0f);
        }
    }

    private void OnDestroy()
    {
        if (isServer)
        {
            CancelInvoke(nameof(CheckPlayersReady));
        }
    }

    [Server]
    private void CheckPlayersReady()
    {
        if (SceneTransitionManager.singleton != null && SceneTransitionManager.singleton.IsTransitioning)
        {
            return;
        }

        // If game already started and we're back in lobby, continue the flow
        if (MyNetworkManager.manager.startGame)
        {
            // Check if we should start a new round (no timers running)
            if (_prepareTimer <= 0 && _startTimer <= 0 && !_votingInProgress)
            {
                Debug.Log("🔄 [LOBBY] Back from minigame, starting new round");
                CmdPrepareMath();
            }
            return;
        }
        
        // Initial game start - wait for all players to ready up
        if (MyNetworkManager.manager.AllPlayersReady())
        {
            Debug.Log("✅ [LOBBY] All players ready, starting game!");
            CmdPrepareMath();
            MyNetworkManager.manager.startGame = true;
        }
    }

    private void Update()
    {
        if (!isServer) return; // server-authoritative update
        
        if (_prepareTimer > 0)
            _prepareTimer -= Time.deltaTime;
            
        if(_startTimer > 0)
            _startTimer -= Time.deltaTime;
        
        // Voting timer
        if (_votingTimer > 0)
        {
            _votingTimer -= Time.deltaTime;
        }
        
        if (_votingTimer <= 0 && _votingTimer != -1)
        {
            // Voting time is up, end voting and transition
            EndVotingAndTransition();
            _votingTimer = -1;
        }
        
        if (_startTimer <= 0 && _startTimer != -1){
            _startTimer = -1;
            
            // Delay voting start to allow "GO!" animation to complete
            if (enableVoting)
            {
                StartCoroutine(DelayedVotingStart());
            }
            else
            {
                ChangeToRandomMinigame();
            }
        }
        if (_prepareTimer <= 0 && _prepareTimer != -1){
            _prepareTimer = -1;
            
            // Delay voting start to allow "GO!" animation to complete
            if (enableVoting)
            {
                StartCoroutine(DelayedVotingStart());
            }
            else
            {
                ChangeToRandomMinigame();
            }
        }
    }

    /// <summary>
    /// Legacy method - kept for compatibility. The ready check is now done automatically via CheckPlayersReady.
    /// </summary>
    public void StartGameWithParty() 
    {
        if (!isServer) return;
        
        Debug.Log($"🎮 [LOBBY] StartGameWithParty called - AllPlayersReady? {MyNetworkManager.manager.AllPlayersReady()}");
        
        if(MyNetworkManager.manager.startGame){
            Debug.Log("🎮 [LOBBY] Game already started, forcing via CmdStartMath()");
            CmdStartMath();
            return;
        }
        
        if (MyNetworkManager.manager.AllPlayersReady()){
            Debug.Log("✅ [LOBBY] All players ready, starting game");
            CmdPrepareMath();
            MyNetworkManager.manager.startGame = true;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPrepareMath() {
        
        if(_prepareTimer > 0) return;
        if(_startTimer > 0) return;
        
        InternalPrepareMath();
    }
    [Command(requiresAuthority = false)]
    public void CmdStartMath() {
        
        if(_startTimer > 0) return;
        
        InternalStartGame();
    }
    [Server]
    void InternalPrepareMath() 
    {
        _prepareTimer = db.serverPrepareDuration;

    }
    [Server]
    void InternalStartGame()
    {
        _startTimer = db.serverStartMatchDuration;
    }
    
    
    public void StartGameSolo()
    {
        StartCoroutine(StartSinglePlayer());
    }
    IEnumerator StartSinglePlayer() 
    {
        NetworkManager.singleton.StartHost();

        while(NetworkClient.localPlayer == null)
            yield return new WaitForEndOfFrame();

        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(false);
    }

    void HookOnPrepareTimerUpdated(float oldValue, float newValue)
    {
        HUDSO.PrepareTimerUpdate(newValue);
    }

    void HookOnStartTimerUpdated(float oldValue, float newValue)
    {
        // Only show countdown when timer is 5 seconds or less
        if (newValue <= 5f && newValue > 0)
        {
            HUDSO.PrepareTimerUpdate(newValue);
        }
        else if (newValue == -1)
        {
            // Clear display
            HUDSO.PrepareTimerUpdate(-1);
        }
        // For values > 5, don't update HUD (silent countdown)
    }

    void HookOnVotingTimerUpdated(float oldValue, float newValue)
    {
        // Update UI with voting timer
        if (HUDSO != null)
        {
            HUDSO.VotingTimerUpdate(newValue);
        }

        // Lock/unlock cursor based on voting state
        if (newValue > 0)
        {
            // Voting is active - unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (oldValue > 0 && newValue <= 0)
        {
            // Voting just ended - lock cursor again
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    [Server]
    IEnumerator DelayedVotingStart()
    {
        // Wait for "GO!" animation to complete (approximately 1 second)
        yield return new WaitForSeconds(1.0f);
        
        Debug.Log("🗳️ [LOBBY] Starting voting after countdown complete");
        StartVotingPhase();
    }

    [Server]
    void StartVotingPhase()
    {
        if (_votingInProgress)
            return;

        Debug.Log("🗳️ [LOBBY] Starting voting phase");

        // Initialize VotingManager and MinigameRotationState if needed
        EnsureVotingSystemInitialized();

        // Check if all active minigames have been played - if so, load victory scene
        if (MinigameRotationState.Instance != null)
        {
            var eligible = MinigameRotationState.Instance.GetEligibleMinigames();
            int playedCount = MinigameRotationState.Instance.PlayedCount;
            
            Debug.Log($"🎮 [LOBBY] Minigames status before voting - Played: {playedCount}, Eligible remaining: {eligible.Count}");
            
            if (eligible.Count == 0)
            {
                Debug.Log("🏆 [LOBBY] All active minigames have been played! Loading victory scene");
                var manager = MyNetworkManager.manager;
                if (manager != null && minigameCatalog != null)
                {
                    string victoryScene = minigameCatalog.VictorySceneIdentifier;
                    if (!string.IsNullOrWhiteSpace(victoryScene))
                    {
                        manager.ServerChangeSceneSynchronized(victoryScene);
                        return;
                    }
                    else
                    {
                        Debug.LogError("🏆 [LOBBY] Victory scene identifier is null or empty!");
                    }
                }
                else
                {
                    Debug.LogError($"🏆 [LOBBY] Manager or catalog is null! Manager: {manager != null}, Catalog: {minigameCatalog != null}");
                }
                
                Debug.LogWarning("🏆 [LOBBY] Victory scene not configured, resetting rotation");
                MinigameRotationState.Instance.Reset();
            }
        }

        // Check if there's already a scene transition in progress
        if (SceneTransitionManager.singleton != null && SceneTransitionManager.singleton.IsTransitioning)
        {
            Debug.LogWarning("🗳️ [LOBBY] Cannot start voting - scene transition already in progress!");
            return;
        }

        // Set voting duration and start voting
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.SetVotingDuration(votingDuration);
            
            if (VotingManager.Instance.StartVotingRound())
            {
                _votingInProgress = true;
                _votingTimer = votingDuration;
                Debug.Log($"🗳️ [LOBBY] Voting started for {votingDuration} seconds");
            }
            else
            {
                Debug.LogError("🗳️ [LOBBY] Failed to start voting, falling back to random selection");
                ChangeToRandomMinigame();
            }
        }
        else
        {
            Debug.LogError("🗳️ [LOBBY] VotingManager not found, falling back to random selection");
            ChangeToRandomMinigame();
        }
    }

    [Server]
    void EndVotingAndTransition()
    {
        Debug.Log("🗳️ [LOBBY] EndVotingAndTransition called");
        
        if (!_votingInProgress)
        {
            Debug.LogWarning("🗳️ [LOBBY] Voting not in progress, ignoring");
            return;
        }

        Debug.Log("🗳️ [LOBBY] Ending voting and transitioning to winner");

        if (VotingManager.Instance != null)
        {
            _votingWinner = VotingManager.Instance.EndVoting();

            if (_votingWinner != null)
            {
                // Mark as played - the victory check will happen when we return to lobby
                if (MinigameRotationState.Instance != null)
                {
                    MinigameRotationState.Instance.MarkAsPlayed(_votingWinner.id);
                }

                // Load the winning scene (always play the minigame first)
                Debug.Log($"🏆 [LOBBY] Loading winner scene: {_votingWinner.displayName} ({_votingWinner.SceneIdentifier})");
                Debug.Log($"🏆 [LOBBY] Calling MyNetworkManager.manager.ServerChangeSceneSynchronized...");
                
                if (MyNetworkManager.manager == null)
                {
                    Debug.LogError("🗳️ [LOBBY] MyNetworkManager.manager is NULL!");
                    return;
                }
                
                MyNetworkManager.manager.ServerChangeSceneSynchronized(_votingWinner.SceneIdentifier);
                Debug.Log($"🏆 [LOBBY] ServerChangeSceneSynchronized called successfully");
            }
            else
            {
                Debug.LogError("🗳️ [LOBBY] Voting winner is null, falling back to random selection");
                ChangeToRandomMinigame();
            }
        }
        else
        {
            Debug.LogError("🗳️ [LOBBY] VotingManager not found, falling back to random selection");
            ChangeToRandomMinigame();
        }

        _votingInProgress = false;
        _votingWinner = null;
    }

    [Server]
    void EnsureVotingSystemInitialized()
    {
        // Ensure MinigameRotationState exists
        if (MinigameRotationState.Instance == null)
        {
            var go = new GameObject("MinigameRotationState");
            go.AddComponent<MinigameRotationState>();
        }

        // Set catalog reference
        if (minigameCatalog != null)
        {
            if (MinigameRotationState.Instance != null)
            {
                MinigameRotationState.Instance.SetCatalog(minigameCatalog);
            }

            if (VotingManager.Instance != null)
            {
                VotingManager.Instance.SetCatalog(minigameCatalog);
            }
        }
        else
        {
            // Try to get catalog from NetworkManager
            var manager = MyNetworkManager.manager;
            if (manager != null)
            {
                var catalogField = manager.GetType().GetField("minigameCatalog",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                
                if (catalogField != null)
                {
                    minigameCatalog = catalogField.GetValue(manager) as MinigameCatalog;
                    
                    if (minigameCatalog != null)
                    {
                        if (MinigameRotationState.Instance != null)
                            MinigameRotationState.Instance.SetCatalog(minigameCatalog);
                        
                        if (VotingManager.Instance != null)
                            VotingManager.Instance.SetCatalog(minigameCatalog);
                    }
                }
            }
        }

        // Ensure VotingManager exists
        if (VotingManager.Instance == null)
        {
            var go = new GameObject("VotingManager");
            go.AddComponent<VotingManager>();
            NetworkServer.Spawn(go);
        }
    }

    void ChangeToRandomMinigame()
    {
        if (!isServer) return; // only server may change scenes
        
        // Check if there's already a scene transition in progress
        if (SceneTransitionManager.singleton != null && SceneTransitionManager.singleton.IsTransitioning)
        {
            Debug.LogWarning("🎮 [LOBBY] Cannot change to random minigame - scene transition already in progress!");
            return;
        }
        
        var manager = MyNetworkManager.manager;
        if (manager == null)
            return;

        if (!manager.TryGetSceneNameAt(manager.indexScene, out var sceneName))
        {
            Debug.LogWarning("🎮 [LOBBY] Nenhuma cena encontrada para o índice atual da rotação.");
            return;
        }

        // Use synchronized scene change for better multiplayer experience
        manager.ServerChangeSceneSynchronized(sceneName);

        manager.AdvanceScenePointer();
        //MyNetworkManager.manager.ChangeScenePlayer(PlayerList.singleton.players[0],sceneToLoad);
    }
    
}
