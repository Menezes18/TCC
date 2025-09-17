using System;
using UnityEngine;
using TMPro;
using Mirror;

public class NetworkPingDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pingText;
    [SerializeField] private TextMeshProUGUI qualityText;

    private void Update()
    {
        // if (!NetworkClient.active || pingText == null || qualityText == null)
        //     return;

        double ping = Math.Round(NetworkTime.rtt * 1000 / 2);
        pingText.text = $"Ping: {ping}ms";

        var quality = NetworkClient.connectionQuality;
        qualityText.text = $"Q: {new string('-', (int)quality)}";
        qualityText.color = quality.ColorCode(); 
    }
}