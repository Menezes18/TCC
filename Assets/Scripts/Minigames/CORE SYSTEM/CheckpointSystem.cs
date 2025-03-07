using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CheckpointSystem : NetworkBehaviour
{
    private Dictionary<uint, Transform> _playerCheckpoints = new Dictionary<uint, Transform>();
    private List<Checkpoint> _checkpoints = new List<Checkpoint>();

    public void RegisterCheckpoint(Checkpoint checkpoint)
    {
        if (!_checkpoints.Contains(checkpoint))
        {
            _checkpoints.Add(checkpoint);
        }
    }

    [Server]
    public void SetPlayerCheckpoint(PlayerScript player, Transform checkpoint)
    {
        if (_playerCheckpoints.ContainsKey(player.netId))
        {
            _playerCheckpoints[player.netId] = checkpoint;
        }
        else
        {
            _playerCheckpoints.Add(player.netId, checkpoint);
        }
        
        // Notificar clientes
        RpcCheckpointReached(player.netId, checkpoint.GetComponent<NetworkIdentity>().netId);
    }

    public Transform GetLastCheckpoint(PlayerScript player)
    {
        if (_playerCheckpoints.TryGetValue(player.netId, out Transform checkpoint))
        {
            return checkpoint;
        }
        
        // Retornar checkpoint padrão se nenhum foi registrado
        return _checkpoints.Count > 0 ? _checkpoints[0].transform : null;
    }

    [ClientRpc]
    private void RpcCheckpointReached(uint playerId, uint checkpointId)
    {
        // Lógica de feedback visual para o cliente
        Debug.Log($"Player {playerId} reached checkpoint {checkpointId}");
    }
}
