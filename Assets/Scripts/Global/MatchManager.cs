using System;
using Mirror;
using UnityEngine;

public enum MatchStatus{
    
    Awaiting,
    Lobby,
    Ongoing,
    
}

public class MathManager : NetworkBehaviour{

    [SerializeField] Database db;
    [SerializeField] HUDSO HUDSO;
    private MatchStatus _status;

    [SyncVar] float _prepareTimer;
    [SyncVar] float _matchTimer;
    
    
    public SyncList<int> ColorsAvailable = new SyncList<int>();

    private void Start()
    {
        ColorsAvailable.Callback += ColorsAvailable_Callback;
        
        if(base.isServer == false) return;

        _matchTimer = -1;
        _prepareTimer = -1;
        
        for(int i = 0; i < db.playerColors.Count; i++){
            ColorsAvailable.Add(i);
            
        }
    }

    private void Update()
    {
        if(base.isServer == false) return;
        
        if (_prepareTimer > 0)
            _prepareTimer -= Time.deltaTime;

        if (_prepareTimer <= 0 && _prepareTimer != -1){
            InternalStartMatch();
            _prepareTimer = -1;
        }
        
        if(_matchTimer > 0)
            _matchTimer -= Time.deltaTime;
            
        if(_matchTimer <= 0 && _matchTimer != -1){

            InternalEndMatch();
            _matchTimer = -1;
            
            
        }
        
    }

    [Command(requiresAuthority = false)]
    public void CmdPrepareMath() {
        InternalPrepareMath();
    }
    
    void InternalPrepareMath() {

        _prepareTimer = db.serverPrepareDuration;

    }
    
    void InternalStartMatch() {
        
        // Teleporta players
        
        _prepareTimer = db.serverMatchDuration;
        
    }

    void InternalEndMatch(){
        
    }
    //
    void ColorsAvailable_Callback(SyncList<int>.Operation op, int itemindex, int olditem, int newitem)
    {
       
    }
    
}
