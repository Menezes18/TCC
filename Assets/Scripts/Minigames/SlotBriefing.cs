using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Steamworks;

public class SlotBriefing : MonoBehaviour
{
    public Database db;
    public TextMeshProUGUI nameText;
    public Image image;
    public Image imageColor;
    public Image imageBackground;
    ulong steamID;
    
    private void Awake()
    {
        Setup();
    }

    public void Setup()
    {
        
        CSteamID myId = SteamUser.GetSteamID();
        string steamName = SteamFriends.GetFriendPersonaName(myId);
        ulong steamIdValue = myId.m_SteamID;
        steamID = steamIdValue;
        InitSlot(steamIdValue, steamName,0, false);
    }
    
    public void InitSlot(ulong steamId, string alias,int colorPlayer, bool isReady)
    {
        nameText.text = alias;
        nameText.color = isReady ? Color.green : Color.red;
        image.color = isReady ? Color.green : Color.red;
        
        imageColor.color = db.GetColor(colorPlayer);
    }

    public void UpdateSlotReady(bool isReady, ulong pdsteamId)
    {
        if(pdsteamId == steamID)
            image.color = isReady ? Color.green : Color.red;
    }
}
