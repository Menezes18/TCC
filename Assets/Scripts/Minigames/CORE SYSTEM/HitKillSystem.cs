using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HitKillSystem : NetworkBehaviour
{
    public event Action<PlayerScript, PlayerScript> OnPlayerKilled;
    public event Action<PlayerScript, Transform> OnPlayerRespawned;

    [Server]
    public void KillPlayer(PlayerScript player, PlayerScript killer = null)
    {
        // Lógica de morte do jogador
        CheckpointSystem checkpointSystem = FindObjectOfType<CheckpointSystem>();
        Transform respawnPoint = checkpointSystem.GetLastCheckpoint(player);
        
        // Notificar clientes
        RpcPlayerKilled(player.netId, killer != null ? killer.netId : 0);
        
        // Respawn do jogador
        RespawnPlayer(player, respawnPoint);
    }

    [Server]
    public void RespawnPlayer(PlayerScript player, Transform respawnPoint)
    {
        player.transform.position = respawnPoint.position;
        player.transform.rotation = respawnPoint.rotation;
        
        // Notificar clientes
        RpcPlayerRespawned(player.netId, respawnPoint.position, respawnPoint.rotation);
        
        OnPlayerRespawned?.Invoke(player, respawnPoint);
    }

    [ClientRpc]
    private void RpcPlayerKilled(uint playerId, uint killerId)
    {
        PlayerScript player = null;
        PlayerScript killer = null;
        
        // Encontrar os jogadores pelos IDs
        foreach (var networkIdentity in NetworkClient.spawned.Values)
        {
            if (networkIdentity.netId == playerId)
                player = networkIdentity.GetComponent<PlayerScript>();
            else if (networkIdentity.netId == killerId)
                killer = networkIdentity.GetComponent<PlayerScript>();
        }
        
        if (player != null)
        {
            OnPlayerKilled?.Invoke(player, killer);
        }
    }

    [ClientRpc]
    private void RpcPlayerRespawned(uint playerId, Vector3 position, Quaternion rotation)
    {
        PlayerScript player = null;
        
        // Encontrar o jogador pelo ID
        foreach (var networkIdentity in NetworkClient.spawned.Values)
        {
            if (networkIdentity.netId == playerId)
            {
                player = networkIdentity.GetComponent<PlayerScript>();
                break;
            }
        }
        
        if (player != null && !isServer) // Servidor já atualizou a posição
        {
            player.transform.position = position;
            player.transform.rotation = rotation;
        }
    }
}
