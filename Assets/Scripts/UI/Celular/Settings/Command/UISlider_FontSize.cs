using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UISlider_FontZoom : MonoBehaviour
{
    [SerializeField] private Slider fontSizeSlider;
    [SerializeField] private Transform[] uiRoot; 

    private TextMeshProUGUI[] texts;
    private float[] originalFontSizes;

    private void Start()
    {
        foreach (var root in uiRoot){
            texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        }
        originalFontSizes = new float[texts.Length];

        for (int i = 0; i < texts.Length; i++)
        {
            originalFontSizes[i] = texts[i].fontSize;
        }

        fontSizeSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float multiplier)
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                texts[i].fontSize = originalFontSizes[i] * multiplier;
            }
        }
    }
}