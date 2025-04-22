using System;
using UnityEngine;
using UnityEngine.UI;

public class UIToggle_ShowPing : MonoBehaviour
{
    private void Start()
    {
        SettingsManager.Instance.RegisterSetting(SettingType.ShowPing, new ShowPingCommand());
    }

    public void OnToggleChanged(bool isOn)
    {
        Debug.Log("Showpinggg");
        SettingsManager.Instance.ApplySetting(SettingType.ShowPing, isOn);
    }
    
}