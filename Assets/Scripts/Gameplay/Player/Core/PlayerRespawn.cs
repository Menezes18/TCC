using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class PlayerRespawn : NetworkBehaviour
{
    [SerializeField] Database db;
    [SerializeField] HUDSO hudso; 
    [SerializeField] private MatchManager _matchManager; // Item14 soft ref
    private IHudEvents _hudEvents; // reutiliza adapter
    
    [SyncVar (hook = nameof(HookOnTimerUpdate))] public float timer = -1;

    public UnityEvent EventOnTimerExpired;

    private void Start()
    {
        if (base.isServer == false) return;
        
        timer = -1;
        _matchManager = SingletonFallback.Resolve(_matchManager, () => MatchManager.singleton, this, nameof(_matchManager));
        _hudEvents = new HudSoAdapter(hudso);
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

            var mm = _matchManager ?? MatchManager.singleton;
            Transform spawn = null;
            if (mm != null)
            {
                // A API recomendada agora é via fluxo de TeleportPlayer interno; como não temos acesso direto ao provider aqui
                // usamos o método obsoleto apenas como fallback evitando warning redundante (suprimindo via pragma if needed).
#pragma warning disable CS0618
                spawn = mm.GetRandomSpawnPoint();
#pragma warning restore CS0618
            }
            if (spawn == null)
            {
                Debug.LogWarning("[Respawn] SpawnPoint nulo (provider não configurado)");
                return;
            }
            ps.TargetRpcTeleport(conn, spawn.position, spawn.rotation);
        }
        
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
        hudso.RespawnTimerUpdate(newVal); // manter chamada direta (listeners existentes)
    }
}
