using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class PlayerRespawn : NetworkBehaviour
{

    [SerializeField] Database db;
    [SerializeField] HUDSO hudso; 
    
    [SyncVar (hook = nameof(HookOnTimerUpdate))] public float timer = -1;

    public UnityEvent EventOnTimerExpired;

    private void Start()
    {
        if (base.isServer == false) return;
        
        timer = -1;
    }

    private void Update()
    {
        if (base.isServer == false) return;
        
        if(timer > 0)
            timer -= Time.deltaTime;
        
        if (timer <= 0 && timer != -1)
        {
            this.EventOnTimerExpired?.Invoke();
            timer = -1;

            PlayerScript ps = transform.GetComponent<PlayerScript>();
            NetworkConnection conn = transform.GetComponent<NetworkIdentity>().connectionToClient;

            Transform random = MatchManager.singleton.GetRandomSpawnPoint();

            ps.TargetRpcTeleport(conn, random.position, random.rotation);
        }
        
    }

    [Server]
    public void ServerSetRespawnTimer()
    {
        timer = db.playerRespawnDuration;
    }

    void HookOnTimerUpdate(float oldVal, float newVal)
    {
        if(timer == -1) return;
        hudso.RespawnTimerUpdate(newVal);
    }
}
