using System;
using UnityEngine;
using UnityEngine.Events;

public class RelayTriggerEvents : MonoBehaviour
{
    public UnityEvent EventOnTriggerEnter;
    public UnityEvent EventOnTriggerExit;
    
    private void OnTriggerEnter(Collider other)
    {
        this.EventOnTriggerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        this.EventOnTriggerExit?.Invoke();
    }
}
