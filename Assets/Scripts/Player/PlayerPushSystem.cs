using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPushSystem : NetworkBehaviour
{
 
    [SerializeField] PlayerControlSO _playerSO;
    [SerializeField] Database db;

    [SerializeField] private Transform pushOrigin;
    [SerializeField] private Camera playerCamera;
    private float lastPushTime;


    private void Start()
    {
        _playerSO.EventOnPush += OnEventPush; 

    }
    private void OnDestroy()
    {
        if (_playerSO != null)
        {
            _playerSO.EventOnPush -= OnEventPush;
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
        
        Vector3 rayDirection = playerCamera.transform.forward;

        if (Physics.Raycast(rayStart, rayDirection, out hit, db.pushRadius))
        {
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
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, db.pushRadius);
        if (playerCamera != null)
        {
            Gizmos.DrawRay(pushOrigin.position, playerCamera.transform.forward * db.pushRadius);
        }
    }
}