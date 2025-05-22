using System;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public GameObject slotPrefab;
    public Transform slotsParent;

    private readonly Dictionary<ulong, LobbySlot> slotsById = new Dictionary<ulong, LobbySlot>();

    private void Awake()
    {
        Instance = this;
        MyNetworkManager.manager.onClientsChanged += SyncSlots;
    }

    private void OnDestroy()
    {
        if (MyNetworkManager.manager != null)
            MyNetworkManager.manager.onClientsChanged -= SyncSlots;
    }

    private void Start()
    {
        SyncSlots();
    }

    private void SyncSlots()
    {
        var seenIds = new HashSet<ulong>();

        foreach (var pd in MyNetworkManager.manager.allClients)
        {
            seenIds.Add(pd.playerInfo.steamId);
            if (!slotsById.TryGetValue(pd.playerInfo.steamId, out var slot))
            {
                var go = Instantiate(slotPrefab, slotsParent);
                slot = go.GetComponent<LobbySlot>();
                slot.Initialize(pd.playerInfo.steamId);
                slotsById[pd.playerInfo.steamId] = slot;
            }
            // primeiro refresh
            slot.Refresh(pd.alias, pd.IsReady);
        }

        foreach (var id in new List<ulong>(slotsById.Keys))
        {
            if (!seenIds.Contains(id))
            {
                Destroy(slotsById[id].gameObject);
                slotsById.Remove(id);
            }
        }
    }

    private void Update()
    {
        foreach (var pd in MyNetworkManager.manager.allClients)
        {
            if (slotsById.TryGetValue(pd.playerInfo.steamId, out var slot))
            {
                slot.Refresh(pd.alias, pd.IsReady);
            }
        }
    }
}

