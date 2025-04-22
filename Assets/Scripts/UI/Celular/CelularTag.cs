using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
public class CelularTag : MonoBehaviour
{
    public TMP_Text username_text, money_text;
    public Image pfp_Image;
    public CharacterSkinElement currentSkinElement;
    public CSteamID steamId;
    Sprite icon;
    void Start()
    {
        if (!SteamManager.Initialized) return; // ou seu check de inicialização
    
        steamId = SteamUser.GetSteamID();
    
        string personaName = SteamFriends.GetPersonaName();
        username_text.text = personaName;
    
        Texture2D tex = SteamHelper.GetAvatar(steamId);
        if (tex != null)
            icon = SteamHelper.ConvertTextureToSprite(tex);
        pfp_Image.sprite = icon;
    }

    public void UpdateTagCelular(string username, string money) 
    {
        var cSteamID = SteamUser.GetSteamID();
        SteamHelper.GetAvatar(cSteamID);
        username_text.text = username;
        money_text.text = money;
    }

    public void UpdatePFP(Sprite icon)
    {
        pfp_Image.sprite = icon;
    }
}