using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
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

            // if (MatchManager.singleton != null){
            //     Transform random = MatchManager.singleton.GetRandomSpawnPoint();
            //     ps.TargetRpcTeleport(conn, random.position, random.rotation);
            // }
            // else
            {
                var spot = FindRandomStart();
                ps.TargetRpcTeleport(conn,
                    spot ? spot.position : Vector3.zero,
                    spot ? spot.rotation : Quaternion.identity);
            }

            
            
        }
        
    }
    public static Transform FindRandomStart()
    {
        var starts = UnityEngine.Object.FindObjectsOfType<NetworkStartPosition>(true);
        return starts.Length > 0 ? starts[Random.Range(0, starts.Length)].transform : null;
    }
    [Server]
    public void ServerSetRespawnTimer()
    {
        timer = db.playerRespawnDuration;
    }

    void HookOnTimerUpdate(float oldVal, float newVal)
    {
        if (!isLocalPlayer) return;
        
        if(timer == -1) return;
        hudso.RespawnTimerUpdate(newVal);
    }
}
