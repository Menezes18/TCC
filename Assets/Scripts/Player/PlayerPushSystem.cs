using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPushSystem : NetworkBehaviour
{
 
    [SerializeField] PlayerControlSO _playerSO;
    [SerializeField] Database db;

    [SerializeField] private Transform pushOrigin; 
    private float lastPushTime;


    private void Start()
    {
        _playerSO.EventOnPush += OnEventPush; 

    }

    public void OnEventPush(InputAction.CallbackContext context)
    {
        Debug.LogError("Fire");
        if (context.phase == InputActionPhase.Performed && Time.time > lastPushTime + db.pushCooldown)
        {
            AttemptPush();
            Debug.LogError("push");
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
        if (Physics.Raycast(rayStart, rayDirection, out hit, db.pushRadius)){
            if (hit.collider.TryGetComponent(out PlayerScript targetPlayer) && targetPlayer.netId != netId)
            {
                Vector3 pushDirection = (hit.collider.transform.position - transform.position).normalized;
                
                pushDirection += Vector3.up * 0.2f;
                pushDirection.Normalize();
                
                targetPlayer.ApplyPush(pushDirection * db.pushForce);
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
        Gizmos.DrawWireSphere(transform.position, db.pushRadius);
        Gizmos.DrawRay(transform.position, transform.forward * db.pushRadius);
    }
}