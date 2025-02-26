using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TriggerCheck : NetworkBehaviour
{
    public Transform spawnPoint; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out NetworkIdentity identity))
        {
            if (identity.isLocalPlayer) 
            {
                other.GetComponent<PlayerScript>().RespawnAt(spawnPoint.position);
            }
        }
    }
}