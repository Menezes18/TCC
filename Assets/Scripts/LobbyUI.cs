using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public GameObject slotPrefab;
    public Transform slotsParent;

    private readonly Dictionary<ulong, LobbySlot> slotsById = new Dictionary<ulong, LobbySlot>();
    private PlayerList _playerList;

    // throttling para evitar FindObjects por frame
    [SerializeField] private float refreshFps = 5f; // 5x/s é suficiente
    private float _nextRefresh;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        // hook into manager events when possible to avoid scanning
        _playerList = PlayerList.singleton;
        if (_playerList != null)
        {
            // SyncList doesn't have standard C# events; we refresh on a light throttle as fallback
            _nextRefresh = 0f;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + (1f / Mathf.Max(1f, refreshFps));
        RefreshLobby();
    }

    // reusable temporaries to avoid per-frame GC
    private static readonly List<ulong> _tmpKeys = new List<ulong>(32);
    private static readonly HashSet<ulong> _tmpSeen = new HashSet<ulong>();
    public void RefreshLobby()
    {
        _tmpSeen.Clear();
        var list = _playerList != null ? _playerList.players : null;
        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var pd = list[i];
                if (pd == null || pd.playerInfo.steamId == 0) continue;
                ulong id = pd.playerInfo.steamId;
                _tmpSeen.Add(id);

                if (!slotsById.TryGetValue(id, out var slot))
                {
                    var go = Instantiate(slotPrefab, slotsParent);
                    slot = go.GetComponent<LobbySlot>();
                    slot.Initialize(id);
                    slotsById[id] = slot;
                }
                slot.Refresh(pd.alias, pd.IsReady, pd.color);
            }
        }
        // remove missing
        _tmpKeys.Clear();
        foreach (var kv in slotsById) _tmpKeys.Add(kv.Key);
        for (int i = 0; i < _tmpKeys.Count; i++)
        {
            var id = _tmpKeys[i];
            if (!_tmpSeen.Contains(id))
            {
                if (slotsById.TryGetValue(id, out var slot) && slot != null)
                    Destroy(slot.gameObject);
                slotsById.Remove(id);
            }
        }
    }
}