using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public GameObject slotPrefab;
    public Transform slotsParent;

    private readonly Dictionary<ulong, LobbySlot> slotsById = new Dictionary<ulong, LobbySlot>();
    private readonly Dictionary<PlayerData, ulong> idsByPlayer = new Dictionary<PlayerData, ulong>();
    private readonly List<PlayerData> stalePlayers = new List<PlayerData>();
    private PlayerList _playerList;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        BindPlayerList();
    }

    private void Start() => BindPlayerList();

    private void OnDisable()
    {
        if (_playerList != null) _playerList.players.Callback -= OnPlayersChanged;
        foreach (var player in idsByPlayer.Keys)
            if (player != null) player.LobbyDataChanged -= RefreshPlayer;
        idsByPlayer.Clear();
        _playerList = null;
    }

    private void BindPlayerList()
    {
        if (_playerList == PlayerList.singleton) return;
        OnDisable();
        _playerList = PlayerList.singleton;
        if (_playerList == null) return;
        _playerList.players.Callback += OnPlayersChanged;
        ReconcileRoster();
    }

    private void OnPlayersChanged(SyncList<PlayerData>.Operation _, int __, PlayerData ___, PlayerData ____)
        => ReconcileRoster();

    public void RefreshLobby() => ReconcileRoster();

    private void ReconcileRoster()
    {
        if (_playerList == null) return;
        foreach (var player in _playerList.players)
        {
            if (player == null || idsByPlayer.ContainsKey(player)) continue;
            idsByPlayer[player] = 0;
            player.LobbyDataChanged += RefreshPlayer;
            RefreshPlayer(player);
        }

        stalePlayers.Clear();
        foreach (var player in idsByPlayer.Keys)
            if (player == null || !_playerList.players.Contains(player)) stalePlayers.Add(player);
        foreach (var player in stalePlayers) RemovePlayer(player);
    }

    private void RefreshPlayer(PlayerData player)
    {
        if (player == null || !idsByPlayer.TryGetValue(player, out ulong previousId)) return;
        ulong playerId = player.playerInfo.steamId;
        if (previousId != 0 && previousId != playerId) RemoveSlot(previousId);
        idsByPlayer[player] = playerId;
        if (playerId == 0) return;

        if (!slotsById.TryGetValue(playerId, out var slot))
        {
            var go = Instantiate(slotPrefab, slotsParent);
            slot = go.GetComponent<LobbySlot>();
            slot.Initialize(playerId);
            slotsById[playerId] = slot;
        }
        slot.Refresh(player.alias, player.IsReady, player.color);
    }

    private void RemovePlayer(PlayerData player)
    {
        if (player != null) player.LobbyDataChanged -= RefreshPlayer;
        if (idsByPlayer.TryGetValue(player, out ulong playerId) && playerId != 0) RemoveSlot(playerId);
        idsByPlayer.Remove(player);
    }

    private void RemoveSlot(ulong playerId)
    {
        if (!slotsById.TryGetValue(playerId, out var slot)) return;
        if (slot != null) Destroy(slot.gameObject);
        slotsById.Remove(playerId);
    }
}
