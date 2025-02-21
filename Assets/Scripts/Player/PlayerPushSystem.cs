using System;
using Mirror;
using UnityEngine;

public class PlayerPushSystem : NetworkBehaviour
{
    [SerializeField] private float pushForce = 15f; 
    [SerializeField] private float pushCooldown = 1f;
    [SerializeField] private float pushRadius = 2f; 
    [SerializeField] private Transform pushOrigin; 
    private float lastPushTime;

    private void Update()
    {
        if (!isOwned) return;
        
        if (Input.GetKeyDown(KeyCode.E) && Time.time > lastPushTime + pushCooldown)
        {
            AttemptPush();
            lastPushTime = Time.time;
        }
    }

    private void AttemptPush()
    {
        if (!isServer)
        {
            CmdPush();
            return;
        }

        RaycastHit hit;
        
        Vector3 rayStart = pushOrigin.position;
        Vector3 rayDirection = transform.forward;
        if (Physics.Raycast(rayStart, rayDirection, out hit, pushRadius)){
            if (hit.collider.TryGetComponent(out PlayerScript targetPlayer) && targetPlayer.netId != netId)
            {
                Vector3 pushDirection = (hit.collider.transform.position - transform.position).normalized;
                // Adicionamos um pequeno componente vertical para cima
                pushDirection += Vector3.up * 0.2f;
                pushDirection.Normalize();
                
                targetPlayer.ApplyPush(pushDirection * pushForce);
            }
        }
    }

    [Command]
    private void CmdPush()
    {
        AttemptPush();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pushRadius);
        Gizmos.DrawRay(transform.position, transform.forward * pushRadius);
    }
}