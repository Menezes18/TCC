using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class SwitchHandler : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] float AnimationSpeed;
    

    [Header("References")]
    [SerializeField] RectTransform HandleImage;
    [SerializeField] TextMeshProUGUI indicator;
    
    Image backgroundImage, handleImage;

    Toggle toggle;

    Vector2 handlePosition;

    void Awake()
    {
        toggle = GetComponent<Toggle>();

        handlePosition = HandleImage.anchoredPosition;

        backgroundImage = HandleImage.parent.GetComponent<Image>();
        handleImage = HandleImage.GetComponent<Image>();

        toggle.onValueChanged.AddListener(OnSwitch);

        if (toggle.isOn)
            OnSwitch(true);
    }

    public void OnSwitch(bool on)
    {
        indicator.text = on ? (indicator.text = "ON") : indicator.text = "OFF";

        //uiHandleRectTransform.anchoredPosition = on ? handlePosition * -1 : handlePosition ; // no anim
        HandleImage.DOAnchorPos(on ? handlePosition * -1 : handlePosition, AnimationSpeed).SetEase(Ease.InSine);
    }

    void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnSwitch);
    }
}