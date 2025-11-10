using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class LobbySlot : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text readyIndicator;
    [SerializeField] Image readyPrompt;
    [SerializeField] Image playerColor;

    [SerializeField] Database database;
    private ulong steamId;

    public void Initialize(ulong id)
    {
        steamId = id;
    }

    public void Refresh(string alias, bool isReady, int colorPlayer)
    {
        nameText.text = alias;
        readyIndicator.color = isReady ? Color.green : Color.red;
        readyIndicator.text = "Pronto!";
        readyPrompt.gameObject.SetActive(false);
        playerColor.color = database.GetColor(colorPlayer);
    }
}
