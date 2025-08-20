using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class MusicVolumeSlider : MonoBehaviour
{
    Slider _slider;

    void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.onValueChanged.AddListener(OnSliderChange);
        
    }

    void Start()
    {
        _slider.SetValueWithoutNotify(AudioManager.Instance.Volume);
    }

    void OnSliderChange(float value)
    {
        AudioManager.Instance.SetVolume(value);
    }
}