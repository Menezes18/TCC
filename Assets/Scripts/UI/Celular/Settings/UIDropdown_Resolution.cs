using UnityEngine;
using TMPro;

public class UIDropdown_Resolution : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private void Start()
    {
        SettingsManager.Instance.RegisterSetting(SettingType.Resolution, new ResolutionCommand());
        dropdown.onValueChanged.AddListener(OnChange);
    }

    private void OnChange(int index)
    {
        Vector2 resolution = index switch
        {
            0 => new Vector2(2560, 1440),
            1 => new Vector2(1920, 1080),
            2 => new Vector2(1600, 900),
            3 => new Vector2(1280, 720),
            4 => new Vector2(800, 600),
            _ => new Vector2(1920, 1080)
        };
        SettingsManager.Instance.ApplySetting(SettingType.Resolution, resolution);
        Debug.Log("Resolução: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height);

    }
}