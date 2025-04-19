using UnityEngine;
using TMPro;

public class UIDropdown_Quality : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private void Start()
    {
        SettingsManager.Instance.RegisterSetting(SettingType.Quality, new QualityCommand());
        dropdown.onValueChanged.AddListener(OnChange);
    }

    private void OnChange(int index)
    {
        SettingsManager.Instance.ApplySetting(SettingType.Quality, index);
        Debug.Log($"Qualidade alterada para: {QualitySettings.names[index]} (index {index})");
    }
}