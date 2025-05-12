using System;
using UnityEngine;
using UnityEngine.Events;
[Serializable]
public class ColliderEvent : UnityEvent<Collider> { }
public class RelayTriggerEvents : MonoBehaviour
{
    
    public UnityEvent EventOnTriggerEnter;
    public UnityEvent EventOnTriggerExit;
    
    
    public ColliderEvent EventOnColliderEnter;
    public ColliderEvent EventOnColliderExit;
    private void OnTriggerEnter(Collider other)
    {
        this.EventOnTriggerEnter?.Invoke();
        this.EventOnColliderEnter?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        this.EventOnTriggerExit?.Invoke();
        this.EventOnColliderExit?.Invoke(other);
    }
    
    
}
