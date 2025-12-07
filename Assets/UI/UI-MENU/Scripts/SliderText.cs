using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SliderText : MonoBehaviour
{
    [SerializeField] TMP_Text sliderValue;
    public Slider slider;

    void Awake()
    {

        slider.minValue = 0;
        slider.maxValue = 10;
        slider.wholeNumbers = true;

        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    void Start()
    {
        slider.SetValueWithoutNotify(AudioManager.Instance.Volume);
        AtualizarUI(AudioManager.Instance.Volume);
    }

    void OnSliderChanged(float v)
    {
        Debug.LogError(v);
        AtualizarUI(v);
        if(transform.parent.name.Contains("music")) AudioManager.Instance.SetVolume(v);
    }

    void AtualizarUI(float v)
    {
        sliderValue.text = v.ToString("F2");
    }
}