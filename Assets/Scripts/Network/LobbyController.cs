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
    [SerializeField] Database db;
    [SerializeField] HUDSO HUDSO;

    
    [SerializeField]
    private List<string> minigameSceneNames = new List<string>();


    private void Start()
    {
        _prepareTimer = -1;

    }

    private void Update()
    {

        if (_prepareTimer > 0)
            _prepareTimer -= Time.deltaTime;

        if (_prepareTimer <= 0 && _prepareTimer != -1){
            
            ChangeToRandomMinigame();
            
            _prepareTimer = -1;
        }
    }

    public void StartGameWithParty() 
    {
        if (AllPlayersReady()) 
        {
            
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPrepareMath() {
        
        if(_prepareTimer > 0) return;
        
        
        InternalPrepareMath();
    }
    [Server]
    void InternalPrepareMath() 
    {
        _prepareTimer = db.serverPrepareDuration;

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
        NetworkManager.singleton.ServerChangeScene(MyNetworkManager.manager.minigames[MyNetworkManager.manager.indexScene]);
        MyNetworkManager.manager.indexScene++;
        //MyNetworkManager.manager.ChangeScenePlayer(PlayerList.singleton.players[0],sceneToLoad);
    }
    
    private bool AllPlayersReady() 
    {
        foreach (PlayerData client in ((MyNetworkManager)NetworkManager.singleton).allClients)
            if (!client.IsReady)
                return false;
        return true;
    }
}
