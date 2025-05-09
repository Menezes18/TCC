﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class ButtonToggle : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform handleTransform;

    [Header("Cores")]
    [SerializeField] private Color onColor  = new Color(0.0f, 0.6f, 0.0f);
    [SerializeField] private Color offColor = new Color(0.8f, 0.8f, 0.8f);

    [Header("Posições do Handle")]
    [SerializeField] private Vector2 onHandlePos  = new Vector2(20f, 0f);
    [SerializeField] private Vector2 offHandlePos = new Vector2(-20f, 0f);

    [Header("Animação")]
    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private Ease  easeType          = Ease.OutQuad;

    [Header("Eventos")]
    public UnityEvent onActivated;    
    public UnityEvent onDeactivated;  

    private Button button;
    public bool _isOn;
    public bool isOn
    {
        get => _isOn;
        set
        {
            if (_isOn == value) return; 
            _isOn = value;
            AnimateToggle();

            if (_isOn)
                onActivated.Invoke();
            else
                onDeactivated.Invoke();
        }
    }

    private void Awake()
    {
        AnimateToggle();
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);

        backgroundImage.color         = offColor;
        handleTransform.anchoredPosition = offHandlePos;
        isOn = _isOn;
    }

    private void OnButtonClick()
    {
        _isOn = !_isOn;
        AnimateToggle();

        if (_isOn)
            onActivated.Invoke();
        else
            onDeactivated.Invoke();
    }

    private void AnimateToggle()
    {
        backgroundImage
            .DOColor(_isOn ? onColor : offColor, animationDuration)
            .SetEase(easeType);

        handleTransform
            .DOAnchorPos(_isOn ? onHandlePos : offHandlePos, animationDuration)
            .SetEase(easeType);
    }
}