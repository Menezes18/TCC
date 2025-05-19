using System;
using UnityEngine;
using UnityEngine.Events;

public class OnEnableDisableTool : MonoBehaviour
{

    public UnityEvent EventOnEnable;
    public UnityEvent EventOnDisable;
    
    void OnEnable()
    {
        this.EventOnEnable?.Invoke();
    }

    void OnDisable()
    {
        this.EventOnDisable?.Invoke();
    }
}
