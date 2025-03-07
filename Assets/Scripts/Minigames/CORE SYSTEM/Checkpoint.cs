using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Checkpoint : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        
        PlayerScript player = other.GetComponent<PlayerScript>();
        if (player != null)
        {
            CheckpointSystem checkpointSystem = FindObjectOfType<CheckpointSystem>();
            checkpointSystem.SetPlayerCheckpoint(player, transform);
        }
    }

    private void Start()
    {
        CheckpointSystem checkpointSystem = FindObjectOfType<CheckpointSystem>();
        checkpointSystem.RegisterCheckpoint(this);
    }
}
