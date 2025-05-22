using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class LobbySlot : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image readyImage;

    private ulong playerId;

    public void Initialize(ulong id)
    {
        playerId = id;
    }

    public void Refresh(string alias, bool isReady)
    {
        nameText.text = alias;
        readyImage.color = isReady ? Color.green : Color.red;
    }

    public ulong PlayerId => playerId;
}