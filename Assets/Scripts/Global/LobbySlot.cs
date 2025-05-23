using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class LobbySlot : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] Image readyIndicator;
    [SerializeField] Image avatarImage;

    private ulong steamId;

    public void Initialize(ulong id)
    {
        steamId = id;
    }

    public void Refresh(string alias, bool isReady)
    {
        nameText.text = alias;
        readyIndicator.color = isReady ? Color.green : Color.red;
    }
}
