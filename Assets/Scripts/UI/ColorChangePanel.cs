using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ColorChangePanel : MonoBehaviour{
    private PlayerList playerList => PlayerList.singleton;
    
    [SerializeField] Database db;
    
    [SerializeField] List<CustomButton> buttons;

    private void Start()
    {
        //
        for (int i = 0; i < buttons.Count; i++)
        {
            
            if(i >= db.playerColors.Count) continue;
            
            buttons[i].Sprite.color = db.playerColors[i];
        }
        
        //
        playerList.players.Callback += PlayersOnCallback;
    }

    private void OnDestroy()
    {
        playerList.players.Callback -= PlayersOnCallback;
    }

    public void Refresh()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool occupied = buttons[i].interactable = playerList.ColorsAvailable.Contains(i) == true;
            buttons[i].interactable = occupied;
            
            
        }
    }
    
    private void PlayersOnCallback(SyncList<PlayerData>.Operation op, int itemindex, PlayerData olditem, PlayerData newitem)
    {
        Refresh();
    }
}
