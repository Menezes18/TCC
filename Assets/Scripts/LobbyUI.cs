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

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        RefreshLobby();
    }

    public void RefreshLobby()
    {
        var foundPlayerData = FindObjectsOfType<PlayerData>();
        var seenIds = new HashSet<ulong>();

        foreach (var pd in foundPlayerData)
        {
            if (pd.playerInfo.steamId == 0) continue; 

            seenIds.Add(pd.playerInfo.steamId);

            if (!slotsById.TryGetValue(pd.playerInfo.steamId, out var slot))
            {
                var go = Instantiate(slotPrefab, slotsParent);
                slot = go.GetComponent<LobbySlot>();
                slot.Initialize(pd.playerInfo.steamId);
                slotsById[pd.playerInfo.steamId] = slot;
            }

            slot.Refresh(pd.alias, pd.IsReady, pd.color); 
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
}