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
    [SerializeField] Database db;
    [SerializeField] HUDSO HUDSO;

    
    [SerializeField]
    private List<string> minigameSceneNames = new List<string>();

    // TODO:
    //Melhor chamar quando a pessoa da pronto, arrumar para depos 
    private bool startgame = true;
    private void Start()
    {
        _prepareTimer = -1;
        _startTimer = -1;
        Invoke("StartGameWithParty", 0.5f );
    }

    private void Update()
    {
        if (!isServer) return; // server-authoritative update
        
        if (_prepareTimer > 0)
            _prepareTimer -= Time.deltaTime;
        if(_startTimer > 0)
            _startTimer -= Time.deltaTime;
        if (_startTimer <= 0 && _startTimer != -1){
            
            ChangeToRandomMinigame();
            
            _startTimer = -1;
        }
        if (_prepareTimer <= 0 && _prepareTimer != -1){
            
            ChangeToRandomMinigame();
            
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

    void ChangeToRandomMinigame()
    {
        if (!isServer) return; // only server may change scenes
        NetworkManager.singleton.ServerChangeScene(MyNetworkManager.manager.minigames[MyNetworkManager.manager.indexScene]);
        MyNetworkManager.manager.indexScene++;
        //MyNetworkManager.manager.ChangeScenePlayer(PlayerList.singleton.players[0],sceneToLoad);
    }
    
}
