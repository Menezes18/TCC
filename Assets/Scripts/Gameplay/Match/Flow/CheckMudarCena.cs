using Mirror;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkIdentity))]
public class CheckMudarCena : NetworkBehaviour
{
    [SerializeField] private string mudarCena = string.Empty;
    [SerializeField, Min(0)] private int jogadoresPlataforma = 0;
    private readonly Dictionary<uint, HashSet<Collider>> _playerCollidersOnPlatform = new Dictionary<uint, HashSet<Collider>>();
    private readonly List<uint> _stalePlayers = new List<uint>();
    private bool _transitionRequested;
    [Server]
    void TrocarCena(string cena)
    {
        MyNetworkManager.manager.ServerChangeSceneSynchronized(cena);
    }

    [ServerCallback]
    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            var identity = other.GetComponentInParent<NetworkIdentity>();
            if (identity == null || identity.netId == 0) return;
            if (!_playerCollidersOnPlatform.TryGetValue(identity.netId, out var colliders))
                _playerCollidersOnPlatform[identity.netId] = colliders = new HashSet<Collider>();
            if (!colliders.Add(other)) return;
            UpdateProgress();
        }
    }

    [ServerCallback]
    public void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            var identity = other.GetComponentInParent<NetworkIdentity>();
            if (identity == null || !_playerCollidersOnPlatform.TryGetValue(identity.netId, out var colliders) ||
                !colliders.Remove(other)) return;
            if (colliders.Count == 0) _playerCollidersOnPlatform.Remove(identity.netId);
            UpdateProgress();
        }
    }

    [ServerCallback]
    private void Update()
    {
        _stalePlayers.Clear();
        foreach (var pair in _playerCollidersOnPlatform)
        {
            pair.Value.RemoveWhere(playerCollider => playerCollider == null);
            if (pair.Value.Count == 0 || !NetworkServer.spawned.ContainsKey(pair.Key))
                _stalePlayers.Add(pair.Key);
        }
        if (_stalePlayers.Count == 0) return;
        foreach (uint netId in _stalePlayers) _playerCollidersOnPlatform.Remove(netId);
        UpdateProgress();
    }

    [Server]
    private void UpdateProgress()
    {
        jogadoresPlataforma = _playerCollidersOnPlatform.Count;
        Debug.Log("Jogadores na plataforma: " + jogadoresPlataforma);
        int eligiblePlayers = MyNetworkManager.manager != null ? MyNetworkManager.manager.allClients.Count : 0;
        if (!_transitionRequested && eligiblePlayers > 0 && jogadoresPlataforma >= eligiblePlayers)
        {
            _transitionRequested = true;
            TrocarCena(mudarCena);
        }
    }
}
