using TMPro;
using UnityEngine;
using System;

public class ManagerUICelular : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI PingText;

    private string _lastDisplayedMinute;

    void Update()
    {
        DateTime currentTime = DateTime.Now;
        string timeString = currentTime.ToString("HH:mm");
        if (timeString == _lastDisplayedMinute) return;
        _lastDisplayedMinute = timeString;
        clockText.text = timeString;
    }
}
