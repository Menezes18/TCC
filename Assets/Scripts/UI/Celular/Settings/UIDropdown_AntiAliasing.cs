using UnityEngine;
using TMPro;

public class UIDropdown_AntiAliasing : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private void Start()
    {
        SettingsManager.Instance.RegisterSetting(SettingType.AntiAliasing, new AntiAliasingCommand());
        dropdown.onValueChanged.AddListener(OnChange);
    }

    private void OnChange(int index)
    {
        int aaLevel = index switch
        {
            0 => 0,
            1 => 2,
            2 => 4,
            3 => 8,
            _ => 0
        };
        SettingsManager.Instance.ApplySetting(SettingType.AntiAliasing, aaLevel);
        Debug.Log("Current AntiAliasing (Unity): " + QualitySettings.antiAliasing);

    }
}