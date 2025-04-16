using TMPro;
using UnityEngine;
using System;

public class ManagerUICelular : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI PingText;

    void FixedUpdate()
    {
        DateTime currentTime = DateTime.Now;
        string timeString = currentTime.ToString("HH:mm");
        clockText.text = timeString;
    }
}
