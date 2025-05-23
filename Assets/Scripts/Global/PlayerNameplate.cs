using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using JetBrains.Annotations;
using Steamworks;
public class PlayerNameplate : MonoBehaviour
{
    
    [SerializeField] Database db;
    [SerializeField] TMP_Text _nameplate;
    [SerializeField] Image _sprite;

    [SerializeField] string nameTest;
    private void Start()
    {
        nameTest = SteamFriends.GetPersonaName();
        SetNameplate(null);
    }

    public void SetNameplate([CanBeNull] string playerName)
    {
    
        _nameplate.text = nameTest;
    }

    private void Update()
    {
        SteamAPI.RunCallbacks();
    }

    public void SetColor(int color)
    {
        _sprite.color = db.playerColors[color].color;
    }

}
