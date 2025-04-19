using UnityEngine;
using UnityEngine.UI;

public class UISlider_Sensitivity : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;

    private void Start()
    {
        SettingsManager.Instance.RegisterSetting(SettingType.Sensitivity, new SensitivityCommand());

        sensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        Debug.Log($"Sensibilidade alterada para: {value}");
        SettingsManager.Instance.ApplySetting(SettingType.Sensitivity, value);
    }
}