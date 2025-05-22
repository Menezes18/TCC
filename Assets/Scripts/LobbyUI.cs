using System;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [Header("Referências")]
    public Database db;                  
    public Transform slotsParent;        
    public GameObject slotPrefab;        

    private readonly List<GameObject> slots = new List<GameObject>();

    private SyncList<PlayerData>.SyncListChanged playersCallback;

    void Awake() => Instance = this;

    void Start()
    {
        playersCallback = OnPlayersChanged;

        // registra o callback
        PlayerList.singleton.players.Callback += playersCallback;
        RefreshLobby();
        
    }
    
    void OnDestroy()
    {
        if (PlayerList.singleton != null)
            PlayerList.singleton.players.Callback -= playersCallback;
    }

    private void OnPlayersChanged(
        SyncList<PlayerData>.Operation op, 
        int index, 
        PlayerData oldPlayer, 
        PlayerData newPlayer
    ) {
        RefreshLobby();
    }

    private void Update()
    {
        RefreshLobby();
    }

    public void RefreshLobby()
    {
        foreach (var go in slots)
            Destroy(go);
        slots.Clear();

        var list = PlayerList.singleton.players;
        for (int i = 0; i < list.Count; i++)
        {
            var pd = list[i];
            var go = Instantiate(slotPrefab, slotsParent);
            slots.Add(go);
            
            

            // Nome
            var txt = go.transform
                .Find("Name")  
                .GetComponent<TextMeshProUGUI>();
            txt.text = pd.alias;
            txt.text = pd.alias;

            // Ready Toggle
            var readyImg = go.transform.Find("readyImg").GetComponent<Image>();
            readyImg.color = pd.IsReady ? Color.green : Color.red;

        }
    }
}
