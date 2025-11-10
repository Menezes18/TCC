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
    [SyncVar(hook = nameof(HookOnPrepareTimerUpdated))] float _startTimer;
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
        Invoke("StartGameWithParty", 0.5f );
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
            
            if (enableVoting)
            {
                StartVotingPhase();
            }
            else
            {
                ChangeToRandomMinigame();
            }
            
            _startTimer = -1;
        }
        if (_prepareTimer <= 0 && _prepareTimer != -1){
            
            if (enableVoting)
            {
                StartVotingPhase();
            }
            else
            {
                ChangeToRandomMinigame();
            }
            
            _prepareTimer = -1;
        }
    }

    public void StartGameWithParty() 
    {
        Debug.Log($"🎮 [LOBBY] AllPlayersReady? {MyNetworkManager.manager.AllPlayersReady()}");
        if(MyNetworkManager.manager.startGame){
            Debug.Log("🎮 [LOBBY] Forçando início via CmdStartMath()");
            CmdStartMath();
            return;
        }
        if (MyNetworkManager.manager.AllPlayersReady()){
            
            CmdPrepareMath();
            MyNetworkManager.manager.startGame = true;
            Debug.Log("✅ [LOBBY] Game started");
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
                        NetworkManager.singleton.ServerChangeScene(victoryScene);
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

        // Start voting
        if (VotingManager.Instance != null && VotingManager.Instance.StartVotingRound())
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

    [Server]
    void EndVotingAndTransition()
    {
        if (!_votingInProgress)
            return;

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
                NetworkManager.singleton.ServerChangeScene(_votingWinner.SceneIdentifier);
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
        var manager = MyNetworkManager.manager;
        if (manager == null)
            return;

        if (!manager.TryGetSceneNameAt(manager.indexScene, out var sceneName))
        {
            Debug.LogWarning("🎮 [LOBBY] Nenhuma cena encontrada para o índice atual da rotação.");
            return;
        }

        NetworkManager.singleton.ServerChangeScene(sceneName);

        manager.AdvanceScenePointer();
        //MyNetworkManager.manager.ChangeScenePlayer(PlayerList.singleton.players[0],sceneToLoad);
    }
    
}
