using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TriggerCheck : NetworkBehaviour
{
    public Transform spawnPoint; 
    public bool kill = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out NetworkIdentity identity))
        {
            if (identity.isLocalPlayer) 
            {
                if(!kill)other.GetComponent<PlayerScript>().RespawnAt(spawnPoint.position);
                else other.GetComponent<PlayerScript>().Die();
            }
        }
    }
}