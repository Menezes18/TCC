using System;
using UnityEngine;
using UnityEngine.UI;

public class UIToggle_ShowFPS : MonoBehaviour
{
    private void Start()
    {
        SettingsManager.Instance.RegisterSetting(SettingType.ShowFPS, new ShowFPSCommand());
    }

    public void OnToggleChanged(bool isOn)
    {
        SettingsManager.Instance.ApplySetting(SettingType.ShowFPS, isOn);
    }
    
}