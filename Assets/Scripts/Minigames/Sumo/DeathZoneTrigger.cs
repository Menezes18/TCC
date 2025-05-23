using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;


public class DeathZoneTrigger : NetworkBehaviour
{
    public UnityEvent<PlayerData> onDeath;
    
    public PlayerData players;

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        var pd = other.GetComponent<PlayerData>();
        
        if (pd == null) return;
        players = pd;
    }

    [ServerCallback]
    void OnTriggerExit(Collider other)
    {
        var pd = other.GetComponent<PlayerData>();
        var pds = other.GetComponent<PlayerScript>();
        if (pd == null) return;
        
        onDeath?.Invoke(pd);
        
        
        pds.InternalDeath();
    }
}
