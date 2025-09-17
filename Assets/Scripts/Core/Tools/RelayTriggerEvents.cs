using System;
using UnityEngine;
using UnityEngine.Events;
using Mirror;


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
       
        var nid = other.GetComponent<NetworkIdentity>();
        if (nid != null && !nid.isOwned) return;
        
        this.EventOnTriggerEnter?.Invoke();
        this.EventOnColliderEnter?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        
        var nid = other.GetComponent<NetworkIdentity>();
        if (nid != null && !nid.isOwned) return;
        
        this.EventOnTriggerExit?.Invoke();
        this.EventOnColliderExit?.Invoke(other);
    }
    
    
}
