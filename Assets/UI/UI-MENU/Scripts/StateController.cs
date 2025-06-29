using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StateController : MonoBehaviour
{
    [Header("Canvas Type")]
    public CanvasType canvasType;
    
    [Header("Canvas Settings")]
    //public bool canGoPreviousCanvas;
    public CanvasType previousCanvas;

    [Header("UI Settings")]
    public Button StartSelectable;

    public TextMeshProUGUI titleText;
    
    StateManager stateManager;

    private void Awake()
    {
        if(titleText != null)
            titleText.text = canvasType.title;
    }

    private void OnEnable()
    {
        stateManager = StateManager.GetInstance();

    }

    
}
