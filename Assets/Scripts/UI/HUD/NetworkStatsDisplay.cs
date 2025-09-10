using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using Steamworks;

public class NetworkStatsDisplay : MonoBehaviour
{
    public Color fontColor = Color.white;
    public int padding = 10;
    public int width = 320;
    public int height = 100;
    public int fontSize = 16;

    private float qualityLocal = 0f;
    private float qualityRemote = 0f;
    private float updateTimer;
    private float updateRate = 1.5f;

    void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateRate)
        {
            updateTimer = 0f;
            UpdateConnectionStats();
        }
    }

    void OnGUI()
    {
        if (!NetworkClient.active) return;

        GUI.color = fontColor;
        GUIStyle style = GUI.skin.GetStyle("Label");
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperRight;

        Rect rect = new Rect(Screen.width - width - padding, Screen.height - height - padding, width, height);

        GUILayout.BeginArea(rect);
        GUILayout.BeginVertical(style);
        GUILayout.Label($"RTT: {Mathf.RoundToInt((float)(NetworkTime.rtt * 1000))} ms");
        GUILayout.Label($"Qualidade Local: {qualityLocal * 100f:F1}%");
        GUILayout.Label($"Qualidade Remota: {qualityRemote * 100f:F1}%");
        GUILayout.EndVertical();
        GUILayout.EndArea();

        GUI.color = Color.white;
    }

    void UpdateConnectionStats()
    {
        if (!SteamManager.Initialized) return;

        HSteamNetConnection connection = MyNetworkManager.manager?.steamConnection ?? HSteamNetConnection.Invalid;
        if (connection == HSteamNetConnection.Invalid) return;

        SteamNetConnectionRealTimeStatus_t status = new SteamNetConnectionRealTimeStatus_t();
        SteamNetConnectionRealTimeLaneStatus_t dummyLane = new SteamNetConnectionRealTimeLaneStatus_t();

        EResult result = SteamNetworkingSockets.GetConnectionRealTimeStatus(
            connection,
            ref status,
            0,
            ref dummyLane
        );


        if (result == EResult.k_EResultOK)
        {
            qualityLocal = status.m_flConnectionQualityLocal;
            qualityRemote = status.m_flConnectionQualityRemote;
        }
    }
}